#if NET_4_6
using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Properties;
using UnityEngine;
using UnityEngine.Assertions;
using Object = UnityEngine.Object;

namespace Unity.Tiny
{
    public interface IUTinyValueProperty
    {
        bool IsOverridden(IPropertyContainer container);
        bool IsDynamic { get; }
    }

    public sealed partial class UTinyObject
    {
        public class PropertiesContainer : IPropertyContainer, IVersionStorage
        {
            private static bool ValueEquals(object a, object b)
            {
                if (null == a && null == b)
                {
                    return true;
                }

                return null != a && a.Equals(b);
            }

            #region Dynamic Properties

            private static readonly Dictionary<Type, Dictionary<string, IProperty>> s_DynamicPropertyCache =
                new Dictionary<Type, Dictionary<string, IProperty>>();

            private class DynamicProperty<TValue> : Property<PropertiesContainer, TValue>, IUTinyValueProperty
            {
                public override bool IsReadOnly => false;
                public bool IsDynamic => true;

                public DynamicProperty(string name) : base(name, null, null)
                {
                }

                public override TValue GetValue(PropertiesContainer container)
                {
                    return (TValue) container.m_DynamicValues[Name];
                }

                public override void SetValue(PropertiesContainer container, TValue value)
                {
                    container.VersionStorage?.IncrementVersion(this, container);
                    container.m_DynamicValues[Name] = value;
                }

                public bool IsOverridden(IPropertyContainer container)
                {
                    return true;
                }
            }

            private class ContainerDynamicProperty<TValue> : ContainerProperty<PropertiesContainer, TValue>, IUTinyValueProperty
                where TValue : class, IPropertyContainer
            {
                public override bool IsReadOnly => false;
                public bool IsDynamic => true;

                public ContainerDynamicProperty(string name) : base(name, null, null)
                {
                }

                public override TValue GetValue(PropertiesContainer container)
                {
                    return (TValue) container.m_DynamicValues[Name];
                }

                public override void SetValue(PropertiesContainer container, TValue value)
                {
                    container.VersionStorage?.IncrementVersion(this, container);
                    container.m_DynamicValues[Name] = value;
                }

                public bool IsOverridden(IPropertyContainer container)
                {
                    return true;
                }
            }
            
            private class MutableContainerDynamicProperty<TValue> : MutableContainerProperty<PropertiesContainer, TValue>, IUTinyValueProperty
                where TValue : struct, IPropertyContainer
            {
                public override bool IsReadOnly => false;
                public bool IsDynamic => true;

                public MutableContainerDynamicProperty(string name) : base(name, null, null, null)
                {
                }

                public override TValue GetValue(PropertiesContainer container)
                {
                    return (TValue) container.m_DynamicValues[Name];
                }

                public override void SetValue(PropertiesContainer container, TValue value)
                {
                    container.VersionStorage?.IncrementVersion(this, container);
                    container.m_DynamicValues[Name] = value;
                }

                public bool IsOverridden(IPropertyContainer container)
                {
                    return true;
                }
            }
            
            private class ObjectDynamicProperty : ContainerProperty<PropertiesContainer, UTinyObject>, IUTinyValueProperty
            {
                public override bool IsReadOnly => false;
                public bool IsDynamic => true;

                public ObjectDynamicProperty(string name) : base(name, null, null)
                {
                }

                public override UTinyObject GetValue(PropertiesContainer container)
                {
                    return (UTinyObject) container.m_DynamicValues[Name];
                }

                public override void SetValue(PropertiesContainer container, UTinyObject value)
                {
                    container.VersionStorage?.IncrementVersion(this, container);
                    var obj = container.m_DynamicValues[Name] as UTinyObject;
                    obj?.CopyFrom(value);
                }

                public bool IsOverridden(IPropertyContainer container)
                {
                    return true;
                }
            }
            
            private class ListDynamicProperty : ContainerProperty<PropertiesContainer, UTinyList>, IUTinyValueProperty
            {
                public override bool IsReadOnly => false;
                public bool IsDynamic => true;

                public ListDynamicProperty(string name) : base(name, null, null)
                {
                }

