#if NET_4_6
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

using Unity.Tiny.Extensions;

namespace Unity.Tiny.Conversions
{
    public class GradientConverter : IConverterTo<Gradient>, IConverterFrom<Gradient>
    {
        public Gradient ConvertTo(object @object)
        {
            return ConvertTo(@object as UTinyObject);
        }

        private Gradient ConvertTo(UTinyObject @object)
        {
            ValidateType(@object);
            var gradient = new Gradient();
            gradient.mode = (GradientMode)((UTinyEnum.Reference)@object["mode"]).Value;

            List<GradientColorKey> colors = new List<GradientColorKey>();
            List<GradientAlphaKey> offsets = new List<GradientAlphaKey>();

            var stops = @object["stops"] as UTinyList;
            for (int i = 0; i < stops.Count; ++i)
            {
                var stop = stops[i] as UTinyObject;
                var offset = stop.GetProperty<float>("offset");
                var color = stop.GetProperty<Color>("color");
                colors.Add(new GradientColorKey(color, offset));
                offsets.Add(new GradientAlphaKey(color.a, offset));
            }

            gradient.alphaKeys = offsets.ToArray();
            gradient.colorKeys = colors.ToArray();
            return gradient;
        }

        public UTinyObject ConvertFrom(UTinyObject @object, Gradient gradient)
        {
            ValidateType(@object);
            var registry = @object.Registry;
            var type = @object.Type.Dereference(registry).Fields.First(f => f.Name == "mode").FieldType.Dereference(registry);
            @object["mode"] = new UTinyEnum.Reference(type, (int)gradient.mode);
            var stops = @object["stops"] as UTinyList;
            stops.Clear();
            Color currentColor = Color.white;
            float currentOffset = float.MaxValue;
            for (int i = 0, j = 0; i < gradient.alphaKeys.Length || j < gradient.colorKeys.Length;)
            {
                var alphaOffset = float.MaxValue;
                var colorOffset = float.MaxValue;

                if (i < gradient.alphaKeys.Length)
                {
                    alphaOffset = gradient.alphaKeys[i].time;
                }

                if (j < gradient.colorKeys.Length)
                {
                    colorOffset = gradient.colorKeys[j].time;
                }

                if (alphaOffset == colorOffset)
                {
                    currentColor = gradient.colorKeys[j].color;
                    currentColor.a = gradient.alphaKeys[i].alpha;
                    currentOffset = colorOffset;
                    ++i;
                    ++j;
                }
                else if (alphaOffset < colorOffset)
                {
                    currentColor.a = gradient.alphaKeys[i].alpha;
                    currentOffset = alphaOffset;
                    ++i;
                }
                else // if (alpha > color)
                {
                    var alpha = currentColor.a;
                    currentColor = gradient.colorKeys[j].color;
                    currentColor.a = alpha;
                    currentOffset = colorOffset;
                    ++j;
                }

                var stop = new UTinyObject(registry, registry.GetGradientStopType(), @object.VersionStorage)
                {
                    ["color"] = new UTinyObject(registry, registry.GetColorType(), @object.VersionStorage)
                    {
                        ["r"] = currentColor.r,
                        ["g"] = currentColor.g,
                        ["b"] = currentColor.b,
                        ["a"] = currentColor.a
                    },
                    ["offset"] = currentOffset
                };

                stops.Add(stop);
            }
            return @object;
        }
        public object ConvertFrom(object @object, Gradient value)
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

            if (!@object.Type.Equals(@object.Registry.GetGradientType()))
            {
                throw new InvalidOperationException("Cannot convert value to or from Gradient");
            }
        }

    }
}
#endif // NET_4_6
