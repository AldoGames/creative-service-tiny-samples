#if NET_4_6
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Properties;
using UnityEngine;
using Unity.Tiny.Attributes;
using Unity.Tiny.Serialization.FlatJson;
using UnityEngine.Assertions;
using Object = UnityEngine.Object;

namespace Unity.Tiny
{
    public sealed class UTinyList : IPropertyContainer, IVersionStorage, IVersioned, IEnumerable
    {
        private readonly IRegistry m_Registry;
        public int Version { get; private set; }
        private readonly IVersionStorage m_SharedVersionStorage;

        private int m_CurrentTypeVersion;
        private UTinyType.Reference m_Type;

        private static readonly MutableContainerProperty<UTinyList, UTinyType.Reference> s_TypeProperty =
            new MutableContainerProperty<UTinyList, UTinyType.Reference>("Type",
                /* GET */ c => c.m_Type,
                /* SET */ (c, v) =>
                {
                    c.m_Type = v;
                    c.m_CurrentTypeVersion = -1;
                },
                /* REF */ (c, a, v) => a(ref c.m_Type, v)
            ).WithAttribute(InspectorAttributes.HideInInspector);

        public UTinyType.Reference Type
        {
            get { return s_TypeProperty.GetValue(this); }
            set { s_TypeProperty.SetValue(this, value); }
        }

        private object m_Items;
        private IListProperty m_ItemsProperty;

        private readonly PropertyBag m_PropertyBag = new PropertyBag(
            s_TypeProperty
        );

        public IPropertyBag PropertyBag => m_PropertyBag;
        public IVersionStorage VersionStorage => this;

        public int Count => m_ItemsProperty.Count(this);

        public string Name { get; set; }

        public object this[int index]
        {
            get { return m_ItemsProperty.GetObjectAt(this, index); }
            set { m_ItemsProperty.SetObjectAt(this, index, value); }
        }

        public UTinyList(IRegistry registry, IVersionStorage versionStorage = null)
        {
            m_Registry = registry;
            m_SharedVersionStorage = versionStorage;
        }

        public UTinyList(IRegistry registry, UTinyType.Reference type, IVersionStorage versionStorage = null) : this(registry, versionStorage)
        {
            m_Registry = registry;
            m_Type = type;
            m_SharedVersionStorage = versionStorage;
            Refresh();
        }

        public IEnumerator GetEnumerator()
        {
            return ((IList)m_Items)?.GetEnumerator() ?? Enumerable.Empty<IList>().GetEnumerator();
        }

        public int GetVersion(IProperty property, IPropertyContainer container)
        {
            return m_SharedVersionStorage?.GetVersion(property, container) ?? 0;
        }

        public void IncrementVersion(IProperty property, IPropertyContainer container)
        {
            Version++;
            m_SharedVersionStorage?.IncrementVersion(m_ItemsProperty, this);
        }

        /// <summary>
        /// Adds an item to the list
        ///
        /// * If the list has no type the type will be infered from the given object
        /// * If the list has no type and `null` is added the type will be set as `object`
        /// </summary>
        /// <param name="obj"></param>
        /// <exception cref="Exception">If the given object is not assignable to the list type</exception>
        public void Add(object obj)
        {
            if (null == m_Items)
            {
                // Special case when adding an element and we have no items
                // Dynamically create the list and properties to be strongly typed. We must use activator in this situation
                var type = obj?.GetType() ?? typeof(object);
                m_Items = Activator.CreateInstance(typeof(List<>).MakeGenericType(type));
                m_ItemsProperty = CreateItemsProperty(type);
                m_PropertyBag.AddProperty(m_ItemsProperty);
            }
            
            if (obj is UTinyObject)
            {
                // Special case for tiny object. We DON'T want to retain the given instance.
                // Instead we create a new object and deep copy the values in. This way the object
                // Will propegate version changes to this list
                var v = new UTinyObject(m_Registry, m_Type, this);
                v.CopyFrom((UTinyObject) obj);
                var typedList = (IListProperty<UTinyList, UTinyObject>) m_ItemsProperty;
                typedList.Add(this, v);
            }
            else
            {
                try
                {
                    var converted = Convert(obj, m_ItemsProperty.ItemType);
                    m_ItemsProperty.AddObject(this, converted);
                }
                catch (Exception e)
                {
                    throw new Exception($"UTinyList.Add Type mismatch expected instance of Type=[{m_ItemsProperty.ItemType}] received Type=[{obj?.GetType()}]", e);
                }
            }
        }
        
