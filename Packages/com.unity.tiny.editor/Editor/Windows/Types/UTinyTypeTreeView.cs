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
    public class UTinyTypeTreeView : UTinyTreeView<UTinyTypeTreeState, UTinyTypeTreeModel>
    {
        #region Types

        /// <summary>
        /// Columns for this tree
        /// </summary>
        private enum ColumnType
        {
            Icon,
            Name,
            Type,
            Array,
            Module,
            Description
        }

        #endregion

        #region Fields

        private List<UTinyType.Reference> m_AvailableFieldTypes;

        #endregion

        #region Public Methods

        public UTinyTypeTreeView(UTinyTypeTreeState state, UTinyTypeTreeModel model) : base(state, model)
        {
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
                    headerContent = new GUIContent("Type"),
                    headerTextAlignment = TextAlignment.Left,
                    sortedAscending = true,
                    sortingArrowAlignment = TextAlignment.Left,
                    width = 200,
                    minWidth = 60,
                    autoResize = false,
                    allowToggleVisibility = false
                },
                new MultiColumnHeaderState.Column
                {
                    headerContent = new GUIContent("Array"),
                    headerTextAlignment = TextAlignment.Left,
                    sortedAscending = false,
                    canSort = false,
                    sortingArrowAlignment = TextAlignment.Left,
                    width = 40,
                    minWidth = 40,
                    autoResize = false,
                    allowToggleVisibility = false
                },
                CreateModuleColumn(),
                CreateDescriptionColumn()
            };

            UnityEngine.Assertions.Assert.AreEqual(columns.Length, Enum.GetValues(typeof(ColumnType)).Length,
                "Number of columns should match number of enum values: You probably forgot to update one of them.");

            var state = new MultiColumnHeaderState(columns)
            {
                sortedColumnIndex = 1, // Default is sort by name
                visibleColumns = new[] {0, 1, 2, 3, 5},
            };

            return state;
        }

        protected override TreeViewItem BuildRoot()
        {
            ClearInstanceIds();

            var root = new TreeViewItem {id = 0, depth = -1};

            var types = Model.GetTypes();

            foreach (var typeReference in types)
            {
                var type = typeReference.Dereference(Model.Registry);

                var module = UTinyUtility.GetModules(Model.Registry, typeReference).FirstOrDefault();
                var moduleReference = null != module ? (UTinyModule.Reference) module : UTinyModule.Reference.None;
                var editable = moduleReference.Id == Model.MainModule.Id;

                if (null != type)
                {
                    if (type.TypeCode == UTinyTypeCode.Component && !State.FilterComponents)
                    {
                        continue;
                    }

                    if (type.TypeCode == UTinyTypeCode.Struct && !State.FilterStructs)
                    {
                        continue;
                    }

                    if (type.TypeCode == UTinyTypeCode.Enum && !State.FilterEnums)
                    {
                        continue;
                    }

                    if (!editable && State.FilterProjectOnly)
                    {
                        continue;
                    }
                }

                var item = new UTinyTypeTreeViewItem(Model.Registry, Model.MainModule, moduleReference, (UTinyType.Reference) type)
                {
                    id = GenerateInstanceId(typeReference),
                    Editable = editable
                };

                if (null != type)
                {
                    foreach (var field in type.Fields)
                    {
                        item.AddChild(new UTinyFieldTreeViewItem(Model.Registry, Model.MainModule, moduleReference, field)
                        {
                            id = GenerateInstanceId(field),
                            icon = UTinyIcons.Variable,
                            depth = 1,
                            Editable = editable
                        });
                    }
                }

                root.AddChild(item);
            }

            return root;
        }

        #endregion

        #region Drawing

        public override bool DrawLayout()
        {
            m_AvailableFieldTypes = Model.GetAvailableFieldTypes();

            return base.DrawLayout();
        }

        protected override void RowGUI(RowGUIArgs args)
        {
            var typeTreeViewItem = args.item as UTinyTypeTreeViewItem;

            if (typeTreeViewItem != null)
            {
                for (var i = 0; i < args.GetNumVisibleColumns(); ++i)
                {
                    CellGUI(args.GetCellRect(i), typeTreeViewItem, (ColumnType) args.GetColumn(i), ref args);
                }
            }
            else
            {
                for (var i = 0; i < args.GetNumVisibleColumns(); ++i)
                {
                    CellGUI(args.GetCellRect(i), args.item as UTinyFieldTreeViewItem, (ColumnType) args.GetColumn(i), ref args);
                }
            }
        }

        private void CellGUI(Rect cellRect, UTinyTypeTreeViewItem item, ColumnType columnType, ref RowGUIArgs args)
        {
            CenterRectUsingSingleLineHeight(ref cellRect);

            var type = item.Type.Dereference(Model.Registry);

            using (new GUIEnabledScope(item.Editable))
            {
                switch (columnType)
                {
                    case ColumnType.Icon:
                    {
                        if (null == type)
                        {
                            GUI.DrawTexture(cellRect, UTinyIcons.Warning, ScaleMode.ScaleToFit);
                        }
                        else if (type.TypeCode == UTinyTypeCode.Component)
                        {
                            GUI.DrawTexture(cellRect, UTinyIcons.Component, ScaleMode.ScaleToFit);
                        }
                        else if (type.TypeCode == UTinyTypeCode.Struct)
                        {
                            GUI.DrawTexture(cellRect, UTinyIcons.Struct, ScaleMode.ScaleToFit);
                        }
                        else if (type.TypeCode == UTinyTypeCode.Enum)
                        {
                            GUI.DrawTexture(cellRect, UTinyIcons.Enum, ScaleMode.ScaleToFit);
                        }
                    }
                        break;

                    case ColumnType.Name:
                    {
                        args.rowRect = cellRect;

                        using (new GUIColorScope(null == type ? Color.red : Color.white))
                        {
                            base.RowGUI(args);
                        }
                    }
                        break;

                    case ColumnType.Type:
                    {
                        if (null != type)
                        {
                            GUI.Label(cellRect, $"{type.TypeCode}");
                        }
                    }
                        break;

                    case ColumnType.Module:
                    {
                        GUI.Label(cellRect, item.Module.Name);
                    }
                        break;

                    case ColumnType.Description:
                    {
                        if (type != null)
                        {
                            if (item.Editable)
                            {
                                type.Documentation.Summary = EditorGUI.TextField(cellRect, type.Documentation.Summary);
                            }
                            else
                            {
                                GUI.Label(cellRect, type.Documentation.Summary);
                            }
                        }
                    }
                        break;
                }
            }
        }

        private void CellGUI(Rect cellRect, UTinyFieldTreeViewItem item, ColumnType columnType, ref RowGUIArgs args)
        {
            CenterRectUsingSingleLineHeight(ref cellRect);

            using (new GUIEnabledScope(item.Editable))
            {
                var field = item.Field;

                Assert.IsNotNull(field);

                var fieldType = field.FieldType.Dereference(Model.Registry);

                // @TODO Seperate enum into a seperate method call
                var isEnum = field.DeclaringType.TypeCode == UTinyTypeCode.Enum;

                switch (columnType)
                {
                    case ColumnType.Name:
                    {
                        args.rowRect = cellRect;
                        base.RowGUI(args);
                    }
                        break;

                    case ColumnType.Type:
                    {
                        if (isEnum)
                        {
                            GUI.Label(cellRect, field.FieldType.Name);
                        }
                        else
                        {
                            using (new GUIColorScope(null == fieldType ? Color.red : !m_AvailableFieldTypes.Contains(field.FieldType) ? Color.yellow : Color.white))
                            {
                                if (GUI.Button(cellRect, new GUIContent(field.FieldType.Name), "DropdownButton"))
                                {
                                    var menu = new GenericMenu();

                                    // Fetch all project types

                                    foreach (var type in m_AvailableFieldTypes)
                                    {
                                        menu.AddItem(new GUIContent(type.Name), field.FieldType.Equals(type), t =>
                                        {
                                            // @TODO This is a lossy operation
                                            // We should query the scenes to see if the type exists as an instance (UTinyClass) then pop a confirmation dialog
                                            field.FieldType = (UTinyType.Reference) t;
                                        }, type);
                                    }

                                    menu.DropDown(cellRect);
                                }
                            }
                        }
                    }
                        break;

                    case ColumnType.Array:
                    {
                        if (!isEnum)
                        {
                            // @TODO We should maybe do an array type enum to support fixed sized array
                            // We can show a text box to allow the user to choose a size
                            field.Array = GUI.Toggle(cellRect, field.Array, GUIContent.none);
                        }
                    }
                        break;

                    case ColumnType.Description:
                    {
                        if (item.Editable)
                        {
                            field.Documentation.Summary = EditorGUI.TextField(cellRect, field.Documentation.Summary);
                        }
                        else
                        {
                            GUI.Label(cellRect, field.Documentation.Summary);
                        }
                    }
                        break;
                }
            }
        }

        #endregion

        #region Sorting

        protected override void SortChildren(TreeViewItem item)
        {
            // Only sort the root element
            if (item.depth > -1)
            {
                return;
            }
            
            base.SortChildren(item);
        }

        protected override List<TreeViewItem> SortRows(List<TreeViewItem> rows)
        {
            var ascending = multiColumnHeader.IsSortedAscending(multiColumnHeader.sortedColumnIndex);

            IEnumerable<TreeViewItem> sorted;

            switch ((ColumnType) multiColumnHeader.sortedColumnIndex)
            {
                case ColumnType.Icon:
                    sorted = ascending
                        ? rows.OrderBy(i => (i as UTinyTypeTreeViewItem)?.Type.Dereference(Model.Registry)?.TypeCode)
                        : rows.OrderByDescending(i => (i as UTinyTypeTreeViewItem)?.Type.Dereference(Model.Registry)?.TypeCode);
                    break;
                case ColumnType.Name:
                    sorted = ascending ? rows.OrderBy(i => i.displayName) : rows.OrderByDescending(i => i.displayName);
                    break;
                case ColumnType.Type:
                    sorted = ascending
                        ? rows.OrderBy(i => (i as UTinyTypeTreeViewItem)?.Type.Dereference(Model.Registry)?.TypeCode)
                        : rows.OrderByDescending(i => (i as UTinyTypeTreeViewItem)?.Type.Dereference(Model.Registry)?.TypeCode);
                    break;
                case ColumnType.Module:
                    sorted = ascending ? rows.OrderBy(i => (i as UTinyTreeViewItem)?.Module.Name) : rows.OrderByDescending(i => (i as UTinyTreeViewItem)?.Module.Name);
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
            return args.draggedItemIDs.All(id => Model.FindByInstanceId(id) is UTinyField);
        }

        protected override void SetupDragAndDrop(SetupDragAndDropArgs args)
        {
            DragAndDrop.PrepareStartDrag();

            var ids = SortItemIDsInRowOrder(args.draggedItemIDs);
            var items = new List<object>(ids.Count);

            items.AddRange(ids.Select(Model.FindByInstanceId).Where(obj => obj != null));

            var title = items.Count > 1 ? "<Multiple>" : (items[0] as INamed)?.Name ?? "<Item>";

            UTinyDragAndDrop.ObjectReferences = items.ToArray();
            UTinyDragAndDrop.StartDrag(title);
        }

        protected override DragAndDropVisualMode HandleDragAndDrop(DragAndDropArgs args)
        {
            var draggedObjects = UTinyDragAndDrop.ObjectReferences;

            if (null == draggedObjects)
            {
                return DragAndDropVisualMode.None;
            }
            
            var fields = new List<UTinyField>();

            foreach (var obj in draggedObjects)
            {
                var field = obj as UTinyField;

                if (field == null)
                {
                    return DragAndDropVisualMode.None;
                }

                fields.Add(field);
            }

            switch (args.dragAndDropPosition)
            {
                case DragAndDropPosition.UponItem:
                case DragAndDropPosition.BetweenItems:
                {
                    var type = (args.parentItem as UTinyTypeTreeViewItem)?.Type.Dereference(Model.Registry);

                    if (type == null)
                    {
                        return DragAndDropVisualMode.None;
                    }

                    if (type.IsEnum && fields.Any(f => f.DeclaringType != type))
                    {
                        return DragAndDropVisualMode.None;
                    }

                    if (args.performDrop)
                    {
                        var index = args.insertAtIndex;

                        foreach (var field in fields)
                        {
                            if (type == field.DeclaringType && field.DeclaringType.Fields.IndexOf(field) < index)
                            {
                                index--;
                            }

                            field.DeclaringType.RemoveField(field);
                        }

                        if (index < 0)
                        {
                            foreach (var field in fields)
                            {
                                type.AddField(field);
                            }
                        }
                        else
                        {
                            for (var i = fields.Count - 1; i >= 0; i--)
                            {
                                type.InsertField(index, fields[i]);
                            }
                        }

                        SetExpanded(State.GetInstanceId(type.Id), true);
                        Reload();
                    }

                    return DragAndDropVisualMode.Move;
                }
                case DragAndDropPosition.OutsideItems:
                    return DragAndDropVisualMode.None;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        #endregion

        #region Rename

        protected override bool IsValidName(object @object, string name)
        {
            var unique = true;

            if (@object is UTinyField)
            {
                var field = (UTinyField) @object;
                unique = field?.DeclaringType.Fields.All(f => f.Name != name) ?? true;
            }
            else if (@object is UTinyType.Reference)
            {
                var module = Model.MainModule.Dereference(Model.Registry);
                unique = module.Components.All(t => t.Name != name) &&
                         module.Structs.All(t => t.Name != name) &&
                         module.Enums.All(t => t.Name != name);
            }

            return unique && base.IsValidName(@object, name);
        }

        #endregion

        #region Selection
        
        protected override void SingleClickedItem(int id)
        {
            ShowTypeInspector();
            base.SingleClickedItem(id);
        }
        
        protected override void SelectionChanged(IList<int> selectedIds)
        {
            ShowTypeInspector();
            base.SelectionChanged(selectedIds);
        }

        private void ShowTypeInspector()
        {
            var @object = GetSelected().FirstOrDefault();

            if (@object is UTinyType.Reference)
            {
                var type = ((UTinyType.Reference) @object).Dereference(Model.Registry);
                UTinyTypeViewer.SetType(type);
            }
            else if (@object is UTinyField)
            {
                var field = (UTinyField) @object;
                UTinyTypeViewer.SetType(field.DeclaringType);
            }
        }

        #endregion
    }
}
#endif // NET_4_6
