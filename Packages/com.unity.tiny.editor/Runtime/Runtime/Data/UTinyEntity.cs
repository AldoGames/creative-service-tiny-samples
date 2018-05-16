#if NET_4_6
using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Properties;
using Unity.Tiny.Attributes;
using static Unity.Tiny.Attributes.InspectorAttributes;

namespace Unity.Tiny
{
    public delegate void UTinyEntityComponentEventHandler(UTinyEntity entity, UTinyObject component);

    /// <inheritdoc cref="UTinyRegistryObjectBase" />
    /// <summary>
    /// </summary>
    public sealed class UTinyEntity : UTinyRegistryObjectBase, IPropertyContainer
    {
        private static readonly EnumProperty<UTinyEntity, UTinyTypeId> s_TypeIdProperty = new EnumProperty<UTinyEntity, UTinyTypeId>("$TypeId",
                /* GET */ c => UTinyTypeId.Entity,
                /* SET */ null
            ).WithAttribute(HideInInspector)
            .WithAttribute(Readonly);

        private static readonly Property<UTinyEntity, bool> s_EnabledProperty = new Property<UTinyEntity, bool>(
            "Enabled",
            /* GET */ c => c.m_Enabled,
            /* SET */ (c, v) => c.m_Enabled = v
        ).WithAttribute(HideInInspector);

        private static readonly Property<UTinyEntity, int> s_LayerProperty = new Property<UTinyEntity, int>(
            "Layer",
            /* GET */ c => c.m_Layer,
            /* SET */ (c, v) => c.m_Layer = v
        ).WithAttribute(HideInInspector);

        private static readonly ContainerListProperty<UTinyEntity, IList<UTinyObject>, UTinyObject> s_ComponentsProperty
            = new ContainerListProperty<UTinyEntity, IList<UTinyObject>, UTinyObject>(
                "Components",
                /* GET */ c => c.m_Components ?? (c.m_Components = new List<UTinyObject>()),
                /* SET */ null,
                /* NEW */ c => c.NewComponent((UTinyType.Reference) UTinyType.NewComponentType)
            ).WithAttribute(DontList);

        private static readonly IPropertyBag s_PropertyBag = new PropertyBag(
            s_TypeIdProperty,
            IdProperty,
            NameProperty,
            s_EnabledProperty,
            s_LayerProperty,
            s_ComponentsProperty);

        private bool m_Enabled = true;
        private int m_Layer;
        private List<UTinyObject> m_Components;

        public bool Enabled
        {
            get { return s_EnabledProperty.GetValue(this); }
            set { s_EnabledProperty.SetValue(this, value); }
        }

        public int Layer
        {
            get { return s_LayerProperty.GetValue(this); }
            set { s_LayerProperty.SetValue(this, value); }
        }

        public IList<UTinyObject> Components => s_ComponentsProperty.GetValue(this);
        public UTinyEntityGroup EntityGroup { get; set; }
        public override IPropertyBag PropertyBag => s_PropertyBag;

        public UTinyEntity(IRegistry registry, IVersionStorage versionStorage) : base(registry, versionStorage)
        {
        }

        public override void IncrementVersion(IProperty property, IPropertyContainer container)
        {
            Version++;
            if (!ReferenceEquals(container, this))
            {
                SharedVersionStorage.IncrementVersion(s_ComponentsProperty, this);
            }
            else
            {
                SharedVersionStorage.IncrementVersion(property, container);
            }
        }

        public UTinyObject AddComponent(UTinyType.Reference type)
        {
            var component = NewComponent(type);
            s_ComponentsProperty.Add(this, component);
            OnComponentAdded?.Invoke(this, component);
            return component;
        }
        
        private UTinyObject NewComponent(UTinyType.Reference type)
        {
            return new UTinyObject(Registry, type, this, false);
        }

        public UTinyObject GetOrAddComponent(UTinyType.Reference type)
        {
            var component = GetComponent(type) ?? AddComponent(type);
            return component;
        }

        public UTinyObject GetComponent(UTinyType.Reference type)
        {
            for(int i = 0; i < Components.Count; ++i)
            {
                var component = Components[i];
                if (component.Type.Equals(type))
                {
                    return component;
                }
            }
            return null;
        }

        public UTinyObject RemoveComponent(UTinyType.Reference type)
        {
            var component = Components.FirstOrDefault(o => o.Type.Equals(type));
            if (null != component)
            {
                var e = OnComponentRemoved;
                e?.Invoke(this, component);
                var self = this;
                s_ComponentsProperty.Remove(self, component);
            }
            return component;
        }

        public UTinyEntityView View { get; set; }

        #region Events

        public event UTinyEntityComponentEventHandler OnComponentAdded;
        public event UTinyEntityComponentEventHandler OnComponentRemoved;

        #endregion

        /// <inheritdoc cref="IPropertyContainer"/>
        /// <summary>
        /// Weak reference to a type that can be serialized
        /// </summary>
        public struct Reference : IReference<UTinyEntity>, IPropertyContainer, IEquatable<Reference>
        {
            private static readonly StructProperty<Reference, UTinyId> s_IdProperty = new StructProperty<Reference, UTinyId>("Id",
                    (ref Reference c) => c.m_Id,
                    null
                ).WithAttribute(HideInInspector)
                .WithAttribute(Readonly);

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

            public UTinyEntity Dereference(IRegistry registry)
            {
                var entity = registry.Dereference<Reference, UTinyEntity>(this);
                m_Name = entity?.Name ?? m_Name;
                return entity;
            }

            public static explicit operator Reference(UTinyEntity @object)
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
