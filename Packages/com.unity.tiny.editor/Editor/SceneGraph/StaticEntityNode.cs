#if NET_4_6
namespace Unity.Tiny
{
    public class StaticEntityNode : IEntityNode
    { 
        public EntityGroupGraph Graph { get; set; }

        public UTinyEntity.Reference Entity { get; set; }

        public bool EnabledInHierarchy
        {
            get
            {
                return Entity.Dereference(UTinyEditorApplication.Registry)?.Enabled ?? false;
            }
        }

        public int SiblingIndex()
        {
            return Graph.StaticEntities.IndexOf(this);
        }
    }
}
#endif // NET_4_6
