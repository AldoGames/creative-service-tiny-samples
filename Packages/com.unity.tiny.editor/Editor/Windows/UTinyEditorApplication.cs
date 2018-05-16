#if NET_4_6
using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Assertions;
using Unity.Tiny.Conversions;
using Unity.Tiny.Extensions;

namespace Unity.Tiny
{
    public delegate void ProjectEventHandler(UTinyProject project);

    [InitializeOnLoad]
    public static class UTinyEditorApplication
    {
        [InitializeOnLoad]
        internal class UTinyModificationProcessor : UnityEditor.AssetModificationProcessor
        {
            static UTinyModificationProcessor()
            {
                EditorApplication.quitting += () =>
                {
                    s_DontSave = true;
                };
            }

            public class DontSaveScope : IDisposable
            {
                private readonly bool m_Value;

                public DontSaveScope()
                {
                    m_Value = s_DontSave;
                    s_DontSave = true;
                }

                public void Dispose()
                {
                    s_DontSave = m_Value;
                }
            }

            private static bool s_DontSave;

            // ReSharper disable once MemberHidesStaticFromOuterClass
            public static bool IsChanged { get; private set; }

            public static void MarkChanged()
            {
                IsChanged = true;
                s_Repaint = true;
            }

            public static void ClearChanged()
            {
                IsChanged = false;
                s_Repaint = true;
            }

            public static string[] OnWillSaveAssets(string[] paths)
            {
                if (IsChanged && !s_DontSave)
                {
                    Save();
                }

                return paths;
            }
        }

        /// <summary>
        /// The name used for the 'workspace' container project. This is used when editing standalone modules
        /// </summary>
        private const string KWorkspaceProjectName = "__workspace__";

        private static int s_WorkspaceVersion = -1;
        private static int s_ProjectVersion = -1;
        private static int s_ModuleVersion = -1;
        private static bool s_Save;
        private static bool s_Repaint;

        public static IRegistry Registry => EditorContext?.Registry;
        public static UTinyEditorContext EditorContext { get; private set; }

        public static UTinyProject Project => EditorContext?.Project;
        public static UTinyModule Module => EditorContext?.Module;
        public static EditorContextType ContextType => EditorContext?.ContextType ?? EditorContextType.None;
        public static UTinyUndo Undo => EditorContext?.Undo;
        public static UTinyEntityGroupManager EntityGroupManager => EditorContext?.EntityGroupManager;
        public static bool IsChanged => UTinyModificationProcessor.IsChanged;

        public static event Action OnChangesDetected;
        public static event ProjectEventHandler OnLoadProject;
        public static event ProjectEventHandler OnSaveProject;
        public static event ProjectEventHandler OnCloseProject;

        static UTinyEditorApplication()
        {
            // Register to unity application events
            EditorApplication.update += Update;

            // Save the project during an assembly reload
            AssemblyReloadEvents.beforeAssemblyReload += SaveTemp;

            // Save the project when exiting the Unity process
            EditorApplication.quitting += SaveTemp;
        }

        private static void Update()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
            {
                return;
            }

            if (null == EditorContext)
            {
                // Flush asset changes from the persistence system
                // We don't care about any changes unless we have a project loaded
                UTinyPersistence.ClearChanges();
                return;
            }

            // Poll for workspace changes
            if (null != EditorContext.Workspace && s_WorkspaceVersion != EditorContext.Workspace.Version)
            {
                UTinyEditorPrefs.SaveWorkspace(EditorContext.Workspace, EditorContext.PersistentObject.PersistenceId);
                s_WorkspaceVersion = EditorContext.Workspace.Version;
            }

            // Poll for file/asset changes
            var changes = UTinyPersistence.DetectChanges(Registry);