        // TODO: this belongs in the Property API
        private static object Convert(object v, Type toType)
        {
            if (ReferenceEquals(v, null))
            {
                return toType.IsClass ? null : Activator.CreateInstance(toType);
            }

            if (v is UnityEngine.Object)
            {
                // handle fake nulls
                var uObj = (UnityEngine.Object)v;
                if (!uObj)
                {
                    Assert.IsTrue(toType.IsClass);
                    return null;
                }
            }
            
            if (v.GetType() == toType)
            {
                return v;
            }

            return System.Convert.ChangeType(v, toType);
        }

        public void RemoveAt(int index)
        {
            m_ItemsProperty?.RemoveAt(this, index);
        }

        public void Clear()
        {
            m_ItemsProperty?.Clear(this);
        }

        public void Refresh(UTinyList defaultValue = null, bool skipTypeCheck = false)
        {
            var type = Type.Dereference(m_Registry);

            if (null == type)
            {
                return;
            }

            // Force the type to be refreshed
            if (!skipTypeCheck)
            {
                type.Refresh();
            }

            if (m_CurrentTypeVersion == type.Version)
            {
                return;
            }

            // Migrate the values
            m_Items = MigrateListValue(m_Registry, this, m_Items as IList, type);

            // Rebuild the default value property
            if (null != m_ItemsProperty)
            {
                m_PropertyBag.RemoveProperty(m_ItemsProperty);
            }

            m_ItemsProperty = CreateItemsProperty(type);

            if (null != m_ItemsProperty)
            {
                m_PropertyBag.AddProperty(m_ItemsProperty);
            }

            m_CurrentTypeVersion = type.Version;
        }

        private static IList MigrateListValue(IRegistry registry, IVersionStorage version, IList value, UTinyType type)
        {
            var result = UTinyType.CreateListInstance(type);
            for (var i = 0; i < value?.Count; i++)
            {
                result.Add(UTinyObject.PropertiesContainer.MigrateFieldValue(registry, version, value[i], type, false));
            }

            return result;
        }
        
        private IListProperty CreateItemsProperty(Type type)
        {
            var typeCode = System.Type.GetTypeCode(type);
            
            switch (typeCode)
            {
                case TypeCode.Empty:
                case TypeCode.Decimal:                
                case TypeCode.DateTime:
                    break;
                case TypeCode.SByte:
                    return CreateSimpleListProperty<sbyte>();
                case TypeCode.Int16:
                    return CreateSimpleListProperty<short>();
                case TypeCode.Int32:
                    return CreateSimpleListProperty<int>();
                case TypeCode.Int64:
                    return CreateSimpleListProperty<long>();
                case TypeCode.Byte:
                    return CreateSimpleListProperty<byte>();
                case TypeCode.UInt16:
                    return CreateSimpleListProperty<ushort>();
                case TypeCode.UInt32:
                    return CreateSimpleListProperty<uint>();
                case TypeCode.UInt64:
                    return CreateSimpleListProperty<ulong>();
                case TypeCode.Single:
                    return CreateSimpleListProperty<float>();
                case TypeCode.Double:
                    return CreateSimpleListProperty<double>();
                case TypeCode.Boolean:
                    return CreateSimpleListProperty<bool>();
                case TypeCode.Char:
                    return CreateSimpleListProperty<char>();
                case TypeCode.String:
                    return CreateSimpleListProperty<string>();
                case TypeCode.Object:
                {
                    if (typeof(UTinyObject) == type)
                    {
                        return CreateContainerListProperty<UTinyObject>();
                    }
                    
                    if (typeof(UTinyEntity.Reference) == type)
                    {
                        return CreateMutableContainerListProperty<UTinyEntity.Reference>();
                    }
                    
                    if (typeof(UTinyEnum.Reference) == type)
                    {
                        return CreateMutableContainerListProperty<UTinyEnum.Reference>();
                    }

                    if (typeof(Texture2D).IsAssignableFrom(type))
                    {
                        return CreateSimpleListProperty<Texture2D>();
                    }
                    
                    if (typeof(Sprite).IsAssignableFrom(type))
                    {
                        return CreateSimpleListProperty<Sprite>();
                    }

                    if (typeof(AudioClip).IsAssignableFrom(type))
                    {
                        return CreateSimpleListProperty<AudioClip>();
                    }

                    if (typeof(Font).IsAssignableFrom(type))
                    {
                        return CreateSimpleListProperty<Font>();
                    }

                    if (typeof(Object).IsAssignableFrom(type))
                    {
                        return CreateSimpleListProperty<Object>();
                    }
                    
                    return CreateSimpleListProperty<object>();
                }
                default:
                    throw new ArgumentOutOfRangeException(nameof(typeCode), typeCode, null);
            }

            return null;
        }

