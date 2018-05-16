#if NET_4_6
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Unity.Tiny
{
    /// <inheritdoc />
    /// <summary>
    /// Root window for UTinyProject
    /// </summary>
    public class UTinyProjectWindow : EditorWindow
    {
        public enum TabType
        {
            Settings,
            Modules,
            Scenes,
            Types,
            Code,
            Assets,
            BuildReport
        }

        private class GUIContents
        {
            public readonly GUIContent[] Tabs;

            public GUIContents(EditorContextType contextType)
            {
                var tabs = new List<GUIContent>
                {
                    new GUIContent("Settings"),
                    new GUIContent("Modules", "Declare your project dependencies."),
                    new GUIContent("Entities", "Define your game data."),
                    new GUIContent("Components", "Define your data types."),
                    new GUIContent("Systems", "Define your game logic"),
                    new GUIContent("Assets", "Preview and include assets."),
                };
                if (contextType == EditorContextType.Project)
                {
                    tabs.Add(new GUIContent("Build Report", "View project build report."));
                }
                Tabs = tabs.ToArray();
            }
        }

        #region Static Fields

        private static readonly List<UTinyProjectWindow> s_ActiveWindows = new List<UTinyProjectWindow>();
        private static GUIContents s_GuiContents;

        #endregion


        /// <summary>
        /// Selected tab
        /// </summary>
        private TabType m_TabType;

        private UTinyProjectSettingsPanel m_ProjectSettingsPanel;
        private UTinyModuleSettingsPanel m_ModuleSettingsPanel;
        private UTinyModulePanel m_ModulePanel;
        private UTinyEntityGroupPanel m_EntityGroupPanel;
        private UTinyTypePanel m_TypePanel;
        private UTinyCodePanel m_CodePanel;
        private UTinyAssetPanel m_AssetPanel;
        private UTinyBuildReportPanel m_BuildReportPanel;

        private int m_ModuleVersion;
        private int m_WorkspaceVersion;

        [SerializeField] private UTinyModulePanel.State m_ModulePanelState = new UTinyModulePanel.State();
        [SerializeField] private UTinyEntityGroupPanel.State m_ScenePanelState = new UTinyEntityGroupPanel.State();
        [SerializeField] private UTinyTypePanel.State m_TypePanelState = new UTinyTypePanel.State();
        [SerializeField] private UTinyCodePanel.State m_CodePanelState = new UTinyCodePanel.State();
        [SerializeField] private UTinyAssetPanel.State m_AssetPanelState = new UTinyAssetPanel.State();
        [SerializeField] private UTinyBuildReportPanel.State m_BuildReportState = new UTinyBuildReportPanel.State();

        [MenuItem(UTinyConstants.MenuItemNames.EditorWindow)]
        private static void Init()
        {
            OpenAndShow();
        }

        public static UTinyProjectWindow OpenAndShow()
        {
            var window = GetWindow<UTinyProjectWindow>($"{UTinyConstants.ApplicationName} Editor");
            window.Show();
            return window;
        }

        public static void RepaintAll()
        {
            foreach (var window in s_ActiveWindows)
            {
                window.SetDirtyAll();
                window.Repaint();
            }
        }

        public void SetTabType(TabType tabType)
        {
            m_TabType = tabType;
        }

        private void OnEnable()
        {
            s_ActiveWindows.Add(this);
            titleContent.text = $"{UTinyConstants.ApplicationName} Editor";
            UTinyEditorApplication.OnLoadProject += OnLoadProject;
            UTinyEditorApplication.OnSaveProject += OnSaveProject;
            UTinyEditorApplication.OnCloseProject += OnCloseProject;
            UTinyEditorApplication.OnChangesDetected += SetDirtyAll;
            
            if (!EditorApplication.isPlayingOrWillChangePlaymode)
            {
                EditorApplication.delayCall += () =>
                {
                    if (UTinyEditorApplication.ContextType == EditorContextType.None)
                    {
                        UTinyEditorApplication.LoadTemp();
                    }
                    else
                    {
                        OnLoadProject(UTinyEditorApplication.Project);
                    }
                };
            }

            EditorApplication.update += Update;
        }

        private void OnDisable()
        {
            UTinyEditorApplication.OnLoadProject -= OnLoadProject;
            UTinyEditorApplication.OnSaveProject -= OnSaveProject;
            UTinyEditorApplication.OnCloseProject -= OnCloseProject;
            UTinyEditorApplication.OnChangesDetected -= SetDirtyAll;
            EditorApplication.update -= Update;
            s_ActiveWindows.Remove(this);
        }

        private void Update()
        {
            if (m_ModuleVersion != UTinyEditorApplication.Module?.Version)
            {
                SetDirtyAll();
                m_ModuleVersion = UTinyEditorApplication.Module?.Version ?? -1;
            }
        }

        private void OnGUI()
        {
            try
            {
                if (s_GuiContents == null)
                {
                    s_GuiContents = new GUIContents(UTinyEditorApplication.ContextType);
                }

                GUI.enabled = !EditorApplication.isPlayingOrWillChangePlaymode;

                DoLayout();
                GUI.enabled = true;
            }
            catch (ExitGUIException)
            {
                throw;
            }
            catch (Exception e)
            {
                TinyEditorAnalytics.SendExceptionOnce("Editor.OnGUI", e);
                throw;
            }
        }

        private void DoLayout()
        {
            DoLayoutToolbar();

            if (UTinyEditorApplication.ContextType == EditorContextType.None)
            {
                // No project is open
                DoLayoutNoOpenProject();
            }
            else
            {
                DoLayoutTabs();

                IDrawable panel;

                switch (m_TabType)
                {
                    case TabType.Settings:
                    {
                        panel = UTinyEditorApplication.ContextType == EditorContextType.Project 
                            ? (IDrawable) m_ProjectSettingsPanel 
                            : m_ModuleSettingsPanel;
                    }
                        break;

                    case TabType.Modules:
                    {
                        panel = m_ModulePanel;
                    }
                        break;

                    case TabType.Scenes:
                    {
                        panel = m_EntityGroupPanel;
                    }
                        break;

                    case TabType.Types:
                    {
                        panel = m_TypePanel;
                    }
                        break;

                    case TabType.Code:
                    {
                        panel = m_CodePanel;
                    }
                        break;

                    case TabType.Assets:
                    {
                        panel = m_AssetPanel;
                    }
                        break;

                    case TabType.BuildReport:
                    {
                        panel = m_BuildReportPanel;
                    }
                        break;

                    default:
                        throw new ArgumentOutOfRangeException();
                }

                if (null == panel)
                {
                    return;
                }

                if (panel.DrawLayout())
                {
                    UTinyInspector.RepaintAll();
                    Repaint();
                }
            }
        }

        private static string GetProjectButtonName()
        {
            var project = UTinyEditorApplication.Project;
            
            if (null == project)
            {
                return "Project";
            }
            
            switch (UTinyEditorApplication.ContextType)
            {
                case EditorContextType.Project:
                    return project.Name;
                case EditorContextType.Module:
                    var module = project.Module.Dereference(UTinyEditorApplication.Registry);

                    if (null == module)
                    {
                        return project.Module.Name;
                    }

                    return module.Name;
            }

            return "Project";
        }

        private void DoLayoutNoOpenProject()
        {
            var newProject = false;
            var loadProject = false;
            var installSamples = false;
            
            EditorGUILayout.BeginVertical();
            {
                EditorGUILayout.BeginHorizontal();
                {
                    GUILayout.FlexibleSpace();
                    
                    GUILayout.Label($"No {UTinyConstants.ApplicationName} Project open");
                    
                    GUILayout.FlexibleSpace();
                }
                EditorGUILayout.EndHorizontal();
                
                GUILayout.Space(5);

                EditorGUILayout.BeginHorizontal();
                {
                    GUILayout.FlexibleSpace();
                    
                    if (GUILayout.Button($"New {UTinyConstants.ApplicationName} project"))
                    {
                        newProject = true;
                    }
                            
                    if (GUILayout.Button($"Open {UTinyConstants.ApplicationName} project"))
                    {
                        loadProject = true;
                    }
                    
                    GUILayout.FlexibleSpace();
                }
                EditorGUILayout.EndHorizontal();

                GUILayout.Space(5);
                
                EditorGUILayout.BeginHorizontal();
                {
                    GUILayout.FlexibleSpace();

                    if (GUILayout.Button("Import sample projects", UTinyStyles.LinkLabelStyle))
                    {
                        installSamples = true;
                    }
                    
                    GUILayout.FlexibleSpace();
                }
                EditorGUILayout.EndHorizontal();

                GUILayout.FlexibleSpace();
            }
            EditorGUILayout.EndVertical();
            
            if (newProject)
            {
                NewProject();
            }
            
            if (loadProject)
            {
                LoadProject();
            }

            if (installSamples)
            {
                UTinyBuildPipeline.InstallSamples(true);
            }
        }

        private void DoLayoutToolbar()
        {
            GUILayout.Space(1);

            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                using (var projectScope = new EditorGUILayout.HorizontalScope(GUILayout.Width(120)))
                {
                    var buttonLabel = GetProjectButtonName();

                    if (UTinyEditorApplication.IsChanged)
                    {
                        buttonLabel += " *";
                    }
                    
                    if (GUILayout.Button(buttonLabel, EditorStyles.toolbarDropDown))
                    {
                        var menu = new GenericMenu();

                        menu.AddItem(new GUIContent("New Project"), false, NewProject);

                        menu.AddItem(new GUIContent("New Module"), false, NewModule);

                        menu.AddSeparator(string.Empty);
                        menu.AddItem(new GUIContent("Load..."), false, LoadProject);
                        
                        menu.AddSeparator(string.Empty);

                        var save = new GUIContent("Save");
                        if (UTinyEditorApplication.Project == null)
                        {
                            menu.AddDisabledItem(save);
                        }
                        else
                        {
                            menu.AddItem(save, false, ()=>
                            {
                                UTinyEditorApplication.Save();
                            });
                        }
                        
                        var saveAs = new GUIContent("Save As...");
                        if (UTinyEditorApplication.Project == null)
                        {
                            menu.AddDisabledItem(saveAs);
                        }
                        else
                        {
                            menu.AddItem(saveAs, false, ()=>
                            {
                                UTinyEditorApplication.SaveAs();
                            });
                        }
                        
                        menu.AddSeparator(string.Empty);

                        var closeProject = new GUIContent("Close");
                        if (UTinyEditorApplication.Project == null)
                        {
                            menu.AddDisabledItem(closeProject);
                        }
                        else
                        {
                            menu.AddItem(closeProject, false, () =>
                            {
                                if (!UTinyEditorApplication.SaveChanges())
                                {
                                    return;
                                }
                                
                                m_TabType = TabType.Settings;
                                UTinyEditorApplication.Close();
                            });
                        }

                        menu.DropDown(projectScope.rect);
                    }
                }

                GUILayout.FlexibleSpace();

                if (UTinyEditorApplication.ContextType == EditorContextType.Project)
                {
                    var project = UTinyEditorApplication.Project;
                    var workspace = UTinyEditorApplication.EditorContext.Workspace;

                    if (null != project && null != workspace)
                    {
                        var lastBuildConfiguration = workspace.BuildConfiguration;
                        workspace.BuildConfiguration = (UTinyBuildConfiguration) EditorGUILayout.EnumPopup(workspace.BuildConfiguration, EditorStyles.toolbarDropDown, GUILayout.Width(100));
                        if (workspace.BuildConfiguration != lastBuildConfiguration)
                        {
                            UTinyEditorUtility.RepaintAllWindows();
                        }

                        if (GUILayout.Button("Export", EditorStyles.toolbarButton, GUILayout.Width(100)))
                        {
                            UTinyBuildPipeline.Export(project);
                        }

                        workspace.Preview = GUILayout.Toggle(
                            workspace.Preview,
                            new GUIContent(UTinyIcons.Export, "Toggles preview in browser."),
                            EditorStyles.toolbarButton,
                            GUILayout.Width(35));
                    }
                }
            }
        }

        private void DoLayoutTabs()
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.FlexibleSpace();

                var newTabType = (TabType) GUILayout.Toolbar((int) m_TabType, s_GuiContents.Tabs, EditorStyles.miniButton
#if UNITY_2017_2_OR_NEWER
                    , GUI.ToolbarButtonSize.FitToContents
#endif
                    , GUILayout.Height(20));

                if (newTabType != m_TabType)
                {
                    m_TabType = newTabType;

                    switch (m_TabType)
                    {
                        case TabType.Settings:
                            m_ModuleSettingsPanel?.SetDirty();
                            m_ProjectSettingsPanel?.SetDirty();
                            break;
                        case TabType.Modules:
                            m_ModulePanel?.SetDirty();
                            break;
                        case TabType.Scenes:
                            m_EntityGroupPanel?.SetDirty();
                            break;
                        case TabType.Types:
                            m_TypePanel?.SetDirty();
                            break;
                        case TabType.Code:
                            m_CodePanel?.SetDirty();
                            break;
                        case TabType.Assets:
                            m_AssetPanel?.SetDirty();
                            break;
                        case TabType.BuildReport:
                            m_BuildReportPanel?.SetDirty();
                            break;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                }

                GUILayout.FlexibleSpace();
            }
        }

        private void OnLoadProject(UTinyProject project)
        {
            var sceneView = SceneView.sceneViews.Count > 0 ? SceneView.sceneViews[0] as SceneView : null;

            // crush the scene view
            if (null != sceneView)
            {
                sceneView.in2DMode = true;
                sceneView.m_SceneLighting = false;
#if UNITY_2017_2_OR_NEWER
                sceneView.sceneViewState.showFog = false;
                sceneView.sceneViewState.showMaterialUpdate = false;
                sceneView.sceneViewState.showSkybox = false;
                sceneView.sceneViewState.showFlares = false;
                sceneView.sceneViewState.showImageEffects = false;
#endif
            }

            s_GuiContents = new GUIContents(UTinyEditorApplication.ContextType);
            m_ModuleSettingsPanel = new UTinyModuleSettingsPanel(project.Registry, project.Module);
            m_ProjectSettingsPanel = new UTinyProjectSettingsPanel(project.Registry, (UTinyProject.Reference) project);
            m_ModulePanel = new UTinyModulePanel(project.Registry, (UTinyProject.Reference) project, project.Module, m_ModulePanelState);
            m_EntityGroupPanel = new UTinyEntityGroupPanel(project.Registry, project.Module, m_ScenePanelState);
            m_TypePanel = new UTinyTypePanel(project.Registry, project.Module, m_TypePanelState);
            m_CodePanel = new UTinyCodePanel(project.Registry, project.Module, m_CodePanelState);
            m_AssetPanel = new UTinyAssetPanel(project.Registry, (UTinyProject.Reference) project, project.Module, m_AssetPanelState);
            m_BuildReportPanel = new UTinyBuildReportPanel(project.Registry, project.Module, m_BuildReportState);
        }

        private void OnSaveProject(UTinyProject project)
        {
            SetDirtyAll();
        }

        private void OnCloseProject(UTinyProject project)
        {
            m_ModuleSettingsPanel = null;
            m_ProjectSettingsPanel = null;
            m_ModulePanel = null;
            m_EntityGroupPanel = null;
            m_TypePanel = null;
            m_CodePanel = null;
            m_AssetPanel = null;
            m_BuildReportPanel = null;
        }

        private void SetDirtyAll()
        {
            m_ModuleSettingsPanel?.SetDirty();
            m_ProjectSettingsPanel?.SetDirty();
            m_ModulePanel?.SetDirty();
            m_EntityGroupPanel?.SetDirty();
            m_TypePanel?.SetDirty();
            m_CodePanel?.SetDirty();
            m_AssetPanel?.SetDirty();
            m_BuildReportPanel?.SetDirty();
            Repaint();
        }

        private void NewProject()
        {
            if (!UTinyEditorApplication.SaveChanges())
            {
                return;
            }
                            
            m_TabType = TabType.Settings;
            UTinyEditorApplication.NewProject();
        }
        
        private void NewModule()
        {
            if (!UTinyEditorApplication.SaveChanges())
            {
                return;
            }
                        
            m_TabType = TabType.Settings;
            UTinyEditorApplication.NewModule();
        }

        private void LoadProject()
        {
            var path = EditorUtility.OpenFilePanel("Load File", Application.dataPath, $"{UTinyPersistence.ProjectFileImporterExtension},{UTinyPersistence.ModuleFileImporterExtension}");

            if (!string.IsNullOrEmpty(path))
            {
                if (!UTinyEditorApplication.SaveChanges())
                {
                    return;
                }
                                
                // Convert to relative path
                if (path.StartsWith(Application.dataPath)) {
                    path =  "Assets" + path.Substring(Application.dataPath.Length);
                }
                                
                m_TabType = TabType.Settings;
                UTinyEditorApplication.Close();

                var extension = Path.GetExtension(path);
                switch (extension)
                {
                    case UTinyPersistence.ProjectFileExtension:
                        UTinyEditorApplication.LoadProject(path);
                        break;
                    case UTinyPersistence.ModuleFileExtension:
                        UTinyEditorApplication.LoadModule(path);
                        break;
                }
            }
        }
    }
}
#endif // NET_4_6
