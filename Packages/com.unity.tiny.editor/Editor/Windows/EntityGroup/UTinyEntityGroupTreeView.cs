#if NET_4_6
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

namespace Unity.Tiny
{
    public class UTinyEntityGroupTreeView : UTinyTreeView<UTinyEntityGroupTreeState, UTinyEntityGroupTreeModel>
    {
        #region Types
        
        /// <summary>
        /// Columns for this tree
        /// </summary>
        public enum ColumnType
        {
            Icon,
            Name,
            Startup,
            Module
        }
        
        #endregion
        
        #region Public Methods

        public UTinyEntityGroupTreeView(UTinyEntityGroupTreeState state, UTinyEntityGroupTreeModel model) : base(state, model)
        {
            extraSpaceBeforeIconAndLabel = 18f;
            columnIndexForTreeFoldouts = 1;
        }
        
        #endregion
        
        #region Data Methods
        
        public static MultiColumnHeaderState CreateMultiColumnHeaderState()
        {
            var columns = new[]
            {
                CreateTagColumn(),
                CreateNameColumn(),
                new MultiColumnHeaderState.Column
                {
                    headerContent = new GUIContent("Startup"),
                    headerTextAlignment = TextAlignment.Center,
                    autoResize = false,
                    canSort = true,
                    maxWidth = 60,
                    allowToggleVisibility = false
                },
                CreateModuleColumn()
            };

            UnityEngine.Assertions.Assert.AreEqual(columns.Length, Enum.GetValues(typeof(ColumnType)).Length,
                "Number of columns should match number of enum values: You probably forgot to update one of them.");

            return new MultiColumnHeaderState(columns)
            {
                sortedColumnIndex = 1 // Default is sort by name
            };
        }
        
        protected override TreeViewItem BuildRoot()
        {
            Model.ClearIds();
            
            var root = new TreeViewItem {id = 0, depth = -1};

            var entityGroupReferences = Model.GetEntityGroups();

            foreach (var entityGroupReference in entityGroupReferences)
            {
                var entityGroup = entityGroupReference.Dereference(Model.Registry);
                var module = UTinyUtility.GetModules(entityGroup).FirstOrDefault();
                var moduleReference = null != module ? (UTinyModule.Reference) module : UTinyModule.Reference.None;
                var editable = moduleReference.Equals(Model.MainModule);
             
                if (null != entityGroup)
                {
                    if (!editable && State.FilterProjectOnly)
                    {
                        continue;
                    }
                }
                
                var item = new UTinyEntityGroupTreeViewItem(Model.Registry, Model.MainModule, moduleReference, entityGroupReference)
                {
                    id = GenerateInstanceId(entityGroupReference),
                    Editable = editable
                };

                root.AddChild(item);
            }

            return root;
        }
        
        #endregion
        
        #region Drawing
        
        protected override void RowGUI(RowGUIArgs args)
        {
            var entityGroupTreeViewItem = args.item as UTinyEntityGroupTreeViewItem;

            UnityEngine.Assertions.Assert.IsNotNull(entityGroupTreeViewItem);
            
            for (var i = 0; i < args.GetNumVisibleColumns(); ++i)
            {
                CellGUI(args.GetCellRect(i), entityGroupTreeViewItem, (ColumnType) args.GetColumn(i), ref args);
            }
        }

        private void CellGUI(Rect cellRect, UTinyEntityGroupTreeViewItem item, ColumnType columnType, ref RowGUIArgs args)
        {
            // Center cell rect vertically (makes it easier to place controls, icons etc in the cells)
            CenterRectUsingSingleLineHeight(ref cellRect);

            var entityGroup = item.EntityGroup.Dereference(Model.Registry);
            
            using (new GUIEnabledScope(item.Editable))
            {
                switch (columnType)
                {
                    case ColumnType.Icon:
                    {
                        GUI.DrawTexture(cellRect, null == entityGroup ? UTinyIcons.Warning : UTinyIcons.EntityGroup,
                            ScaleMode.ScaleToFit);
                    }
                    break;

                    case ColumnType.Name:
                    {
                        args.rowRect = cellRect;

                        using (new GUIColorScope(null == entityGroup ? Color.red : Color.white))
                        {
                            base.RowGUI(args);
                        }
                    }
                    break;

                    case ColumnType.Startup:
                    {
                        var mainModule = Model.MainModule.Dereference(Model.Registry);
                        if (entityGroup != null && mainModule.StartupEntityGroup.Equals(item.EntityGroup))
                        {
                            GUI.Label(cellRect, "X");
                        }
                    }
                    break;

                    case ColumnType.Module:
                    {
                        GUI.Label(cellRect, item.Module.Name);
                    }
                    break;
                }
            }
        }

        #endregion
        
        #region Clicking

        protected override void DoubleClickedItem(int id)
        {
            var obj = Model.FindByInstanceId(id);
            if (obj is UTinyEntityGroup.Reference)
            {
                UTinyEditorApplication.EntityGroupManager.LoadEntityGroup((UTinyEntityGroup.Reference) obj);
            }
        }

        #endregion
        
        #region Sorting

        protected override List<TreeViewItem> SortRows(List<TreeViewItem> rows)
        {
            var ascending = multiColumnHeader.IsSortedAscending(multiColumnHeader.sortedColumnIndex);

            var module = Model.MainModule.Dereference(Model.Registry); 
            
            IEnumerable<TreeViewItem> sorted;

            switch ((ColumnType) multiColumnHeader.sortedColumnIndex)
            {
                case ColumnType.Name:
                    sorted = ascending ? rows.OrderBy(i => i.displayName) : rows.OrderByDescending(i => i.displayName);
                    break;
                case ColumnType.Module:
                    sorted = ascending ? rows.OrderBy(i => (i as UTinyTreeViewItem)?.Module.Name) : rows.OrderByDescending(i => (i as UTinyTreeViewItem)?.Module.Name);
                    break;
                case ColumnType.Startup:
                    sorted = ascending ? rows.OrderBy(i => (i as UTinyEntityGroupTreeViewItem)?.EntityGroup.Equals(module.StartupEntityGroup)) : rows.OrderByDescending(i => (i as UTinyEntityGroupTreeViewItem)?.EntityGroup.Equals(module.StartupEntityGroup));
                    break;
                default:
                    return rows;
            }

            return sorted.ToList();
        }

        #endregion
        
        #region Dragging
        
        protected override bool CanStartDrag(CanStartDragArgs args)
        {
            return false;
        }

        protected override void SetupDragAndDrop(SetupDragAndDropArgs args)
        {
            DragAndDrop.PrepareStartDrag ();

            var ids = SortItemIDsInRowOrder(args.draggedItemIDs);
            var items = new List<UnityEngine.Object>(ids.Count);
            
            items.AddRange(ids.Select(EditorUtility.InstanceIDToObject).Where(obj => obj != null));

            DragAndDrop.objectReferences = items.ToArray();

            var title = items.Count > 1 ? "<Multiple>" : items[0].name;
			
            DragAndDrop.StartDrag(title);
        }

        protected override DragAndDropVisualMode HandleDragAndDrop (DragAndDropArgs args)
        {
            return DragAndDropVisualMode.None;
        }
        
        #endregion
        
        #region Rename

        protected override bool IsValidName(object @object, string name)
        {
            var module = Model.MainModule.Dereference(Model.Registry);
            var unique = module.EntityGroups.All(t => t.Name != name);
            return unique && base.IsValidName(@object, name);
        }

        #endregion
    }
}
#endif // NET_4_6
