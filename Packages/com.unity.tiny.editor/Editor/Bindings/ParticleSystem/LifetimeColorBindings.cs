#if NET_4_6
using UnityEngine;

using Unity.Tiny.Extensions;
using Unity.Tiny.Conversions;

namespace Unity.Tiny
{
    public static partial class ParticleSystemBindings
    {
        public class LifetimeColorBindings : ComponentBinding
        {
            public LifetimeColorBindings(UTinyType.Reference typeRef)
                : base(typeRef)
            {
                RequireComponentType<ParticleSystem>();
                UnityEditor.EditorApplication.delayCall += () => RegisterForEvent(UTinyEditorApplication.Registry.GetGradientType());
            }

            protected override void OnRemoveComponent(UTinyEntity entity, UTinyObject component)
            {
                var module = GetComponent<ParticleSystem>(entity).colorOverLifetime;
                module.enabled = null != entity.GetComponent(entity.Registry.GetLifetimeAlphaType());
            }

            protected override void OnUpdateBinding(UTinyEntity entity, UTinyObject component)
            {
                var module = GetComponent<ParticleSystem>(entity).colorOverLifetime;
                var registry = entity.Registry;
                var gradientRef = component.GetProperty<UTinyEntity.Reference>("gradient");
                var gradientEntity = gradientRef.Dereference(registry);
                var colorGradient = gradientEntity?.GetComponent(registry.GetGradientType());

                if (null == gradientEntity || null == colorGradient)
                {
                    module.enabled = false;
                }
                else
                {
                    module.enabled = true;
                    module.color = new ParticleSystem.MinMaxGradient(colorGradient.As<Gradient>());
                }
            }
        }
    }
}
#endif // NET_4_6
