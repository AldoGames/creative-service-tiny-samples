#if NET_4_6
using System;
using UnityEngine;

namespace Unity.Tiny.Conversions
{
    public abstract class BuiltinTypeConverter<TValue> : IConverterTo<TValue>, IConverterFrom<TValue>
    {
        public UTinyObject ConvertFrom(UTinyObject obj, TValue vec2)
        {
            throw new InvalidOperationException($"{UTinyConstants.ApplicationName}: Cannot convert {typeof(TValue).Name} into a UTinyObject.");
        }

        public object ConvertFrom(object obj, TValue value)
        {
            obj = value;
            return obj;
        }

        public TValue ConvertTo(object obj)
        {
            if (obj == null)
            {
                return default(TValue);
            }
            
            if (!typeof(TValue).IsAssignableFrom(obj.GetType()))
            {
                throw new InvalidOperationException($"{UTinyConstants.ApplicationName}: Cannot convert from {obj.GetType().Name} to {typeof(TValue).Name}.");
            }
            return (TValue)obj;
        }
    }


    public class BoolConverter : BuiltinTypeConverter<bool> { }
    public class CharConverter : BuiltinTypeConverter<char> { }
    public class SByteConverter : BuiltinTypeConverter<sbyte> { }
    public class ByteConverter : BuiltinTypeConverter<byte> { }
    public class ShortConverter : BuiltinTypeConverter<short> { }
    public class IntConverter : BuiltinTypeConverter<int> { }
    public class LongConverter : BuiltinTypeConverter<long> { }
    public class UIntConverter : BuiltinTypeConverter<uint> { }
    public class UShortConverter : BuiltinTypeConverter<ushort> { }
    public class ULongConverter : BuiltinTypeConverter<ulong> { }
    public class FloatConverter : BuiltinTypeConverter<float> { }
    public class DoubleConverter : BuiltinTypeConverter<double> { }
    public class StringConverter : BuiltinTypeConverter<string> { }
    public class Texture2DConverter : BuiltinTypeConverter<Texture2D> { }
    public class SpriteConverter : BuiltinTypeConverter<Sprite> { }
    public class FontConverter : BuiltinTypeConverter<Font> { }
    public class EntityRefConverter : BuiltinTypeConverter<UTinyEntity.Reference> { }
    public class EnumRefConverter : BuiltinTypeConverter<UTinyEnum.Reference> { }
}
#endif // NET_4_6