            if (changes.changesDetected)
            {
                var persistenceId = EditorContext.PersistentObject.PersistenceId;

                foreach (var change in changes.changedSources)
                {
                    // The currently opened project or module has been changed on disc
                    if (change.Equals(persistenceId))
                    {
                        // Ask the user if they want to keep their changes or reload from disc
                        if (EditorUtility.DisplayDialog($"{UTinyConstants.ApplicationName} assets changed", $"{UTinyConstants.ApplicationName} assets have changed on disk, would you like to reload the current project?", "Yes", "No"))
                        {
                            LoadPersistenceId(persistenceId);
                        }
                        else
                        {
                            UTinyModificationProcessor.MarkChanged();
                        }
                    }
                    else
                    {
                        // This is some other file. We assume they are in a readonly state and we silently reload the object
                        UTinyPersistence.ReloadObject(EditorContext.Registry, change);
                    }
                }

                foreach (var deletion in changes.deletedSources)
                {
                    // The currently opened project or module has been deleted on disc
                    if (deletion.Equals(persistenceId))
                    {
                        // Ask the user if they want to keep their changes or close the project
                        if (EditorUtility.DisplayDialog($"{UTinyConstants.ApplicationName} assets changed", "The current project has been deleted, would you like to close the current project?", "Yes", "No"))
                        {
                            // Force close the project
                            Close();
                        }
                        else
                        {
                            UTinyModificationProcessor.MarkChanged();
                            EditorContext.PersistentObject.PersistenceId = string.Empty;
                        }
                    }
                    else
                    {
                        // This is some other file. We assume they are in a readonly state and we silently reload the object
                        EditorContext.Registry.UnregisterAllBySource(deletion);
                    }
                }

                foreach (var moved in changes.movedSources)
                {
                    if (!moved.Equals(persistenceId))
                    {
                        continue;
                    }
                    
                    var path = AssetDatabase.GUIDToAssetPath(moved);
                    var asset = AssetDatabase.LoadAssetAtPath(path, typeof(UnityEngine.Object));
                    if (null != asset)
                    {
                        EditorContext.PersistentObject.Name = asset.name;
                    }
                }

                OnChangesDetected?.Invoke();
            }
           
            // Poll for module or project changes
            if (EditorContext.ContextType == EditorContextType.Project && (s_ProjectVersion != EditorContext.Project.Version || s_ModuleVersion != EditorContext.Module.Version))
            {
                EditorContext.Project.RefreshConfiguration();
                s_ProjectVersion = EditorContext.Project.Version;
                s_ModuleVersion = EditorContext.Module.Version;
            }

            if (s_Save)
            {
                s_Save = false;
                
                // NOTE: It is possible that this call will fail
                Save();
            }

            if (s_Repaint)
            {
                UTinyEditorUtility.RepaintAllWindows();
                s_Repaint = false;
            }
        }

        /// <summary>
        /// Creates and loads a new .utproject
        /// @NOTE The project only exists in memory until Save() is called
        /// </summary>
        public static void NewProject()
        {
            Assert.IsFalse(EditorApplication.isPlayingOrWillChangePlaymode);

            var context = new UTinyContext();
            var registry = context.Registry;

            UTinyPersistence.LoadAllModules(registry);

            // Create new objects for the project
            var project = registry.CreateProject(UTinyId.New(), "NewProject");
            var module = registry.CreateModule(UTinyId.New(), "Main");
            
            // Setup the start scene
            var entityGroup = registry.CreateEntityGroup(UTinyId.New(), "NewEntityGroup");
            var entityGroupRef = (UTinyEntityGroup.Reference) entityGroup;
            var cameraEntity = registry.CreateEntity(UTinyId.New(), "Camera");
            var transform = cameraEntity.AddComponent(registry.GetTransformType());
            transform.Refresh();
            var camera = cameraEntity.AddComponent(registry.GetCamera2DType());
            camera.Refresh();
            camera["clearFlags"] = new UTinyEnum.Reference(registry.GetCameraClearFlagsType().Dereference(registry), 1);
            camera.AssignPropertyFrom("backgroundColor", Color.black);
            camera["depth"] = -1.0f;
            entityGroup.AddEntityReference((UTinyEntity.Reference) cameraEntity);

            // Setup initial state for the project
            module.Options |= UTinyModuleOptions.ProjectModule;
            module.Namespace = "game";
            module.StartupEntityGroup = (UTinyEntityGroup.Reference) entityGroup;

            module.AddEntityGroupReference(entityGroupRef);

            project.Module = (UTinyModule.Reference) module;
            project.Settings.EmbedAssets = true;
            project.Settings.CanvasWidth = 1920;
            project.Settings.CanvasHeight = 1080;

            SetupProject(registry, project);
            
            // Always include a dependency on core, math, core2d by default
            // And HTML for now, since it is the only renderer we have right now.
            module.AddExplicitModuleDependency((UTinyModule.Reference) registry.FindByName<UTinyModule>("UTiny.HTML"));

            var workspace = new UTinyEditorWorkspace
            {
                OpenedEntityGroups = {entityGroupRef},
                ActiveEntityGroup = entityGroupRef
            };

            UTinyEditorPrefs.SaveWorkspace(workspace);

            var editorContext = new UTinyEditorContext((UTinyProject.Reference) project, EditorContextType.Project, context, workspace);
            LoadContext(editorContext, isChanged: true);
        }

