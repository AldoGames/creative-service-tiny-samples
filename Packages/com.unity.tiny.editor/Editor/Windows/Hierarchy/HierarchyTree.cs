#if NET_4_6
using System.Collections.Generic;
using System.Linq;

using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;
using UnityEngine.Assertions;
using Unity.Tiny.Extensions;

using StringComparison = System.StringComparison;
using DropOnItemAction = System.Action<Unity.Tiny.HierarchyTreeItemBase, System.Collections.Generic.IEnumerable<Unity.Tiny.IEntityNode>>;
using DropBetweenAction = System.Action<Unity.Tiny.HierarchyTreeItemBase, System.Collections.Generic.IEnumerable<Unity.Tiny.IEntityNode>, int>;

namespace Unity.Tiny
{ 
    public class HierarchyTree : TreeView
    {
        #region Fields
        private readonly UTinyEditorContext m_Context;
        private readonly List<UTinyEntityGroup.Reference> m_EntityGroups;
        private string m_FilterString = string.Empty;
        private readonly Dictionary<System.Type, DropOnItemAction> DroppedOnMethod;
        private readonly Dictionary<System.Type, DropBetweenAction> DroppedBetweenMethod;
        #endregion

        #region Properties
        public IRegistry Registry { get { return m_Context?.Registry; } }
        public UTinyEntityGroupManager EntityGroupManager { get { return m_Context?.EntityGroupManager; } }
        private UTinyEntityGroup.Reference ActiveScene { get { return EntityGroupManager?.ActiveEntityGroup ?? UTinyEntityGroup.Reference.None; } }

        public string FilterString
        {
            get
            {
                return m_FilterString;
            }
            set
            {
                if (m_FilterString != value)
                {
                    m_FilterString = value;
                    Invalidate();
                }
            }
        }

        private bool ShouldReload { get; set; }
        private bool ContextClickedWithId { get; set; }

        private IList<int> IdsToExpand { get; set; }
        #endregion

        public HierarchyTree(UTinyEditorContext context, TreeViewState treeViewState)
        : base(treeViewState)
        {
            m_Context = context;
            m_EntityGroups = new List<UTinyEntityGroup.Reference>();

            DroppedOnMethod = new Dictionary<System.Type, DropOnItemAction>
            {
                { typeof(HierarchyEntityGroupGraph), DropUponSceneItem },
                { typeof(HierarchyEntity), DropUponEntityItem },
                { typeof(HierarchyStaticEntity), DropUponStaticEntityItem },
            };

            DroppedBetweenMethod = new Dictionary<System.Type, DropBetweenAction>
            {
                { typeof(HierarchyTreeItemBase), DropBetweenEntityGroupItems },
                { typeof(HierarchyEntityGroupGraph), DropBetweenRootEntities },
                { typeof(HierarchyEntity), DropBetweenChildrenEntities },
                { typeof(HierarchyStaticEntity), DropBetweenStaticEntities },
            };
            Invalidate();
            Reload();
        }

        #region API
        public void AddEntityGroup(UTinyEntityGroup.Reference entityGroupRef)
        {
            if (!m_EntityGroups.Contains(entityGroupRef))
            {
                m_EntityGroups.Add(entityGroupRef);
                Invalidate();
            }
        }

        public void RemoveEntityGroup(UTinyEntityGroup.Reference entityGroupRef)
        {
            if (m_EntityGroups.Remove(entityGroupRef))
            {
                Invalidate();
            }
        }

        public void ClearScenes()
        {
            m_EntityGroups.Clear();
            Invalidate();
        }

        public UTinyEntity.Reference CreateEntity(UTinyEntityGroup.Reference entityGroupRef)
        {
            return CreateEntity(entityGroupRef, true);
        }

        public UTinyEntity.Reference CreateStaticEntity(UTinyEntityGroup.Reference entityGroupRef)
        {
            return CreateEntity(entityGroupRef, false);
        }

