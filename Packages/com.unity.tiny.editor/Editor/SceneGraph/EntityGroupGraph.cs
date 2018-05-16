#if NET_4_6
using System.Linq;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Assertions;

using Unity.Properties;

using Unity.Tiny.Attributes;
using Unity.Tiny.Extensions;
using Unity.Tiny.Filters;
using static Unity.Tiny.Attributes.EditorInspectorAttributes;

namespace Unity.Tiny
{
    public class EntityGroupGraph
    {
        #region Static

        public static EntityGroupGraph CreateFromEntityGroup(UTinyEntityGroup entityGroup)
        {
            var graph = new EntityGroupGraph(entityGroup);
            graph.SyncFromUTiny();
            foreach (var entity in entityGroup.Entities.Select(entityRef => entityRef.Dereference(entityGroup.Registry)))
            {
                if (null == entity)
                {
                    continue;
                }
                entity.EntityGroup = entityGroup;
                graph.CreateLink(entity);
            }

            graph.CommitToUnity();

            foreach (var entity in entityGroup.Entities.Select(entityRef => entityRef.Dereference(entityGroup.Registry)))
            {
                if (null == entity)
                {
                    continue;
                }
                BindingsHelper.RunAllBindings(entity);
            }

            return graph;
        }

        #endregion

        #region Fields and Properties

        public UTinyEntityGroup.Reference EntityGroupRef { get; }
        private IRegistry Registry { get; }

        public readonly List<EntityNode> Roots = new List<EntityNode>();
        public readonly List<StaticEntityNode> StaticEntities = new List<StaticEntityNode>();

        #endregion

        #region API

        private EntityGroupGraph(UTinyEntityGroup entityGroup)
        {
            EntityGroupRef = (UTinyEntityGroup.Reference) entityGroup;
            Registry = entityGroup.Registry;
        }

        public IEntityNode FindNode(UTinyEntity.Reference entity)
        {
            if (entity.Dereference(Registry).HasTransform())
            {
                return Roots.Select(r => FindRecursive(r, entity)).NotNull().FirstOrDefault();
            }
            else
            {
                return StaticEntities.Find(n => n.Entity.Equals(entity));
            }
        }

        public void Add(List<EntityNode> nodes)
        {
            foreach (var node in nodes)
            {
                Add(node);
            }
        }

        public void Add(EntityNode node)
        {
            Insert(-1, node, null);
        }

        public void Add(EntityNode node, EntityNode parent)
        {
            Insert(-1, node, parent);
        }

        public void Add(List<StaticEntityNode> nodes)
        {
            foreach (var node in nodes)
            {
                Add(node);
            }
        }

        public void Add(StaticEntityNode node)
        {
            Insert(-1, node);
        }

        public void Add(List<IEntityNode> nodes)
        {
            foreach(var node in nodes)
            {
                Add(node);
            }
        }

        public void Add(IEntityNode node)
        {
            var entityNode = node as EntityNode;
            if (entityNode != null)
            {
                Add(entityNode);
            }
            else
            {
                var staticEntityNode = node as StaticEntityNode;
                if (staticEntityNode != null)
                {
                    Add(staticEntityNode);
                }
            }
        }

        public void Insert(int siblingIndex, List<EntityNode> nodes, EntityNode parent = null)
        {
            foreach (var node in nodes)
            {
                if (node.IsAncestorOrParentOf(parent))
                {
                    continue;
                }
                Insert(siblingIndex, node, parent);
                siblingIndex += siblingIndex < 0 ? 0 : 1;
            }
        }

        public void Insert(int siblingIndex, EntityNode node, EntityNode parent = null)
        {
            if (node.IsAncestorOrParentOf(parent))
            {
                return;
            }

            if (this != node.Graph)
            {
                // Remove from old Graph
                node.Graph.RemoveFromGraph(node.Graph.Roots, node);
                SetAsCurrent(node);
            }

            // Remove from previous parent
            if (null != node.Parent)
            {
                node.Parent.Children.Remove(node);
            }
            else
            {
                Roots.Remove(node);
            }

            node.Parent = parent;
            UpdateTransform(node, parent);
            if (null == node.Parent)
            {
                // Add as root
                InsertInList(siblingIndex, Roots, node);
            }
            else
            {
                InsertInList(siblingIndex, node.Parent.Children, node);
            }
        }

