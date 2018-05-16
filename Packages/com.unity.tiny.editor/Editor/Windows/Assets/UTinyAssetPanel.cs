#if NET_4_6
using System;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Unity.Tiny
{
    public class UTinyAssetPanel : UTinyPanel
    {
        #region Types

        [Serializable]
        public class State
        {
            public UTinyAssetTreeState TreeState;
            public Separator.State SeparatorState;
        }

        #endregion

        #region Fields

        private static readonly int s_ObjectPickerId = "UTinyAssetPanel.ObjectPicker".GetHashCode();

        private readonly IRegistry m_Registry;
        private UTinyProject.Reference m_Project;
        private UTinyModule.Reference m_MainModule;

        private readonly UTinyAssetTreeView m_TreeView;
        private readonly UTinyAssetPreviewControl m_AssetPreviewControl;
        
        private readonly Separator m_Separator;
        private readonly UTinyPanel m_LeftPanel;
        private readonly UTinyPanel m_RightPanel;

        private Vector2 m_ScrollPosition;

        #endregion
       
        #region Public Methods

        public UTinyAssetPanel(IRegistry registry, UTinyProject.Reference project, UTinyModule.Reference mainModule, State state)
        {
            m_Registry = registry;
            m_Project = project;
            m_MainModule = mainModule;
            
            UnityEngine.Assertions.Assert.IsNotNull(state);

            if (null == state.TreeState)
            {
                state.TreeState = new UTinyAssetTreeState();
            }
            // @TODO Find a way to move this to the base class
            state.TreeState.Init(UTinyAssetTreeView.CreateMultiColumnHeaderState());

            m_TreeView = new UTinyAssetTreeView(state.TreeState, new UTinyAssetTreeModel(m_Registry, m_MainModule));

            m_AssetPreviewControl = new UTinyAssetPreviewControl(m_Registry, m_Project, m_MainModule);
            
            m_LeftPanel = new UTinyPanel();
            m_LeftPanel.AddElement(CreateLeftToolbar());
            m_LeftPanel.AddElement(m_TreeView);
            
            m_RightPanel = new UTinyPanel();
            m_RightPanel.AddElement(CreateRightToolbar());
            m_RightPanel.AddElement(m_AssetPreviewControl);
            
            if (null == state.SeparatorState)
            {
                state.SeparatorState = new Separator.State();
            }
            
            m_Separator = new Separator(m_LeftPanel, m_RightPanel, state.SeparatorState)
            {
                MinLeft = 350,
                MinRight = 300
            };
            
            AddElement(m_Separator);
        }

        #endregion

        #region Drawing
        
        public override bool DrawLayout()
        {
            m_ScrollPosition = EditorGUILayout.BeginScrollView(m_ScrollPosition);
            try
            {
                if (Event.current.commandName == "ObjectSelectorUpdated" &&
                    EditorGUIUtility.GetObjectPickerControlID() == s_ObjectPickerId)
                {
                    var @object = EditorGUIUtility.GetObjectPickerObject();

                    if (null != @object)
                    {
                        m_MainModule.Dereference(m_Registry).AddAsset(@object);
                        m_TreeView.SetDirty();
                    }
                }

                var selections = m_TreeView.GetSelection();
                m_AssetPreviewControl.SetAssets(selections.Select(s => m_TreeView.Model.Find(s)).NotNull());

                return base.DrawLayout();
            }
            finally
            {
                EditorGUILayout.EndScrollView();
            }
        }
        
        #endregion

        #region Private Methods

        private UTinyToolbar CreateLeftToolbar()
        {
            var toolbar = new UTinyToolbar();
            
            toolbar.Add(new UTinyToolbar.Menu
            {
                Name = "Add",
                Items = new []
                {
                    new UTinyToolbar.Menu.Item
                    {
                        Path = "Add Explicit Reference",
                        Action = AddExplicitReference
                    },
                    new UTinyToolbar.Menu.Item
                    {
                        Path = "Remove Explicit Reference",
                        Action = RemoveExplicitReference
                    }
                }
            });
            
            toolbar.Add(new UTinyToolbar.Search
            {
                Alignment = UTinyToolbar.Alignment.Center,
                SearchString = m_TreeView.State.SearchString,
                Changed = searchString =>
                {
                    m_TreeView.State.SearchString = m_TreeView.searchString = searchString;
                }
            });

            return toolbar;
        }
        
        private UTinyToolbar CreateRightToolbar()
        {
            var toolbar = new UTinyToolbar();

            return toolbar;
        }

        #endregion
        
        #region Private Methods

        private void AddExplicitReference()
        {
            EditorGUIUtility.ShowObjectPicker<Object>(null, false, string.Empty, s_ObjectPickerId);
        }

        private void RemoveExplicitReference()
        {
            foreach (var selection in m_TreeView.GetSelection())
            {
                var @object = EditorUtility.InstanceIDToObject(selection);

                if (null == @object)
                {
                    continue;
                }
                
                m_MainModule.Dereference(m_Registry).RemoveAsset(@object);
            }
            
            m_TreeView.SetDirty();
        }
        
        #endregion
    }
}
#endif // NET_4_6