        public UTinyEntity.Reference CreateEntity(UTinyEntityGroup.Reference entityGroupRef, bool addTransform)
        {
            var graph = UTinyHierarchyWindow.GetSceneGraph(entityGroupRef);
            if (null == graph)
            {
                return UTinyEntity.Reference.None;
            }
            var node = addTransform ? graph.Create() : graph.CreateStatic();

            var ids = AsInstanceIds(node);
            Selection.instanceIDs = ids;
            IdsToExpand = ids;

            UTinyHierarchyWindow.InvalidateSceneGraph();
            return node.Entity;
        }

        public List<IEntityNode> GetEntitySelection()
        {
            return GetSelection()
                .Select(id => FindItem(id, rootItem))
                .OfType<IEntityTreeItem>()
                .Select(item => item.Node)
                .ToList();
        }

        public void CreateEntityFromSelection()
        {
            var selection = new List<int>();
            var expanded = new List<int>();
            foreach (var group in GetEntitySelection().GroupBy(n => n.Graph))
            {
                var nodes = group.Key.Create(group.ToList());
                foreach (var node in nodes)
                {
                    selection.Add(GetInstanceId(node));
                    if (node is EntityNode)
                    {
                        var parentNode = (node as EntityNode).Parent;
                        if (null != parentNode)
                        {
                            expanded.Add(GetInstanceId(parentNode));
                        }
                    }
                }
            }
            Selection.instanceIDs = selection.ToArray();
            IdsToExpand = selection;
            UTinyHierarchyWindow.InvalidateSceneGraph();
        }

        public int GetInstanceId(IEntityNode node)
        {
            return node.Entity.Dereference(Registry).View.gameObject.GetInstanceID();
        }

        public void DuplicateSelection()
        {
            var selection = new List<int>();
            var expanded = new List<int>();
            foreach (var group in GetEntitySelection().GroupBy(n => n.Graph))
            {
                var nodes = group.Key.Duplicate(group.ToList());
                foreach (var node in nodes)
                {
                    selection.Add(GetInstanceId(node));
                    if (node is EntityNode)
                    {
                        var parentNode = (node as EntityNode).Parent;
                        if (null != parentNode)
                        {
                            expanded.Add(GetInstanceId(parentNode));
                        }
                    }
                }
            }
            Selection.instanceIDs = selection.ToArray();
            IdsToExpand = selection;
            UTinyHierarchyWindow.InvalidateSceneGraph();
        }

        public void DeleteSelection()
        {
            var nodes = GetEntitySelection();
            foreach (var node in nodes)
            {
                node.Graph.Delete(node);
            }

            UTinyHierarchyWindow.InvalidateSceneGraph();
        }

        public void Invalidate()
        {
            ShouldReload = true;
        }

        public void Rename(UTinyEntity.Reference entity)
        {
            var item = FindItem(entity.Dereference(m_Context.Registry).View.gameObject.GetInstanceID(), rootItem);
            BeginRename(item);
        }
        #endregion
        
        #region TreeView
        protected override TreeViewItem BuildRoot()
        {
            var nextId = int.MaxValue;
            var root = new HierarchyTreeItemBase() { id = nextId--, depth = -1, displayName = "Root" };

            if (null == m_EntityGroups || m_EntityGroups.Count == 0)
            {
                var item = new TreeViewItem { id = nextId--, depth = 0, displayName = "No group Opened" };
                root.AddChild(item);
                return root;
            }

            foreach (var entityGroupRef in m_EntityGroups)
            {
                var graph = UTinyHierarchyWindow.GetSceneGraph(entityGroupRef);
                if (null == graph)
                {
                    RemoveEntityGroup(entityGroupRef);
                    continue;
                }

                var entityGroup = graph.EntityGroupRef.Dereference(Registry);
                Assert.IsNotNull(entityGroup);
                var item = new HierarchyEntityGroupGraph { id = nextId--, depth = 0, displayName = entityGroup.Name, Value = graph };
                root.AddChild(item);

                foreach (var node in graph.Roots)
                {
                    BuildFromNode(node, item);
                }

                if (graph.StaticEntities.Count > 0)
                {
                    item.AddChild(new HierarchyTreeItemBase { id = nextId--, depth = 1});
                }

                foreach (var node in graph.StaticEntities)
                {
                    BuildFromNode(node, item);
                }
            }

            ShouldReload = false;
            return root;
        }