        private void UpdateTransform(EntityNode node, EntityNode parent)
        {
            var entity = node.Entity.Dereference(Registry);
            var transform = entity.View.transform;
            var tinyTransform = entity.GetComponent(Registry.GetTransformType());
            if (null == parent)
            {
                transform.SetParent(null, true);
            }
            else
            {
                transform.SetParent(parent.Entity.Dereference(Registry).View.transform, true);
            }
            TransformInversedBindings.SyncTransform(transform, tinyTransform);
        }

        public void Insert(int siblingIndex, List<StaticEntityNode> nodes)
        {
            foreach(var node in nodes)
            {
                Insert(siblingIndex++, node);
            }
        }

        public void Insert(int siblingIndex, StaticEntityNode node)
        {
            if (this != node.Graph)
            {
                // Remove from old Graph
                node.Graph.RemoveFromGraph(node.Graph.StaticEntities, node);
                SetAsCurrent(node);
            }

            StaticEntities.Remove(node);
            if (siblingIndex < 0 || siblingIndex >= StaticEntities.Count)
            {
                StaticEntities.Add(node);
            }
            else
            {
                StaticEntities.Insert(siblingIndex, node);
            }
        }

        public void Delete(IEntityNode node)
        {
            var entityNode = node as EntityNode;
            if (entityNode != null)
            {
                Delete(entityNode);
            }
            else
            {
                var staticEntityNode = node as StaticEntityNode;
                if (staticEntityNode != null)
                {
                    Delete(staticEntityNode);
                }
            }
        }

        public void Delete(EntityNode node)
        {
            Assert.IsTrue(this == node.Graph);
            RemoveFromGraph(Roots, node, true);
            Unregister(node);
        }

        public void Delete(StaticEntityNode node)
        {
            Assert.IsTrue(this == node.Graph);
            RemoveFromGraph(StaticEntities, node, true);
            Unregister(node);
        }

        public EntityNode CreateFromExisting(Transform t, Transform parent)
        {
            var entity = Registry.CreateEntity(
                new UTinyId(System.Guid.NewGuid()),
                t.name);

            var entityRef = (UTinyEntity.Reference) entity;
            var entityGroupRef = EntityGroupRef.Dereference(Registry);
            Assert.IsNotNull(entityGroupRef);
            entity.EntityGroup = entityGroupRef;
            entityGroupRef.AddEntityReference(entityRef);
            
            entity.AddComponent(Registry.GetTransformType());
            entity.View = t.GetComponent<UTinyEntityView>();

            CreateLink(entity);
            var node = new EntityNode {Entity = entityRef, Graph = this};
            node.Graph.Add(node);

            if (parent)
            {
                var parentview = parent.GetComponent<UTinyEntityView>();
                Assert.IsNotNull(parentview);
                var parentNode = FindNode((UTinyEntity.Reference) parentview.EntityRef.Dereference(parentview.Registry));
                node.SetParent(parentNode as EntityNode);
            }

            return node;
        }
        
        public EntityNode Create(EntityNode parent = null)
        {
            Assert.IsTrue(null == parent || this == parent.Graph);

            var entity = Registry.CreateEntity(
                new UTinyId(System.Guid.NewGuid()),
                GetUniqueName((parent?.Children.Select(c => c.Entity) ?? Roots.Select(r => r.Entity)), "Entity"));

            var entityRef = (UTinyEntity.Reference) entity;

            var entityGroup = EntityGroupRef.Dereference(Registry);
            Assert.IsNotNull(entityGroup);
            entity.EntityGroup = entityGroup;
            entityGroup.AddEntityReference(entityRef);

            entity.AddComponent(Registry.GetTransformType());
            var view = CreateLink(entity);
            if (null != parent)
            {
                view.transform.SetParent(parent.Entity.Dereference(Registry).View.transform, false);
            }

            var node = new EntityNode {Entity = entityRef, Graph = this};
            node.Graph.Add(node, parent);
            return node;
        }

