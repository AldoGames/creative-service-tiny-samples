#if NET_4_6
using System;
using System.Linq;
using Unity.Properties;
using UnityEngine.Assertions;
using Unity.Tiny.Attributes;
using static Unity.Tiny.Attributes.InspectorAttributes;

namespace Unity.Tiny
{
    public static class UTinyEnum
    {
        public struct Reference : IPropertyContainer, IEquatable<Reference>
        {
            private static readonly StructEnumProperty<Reference, UTinyTypeId> s_TypeIdProperty = new StructEnumProperty<Reference, UTinyTypeId>("$TypeId",
                (ref Reference c) => UTinyTypeId.EnumReference,
                null
            ).WithAttribute(HideInInspector)
             .WithAttribute(Readonly); 

            private static readonly StructProperty<Reference, UTinyType.Reference> s_TypeProperty = new StructProperty<Reference, UTinyType.Reference>("Type",
                (ref Reference c) => c.m_Type,
                (ref Reference c, UTinyType.Reference v) => c.m_Type = v
            );

            private static readonly StructProperty<Reference, UTinyId> s_IdProperty = new StructProperty<Reference, UTinyId>("Id",
                (ref Reference c) => c.m_Id,
                (ref Reference c, UTinyId v) => c.m_Id = v
            );
            
            private static readonly StructProperty<Reference, string> s_NameProperty = new StructProperty<Reference, string>("Name",
                (ref Reference c) => c.m_Name,
                (ref Reference c, string v) => c.m_Name = v
            );

            private static readonly StructProperty<Reference, int> s_ValueProperty = new StructProperty<Reference, int>("Value",
                (ref Reference c) => c.m_Value,
                (ref Reference c, int v) => c.m_Value = v
            );

            private static readonly IPropertyBag s_PropertyBag = new PropertyBag(
                s_TypeIdProperty, 
                s_TypeProperty, 
                s_IdProperty, 
                s_NameProperty, 
                s_ValueProperty);
            
            public static Reference None { get; } = new Reference();

            private UTinyType.Reference m_Type;
            private UTinyId m_Id;
            private string m_Name;
            private int m_Value;

            public UTinyType.Reference Type => s_TypeProperty.GetValue(ref this);
            
            public UTinyId Id => s_IdProperty.GetValue(ref this);
            public string Name => s_NameProperty.GetValue(ref this);
            public int Value => s_ValueProperty.GetValue(ref this);

            public IPropertyBag PropertyBag => s_PropertyBag;
            public IVersionStorage VersionStorage => PassthroughVersionStorage.Instance;

            public Reference(UTinyType.Reference type, UTinyId id, string name, int value)
            {
                m_Type = type;
                m_Id = id;
                m_Name = name;
                m_Value = value;
            }

            public Reference(UTinyType type, int value)
            {
                Assert.IsNotNull(type);
                Assert.IsTrue(type.IsEnum);

                var defaultValue = type.DefaultValue as UTinyObject;

                UTinyField field;
                if (null != defaultValue)
                {
                    var name = string.Empty;
                    var container = (IPropertyContainer) defaultValue.Properties;
                    foreach (var property in defaultValue.Properties.PropertyBag.Properties)
                    {
                        var propertyValue = property.GetObjectValue(container);
                        if (!value.Equals(propertyValue))
                        {
                            continue;
                        }
                        name = property.Name;
                        break;
                    }
                    field = type.FindFieldByName(name);
                }
                else
                {
                    field = type.Fields.FirstOrDefault();
                }

                m_Type = (UTinyType.Reference) type;
                m_Id = field?.Id ?? UTinyId.Empty;
                m_Name = field?.Name ?? string.Empty;
                m_Value = null != field ? (int?) defaultValue?[m_Name] ?? 0 : 0;
            }

            public Reference(UTinyType type, string name)
            {
                Assert.IsNotNull(type);
                Assert.IsTrue(type.IsEnum);

                var defaultValue = type.DefaultValue as UTinyObject;
                var field = type.FindFieldByName(name);

                m_Type = (UTinyType.Reference) type;
                m_Id = field?.Id ?? UTinyId.Empty;
                m_Name = field?.Name ?? string.Empty;
                m_Value = null != field ? (int?) defaultValue?[m_Name] ?? 0 : 0;
            }
            
            public Reference(UTinyType type, UTinyId fieldId)
            {
                Assert.IsNotNull(type);
                Assert.IsTrue(type.IsEnum);

                var defaultValue = type.DefaultValue as UTinyObject;
                var field = type.FindFieldById(fieldId);

                m_Type = (UTinyType.Reference) type;
                m_Id = field?.Id ?? UTinyId.Empty;
                m_Name = field?.Name ?? string.Empty;
                m_Value = null != field ? (int?) defaultValue?[m_Name] ?? 0 : 0;
            }
            
            public bool Equals(Reference other)
            {
                return m_Type.Equals(other.m_Type) && m_Id.Equals(other.m_Id);
            }

            public override bool Equals(object obj)
            {
                if (ReferenceEquals(null, obj)) return false;
                return obj is Reference && Equals((Reference) obj);
            }

            public override int GetHashCode()
            {
                unchecked
                {
                    return (m_Type.GetHashCode() * 397) ^ m_Id.GetHashCode();
                }
            }
        }
    }
}
#endif // NET_4_6