        public override void OnGUI(Rect rect)
        {
            if (ShouldReload)
            {
                Reload();
            }

            if (null != IdsToExpand)
            {
                ForceExpanded(IdsToExpand);
                IdsToExpand = null;
            }

            base.OnGUI(rect);
        }

        protected override void KeyEvent()
        {
            base.KeyEvent();
            if (Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.Delete)
            {
                DeleteSelection();
                Event.current.Use();
                return;
            }
        }

        protected override float GetCustomRowHeight(int row, TreeViewItem item)
        {
            var baseHeight = base.GetCustomRowHeight(row, item);
            if (item is HierarchyEntityGroupGraph)
            {
                return baseHeight + 4.0f;
            }

            if (item is IEntityTreeItem)
            {
                return baseHeight;
            }

            if (item is HierarchyTreeItemBase)
            {
                return 5.0f;
            }

            return baseHeight;
        }

        protected override void RowGUI(RowGUIArgs args)
        {
            var itemRect = args.rowRect;
            var item = args.item;

            if (item is HierarchyEntityGroupGraph)
            {
                DrawItem(itemRect, item as HierarchyEntityGroupGraph, args);
                return;
            }

            if (args.item is IEntityTreeItem)
            {
                DrawItem(itemRect, item as IEntityTreeItem, args);
                return;
            }

            if (args.item is HierarchyTreeItemBase)
            {
                DrawItem(itemRect, item as HierarchyTreeItemBase, args);
                return;
            }

            base.RowGUI(args);
        }

        protected override void ContextClickedItem(int id)
        {
            var item = FindItem(id, rootItem);

            if (item is HierarchyEntityGroupGraph)
            {
                this.ShowEntityGroupContextMenu((item as HierarchyEntityGroupGraph).Value.EntityGroupRef);
            }

            if (item is IEntityTreeItem)
            {
                this.ShowEntityContextMenu((item as IEntityTreeItem).EntityRef);
            }
            ContextClickedWithId = true;
        }

        protected override void ContextClicked()
        {
            if (!ContextClickedWithId)
            {
                this.ShowEntityGroupContextMenu(UTinyEntityGroup.Reference.None);
            }
            ContextClickedWithId = false;
        }

        protected override void DoubleClickedItem(int id)
        {
            var item = FindItem(id, rootItem);
            if (item is IEntityTreeItem)
            {
                SceneView.lastActiveSceneView?.FrameSelected();
            }
        }

        protected override void SelectionChanged(IList<int> selectedIds)
        {
            Selection.instanceIDs = selectedIds.ToArray();
        }

        protected override bool CanRename(TreeViewItem item)
        {
            return null != item as IEntityTreeItem;
        }

        protected override void RenameEnded(RenameEndedArgs args)
        {
            if (args.acceptedRename)
            {
                var item = FindItem(args.itemID, rootItem);
                var node = item as IEntityTreeItem;
                var entityRef = node.EntityRef;
                var entity = entityRef.Dereference(m_Context.Registry);
                entity.Name = args.newName;
                node.EntityRef = (UTinyEntity.Reference)entity;
                item.displayName = args.newName;
                UTinyInspector.RepaintAll();
            }
        }

        protected override bool CanStartDrag(CanStartDragArgs args)
        {
            bool canDrag = false;
            // You can only drag if all of the selected entities are either:
            //  1- static entities
            //  2- dynamic entities
            var entities = args.draggedItemIDs
                .Select(id => (FindItem(id, rootItem) as HierarchyEntity))
                .Where(e => null != e)
                .Select(e => e.Value);

            canDrag |= entities.All(e => !e.Entity.Dereference(Registry).HasTransform());
            canDrag |= entities.All(e =>  e.Entity.Dereference(Registry).HasTransform());

            // Can only drag from an Entity
            canDrag &= args.draggedItem is IEntityTreeItem;

            return canDrag;
        }

