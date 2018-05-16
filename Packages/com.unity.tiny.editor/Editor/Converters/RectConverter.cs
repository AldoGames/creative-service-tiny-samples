#if NET_4_6
using System;

using UnityEngine;

using Unity.Tiny.Extensions;

namespace Unity.Tiny.Conversions
{
    public class RectConverter : IConverterTo<Rect>, IConverterFrom<Rect>
    {
        public Rect ConvertTo(object @object)
        {
            return ConvertTo(@object as UTinyObject);
        }

        private Rect ConvertTo(UTinyObject @object)
        {
            ValidateType(@object);
            return new Rect(
                @object.GetProperty<float>("x"),
                @object.GetProperty<float>("y"),
                @object.GetProperty<float>("width"),
                @object.GetProperty<float>("height")
            );
        }

        public UTinyObject ConvertFrom(UTinyObject @object, Rect rect)
        {
            ValidateType(@object);
            @object["x"] = rect.x;
            @object["y"] = rect.y;
            @object["width"] = rect.width;
            @object["height"] = rect.height;
            return @object;
        }
        public object ConvertFrom(object @object, Rect value)
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

            if (!@object.Type.Equals(@object.Registry.GetRectType()))
            {
                throw new InvalidOperationException("Cannot convert value to or from Rect");
            }
        }

    }
}
#endif // NET_4_6
