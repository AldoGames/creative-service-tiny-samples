#if NET_4_6
using System.Collections.Generic;

using UnityEngine;
using Unity.Tiny.Extensions;
using Unity.Tiny.Conversions;

namespace Unity.Tiny
{
    public static partial class ParticleSystemBindings
    {
        public class LifetimeAlphaBindings : ComponentBinding
        {
            public LifetimeAlphaBindings(UTinyType.Reference typeRef)
                :base(typeRef)
            {
                RequireComponentType<ParticleSystem>();
                UnityEditor.EditorApplication.delayCall += () =>
                {
                    RegisterForEvent(UTinyEditorApplication.Registry.GetCurveType());
                    RegisterForEvent(UTinyEditorApplication.Registry.GetLifetimeColorType());
                };
            }

            protected override void OnRemoveComponent(UTinyEntity entity, UTinyObject component)
            {
                var module = GetComponent<ParticleSystem>(entity).colorOverLifetime;
                module.enabled = null != entity.GetComponent(entity.Registry.GetLifetimeColorType());
            }

            protected override void OnUpdateBinding(UTinyEntity entity, UTinyObject component)
            {
                var module = GetComponent<ParticleSystem>(entity).colorOverLifetime;
                var registry = entity.Registry;
                var curveRef = component.GetProperty<UTinyEntity.Reference>("curve");
                var curveEntity = curveRef.Dereference(registry);
                var curve = curveEntity?.GetComponent(registry.GetCurveType());

                if (UsesLifetimeColor(entity))
                {
                    return;
                }

                if (null == curveEntity || null == curve)
                {
                    module.enabled = false;
                }
                else
                {
                    module.enabled = true;
                    module.color = new ParticleSystem.MinMaxGradient(ConvertToAlphaGradient(curve));
                }
            }

            private bool UsesLifetimeColor(UTinyEntity entity)
            {
                if (null == entity)
                {
                    return false;
                }
                var registry = entity.Registry;
                var lifetimeColor = entity.GetComponent(registry.GetLifetimeColorType());
                if (null == lifetimeColor)
                {
                    return false;
                }

                var entityRef = lifetimeColor.GetProperty<UTinyEntity.Reference>("gradient");
                if (entityRef.Equals(UTinyEntity.Reference.None))
                {
                    return false;
                }

                var gradientEntity = entityRef.Dereference(registry);
                if (null == gradientEntity)
                {
                    return false;
                }

                var colorGradient = gradientEntity.GetComponent(registry.GetGradientType());
                return null != colorGradient;
            }

            public static Gradient ConvertToAlphaGradient(UTinyObject obj)
            {
                var gradient = new Gradient();
                gradient.mode = (GradientMode)obj.GetProperty<UTinyEnum.Reference>("mode").Value;

                List<GradientAlphaKey> offsets = new List<GradientAlphaKey>();

                var stops = obj["stops"] as UTinyList;
                for (int i = 0; i < stops.Count; ++i)
                {
                    var stop = stops[i] as UTinyObject;
                    var offset = stop.GetProperty<float>("offset");
                    var value = stop.GetProperty<float>("value");
                    offsets.Add(new GradientAlphaKey(value, offset));
                }

                gradient.alphaKeys = offsets.ToArray();
                return gradient;
            }
        }
    }
}
#endif // NET_4_6
