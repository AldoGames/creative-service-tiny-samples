#if NET_4_6
using System;

using UnityEngine;

using Unity.Tiny.Extensions;

namespace Unity.Tiny.Conversions
{
    public class Vector2Converter : IConverterTo<Vector2>, IConverterFrom<Vector2>
    {
        public Vector2 ConvertTo(object @object)
        {
            return ConvertTo(@object as UTinyObject);
        }

        private Vector2 ConvertTo(UTinyObject @object)
        {
            ValidateType(@object);
            return new Vector2(
                @object.GetProperty<float>("x"),
                @object.GetProperty<float>("y")
            );
        }

        public UTinyObject ConvertFrom(UTinyObject @object, Vector2 vec2)
        {
            ValidateType(@object);
            @object["x"] = vec2.x;
            @object["y"] = vec2.y;
            return @object;
        }
        public object ConvertFrom(object @object, Vector2 value)
        {
            return ConvertFrom(@object as UTinyObject, value);
        }

        private void ValidateType(UTinyObject @object)
        {
            if (null == @object)
            {
                throw new ArgumentNullException("object");
            }
            
            if (null == @object.Registry)
            {
                throw new ArgumentNullException("registry");
            }

            if (!@object.Type.Equals(@object.Registry.GetVector2Type()))
            {
                throw new InvalidOperationException("Cannot convert value to or from Vector2");
            }
        }
    }
}
#endif // NET_4_6
