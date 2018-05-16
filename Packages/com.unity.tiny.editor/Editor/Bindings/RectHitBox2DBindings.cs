#if NET_4_6
using Unity.Tiny.Conversions;
using Unity.Tiny.Extensions;
using UnityEngine;

namespace Unity.Tiny
{
    public class RectHitBox2DBindings : ComponentBinding
    {
        public RectHitBox2DBindings(UTinyType.Reference typeRef)
            : base(typeRef)
        {
        }

        protected override void OnAddBinding(UTinyEntity entity, UTinyObject component)
        {
            AddMissingComponent<RectHitBox2D>(entity);
        }

        protected override void OnRemoveBinding(UTinyEntity entity, UTinyObject component)
        {
            RemoveComponent<RectHitBox2D>(entity);
        }

        protected override void OnUpdateBinding(UTinyEntity entity, UTinyObject component)
        {
            OnAddBinding(entity, component);
            
            var behaviour = GetComponent<RectHitBox2D>(entity);
            behaviour.Box = component.GetProperty<Rect>("box");
        }

        protected override void OnAddComponent(UTinyEntity entity, UTinyObject component)
        {
            var spriteRenderer = entity.GetComponent(entity.Registry.GetSprite2DRendererType());
            var sprite = spriteRenderer?.GetProperty<Sprite>("sprite");

            if (null == sprite || !sprite)
            {
                return;
            }

            var rect = new Rect
            {
                min = sprite.bounds.min,
                max = sprite.bounds.max
            };
            
            component.AssignPropertyFrom("box", rect);
        }
    }
}
#endif // NET_4_6
