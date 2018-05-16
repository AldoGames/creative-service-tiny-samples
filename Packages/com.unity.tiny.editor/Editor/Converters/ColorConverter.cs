#if NET_4_6
using System;

using UnityEngine;

using Unity.Tiny.Extensions;

namespace Unity.Tiny.Conversions
{
    public class ColorConverter : IConverterTo<Color>, IConverterFrom<Color>
    {
        public Color ConvertTo(object @object)
        {
            return ConvertTo(@object as UTinyObject);
        }

        private Color ConvertTo(UTinyObject @object)
        {
            ValidateType(@object);
            return new Color(
                @object.GetProperty<float>("r"),
                @object.GetProperty<float>("g"),
                @object.GetProperty<float>("b"),
                @object.GetProperty<float>("a")
            );
        }

        public UTinyObject ConvertFrom(UTinyObject @object, Color color)
        {
            ValidateType(@object);
            @object["r"] = color.r;
            @object["g"] = color.g;
            @object["b"] = color.b;
            @object["a"] = color.a;
            return @object;
        }
        public object ConvertFrom(object @object, Color value)
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

            if (!@object.Type.Equals(@object.Registry.GetColorType()))
            {
                throw new InvalidOperationException("Cannot convert value to or from Color");
            }
        }

    }
}
#endif // NET_4_6
