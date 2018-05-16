#if NET_4_6
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Unity.Tiny.Filters;
using UnityEditor;
using UnityEditor.SceneManagement;

using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Assertions;

namespace Unity.Tiny
{
    public class UTinyEntityGroupManager
    {
        #region Fields
        private readonly UTinyEditorContext m_Context;
        private UTinyEntityGroup.Reference m_ActiveEntityGroup = UTinyEntityGroup.Reference.None;
        private readonly List<UTinyEntityGroup.Reference> m_LoadedEntityGroups = new List<UTinyEntityGroup.Reference>();
        private readonly Dictionary<UTinyEntityGroup.Reference, EntityGroupGraph> m_EntityGroupToGraph = new Dictionary<UTinyEntityGroup.Reference, EntityGroupGraph>();

        #endregion

        #region Events
        public delegate void EntityGroupEventHandler(UTinyEntityGroup.Reference entityGroupRef);
        public delegate void OnEntityGroupsReorderedHandler(ReadOnlyCollection<UTinyEntityGroup.Reference> entityGroupRefs);

        public event EntityGroupEventHandler OnWillLoadEntityGroup;
        public event EntityGroupEventHandler OnEntityGroupLoaded;
        public event EntityGroupEventHandler OnWillUnloadEntityGroup;
        public event EntityGroupEventHandler OnEntityGroupUnloaded;
        public event OnEntityGroupsReorderedHandler OnEntityGroupsReordered;
        #endregion

        #region Properties
        public UTinyEntityGroup.Reference ActiveEntityGroup => m_ActiveEntityGroup;
        public ReadOnlyCollection<UTinyEntityGroup.Reference> LoadedEntityGroups => m_LoadedEntityGroups.AsReadOnly();
        public int LoadedEntityGroupCount => m_LoadedEntityGroups.Count;

        public Scene UnityScratchPad => HierarchyHelper.GetScratchPad(m_Context);

        private UTinyRegistry Registry => m_Context?.Registry;
        private UTinyUndo Undo => m_Context?.Undo;
        #endregion

        #region API
        public UTinyEntityGroupManager(UTinyEditorContext context)
        {
            Assert.IsNotNull(context);
            m_Context = context;
        }

        public void LoadEntityGroup(UTinyEntityGroup.Reference entityGroupRef, int index = -1)
        {
            LoadEntityGroup(entityGroupRef, index, true);
        }

        public void UnloadEntityGroup(UTinyEntityGroup.Reference entityGroupRef)
        {
            UnloadEntityGroup(entityGroupRef, true);
        }

        public void LoadSingleEntityGroup(UTinyEntityGroup.Reference entityGroupRef)
        {
            LoadEntityGroup(entityGroupRef);
            UnloadAllEntityGroupsExcept(entityGroupRef);
        }

        public void UnloadAllEntityGroupsExcept(UTinyEntityGroup.Reference entityGroupRef)
        {
            var entityGroupList = Pooling.ListPool<UTinyEntityGroup.Reference>.Get();
            try
            {
                foreach (var entityGroup in LoadedEntityGroups)
                {
                    if (!entityGroup.Equals(entityGroupRef))
                    {
                        entityGroupList.Add(entityGroup);
                    }
                }
                foreach(var entityGroup in entityGroupList)
                {
                    UnloadEntityGroup(entityGroup);
                }
            }
            finally
            {
                Pooling.ListPool<UTinyEntityGroup.Reference>.Release(entityGroupList);
            }
        }

        public void SetActiveEntityGroup(UTinyEntityGroup.Reference entityGroupRef)
        {
            SetActiveEntityGroup(entityGroupRef, true);
        }

        public void MoveUp(UTinyEntityGroup.Reference entityGroupRef)
        {
            var index = LoadedEntityGroups.IndexOf(entityGroupRef);
            if (index < 0 || index == 0)
            {
                return;
            }

            m_LoadedEntityGroups.Swap(index, index - 1);
            RebuildWorkspace();
            OnEntityGroupsReordered?.Invoke(LoadedEntityGroups);
        }

