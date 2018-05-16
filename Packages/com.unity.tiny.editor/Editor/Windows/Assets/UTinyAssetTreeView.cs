#if NET_4_6
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

namespace Unity.Tiny
{
    public class UTinyAssetTreeView : TreeView, IDrawable, IDirtyable
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
            Status,
            References,
            Path,
        }

        #endregion

        #region Fields

        private static readonly int s_DropFieldHash = "UTinyAssetTreeView.DropField".GetHashCode();
        private bool m_Dirty;
        private List<string> m_AssetNames;

        #endregion

        #region Properties

        public UTinyAssetTreeState State { get; }
        public UTinyAssetTreeModel Model { get; }

        #endregion

        #region Public Methods

        public UTinyAssetTreeView(UTinyAssetTreeState state, UTinyAssetTreeModel model) : base(state.TreeViewState, new MultiColumnHeader(state.MultiColumnHeaderState))
        {
            rowHeight = 20;
            showAlternatingRowBackgrounds = true;
            showBorder = true;

            State = state;
            Model = model;

            multiColumnHeader.sortingChanged += HandleSortingChanged;

            Reload();

            columnIndexForTreeFoldouts = 1;
        }

        #endregion

        #region Data Methods

        public static MultiColumnHeaderState CreateMultiColumnHeaderState()
        {
            var columns = new[]
            {
                UTinyTreeView.CreateTagColumn(),
                UTinyTreeView.CreateNameColumn(),
                new MultiColumnHeaderState.Column
                {
                    headerContent = new GUIContent("Type"),
                    headerTextAlignment = TextAlignment.Left,
                    sortedAscending = true,
                    sortingArrowAlignment = TextAlignment.Left,
                    width = 75,
                    minWidth = 60,
                    autoResize = false
                },
                new MultiColumnHeaderState.Column
                {
                    headerContent = new GUIContent("Include"),
                    headerTextAlignment = TextAlignment.Left,
                    sortedAscending = true,
                    sortingArrowAlignment = TextAlignment.Left,
                    width = 60,
                    minWidth = 60,
                    autoResize = false
                },
                new MultiColumnHeaderState.Column
                {
                    headerContent = new GUIContent("Entity Ref"),
                    headerTextAlignment = TextAlignment.Left,
                    sortedAscending = true,
                    sortingArrowAlignment = TextAlignment.Left,
                    width = 75,
                    minWidth = 60,
                    autoResize = false
                },
                new MultiColumnHeaderState.Column
                {
                    headerContent = new GUIContent("Path"),
                    headerTextAlignment = TextAlignment.Left,
                    sortedAscending = true,
                    sortingArrowAlignment = TextAlignment.Left,
                    width = 75,
                    minWidth = 60,
                    autoResize = false
                }
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

            var assets = Model.GetAssetInfos();

            foreach (var asset in assets)
            {
                root.AddChild(BuildItem(asset, 0));
            }

            return root;
        }

        private UTinyAssetTreeViewItem BuildItem(UTinyAssetInfo assetInfo, int depth)
        {
            var item = new UTinyAssetTreeViewItem(Model.Registry, Model.MainModule, Model.MainModule, assetInfo)
            {
                id = Model.GenerateId(assetInfo),
                Editable = assetInfo.ExplicitReferences.Contains(Model.MainModule),
                depth = depth
            };

            if (assetInfo.Parent != null)
            {
                item.icon = EditorGUIUtility.ObjectContent(null, assetInfo.Object.GetType()).image as Texture2D;
            }

            foreach (var child in assetInfo.Children)
            {
                item.AddChild(BuildItem(child, depth + 1));
            }

            return item;
        }

        protected override IList<TreeViewItem> BuildRows(TreeViewItem root)
        {
            var rows = base.BuildRows(root);
            Sort(root, rows);
            return rows;
        }

        #endregion

        #region IDirtyable

        public void SetDirty()
        {
            m_Dirty = true;
        }

        #endregion

        #region Drawing

        public bool DrawLayout()
        {
            if (m_Dirty)
            {
                m_Dirty = false;
                Reload();
            }

            m_AssetNames = Model.GetAssetInfos().Select(a => a.Name).ToList();

            GUILayout.Space(2);

            var rect = GUILayoutUtility.GetRect(GUIContent.none, GUIStyle.none, GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));

            // Support dropping scene objects
            var objects = UTinyEditorUtility.DoDropField(rect, s_DropFieldHash, objs => objs.ToArray(), UTinyStyles.DropField);

            if (null != objects)
            {
                foreach (var @object in objects)
                {
                    Model.MainModule.Dereference(Model.Registry).AddAsset(@object);
                    SetDirty();
                }
            }

            OnGUI(rect);

            return false;
        }

        protected override void SingleClickedItem(int id)
        {
            var info = Model.Find(id);
            if (null != info.Object)
            {
                EditorGUIUtility.PingObject(info.Object);
                Selection.activeObject = info.Object;
            }
            base.SingleClickedItem(id);
        }

        protected override void DoubleClickedItem(int id)
        {
            var info = Model.Find(id);
            if (null != info.Object)
            {
                Selection.activeObject = info.Object;
            }
            base.DoubleClickedItem(id);
        }

        protected override void RowGUI(RowGUIArgs args)
        {
            var assetTreeViewItem = args.item as UTinyAssetTreeViewItem;

            if (assetTreeViewItem != null)
            {
                for (var i = 0; i < args.GetNumVisibleColumns(); ++i)
                {
                    CellGUI(args.GetCellRect(i), assetTreeViewItem, (ColumnType) args.GetColumn(i), ref args);
                }
            }
        }

        private void CellGUI(Rect cellRect, UTinyAssetTreeViewItem item, ColumnType columnType, ref RowGUIArgs args)
        {
            // Center cell rect vertically (makes it easier to place controls, icons etc in the cells)
            CenterRectUsingSingleLineHeight(ref cellRect);

            var @object = item.AssetInfo.Object;
            var name = item.AssetInfo.Name;
            var isUnique = m_AssetNames.Count(s => string.Equals(s, name)) <= 1;

            using (new GUIColorScope(isUnique ? Color.white : Color.red))
            using (new GUIEnabledScope(item.Editable))
            {
                var content = EditorGUIUtility.ObjectContent(null, @object.GetType());
                switch (columnType)
                {
                    case ColumnType.Icon:
                    {
                        if (item.AssetInfo.Parent == null)
                        {
                            GUI.DrawTexture(cellRect, content.image, ScaleMode.ScaleToFit);
                        }
                    }
                        break;

                    case ColumnType.Name:
                    {
                        args.rowRect = cellRect;
                        base.RowGUI(args);
                    }
                        break;

                    case ColumnType.Path:
                    {
                        GUI.Label(cellRect, item.AssetInfo.AssetPath);
                    }
                        break;

                    case ColumnType.Type:
                    {
                        GUI.Label(cellRect, @object.GetType().Name);
                    }
                        break;

                    case ColumnType.Status:
                    {
                        var status = item.AssetInfo.IncludedExplicitly ? "Explicit" : "Implicit";

                        using (new GUIColorScope(item.AssetInfo.IncludedExplicitly ? Color.green : Color.white))
                        {
                            GUI.Label(cellRect, status);
                        }
                    }
                        break;

                    case ColumnType.References:
                    {
                        GUI.Label(cellRect, item.AssetInfo.ImplicitReferences.Count.ToString());
                    }
                        break;
                }
            }

            if (Event.current.type == EventType.MouseDrag && cellRect.Contains(Event.current.mousePosition))
            {
                DragAndDrop.PrepareStartDrag ();
                DragAndDrop.objectReferences = new[] { item.AssetInfo.Object };
                DragAndDrop.StartDrag("Dragging sprite");
                Event.current.Use();
            }
        }

        #endregion

        private string GetAssetName(UTinyAssetInfo info)
        {
            if (null == info.Object)
            {
                return "(null)";
            }
            
            if (info.ExplicitReferences.Count <= 0)
            {
                return info.Object.name;
            }

            foreach (var reference in info.ExplicitReferences)
            {
                var module = reference.Dereference(Model.Registry);
                var asset = module.GetAsset(info.Object);
                
                if (!string.IsNullOrEmpty(asset?.Name))
                {
                    return asset.Name;
                }
            }

            return info.Object.name;
        }

        #region Keyboard

        protected override void KeyEvent()
        {
            base.KeyEvent();

            if (Event.current.type != EventType.KeyDown)
            {
                return;
            }

            if (Event.current.keyCode != KeyCode.Delete)
            {
                return;
            }

            if (EditorUtility.DisplayDialog(string.Empty, "Are you sure you want to remove the selected asset?", "Remove", "No"))
            {
                var module = Model.MainModule.Dereference(Model.Registry);
                var objects = GetSelection().Select(EditorUtility.InstanceIDToObject).Where(o => o != null);
                foreach (var @object in objects)
                {
                    module.RemoveAsset(@object);
                }

                SetDirty();
            }
        }

        #endregion

        #region Context Menu

        protected override void ContextClicked()
        {
            base.ContextClicked();

            var menu = new GenericMenu();

            var module = Model.MainModule.Dereference(Model.Registry);

            // @TODO This needs to be cleaned up, we should refactor the way we return asset infos. This should be done at the `AssetIterator` level
            //       Sometimes we need a `hierarchical` representation (UI) and other times we want a `flat` representation (vaidation and export)
            var assets = Model.GetAssetInfos();
            var selections = GetSelection().Select(EditorUtility.InstanceIDToObject).Where(o => o != null).ToList();
            var selectedAssets = selections.Select(s => GetAssetInfo(assets, s));
            
            if (selectedAssets.Any(i => i.IncludedExplicitly))
            {
                menu.AddItem(new GUIContent("Make Explicit Reference"), false, () =>
                {
                    foreach (var @object in selections)
                    {
                        module.AddAsset(@object);
                    }

                    SetDirty();
                });

                menu.AddItem(new GUIContent("Remove Explicit Reference"), false, () =>
                {
                    foreach (var @object in selections)
                    {
                        module.RemoveAsset(@object);
                    }

                    SetDirty();
                });
            }
            else
            {
                menu.AddDisabledItem(new GUIContent("Make Explicit Reference"));
                menu.AddDisabledItem(new GUIContent("Remove Explicit Reference"));
            }

            menu.ShowAsContext();
        }
        
        private UTinyAssetInfo GetAssetInfo(IEnumerable<UTinyAssetInfo> assets, UnityEngine.Object @object)
        {
            foreach (var asset in assets)
            {
                if (asset.Object == @object)
                {
                    return asset;
                }

                var info = GetAssetInfo(asset.Children, @object);
                if (null != info)
                {
                    return info;
                }
            }

            return null;
        }

        #endregion

        #region Sorting

        private void Sort(TreeViewItem root, IList<TreeViewItem> rows)
        {
            if (rows.Count <= 1)
            {
                return;
            }

            if (multiColumnHeader.sortedColumnIndex == -1)
            {
                return;
            }

            root.children = SortRows(root.children);

            TreeToList(root, rows);

            Repaint();
        }

        private List<TreeViewItem> SortRows(List<TreeViewItem> rows)
        {
            var ascending = multiColumnHeader.IsSortedAscending(multiColumnHeader.sortedColumnIndex);

            IEnumerable<TreeViewItem> sorted;

            switch ((ColumnType) multiColumnHeader.sortedColumnIndex)
            {
                case ColumnType.Icon:
                    sorted = ascending
                        ? rows.OrderBy(i => (i as UTinyAssetTreeViewItem)?.AssetInfo.Object.GetType().Name)
                        : rows.OrderByDescending(i => (i as UTinyAssetTreeViewItem)?.AssetInfo.Object.GetType().Name);
                    break;
                case ColumnType.Name:
                    sorted = ascending
                        ? rows.OrderBy(i => i.displayName)
                        : rows.OrderByDescending(i => i.displayName);
                    break;
                case ColumnType.Type:
                    sorted = ascending
                        ? rows.OrderBy(i => (i as UTinyAssetTreeViewItem)?.AssetInfo.Object.GetType().Name)
                        : rows.OrderByDescending(i => (i as UTinyAssetTreeViewItem)?.AssetInfo.Object.GetType().Name);
                    break;
                case ColumnType.Status:
                    sorted = ascending
                        ? rows.OrderBy(i => (i as UTinyAssetTreeViewItem)?.AssetInfo.ExplicitReferences.Count).ThenBy(i => (i as UTinyAssetTreeViewItem)?.AssetInfo.ImplicitReferences.Count)
                        : rows.OrderByDescending(i => (i as UTinyAssetTreeViewItem)?.AssetInfo.ExplicitReferences.Count)
                            .ThenByDescending(i => (i as UTinyAssetTreeViewItem)?.AssetInfo.ImplicitReferences.Count);
                    break;
                case ColumnType.References:
                    sorted = ascending
                        ? rows.OrderBy(i => (i as UTinyAssetTreeViewItem)?.AssetInfo.ImplicitReferences.Count)
                        : rows.OrderByDescending(i => (i as UTinyAssetTreeViewItem)?.AssetInfo.ImplicitReferences.Count);
                    break;
                case ColumnType.Path:
                    sorted = ascending
                        ? rows.OrderBy(i => (i as UTinyAssetTreeViewItem)?.AssetInfo.AssetPath)
                        : rows.OrderByDescending(i => (i as UTinyAssetTreeViewItem)?.AssetInfo.AssetPath);
                    break;

                default:
                    return rows;
            }

            return sorted.ToList();
        }

        private void TreeToList(TreeViewItem root, ICollection<TreeViewItem> result)
        {
            result.Clear();

            if (root.children == null)
            {
                return;
            }

            var stack = new Stack<TreeViewItem>();

            for (var i = root.children.Count - 1; i >= 0; i--)
            {
                stack.Push(root.children[i]);
            }

            while (stack.Count > 0)
            {
                var current = stack.Pop();

                if (!DoesItemMatchSearch(current, searchString ?? string.Empty))
                {
                    continue;
                }

                result.Add(current);

                if (!current.hasChildren || current.children[0] == null)
                {
                    continue;
                }

                if (!IsExpanded(current.id) && string.IsNullOrEmpty(searchString))
                {
                    continue;
                }

                for (var i = current.children.Count - 1; i >= 0; i--)
                {
                    if (!DoesItemMatchSearch(current.children[i], searchString ?? string.Empty))
                    {
                        continue;
                    }

                    stack.Push(current.children[i]);
                }
            }
        }

        #endregion

        #region Dragging

        protected override bool CanStartDrag(CanStartDragArgs args)
        {
            return false;
        }

        protected override void SetupDragAndDrop(SetupDragAndDropArgs args)
        {
        }

        protected override DragAndDropVisualMode HandleDragAndDrop(DragAndDropArgs args)
        {
            return DragAndDropVisualMode.None;
        }

        #endregion

        #region Event Handlers

        private void HandleSortingChanged(MultiColumnHeader multiColumnHeader)
        {
            Sort(rootItem, GetRows());
        }

        #endregion

        #region Rename

        protected override Rect GetRenameRect(Rect rowRect, int row, TreeViewItem item)
        {
            var cellRect = GetCellRectForTreeFoldouts(rowRect);
            CenterRectUsingSingleLineHeight(ref cellRect);
            return base.GetRenameRect(cellRect, row, item);
        }

        protected override bool CanRename(TreeViewItem item)
        {
            return true;
        }

        protected override void RenameEnded(RenameEndedArgs args)
        {
            if (!args.acceptedRename)
            {
                return;
            }

            if (!UTinyUtility.IsValidObjectName(args.newName))
            {
                Debug.LogWarningFormat("Invalid name  [{0}]", args.newName);
                return;
            }

            var info = Model.Find(args.itemID);
            var asset = Model.MainModule.Dereference(Model.Registry)?.GetOrAddAsset(info.Object);
            asset.Name = args.newName;
        }

        #endregion
    }
}
#endif // NET_4_6