        protected override void SetupDragAndDrop(SetupDragAndDropArgs args)
        {
            DragAndDrop.PrepareStartDrag();

            var sortedDraggedIDs = SortItemIDsInRowOrder(args.draggedItemIDs);

            List<GameObject> objList = new List<GameObject>(sortedDraggedIDs.Count);
            foreach (var id in sortedDraggedIDs)
            {
                var item = (FindItem(id, rootItem) as IEntityTreeItem);
                if (null != item)
                {
                    objList.Add(item.EntityRef.Dereference(m_Context.Registry).View.gameObject);
                }
            }

            DragAndDrop.objectReferences = objList.ToArray();

            DragAndDrop.StartDrag("Multiple");
        }

        protected override DragAndDropVisualMode HandleDragAndDrop(DragAndDropArgs args)
        {
            var draggedObjects = DragAndDrop.objectReferences;

            var nodes = draggedObjects
                .Select(d => FindItem(d.GetInstanceID(), rootItem))
                .OfType<IEntityTreeItem>()
                .Select(item => item.Node);

            var mixed = nodes.Any(e => e is IStatic) && nodes.Any(e => !(e is IStatic));

            if (args.performDrop)
            {
                if (HandleSingleObjectDrop<Sprite>(args, HandleResourceDropped) == DragAndDropVisualMode.Link)
                {
                    return DragAndDropVisualMode.Link;
                }
                if (HandleSingleObjectDrop<Texture2D>(args, HandleResourceDropped) == DragAndDropVisualMode.Link)
                {
                    return DragAndDropVisualMode.Link;
                }                
                if (HandleSingleObjectDrop<Font>(args, HandleResourceDropped) == DragAndDropVisualMode.Link)
                {
                    return DragAndDropVisualMode.Link;
                }
                if (HandleSingleObjectDrop<AudioClip>(args, HandleResourceDropped) == DragAndDropVisualMode.Link)
                {
                    return DragAndDropVisualMode.Link;
                }
                if (HandleSingleObjectDrop<AudioClip>(args, HandleResourceDropped) == DragAndDropVisualMode.Link)
                {
                    return DragAndDropVisualMode.Link;
                }

                if (args.dragAndDropPosition == DragAndDropPosition.UponItem)
                {
                    return HandleDropUponItem(nodes, args);
                }
                else if (args.dragAndDropPosition == DragAndDropPosition.BetweenItems)
                {
                    return HandleDropBetweenItems(nodes, args);
                }
                else if (args.dragAndDropPosition == DragAndDropPosition.OutsideItems)
                {
                    return DropOutsideOfItems(nodes, args);
                }
                return DragAndDropVisualMode.Rejected;
            }

            // In mix mode, we only support moving between scenes.
            if (mixed)
            {
                if (args.dragAndDropPosition == DragAndDropPosition.OutsideItems ||
                    (args.dragAndDropPosition == DragAndDropPosition.UponItem && args.parentItem is HierarchyEntityGroupGraph))
                {
                    return DragAndDropVisualMode.Move;
                }
                return DragAndDropVisualMode.Rejected;
            }

            return DragAndDropVisualMode.Move;
        }
        #endregion

        #region Implementation
        private void DrawItem(Rect rect, HierarchyEntityGroupGraph item, RowGUIArgs args)
        {
            if (null == item)
            {
                return;
            }

            var indent = GetContentIndent(item);
            if (!args.selected)
            {
                var headerRect = rect;
                headerRect.width += 1;

                var topLine = headerRect;
                topLine.height = 1;
                UTinyGUI.BackgroundColor(topLine, UTinyColors.Hierarchy.SceneSeparator);

                headerRect.y += 2;
                UTinyGUI.BackgroundColor(headerRect, UTinyColors.Hierarchy.SceneItem);
                
                var bottomLine = headerRect;
                bottomLine.y += bottomLine.height - 1;
                bottomLine.height = 1;
                UTinyGUI.BackgroundColor(bottomLine, UTinyColors.Hierarchy.SceneSeparator);
            }


            rect.y += 2;
            rect.x = indent;
            rect.width -= indent;

            var iconRect = rect;
            iconRect.width = 20;

            var image = ActiveScene.Equals(item.Value.EntityGroupRef) ? UTinyIcons.ActiveEntityGroup : UTinyIcons.EntityGroup;
            EditorGUI.LabelField(iconRect, new GUIContent { image = image });

            rect.x += 20;
            rect.width -= 40;

            item.displayName = item.Value.EntityGroupRef.Dereference(Registry).Name;
            var style = ActiveScene.Equals(item.Value.EntityGroupRef) ? EditorStyles.boldLabel : GUI.skin.label;
            EditorGUI.LabelField(rect, item.displayName, style);
            rect.x += rect.width;
            rect.width = 16;

            rect.y = rect.center.y - 5.5f;
            rect.height = 11;

            if (GUI.Button(rect, GUIContent.none, UTinyStyles.PaneOptionStyle))
            {
                HierarchyContextMenus.ShowEntityGroupContextMenu(this, item.Value.EntityGroupRef);
            }
        }

