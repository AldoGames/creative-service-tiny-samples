#if NET_4_6
namespace Unity.Tiny
{
    public class UTinyScriptTreeViewItem : UTinyTreeViewItem
    {
        public UTinyScript.Reference Script { get; }
        public override string displayName => Script.Dereference(Registry)?.Name ?? Script.Name;

        public UTinyScriptTreeViewItem(IRegistry registry, UTinyModule.Reference mainModule, UTinyModule.Reference module, UTinyScript.Reference script) 
            : base(registry, mainModule, module)
        {
            Script = script;
        }
    }
}
#endif // NET_4_6
