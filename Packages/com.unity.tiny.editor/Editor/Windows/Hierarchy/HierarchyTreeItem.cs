#if NET_4_6
using UnityEditor.IMGUI.Controls;

namespace Unity.Tiny
{
    public class HierarchyTreeItemBase : TreeViewItem
    {
        public int Index { get; set; }
    }

    public class HierarchyTreeItem<T> : HierarchyTreeItemBase
    {
        public T Value { get; set; }
    }

    public interface IEntityTreeItem
    {
        UTinyEntity.Reference EntityRef { get; set; }
        EntityGroupGraph Graph { get; }
        IEntityNode Node { get; }
    }

    public interface IStatic { }

    public class HierarchyEntityBase<T> : HierarchyTreeItem<T>, IEntityTreeItem where T : IEntityNode
    {
        public UTinyEntity.Reference EntityRef { get { return Value.Entity; } set { Value.Entity = value; } }

        public EntityGroupGraph Graph { get { return Value.Graph; } }

        public IEntityNode Node { get { return Value; } }

    }

    public class HierarchyStaticEntity : HierarchyEntityBase<StaticEntityNode>, IStatic { }

    public class HierarchyEntity : HierarchyEntityBase<EntityNode> { }

    public class HierarchyEntityGroupGraph : HierarchyTreeItem<EntityGroupGraph> { }
}
#endif // NET_4_6