        private void DrawItem(Rect rect, IEntityTreeItem item, RowGUIArgs args)
        {
            var entity = item.EntityRef.Dereference(Registry);
            using (new GUIColorScope(item.Node.EnabledInHierarchy ? Color.white : UTinyColors.Hierarchy.Disabled))
            {
                base.RowGUI(args);
            }

            if (!entity.HasTransform())
            {
                rect.x = rect.xMax - 50.0f;
                rect.width = 50.0f;
                EditorGUI.LabelField(rect, "(static)");
            }
        }

        private void DrawItem(Rect rect, HierarchyTreeItemBase item, RowGUIArgs args)
        {
            var indent = GetContentIndent(item);
            rect.width -= indent + 8.0f;
            rect.x += indent;
            rect.height = 1;
            rect.y += 3;
            UTinyGUI.BackgroundColor(rect, Color.gray);
        }

        private void BuildFromNode(EntityNode node, HierarchyTreeItemBase parentItem)
        {
            var entity = node.Entity.Dereference(m_Context.Registry);
            var item = new HierarchyEntity
            {
                Value = node,
                id = GetInstanceId(node),
                depth = parentItem.depth + 1,
                displayName = entity.Name
            };

            if (entity.Name.IndexOf(FilterString, StringComparison.OrdinalIgnoreCase) >= 0)
            {
                parentItem.AddChild(item);
            }

            foreach (var child in node.Children)
            {
                if (string.IsNullOrEmpty(m_FilterString))
                {
                    BuildFromNode(child, item);
                }
                else
                {
                    BuildFromNode(child, parentItem);
                }
            }
        }

        private void BuildFromNode(StaticEntityNode node, HierarchyTreeItemBase parentItem)
        {
            var entity = node.Entity.Dereference(m_Context.Registry);

            if (entity.Name.IndexOf(FilterString, StringComparison.OrdinalIgnoreCase) < 0)
            {
                return;
            }

            var item = new HierarchyStaticEntity
            {
                Value = node,
                id = GetInstanceId(node),
                depth = parentItem.depth + 1,
                displayName = entity.Name
            };

            parentItem.AddChild(item);
        }

        private DragAndDropVisualMode HandleDropUponItem(IEnumerable<IEntityNode> entities, DragAndDropArgs args)
        {
            var parentItem = args.parentItem as HierarchyTreeItemBase;
            DropOnItemAction method;
            if (DroppedOnMethod.TryGetValue(parentItem.GetType(), out method))
            {
                method(parentItem, entities);
                var ids = AsInstanceIds(entities);
                Selection.instanceIDs = ids;
                IdsToExpand = ids;
                UTinyHierarchyWindow.InvalidateSceneGraph();
                return DragAndDropVisualMode.Link;
            }
            return DragAndDropVisualMode.Rejected;
        }

        private DragAndDropVisualMode HandleDropBetweenItems(IEnumerable<IEntityNode> entities, DragAndDropArgs args)
        {
            var parentItem = args.parentItem as HierarchyTreeItemBase;
            DropBetweenAction method;
            if (DroppedBetweenMethod.TryGetValue(parentItem.GetType(), out method))
            {
                method(parentItem, entities, args.insertAtIndex);
                var ids = AsInstanceIds(entities);
                Selection.instanceIDs = ids;
                IdsToExpand = ids;
                UTinyHierarchyWindow.InvalidateSceneGraph();
                return DragAndDropVisualMode.Link;
            }
            return DragAndDropVisualMode.Rejected;
        }