                public override UTinyList GetValue(PropertiesContainer container)
                {
                    return (UTinyList) container.m_DynamicValues[Name];
                }

                public override void SetValue(PropertiesContainer container, UTinyList value)
                {
                    var obj = container.m_DynamicValues[Name] as UTinyList;
                    obj?.CopyFrom(value);
                    container.VersionStorage?.IncrementVersion(this, container);
                }

                public bool IsOverridden(IPropertyContainer container)
                {
                    return true;
                }
            }
            
            private static IProperty CreateDynamicProperty(string name, Type type)
            {
                Dictionary<string, IProperty> typedPropertyByName;
                if (!s_DynamicPropertyCache.TryGetValue(type, out typedPropertyByName))
                {
                    typedPropertyByName = new Dictionary<string, IProperty>();
                    s_DynamicPropertyCache.Add(type, typedPropertyByName);
                }

                IProperty property;
                if (typedPropertyByName.TryGetValue(name, out property))
                {
                    return property;
                }

                var typeCode = System.Type.GetTypeCode(type);

                switch (typeCode)
                {
                    case TypeCode.Byte:
                        property = new DynamicProperty<byte>(name);
                        break;
                    case TypeCode.UInt16:
                        property = new DynamicProperty<ushort>(name);
                        break;
                    case TypeCode.UInt32:
                        property = new DynamicProperty<uint>(name);
                        break;
                    case TypeCode.UInt64:
                        property = new DynamicProperty<ulong>(name);
                        break;
                    case TypeCode.SByte:
                        property = new DynamicProperty<sbyte>(name);
                        break;
                    case TypeCode.Int16:
                        property = new DynamicProperty<short>(name);
                        break;
                    case TypeCode.Int32:
                        property = new DynamicProperty<int>(name);
                        break;
                    case TypeCode.Int64:
                        property = new DynamicProperty<long>(name);
                        break;
                    case TypeCode.Single:
                        property = new DynamicProperty<float>(name);
                        break;
                    case TypeCode.Double:
                        property = new DynamicProperty<double>(name);
                        break;
                    case TypeCode.Boolean:
                        property = new DynamicProperty<bool>(name);
                        break;
                    case TypeCode.Char:
                        property = new DynamicProperty<char>(name);
                        break;
                    case TypeCode.String:
                        property = new DynamicProperty<string>(name);
                        break;
                    case TypeCode.Object:
                        if (typeof(UTinyObject) == type)
                        {
                            property = new ObjectDynamicProperty(name);
                        }
                        else if (typeof(UTinyList) == type)
                        {
                            property = new ListDynamicProperty(name);
                        }
                        else if (typeof(UTinyEntity.Reference) == type)
                        {
                            property = new MutableContainerDynamicProperty<UTinyEntity.Reference>(name);
                        }
                        else if (typeof(UTinyEnum.Reference) == type)
                        {
                            property = new MutableContainerDynamicProperty<UTinyEnum.Reference>(name);
                        }
                        else if (typeof(Texture2D) == type)
                        {
                            property = new DynamicProperty<Texture2D>(name);
                        }
                        else if (typeof(Sprite) == type)
                        {
                            property = new DynamicProperty<Sprite>(name);
                        }
                        else if (typeof(Object).IsAssignableFrom(type))
                        {
                            property = new DynamicProperty<Object>(name);
                        }
                        else
                        {
                            throw new NotSupportedException();
                        }
                        break;
                    case TypeCode.DBNull:
                    case TypeCode.Empty:
                    case TypeCode.Decimal:
                    case TypeCode.DateTime:
                        throw new NotImplementedException();
                    default:
                        throw new ArgumentOutOfRangeException();
                }

                typedPropertyByName.Add(name, property);
                return property;
            }

            #endregion

            #region Field Properties

            private interface IFieldProperty
            {
                int Index { get; set; }
            }

            private static readonly Dictionary<UTinyId, IProperty> s_FieldPropertyCache =
                new Dictionary<UTinyId, IProperty>();

            private class FieldProperty<TValue> : Property<PropertiesContainer, TValue>, IFieldProperty, IUTinyValueProperty
            {
                public int Index { get; set; }
                public override bool IsReadOnly => false;
                public bool IsDynamic => false;

