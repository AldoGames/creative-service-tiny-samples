#if NET_4_6
using Unity.Properties;
using Unity.Tiny.Attributes;

namespace Unity.Tiny
{
    /// <inheritdoc cref="IPropertyContainer" />
    /// <summary>
    /// UTinyField
    /// Represents a field instance for a given type
    /// </summary>
    public sealed class UTinyField : IPropertyContainer, IIdentifiable<UTinyId>, INamed
    {
        private static readonly Property<UTinyField, UTinyId> s_IdProperty 
            = new Property<UTinyField, UTinyId>("Id",
                /* GET */ c => c.m_Id,
                /* SET */ (c, v) => c.m_Id = v
            ).WithAttribute(InspectorAttributes.HideInInspector);
        
        private static readonly Property<UTinyField, string> s_NameProperty 
            = new Property<UTinyField, string>("Name",
                /* GET */ c => c.m_Name,
                /* SET */ (c, v) => c.m_Name = v
            ).WithAttribute(InspectorAttributes.HideInInspector);
        
        private static readonly ContainerProperty<UTinyField, UTinyDocumentation> s_DocumentationProperty 
            = new ContainerProperty<UTinyField, UTinyDocumentation>("Documentation",
                /* GET */ c => c.m_Documentation ?? (c.m_Documentation = new UTinyDocumentation(c.VersionStorage)),
                /* SET */ null
            );

        private static readonly MutableContainerProperty<UTinyField, UTinyType.Reference> s_FieldTypeProperty =
            new MutableContainerProperty<UTinyField, UTinyType.Reference>("FieldType",
                /* GET */ c => c.m_FieldType,
                /* SET */ (c, v) => c.m_FieldType = v,
                /* REF */ (c, a, v) => a(ref c.m_FieldType, v)
            );
        
        private static readonly Property<UTinyField, bool> s_ArrayProperty = new Property<UTinyField, bool>("Array",
            /* GET */ c => c.m_Array,
            /* SET */ (c, v) => c.m_Array = v
        );
        
        private static readonly IPropertyBag s_PropertyBag = new PropertyBag(
            s_IdProperty, 
            s_NameProperty,
            s_DocumentationProperty, 
            s_FieldTypeProperty,
            s_ArrayProperty);

        private UTinyId m_Id;
        private string m_Name;
        private UTinyDocumentation m_Documentation;
        private int m_FieldTypeVersion;
        private UTinyType.Reference m_FieldType;
        private bool m_Array;
       
        public UTinyId Id
        {
            get { return s_IdProperty.GetValue(this); }
            set { s_IdProperty.SetValue(this, value); }
        }
        
        public string Name
        {
            get { return s_NameProperty.GetValue(this); }
            set { s_NameProperty.SetValue(this, value); }
        }
        
        /// <summary>
        /// Type for this field (i.e. Int32, Single, Vector2)
        /// </summary>
        public UTinyType.Reference FieldType
        {
            get { return s_FieldTypeProperty.GetValue(this); }
            set { s_FieldTypeProperty.SetValue(this, value); }
        }
        
        /// <summary>
        /// Is this an array field
        /// </summary>
        public bool Array
        {
            get { return s_ArrayProperty.GetValue(this); }
            set { s_ArrayProperty.SetValue(this, value); }
        }
        
        public UTinyDocumentation Documentation => s_DocumentationProperty.GetValue(this);
        public UTinyType DeclaringType { get; internal set; }
        public IVersionStorage VersionStorage { get; }
        public IPropertyBag PropertyBag => s_PropertyBag;
        
        public UTinyField(IVersionStorage versionStorage)
        {
            VersionStorage = versionStorage;
        }

        public void Refresh(IRegistry registry)
        {
            var fieldtype = m_FieldType.Dereference(registry);

            if (fieldtype != null)
            {
                fieldtype.Refresh();

                if (fieldtype.Version == m_FieldTypeVersion)
                {
                    return;
                }

                // Fix up the reference
                m_FieldType = (UTinyType.Reference) fieldtype;

                VersionStorage.IncrementVersion(null, this);
                m_FieldTypeVersion = fieldtype.Version;
            }
            else
            {
                if (m_FieldTypeVersion == -1)
                {
                    return;
                }
                
                VersionStorage.IncrementVersion(null, this);
                m_FieldTypeVersion = -1;
            }
        }
    }
}
#endif // NET_4_6
