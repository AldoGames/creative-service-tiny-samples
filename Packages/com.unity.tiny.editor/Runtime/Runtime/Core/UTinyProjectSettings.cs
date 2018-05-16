#if NET_4_6
using System;
using Unity.Properties;

namespace Unity.Tiny
{
    public enum UTinyBuildConfiguration
    {
        Debug,
        Development,
        Release
    }

    /// <summary>
    /// Placeholder implementation.
    /// 
    /// This class should be used as the root entry point for all project and platform settings. 
    /// 
    /// Currently this is used as a place to dump all settings for all platforms. 
    /// 
    /// </summary>
    public class UTinyProjectSettings : IPropertyContainer
    {
        private static readonly Property<UTinyProjectSettings, int> s_CanvasWidthProperty 
            = new Property<UTinyProjectSettings, int>("CanvasWidth",
            c => c.m_CanvasWidth,
            (c, v) => c.m_CanvasWidth = Math.Max(v, 1));

        private static readonly Property<UTinyProjectSettings, int> s_CanvasHeightProperty 
            = new Property<UTinyProjectSettings, int>("CanvasHeight",
            c => c.m_CanvasHeight,
            (c, v) => c.m_CanvasHeight = Math.Max(v, 1));
        
        private static readonly Property<UTinyProjectSettings, bool> s_CanvasAutoResizeProperty 
            = new Property<UTinyProjectSettings, bool>("CanvasAutoResize",
                c => c.m_CanvasAutoResize,
                (c, v) => c.m_CanvasAutoResize = v);
        
        
        private static readonly Property<UTinyProjectSettings, bool> s_EmbedAssetsProperty 
            = new Property<UTinyProjectSettings, bool>("EmbedAssets",
                c => c.m_EmbedAssets,
                (c, v) =>
                {
                    c.m_EmbedAssets = v;
                    c.DefaultTextureSettings.Embedded = v;
                    c.DefaultAudioClipSettings.Embedded = v;
                });
        
        private static readonly ContainerProperty<UTinyProjectSettings, UTinyTextureSettings> s_DefaultTextureSettingsProperty 
            = new ContainerProperty<UTinyProjectSettings, UTinyTextureSettings>("DefaultTextureSettings",
                c => c.m_DefaultTextureSettings,
                (c, v) => c.m_DefaultTextureSettings = v);
        
        private static readonly ContainerProperty<UTinyProjectSettings, UTinyAudioClipSettings> s_DefaultAudioClipSettingsProperty 
            = new ContainerProperty<UTinyProjectSettings, UTinyAudioClipSettings>("DefaultAudioClipSettings",
                c => c.m_DefaultAudioClipSettings,
                (c, v) => c.m_DefaultAudioClipSettings = v);

        private static readonly Property<UTinyProjectSettings, int> s_LocalHTTPServerPort
            = new Property<UTinyProjectSettings, int>("LocalHTTPServerPort",
                c => c.m_LocalHTTPServerPort,
                (c, v) => c.m_LocalHTTPServerPort = v);

        private static readonly Property<UTinyProjectSettings, bool> s_SingleFileHtmlProperty
            = new Property<UTinyProjectSettings, bool>("SingleFileHtml",
                c => c.m_SingleFileHtml,
                (c, v) => c.m_SingleFileHtml = v);

        private static readonly Property<UTinyProjectSettings, int> s_MemorySize
            = new Property<UTinyProjectSettings, int>("MemorySize",
                c => c.m_MemorySize,
                (c, v) => c.m_MemorySize = v);

        private static readonly Property<UTinyProjectSettings, bool> s_IncludeWSClient
            = new Property<UTinyProjectSettings, bool>("IncludeWSClient",
                c => c.m_IncludeWSClient,
                (c, v) => c.m_IncludeWSClient = v);

        private static readonly Property<UTinyProjectSettings, bool> s_IncludeWebPDecompressor
            = new Property<UTinyProjectSettings, bool>("IncludeWebPDecompressor",
                c => c.m_IncludeWebPDecompressor,
                (c, v) => c.m_IncludeWebPDecompressor = v);

        private static readonly Property<UTinyProjectSettings, bool> s_RunBabel
            = new Property<UTinyProjectSettings, bool>("RunBabel",
                c => c.m_RunBabel,
                (c, v) => c.m_RunBabel = v);

        private static readonly IPropertyBag s_PropertyBag = new PropertyBag(
            s_CanvasWidthProperty,
            s_CanvasHeightProperty,
            s_CanvasAutoResizeProperty,
            s_EmbedAssetsProperty,
            s_DefaultTextureSettingsProperty,
            s_DefaultAudioClipSettingsProperty,
            s_LocalHTTPServerPort,
            s_SingleFileHtmlProperty,
            s_MemorySize,
            s_IncludeWSClient,
            s_IncludeWebPDecompressor,
            s_RunBabel
        );

