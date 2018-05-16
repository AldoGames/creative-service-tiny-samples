#if NET_4_6
using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Properties;
using UnityEngine.Assertions;
using Unity.Tiny.Attributes;
using static Unity.Tiny.Attributes.InspectorAttributes;

namespace Unity.Tiny
{
    public enum UTinyTypeCode
    {
        Unknown = 0,

        // IEEE Types; low level types with well defined storage
        Int8 = 1,
        Int16 = 2,
        Int32 = 3,
        Int64 = 4,
        UInt8 = 5,
        UInt16 = 6,
        UInt32 = 7,
        UInt64 = 8,
        Float32 = 9,
        Float64 = 10,

        // Mid level platform types; implementations may vary across platforms
        Boolean = 11,
        Char = 12,
        String = 13,

        // Internal types
        Component = 14,
        Struct = 15,
        Enum = 16,
        Configuration = 17,

        // High level dynamic types; these are handled in a special way by UTiny code
        EntityReference = 18,

        // Unity object types
        UnityObject = 19,
    }

    /// <inheritdoc cref="UTinyRegistryObjectBase"/>
    /// <summary>
    /// Represents a single type in the system. This can be used to define custom components, structs, enums etc.
    /// It is also used to define built in types (Int32, Float32 etc.)
    /// </summary>
    public sealed partial class UTinyType : UTinyRegistryObjectBase, IAttributable
    {
        private static readonly EnumProperty<UTinyType, UTinyTypeId> s_TypeIdProperty =
            new EnumProperty<UTinyType, UTinyTypeId>("$TypeId",
                    /* GET */ c => UTinyTypeId.Type,
                    /* SET */ null
                ).WithAttribute(HideInInspector)
                .WithAttribute(Readonly);

        private static readonly MutableContainerProperty<UTinyType, Reference> s_BaseTypeProperty =
            new MutableContainerProperty<UTinyType, Reference>("BaseType",
                /* GET */ c => c.m_BaseType,
                /* SET */ (c, v) => c.m_BaseType = v,
                /* REF */ (c, a, v) => a(ref c.m_BaseType, v)
            );

        private static readonly EnumProperty<UTinyType, UTinyTypeCode> s_TypeCodeProperty =
            new EnumProperty<UTinyType, UTinyTypeCode>("TypeCode",
                /* GET */ c => c.m_TypeCode,
                /* SET */ (c, v) => c.m_TypeCode = v
            );

        private static readonly ContainerListProperty<UTinyType, List<UTinyField>, UTinyField> s_FieldsProperty =
            new ContainerListProperty<UTinyType, List<UTinyField>, UTinyField>("Fields",
                /* GET */ c => c.m_Fields,
                /* SET */ null,
                /* NEW */ c => c.NewField(UTinyId.New(), "NewField", (Reference) Int32)
            );

        private struct DefaultValueBackingField
        {
            public int ObjectVersion;
            public int TypeCodeVersion;
            public int FieldsVersion;
            public object Value;
            public UTinyObject Object;
        }

        private Reference m_BaseType;
        private UTinyTypeCode m_TypeCode;
        private readonly List<UTinyField> m_Fields;
        private DefaultValueBackingField m_DefaultValue;
        private IProperty m_DefaultValueProperty;

        private readonly PropertyBag m_PropertyBag = new PropertyBag(
            s_TypeIdProperty,
            // inherited
            IdProperty,
            NameProperty,
            ExportFlagsProperty,
            DocumentationProperty,
            // end - inherited
            s_TypeCodeProperty,
            s_BaseTypeProperty,
            s_FieldsProperty);

        /// <summary>
        /// Base type this type inherits from
        /// NOTE: This is also used for enums with an underlying type
        /// </summary>
        public Reference BaseType
        {
            get { return s_BaseTypeProperty.GetValue(this); }
            set { s_BaseTypeProperty.SetValue(this, value); }
        }

        /// <summary>
        /// TypeCode for this type; used for built in types and internal systems
        /// </summary>
        public UTinyTypeCode TypeCode
        {
            get { return s_TypeCodeProperty.GetValue(this); }
            set { s_TypeCodeProperty.SetValue(this, value); }
        }

        /// <summary>
        /// The fields declared by this type
        /// </summary>
        public IList<UTinyField> Fields => s_FieldsProperty.GetValue(this);

