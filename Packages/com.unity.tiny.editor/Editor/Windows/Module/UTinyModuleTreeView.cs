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
    /// <summary>
    /// TreeView for modules
    /// </summary>
    public class UTinyModuleTreeView : UTinyTreeView<UTinyTreeState, UTinyModuleTreeModel>
    {
        #region Types
        
        /// <summary>
        /// Columns for this tree
        /// </summary>
        private enum ColumnType
        {
            Icon,
            Name,
            Status,
            Dependencies,
            ReferencedBy,
            Description
        }
        
        #endregion
        
        #region Public Methods

        public UTinyModuleTreeView(UTinyTreeState state, UTinyModuleTreeModel model) : base(state, model)
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
                    headerContent = new GUIContent("Status"),
                    headerTextAlignment = TextAlignment.Left,
                    sortedAscending = true,
                    sortingArrowAlignment = TextAlignment.Left,
                    width = 200,
                    minWidth = 60,
                    autoResize = false
                },
                new MultiColumnHeaderState.Column
                {
                    headerContent = new GUIContent("Dependencies"),
                    headerTextAlignment = TextAlignment.Left,
                    sortedAscending = true,
                    sortingArrowAlignment = TextAlignment.Left,
                    width = 200,
                    minWidth = 60,
                    canSort = false,
                    autoResize = false
                },
                new MultiColumnHeaderState.Column
                {
                    headerContent = new GUIContent("Referenced By"),
                    headerTextAlignment = TextAlignment.Left,
                    sortedAscending = true,
                    sortingArrowAlignment = TextAlignment.Left,
                    width = 200,
                    minWidth = 60,
                    canSort = false,
                    autoResize = false
                },
                CreateDescriptionColumn()
            };

            Assert.AreEqual(columns.Length, Enum.GetValues(typeof(ColumnType)).Length,
                "Number of columns should match number of enum values: You probably forgot to update one of them.");

            return new MultiColumnHeaderState(columns)
            {
                sortedColumnIndex = 1 // Default is sort by name
            };
        }

        protected override TreeViewItem BuildRoot()
        {
            ClearInstanceIds();
            
            var root = new TreeViewItem {id = 0, depth = -1};

            var modules = Model.GetModules();

            foreach (var reference in modules)
            {
                var item = new UTinyModuleTreeViewItem(Model.Registry, Model.MainModule, reference)
                {
                    id = GenerateInstanceId(reference)
                };

                var module = reference.Dereference(Model.Registry);
                
                if (null != module)
                {
                    module.Refresh();
                    
                    foreach (var component in module.Components)
                    {
                        item.AddChild(new TreeViewItem
                        {
                            id = GenerateInstanceId(component),
                            displayName = component.Name,
                            icon = UTinyIcons.Component,
                            depth = 1
                        });
                    }

                    foreach (var @struct in module.Structs)
                    {
                        item.AddChild(new TreeViewItem
                        {
                            id = GenerateInstanceId(@struct),
                            displayName = @struct.Name,
                            icon = UTinyIcons.Struct,
                            depth = 1
                        });
                    }

                    foreach (var script in module.Scripts)
                    {
                        item.AddChild(new TreeViewItem
                        {
                            id = GenerateInstanceId(script),
                            displayName = script.Name,
                            icon = UTinyIcons.Function,
                            depth = 1
                        });
                    }
                }

                root.AddChild(item);
            }

            return root;
        }
        
        #endregion

        #region Drawing

        protected override void RowGUI(RowGUIArgs args)
        {
            var moduleTreeViewItem = args.item as UTinyModuleTreeViewItem;

            if (moduleTreeViewItem != null)
            {
                for (var i = 0; i < args.GetNumVisibleColumns(); ++i)
                {
                    CellGUI(args.GetCellRect(i), moduleTreeViewItem, (ColumnType) args.GetColumn(i), ref args);
                }
            }
            else
            {
                for (var i = 0; i < args.GetNumVisibleColumns(); ++i)
                {
                    CellGUI(args.GetCellRect(i), args.item, (ColumnType) args.GetColumn(i), ref args);
                }
            }
        }

        private void CellGUI(Rect cellRect, UTinyModuleTreeViewItem item, ColumnType columnType, ref RowGUIArgs args)
        {
            // Center cell rect vertically (makes it easier to place controls, icons etc in the cells)
            CenterRectUsingSingleLineHeight(ref cellRect);

            using (new GUIEnabledScope(item.Status != UTinyModuleTreeViewItem.StatusType.Self && 
                                       (item.Status != UTinyModuleTreeViewItem.StatusType.IncludedRequired ||
                                       (item.Status == UTinyModuleTreeViewItem.StatusType.IncludedRequired && !item.Included))))
            {
                var moduleReference = item.Module;
                var module = item.Module.Dereference(Model.Registry);
                
                switch (columnType)
                {
                    case ColumnType.Icon:
                    {
                        GUI.DrawTexture(cellRect, null == module ? UTinyIcons.Warning : UTinyIcons.Module,
                            ScaleMode.ScaleToFit);
                    }
                        break;

                    case ColumnType.Name:
                    {
                        var toggleRect = cellRect;
                        toggleRect.x += GetContentIndent(item);
                        toggleRect.width = 18;

                        EditorGUI.BeginChangeCheck();

                        item.Included = EditorGUI.Toggle(toggleRect, item.Included);

                        if (EditorGUI.EndChangeCheck())
                        {
                            ShowConfigurationInspector();
                            Reload();
                        }

                        args.rowRect = cellRect;

                        using (new GUIColorScope(null == module ? Color.red : Color.white))
                        {
                            base.RowGUI(args);
                        }
                    }
                        break;

                    case ColumnType.Status:
                    {
                        string status;

                        switch (item.Status)
                        { 
                            case UTinyModuleTreeViewItem.StatusType.Self:
                                status = "";
                                break;
                            case UTinyModuleTreeViewItem.StatusType.Excluded:
                                status = "Excluded";
                                break;
                            case UTinyModuleTreeViewItem.StatusType.IncludedRequired:
                                status = "Included (Required)";
                                break;
                            case UTinyModuleTreeViewItem.StatusType.IncludedExplicit:
                                status = "Included (Explicit)";
                                break;
                            case UTinyModuleTreeViewItem.StatusType.IncludedImplicit:
                                status = "Included (Implicit)";
                                break;
                            default:
                                throw new ArgumentOutOfRangeException();
                        }

                        using (new GUIColorScope(item.Status != UTinyModuleTreeViewItem.StatusType.Excluded
                            ? item.Status != UTinyModuleTreeViewItem.StatusType.IncludedImplicit ?
                              Color.green
                            : Color.cyan
                            : Color.white))
                        {
                            GUI.Label(cellRect, status);
                        }
                    }
                        break;

                    case ColumnType.Dependencies:
                    {
                        var @string = "";

                        if (module != null)
                        {
                            for (var i = 0; i < module.Dependencies.Count; i++)
                            {
                                var @ref = module.Dependencies[i];
                                var m = @ref.Dereference(Model.Registry);

                                if (i > 0)
                                {
                                    @string += ", ";
                                }

                                if (null == m)
                                {
                                    @string += $"{@ref.Name} (missing)";
                                }
                                else
                                {
                                    @string += m.Name;
                                }
                            }
                        }

                        GUI.Label(cellRect, @string);
                    }
                        break;

                    case ColumnType.ReferencedBy:
                    {
                        var modules = UTinyModule.GetExplicitDependantModules(Model.Registry, moduleReference).Where(m => !m.IsProjectModule);
                        var @string = string.Join(", ", modules.Select(m => m.Name).ToArray());
                        GUI.Label(cellRect, @string);
                    }
                    break;

                    case ColumnType.Description:
                    {
                        if (module != null)
                        {
                            EditorGUI.LabelField(cellRect, module.Documentation.Summary);
                        }
                    }
                    break;
                }
            }
        }

        private void CellGUI(Rect cellRect, TreeViewItem item, ColumnType columnType, ref RowGUIArgs args)
        {
            CenterRectUsingSingleLineHeight(ref cellRect);

            if (columnType != ColumnType.Name)
            {
                return;
            }

            args.rowRect = cellRect;
            base.RowGUI(args);
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
                        ? rows.OrderBy(i => null != (i as UTinyModuleTreeViewItem)?.Module.Dereference(Model.Registry))
                        : rows.OrderByDescending(i => null == (i as UTinyModuleTreeViewItem)?.Module.Dereference(Model.Registry));
                    break;
                case ColumnType.Name:
                    sorted = ascending ? rows.OrderBy(i => i.displayName) : rows.OrderByDescending(i => i.displayName);
                    break;
                case ColumnType.Status:
                    sorted = ascending ? rows.OrderBy(i => (i as UTinyModuleTreeViewItem)?.Status) : rows.OrderByDescending(i => (i as UTinyModuleTreeViewItem)?.Status);
                    break;
                default:
                    return rows;
            }

            return sorted?.ToList();
        }

        #endregion
        
        #region Context

        protected override void ContextClicked()
        {
            
        }

        #endregion
        
        #region Selection

        protected override void SingleClickedItem(int id)
        {
            ShowConfigurationInspector();
            base.SingleClickedItem(id);
        }

        protected override void SelectionChanged(IList<int> selectedIds)
        {
            ShowConfigurationInspector();
            base.SelectionChanged(selectedIds);
        }

        private void ShowConfigurationInspector()
        {
            var project = Model.Project.Dereference(Model.Registry);
            if (project.Configuration.Equals(UTinyEntity.Reference.None))
            {
                return;
            }

            var entity = project.Configuration.Dereference(Model.Registry);

            if (null == entity)
            {
                return;
            }
                
            UTinyConfigurationViewer.SetEntity(entity);
        }

        #endregion
    }
}
#endif // NET_4_6
