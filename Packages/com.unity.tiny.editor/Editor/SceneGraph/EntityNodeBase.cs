#if NET_4_6
namespace Unity.Tiny
{
    public interface IEntityNode
    {
        EntityGroupGraph Graph { get; set; }
        UTinyEntity.Reference Entity { get; set; }
        bool EnabledInHierarchy { get; }

        int SiblingIndex();
    }
}
#endif // NET_4_6