        public bool IsPrimitive
        {
            get
            {
                var typeCode = TypeCode;
                return typeCode != UTinyTypeCode.Struct &&
                       typeCode != UTinyTypeCode.Component &&
                       typeCode != UTinyTypeCode.Enum &&
                       typeCode != UTinyTypeCode.Configuration;
            }
        }

        public bool IsComponent => TypeCode == UTinyTypeCode.Component;
        public bool IsStruct => TypeCode == UTinyTypeCode.Struct;
        public bool IsEnum => TypeCode == UTinyTypeCode.Enum;
        public bool IsConfiguration => TypeCode == UTinyTypeCode.Configuration;

        public object DefaultValue
        {
            get { return GetDefaultValue(); }
            set
            {
                Refresh();
                m_DefaultValueProperty.SetObjectValue(this, value);
            }
        }

        public override IPropertyBag PropertyBag => m_PropertyBag;

        public UTinyType(IRegistry registry, IVersionStorage versionStorage) : base(registry, versionStorage)
        {
            m_Fields = new List<UTinyField>();
            m_DefaultValue = new DefaultValueBackingField();
        }

        public object GetDefaultValue(bool skipTypeCheck = false)
        {
            if (!skipTypeCheck)
            {
                Refresh();
            }
            return m_DefaultValueProperty?.GetObjectValue(this);
        }

        public override void Refresh()
        {
            // Primitives are immutable
            if (IsPrimitive)
            {
                return;
            }

            // Update each field, this will make sure its type is still up to date
            var fields = Fields;
            for (var i = 0; i < fields.Count; i++)
            {
                fields[i].Refresh(Registry);
            }

            var typeCodeVersion = VersionStorage.GetVersion(s_TypeCodeProperty, this);
            var fieldsVersion = VersionStorage.GetVersion(s_FieldsProperty, this);

            if (m_DefaultValue.TypeCodeVersion != typeCodeVersion)
            {
                // Rebuild the default value property
                if (null != m_DefaultValueProperty)
                {
                    m_PropertyBag.RemoveProperty(m_DefaultValueProperty);
                }

                m_DefaultValueProperty = CreateDefaultValueProperty(TypeCode);

                if (null == m_DefaultValue.Object)
                {
                    // @TODO Overload constructor
                    m_DefaultValue.Object = new UTinyObject(Registry, (Reference) this, this, false)
                    {
                        IsDefaultValue = true
                    };
                }

                m_DefaultValue.Object.Refresh();

                if (null != m_DefaultValueProperty)
                {
                    m_PropertyBag.AddProperty(m_DefaultValueProperty);
                }
            }
            else if (!IsPrimitive && (m_DefaultValue.FieldsVersion != fieldsVersion || m_DefaultValue.ObjectVersion != m_DefaultValue.Object?.Version))
            {
                m_DefaultValue.Object?.Refresh();
            }
            else
            {
                return;
            }

            m_DefaultValue.ObjectVersion = m_DefaultValue.Object?.Version ?? -0;
            m_DefaultValue.TypeCodeVersion = typeCodeVersion;
            m_DefaultValue.FieldsVersion = fieldsVersion;
        }

        public UTinyField CreateField(string name, Reference type, bool array = false)
        {
            return CreateField(UTinyId.New(), name, type, array);
        }

        public UTinyField CreateField(UTinyId id, string name, Reference type, bool array = false)
        {
            var field = NewField(id, name, type, array);
            s_FieldsProperty.Add(this, field);
            return field;
        }
        
        private UTinyField NewField(UTinyId id, string name, Reference type, bool array = false)
        {
            Assert.IsTrue(m_TypeCode == UTinyTypeCode.Component || 
                          m_TypeCode == UTinyTypeCode.Struct || 
                          m_TypeCode == UTinyTypeCode.Enum ||
                          m_TypeCode == UTinyTypeCode.Configuration);

            var field = new UTinyField(this)
            {
                Id = id,
                Name = name,
                FieldType = type,
                Array = array,
                DeclaringType = this
            };

            return field;
        }

        public void AddField(UTinyField field)
        {
            Assert.IsFalse(s_FieldsProperty.Contains(this, field));
            Assert.IsTrue(field.DeclaringType == null);
            field.DeclaringType = this;
            s_FieldsProperty.Add(this, field);
        }

        /// <summary>
        /// Removes the given field from this type
        /// </summary>
        /// <param name="field">The field to remove</param>
        public void RemoveField(UTinyField field)
        {
            Assert.IsTrue(s_FieldsProperty.Contains(this, field));
            Assert.IsTrue(field.DeclaringType == this);
            field.DeclaringType = null;
            s_FieldsProperty.Remove(this, field);
        }