        /// <summary>
        /// Creates and loads a new .utmodule
        /// @NOTE The module only exists in memory until Save() is called
        /// </summary>
        public static void NewModule()
        {
            var context = new UTinyContext();
            var registry = context.Registry;

            UTinyPersistence.LoadAllModules(registry);

            // Create a `workspace` project to host the module for editing purposes
            var project = registry.CreateProject(UTinyId.Generate(KWorkspaceProjectName), KWorkspaceProjectName);

            // Create objects for the new module
            var module = registry.CreateModule(UTinyId.New(), "NewModule");

            // Setup initial state for the module
            module.Namespace = "module";

            SetupModule(registry, module);

            project.Module = (UTinyModule.Reference) module;

            var workspace = new UTinyEditorWorkspace();

            UTinyEditorPrefs.SaveWorkspace(workspace);

            var editorContext = new UTinyEditorContext((UTinyProject.Reference) project, EditorContextType.Module, context, workspace);
            LoadContext(editorContext, isChanged: true);
        }

        /// <summary>
        /// Loads the utproject at the given file path
        /// </summary>
        /// <param name="projectFile">Relative path to the .utproject file</param>
        public static void LoadProject(string projectFile)
        {
            Assert.IsFalse(EditorApplication.isPlayingOrWillChangePlaymode);

            var context = new UTinyContext();
            var registry = context.Registry;

            UTinyPersistence.LoadProject(projectFile, registry);

            var project = registry.FindAllByType<UTinyProject>().First();
            
            Assert.IsNotNull(project);
            
            project.Name = Path.GetFileNameWithoutExtension(projectFile);

            SetupProject(registry, project);
            
            var editorContext = new UTinyEditorContext((UTinyProject.Reference) project, EditorContextType.Project, context, UTinyEditorPrefs.LoadWorkspace(project.PersistenceId));
            LoadContext(editorContext, isChanged: false);
        }

        /// <summary>
        /// Loads the utmodule at the given file path
        /// </summary>
        /// <param name="moduleFile">Relative path to the .utmodule file</param>
        public static void LoadModule(string moduleFile)
        {
            Assert.IsFalse(EditorApplication.isPlayingOrWillChangePlaymode);

            var context = new UTinyContext();
            var registry = context.Registry;

            UTinyPersistence.LoadModule(moduleFile, registry);

            var module = registry.FindAllBySource(UTinyRegistry.DefaultSourceIdentifier).OfType<UTinyModule>().First();
            
            Assert.IsNotNull(module);
            
            module.Name = Path.GetFileNameWithoutExtension(moduleFile);
            
            SetupModule(registry, module);

            var project = registry.CreateProject(UTinyId.Generate(KWorkspaceProjectName), KWorkspaceProjectName);
            project.Module = (UTinyModule.Reference) module;

            var editorContext = new UTinyEditorContext((UTinyProject.Reference) project, EditorContextType.Module, context, UTinyEditorPrefs.LoadWorkspace(project.PersistenceId));
            LoadContext(editorContext, isChanged: false);
        }

        /// <summary>
        /// Loads the given asset by its guid
        /// </summary>
        /// <param name="persistenceId"></param>
        public static void LoadPersistenceId(string persistenceId)
        {
            if (string.IsNullOrEmpty(persistenceId))
            {
                return;
            }

            var assetPath = AssetDatabase.GUIDToAssetPath(persistenceId);

            if (string.IsNullOrEmpty(assetPath))
            {
                return;
            }

            if (Path.GetExtension(assetPath).Equals(UTinyPersistence.ProjectFileExtension))
            {
                LoadProject(assetPath);
            }
            else if (Path.GetExtension(assetPath).Equals(UTinyPersistence.ModuleFileExtension))
            {
                LoadModule(assetPath);
            }
        }