        public IEntityNode CreateStatic()
        {
            var entity = Registry.CreateEntity(new UTinyId(System.Guid.NewGuid()), GetUniqueName(StaticEntities.Select(e => e.Entity), "Entity"));
            var entityRef = (UTinyEntity.Reference) entity;
            
            CreateLink(entity);

            var entityGroup = EntityGroupRef.Dereference(Registry);
            Assert.IsNotNull(entityGroup);
            entityGroup.AddEntityReference(entityRef);

            var node = new StaticEntityNode {Entity = entityRef, Graph = this};
            node.Graph.Add(node);

            return node;
        }

        public List<IEntityNode> Create(List<IEntityNode> candidates)
        {
            var result = new List<IEntityNode>();
            foreach (var node in candidates.Where(node => node is StaticEntityNode || !IsChildOfAny(node as EntityNode, candidates)))
            {
                if (node is StaticEntityNode)
                {
                    result.Add(CreateStatic());
                    continue;
                }

                result.Add(Create(node as EntityNode));
            }

            return result;
        }

        public List<IEntityNode> Duplicate(List<IEntityNode> candidates)
        {
            return candidates.Where(node => node is StaticEntityNode || !IsChildOfAny(node as EntityNode, candidates)).Select(Duplicate).ToList();
        }

        public bool IsRoot(IEntityNode node)
        {
            if (node is EntityNode)
            {
                return Roots.Contains(node);
                
            }
            return StaticEntities.Contains(node);
        }

        public void Unlink()
        {
            var entityRefs = Pooling.ListPool<UTinyEntity.Reference>.Get();
            try
            {
                GetOrderedEntityList(entityRefs);
                foreach (var entityRef in entityRefs)
                {
                    var entity = entityRef.Dereference(Registry);
                    if (null == entity)
                    {
                        continue;
                    }

                    DeleteLink(entity);
                }
            }
            finally
            {
                Pooling.ListPool<UTinyEntity.Reference>.Release(entityRefs);
            }
        }

        #endregion

        #region

        public void SyncFromUTiny()
        {
            Roots.Clear();
            StaticEntities.Clear();

            var cache = new Dictionary<UTinyEntity, EntityNode>();

            var entityGroup = EntityGroupRef.Dereference(Registry);
            Assert.IsNotNull(entityGroup);

            foreach (var entityRef in entityGroup.Entities)
            {
                var entity = entityRef.Dereference(Registry);

                if (null == entity)
                {
                    Debug.LogWarning($"SceneGraph failed to load entity Name=[{entityRef.Name}] Id=[{entityRef.Id}]");
                    continue;
                }

                if (entity.HasTransform())
                {
                    var node = new EntityNode() {Entity = entityRef, Graph = this};
                    cache[entity] = node;

                    EntityNode parentNode = null;
                    var parent = entity.Parent();
                    if (UTinyEntity.Reference.None.Id == parent.Id)
                    {
                        Roots.Add(node);
                        continue;
                    }

                    if (cache.TryGetValue(parent.Dereference(Registry), out parentNode))
                    {
                        node.Parent = parentNode;
                        parentNode.Children.Add(node);
                    }
                }
                else
                {
                    StaticEntities.Add(new StaticEntityNode {Entity = entityRef, Graph = this});
                }
            }
        }

        public void CommitChanges()
        {
            CommitToUTiny();
            CommitToUnity();
        }

        #endregion

        #region Implementation

