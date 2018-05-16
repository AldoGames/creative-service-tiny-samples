#if NET_4_6
using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Unity.Tiny.Filters;

namespace Unity.Tiny
{
    public class UTinyCodePanel : UTinyPanel
    {
        #region Types

        [Serializable]
        public class State
        {
            public UTinyCodeTreeState TreeState;
            public Separator.State SeparatorState;
        }

        #endregion

        #region Fields

        private readonly IRegistry m_Registry;
        private UTinyModule.Reference m_MainModule;

        private readonly UTinyCodeTreeView m_TreeView;
        private readonly UTinySystemEditorControl m_SystemEditorControl;
        private readonly UTinyScriptEditorControl m_ScriptEditorControl;

        private readonly UTinyPanel m_RightPanel;

        #endregion

        #region Public Methods

        public UTinyCodePanel(IRegistry registry, UTinyModule.Reference mainModule, State state)
        {
            m_Registry = registry;
            m_MainModule = mainModule;

            // Init tree
            if (null == state.TreeState)
            {
                state.TreeState = new UTinyCodeTreeState();
            }

            state.TreeState.Init(UTinyCodeTreeView.CreateMultiColumnHeaderState());
            m_TreeView = new UTinyCodeTreeView(state.TreeState, new UTinyCodeTreeModel(registry, mainModule))
            {
                HasContextMenu = true,
            };

            // Init code editors
            m_SystemEditorControl = new UTinySystemEditorControl(registry, mainModule);
            m_ScriptEditorControl = new UTinyScriptEditorControl(registry);
            
            // Register events
            m_TreeView.OnContextMenuEvent += HandleContextMenuEvent;
            m_TreeView.OnRenameEnded += HandleRenameEnded;
            m_SystemEditorControl.OnRenameEnded += HandleRenameEnded;
            m_ScriptEditorControl.OnRenameEnded += HandleRenameEnded;

            var leftPanel = new UTinyPanel();
            leftPanel.AddElement(CreateLeftToolbar());
            leftPanel.AddElement(m_TreeView);

            m_RightPanel = new UTinyPanel();
            m_RightPanel.AddElement(CreateRightToolbar());
            m_RightPanel.AddElement(m_SystemEditorControl);

            if (null == state.SeparatorState)
            {
                state.SeparatorState = new Separator.State();
            }

            var separator = new Separator(leftPanel, m_RightPanel, state.SeparatorState)
            {
                MinLeft = 350,
                MinRight = 300
            };

            AddElement(separator);

            if (null != UTinySystemExecutionGraphWindow.Instance)
            {
                UTinySystemExecutionGraphWindow.Instance.Registry = m_Registry;
                UTinySystemExecutionGraphWindow.Instance.Module = m_MainModule;
            }
        }

        public override bool DrawLayout()
        {
            var selections = m_TreeView.GetSelected().ToList();

            if (selections.Count > 0)
            {
                var selection = selections.FirstOrDefault();
                m_SystemEditorControl.System = selection as UTinySystem.Reference? ?? UTinySystem.Reference.None;
                m_ScriptEditorControl.Script = selection as UTinyScript.Reference? ?? UTinyScript.Reference.None;
                
                m_RightPanel.SetElement(1, !m_SystemEditorControl.System.Equals(UTinySystem.Reference.None) ? (IDrawable) m_SystemEditorControl : m_ScriptEditorControl);
            }
            else
            {
                m_SystemEditorControl.System = UTinySystem.Reference.None;
                m_ScriptEditorControl.Script = UTinyScript.Reference.None;
            }

            return base.DrawLayout();
        }

        private UTinyToolbar CreateLeftToolbar()
        {
            var toolbar = new UTinyToolbar();

            toolbar.Add(new UTinyToolbar.Menu()
            {
                Alignment = UTinyToolbar.Alignment.Left,
                Name = "Create",
                Items = new[]
                {
                    new UTinyToolbar.Menu.Item
                    {
                        Path = "System",
                        Action = CreateSystem
                    },
                    new UTinyToolbar.Menu.Item
                    {
                        Path = "System (External)",
                        Action = CreateSystemExternal
                    },
                    new UTinyToolbar.Menu.Item
                    {
                        Path = "Code",
                        Action = CreateCodeBlock
                    }
                }
            });

            toolbar.Add(new UTinyToolbar.Search
            {
                Alignment = UTinyToolbar.Alignment.Center,
                SearchString = m_TreeView.SearchString,
                Changed = searchString => { m_TreeView.SearchString = searchString; }
            });

            toolbar.Add(new UTinyToolbar.Popup
            {
                Alignment = UTinyToolbar.Alignment.Right,
                Name = "Filter",
                Content = new UTinyToolbar.FilterPopup
                {
                    Items = new[]
                    {
                        new UTinyToolbar.FilterPopup.Item
                        {
                            Name = "Project Only",
                            State = m_TreeView.State.FilterProjectOnly,
                            Changed = state =>
                            {
                                m_TreeView.State.FilterProjectOnly = state;
                                m_TreeView.SetDirty();
                                m_TreeView.Repaint();
                            }
                        },
                        new UTinyToolbar.FilterPopup.Item
                        {
                            Name = "System",
                            State = m_TreeView.State.FilterSystems,
                            Changed = state =>
                            {
                                m_TreeView.State.FilterSystems = state;
                                m_TreeView.SetDirty();
                                m_TreeView.Repaint();
                            }
                        },
                        new UTinyToolbar.FilterPopup.Item
                        {
                            Name = "Code",
                            State = m_TreeView.State.FilterScripts,
                            Changed = state =>
                            {
                                m_TreeView.State.FilterScripts = state;
                                m_TreeView.SetDirty();
                                m_TreeView.Repaint();
                            }
                        }
                    }
                }
            });

            return toolbar;
        }