        /// <summary>
        /// Saves the current project or module to the assets directory
        ///
        /// NOTE: If the project has never been saved before a `Save As` is called instead
        /// </summary>
        public static bool Save()
        {
            var persistentObject = EditorContext.PersistentObject;

            // No `PersistenceId` means this object has never been saved, promt the user with the `Save As` instead
            if (string.IsNullOrEmpty(persistentObject.PersistenceId))
            {
                return SaveAs();
            }
            
            // Use a DontSaveScope since saving may trigger the `OnWillSaveAssets` callback
            using (new UTinyModificationProcessor.DontSaveScope())
            {
                var path = UTinyPersistence.PersistObject(persistentObject);

                if (string.IsNullOrEmpty(path))
                {
                    return false;
                }
            } 
            
            UTinyEditorPrefs.SaveWorkspace(EditorContext.Workspace, persistentObject.PersistenceId);
            OnSaveProject?.Invoke(EditorContext.Project);
            UTinyModificationProcessor.ClearChanged();
            
            return true;
        }

        public static bool SaveAs()
        {
            var persistentObject = EditorContext.PersistentObject;
            var extension = UTinyPersistence.GetFileExtension(persistentObject);

            var path = EditorUtility.SaveFilePanelInProject($"Save {ContextType}", persistentObject.Name, extension.Substring(1), string.Empty);

            if (string.IsNullOrEmpty(path))
            {
                return false;
            }

            // Use a DontSaveScope since saving may trigger the `OnWillSaveAssets` callback
            using (new UTinyModificationProcessor.DontSaveScope())
            {
                // Fix-up the name
                persistentObject.Name = Path.GetFileNameWithoutExtension(path);
                
                // Flush the caretaker so this operation is not undoable
                EditorContext.Caretaker.Update();
                
                path = UTinyPersistence.PersistObjectAs(persistentObject, path);
            
                if (string.IsNullOrEmpty(path))
                {
                    return false;
                }
            } 

            UTinyEditorPrefs.SaveWorkspace(EditorContext.Workspace, persistentObject.PersistenceId);
            OnSaveProject?.Invoke(EditorContext.Project);
            UTinyModificationProcessor.ClearChanged();

            return true;
        }