                public FieldProperty(int index, string name) : base(name, null, null)
                {
                    Index = index;
                }

                public override TValue GetValue(PropertiesContainer container)
                {
                    var value = container.m_FieldValues[Index].Value;

                    if (!(value is TValue))
                    {
                        return default(TValue);
                    }

                    return (TValue) value;
                }

                public override void SetValue(PropertiesContainer container, TValue value)
                {
                    container.VersionStorage?.IncrementVersion(this, container);
                    container.m_FieldValues[Index].Value = value;
                }

                public bool IsOverridden(IPropertyContainer container)
                {
                    var c = (PropertiesContainer) container;
                    return c.m_FieldValues[Index].Overridden;
                }
            }
            
            private class ContainerFieldProperty<TValue> : ContainerProperty<PropertiesContainer, TValue>, IFieldProperty, IUTinyValueProperty
                where TValue : class, IPropertyContainer
            {
                public int Index { get; set; }
                public override bool IsReadOnly => false;
                public bool IsDynamic => false;

                public ContainerFieldProperty(int index, string name)
                    : base(name, null, null)
                {
                    Index = index;
                }

                public override TValue GetValue(PropertiesContainer container)
                {
                    var r = container.m_FieldValues[Index].Value;

                    if (null == r || typeof(TValue) != r.GetType())
                    {
                        return default(TValue);
                    }

                    return (TValue) r;
                }

                public override void SetValue(PropertiesContainer container, TValue value)
                {
                    if (ValueEquals(GetValue(container), value))
                    {
                        return;
                    }
                    
                    container.m_FieldValues[Index].Value = value;
                    container.VersionStorage?.IncrementVersion(this, container);
                }

                public bool IsOverridden(IPropertyContainer container)
                {
                    var c = (PropertiesContainer) container;
                    return c.m_FieldValues[Index].Overridden;
                }
            }
            
            private class MutableContainerFieldProperty<TValue> : MutableContainerProperty<PropertiesContainer, TValue>, IFieldProperty, IUTinyValueProperty
                where TValue : struct, IPropertyContainer
            {
                public int Index { get; set; }
                public override bool IsReadOnly => false;
                public bool IsDynamic => false;

                public MutableContainerFieldProperty(int index, string name)
                    : base(name, null, null, null)
                {
                    Index = index;
                }

                public override TValue GetValue(PropertiesContainer container)
                {
                    var r = container.m_FieldValues[Index].Value;

                    if (null == r || typeof(TValue) != r.GetType())
                    {
                        return default(TValue);
                    }

                    return (TValue) r;
                }

                public override void SetValue(PropertiesContainer container, TValue value)
                {
                    if (ValueEquals(GetValue(container), value))
                    {
                        return;
                    }
                    
                    container.m_FieldValues[Index].Value = value;
                    container.VersionStorage?.IncrementVersion(this, container);
                }

                public bool IsOverridden(IPropertyContainer container)
                {
                    var c = (PropertiesContainer) container;
                    return c.m_FieldValues[Index].Overridden;
                }
            }

            private class ObjectFieldProperty : ContainerProperty<PropertiesContainer, UTinyObject>, IFieldProperty, IUTinyValueProperty
            {
                public int Index { get; set; }
                public override bool IsReadOnly => false;
                public bool IsDynamic => false;

                public ObjectFieldProperty(int index, string name)
                    : base(name, null, null)
                {
                    Index = index;
                }

                public override UTinyObject GetValue(PropertiesContainer container)
                {
                    var value = container.m_FieldValues[Index].Value;

                    if (null == value || typeof(UTinyObject) != value.GetType())
                    {
                        return null;
                    }

                    return (UTinyObject) value;
                }

                public override void SetValue(PropertiesContainer container, UTinyObject value)
                {
                    if (null == value)
                    {
                        throw new NullReferenceException();
                    }
                    
                    if (ValueEquals(GetValue(container), value))
                    {
                        return;
                    }

                    var obj = container.m_FieldValues[Index].Value as UTinyObject;
                    obj?.CopyFrom(value);
                    container.VersionStorage?.IncrementVersion(this, container);
                }

