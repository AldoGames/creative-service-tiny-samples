#if NET_4_6
using UnityEngine;
using Unity.Tiny.Conversions;

namespace Unity.Tiny
{
    public static partial class ParticleSystemBindings
    {
        public class EmitterBoxSource : ComponentBinding
        {
            public EmitterBoxSource(UTinyType.Reference typeRef)
                :base(typeRef)
            {
                RequireComponentType<ParticleSystem>();
            }

            protected override void OnRemoveComponent(UTinyEntity entity, UTinyObject component)
            {
                var emission = GetComponent<ParticleSystem>(entity).emission;
                emission.enabled = false;
            }

            protected override void OnUpdateBinding(UTinyEntity entity, UTinyObject component)
            {
                var particleSystem = GetComponent<ParticleSystem>(entity);
                var registry = entity.Registry;

                // [MP] @TODO: At some point, we'll have more than one type of source.
                var shape = particleSystem.shape;
                shape.shapeType = ParticleSystemShapeType.Box;

                var boxRect = component.GetProperty<Rect>("rect");
                var boxScale = shape.scale;
                boxScale.x = boxRect.width;
                boxScale.y = boxRect.height;
                boxScale.z = 1.0f;
                shape.scale = boxScale;

                var boxPosition = shape.position;
                boxPosition.x = boxRect.x;
                boxPosition.y = boxRect.y;
                boxPosition.z = 0.0f;
                shape.position = boxPosition;

                var emission = particleSystem.emission;
                emission.enabled = boxRect.width != 0 || boxRect.height != 0;
            }
        }
    }
}
#endif // NET_4_6
