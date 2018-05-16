#if NET_4_6
using UnityEngine;
using Unity.Tiny.Conversions;

namespace Unity.Tiny
{
    public static partial class ParticleSystemBindings
    {
        public class EmitterInitialScale : ComponentBinding
        {
            public EmitterInitialScale(UTinyType.Reference typeRef)
                : base(typeRef)
            {
                RequireComponentType<ParticleSystem>();
            }

            protected override void OnRemoveComponent(UTinyEntity entity, UTinyObject component)
            {
                var module = entity.View.GetComponent<ParticleSystem>().main;
                SetScaleRange(module, new Range(1, 1));
            }

            protected override void OnUpdateBinding(UTinyEntity entity, UTinyObject component)
            {
                var module = entity.View.GetComponent<ParticleSystem>().main;
                var scale = component.GetProperty<Range>("scale");
                SetScaleRange(module, scale);
            }

            private void SetScaleRange(ParticleSystem.MainModule module, Range scale)
            {
                module.startSize = new ParticleSystem.MinMaxCurve(scale.start, scale.end);
            }
        }
    }
}
#endif // NET_4_6