                public bool IsOverridden(IPropertyContainer container)
                {
                    var c = (PropertiesContainer) container;
                    return c.m_FieldValues[Index].Overridden;
                }
            }

            private class ListFieldProperty : ContainerProperty<PropertiesContainer, UTinyList>, IFieldProperty, IUTinyValueProperty
            {
                public int Index { get; set; }
                public override bool IsReadOnly => false;
                public bool IsDynamic => false;

                public ListFieldProperty(int index, string name)
                    : base(name, null, null)
                {
                    Index = index;
                }

                public override UTinyList GetValue(PropertiesContainer container)
                {
                    var value = container.m_FieldValues[Index].Value;

                    if (null == value || typeof(UTinyList) != value.GetType())
                    {
                        return null;
                    }

                    return (UTinyList) value;
                }

                public override void SetValue(PropertiesContainer container, UTinyList value)
                {
                    if (null == value)
                    {
                        throw new NullReferenceException();
                    }

                    var obj = container.m_FieldValues[Index].Value as UTinyList;
                    obj?.CopyFrom(value);
                    container.VersionStorage?.IncrementVersion(this, container);
                }

                public bool IsOverridden(IPropertyContainer container)
                {
                    var c = (PropertiesContainer) container;
                    return c.m_FieldValues[Index].Overridden;
                }
            }

            private static IProperty CreateFieldProperty(UTinyId fieldId, int index, string name, UTinyType type, bool array)
            {
                IProperty property;
                s_FieldPropertyCache.TryGetValue(fieldId, out property);

                if (array)
                {
                    property = property is ListFieldProperty && property.Name.Equals(name) ? property : new ListFieldProperty(index, name);
                }
                else
                {
                    switch (type.TypeCode)
                    {
                        case UTinyTypeCode.Unknown:
                            break;
                        case UTinyTypeCode.Int8:
                            property = property is FieldProperty<sbyte> && property.Name.Equals(name)
                                ? property
                                : new FieldProperty<sbyte>(index, name);
                            break;
                        case UTinyTypeCode.Int16:
                            property = property is FieldProperty<short> && property.Name.Equals(name)
                                ? property
                                : new FieldProperty<short>(index, name);
                            break;
                        case UTinyTypeCode.Int32:
                            property = property is FieldProperty<int> && property.Name.Equals(name)
                                ? property
                                : new FieldProperty<int>(index, name);
                            break;
                        case UTinyTypeCode.Int64:
                            property = property is FieldProperty<long> && property.Name.Equals(name)
                                ? property
                                : new FieldProperty<long>(index, name);
                            break;
                        case UTinyTypeCode.UInt8:
                            property = property is FieldProperty<byte> && property.Name.Equals(name)
                                ? property
                                : new FieldProperty<byte>(index, name);
                            break;
                        case UTinyTypeCode.UInt16:
                            property = property is FieldProperty<ushort> && property.Name.Equals(name)
                                ? property
                                : new FieldProperty<ushort>(index, name);
                            break;
                        case UTinyTypeCode.UInt32:
                            property = property is FieldProperty<uint> && property.Name.Equals(name)
                                ? property
                                : new FieldProperty<uint>(index, name);
                            break;
                        case UTinyTypeCode.UInt64:
                            property = property is FieldProperty<ulong> && property.Name.Equals(name)
                                ? property
                                : new FieldProperty<ulong>(index, name);
                            break;
                        case UTinyTypeCode.Float32:
                            property = property is FieldProperty<float> && property.Name.Equals(name)
                                ? property
                                : new FieldProperty<float>(index, name);
                            break;
                        case UTinyTypeCode.Float64:
                            property = property is FieldProperty<double> && property.Name.Equals(name)
                                ? property
                                : new FieldProperty<double>(index, name);
                            break;
                        case UTinyTypeCode.Boolean:
                            property = property is FieldProperty<bool> && property.Name.Equals(name)
                                ? property
                                : new FieldProperty<bool>(index, name);
                            break;
                        case UTinyTypeCode.Char:
                            property = property is FieldProperty<char> && property.Name.Equals(name)
                                ? property
                                : new FieldProperty<char>(index, name);
                            break;
                        case UTinyTypeCode.String:
                            property = property is FieldProperty<string> && property.Name.Equals(name)
                                ? property
                                : new FieldProperty<string>(index, name);
                            break;
                        case UTinyTypeCode.Configuration:
                        case UTinyTypeCode.Component:
                            throw new NotSupportedException();
                        case UTinyTypeCode.Struct:
                            property = property is ObjectFieldProperty && property.Name.Equals(name)
                                ? property
                                : new ObjectFieldProperty(index, name);
                            break;
                        case UTinyTypeCode.Enum:
                            property =
                                property is MutableContainerFieldProperty<UTinyEnum.Reference> &&
                                property.Name.Equals(name)
                                    ? property
                                    : new MutableContainerFieldProperty<UTinyEnum.Reference>(index, name);
                            break;
                        case UTinyTypeCode.EntityReference:
                            property = property is MutableContainerFieldProperty<UTinyEntity.Reference> && property.Name.Equals(name)
                                ? property
                                : new MutableContainerFieldProperty<UTinyEntity.Reference>(index, name);
                            break;
                        case UTinyTypeCode.UnityObject:
                            if (type.Id == UTinyType.Texture2DEntity.Id)
                            {
                                property = property is FieldProperty<Texture2D> && property.Name.Equals(name)
                                    ? property
                                    : new FieldProperty<Texture2D>(index, name);
                            }
                            else if (type.Id == UTinyType.SpriteEntity.Id)
                            {
                                property = property is FieldProperty<Sprite> && property.Name.Equals(name)
                                    ? property
                                    : new FieldProperty<Sprite>(index, name);
                            }
                            else if (type.Id == UTinyType.AudioClipEntity.Id)
                            {
                                property = property is FieldProperty<AudioClip> && property.Name.Equals(name) ? property : new FieldProperty<AudioClip>(index, name);
                            }
                            else if (type.Id == UTinyType.FontEntity.Id)
                            {
                                property = property is FieldProperty<Font> && property.Name.Equals(name)
                                    ? property
                                    : new FieldProperty<Font>(index, name);
                            }
                            else
                            {
                                property = property is FieldProperty<Object> && property.Name.Equals(name)
                                    ? property
                                    : new FieldProperty<Object>(index, name);
                            }

                            break;
                        default:
                            throw new ArgumentOutOfRangeException();
                    }
                }

                var fieldProperty = property as IFieldProperty;
                if (null != fieldProperty)
                {
                    fieldProperty.Index = index;
                }

                s_FieldPropertyCache[fieldId] = property;

                return property;
            }

