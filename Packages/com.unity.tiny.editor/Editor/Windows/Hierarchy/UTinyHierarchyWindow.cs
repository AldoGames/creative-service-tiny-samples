#if NET_4_6
using System;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEditor;
using UnityEditor.IMGUI.Controls;

namespace Unity.Tiny
{
    public class UTinyHierarchyWindow : EditorWindow
    {
        #region Static Fields
        private static readonly List<UTinyHierarchyWindow> s_ActiveWindows = new List<UTinyHierarchyWindow>();
        #endregion

        #region Properties
        private static HierarchyTree AnyTree
        {
            get
            {
                if (s_ActiveWindows.Count > 0)
                {
                    return s_ActiveWindows[0].m_TreeView;
                }
                return null;
            }
        }

        private static UTinyEditorContext EditorContext => UTinyEditorApplication.EditorContext;
        private static UTinyEntityGroupManager EntityGroupManager => EditorContext?.EntityGroupManager;
        private static ReadOnlyCollection<UTinyEntityGroup.Reference> LoadedEntityGroups => EntityGroupManager?.LoadedEntityGroups ?? new List<UTinyEntityGroup.Reference>().AsReadOnly();
        private static UTinyEntityGroup.Reference ActiveScene => EntityGroupManager?.ActiveEntityGroup ?? UTinyEntityGroup.Reference.None;
        private static int LoadedEntityGroupCount => EntityGroupManager?.LoadedEntityGroupCount ?? 0;
        private static UTinyUndo Undo => EditorContext?.Undo;

        #endregion

        #region Fields
        [SerializeField]
        private TreeViewState m_TreeState = new TreeViewState();

        private HierarchyTree m_TreeView;
        private Vector2 m_ScrollPosition;
        private SearchField m_Filter;
        #endregion

        #region Menu Items
        [MenuItem(UTinyConstants.MenuItemNames.CreateEntity, priority = 0)]
        public static void CreateEntity()
        {
            AnyTree.CreateEntity(ActiveScene);
        }

        [MenuItem(UTinyConstants.MenuItemNames.CreateStaticEntity, priority = 1)]
        public static void CreateStaticEntity()
        {
            AnyTree.CreateStaticEntity(ActiveScene);
        }

        [MenuItem(UTinyConstants.MenuItemNames.CreateEntity, validate = true)]
        [MenuItem(UTinyConstants.MenuItemNames.CreateStaticEntity, validate = true)]
        public static bool ValidateCreateEntity()
        {
            return s_ActiveWindows.Count > 0 && LoadedEntityGroupCount > 0;
        }

        [MenuItem(UTinyConstants.MenuItemNames.DuplicateSelection, priority = 100)]
        public static void DuplicateSelection()
        {
            AnyTree.DuplicateSelection();
        }

        [MenuItem(UTinyConstants.MenuItemNames.DuplicateSelection, validate = true)]
        public static bool ValidateDuplicateSelection()
        {
            return s_ActiveWindows.Count > 0 && AnyTree?.GetEntitySelection().Count > 0;
        }

        [MenuItem(UTinyConstants.MenuItemNames.DeleteSelection, priority = 200)]
        public static void DeleteSelection()
        {
            AnyTree.DeleteSelection();
        }

        [MenuItem(UTinyConstants.MenuItemNames.DeleteSelection, validate = true)]
        public static bool ValidateDeleteSelection()
        {
            return s_ActiveWindows.Count > 0 && AnyTree?.GetEntitySelection().Count > 0;
        }
        #endregion

        #region Static API
        [MenuItem(UTinyConstants.MenuItemNames.HierarchyWindow)]
        public static UTinyHierarchyWindow OpenNewHierarchyWindow()
        {
            var window = CreateInstance<UTinyHierarchyWindow>();
            window.titleContent.text = $"{UTinyConstants.ApplicationName} Hierarchy";
            window.Show();
            return window;
        }

        public static EntityGroupGraph GetSceneGraph(UTinyEntityGroup.Reference entityGroupRef)
        {
            return EntityGroupManager?.GetSceneGraph(entityGroupRef);
        }

        public static void InvalidateDataModel()
        {
            EntityGroupManager?.RecreateEntityGroupGraphs();

            foreach (var window in s_ActiveWindows)
            {
                window.m_TreeView.Invalidate();
                window.Repaint();
            }
            UTinyInspector.RepaintAll();
        }

