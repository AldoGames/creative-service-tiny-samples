#if NET_4_6
namespace Unity.Tiny
{
    public class UTinyAssetTreeViewItem : UTinyTreeViewItem
    {
        public UTinyAssetInfo AssetInfo { get; }
        public override string displayName
        {
            get
            {
                if (!AssetInfo.Object || null == AssetInfo.Object)
                {
                    return "(null)";
                }
                
                var asset = Module.Dereference(Registry).GetAsset(AssetInfo.Object);
                return null != asset ? asset.Name : AssetInfo.Object.name;
            }
        }

        public UTinyAssetTreeViewItem(IRegistry registry, UTinyModule.Reference mainModule, UTinyModule.Reference module, UTinyAssetInfo assetInfo) : base(registry, mainModule, module)
        {
            AssetInfo = assetInfo;
        }
    }
}
#endif // NET_4_6