        private DragAndDropVisualMode HandleResourceDropped(Object obj, DragAndDropArgs args)
        {
            var parent = args.parentItem as HierarchyTreeItemBase;
            EntityNode entityNode = null;
            switch (args.dragAndDropPosition)
            {
                case DragAndDropPosition.UponItem:
                    {

                        if (parent is HierarchyEntityGroupGraph)
                        {
                            var graph = (parent as HierarchyEntityGroupGraph).Value;
                            entityNode = graph.Create();
                        }

                        if (parent is HierarchyEntity)
                        {
                            var node = (parent as HierarchyEntity).Value;
                            entityNode = node.Graph.Create(node);
                        }
                        // Set as last non-static sibling.
                        else if (parent is HierarchyStaticEntity)
                        {
                            var node = (parent as HierarchyStaticEntity).Value;
                            entityNode = node.Graph.Create();
                        }
                    }
                    break;
                case DragAndDropPosition.BetweenItems:
                    {
                        if (rootItem == parent)
                        {
                            if (args.insertAtIndex <= 0)
                            {
                                return DragAndDropVisualMode.Rejected;
                            }

                            var graph = (parent.children[args.insertAtIndex - 1] as HierarchyEntityGroupGraph).Value;
                            entityNode = graph.Create();
                        }
                        else if (parent is HierarchyEntityGroupGraph)
                        {
                            var groupItem = parent as HierarchyEntityGroupGraph;
                            var graph = groupItem.Value;

                            var index = (args.insertAtIndex >= parent.children.Count || args.insertAtIndex >= graph.Roots.Count) ? -1 : args.insertAtIndex;

                            entityNode = graph.Create();
                            graph.Insert(index, entityNode);
                        }
                        else if (parent is HierarchyEntity)
                        {
                            var parentNode = (parent as HierarchyEntity).Value;
                            var firstIndex = args.insertAtIndex;
                            entityNode = parentNode.Graph.Create();
                            entityNode.SetParent(firstIndex, parentNode);
                        }
                        // Between static entities, set as last sibling of non-static entities
                        else if (parent is HierarchyStaticEntity)
                        {
                            var graph = (parent as HierarchyStaticEntity).Value.Graph;
                            entityNode = graph.Create();
                        }
                    }
                    break;
                case DragAndDropPosition.OutsideItems:
                    {
                        var graph = UTinyHierarchyWindow.GetSceneGraph(m_EntityGroups.Last());
                        entityNode = graph.Create();
                    }
                    break;
                default:
                    {
                        return DragAndDropVisualMode.Rejected;
                    }
            }


            if (!UTinyEntity.Reference.None.Equals(entityNode.Entity))
            {
                AddToEntity(entityNode.Entity, obj);
                var ids = AsInstanceIds(entityNode);
                Selection.instanceIDs = ids;
                IdsToExpand = ids;
                return DragAndDropVisualMode.Link;
            }
            return DragAndDropVisualMode.Rejected;
        }

        private DragAndDropVisualMode HandleSingleObjectDrop<T>(DragAndDropArgs args, System.Func<T, DragAndDropArgs, DragAndDropVisualMode> action) where T : Object
        {
            var draggedObjects = DragAndDrop.objectReferences;
            if (draggedObjects.Length > 1)
            {
                return DragAndDropVisualMode.Rejected;
            }
            var objects = draggedObjects
               .Select(d => d as T)
               .Where(s => null != s).ToList();

            if (objects.Count == 1)
            {
                return action(objects[0], args);
            }

            return DragAndDropVisualMode.Rejected;
        }

        private bool AddToEntity(UTinyEntity.Reference entity, Object obj, bool runBindings = true)
        {
            if (obj is Texture2D)
            {
                var texture = (Texture2D) obj;
                var path = AssetDatabase.GetAssetPath(texture);
                var sprite = AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().FirstOrDefault();
                if (null == sprite)
                {
                    Debug.LogWarning($"{UTinyConstants.ApplicationName}: Only Sprites are supported in {UTinyConstants.ApplicationName}.");
                }
                return AddToEntity(entity, sprite, runBindings);
            }
            else if (obj is Sprite)
            {
                return AddToEntity(entity, obj as Sprite, runBindings);
            }
            else if (obj is AudioClip)
            {
                return AddToEntity(entity, obj as AudioClip, runBindings);
            }
            else if (obj is Font)
            {
                return AddToEntity(entity, obj as Font, runBindings);
            }
            return false;
        }