        private IListProperty CreateItemsProperty(UTinyType type)
        {
            switch (type.TypeCode)
            {
                case UTinyTypeCode.Unknown:
                    break;
                case UTinyTypeCode.Int8:
                    return CreateSimpleListProperty<sbyte>();
                case UTinyTypeCode.Int16:
                    return CreateSimpleListProperty<short>();
                case UTinyTypeCode.Int32:
                    return CreateSimpleListProperty<int>();
                case UTinyTypeCode.Int64:
                    return CreateSimpleListProperty<long>();
                case UTinyTypeCode.UInt8:
                    return CreateSimpleListProperty<byte>();
                case UTinyTypeCode.UInt16:
                    return CreateSimpleListProperty<ushort>();
                case UTinyTypeCode.UInt32:
                    return CreateSimpleListProperty<uint>();
                case UTinyTypeCode.UInt64:
                    return CreateSimpleListProperty<ulong>();
                case UTinyTypeCode.Float32:
                    return CreateSimpleListProperty<float>();
                case UTinyTypeCode.Float64:
                    return CreateSimpleListProperty<double>();
                case UTinyTypeCode.Boolean:
                    return CreateSimpleListProperty<bool>();
                case UTinyTypeCode.Char:
                    return CreateSimpleListProperty<char>();
                case UTinyTypeCode.String:
                    return CreateSimpleListProperty<string>();
                case UTinyTypeCode.Configuration:
                case UTinyTypeCode.Component:
                case UTinyTypeCode.Struct:
                {
                    return CreateContainerListProperty<UTinyObject>();
                }
                case UTinyTypeCode.Enum:
                    return CreateMutableContainerListProperty<UTinyEnum.Reference>();
                case UTinyTypeCode.EntityReference:
                    return CreateSimpleListProperty<UTinyEntity.Reference>();
                case UTinyTypeCode.UnityObject:

                    if (type.Id == UTinyType.Texture2DEntity.Id)
                    {
                        return CreateSimpleListProperty<Texture2D>();
                    }
                    else if (type.Id == UTinyType.SpriteEntity.Id)
                    {
                        return CreateSimpleListProperty<Sprite>();
                    }
                    else if (type.Id == UTinyType.AudioClipEntity.Id)
                    {
                        return CreateSimpleListProperty<AudioClip>();
                    }
                    else if (type.Id == UTinyType.FontEntity.Id)
                    {
                            
                        return CreateSimpleListProperty<Font>();
                    }
                    else
                    {
                        return CreateSimpleListProperty<Object>();
                    }
                    
                default:
                    throw new ArgumentOutOfRangeException(nameof(type.TypeCode), type.TypeCode, null);
            }

            return null;
        }

        private IListProperty CreateSimpleListProperty<TValue>()
        {
            return new ListProperty<UTinyList, IList<TValue>, TValue>("Items",
                container => container.m_Items as IList<TValue>,
                null,
                list => default(TValue));
        }
        
        private IListProperty CreateContainerListProperty<TValue>() 
            where TValue : class, IPropertyContainer
        {
            return new ContainerListProperty<UTinyList, IList<TValue>, TValue>("Items",
                container => container.m_Items as IList<TValue>,
                null,
                list => new UTinyObject(m_Registry, m_Type, this) as TValue);
        }
        
        private IListProperty CreateMutableContainerListProperty<TValue>() 
            where TValue : struct, IPropertyContainer
        {
            return new MutableContainerListProperty<UTinyList, IList<TValue>, TValue>("Items",
                container => container.m_Items as IList<TValue>,
                null,
                list =>
                {
                    if (typeof(TValue) == typeof(UTinyObject))
                    {
                        return (TValue)(object) new UTinyObject(m_Registry, m_Type);
                    }
                    else if (typeof(TValue) == typeof(UTinyEnum.Reference))
                    {
                        var type = m_Type.Dereference(m_Registry);
                        var id = UTinyId.Empty;
                        if (type.Fields.Count > 0)
                        {
                            id = type.Fields[0].Id;
                        }
                        return (TValue)(object)new UTinyEnum.Reference(m_Type.Dereference(m_Registry), id);
                    }
                    return default(TValue);
                });
        }

        public void CopyFrom(UTinyList other)
        {
            Clear();

            Type = other.Type;
            foreach (var item in other)
            {
                Add(item);
            }
        }

        public override string ToString()
        {
            return BackEnd.Persist(this);
        }
    }
}
#endif // NET_4_6