            #endregion

            private class FieldValue
            {
                /// <summary>
                /// The field Id that this value maps to
                /// NOTE: This is to survive field renaming
                /// </summary>
                public UTinyId Id;

                /// <summary>
                /// Raw value (untyped)
                /// </summary>
                public object Value;

                /// <summary>
                /// Is this value in it's default state
                /// NOTE: This is only ever true for values derived from a UTinyType
                /// </summary>
                public bool Overridden;
            }

            private readonly UTinyObject m_Object;
            private readonly List<FieldValue> m_FieldValues;
            private Dictionary<string, object> m_DynamicValues;

            public UTinyObject ParentObject => m_Object;

            /// <summary>
            /// Each dynamic object will have it's own PropertyBag instance (no sharing or re-use)
            /// </summary>
            private readonly PropertyBag m_PropertyBag;

            public IVersionStorage VersionStorage => this;
            public IPropertyBag PropertyBag => m_PropertyBag;

            /// <summary>
            /// Returns true if any values have been explicitly overridden
            /// </summary>
            public bool IsOverridden
            {
                get
                {
                    if (m_DynamicValues?.Count > 0)
                    {
                        return true;
                    }
                    
                    foreach (var value in m_FieldValues)
                    {
                        if (value.Overridden)
                        {
                            return true;
                        }
                    }

                    return false;
                }
            }

            /// <summary>
            /// Initializes a new PropertiesContainer
            /// </summary>
            /// <param name="object">The host class for this instance</param>
            public PropertiesContainer(UTinyObject @object)
            {
                m_Object = @object;
                m_PropertyBag = new PropertyBag();
                m_FieldValues = new List<FieldValue>();
            }

