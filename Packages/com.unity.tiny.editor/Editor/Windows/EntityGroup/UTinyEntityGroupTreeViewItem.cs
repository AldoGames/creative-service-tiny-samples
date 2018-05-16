#if NET_4_6
namespace Unity.Tiny
{
    public class UTinyEntityGroupTreeViewItem : UTinyTreeViewItem
    {
        public UTinyEntityGroup.Reference EntityGroup { get; }
        public override string displayName => EntityGroup.Dereference(Registry)?.Name ?? EntityGroup.Name;
        
        public UTinyEntityGroupTreeViewItem(IRegistry registry, UTinyModule.Reference mainModule, UTinyModule.Reference module, UTinyEntityGroup.Reference entityGroup) 
            : base(registry, mainModule, module)
        {
            EntityGroup = entityGroup;
        }
    }
}
#endif // NET_4_6
