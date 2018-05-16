#if NET_4_6
using Unity.Properties;

namespace Unity.Tiny
{
    public class UTinyAudioClipSettings : UTinyAssetExportSettings, ICopyable<UTinyAudioClipSettings>
    {
        private static readonly IPropertyBag s_PropertyBag = new PropertyBag(
            // inherited
            s_EmbeddedProperty,
            s_IncludePreviewInDocumentationProperty);
        
        public override IPropertyBag PropertyBag => s_PropertyBag;
        
        public void CopyFrom(UTinyAudioClipSettings other)
        {
            base.CopyFrom(other);
            VersionStorage.IncrementVersion(null, this);
        }
    }
}
#endif // NET_4_6
