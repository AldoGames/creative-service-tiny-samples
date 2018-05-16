#if NET_4_6
using System;

using UnityEngine;

using Unity.Tiny.Extensions;

namespace Unity.Tiny.Conversions
{
    public class Vector4Converter : IConverterTo<Vector4>, IConverterFrom<Vector4>
    {
        public Vector4 ConvertTo(object @object)
        {
            return ConvertTo(@object as UTinyObject);
        }

        private Vector4 ConvertTo(UTinyObject @object)
        {
            ValidateType(@object);
            return new Vector4(
                @object.GetProperty<float>("x"),
                @object.GetProperty<float>("y"),
                @object.GetProperty<float>("z"),
                @object.GetProperty<float>("w")
            );
        }

        public UTinyObject ConvertFrom(UTinyObject @object, Vector4 v)
        {
            ValidateType(@object);
            @object["x"] = v.x;
            @object["y"] = v.y;
            @object["z"] = v.z;
            @object["w"] = v.w;
            return @object;
        }

        public object ConvertFrom(object @object, Vector4 value)
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

            if (!@object.Type.Equals(@object.Registry.GetVector4Type()))
            {
                throw new InvalidOperationException("Cannot convert value to or from Vector4");
            }
        }
    }
}
#endif // NET_4_6