            /// <summary>
            /// This method does a lot of magic
            /// You can associate ANY value with a name and the correct property will be generated for you
            /// </summary>
            public object this[string key]
            {
                get
                {
                    // Incurs a dictionary lookup
                    var property = m_PropertyBag.FindProperty(key);

                    // Must asset to avoid undefined behaviour
                    Assert.IsNotNull(property, $"Property '{key}' does not exist on object");
                    var container = (IPropertyContainer) this;

                    // This works well since the implementation details are abstracted
                    // i.e. We don't care how the value is unpacked (List, Dictionary, NativeArray etc)
                    return property.GetObjectValue(container);
                }
                set
                {
                    // Incurs a dictionary lookup
                    var property = m_PropertyBag.FindProperty(key);

                    if (null == property)
                    {
                        // Auto-generate a dynamic property for the user
                        var type = value?.GetType() ?? typeof(object);
                        property = CreateDynamicProperty(key, type);
                        Assert.IsNotNull(property);
                        m_PropertyBag.AddProperty(property);

                        if (null == m_DynamicValues)
                        {
                            m_DynamicValues = new Dictionary<string, object>();
                        }

                        // Setup the underlying storage
                        // This breaks our abstraction but must be done at some point
                        if (typeof(UTinyObject) == type)
                        {
                            var obj = new UTinyObject(m_Object.Registry, UTinyType.Reference.None);
                            obj.CopyFrom(value as UTinyObject);
                            m_DynamicValues.Add(key, obj);

                        }
                        else if (typeof(UTinyList) == type)
                        {
                            var obj = new UTinyList(m_Object.Registry, UTinyType.Reference.None);
                            obj.CopyFrom(value as UTinyList);
                            m_DynamicValues.Add(key, obj);
                        }
                        else
                        {
                            m_DynamicValues.Add(key, value);
                        }
                    }
                    else
                    {
                        // @TODO There is an unhandled case here when we encounter a type mis-match, we need to detect this and throw
                        try
                        {
                            property.SetObjectValue(this, value);
                        }
                        catch (InvalidCastException)
                        {
                            Debug.LogError($"Could not cast {value.GetType()} to {property.ValueType}. Value is '{value}'.");
                            throw;
                        }
                    }
                }
            }

            public void SetOverridden(string key, bool overridden)
            {
                var index = (m_PropertyBag.FindProperty(key) as IFieldProperty)?.Index ?? -1;
                if (index >= 0)
                {
                    m_FieldValues[index].Overridden = overridden;
                }
            }

            public bool HasProperty(string key)
            {
                return m_PropertyBag.FindProperty(key) != null;
            }

            public void RemoveProperty(string key)
            {
                m_DynamicValues.Remove(key);
                m_PropertyBag.RemoveProperty(m_PropertyBag.FindProperty(key));
            }

            public void Reset(UTinyType type, UTinyObject defaultValue)
            {
                if (null == type)
                {
                    return;
                }

                // The default value for this type
                var typeDefaultValue = !m_Object.IsDefaultValue ? defaultValue ?? type.DefaultValue as UTinyObject : null;

                var fields = type.Fields;
                for (var i = 0; i < fields.Count; i++)
                {
                    var field = fields[i];
                    var fieldType = field.FieldType.Dereference(m_Object.Registry);
                    var fieldValue = m_FieldValues[i];
                    fieldValue.Overridden = false;

                    // The default value for this field
                    var fieldDefaultValue = typeDefaultValue?[field.Name];

                    if (fieldType.IsPrimitive || fieldType.IsEnum)
                    {
                        fieldValue.Value = fieldDefaultValue;
                    }
                    else
                    {
                        (fieldValue.Value as UTinyObject)?.Reset(fieldDefaultValue as UTinyObject);
                    }
                }
            }