        public void InsertField(int index, UTinyField field)
        {
            Assert.IsFalse(s_FieldsProperty.Contains(this, field));
            Assert.IsTrue(field.DeclaringType == null);
            field.DeclaringType = this;
            s_FieldsProperty.Insert(this, index, field);
        }

        public UTinyField FindFieldById(UTinyId id)
        {
            return m_Fields.FirstOrDefault(field => Equals(field.Id, id));
        }

        public UTinyField FindFieldByName(string name)
        {
            return m_Fields.FirstOrDefault(field => string.Equals(field.Name, name));
        }

        public override void IncrementVersion(IProperty property, IPropertyContainer container)
        {
            Version++;

            if (container is UTinyField)
            {
                SharedVersionStorage?.IncrementVersion(s_FieldsProperty, this);
                
            }
            else if (!ReferenceEquals(container, this))
            {
                SharedVersionStorage?.IncrementVersion(m_DefaultValueProperty, this);
            }
            else
            {
                SharedVersionStorage?.IncrementVersion(property, container);
            }
        }

        private static IProperty CreateDefaultValueProperty(UTinyTypeCode typeCode)
        {
            switch (typeCode)
            {
                case UTinyTypeCode.Unknown:
                    return null;
                case UTinyTypeCode.Int8:
                    return CreateSimpleDefaultValueProperty<sbyte>();
                case UTinyTypeCode.Int16:
                    return CreateSimpleDefaultValueProperty<short>();
                case UTinyTypeCode.Int32:
                    return CreateSimpleDefaultValueProperty<int>();
                case UTinyTypeCode.Int64:
                    return CreateSimpleDefaultValueProperty<long>();
                case UTinyTypeCode.UInt8:
                    return CreateSimpleDefaultValueProperty<byte>();
                case UTinyTypeCode.UInt16:
                    return CreateSimpleDefaultValueProperty<ushort>();
                case UTinyTypeCode.UInt32:
                    return CreateSimpleDefaultValueProperty<uint>();
                case UTinyTypeCode.UInt64:
                    return CreateSimpleDefaultValueProperty<ulong>();
                case UTinyTypeCode.Float32:
                    return CreateSimpleDefaultValueProperty<float>();
                case UTinyTypeCode.Float64:
                    return CreateSimpleDefaultValueProperty<double>();
                case UTinyTypeCode.Boolean:
                    return CreateSimpleDefaultValueProperty<bool>();
                case UTinyTypeCode.Char:
                    return CreateSimpleDefaultValueProperty<char>();
                case UTinyTypeCode.String:
                    return CreateSimpleDefaultValueProperty<string>();
                case UTinyTypeCode.Configuration:
                case UTinyTypeCode.Component:
                case UTinyTypeCode.Struct:
                case UTinyTypeCode.Enum:
                    return new ContainerProperty<UTinyType, UTinyObject>("DefaultValue",
                        c => c.m_DefaultValue.Object,
                        (c, v) =>
                        {
                            if (null == v)
                            {
                                throw new NullReferenceException();
                            }

                            var obj = c.m_DefaultValue.Object;
                            obj?.CopyFrom(v);
                        });
                case UTinyTypeCode.EntityReference:
                    return CreateSimpleDefaultValueProperty<UTinyEntity.Reference>();
                case UTinyTypeCode.UnityObject:
                    return CreateSimpleDefaultValueProperty<UnityEngine.Object>();
                default:
                    throw new ArgumentOutOfRangeException(nameof(typeCode), typeCode, null);
            }
        }

        private static IProperty CreateSimpleDefaultValueProperty<TValue>()
        {
            return new Property<UTinyType, TValue>("DefaultValue",
                c => (TValue) c.m_DefaultValue.Value,
                (c, v) => c.m_DefaultValue.Value = v);
        }

        public override string ToString()
        {
            return Serialization.FlatJson.BackEnd.Persist(this);
        }

        /// <inheritdoc cref="IPropertyContainer"/>
        /// <summary>
        /// Weak reference to a type that can be serialized
        /// </summary>
        public struct Reference : IReference<UTinyType>, IPropertyContainer, IEquatable<Reference>
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

            public UTinyType Dereference(IRegistry registry)
            {
                return registry.Dereference<Reference, UTinyType>(this);
            }

            public static explicit operator Reference(UTinyType @object)
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
