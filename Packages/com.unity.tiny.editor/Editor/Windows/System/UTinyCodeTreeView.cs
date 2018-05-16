#if NET_4_6
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

namespace Unity.Tiny
{
	public class UTinyCodeTreeView : UTinyTreeView<UTinyCodeTreeState, UTinyCodeTreeModel>
    {
        #region Types
	    
        /// <summary>
        /// Columns for this tree
        /// </summary>
        private enum ColumnType
        {
            Icon,
            Name,
	        Module,
        }
	    
	    #endregion
	    
	    #region Public Methods
	    
        public UTinyCodeTreeView(UTinyCodeTreeState state, UTinyCodeTreeModel model) : base(state, model)
        {
            extraSpaceBeforeIconAndLabel = 8f;
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
	            CreateModuleColumn(),
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

            var systems = Model.GetSystems();

            foreach (var systemReference in systems)
            {
	            var system = systemReference.Dereference(Model.Registry);
                
	            var module = UTinyUtility.GetModules(Model.Registry, systemReference).FirstOrDefault();
	            var moduleReference = null != module ? (UTinyModule.Reference) module : UTinyModule.Reference.None;
	            var editable = moduleReference.Id == Model.MainModule.Id;
             
	            if (system != null)
	            {
		            if (!State.FilterSystems)
		            {
			            continue;
		            }

		            if (!editable && State.FilterProjectOnly)
		            {
			            continue;
		            }
	            }
	            
                var item = new UTinySystemTreeViewItem(Model.Registry, Model.MainModule, moduleReference, systemReference)
                {
                    id = GenerateInstanceId(systemReference),
                    Editable = editable
                };

                root.AddChild(item);
            }
	        
	        var scripts = Model.GetScripts();

	        foreach (var scriptReference in scripts)
	        {
		        var script = scriptReference.Dereference(Model.Registry);
                
		        var module = UTinyUtility.GetModules(Model.Registry, scriptReference).FirstOrDefault();
		        var moduleReference = null != module ? (UTinyModule.Reference) module : UTinyModule.Reference.None;
		        var editable = moduleReference.Id == Model.MainModule.Id;
             
		        if (script != null)
		        {
			        if (!State.FilterScripts)
			        {
				        continue;
			        }

			        if (!editable && State.FilterProjectOnly)
			        {
				        continue;
			        }
		        }
	            
		        var item = new UTinyScriptTreeViewItem(Model.Registry, Model.MainModule, moduleReference, scriptReference)
		        {
			        id = GenerateInstanceId(scriptReference),
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
            var systemTreeViewItem = args.item as UTinySystemTreeViewItem;

            if (systemTreeViewItem != null)
            {
                for (var i = 0; i < args.GetNumVisibleColumns(); ++i)
                {
                    CellGUI(args.GetCellRect(i), systemTreeViewItem, (ColumnType) args.GetColumn(i), ref args);
                }
            }
	        
	        var scriptTreeViewItem = args.item as UTinyScriptTreeViewItem;

	        if (scriptTreeViewItem != null)
	        {
		        
		        for (var i = 0; i < args.GetNumVisibleColumns(); ++i)
		        {
			        CellGUI(args.GetCellRect(i), scriptTreeViewItem, (ColumnType) args.GetColumn(i), ref args);
		        }
	        }
        }

        private void CellGUI(Rect cellRect, UTinySystemTreeViewItem item, ColumnType columnType, ref RowGUIArgs args)
        {
            CenterRectUsingSingleLineHeight(ref cellRect);
	        
            using (new GUIEnabledScope(item.Editable))
            {
	            var system = item.System.Dereference(Model.Registry);
                switch (columnType)
                {
                    case ColumnType.Icon:
                    {
                        GUI.DrawTexture(cellRect, null == system ? UTinyIcons.Warning : UTinyIcons.System, ScaleMode.ScaleToFit);
                    }
					break;

                    case ColumnType.Name:
                    {
						var toggleRect = cellRect;
						toggleRect.x += 8;
						toggleRect.width = 18;
						item.Schedule = EditorGUI.Toggle(toggleRect, item.Schedule);
                        args.rowRect = cellRect;
						base.RowGUI(args);
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
	    
	    private void CellGUI(Rect cellRect, UTinyScriptTreeViewItem item, ColumnType columnType, ref RowGUIArgs args)
	    {
		    CenterRectUsingSingleLineHeight(ref cellRect);
		    using (new GUIEnabledScope(item.Editable))
		    {
			    var script = item.Script.Dereference(Model.Registry);
			    switch (columnType)
			    {
				    case ColumnType.Icon:
				    {
					    GUI.DrawTexture(cellRect, null == script ? UTinyIcons.Warning : UTinyIcons.Function, ScaleMode.ScaleToFit);
				    }
					    break;

				    case ColumnType.Name:
				    {
					    args.rowRect = cellRect;
					    base.RowGUI(args);
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

        #region Sorting

        protected override List<TreeViewItem> SortRows(List<TreeViewItem> rows)
        {
            var ascending = multiColumnHeader.IsSortedAscending(multiColumnHeader.sortedColumnIndex);

	        IEnumerable<TreeViewItem> sorted;
	        
            switch ((ColumnType) multiColumnHeader.sortedColumnIndex)
            {
                case ColumnType.Icon:
	                sorted = ascending
                        ? rows.OrderBy(i => i is UTinySystemTreeViewItem)
                        : rows.OrderByDescending(i => i is UTinySystemTreeViewItem);
                    break;
                case ColumnType.Name:
	                sorted = ascending ? rows.OrderBy(i => i.displayName) : rows.OrderByDescending(i => i.displayName);
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
			return false;
		}

		protected override void SetupDragAndDrop(SetupDragAndDropArgs args)
		{
			/*
			DragAndDrop.PrepareStartDrag ();

			var ids = SortItemIDsInRowOrder(args.draggedItemIDs);
			var items = new List<UnityEngine.Object>(ids.Count);
			
			items.AddRange(ids.Select(EditorUtility.InstanceIDToObject).Where(obj => obj != null));

			DragAndDrop.objectReferences = items.ToArray();

			var title = items.Count > 1 ? "<Multiple>" : items[0].name;
			
			DragAndDrop.StartDrag(title);
			*/
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
		    var unique = module.Systems.All(t => t.Name != name) &&
						 module.Scripts.All(t => t.Name != name);
		    return unique && base.IsValidName(@object, name);
	    }

	    #endregion
    }
}
#endif // NET_4_6