        /// <summary>
        /// Prompts the user to save the current project if any changes were detected
        /// </summary>
        /// <returns>True if the project has been saved or the user decided NOT to save; False if the user canceled the save operation</returns>
        public static bool SaveChanges()
        {
            if (null == EditorContext)
            {
                return true;
            }
            
            if (IsChanged)
            {
                var dialogResult = EditorUtility.DisplayDialogComplex(
                    $"Save {ContextType}",
                    $"There are unsaved changes in the {UTinyConstants.ApplicationName} {ContextType}, do you want to save?",
                    "Yes",
                    "No",
                    "Cancel");
                
                switch (dialogResult)
                {
                    case 0:
                        // Yes: Save and continue closing the project
                        if (!Save())
                        {
                            // We failed to save the current project
                            // Bail out to avoid loss of data
                            return false;
                        }
                        
                        break;
                    
                    case 1:
                        // No: Don't save and continue closing the project
                        break;
                        
                    case 2: 
                        // Cancel: Opt out, the user has canceled the operation
                        return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Closes the current Tiny project
        /// </summary>
        public static void Close()
        {
            if (null == EditorContext)
            {
                return;
            }

            OnCloseProject?.Invoke(EditorContext.Project);

            EditorContext?.Unload();
            EditorContext = null;

            UTinyTemp.Delete();
            UTinyModificationProcessor.ClearChanged();
        }

        /// <summary>
        /// Saves the current context as a temp file
        /// </summary>
        public static void SaveTemp()
        {
            if (null == EditorContext)
            {
                return;
            }

            IPersistentObject obj;

            switch (ContextType)
            {
                case EditorContextType.Project:
                    obj = Project;
                    break;
                case EditorContextType.Module:
                    obj = Module;
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            if (string.IsNullOrEmpty(obj.PersistenceId))
            {
                // This is a temporary asset
                // Save the full object state
                UTinyTemp.SaveTemporary(obj);
            }
            else
            {
                if (UTinyModificationProcessor.IsChanged)
                {
                    // This is a persistent asset but the user has made some changes
                    // Save the full object state WITH the persistent Id
                    UTinyTemp.SavePersistentChanged(obj);
                }
                else
                {
                    // We only need to save the persistentId in this case
                    // We will reload any asset changes from disc without prompting the user
                    UTinyTemp.SavePersistentUnchanged(obj.PersistenceId);
                }
            }
        }

        /// <summary>
        /// Trys to loads the last saved temp file
        /// </summary>
        public static void LoadTemp()
        {
            Assert.IsFalse(EditorApplication.isPlayingOrWillChangePlaymode);

            if (!UTinyTemp.Exists())
            {
                return;
            }

            var context = new UTinyContext();
            var registry = context.Registry;

            string persistenceId;
            if (!UTinyTemp.Accept(registry, out persistenceId))
            {
                LoadPersistenceId(persistenceId);
                return;
            }

            var project = registry.FindAllByType<UTinyProject>().FirstOrDefault();
            UTinyEditorContext editorContext = null;

            if (project != null)
            {
                SetupProject(registry, project);
                
                editorContext = new UTinyEditorContext((UTinyProject.Reference) project, EditorContextType.Project, context, UTinyEditorPrefs.LoadLastWorkspace());
            }
            else
            {
                var module = registry.FindAllBySource(UTinyRegistry.DefaultSourceIdentifier).OfType<UTinyModule>().First();

                SetupModule(registry, module);
                
                if (null != module)
                {
                    project = registry.CreateProject(UTinyId.Generate(KWorkspaceProjectName), KWorkspaceProjectName);
                    project.Module = (UTinyModule.Reference) module;

                    editorContext = new UTinyEditorContext((UTinyProject.Reference) project, EditorContextType.Module, context, UTinyEditorPrefs.LoadLastWorkspace());
                }
            }

            Assert.IsNotNull(project);
            LoadContext(editorContext, true);
        }
        
        /// <summary>
        /// Sets up or migrates the initial state of the project
        /// * Includes required modules
        /// * Perfrorms any migration
        /// </summary>
        /// <param name="registry"></param>
        /// <param name="project"></param>
        private static void SetupProject(IRegistry registry, UTinyProject project)
        {
            var module = project.Module.Dereference(registry);
            // Make sure there's a dependency on the core modules
            module.AddExplicitModuleDependency((UTinyModule.Reference) registry.FindByName<UTinyModule>("UTiny.Core"));
            module.AddExplicitModuleDependency((UTinyModule.Reference) registry.FindByName<UTinyModule>("UTiny.Math"));
            module.AddExplicitModuleDependency((UTinyModule.Reference) registry.FindByName<UTinyModule>("UTiny.Core2D"));
            
            if (project.Configuration.Equals(UTinyEntity.Reference.None))
            {
                var configurationEntity = registry.CreateEntity(UTinyId.New(), "Configuration");
                project.Configuration = (UTinyEntity.Reference) configurationEntity;
            }
        }

        /// <summary>
        /// Sets up or migrates the initial state of a standalone module
        /// * Includes required modules
        /// * Perfrorms any migration
        /// </summary>
        /// <param name="registry"></param>
        /// <param name="module"></param>
        private static void SetupModule(IRegistry registry, UTinyModule module)
        {
            // Always include a dependency on core
            module.AddExplicitModuleDependency((UTinyModule.Reference) registry.FindByName<UTinyModule>("UTiny.Core"));
        }

        private static void LoadContext(UTinyEditorContext context, bool isChanged)
        {
            Assert.IsNotNull(context);

            UTinyModificationProcessor.ClearChanged();

            // @NOTE Loading a project can cause a Unity scene to change or be loaded during this operation we dont want to trigger a save 
            using (new UTinyModificationProcessor.DontSaveScope())
            {
                // Cleanup the previous context
                if (context != EditorContext)
                {
                    EditorContext?.Unload();
                }

                // Load the new context
                EditorContext = context;
                EditorContext.Load();

                // Setup the initial state
                s_WorkspaceVersion = EditorContext.Workspace.Version;

                OnLoadProject?.Invoke(EditorContext.Project);

                // Flush the Undo stack
                EditorContext.Undo.Update();

                // Listen for ANY changes and flag the project as changed (*)
                EditorContext.Caretaker.OnObjectChanged += (originator, memento) => { UTinyModificationProcessor.MarkChanged(); };
                EditorContext.Undo.OnUndoPerformed += UTinyModificationProcessor.MarkChanged;
                EditorContext.Undo.OnRedoPerformed += UTinyModificationProcessor.MarkChanged;
            }

            if (isChanged)
            {
                UTinyModificationProcessor.MarkChanged();
            }
        }
    }
}
#endif // NET_4_6
