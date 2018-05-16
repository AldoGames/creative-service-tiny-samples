#if NET_4_6
using System;
using Unity.Properties;
using UnityEngine;
using Unity.Tiny.Attributes;

namespace Unity.Tiny
{
    /// <inheritdoc cref="UTinyRegistryObjectBase" />
    /// <summary>
    /// </summary>
    public sealed class UTinyScript : UTinyRegistryObjectBase, IPropertyContainer
    {
        private static readonly EnumProperty<UTinyScript, UTinyTypeId> s_TypeIdProperty =
            new EnumProperty<UTinyScript, UTinyTypeId>("$TypeId",
                    /* GET */ c => UTinyTypeId.Script,
                    /* SET */ null
                ).WithAttribute(InspectorAttributes.HideInInspector)
                .WithAttribute(InspectorAttributes.Readonly);

        private static readonly Property<UTinyScript, bool> s_IncludedProperty = new Property<UTinyScript, bool>(
            "Included",
            /* GET */ c => c.m_Included,
            /* SET */ (c, v) => c.m_Included = v
        );

        private static readonly Property<UTinyScript, TextAsset> s_TextAssetProperty =
            new Property<UTinyScript, TextAsset>(
                "TextAsset",
                /* GET */ c => c.m_TextAsset,
                /* SET */ (c, v) => c.m_TextAsset = v
            );

        private static readonly IPropertyBag s_PropertyBag = new PropertyBag(
            s_TypeIdProperty,
            // inherited
            IdProperty,
            NameProperty,
            ExportFlagsProperty,
            DocumentationProperty,
            // end - inherited
            s_IncludedProperty,
            s_TextAssetProperty);

        private bool m_Included;
        private TextAsset m_TextAsset;

        public bool Included
        {
            get { return s_IncludedProperty.GetValue(this); }
            set { s_IncludedProperty.SetValue(this, value); }
        } 
        
        public TextAsset TextAsset
        {
            get { return s_TextAssetProperty.GetValue(this); }
            set { s_TextAssetProperty.SetValue(this, value); }
        }

        public override IPropertyBag PropertyBag => s_PropertyBag;

        public UTinyScript(IRegistry registry, IVersionStorage versionStorage) : base(registry, versionStorage)
        {
        }

        /// <inheritdoc cref="IPropertyContainer"/>
        /// <summary>
        /// Weak reference to a type that can be serialized
        /// </summary>
        public struct Reference : IReference<UTinyScript>, IPropertyContainer, IEquatable<Reference>
        {
            private static readonly StructProperty<Reference, UTinyId> s_IdProperty =
                new StructProperty<Reference, UTinyId>("Id",
                        (ref Reference c) => c.m_Id,
                        null
                    ).WithAttribute(InspectorAttributes.HideInInspector)
                    .WithAttribute(InspectorAttributes.Readonly);

            private static readonly StructProperty<Reference, string> s_NameProperty =
                new StructProperty<Reference, string>("Name",
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

            public UTinyScript Dereference(IRegistry registry)
            {
                return registry.Dereference<Reference, UTinyScript>(this);
            }

            public static explicit operator Reference(UTinyScript @object)
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
