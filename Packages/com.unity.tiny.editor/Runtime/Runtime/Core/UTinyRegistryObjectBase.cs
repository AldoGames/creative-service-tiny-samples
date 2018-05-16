#if NET_4_6
using System.IO;
using Unity.Properties;
using Unity.Tiny.Attributes;
using Unity.Tiny.Serialization.CommandStream;

namespace Unity.Tiny
{
    /// <inheritdoc cref="IRegistryObject" />
    /// <summary>
    /// Base class for UTiny registry objects
    /// </summary>
    public abstract class UTinyRegistryObjectBase : IPropertyContainer, IVersionStorage, IRegistryObject, IOriginator
    {
        protected static readonly Property<UTinyRegistryObjectBase, UTinyId> IdProperty
            = new Property<UTinyRegistryObjectBase, UTinyId>("Id",
                /* GET */ c => c.m_Id,
                /* SET */ (c, v) => c.m_Id = v
            ).WithAttribute(InspectorAttributes.HideInInspector).WithAttribute(InspectorAttributes.Readonly);

        protected static readonly Property<UTinyRegistryObjectBase, string> NameProperty
            = new Property<UTinyRegistryObjectBase, string>("Name",
                /* GET */ c => c.m_Name,
                /* SET */ (c, v) => c.m_Name = v
            ).WithAttribute(InspectorAttributes.HideInInspector).WithAttribute(InspectorAttributes.Readonly);
        
        /// <summary>
        /// @TODO Remove this from the base class this should be composition
        /// @NOTE This is NOT included as a Property by default. You must opt-in from the inheriting class
        /// </summary>
        protected static readonly ContainerProperty<UTinyRegistryObjectBase, UTinyDocumentation> DocumentationProperty 
            = new ContainerProperty<UTinyRegistryObjectBase, UTinyDocumentation>("Documentation",
            /* GET */ c => c.m_Documentation ?? (c.m_Documentation = new UTinyDocumentation(c)),
            /* SET */ null
        );
        
        /// <summary>
        /// @TODO Remove this from the base class this should be composition
        /// @NOTE This is NOT included as a Property by default. You must opt-in from the inheriting class
        /// </summary>
        protected static readonly EnumProperty<UTinyRegistryObjectBase, UTinyExportFlags> ExportFlagsProperty 
            = new EnumProperty<UTinyRegistryObjectBase, UTinyExportFlags>("ExportFlags",
                /* GET */ c => c.m_ExportFlags,
                /* SET */ (c, v) => c.m_ExportFlags = v
            );
        
        private static readonly IPropertyBag s_PropertyBag = new PropertyBag(
            IdProperty, 
            NameProperty);
        
        protected readonly IVersionStorage SharedVersionStorage;
        private UTinyId m_Id;
        private string m_Name;
        private UTinyDocumentation m_Documentation;
        private UTinyExportFlags m_ExportFlags;

        public IRegistry Registry { get; }

        public int Version { get; protected set; }
        
        public UTinyId Id
        {
            get { return IdProperty.GetValue(this); }
            set { IdProperty.SetValue(this, value); }
        }
        
        public string Name
        {
            get { return NameProperty.GetValue(this); }
            set { NameProperty.SetValue(this, value); }
        }
        
        public UTinyExportFlags ExportFlags
        {
            get { return ExportFlagsProperty.GetValue(this); }
            set { ExportFlagsProperty.SetValue(this, value); }
        }

        public bool IsRuntimeIncluded => 0 != (ExportFlags & UTinyExportFlags.RuntimeIncluded);

        public UTinyDocumentation Documentation => DocumentationProperty.GetValue(this);

        public virtual IPropertyBag PropertyBag => s_PropertyBag;
        public virtual IVersionStorage VersionStorage => this;
        
        protected UTinyRegistryObjectBase(IRegistry registry, IVersionStorage versionStorage)
        {
            Registry = registry;
            SharedVersionStorage = versionStorage;
        }

        private class CommandMemento : IMemento
        {
            public int Version { get; }
            private MemoryStream Data { get; }

            public CommandMemento(UTinyRegistryObjectBase obj)
            {
                Version = obj.Version;
                Data = new MemoryStream();
                BackEnd.Persist(Data, obj);
            }

            public void Restore(UTinyRegistryObjectBase obj)
            {
                Data.Position = 0;
                FrontEnd.Accept(Data, obj.Registry);
            }
        }
        
        public IMemento Save()
        {
            return new CommandMemento(this);
        }

        public void Restore(IMemento memento)
        {
            ((CommandMemento)memento).Restore(this);
            Version = memento.Version;
        }

        public int GetVersion(IProperty property, IPropertyContainer container)
        {
            return SharedVersionStorage.GetVersion(property, container);
        }

        public virtual void IncrementVersion(IProperty property, IPropertyContainer container)
        {
            Version++;
            SharedVersionStorage.IncrementVersion(property, this);
        }

        public virtual void Refresh()
        {
            
        }
    }
}
#endif // NET_4_6