        private UTinyToolbar CreateRightToolbar()
        {
            var toolbar = new UTinyToolbar();

            toolbar.Add(new UTinyToolbar.Button
            {
                Alignment = UTinyToolbar.Alignment.Right,
                Icon = UTinyIcons.System,
                Action = () =>
                {
                    var window = EditorWindow.GetWindow<UTinySystemExecutionGraphWindow>("System Execution");
                    window.Registry = m_Registry;
                    window.Module = m_MainModule;
                }
            });

            return toolbar;
        }

        #endregion

        #region Private Methods

        private void CreateSystem()
        {
            var module = m_MainModule.Dereference(m_Registry);
            var system = m_Registry.CreateSystem(UTinyId.New(), UTinyUtility.GetUniqueName(module.EnumerateDependencies().SystemRefs(), "NewSystem"));
            module.AddSystemReference((UTinySystem.Reference) system);
            m_TreeView.Reload();
            m_TreeView.SetSelection(system.Id);
            system.TextAsset = CreateTextAsset(system.Name);
        }

        private void CreateSystemExternal()
        {
            var module = m_MainModule.Dereference(m_Registry);
            var system = m_Registry.CreateSystem(UTinyId.New(), UTinyUtility.GetUniqueName(module.EnumerateDependencies().SystemRefs(), "NewSystem"));
            system.IncludeIterator = false;
            system.External = true;
            module.AddSystemReference((UTinySystem.Reference) system);
            m_TreeView.Reload();
            m_TreeView.SetSelection(system.Id);
        }

        private void CreateCodeBlock()
        {
            var module = m_MainModule.Dereference(m_Registry);
            var script = m_Registry.CreateScript(UTinyId.New(), UTinyUtility.GetUniqueName(module.EnumerateDependencies().ScriptRefs(), "NewScript"));
            module.AddScriptReference((UTinyScript.Reference) script);
            m_TreeView.Reload();
            m_TreeView.SetSelection(script.Id);
            script.TextAsset = CreateTextAsset(script.Name);
        }

        private static TextAsset CreateTextAsset(string name)
        {
            var directory = GetWorkingDirectoy();
            var filePath = $"{directory.FullName}/{name + ".js.txt"}";
            File.WriteAllText(filePath, string.Empty);
            AssetDatabase.Refresh();
            var assetPath = FullPathToAssetPath(filePath);
            return AssetDatabase.LoadAssetAtPath<TextAsset>(assetPath);
        }

        private static DirectoryInfo GetWorkingDirectoy()
        {
            var persistenceId = UTinyEditorApplication.EditorContext.PersistentObject.PersistenceId;
            if (!string.IsNullOrEmpty(persistenceId))
            {
                var assetPath = AssetDatabase.GUIDToAssetPath(persistenceId);
                if (!string.IsNullOrEmpty(assetPath))
                {
                    var filePath = Application.dataPath + assetPath.Substring(assetPath.IndexOf('/'));
                    var fileInfo = new FileInfo(filePath);
                    return fileInfo.Directory;
                }
            }
            
            return new DirectoryInfo(Application.dataPath);
        }

        private static string FullPathToAssetPath(string path)
        {
            return Path.Combine("Assets", path.Substring(Application.dataPath.Length + 1, path.Length - (Application.dataPath.Length + 1)));
        }

        #endregion

        #region Event Handlers

        private void HandleContextMenuEvent(GenericMenu menu)
        {
            menu.AddItem(new GUIContent("Create/System"), false, CreateSystem);
            menu.AddItem(new GUIContent("Create/System (External)"), false, CreateSystemExternal);
            menu.AddItem(new GUIContent("Create/Code"), false, CreateCodeBlock);

            var selected = m_TreeView.GetSelected().NotNull().OfType<IIdentifiable<UTinyId>>().ToList();
            if (selected.Count == 0)
            {
                return;
            }

            if (selected.Count == 1)
            {
                menu.AddItem(new GUIContent("Rename"), false, () => m_TreeView.BeginRename(selected[0].Id));
            }
            else
            {
                menu.AddDisabledItem(new GUIContent("Rename"));
            }
        }

        private static void HandleRenameEnded(UTinyRegistryObjectBase obj)
        {
            var textAsset = (TextAsset) null;
            
            if (obj is UTinySystem)
            {
                textAsset = ((UTinySystem) obj)?.TextAsset;
            }
            else if (obj is UTinyScript)
            {
                textAsset = ((UTinyScript) obj)?.TextAsset;
            }
            
            var oldPath = AssetDatabase.GetAssetPath(textAsset);
            AssetDatabase.RenameAsset(oldPath, $"{obj.Name}.js.txt");
        }

        #endregion
    }
}
#endif // NET_4_6