        public void MoveDown(UTinyEntityGroup.Reference entityGroupRef)
        {
            var index = LoadedEntityGroups.IndexOf(entityGroupRef);
            if (index < 0 || index == LoadedEntityGroupCount - 1)
            {
                return;
            }

            m_LoadedEntityGroups.Swap(index, index + 1);
            RebuildWorkspace();
            OnEntityGroupsReordered?.Invoke(LoadedEntityGroups);
        }

        public void CreateNewEntityGroup()
        {
            var entityGroupRef = (UTinyEntityGroup.Reference)Registry.CreateEntityGroup(UTinyId.New(), UTinyUtility.GetUniqueName(m_Context.Module.EntityGroups, "NewEntityGroup"));
            m_Context.Module.AddEntityGroupReference(entityGroupRef);
            LoadEntityGroup(entityGroupRef);
        }

        public void RecreateEntityGroupGraphs()
        {
            foreach (var entityGroupRef in LoadedEntityGroups)
            {
                var entityGroup = entityGroupRef.Dereference(Registry);
                if (null == entityGroup)
                {
                    continue;
                }
                m_EntityGroupToGraph[entityGroupRef] = EntityGroupGraph.CreateFromEntityGroup(entityGroup);
            }
        }

        public EntityGroupGraph GetActiveSceneGraph()
        {
            return GetSceneGraph(m_ActiveEntityGroup);
        }

        public EntityGroupGraph GetSceneGraph(UTinyEntityGroup.Reference entityGroupRef)
        {
            EntityGroupGraph graph = null;
            return m_EntityGroupToGraph.TryGetValue(entityGroupRef, out graph) ? graph : null;
        }

        public void ShowOpenEntityGroupMenu()
        {
            var menu = new GenericMenu();
            var mainModule = m_Context.Module;
            var any = false;
            foreach (var module in mainModule.EnumerateDependencies())
            {
                foreach (var entityGroupRef in module.EntityGroups)
                {
                    var entityGroup = entityGroupRef.Dereference(Registry);
                    if (null != entityGroup && !LoadedEntityGroups.Contains(entityGroupRef))
                    {
                        var entityGroupName = module.Name == "Main" ? entityGroup.Name : module.Name + "/" + entityGroup.Name;
                        menu.AddItem(new GUIContent(entityGroupName), false, () => LoadEntityGroup(entityGroupRef));
                        any = true;
                    }
                }
            }
            if (!any)
            {
                menu.AddDisabledItem(new GUIContent("All groups are loaded"));
            }
            menu.ShowAsContext();
        }

        public void Load()
        {
            HierarchyHelper.GetOrGenerateScratchPad(m_Context);
            var workspace = m_Context.Workspace;

            foreach(var entityGroupRef in workspace.OpenedEntityGroups)
            {
                LoadEntityGroup(entityGroupRef, -1, false);
            }
            SetActiveEntityGroup(workspace.ActiveEntityGroup, false);

            if (null == workspace.ActiveEntityGroup.Dereference(m_Context.Registry))
            {
                SetActiveEntityGroup(LoadedEntityGroups.FirstOrDefault(), false);
            }
            
            RebuildWorkspace();
            
            EditorSceneManager.sceneOpening += HandleSceneOpening;
            EditorSceneManager.newSceneCreated += HandleSceneCreated;

        }

        private static void HandleSceneCreated(Scene scene, NewSceneSetup setup, NewSceneMode mode)
        {
            UTinyEditorApplication.SaveChanges();
            UTinyEditorApplication.Close();
        }

        private static void HandleSceneOpening(string path, OpenSceneMode mode)
        {
            UTinyEditorApplication.SaveChanges();
            UTinyEditorApplication.Close();
        }

        public void Unload()
        {
            EditorSceneManager.sceneOpening -= HandleSceneOpening;
            EditorSceneManager.newSceneCreated -= HandleSceneCreated;
            HierarchyHelper.ReleaseScratchPad(m_Context);
        }
        #endregion

