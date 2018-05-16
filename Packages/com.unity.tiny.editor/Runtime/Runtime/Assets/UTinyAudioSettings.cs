#if NET_4_6
using Unity.Properties;

namespace Unity.Tiny
{
    public class UTinyAudioSettings : UTinyAssetExportSettings
    {
        private static readonly IPropertyBag s_PropertyBag = new PropertyBag();
        
        public override IPropertyBag PropertyBag => s_PropertyBag;
    }
}
#endif // NET_4_6
