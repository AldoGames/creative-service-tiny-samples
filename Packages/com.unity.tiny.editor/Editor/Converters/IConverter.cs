#if NET_4_6
using System;

using UnityEngine.Assertions;

namespace Unity.Tiny.Conversions
{
    public interface IConverterTo<out TValue>
    {
        TValue ConvertTo(object obj);
    }

    public interface IConverterFrom<in TValue>
    {
        object ConvertFrom(object obj, TValue value);
        UTinyObject ConvertFrom(UTinyObject obj, TValue vec2);
    }

    public static class ObjectConverter<TValue>
    {
        public static IRegistry TestRegistry { get; set; }
        private static IConverterTo<TValue> m_ToConverter;
        private static IConverterFrom<TValue> m_FromConverter;

        public static TValue ConvertTo(object obj)
        {
            if (null == m_ToConverter)
            {
                throw new NullReferenceException($"{UTinyConstants.ApplicationName}: Cannot convert object to {typeof(TValue).Name}, no converter have been registered.");
            }
            return m_ToConverter.ConvertTo(obj);
        }

        public static object ConvertFrom(object obj, TValue value)
        {
            if (null == m_FromConverter)
            {
                throw new NullReferenceException($"{UTinyConstants.ApplicationName}: Cannot convert from {typeof(TValue).Name}, no converter have been registered.");
            }
            return m_FromConverter.ConvertFrom(obj, value);
        }

        public static UTinyObject ConvertFrom(UTinyObject obj, TValue value)
        {
            if (null == m_FromConverter)
            {
                throw new NullReferenceException($"{UTinyConstants.ApplicationName}: Cannot convert from {typeof(TValue).Name}, no converter have been registered.");
            }
            return m_FromConverter.ConvertFrom(obj, value);
        }

        public static void Register(IConverterTo<TValue> converter)
        {
            m_ToConverter = converter;
        }

        public static void Register(IConverterFrom<TValue> converter)
        {
            m_FromConverter = converter;
        }
    }

    public static class UTinyObjectConverters
    {
        public static TValue As<TValue>(this object obj)
        {
            return ObjectConverter<TValue>.ConvertTo(obj);
        }

        public static TValue As<TValue>(this UTinyObject obj)
        {
            return ObjectConverter<TValue>.ConvertTo(obj);
        }

        public static TValue GetProperty<TValue>(this UTinyObject obj, string propertyName)
        {
            Assert.IsFalse(string.IsNullOrEmpty(propertyName));
            return ObjectConverter<TValue>.ConvertTo(obj[propertyName]);
        }

        public static UTinyObject AssignPropertyFrom<TValue>(this UTinyObject obj, string propertyName, TValue value)
        {
            Assert.IsFalse(string.IsNullOrEmpty(propertyName));
            obj[propertyName] = ObjectConverter<TValue>.ConvertFrom(obj[propertyName], value);
            return obj;
        }

        public static UTinyObject AssignFrom<TValue>(this object obj, TValue value)
        {
            return ObjectConverter<TValue>.ConvertFrom(obj as UTinyObject, value);
        }

        public static UTinyObject AssignFrom<TValue>(this UTinyObject obj, TValue value)
        {
            return ObjectConverter<TValue>.ConvertFrom(obj, value);
        }
    }
}
#endif // NET_4_6