        private void CommitToUTiny()
        {
            var entityRefs = Pooling.ListPool<UTinyEntity.Reference>.Get();
            try
            {
                GetOrderedEntityList(entityRefs);

                var entityGroup = EntityGroupRef.Dereference(Registry);
                Assert.IsNotNull(entityGroup);
                entityGroup.ClearEntityReferences();
                foreach (var entityRef in entityRefs)
                {
                    entityGroup.AddEntityReference(entityRef);
                }
            }
            finally
            {
                Pooling.ListPool<UTinyEntity.Reference>.Release(entityRefs);
            }
        }

        private void CommitToUnity()
        {
            var entityGroup = UTinyEditorApplication.EntityGroupManager.UnityScratchPad;
            if (!entityGroup.isLoaded || !entityGroup.IsValid())
            {
                return;
            }

            var cache = new HashSet<UTinyEntity.Reference>();

            foreach (var root in Roots)
            {
                var entityRef = root.Entity;
                var entity = entityRef.Dereference(Registry);
                CreateLink(entity);
                var view = entity.View;
                view.gameObject.name = entity.Name;
                cache.Add(entityRef);
                view.transform.SetParent(null, true);
                view.transform.SetAsLastSibling();
                PopulateChildren(root, cache);
            }

            foreach (var root in StaticEntities)
            {
                var entityRef = root.Entity;
                var entity = entityRef.Dereference(Registry);
                CreateLink(entity);
                var view = entity.View;
                view.gameObject.name = entity.Name;
                cache.Add(entityRef);
                view.transform.SetParent(null, true);
                view.transform.SetAsLastSibling();
                if (view.transform.childCount == 0)
                {
                    continue;
                }

                for (var j = view.transform.childCount - 1; j >= 0; --j)
                {
                    view.transform.GetChild(j).SetParent(null, true);
                }
            }

            // Run the inversed bindings
            UpdateMeshes();
        }

        private string GetUniqueName(IEnumerable<UTinyEntity.Reference> elements, string name)
        {
            var digits = name.Reverse().TakeWhile(c => char.IsDigit(c)).Reverse().ToArray();
            var baseName = name.Substring(0, name.Length - digits.Length);
            var next = baseName;
            var index = 1;

            while (true)
            {
                if (elements.Deref(Registry).All(element => !string.Equals(element.Name, next)))
                {
                    return next;
                }

                next = $"{baseName}{index++}";
            }
        }

        private UTinyEntityView CreateLink(UTinyEntity entity)
        {
            try
            {
                var entityRef = (UTinyEntity.Reference)entity;
                if (null != entity.View && entity.View)
                {
                    entity.View.EntityRef = entityRef;
                    entity.View.Registry = entity.Registry;
                    entity.View.gameObject.SetActive(entity.Enabled);
                    entity.View.gameObject.layer = entity.Layer;
                    return null;
                }

                // We may have recreated the entity, try to find an active view
                {
                    var scene = UTinyEditorApplication.EntityGroupManager.UnityScratchPad;
                    foreach (var r in scene.GetRootGameObjects())
                    {
                        foreach (var v in r.GetComponentsInChildren<UTinyEntityView>())
                        {
                            if (v.EntityRef.Equals(entityRef))
                            {
                                entity.View = v;
                                entity.View.EntityRef = entityRef;
                                entity.View.Registry = entity.Registry;
                                v.gameObject.SetActive(entity.Enabled);
                                v.gameObject.layer = entity.Layer;
                                return null;
                            }
                        }
                    }
                }

                var go = new GameObject(entity.Name);
                var view = go.AddComponent<UTinyEntityView>();
                view.gameObject.SetActive(entity.Enabled);
                view.gameObject.layer = entity.Layer;
                view.EntityRef = entityRef;
                view.Registry = entity.Registry;
                entity.View = view;

                return view;
            }
            finally
            {
                // At this point, it is not clear if the bindings have been added or not (we may have undo-ed something).
                entity.OnComponentAdded -= HandleComponentAdded;
                entity.OnComponentRemoved -= HandleComponentRemoved;
                entity.OnComponentAdded += HandleComponentAdded;
                entity.OnComponentRemoved += HandleComponentRemoved;
                BindingsHelper.RunAllBindings(entity);
            }
        }

