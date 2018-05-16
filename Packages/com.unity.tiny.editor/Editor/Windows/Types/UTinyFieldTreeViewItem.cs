#if NET_4_6
namespace Unity.Tiny
{
    /// <summary>
    /// Item representing a field in the TreeView
    /// </summary>
    public class UTinyFieldTreeViewItem : UTinyTreeViewItem
    {
        public UTinyField Field { get; }
        public override string displayName => Field.Name;

        public UTinyFieldTreeViewItem(IRegistry registry, UTinyModule.Reference mainModule, UTinyModule.Reference module, UTinyField field) 
            : base(registry, mainModule, module)
        {
            Field = field;
        }
    }
}
#endif // NET_4_6