        private bool AddToEntity(UTinyEntity.Reference entityRef, Texture2D texture, bool runBindings)
        {
            var entity = entityRef.Dereference(m_Context.Registry);
            entity.Name = texture.name;

            var image2d = entity.GetOrAddComponent(Registry.GetImage2DType());
            image2d.Refresh();
            image2d["imageFile"] = texture;
            
            // @TODO Pull from texture importer
            image2d["pixelsToWorldUnits"] = 1.0f;

            if (runBindings)
            {
                BindingsHelper.RunAllBindings(entity);
            }
            UTinyHierarchyWindow.InvalidateSceneGraph();
            return true;
        }

        private bool AddToEntity(UTinyEntity.Reference entityRef, Sprite sprite, bool runBindings)
        {
            var entity = entityRef.Dereference(m_Context.Registry);
            entity.Name = sprite?.name ?? "NullSprite";

            var renderer = entity.GetOrAddComponent(Registry.GetSprite2DRendererType());
            renderer["sprite"] = sprite;

            if (runBindings)
            {
                BindingsHelper.RunAllBindings(entity);
            }
            
            UTinyHierarchyWindow.InvalidateSceneGraph();
            return true;
        }

        private bool AddToEntity(UTinyEntity.Reference entityRef, AudioClip audioClip, bool runBindings)
        {
            var entity = entityRef.Dereference(m_Context.Registry);
            entity.Name = audioClip.name;

            var audioSource = entity.GetOrAddComponent(Registry.GetAudioSourceType());
            audioSource.Refresh();
            audioSource["clip"] = audioClip;
            audioSource["volume"] = 1.0f;

            if (runBindings)
            {
                BindingsHelper.RunAllBindings(entity);
            }
            UTinyHierarchyWindow.InvalidateSceneGraph();
            return true;
        }
        
        private bool AddToEntity(UTinyEntity.Reference entityRef, Font font, bool runBindings)
        {
            var entity = entityRef.Dereference(m_Context.Registry);
            entity.Name = font.name;

            var renderer = entity.GetOrAddComponent(Registry.GetTextRendererType());
            renderer["text"] = "Sample Text";
            renderer["font"] = font;

            if (runBindings)
            {
                BindingsHelper.RunAllBindings(entity);
            }
            
            UTinyHierarchyWindow.InvalidateSceneGraph();
            return true;
        }

        private void DropUponSceneItem(HierarchyTreeItemBase parent, IEnumerable<IEntityNode> entities)
        {
            var graph = (parent as HierarchyEntityGroupGraph).Value;
            foreach (var node in entities)
            {
                graph.Add(node);
            }
            var ids = AsInstanceIds(entities);
            Selection.instanceIDs = ids;
            IdsToExpand = ids;
        }

        private void DropUponEntityItem(HierarchyTreeItemBase parent, IEnumerable<IEntityNode> entities)
        {
            var item = (parent as IEntityTreeItem);
            var parentNode = item.Node as EntityNode;
            var graph = item.Graph;

            var staticEntities = entities.OfType<StaticEntityNode>().ToList();
            graph.Insert(0, staticEntities);

            var nonStaticEntities = entities.OfType<EntityNode>().ToList();
            graph.Insert(-1, nonStaticEntities, parentNode);
        }

        private void DropUponStaticEntityItem(HierarchyTreeItemBase parent, IEnumerable<IEntityNode> entities)
        {
            var item = (parent as IEntityTreeItem);
            var parentNode = item.Node as StaticEntityNode;
            var graph = item.Graph;

            var staticEntities = entities.OfType<StaticEntityNode>().Where(node => node != parentNode).ToList();
            graph.Insert(item.Node.SiblingIndex() + 1, staticEntities);

            var NonStaticEntities = entities.OfType<EntityNode>().ToList();
            graph.Add(NonStaticEntities);
        }

