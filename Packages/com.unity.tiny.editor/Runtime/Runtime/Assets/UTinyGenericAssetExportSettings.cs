#if NET_4_6
using Unity.Properties;

namespace Unity.Tiny
{
    public class UTinyGenericAssetExportSettings : UTinyAssetExportSettings, ICopyable<UTinyGenericAssetExportSettings>
    {
        private static readonly IPropertyBag s_PropertyBag = new PropertyBag(
            // inherited
            s_EmbeddedProperty,
            s_IncludePreviewInDocumentationProperty);
        
        public override IPropertyBag PropertyBag => s_PropertyBag;
        
        public void CopyFrom(UTinyGenericAssetExportSettings other)
        {
            base.CopyFrom(other);
            VersionStorage.IncrementVersion(null, this);
        }
    }
}
#endif // NET_4_6