        private void DeleteLink(UTinyEntity entity)
        {
            entity.OnComponentAdded -= HandleComponentAdded;
            entity.OnComponentRemoved -= HandleComponentRemoved;

            var view = entity.View;
            if (null != view && view && entity.View.gameObject)
            {
                Object.DestroyImmediate(entity.View.gameObject, false);
            }

            entity.View = null;
        }

        private void HandleComponentAdded(UTinyEntity entity, UTinyObject component)
        {
            component.Refresh();
            var type = component.Type.Dereference(Registry);
            if (type.HasAttribute<BindingsAttribute>())
            {
                var bindings = type.GetAttribute<BindingsAttribute>().Binding;
                
                // Invoke bindings to register and add `unity` components
                bindings.Run(BindingTiming.OnAddBindings, entity, component);
                bindings.Run(BindingTiming.OnUpdateBindings, entity, component);
                
                // Invoke callback to perform first time setup hook
                bindings.Run(BindingTiming.OnAddComponent, entity, component);
            }
        }

        private void HandleComponentRemoved(UTinyEntity entity, UTinyObject component)
        {
            component.Refresh();
            var type = component.Type.Dereference(Registry);
            if (type.HasAttribute<BindingsAttribute>())
            {
                var bindings = type.GetAttribute<BindingsAttribute>().Binding;
                
                // Invoke callback to perform teardown hook
                bindings.Run(BindingTiming.OnRemoveComponent, entity, component);
                
                // Invoke binding to unregister and remove `unity` components
                bindings.Run(BindingTiming.OnRemoveBindings, entity, component);
            }
        }

        private IEntityNode Duplicate(IEntityNode node)
        {
            var duplicated = CreateNodes(node);

            Copy(node, duplicated);

            var sourceEntities = GetSubNodes(node).Select(n => n.Entity.Dereference(Registry)).ToList();
            var duplicatedEntities = GetSubNodes(duplicated).Select(n => n.Entity.Dereference(Registry)).ToList();
            foreach (var property in duplicatedEntities.SelectMany(e => e.Components))
            {
                Rebind(property, sourceEntities, duplicatedEntities);
            }

            return duplicated;
        }

        private IEntityNode CreateNodes(IEntityNode source)
        {
            var sourceNode = source as EntityNode;
            var sourceEntity = source.Entity.Dereference(Registry);
            if (sourceNode != null)
            {
                var node = CreateNodes(sourceNode, sourceNode.Parent);
                node.Entity.Dereference(Registry).Name = GetUniqueName((sourceNode.Parent?.Children.Select(n => n.Entity) ?? Roots.Select(n => n.Entity)), sourceEntity.Name);
                return node;
            }
            else
            {
                var node = CreateStatic();
                var entity = node.Entity.Dereference(Registry);
                entity.Name = GetUniqueName(StaticEntities.Select(n => n.Entity), sourceEntity.Name);
                entity.Layer = sourceEntity.Layer;
                entity.Enabled = sourceEntity.Enabled;
                return node;
            }
        }

        private EntityNode CreateNodes(EntityNode source, EntityNode parent)
        {
            var node = Create(parent);
            var entity = node.Entity.Dereference(Registry);
            var sourceEntity = source.Entity.Dereference(Registry);
            entity.Name = sourceEntity.Name;
            entity.Layer = sourceEntity.Layer;
            entity.Enabled = sourceEntity.Enabled;

            foreach (var child in source.Children)
            {
                CreateNodes(child, node);
            }

            return node;
        }

        private void Copy(IEntityNode from, IEntityNode to)
        {
            var source = from.Entity.Dereference(Registry);
            var duplicated = to.Entity.Dereference(Registry);

            foreach (var sourceComponent in source.Components)
            {
                sourceComponent.Refresh();

                var typeRef = sourceComponent.Type;
                // There might be some automatic bindings that will add the component, so check if it is already present.
                var component = duplicated.GetOrAddComponent(typeRef);
                component.Refresh();
                component.CopyFrom(sourceComponent);
            }

            var fromRecursive = from as EntityNode;
            if (fromRecursive != null)
            {
                var toRecursive = to as EntityNode;
                if (toRecursive != null)
                {
                    for (var i = 0; i < fromRecursive.Children.Count; ++i)
                    {
                        Copy(fromRecursive.Children[i], toRecursive.Children[i]);
                    }
                }
            }
        }