        public const int DefaultLocalHTTPServerPort = 9050;
        public const int DefaultMemorySize = 16;

        // IMPORTANT NOTE: If you change a default value here, make sure to also change it where we are
        // currently parsing these properties (currently in FrontEnd.cs at the time of writing this).
        private int m_CanvasWidth;
        private int m_CanvasHeight;
        private bool m_CanvasAutoResize = true;
        private UTinyTextureSettings m_DefaultTextureSettings;
        private UTinyAudioClipSettings m_DefaultAudioClipSettings;
        private bool m_EmbedAssets = true;
        private int m_LocalHTTPServerPort = DefaultLocalHTTPServerPort;
        private bool m_SingleFileHtml = false;
        private int m_MemorySize = DefaultMemorySize;
        private bool m_IncludeWSClient = true;
        private bool m_IncludeWebPDecompressor = false;
        private bool m_RunBabel = true;

        /// <summary>
        /// HTML5 canvas width
        /// </summary>
        public int CanvasWidth
        {
            get { return s_CanvasWidthProperty.GetValue(this); }
            set { s_CanvasWidthProperty.SetValue(this, value); }
        }

        /// <summary>
        /// HTML5 canvas height
        /// </summary>
        public int CanvasHeight
        {
            get { return s_CanvasHeightProperty.GetValue(this); }
            set { s_CanvasHeightProperty.SetValue(this, value); }
        }

        /// <summary>
        /// HTML5 auto resize canvas
        /// </summary>
        public bool CanvasAutoResize
        {
            get { return s_CanvasAutoResizeProperty.GetValue(this); }
            set { s_CanvasAutoResizeProperty.SetValue(this, value); }
        }
        
        public UTinyTextureSettings DefaultTextureSettings => s_DefaultTextureSettingsProperty.GetValue(this);
        public UTinyAudioClipSettings DefaultAudioClipSettings => s_DefaultAudioClipSettingsProperty.GetValue(this);

        /// <summary>
        /// Should assets be embedded if no user defined override was specified
        /// This is exposed at a high level. Internally we still use the settings for the specific asset type
        /// </summary>
        public bool EmbedAssets
        {
            get { return s_EmbedAssetsProperty.GetValue(this); }
            set { s_EmbedAssetsProperty.SetValue(this, value); }
        }

        public int LocalHTTPServerPort
        {
            get { return s_LocalHTTPServerPort.GetValue(this); }
            set { s_LocalHTTPServerPort.SetValue(this, value); }
        }

        public bool SingleFileHtml
        {
            get { return s_SingleFileHtmlProperty.GetValue(this); }
            set { s_SingleFileHtmlProperty.SetValue(this, value); }
        }

        public int MemorySize
        {
            get { return s_MemorySize.GetValue(this); }
            set {
                const int multiple = 16;
                const int max = 2048 - 16;

                // Clamp between multiple and max
                value = Math.Min(Math.Max(value, multiple), max);

                // Round up to multiple
                value = value + 0xF & -0x10;

                s_MemorySize.SetValue(this, value);
            }
        }

        public bool IncludeWSClient
        {
            get { return s_IncludeWSClient.GetValue(this); }
            set { s_IncludeWSClient.SetValue(this, value); }
        }

        public bool IncludeWebPDecompressor
        {
            get { return s_IncludeWebPDecompressor.GetValue(this); }
            set { s_IncludeWebPDecompressor.SetValue(this, value); }
        }

        public bool RunBabel
        {
            get { return s_RunBabel.GetValue(this); }
            set { s_RunBabel.SetValue(this, value); }
        }

        public IVersionStorage VersionStorage { get; }
        public IPropertyBag PropertyBag => s_PropertyBag;

        public UTinyProjectSettings(IVersionStorage versionStorage)
        {
            VersionStorage = versionStorage;
            m_DefaultTextureSettings = new UTinyTextureSettings { VersionStorage = versionStorage };
            m_DefaultAudioClipSettings = new UTinyAudioClipSettings { VersionStorage = versionStorage };
        }

        public UTinyAssetExportSettings GetDefaultAssetExportSettings(Type type)
        {
            if (typeof(UnityEngine.Texture2D).IsAssignableFrom(type))
            {
                return DefaultTextureSettings;
            }
            
            if (typeof(UnityEngine.AudioClip).IsAssignableFrom(type))
            {
                return DefaultAudioClipSettings;
            }

            return new UTinyGenericAssetExportSettings { Embedded = m_EmbedAssets, IncludePreviewInDocumentation = false };
        }
    }
}
#endif // NET_4_6