        public static void InvalidateSceneGraph()
        {
            foreach (var entityGroup in LoadedEntityGroups)
            {
                var graph = EntityGroupManager.GetSceneGraph(entityGroup);
                graph.CommitChanges();
            }

            foreach (var window in s_ActiveWindows)
            {
                window.m_TreeView.Invalidate();
                window.Repaint();
            }
            UTinyInspector.RepaintAll();
        }

        public static void RepaintAll()
        {
            foreach (var window in s_ActiveWindows)
            {
                window.Repaint();
            }
            UTinyInspector.RepaintAll();
        }
        #endregion

        #region Unity
        private void OnEnable()
        {
            s_ActiveWindows.Add(this);
            titleContent.text = $"{UTinyConstants.ApplicationName} Hierarchy";
            UTinyEditorApplication.OnLoadProject += HandleProjectLoaded;
            UTinyEditorApplication.OnCloseProject += HandleProjectClosed;
            HandleProjectLoaded(UTinyEditorApplication.Project);

            if (null == m_TreeView)
            {
                m_TreeView = new HierarchyTree(EditorContext, m_TreeState);
            }

            m_Filter = new SearchField();
        }

        private void OnDisable()
        {
            if (null != m_TreeView)
            {
                foreach (var entityGroup in LoadedEntityGroups)
                {
                    m_TreeView.RemoveEntityGroup(entityGroup);
                }
            }
            s_ActiveWindows.Remove(this);

            UTinyEditorApplication.OnLoadProject -= HandleProjectLoaded;
            UTinyEditorApplication.OnCloseProject -= HandleProjectClosed;
        }

        private void OnGUI()
        {
            if (null == EditorContext)
            {
                EditorGUILayout.LabelField("No project loaded.");
                return;
            }

            try
            {
                VerifyEntityGroups();

                using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar, GUILayout.ExpandWidth(true)))
                {
                    if (GUILayout.Button("Create", EditorStyles.toolbarDropDown, GUILayout.Width(75)))
                    {
                        var menu = new GenericMenu();
                        menu.AddItem(new GUIContent("Create EntityGroup"), false, () =>
                        {
                            EntityGroupManager.CreateNewEntityGroup();
                        });

                        menu.AddSeparator("");

                        if (LoadedEntityGroups.Count > 0)
                        {
                            // Entity
                            menu.AddItem(new GUIContent("Create Entity"), false, () =>
                            {
                                m_TreeView.CreateEntity(ActiveScene);
                                InvalidateSceneGraph();
                            });
                            menu.AddItem(new GUIContent("Create Static Entity"), false, () =>
                            {
                                m_TreeView.CreateStaticEntity(ActiveScene);
                                InvalidateSceneGraph();
                            });
                        }

                        menu.ShowAsContext();
                    }

                    if (GUILayout.Button("Load", EditorStyles.toolbarDropDown, GUILayout.Width(75)))
                    {
                        EntityGroupManager.ShowOpenEntityGroupMenu();
                    }

                    // HACK For some reason flexible space does not work here...
                    GUILayout.Space(position.width - 50);
                }

                var searchRect = EditorGUILayout.GetControlRect(false, EditorGUIUtility.singleLineHeight);
                m_TreeView.FilterString = m_Filter.OnGUI(searchRect, m_TreeView.FilterString);

                if (LoadedEntityGroupCount == 0)
                {
                    EditorGUILayout.LabelField("No EntityGroups are loaded.");
                    return;
                }

                m_ScrollPosition = EditorGUILayout.BeginScrollView(m_ScrollPosition);
                {
                    var rect = EditorGUILayout.GetControlRect(false, position.height - 42.0f);
                    rect.width = Screen.width + 1;
                    rect.x = 0;
                    m_TreeView.OnGUI(rect);

                    // Check for a click on empty space.
                    rect.height = rect.height - m_TreeView.totalHeight;
                    rect.y += m_TreeView.totalHeight;
                    if (rect.height > 0 && Event.current.type == EventType.MouseDown &&
                        rect.Contains(Event.current.mousePosition))
                    {
                        Selection.instanceIDs = new int[0];
                    }
                }
                EditorGUILayout.EndScrollView();

