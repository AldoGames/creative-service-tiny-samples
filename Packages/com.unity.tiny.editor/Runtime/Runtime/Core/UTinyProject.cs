#if NET_4_6
using System;
using System.Collections.Generic;
using Unity.Properties;
using Unity.Tiny.Attributes;
using Unity.Tiny.Extensions;
using Unity.Tiny.Filters;

namespace Unity.Tiny
{
    /// <inheritdoc cref="UTinyRegistryObjectBase"/>
    /// <summary>
    /// </summary>
    public sealed partial class UTinyProject : UTinyRegistryObjectBase, IPersistentObject
    {
        private const int s_CurrentSerializedVersion = 1;
        public static int CurrentSerializedVersion => s_CurrentSerializedVersion;

        private static readonly EnumProperty<UTinyProject, UTinyTypeId> s_TypeIdProperty =
            new EnumProperty<UTinyProject, UTinyTypeId>("$TypeId",
                /* GET */ c => UTinyTypeId.Project,
                /* SET */ null
            );

        private static readonly Property<UTinyProject, int> s_SerializedVersionProperty =
            new Property<UTinyProject, int>("Version",
                /* GET */ c => c.m_SerializedVersion,
                /* SET */ (c, v) => c.m_SerializedVersion = v
            );

        private static readonly ContainerProperty<UTinyProject, UTinyProjectSettings> s_ProjectSettingsProperty =
            new ContainerProperty<UTinyProject, UTinyProjectSettings>("Settings",
                c => c.m_Settings,
                null
            );

        private static readonly MutableContainerProperty<UTinyProject, UTinyModule.Reference> s_ModuleProperty =
            new MutableContainerProperty<UTinyProject, UTinyModule.Reference>("Module",
                c => c.m_Module,
                (c, v) => c.m_Module = v,
                (c, a, v) => a(ref c.m_Module, v)
            );
        
        private static readonly MutableContainerProperty<UTinyProject, UTinyEntity.Reference> s_ConfigurationProperty =
            new MutableContainerProperty<UTinyProject, UTinyEntity.Reference>("Configuration",
                c => c.m_Configuration,
                (c, v) => c.m_Configuration = v,
                (c, a, v) => a(ref c.m_Configuration, v)
            );

        private static readonly IPropertyBag s_PropertyBag = new PropertyBag(
            s_TypeIdProperty,
            s_SerializedVersionProperty,
            // inherited
            IdProperty,
            NameProperty,
            ExportFlagsProperty,
            DocumentationProperty,
            // end - inherited
            s_ProjectSettingsProperty,
            s_ModuleProperty,
            s_ConfigurationProperty
        );

        private int m_SerializedVersion;
        private readonly UTinyProjectSettings m_Settings;
        private UTinyModule.Reference m_Module;
        private UTinyEntity.Reference m_Configuration;

        public int SerializedVersion
        {
            get { return s_SerializedVersionProperty.GetValue(this); }
            set { s_SerializedVersionProperty.SetValue(this, value); }
        }

        public UTinyProjectSettings Settings => s_ProjectSettingsProperty.GetValue(this);

        public string PersistenceId { get; set; }

        /// <summary>
        /// Main module
        /// </summary>
        public UTinyModule.Reference Module
        {
            get { return s_ModuleProperty.GetValue(this); }
            set { s_ModuleProperty.SetValue(this, value); }
        }

        /// <summary>
        /// Configuration entity
        /// </summary>
        public UTinyEntity.Reference Configuration
        {
            get { return s_ConfigurationProperty.GetValue(this); }
            set { s_ConfigurationProperty.SetValue(this, value); }
        }

        public IEnumerable<IPropertyContainer> EnumeratePersistedObjects()
        {
            yield return this;
            
            var module = Module.Dereference(Registry);
            if (module != null)
            {
                foreach (var c in module.EnumeratePersistedObjects())
                {
                    yield return c;
                }
            }

            var entity = Configuration.Dereference(Registry);
            if (entity != null)
            {
                yield return entity;
            }
        }
        
        public override IPropertyBag PropertyBag => s_PropertyBag;

        public UTinyProject(IRegistry registry, IVersionStorage versionStorage) : base(registry, versionStorage)
        {
            m_Settings = new UTinyProjectSettings(this);
        }
        
        public override void IncrementVersion(IProperty property, IPropertyContainer container)
        {
            Version++;
            if (container is UTinyProjectSettings)
            {
                SharedVersionStorage?.IncrementVersion(s_ProjectSettingsProperty, this);
            }
            else if (container is UTinyModule.Reference)
            {
                SharedVersionStorage?.IncrementVersion(s_ModuleProperty, this);
            }
            else
            {
                SharedVersionStorage?.IncrementVersion(property, this);
            }
        }

        public void RefreshConfiguration()
        {
            var entity = m_Configuration.Dereference(Registry);
            
            foreach (var typeRef in Module.Dereference(Registry).EnumerateDependencies().ConfigurationTypeRefs())
            {
                var component = entity.GetComponent(typeRef) ?? entity.AddComponent(typeRef);

                // @HACK Until we move settings exclusively to configuration components
                if (component.Type.Equals(Registry.GetDisplayInfoType()))
                {
                    component["width"] = m_Settings.CanvasWidth;
                    component["height"] = m_Settings.CanvasHeight;
                    component["autoSizeToFrame"] = m_Settings.CanvasAutoResize;
                }
            }
        }

        /// <inheritdoc cref="IPropertyContainer"/>
        /// <summary>
        /// Weak reference to a type that can be serialized
        /// </summary>
        public struct Reference : IReference<UTinyProject>, IPropertyContainer, IEquatable<Reference>
        {
            private static readonly StructProperty<Reference, UTinyId> s_IdProperty = new StructProperty<Reference, UTinyId>("Id",
                    (ref Reference c) => c.m_Id,
                    null
                ).WithAttribute(InspectorAttributes.HideInInspector)
                 .WithAttribute(InspectorAttributes.Readonly);

            private static readonly StructProperty<Reference, string> s_NameProperty = new StructProperty<Reference, string>("Name",
                (ref Reference c) => c.m_Name,
                (ref Reference c, string v) => c.m_Name = v
            );

            private static readonly IPropertyBag s_PropertyBag = new PropertyBag(
                s_IdProperty,
                s_NameProperty);

            public static Reference None { get; } = new Reference(UTinyId.Empty, string.Empty);

            private readonly UTinyId m_Id;
            private string m_Name;

            public UTinyId Id => s_IdProperty.GetValue(ref this);
            public string Name => s_NameProperty.GetValue(ref this);
            public IPropertyBag PropertyBag => s_PropertyBag;
            public IVersionStorage VersionStorage => PassthroughVersionStorage.Instance;

            public Reference(UTinyId id, string name)
            {
                m_Id = id;
                m_Name = name;
            }

            public UTinyProject Dereference(IRegistry registry)
            {
                return registry.Dereference<Reference, UTinyProject>(this);
            }

            public static explicit operator Reference(UTinyProject @object)
            {
                return new Reference(@object.Id, @object.Name);
            }

            public override string ToString()
            {
                return "Reference " + Name;
            }

            public bool Equals(Reference other)
            {
                return m_Id.Equals(other.m_Id);
            }

            public override bool Equals(object obj)
            {
                if (ReferenceEquals(null, obj)) return false;
                return obj is Reference && Equals((Reference) obj);
            }

            public override int GetHashCode()
            {
                return m_Id.GetHashCode();
            }
        }
    }
}
#endif // NET_4_6
