#if NET_4_6
using Unity.Properties;

namespace Unity.Tiny
{
    public enum TextureFormatType
    {
        /// <summary>
        /// 
        /// </summary>
        Source,

        /// <summary>
        /// Standard PNG format
        /// </summary>
        PNG,

        /// <summary>
        /// Standard JPEG format
        /// </summary>
        JPG,
        
        /// <summary>
        /// Google's webp format
        /// </summary>
        WebP
    }

    public class UTinyTextureSettings : UTinyAssetExportSettings, ICopyable<UTinyTextureSettings>
    {
        private static readonly EnumProperty<UTinyTextureSettings, TextureFormatType> s_FormatTypeProperty =
            new EnumProperty<UTinyTextureSettings, TextureFormatType>(
                "FormatType",
                c => c.m_FormatType,
                (c, v) => c.m_FormatType = v);

        private static readonly Property<UTinyTextureSettings, int> s_JpgCompressionQualityProperty =
            new Property<UTinyTextureSettings, int>(
                "JpgCompressionQuality",
                c => c.m_JpgCompressionQuality,
                (c, v) => c.m_JpgCompressionQuality = v);

        private static readonly Property<UTinyTextureSettings, int> s_WebPCompressionQualityProperty =
            new Property<UTinyTextureSettings, int>(
                "WebPCompressionQuality",
                c => c.m_WebPCompressionQuality,
                (c, v) => c.m_WebPCompressionQuality = v);

        private static readonly IPropertyBag s_PropertyBag = new PropertyBag(
            s_FormatTypeProperty,
            s_JpgCompressionQualityProperty,
            s_WebPCompressionQualityProperty,
            // inherited
            s_EmbeddedProperty,
            s_IncludePreviewInDocumentationProperty);

        private TextureFormatType m_FormatType = TextureFormatType.JPG;
        private int m_JpgCompressionQuality = 75;
        private int m_WebPCompressionQuality = 60;

        /// <summary>
        /// The format this texture should be exported as
        /// </summary>
        public TextureFormatType FormatType
        {
            get { return s_FormatTypeProperty.GetValue(this); }
            set { s_FormatTypeProperty.SetValue(this, value); }
        }

        /// <summary>
        /// Jpg compression quality 1...100 (default is 75)
        /// </summary>
        public int JpgCompressionQuality
        {
            get { return s_JpgCompressionQualityProperty.GetValue(this); }
            set { s_JpgCompressionQualityProperty.SetValue(this, value); }
        }

        /// <summary>
        /// WebP compression quality 1...100 (default is 60)
        /// </summary>
        public int WebPCompressionQuality
        {
            get { return s_WebPCompressionQualityProperty.GetValue(this); }
            set { s_WebPCompressionQualityProperty.SetValue(this, value); }
        }

        public override IPropertyBag PropertyBag => s_PropertyBag;

        public void CopyFrom(UTinyTextureSettings other)
        {
            base.CopyFrom(other);
            
            m_FormatType = other.m_FormatType;
            m_JpgCompressionQuality = other.m_JpgCompressionQuality;
            m_WebPCompressionQuality = other.m_WebPCompressionQuality;

            VersionStorage.IncrementVersion(null, this);
        }
    }
}
#endif // NET_4_6