        private void Rebind(UTinyObject property, List<UTinyEntity> source, List<UTinyEntity> duplicate)
        {
            var typeCode = property.Type.Dereference(Registry).TypeCode;

            if (typeCode != UTinyTypeCode.Struct && typeCode != UTinyTypeCode.Component)
            {
                return;
            }

            foreach (var value in property.Properties.PropertyBag.Properties)
            {
                var container = property.Properties as IPropertyContainer;

                // Component or Struct, look in their properties.
                if (value.ValueType == typeof(UTinyObject))
                {
                    var tinyObject = (UTinyObject)value.GetObjectValue(container);
                    Rebind(tinyObject, source, duplicate);
                    continue;
                }

                // Non-array Entity reference
                if (value.ValueType == typeof(UTinyEntity.Reference))
                {
                    var entityRef = (UTinyEntity.Reference)value.GetObjectValue(container);
                    var index = source.IndexOf(entityRef.Dereference(Registry));
                    if (index >= 0)
                    {
                        value.SetObjectValue(container, (UTinyEntity.Reference) duplicate[index]);
                    }

                    continue;
                }

                // Array of Struct or of Entity references
                if (value.ValueType == typeof(UTinyList))
                {
                    var list = (UTinyList)value.GetObjectValue(container);

                    for (var i = 0; i < list.Count; ++i)
                    {
                        var item = list[i];

                        if (item is UTinyObject)
                        {
                            Rebind(item as UTinyObject, source, duplicate);
                        }
                        else if (item is UTinyEntity.Reference)
                        {
                            var entityRef = (UTinyEntity.Reference) item;
                            var index = source.IndexOf(entityRef.Dereference(Registry));
                            if (index >= 0)
                            {
                                list[i] = (UTinyEntity.Reference) duplicate[index];
                            }
                        }
                    }
                }
            }
        }

        private static bool IsChildOfAny(EntityNode child, List<IEntityNode> candidates)
        {
            var parents = new List<EntityNode>();
            var parent = child.Parent;

            while (null != parent)
            {
                parents.Add(parent);
                parent = parent.Parent;
            }

            return parents.Intersect(candidates.Where(c => c is EntityNode).Cast<EntityNode>()).Any();
        }

        private static void InsertInList(int index, List<EntityNode> list, EntityNode node)
        {
            list.Remove(node);
            if (index < 0 || index >= list.Count)
            {
                list.Add(node);
            }
            else
            {
                list.Insert(index, node);
            }
        }

        private void SetAsCurrent(EntityNode node)
        {
            node.Graph = this;
            foreach (var child in node.Children)
            {
                SetAsCurrent(child);
            }
        }

        private void SetAsCurrent(StaticEntityNode node)
        {
            node.Graph = this;
            var entity = node.Entity.Dereference(Registry);
            entity.EntityGroup = node.Graph.EntityGroupRef.Dereference(Registry);
        }

        private void RemoveFromGraph(List<EntityNode> inspect, EntityNode toRemove, bool unlink = false)
        {
            if (inspect.Remove(toRemove))
            {
                if (unlink)
                {
                    DeleteLink(toRemove.Entity.Dereference(Registry));
                }
                return;
            }

            foreach (var node in inspect)
            {
                RemoveFromGraph(node.Children, toRemove, unlink);
            }
        }

        private void RemoveFromGraph(List<StaticEntityNode> inspect, StaticEntityNode toRemove, bool unlink = false)
        {
            if (inspect.Remove(toRemove))
            {
                if (unlink)
                {
                    DeleteLink(toRemove.Entity.Dereference(Registry));
                }
            }
        }