            public void Refresh(UTinyType type, UTinyObject defaultValue, bool skipTypeCheck = false)
            {
                Assert.IsNotNull(type);

                var fields = type.Fields;

                Assert.IsTrue(type.TypeCode == UTinyTypeCode.Struct ||
                              type.TypeCode == UTinyTypeCode.Component ||
                              type.TypeCode == UTinyTypeCode.Enum ||
                              type.TypeCode == UTinyTypeCode.Configuration);

                // Rebuild all fields and re-map the indicies correctly
                MigrateFields(fields, m_FieldValues);

                // Dynamically rebuild the property bag                
                m_PropertyBag.Clear();

                // Migrate dynamic values
                if (null != m_DynamicValues)
                {
                    for (var i = 0; i < fields.Count; i++)
                    {
                        object dynamicValue;
                        if (!m_DynamicValues.TryGetValue(fields[i].Name, out dynamicValue))
                        {
                            continue;
                        }

                        m_FieldValues[i].Value = dynamicValue;
                        m_FieldValues[i].Overridden = true;
                    }

                    m_DynamicValues = null;
                }

                for (var i = 0; i < fields.Count; i++)
                {
                    var field = fields[i];
                    var fieldName = field.Name;
                    var fieldType = field.FieldType.Dereference(type.Registry);
                    var fieldValue = m_FieldValues[i];

                    if (null == fieldType)
                    {
                        continue;
                    }

                    // Force the field type to be refreshed if needed
                    if (!skipTypeCheck)
                    {
                        fieldType.Refresh();
                    }

                    // The default value for this field
                    var fieldDefaultValue = defaultValue?[fieldName];

                    if (!fieldValue.Overridden && fieldType.IsPrimitive && !field.Array)
                    {
                        fieldValue.Value = fieldDefaultValue;
                    }
                    else
                    {
                        fieldValue.Value = MigrateFieldValue(m_Object.Registry, this, fieldValue.Value, fieldType, field.Array, fieldDefaultValue);
                    }

                    // @HACK 
                    if (fieldValue.Value is UTinyObject)
                    {
                        (fieldValue.Value as UTinyObject).Name = fieldName;
                    }
                    else if (fieldValue.Value is UTinyList)
                    {
                        (fieldValue.Value as UTinyList).Name = fieldName;
                    }

                    m_PropertyBag.AddProperty(CreateFieldProperty(field.Id, i, fieldName, fieldType, field.Array));

                    IncrementVersion(null, this);
                }
            }

            private static void MigrateFields(IList<UTinyField> fields, List<FieldValue> fieldValues)
            {
                var fieldsCount = fields.Count;
                var dataListCount = fieldValues.Count;

                if (dataListCount == 0)
                {
                    fieldValues.Capacity = fieldsCount;
                }

                for (var f = 0; f < fieldsCount; f++)
                {
                    var fieldId = fields[f].Id;

                    var p = f;

                    // We have no guarantee that the field order matches our property order (i.e. We assume the user can re-order field definitions)
                    for (; p < dataListCount; p++)
                    {
                        if (fieldId != fieldValues[p].Id)
                        {
                            continue;
                        }

                        break;
                    }

                    // The property was not found; this is a new field that was added, create a new corresponding property
                    if (p >= dataListCount)
                    {
                        fieldValues.Add(new FieldValue {Id = fieldId, Value = null, Overridden = false});
                        dataListCount++;
                    }

                    // This property exists and is sorted
                    if (p == f)
                    {
                        continue;
                    }

                    // Swap the property in to its correct place
                    var fieldValue = fieldValues[f];
                    fieldValues[f] = fieldValues[p];
                    fieldValues[p] = fieldValue;
                }

                // Remove any excess properties
                if (dataListCount > fieldsCount)
                {
                    fieldValues.RemoveRange(fields.Count, dataListCount - fieldsCount);
                }
            }

            public static object MigrateFieldValue(IRegistry registry, IVersionStorage versionStorage, object value, UTinyType type, bool array, object defaultValue = null, bool skipTypeCheck = false)
            {
                if (array)
                {
                    var list = value as UTinyList;
                    if (null == list)
                    {
                        list = new UTinyList(registry, versionStorage)
                        {
                            Type = (UTinyType.Reference) type
                        };
                        list.Refresh(defaultValue as UTinyList, skipTypeCheck);
                    }
                    else
                    {
                        list.Type = (UTinyType.Reference) type;
                        list.Refresh(defaultValue as UTinyList, skipTypeCheck);
                    }

                    return list;
                }

