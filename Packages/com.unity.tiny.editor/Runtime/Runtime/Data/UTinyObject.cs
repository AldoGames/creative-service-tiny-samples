#if NET_4_6
using System.Collections.Generic;
using Unity.Properties;
using Unity.Tiny.Attributes;
using Unity.Tiny.Serialization.FlatJson;
using static Unity.Tiny.Attributes.InspectorAttributes;

namespace Unity.Tiny
{
    /// <summary>
    /// Represents dynamic object instance of a UTinyType
    /// </summary>
    public sealed partial class UTinyObject : IPropertyContainer, IVersionStorage, IVersioned
    {
        private static readonly MutableContainerProperty<UTinyObject, UTinyType.Reference> s_TypeProperty =
            new MutableContainerProperty<UTinyObject, UTinyType.Reference>("Type",
                /* GET */ c => c.m_Type,
                /* SET */ (c, v) =>
                {
                    c.m_Type = v;
                    c.m_CurrentTypeVersion = -1;
                },
                /* REF */ (c, a, v) => a(ref c.m_Type, v)
            ).WithAttribute(HideInInspector);

        private static readonly ContainerProperty<UTinyObject, PropertiesContainer> s_PropertiesProperty =
            new ContainerProperty<UTinyObject, PropertiesContainer>("Properties",
                /* GET */ c => c.m_Properties,
                /* SET */ null
            );
        
        private static readonly IPropertyBag s_PropertyBag = new PropertyBag(
            s_TypeProperty, 
            s_PropertiesProperty);

        private readonly IVersionStorage m_SharedVersionStorage;
        private int m_CurrentTypeVersion;
        private int m_DefaultValueVersion;
        private UTinyType.Reference m_Type;
        private readonly PropertiesContainer m_Properties;

        public IRegistry Registry { get; }
        
        public UTinyType.Reference Type
        {
            get { return s_TypeProperty.GetValue(this); }
            set { s_TypeProperty.SetValue(this, value); }
        }

        public string Name { get; set; }

        public PropertiesContainer Properties => s_PropertiesProperty.GetValue(this);

        /// <summary>
        /// Is this object a default value? 
        /// NOTE: This is only true on top level/root objects
        /// </summary>
        public bool IsDefaultValue { get; internal set; }

        /// <summary>
        /// Does this object have any overridden values
        /// </summary>
        public bool IsOverridden => m_Properties.IsOverridden;
        
        /// <summary>
        /// Version for this object, incremented when any field changes
        /// </summary>
        public int Version { get; private set; }
        
        public IPropertyBag PropertyBag => s_PropertyBag;
        public IVersionStorage VersionStorage => this;

        public UTinyObject(IRegistry registry, UTinyType.Reference type, IVersionStorage versionStorage = null, bool refresh = true)
        {
            Registry = registry;
            m_SharedVersionStorage = versionStorage;
            m_Properties = new PropertiesContainer(this);
            m_Type = type;

            if (refresh)
            {
                Refresh();
            }
        }

        public object this[string key]
        {
            get
            {
                return m_Properties[key];
            }
            set
            {
                m_Properties[key] = value;
            }
        }

        public bool HasProperty(string key)
        {
            return m_Properties.HasProperty(key);
        }

        public void RemoveProperty(string key)
        {
            m_Properties.RemoveProperty(key);
        }
        
        public IEnumerable<KeyValuePair<string, object>> EnumerateProperties()
        {
            var container = (IPropertyContainer) m_Properties;
            foreach (var property in m_Properties.PropertyBag.Properties)
            {
                yield return new KeyValuePair<string, object>(property.Name, property.GetObjectValue(container));
            }
        }

        public int GetVersion(IProperty property, IPropertyContainer container)
        {
            return m_SharedVersionStorage?.GetVersion(property, container) ?? 0;
        }

        public void IncrementVersion(IProperty property, IPropertyContainer container)
        {
            Version++;
            
            // Is this one of our sub properties
            if (!ReferenceEquals(container, this))
            {
                // One of our properties or sub properties has been updated
                m_SharedVersionStorage?.IncrementVersion(s_PropertiesProperty, this);
            }
            else
            {
                m_SharedVersionStorage?.IncrementVersion(property, container);
            }
        }

        /// <summary>
        /// Updates the value tree based on its internal type and migrates any values
        ///
        /// @TODO This method does WAY to much
        ///     - We migrate data
        ///     - Ensure types are up to date
        ///     - Rebuild properties
        /// </summary>
        public void Refresh(UTinyObject defaultValue = null, bool skipTypeCheck = false)
        {
            var type = Type.Dereference(Registry);

            if (null == type)
            {
                // m_Properties.Clear();
                return;
            }
            
            if (!IsDefaultValue)
            {
                // Force the type to be refreshed
                if (!skipTypeCheck)
                {
                    type.Refresh();
                }

                if (defaultValue == null)
                {
                    defaultValue = type.GetDefaultValue() as UTinyObject;
                }
            }

            var defaultValueVersion = defaultValue?.Version ?? -1;
            
            if (m_CurrentTypeVersion == type.Version && m_DefaultValueVersion == defaultValueVersion)
            {
                return;
            }
            
            // Fix up the ref name
            m_Type = (UTinyType.Reference) type;
            
            m_Properties.Refresh(type, defaultValue, skipTypeCheck);
            m_CurrentTypeVersion = type.Version;
            m_DefaultValueVersion = defaultValueVersion;
            if (type.TypeCode == UTinyTypeCode.Component)
            {
                Name = type.Name;
            }
        }

        /// <summary>
        /// Resets all values to thier initial/default state
        /// </summary>
        public void Reset(UTinyObject defaultValue = null)
        {
            Refresh();
            var type = Type.Dereference(Registry);
            m_Properties.Reset(type, defaultValue);
        }
        
        /// <summary>
        /// Copies the properties from the given UTinyObject to this object
        ///
        /// !!! IMPORTANT !!! Property `override` flags are copied from the source object and do NOT respect the defaults of this object
        /// </summary>
        /// <param name="other"></param>
        public void CopyFrom(UTinyObject other)
        {
            Type = other.Type;
            foreach (var property in other.Properties.PropertyBag.Properties)
            {
                var vc = (IPropertyContainer) other.Properties;
                this[property.Name] = property.GetObjectValue(vc);
                Properties.SetOverridden(property.Name, (property as IUTinyValueProperty)?.IsOverridden(vc) ?? true);
            }
        }
        
        public override string ToString()
        {
            return BackEnd.Persist(this);
        }
    }
}
#endif // NET_4_6
