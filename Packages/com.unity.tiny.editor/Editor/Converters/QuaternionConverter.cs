#if NET_4_6
using System;

using UnityEngine;

using Unity.Tiny.Extensions;

namespace Unity.Tiny.Conversions
{
    public class QuaternionConverter : IConverterTo<Quaternion>, IConverterFrom<Quaternion>
    {
        public Quaternion ConvertTo(object @object)
        {
            return ConvertTo(@object as UTinyObject);
        }

        private Quaternion ConvertTo(UTinyObject @object)
        {
            ValidateType(@object);
            return new Quaternion(
                @object.GetProperty<float>("x"),
                @object.GetProperty<float>("y"),
                @object.GetProperty<float>("z"),
                @object.GetProperty<float>("w")
            );
        }

        public UTinyObject ConvertFrom(UTinyObject @object, Quaternion q)
        {
            ValidateType(@object);
            @object["x"] = q.x;
            @object["y"] = q.y;
            @object["z"] = q.z;
            @object["w"] = q.w;
            return @object;
        }

        public object ConvertFrom(object @object, Quaternion value)
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

            if (!@object.Type.Equals(@object.Registry.GetQuaternionType()))
            {
                throw new InvalidOperationException("Cannot convert value to or from Quaternion");
            }
        }
    }
}
#endif // NET_4_6
