#if NET_4_6
using System;
using Unity.Tiny.Extensions;

namespace Unity.Tiny.Conversions
{
    public struct Range
    {
        public float start;
        public float end;

        public Range(float s, float e)
        {
            start = s;
            end = e;
        }
    }

    public class RangeConverter : IConverterTo<Range>, IConverterFrom<Range>
    {
        public Range ConvertTo(object @object)
        {
            return ConvertTo(@object as UTinyObject);
        }

        private Range ConvertTo(UTinyObject @object)
        {
            ValidateType(@object);
            return new Range(
                @object.GetProperty<float>("start"),
                @object.GetProperty<float>("end")
            );
        }

        public UTinyObject ConvertFrom(UTinyObject @object, Range value)
        {
            ValidateType(@object);
            @object["start"] = value.start;
            @object["end"] = value.end;
            return @object;
        }
        public object ConvertFrom(object @object, Range value)
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

            if (!@object.Type.Equals(@object.Registry.GetRangeType()))
            {
                throw new InvalidOperationException("Cannot convert value to or from Range");
            }
        }

    }
}
#endif // NET_4_6