        #region Implementation
        /// <summary>
        /// Rebuilds the UTinyEditorWorkspace based on the current Hierarchy
        /// </summary>
        private void RebuildWorkspace()
        {
            var workspace = m_Context.Workspace;

            workspace.ClearOpenedEntityGroups();
            foreach (var entityGroup in LoadedEntityGroups)
            {
                workspace.AddOpenedEntityGroup(entityGroup);
            }
            workspace.ActiveEntityGroup = ActiveEntityGroup;
        }

        private void LoadEntityGroup(UTinyEntityGroup.Reference entityGroupRef, int index, bool rebuildWorkspace)
        {
            var entityGroup = entityGroupRef.Dereference(Registry);
            if (null == entityGroup)
            {
                Debug.Log($"{UTinyConstants.ApplicationName}: Could not load group named '{entityGroupRef.Name}' as the reference could not be resolved.");
                return;
            }

            if (m_LoadedEntityGroups.Contains(entityGroupRef))
            {
                Debug.Log($"{UTinyConstants.ApplicationName}: Cannot load the group named '{entityGroupRef.Name}'. It is already loaded");
                return;
            }

            if (HierarchyHelper.GetOrGenerateScratchPad(m_Context))
            {
                OnWillLoadEntityGroup?.Invoke(entityGroupRef);
                if (index >= 0 && index < m_LoadedEntityGroups.Count)
                {
                    m_LoadedEntityGroups.Insert(index, entityGroupRef);
                }
                else
                {
                    m_LoadedEntityGroups.Add(entityGroupRef);
                }
                m_EntityGroupToGraph[entityGroupRef] = EntityGroupGraph.CreateFromEntityGroup(entityGroup);
            }

            if (rebuildWorkspace)
            {
                RebuildWorkspace();
            }
            SetActiveEntityGroup(entityGroupRef, rebuildWorkspace);

            OnEntityGroupLoaded?.Invoke(entityGroupRef);
        }

        private void UnloadEntityGroup(UTinyEntityGroup.Reference entityGroupRef, bool rebuildWorkspace = true)
        {
            if (!m_LoadedEntityGroups.Contains(entityGroupRef))
            {
                Debug.Log($"{UTinyConstants.ApplicationName}: Cannot unload the group named '{entityGroupRef.Name}'. It is not loaded");
                return;
            }

            if (m_EntityGroupToGraph.ContainsKey(entityGroupRef))
            {
                OnWillUnloadEntityGroup?.Invoke(entityGroupRef);
                m_EntityGroupToGraph[entityGroupRef].Unlink();
                m_EntityGroupToGraph.Remove(entityGroupRef);
            }
            m_LoadedEntityGroups.Remove(entityGroupRef);

            var entityGroup = entityGroupRef.Dereference(Registry);
            if (null != entityGroup)
            {
                Undo.FlushChanges(entityGroup);
                Undo.FlushChanges(entityGroup.Entities.Deref(entityGroup.Registry));
            }

            if (m_LoadedEntityGroups.Count == 0)
            {
                SetActiveEntityGroup(UTinyEntityGroup.Reference.None, rebuildWorkspace);
            }
            else
            {
                if (entityGroupRef.Id == ActiveEntityGroup.Id)
                {
                    SetActiveEntityGroup(m_LoadedEntityGroups[0]);
                }
            }

            if (rebuildWorkspace)
            {
                RebuildWorkspace();
            }

            OnEntityGroupUnloaded?.Invoke(entityGroupRef);
        }

        public void SetActiveEntityGroup(UTinyEntityGroup.Reference entityGroupRef, bool rebuildWorkspace = true)
        {
            if (m_ActiveEntityGroup.Equals(entityGroupRef))
            {
                return;
            }
            m_ActiveEntityGroup = entityGroupRef;
            if (rebuildWorkspace)
            {
                RebuildWorkspace();
            }
        }
        #endregion
    }
}
#endif // NET_4_6