                ExecuteCommands();
            }
            catch (ExitGUIException)
            {
                throw;
            }
            catch (Exception e)
            {
                TinyEditorAnalytics.SendExceptionOnce("Hierarchy.OnGUI", e);
                throw;
            }
        }

        void OnSelectionChange()
        {
            m_TreeView.SetSelection(Selection.instanceIDs);
            Repaint();
        }
        #endregion

        #region Implementation
        private static void AddToTrees(UTinyEntityGroup.Reference entityGroupRef)
        {
            foreach (var window in s_ActiveWindows)
            {
                window.m_TreeView.AddEntityGroup(entityGroupRef);
                window.Repaint();
            }
            InvalidateSceneGraph();
        }

        private static void RemoveFromTrees(UTinyEntityGroup.Reference entityGroupRef)
        {
            foreach (var window in s_ActiveWindows)
            {
                window.m_TreeView.RemoveEntityGroup(entityGroupRef);
                window.Repaint();
            }
            InvalidateSceneGraph();
        }

        private static void ReorderTrees(ReadOnlyCollection<UTinyEntityGroup.Reference> entityGroupRefs)
        {
            foreach (var window in s_ActiveWindows)
            {
                foreach (var entityGroupRef in entityGroupRefs)
                {
                    window.m_TreeView.RemoveEntityGroup(entityGroupRef);
                    window.m_TreeView.AddEntityGroup(entityGroupRef);
                    window.Repaint();
                }
            }
            InvalidateSceneGraph();
        }

        private static void VerifyEntityGroups()
        {
            var entityGroupRefs = Pooling.ListPool<UTinyEntityGroup.Reference>.Get();
            foreach (var entityGroup in LoadedEntityGroups)
            {
                if (null == entityGroup.Dereference(EditorContext.Registry))
                {
                    entityGroupRefs.Add(entityGroup);
                }
            }

            foreach (var entityGroup in entityGroupRefs)
            {
                EntityGroupManager?.UnloadEntityGroup(entityGroup);
            }

            Pooling.ListPool<UTinyEntityGroup.Reference>.Release(entityGroupRefs);
        }

        private static void ExecuteCommand(Action action, Event evt)
        {
            Assert.IsNotNull(action);
            var execute = evt.type == EventType.ExecuteCommand;
            if (execute)
            {
                action();
            }
            evt.Use();
            InvalidateSceneGraph();
            GUIUtility.ExitGUI();
        }

        private void ExecuteCommands()
        {
            Event evt = Event.current;

            if (evt.type != EventType.ExecuteCommand && evt.type != EventType.ValidateCommand)
                return;

            switch (evt.commandName)
            {
                case EventCommandNames.SoftDelete:
                case EventCommandNames.Delete:
                    ExecuteCommand(DeleteSelectionImpl, evt);
                    break;
                case EventCommandNames.Duplicate:
                    ExecuteCommand(DuplicateSelectionImpl, evt);
                    break;
                case EventCommandNames.Copy:
                    ExecuteCommand(CopySelection, evt);
                    break;
                case EventCommandNames.Paste:
                    ExecuteCommand(PasteSelection, evt);
                    break;
                default:
                    break;
            }
        }


        private void HandleProjectLoaded(UTinyProject project)
        {
            if (null == project)
            {
                Repaint();
                return;
            }

            EntityGroupManager.OnEntityGroupLoaded += AddToTrees;
            EntityGroupManager.OnEntityGroupUnloaded += RemoveFromTrees;
            EntityGroupManager.OnEntityGroupsReordered += ReorderTrees;
            m_TreeView = new HierarchyTree(EditorContext, m_TreeState);

            foreach (var entityGroup in LoadedEntityGroups)
            {
                AddToTrees(entityGroup);
            }

            Undo.OnUndoPerformed += InvalidateDataModel;
            Undo.OnRedoPerformed += InvalidateDataModel;

            Repaint();
        }

        private void HandleProjectClosed(UTinyProject project)
        {
            m_TreeView.ClearScenes();
            Repaint();
        }

        private void DeleteSelectionImpl()
        {
            m_TreeView.DeleteSelection();
            InvalidateSceneGraph();
        }

        private void DuplicateSelectionImpl()
        {
            m_TreeView.DuplicateSelection();
            InvalidateSceneGraph();
        }

        private void CopySelection()
        {
            // [MP] @TODO: Implement this
            //AnyTree.CopySelection();
        }

        private void PasteSelection()
        {
            // [MP] @TODO: Implement this
            //AnyTree.PasteSelection();
        }
        #endregion

        /// <summary>
        /// Subset of the Unity's own command names.
        /// </summary>
        internal static class EventCommandNames
        {
            //Some of these strings are also hard-coded on the native side. Change them at your own risk!
            public const string Cut = "Cut";
            public const string Copy = "Copy";
            public const string Paste = "Paste";
            public const string SelectAll = "SelectAll";
            public const string Duplicate = "Duplicate";
            public const string Delete = "Delete";
            public const string SoftDelete = "SoftDelete";
            public const string Find = "Find";
        }
    }
}
#endif // NET_4_6
