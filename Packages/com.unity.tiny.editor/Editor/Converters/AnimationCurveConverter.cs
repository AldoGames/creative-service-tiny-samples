#if NET_4_6
using System;

using UnityEditor;
using UnityEngine;

using Unity.Tiny.Extensions;

namespace Unity.Tiny.Conversions
{
    public class AnimationCurveConverter : IConverterTo<AnimationCurve>, IConverterFrom<AnimationCurve>
    {
        public AnimationCurve ConvertTo(object @object)
        {
            return ConvertTo(@object as UTinyObject);
        }

        private AnimationCurve ConvertTo(UTinyObject @object)
        {
            ValidateType(@object);
            // [MP] @TODO: Do a more thorough curve conversion when the value gradient becomes a curve.
            var curve = new AnimationCurve();

            var mode = (GradientMode)((UTinyEnum.Reference)@object["mode"]).Value;

            var stops = @object["stops"] as UTinyList;
            for (int i = 0; i < stops.Count; ++i)
            {
                var stop = stops[i] as UTinyObject;
                var offset = stop.GetProperty<float>("offset");
                var value = stop.GetProperty<float>("value");
                curve.AddKey(offset, value);

            }
            for (int i = 0; i < curve.keys.Length; ++i)
            {
                AnimationUtility.SetKeyLeftTangentMode(curve, i, AnimationUtility.TangentMode.Linear);
                AnimationUtility.SetKeyRightTangentMode(curve, i, AnimationUtility.TangentMode.Linear);
            }
            return curve;
        }

        public UTinyObject ConvertFrom(UTinyObject @object, AnimationCurve curve)
        {
            ValidateType(@object);
            // [MP] @TODO: Do a more thorough curve conversion when the value gradient becomes a curve.
            var registry = @object.Registry;
            var stops = @object["stops"] as UTinyList;
            stops.Clear();
            for (int i = 0; i < curve.keys.Length; ++i)
            {
                var key = curve.keys[i];
                var stop = new UTinyObject(registry, registry.GetCurveStopType(), @object.VersionStorage)
                {
                    ["value"] = key.value,
                    ["offset"] = key.time
                };

                stops.Add(stop);
            }
            return @object;
        }
        public object ConvertFrom(object @object, AnimationCurve value)
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

            if (!@object.Type.Equals(@object.Registry.GetCurveType()))
            {
                throw new InvalidOperationException("Cannot convert value to or from AnimationCurve");
            }
        }

    }
}
#endif // NET_4_6
