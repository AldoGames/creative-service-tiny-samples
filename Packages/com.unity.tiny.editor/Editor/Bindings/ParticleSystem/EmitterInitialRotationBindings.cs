#if NET_4_6
using UnityEngine;
using Unity.Tiny.Conversions;

namespace Unity.Tiny
{
    public static partial class ParticleSystemBindings
    {
        public class EmitterInitialRotation : ComponentBinding
        {
            public EmitterInitialRotation(UTinyType.Reference typeRef)
                : base(typeRef)
            {
                RequireComponentType<ParticleSystem>();
            }

            protected override void OnRemoveComponent(UTinyEntity entity, UTinyObject component)
            {
                SetRotationRange(GetComponent<ParticleSystem>(entity).main, new Range());
            }

            protected override void OnUpdateBinding(UTinyEntity entity, UTinyObject component)
            {
                var module = entity.View.GetComponent<ParticleSystem>().main;
                var rotation = component.GetProperty<Range>("rotation");
                SetRotationRange(module, rotation);
            }

            private void SetRotationRange(ParticleSystem.MainModule module, Range rotation)
            {
                module.startRotation3D = true;
                module.startRotationZ = new ParticleSystem.MinMaxCurve(rotation.start * Mathf.Deg2Rad, rotation.end * Mathf.Deg2Rad);
            }
        }
    }
}
#endif // NET_4_6
