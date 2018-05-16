#if NET_4_6
using Unity.Properties;

namespace Unity.Tiny
{
    public interface ICopyable<in T>
    {
        void CopyFrom(T original);
    }
    
    public abstract class UTinyAssetExportSettings : IPropertyContainer
    {
        protected static readonly Property<UTinyAssetExportSettings, bool> s_EmbeddedProperty =
            new Property<UTinyAssetExportSettings, bool>(
                "Embedded",
                c => c.m_Embedded,
                (c, v) => c.m_Embedded = v);

        protected static readonly Property<UTinyAssetExportSettings, bool> s_IncludePreviewInDocumentationProperty =
            new Property<UTinyAssetExportSettings, bool>(
                "IncludePreviewInDocumentation",
                c => c.m_IncludePreviewInDocumentation,
                (c, v) => c.m_IncludePreviewInDocumentation = v);
        
        private bool m_Embedded = true;
        private bool m_IncludePreviewInDocumentation = true;
        
        public abstract IPropertyBag PropertyBag { get; }
        public IVersionStorage VersionStorage { get; internal set; }

        /// <summary>
        /// Should this image be encoded as base64
        /// </summary>
        public bool Embedded
        {
            get { return s_EmbeddedProperty.GetValue(this); }
            set { s_EmbeddedProperty.SetValue(this, value); }
        }

        /// <summary>
        /// Should this image be exported with the documentation
        /// </summary>
        public bool IncludePreviewInDocumentation
        {
            get { return s_IncludePreviewInDocumentationProperty.GetValue(this); }
            set { s_IncludePreviewInDocumentationProperty.SetValue(this, value); }
        }

        protected void CopyFrom(UTinyAssetExportSettings other)
        {
            m_Embedded = other.Embedded;
            m_IncludePreviewInDocumentation = other.IncludePreviewInDocumentation;
        }
    }
}
#endif // NET_4_6
