#if NET_4_6
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;
using UnityEngine.Assertions;

namespace Unity.Tiny
{
    public class UTinyTypePanel : UTinyPanel
    {
        #region Types

        [Serializable]
        public class State
        {
            public UTinyTypeTreeState TreeState;
        }

        #endregion

        #region Fields

        private readonly IRegistry m_Registry;
        private UTinyModule.Reference m_MainModule;

        private readonly UTinyTypeTreeView m_TreeView;

        #endregion

        #region Public Methods

        public UTinyTypePanel(IRegistry registry, UTinyModule.Reference mainModule, State state)
        {
            Assert.IsNotNull(state);

            m_Registry = registry;
            m_MainModule = mainModule;

            if (null == state.TreeState)
            {
                state.TreeState = new UTinyTypeTreeState();
            }
            
            state.TreeState.Init(UTinyTypeTreeView.CreateMultiColumnHeaderState());

            m_TreeView = new UTinyTypeTreeView(state.TreeState, new UTinyTypeTreeModel(m_Registry, mainModule))
            {
                HasContextMenu = true
            };
            m_TreeView.OnContextMenuEvent += HandleContextMenuEvent;
            
            AddElement(CreateToolbar());
            AddElement(m_TreeView);
        }
        
        #endregion

        #region Private Methods
        
        private UTinyToolbar CreateToolbar()
        {
            var toolbar = new UTinyToolbar();
            
            toolbar.Add(new UTinyToolbar.Menu
            {
                Alignment = UTinyToolbar.Alignment.Left,
                Name = "Create",
                Items = new []
                {
                    new UTinyToolbar.Menu.Item
                    {
                        Path = "Component",
                        Action = CreateComponent
                    },
                    new UTinyToolbar.Menu.Item
                    {
                        Path = "Struct",
                        Action = CreateStruct
                    },
                    new UTinyToolbar.Menu.Item
                    {
                        Path = "Enum",
                        Action = CreateEnum
                    },
                    new UTinyToolbar.Menu.Item
                    {
                        Path = "Field",
                        Action = CreateField,
                        Validation = () => m_TreeView.GetSelected().Any(i => i is UTinyType.Reference)
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
                        },
                        new UTinyToolbar.FilterPopup.Item
                        {
                            Name = "Component",
                            State = m_TreeView.State.FilterComponents,
                            Changed = state => {
                                m_TreeView.State.FilterComponents = state;
                                m_TreeView.SetDirty();
                                m_TreeView.Repaint();
                            }
                        },
                        new UTinyToolbar.FilterPopup.Item
                        {
                            Name = "Struct",
                            State = m_TreeView.State.FilterStructs,
                            Changed = state => {
                                m_TreeView.State.FilterStructs = state;
                                m_TreeView.SetDirty();
                                m_TreeView.Repaint();
                            }
                        },
                        new UTinyToolbar.FilterPopup.Item
                        {
                            Name = "Enum",
                            State = m_TreeView.State.FilterEnums,
                            Changed = state => {
                                m_TreeView.State.FilterEnums = state;
                                m_TreeView.SetDirty();
                                m_TreeView.Repaint();
                            }
                        } 
                    }
                }
            });
            
            return toolbar;
        }
        
        private void CreateComponent()
        {
            var module = m_MainModule.Dereference(m_Registry);
            var type = m_Registry.CreateType(
                UTinyId.New(), 
                UTinyUtility.GetUniqueName(module.Components, "NewComponent"), 
                UTinyTypeCode.Component);

            module.AddComponentReference((UTinyType.Reference) type);
            
            m_TreeView.Reload();
            m_TreeView.SetSelection(new List<int> { m_TreeView.State.GetInstanceId(type.Id) }, TreeViewSelectionOptions.RevealAndFrame | 
                                                                                               TreeViewSelectionOptions.FireSelectionChanged);
        }
        
        private void CreateStruct()
        {
            var module = m_MainModule.Dereference(m_Registry);
            var type = m_Registry.CreateType(
                UTinyId.New(), 
                UTinyUtility.GetUniqueName(module.Structs, "NewStruct"), 
                UTinyTypeCode.Struct);

            module.AddStructReference((UTinyType.Reference) type);
            
            m_TreeView.Reload();
            m_TreeView.SetSelection(new List<int> { m_TreeView.State.GetInstanceId(type.Id) }, TreeViewSelectionOptions.RevealAndFrame | 
                                                                                               TreeViewSelectionOptions.FireSelectionChanged);
        }
        
        private void CreateEnum()
        {
            var module = m_MainModule.Dereference(m_Registry);
            var type = m_Registry.CreateType(
                UTinyId.New(), 
                UTinyUtility.GetUniqueName(module.Structs, "NewEnum"), 
                UTinyTypeCode.Enum);

            type.BaseType = (UTinyType.Reference) UTinyType.Int32;

            module.AddEnumReference((UTinyType.Reference) type);
            
            m_TreeView.Reload();
            m_TreeView.SetSelection(new List<int> { m_TreeView.State.GetInstanceId(type.Id) }, TreeViewSelectionOptions.RevealAndFrame | 
                                                                                               TreeViewSelectionOptions.FireSelectionChanged);
        }

        private void CreateField()
        {
            var fields = new List<UTinyField>();
            
            foreach (var selection in m_TreeView.GetSelected())
            {
                var reference = (UTinyType.Reference) selection;
                var type = reference.Dereference(m_Registry);

                if (null == type)
                {
                    continue;
                }
                
                if (type.IsEnum)
                {
                    var field = type.CreateField(UTinyUtility.GetUniqueName(type.Fields, "NewElement"), type.BaseType);
                    fields.Add(field);
                    
                    // @HACK
                    if (type.Fields.Count > 1)
                    {
                        var defaultEnum = type.DefaultValue as UTinyObject;
                        defaultEnum[field.Name] = (int) defaultEnum[type.Fields[type.Fields.Count - 2].Name] + 1;
                    }
                }
                else
                {
                    var field = type.CreateField(UTinyUtility.GetUniqueName(type.Fields, "NewField"), (UTinyType.Reference) UTinyType.Int32);
                    fields.Add(field);
                }

                m_TreeView.SetExpanded(m_TreeView.State.GetInstanceId(type.Id), true);
            }
                        
            m_TreeView.Reload();

            var selections = fields.Select(field => m_TreeView.State.GetInstanceId(field.Id)).ToList();
            m_TreeView.SetSelection(selections, TreeViewSelectionOptions.RevealAndFrame);
        }
        
        #endregion
        
        #region Event Handlers

        private void HandleContextMenuEvent(GenericMenu menu)
        {
            menu.AddItem(new GUIContent("Create/Component"), false, CreateComponent);
            menu.AddItem(new GUIContent("Create/Struct"), false, CreateStruct);
            menu.AddItem(new GUIContent("Create/Enum"), false, CreateEnum);
            
            var references = m_TreeView.GetSelected();
            if (references.Any(r=>r is UTinyType.Reference))
            {
                menu.AddItem(new GUIContent("Create/Field"), false, CreateField);
            }
            else
            {
                menu.AddDisabledItem(new GUIContent("Create/Field"));
            }

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
        
        #endregion
    }
}
#endif // NET_4_6