                switch (type.TypeCode)
                {
                    case UTinyTypeCode.Unknown:
                        break;
                    case UTinyTypeCode.Int8:
                        return TryChangeType<sbyte>(value);
                    case UTinyTypeCode.Int16:
                        return TryChangeType<short>(value);
                    case UTinyTypeCode.Int32:
                        return TryChangeType<int>(value);
                    case UTinyTypeCode.Int64:
                        return TryChangeType<long>(value);
                    case UTinyTypeCode.UInt8:
                        return TryChangeType<byte>(value);
                    case UTinyTypeCode.UInt16:
                        return TryChangeType<ushort>(value);
                    case UTinyTypeCode.UInt32:
                        return TryChangeType<uint>(value);
                    case UTinyTypeCode.UInt64:
                        return TryChangeType<ulong>(value);
                    case UTinyTypeCode.Float32:
                        return TryChangeType<float>(value);
                    case UTinyTypeCode.Float64:
                        return TryChangeType<double>(value);
                    case UTinyTypeCode.Boolean:
                        return TryChangeType<bool>(value);
                    case UTinyTypeCode.Char:
                        return TryChangeType<char>(value);
                    case UTinyTypeCode.String:
                        return TryChangeType<string>(value) ?? string.Empty;
                    case UTinyTypeCode.Configuration:
                    case UTinyTypeCode.Component:
                        // Components can not be fields, they can only exist at the entity level
                        throw new NotSupportedException();
                    case UTinyTypeCode.Struct:
                    {
                        var obj = value as UTinyObject;
                        if (null == obj)
                        {
                            obj = new UTinyObject(registry, (UTinyType.Reference) type, versionStorage, false);
                            obj.Refresh(defaultValue as UTinyObject, skipTypeCheck);
                        }
                        else
                        {
                            obj.Type = (UTinyType.Reference) type;
                            obj.Refresh(defaultValue as UTinyObject, skipTypeCheck);
                        }

                        return obj;
                    }

                    case UTinyTypeCode.Enum:
                    {
                        if (value is UTinyEnum.Reference)
                        {
                            return new UTinyEnum.Reference(type, ((UTinyEnum.Reference) value).Id);
                        }

                        return defaultValue is UTinyEnum.Reference
                            ? new UTinyEnum.Reference(type, ((UTinyEnum.Reference) defaultValue).Id)
                            : new UTinyEnum.Reference(type, type.Fields.First().Id);
                    }

                    case UTinyTypeCode.EntityReference:
                    {
                        if (value is UTinyEntity.Reference)
                        {
                            return value;
                        }

                        return defaultValue is UTinyEntity.Reference
                            ? defaultValue
                            : UTinyEntity.Reference.None;
                    }
                    case UTinyTypeCode.UnityObject:
                    {
                        if (value is Object)
                        {
                            return value;
                        }

                        return defaultValue is Object
                            ? defaultValue
                            : null;
                    }
                    default:
                        throw new ArgumentOutOfRangeException();
                }

                return null;
            }

            private static object TryChangeType<T>(object value)
            {
                if (!(value is IConvertible))
                {
                    return default(T);
                }

                try
                {
                    return Convert.ChangeType(value, typeof(T));
                }
                catch
                {
                    // ignored
                }

                return default(T);
            }

            public int GetVersion(IProperty property, IPropertyContainer container)
            {
                // @TODO?
                return -1;
            }

            public void IncrementVersion(IProperty property, IPropertyContainer container)
            {
                var fieldProperty = property as IFieldProperty;

                if (null != fieldProperty)
                {
                    // One of our direct properties has Overridden
                    m_FieldValues[fieldProperty.Index].Overridden = true;
                }
                else
                {
                    // A property of one of our sub objects has Overridden
                    for (var i = 0; i < m_FieldValues.Count; i++)
                    {
                        var fieldValue = m_FieldValues[i];
                        if (ReferenceEquals(container, fieldValue.Value))
                        {
                            fieldValue.Overridden = true;
                        }
                    }
                }

                // @NOTE We can version our own values here

                // Propagate version change up the tree
                var c = (IPropertyContainer) m_Object;
                m_Object.VersionStorage.IncrementVersion(s_PropertiesProperty, c);
            }
        }
    }
}
#endif // NET_4_6