        private DragAndDropVisualMode DropOutsideOfItems(IEnumerable<IEntityNode> entities, DragAndDropArgs args)
        {
            var graph = UTinyHierarchyWindow.GetSceneGraph(m_EntityGroups.Last());
            graph.Add(entities.ToList());

            var ids = AsInstanceIds(entities);
            Selection.instanceIDs = ids;
            IdsToExpand = ids;
            UTinyHierarchyWindow.InvalidateSceneGraph();
            return DragAndDropVisualMode.Link;
        }

        private void DropBetweenEntityGroupItems(HierarchyTreeItemBase parent, IEnumerable<IEntityNode> entities, int insertAtIndex)
        {
            // Can't add entities before the first group.
            if (insertAtIndex <= 0)
            {
                return;
            }

            var graph = (parent.children[insertAtIndex - 1] as HierarchyEntityGroupGraph).Value;
            graph.Add(entities.ToList());
        }

        private void DropBetweenRootEntities(HierarchyTreeItemBase parent, IEnumerable<IEntityNode> entities, int insertAtIndex)
        {
            var item = parent as HierarchyEntityGroupGraph;
            var graph = item.Value;

            {
                var staticEntities = entities.OfType<StaticEntityNode>().ToList();
                if (insertAtIndex < item.children.Count)
                {
                    var current = item.children[insertAtIndex];
                    var firstIndex = 0;
                    if (current is HierarchyStaticEntity)
                    {
                        // +1 is for the static separator object.
                        firstIndex = insertAtIndex - (graph.Roots.Count + 1);
                    }

                    foreach (var node in staticEntities)
                    {
                        if (graph.IsRoot(node) && node.SiblingIndex() < firstIndex)
                        {
                            firstIndex -= 1;
                        }
                        graph.Insert(firstIndex++, node);
                    }
                }
                else
                {
                    graph.Add(staticEntities);
                }
            }

            {
                var nonStaticEntities = entities.OfType<EntityNode>().ToList();
                var firstIndex = insertAtIndex;
                foreach (var node in nonStaticEntities)
                {
                    if (graph.IsRoot(node) && node.SiblingIndex() < firstIndex)
                    {
                        firstIndex -= 1;
                    }
                    graph.Insert(firstIndex++, node);
                }
            }
        }

        private void DropBetweenChildrenEntities(HierarchyTreeItemBase parent, IEnumerable<IEntityNode> entities, int insertAtIndex)
        {
            {
                var staticEntities = entities.OfType<StaticEntityNode>().ToList();
                var graph = (parent as HierarchyEntity).Graph;
                graph.Insert(0, staticEntities);
            }

            {
                var nonStaticEntities = entities.OfType<EntityNode>().ToList();
                var entityNode = (parent as HierarchyEntity).Value;
                var firstIndex = insertAtIndex;
                foreach (var node in nonStaticEntities)
                {
                    if (node.IsChildrenOf(entityNode) && node.SiblingIndex() < firstIndex)
                    {
                        firstIndex -= 1;
                    }
                    entityNode.Insert(firstIndex++, node);
                }
            }
        }

        private void DropBetweenStaticEntities(HierarchyTreeItemBase parent, IEnumerable<IEntityNode> entities, int insertAtIndex)
        {
            var graph = (parent as HierarchyStaticEntity).Value.Graph;
            graph.Add(entities.OfType<EntityNode>().ToList());
        }


        private int[] AsInstanceIds(IEntityNode entity)
        {
            return new int[] { entity.Entity.Dereference(Registry).View.gameObject.GetInstanceID() };
        }

        private int[] AsInstanceIds(IEnumerable<IEntityNode> entities)
        {
            return entities
                .Select(node => node.Entity.Dereference(Registry))
                .Where(e => null != e)
                .Select(e => e.View.gameObject.GetInstanceID())
                .ToArray();
        }

        private void ForceExpanded(IList<int> ids)
        {
            foreach(var id in ids)
            {
                foreach (var ancestorId in GetAncestors(id))
                {
                    SetExpanded(ancestorId, true);
                }
                SetExpanded(id, true);
            }
        }
        #endregion
    }
}
#endif // NET_4_6
