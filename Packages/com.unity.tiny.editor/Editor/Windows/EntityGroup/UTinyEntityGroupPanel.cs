#if NET_4_6
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

namespace Unity.Tiny
{
    public class UTinyEntityGroupPanel : UTinyPanel
    {
        #region
        [Serializable]
        public class State
        {
            public UTinyEntityGroupTreeState TreeState;
        }

        private readonly IRegistry m_Registry;
        private readonly UTinyEntityGroupManager m_EntityGroupManager;
        private UTinyModule.Reference m_MainModule;
        private readonly UTinyEntityGroupTreeView m_TreeView;

        public UTinyEntityGroupPanel(IRegistry registry, UTinyModule.Reference mainModule, State state)
        {
            m_Registry = registry;
            m_MainModule = mainModule;
            m_EntityGroupManager = UTinyEditorApplication.EntityGroupManager;

            if (null == state.TreeState)
            {
                state.TreeState = new UTinyEntityGroupTreeState();
            }

            state.TreeState.Init(UTinyEntityGroupTreeView.CreateMultiColumnHeaderState());

            m_TreeView = new UTinyEntityGroupTreeView(state.TreeState, new UTinyEntityGroupTreeModel(m_Registry, m_MainModule))
            {
                HasContextMenu = true
            };
            m_TreeView.OnContextMenuEvent += HandleContextMenuEvent;
            
            AddElement(CreateToolbar());
            AddElement(m_TreeView);
        }

        private UTinyToolbar CreateToolbar()
        {
            var toolbar = new UTinyToolbar();
            
            toolbar.Add(new UTinyToolbar.Menu()
            {
                Alignment = UTinyToolbar.Alignment.Left,
                Name = "Create",
                Items = new []
                {
                    new UTinyToolbar.Menu.Item
                    {
                        Path = "EntityGroup",
                        Action = CreateEntityGroup
                    }
                }
            });
            
            toolbar.Add(new UTinyToolbar.Search
            {
                Alignment = UTinyToolbar.Alignment.Center,
                SearchString = m_TreeView.SearchString,
                Changed = searchString =>
                {
                    m_TreeView.SearchString = searchString;
                }
            });
            
            toolbar.Add(new UTinyToolbar.Popup
            {
                Alignment = UTinyToolbar.Alignment.Right,
                Name = "Filter",
                Content = new UTinyToolbar.FilterPopup
                {
                    Items = new []
                    {
                        new UTinyToolbar.FilterPopup.Item
                        {
                            Name = "Project Only",
                            State = m_TreeView.State.FilterProjectOnly,
                            Changed = state => {
                                m_TreeView.State.FilterProjectOnly = state;
                                m_TreeView.SetDirty();
                                m_TreeView.Repaint();
                            }
                        }
                    }
                }
            });

            return toolbar;
        }
        
        #endregion
        
        #region Private Methods
        
        private void CreateEntityGroup()
        {
            var module = m_MainModule.Dereference(m_Registry);
            var entityGroup = m_Registry.CreateEntityGroup(UTinyId.New(), UTinyUtility.GetUniqueName(module.EntityGroups, "NewEntityGroup"));
            module.AddEntityGroupReference((UTinyEntityGroup.Reference) entityGroup);
            m_TreeView.Reload();
            m_TreeView.SetSelection(new List<int>{m_TreeView.State.GetInstanceId(entityGroup.Id)}, TreeViewSelectionOptions.RevealAndFrame);
        }

        #endregion

        #region Event Handlers

        private void HandleContextMenuEvent(GenericMenu menu)
        {
            var module = m_MainModule.Dereference(m_Registry);
            if (null == module)
            {
                return;
            }

            menu.AddItem(new GUIContent("Create/EntityGroup"), false, CreateEntityGroup);

            var selected = m_TreeView.GetSelected().NotNull().OfType<UTinyEntityGroup.Reference>().ToList();

            if (selected.Count == 0)
            {
                return;
            }

            menu.AddItem(new GUIContent("Load/Single"), false, () =>
            {
                m_EntityGroupManager.LoadSingleEntityGroup(selected.First());
                foreach (var entityGroupRef in selected.Skip(1))
                {
                    m_EntityGroupManager.LoadEntityGroup(entityGroupRef);
                }
            });

            menu.AddItem(new GUIContent("Load/Additive"), false, () =>
            {
                foreach (var entityGroupRef in selected)
                {
                    m_EntityGroupManager.LoadEntityGroup(entityGroupRef);
                }
            });

            if (selected.Count == 1 && !selected[0].Equals(module.StartupEntityGroup))
            {

                menu.AddItem(new GUIContent("Set as startup"), false, () =>
                {
                    var reference = selected.FirstOrDefault();
                    m_MainModule.Dereference(m_Registry).StartupEntityGroup = reference;
                });
            }
            else
            {
                menu.AddDisabledItem(new GUIContent("Set as startup"));
            }
            menu.AddSeparator("");

            if (selected.Count == 1)
            {
                menu.AddItem(new GUIContent("Rename"), false, () => m_TreeView.BeginRename(selected[0].Id));
            }
            else
            {
                menu.AddDisabledItem(new GUIContent("Rename"));
            }
        }
        
        #endregion
    }
}
#endif // NET_4_6
