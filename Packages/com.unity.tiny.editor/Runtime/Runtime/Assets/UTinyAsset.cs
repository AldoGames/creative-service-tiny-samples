#if NET_4_6
using System;
using Unity.Properties;
using Object = UnityEngine.Object;

namespace Unity.Tiny
{
    public class UTinyAsset : IPropertyContainer, IEquatable<UTinyAsset>
    {
        private static readonly Property<UTinyAsset, Object> s_ObjectProperty =
            new Property<UTinyAsset, Object>(
                "Object",
                c => c.m_Object,
                (c, v) => c.m_Object = v);

        private static readonly Property<UTinyAsset, string> s_NameProperty =
            new Property<UTinyAsset, string>(
                "Name",
                c => c.m_Name,
                (c, v) => c.m_Name = v);

        private static readonly ContainerProperty<UTinyAsset, UTinyAssetExportSettings> s_ExportSettings =
            new ContainerProperty<UTinyAsset, UTinyAssetExportSettings>(
                "ExportSettings",
                c => c.m_ExportSettings,
                (c, v) => c.m_ExportSettings = v);

        private static readonly IPropertyBag s_PropertyBag = new PropertyBag(
            s_ObjectProperty,
            s_NameProperty,
            s_ExportSettings);

        public IPropertyBag PropertyBag => s_PropertyBag;
        public IVersionStorage VersionStorage { get; }

        private Object m_Object;
        private string m_Name;
        private UTinyAssetExportSettings m_ExportSettings;

        /// <summary>
        /// The object reference
        /// </summary>
        public Object Object
        {
            get { return s_ObjectProperty.GetValue(this); }
            set { s_ObjectProperty.SetValue(this, value); }
        }

        /// <summary>
        /// Addressable name
        /// </summary>
        public string Name
        {
            get
            {
                var name = s_NameProperty.GetValue(this); 
                
                if (string.IsNullOrEmpty(name) && m_Object)
                {
                    return m_Object.name;
                }

                return name;
            }
            set
            {
                s_NameProperty.SetValue(this, value);
            }
        }

        /// <summary>
        /// Export settings for this asset
        /// </summary>
        public UTinyAssetExportSettings ExportSettings => s_ExportSettings.GetValue(this);

        public UTinyAsset(IVersionStorage versionStorage)
        {
            VersionStorage = versionStorage;
        }

        public TSettings CreateExportSettings<TSettings>()
            where TSettings : UTinyAssetExportSettings, new()
        {
            var settings = new TSettings()
            {
                VersionStorage = VersionStorage
            };

            s_ExportSettings.SetValue(this, settings);

            return settings;
        }

        public void ClearExportSettings()
        {
            s_ExportSettings.SetValue(this, null);
        }

        public bool Equals(UTinyAsset other)
        {
            if (ReferenceEquals(null, other)) return false;
            return ReferenceEquals(this, other) || Equals(m_Object, other.m_Object);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            return obj.GetType() == GetType() && Equals((UTinyAsset) obj);
        }

        public override int GetHashCode()
        {
            return (m_Object && m_Object != null ? m_Object.GetHashCode() : 0);
        }
    }
}
#endif // NET_4_6
