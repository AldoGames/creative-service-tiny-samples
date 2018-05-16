#if NET_4_6
using System.Collections.Generic;

namespace Unity.Tiny
{
    public class EntityNode : IEntityNode
    {
        #region Fields and Properties
        public EntityGroupGraph Graph { get; set; }
        public UTinyEntity.Reference Entity { get; set; }

        public EntityNode Parent { get; set; }

        public bool EnabledInHierarchy
        {
            get
            {
                var self = Entity.Dereference(UTinyEditorApplication.Registry)?.Enabled ?? false;
                return self && (Parent?.EnabledInHierarchy ?? true);
            }
        }

        public readonly List<EntityNode> Children = new List<EntityNode>();
        #endregion

        #region API
        public void SetParent(EntityNode parent)
        {
            SetParent(-1, parent);
        }

        public void SetParent(int siblingIndex, EntityNode parent)
        {
            // Cannot SetParent on a node that is part of the children.
            if (IsAncestorOrParentOf(parent))
            {
                return;
            }

            // We defer the operations to the owning graph, since the current node and the parent node may not be in the
            // same graph.
            if (null == parent)
            {
                Graph.Insert(siblingIndex, this);
            }
            else
            {
                parent.Graph.Insert(siblingIndex, this, parent);
            }
        }

        public void Add(EntityNode child)
        {
            child.SetParent(this);
        }

        public void Insert(int siblingIndex, EntityNode child)
        {
            child.SetParent(siblingIndex, this);
        }

        public bool IsChildrenOf(EntityNode parent)
        {
            return null != parent && parent.Children.Contains(this);
        }

        public int SiblingIndex()
        {
            if (null == Parent)
            {
                return Graph.Roots.IndexOf(this);
            }
            return Parent.Children.IndexOf(this);
        }
        #endregion

        #region Implementation
        public bool IsAncestorOrParentOf(EntityNode node)
        {
            if (this == node)
            {
                return true;
            }

            foreach(var child in Children)
            {
                if (child == node || child.IsAncestorOrParentOf(node))
                {
                    return true;
                }
            }
            return false;
        }
        #endregion
    }
}
#endif // NET_4_6