        private void StealFromGraph(List<StaticEntityNode> inspect, StaticEntityNode toRemove)
        {
            inspect.Remove(toRemove);
        }

        private void GetOrderedEntityList(List<UTinyEntity.Reference> list)
        {
            foreach (var root in Roots)
            {
                PopulateFromNode(root, list);
            }

            foreach (var node in StaticEntities)
            {
                PopulateFromNode(node, list);
            }
        }

        private void PopulateChildren(EntityNode parent, HashSet<UTinyEntity.Reference> cache)
        {
            for (int i = 0; i < parent.Children.Count; ++i)
            {
                var child = parent.Children[i];
                var entity = child.Entity.Dereference(Registry);
                CreateLink(entity);
                var view = entity.View;
                view.gameObject.name = child.Entity.Name;
                cache.Add(child.Entity);

                view.transform.SetParent(parent.Entity.Dereference(Registry).View.transform, true);
                view.transform.SetSiblingIndex(i);
                PopulateChildren(child, cache);
            }
        }

        private void PopulateFromNode(EntityNode node, List<UTinyEntity.Reference> list)
        {
            var entity = node.Entity.Dereference(Registry);
            list.Add(node.Entity);
            if (null == node.Parent)
            {
                entity.SetParent(UTinyEntity.Reference.None);
            }
            else
            {
                entity.SetParent(node.Parent.Entity);
            }

            foreach (var child in node.Children)
            {
                PopulateFromNode(child, list);
            }
        }

        private static void PopulateFromNode(StaticEntityNode node, List<UTinyEntity.Reference> list)
        {
            list.Add(node.Entity);
        }

        private static List<IEntityNode> GetSubNodes(IEntityNode node)
        {
            var result = new List<IEntityNode>();
            GetSubNodes(node, result);
            return result;
        }

        private static void GetSubNodes(IEntityNode node, List<IEntityNode> result)
        {
            result.Add(node);

            if (node is EntityNode)
            {
                foreach (var child in (node as EntityNode).Children)
                {
                    GetSubNodes(child, result);
                }
            }
        }

        private static void UpdateMeshes()
        {
            var repaint = false;
            var context = UTinyEditorApplication.EditorContext;
            if (null == context)
            {
                return;
            }

            // We want to make sure that all the renderers are "ordered" by hierarchy oder.
            var currentQueue = 3000;
            var scene = UTinyEditorApplication.EntityGroupManager.UnityScratchPad;
            if (scene.isLoaded && scene.IsValid())
            {
                foreach (var go in scene.GetRootGameObjects())
                {
                    foreach (var renderer in go.GetComponentsInChildren<Renderer>())
                    {
                        var r = currentQueue;
                        
                        if (renderer.gameObject.GetComponent<TextMesh>())
                        {
                            r += 2000;
                        }
                        
                        repaint |= renderer.sharedMaterial.renderQueue != r;
                        renderer.sharedMaterial.renderQueue = r;

                        currentQueue++;
                        
                        var view = go.GetComponent<UTinyEntityView>();
                        if (!view)
                        {
                            continue;
                        }

                        var entity = view.EntityRef.Dereference(context.Registry);

                        if (null != entity)
                        {
                            entity.View = view;
                            // Update mesh information.
                            BindingsHelper.RunBindings(entity, entity.GetComponent(context.Registry.GetSprite2DRendererType()));
                        }
                    }
                }
            }

            if (repaint)
            {
                SceneView.RepaintAll();
            }
        }
        
        private EntityNode FindRecursive(EntityNode node, UTinyEntity.Reference entity)
        {
            if (node.Entity.Equals(entity))
            {
                return node;
            }

            return node.Children.Select(r => FindRecursive(r, entity)).NotNull().FirstOrDefault();
        }

        private void Unregister(IEntityNode node)
        {
            Registry.Unregister(node.Entity.Id);
            (node as EntityNode)?.Children.ForEach(Unregister);
        }
        
        #endregion
    }
}
#endif // NET_4_6
