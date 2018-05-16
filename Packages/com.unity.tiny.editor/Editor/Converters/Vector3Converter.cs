#if NET_4_6
using System;

using UnityEngine;

using Unity.Tiny.Extensions;

namespace Unity.Tiny.Conversions
{
    public class Vector3Converter : IConverterTo<Vector3>, IConverterFrom<Vector3>
    {
        public Vector3 ConvertTo(object @object)
        {
            return ConvertTo(@object as UTinyObject);
        }

        private Vector3 ConvertTo(UTinyObject @object)
        {
            ValidateType(@object);
            return new Vector3(
                @object.GetProperty<float>("x"),
                @object.GetProperty<float>("y"),
                @object.GetProperty<float>("z")
            );
        }

        public UTinyObject ConvertFrom(UTinyObject @object, Vector3 vec3)
        {
            ValidateType(@object);
            @object["x"] = vec3.x;
            @object["y"] = vec3.y;
            @object["z"] = vec3.z;
            return @object;
        }

        public object ConvertFrom(object @object, Vector3 value)
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

            if (!@object.Type.Equals(@object.Registry.GetVector3Type()))
            {
                throw new InvalidOperationException("Cannot convert value to or from Vector3");
            }
        }
    }
}
#endif // NET_4_6
