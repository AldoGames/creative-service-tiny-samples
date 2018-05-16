#if NET_4_6
using UnityEngine;
using Unity.Tiny.Conversions;
using Unity.Tiny.Extensions;

namespace Unity.Tiny
{
    public static partial class ParticleSystemBindings
    {
        public class LifetimeScaleBindings : ComponentBinding
        {
            public LifetimeScaleBindings(UTinyType.Reference typeRef)
                : base(typeRef)
            {
                RequireComponentType<ParticleSystem>();
                UnityEditor.EditorApplication.delayCall += () =>
                {
                    RegisterForEvent(UTinyEditorApplication.Registry.GetCurveType());
                };
            }

            protected override void OnRemoveComponent(UTinyEntity entity, UTinyObject component)
            {
                var module = GetComponent<ParticleSystem>(entity).sizeOverLifetime;
                module.enabled = false;
            }

            protected override void OnUpdateBinding(UTinyEntity entity, UTinyObject component)
            {
                var module = GetComponent<ParticleSystem>(entity).sizeOverLifetime;
                var registry = entity.Registry;
                var curveRef = component.GetProperty<UTinyEntity.Reference>("curve");
                var curveEntity = curveRef.Dereference(registry);
                var curve = curveEntity?.GetComponent(registry.GetCurveType());

                if (null == curveEntity || null == curve)
                {
                    module.enabled = false;
                }
                else
                {
                    module.enabled = true;
                    module.separateAxes = false;
                    module.size = new ParticleSystem.MinMaxCurve(1.0f, curve.As<AnimationCurve>());
                }
            }
        }
    }
}
#endif // NET_4_6
