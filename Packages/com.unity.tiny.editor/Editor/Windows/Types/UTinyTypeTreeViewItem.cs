#if NET_4_6
namespace Unity.Tiny
{
    /// <summary>
    /// Item representing a type in the TreeView
    /// </summary>
    public class UTinyTypeTreeViewItem : UTinyTreeViewItem
    {
        public UTinyType.Reference Type { get; }
        public override string displayName => Type.Dereference(Registry)?.Name ?? Type.Name;

        public UTinyTypeTreeViewItem(IRegistry registry, UTinyModule.Reference mainModule, UTinyModule.Reference module, UTinyType.Reference type) :
            base(registry, mainModule, module)
        {
            Type = type;
        }
    }
}
#endif // NET_4_6
