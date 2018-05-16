#if NET_4_6
using UnityEngine;
using Unity.Tiny.Extensions;
using Unity.Tiny.Conversions;

namespace Unity.Tiny
{
    public static partial class ParticleSystemBindings
    {
        public class LifetimeRotationBindings : ComponentBinding
        {
            static ParticleSystem.MinMaxCurve NoCurve = new ParticleSystem.MinMaxCurve(0.0f, AnimationCurve.Constant(0, 1, 0));

            public LifetimeRotationBindings(UTinyType.Reference typeRef)
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
                var module = GetComponent<ParticleSystem>(entity).rotationOverLifetime;
                // This should be false, but for some reason, the particle system continues to rotate.
                module.enabled = true;
                module.separateAxes = true;
                module.x = module.y = module.z = NoCurve;
            }

            protected override void OnUpdateBinding(UTinyEntity entity, UTinyObject component)
            {
                var module = GetComponent<ParticleSystem>(entity).rotationOverLifetime;
                var registry = entity.Registry;
                var curveRef = component.GetProperty<UTinyEntity.Reference>("curve");
                var curveEntity = curveRef.Dereference(registry);
                var curve = curveEntity?.GetComponent(registry.GetCurveType());

                if (null == curveEntity || null == curve)
                {
                    // This should be false, but for some reason, the particle system continues to rotate.
                    module.separateAxes = true;
                    module.x = module.y = module.z = NoCurve;
                    module.enabled = true;
                }
                else
                {
                    module.enabled = true;
                    module.separateAxes = true;
                    module.x = module.y = NoCurve;
                    module.z = new ParticleSystem.MinMaxCurve(1.0f * Mathf.Deg2Rad, curve.As<AnimationCurve>());
                }
            }
        }
    }
}
#endif // NET_4_6
