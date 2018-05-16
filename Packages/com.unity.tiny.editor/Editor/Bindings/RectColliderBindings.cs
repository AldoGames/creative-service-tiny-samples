#if NET_4_6
using UnityEngine;

using Unity.Tiny.Conversions;
using Unity.Tiny.Extensions;

namespace Unity.Tiny
{
    public class RectColliderBindings : ComponentBinding
    {
        public RectColliderBindings(UTinyType.Reference typeRef)
            : base(typeRef)
        {
        }

        protected override void OnAddBinding(UTinyEntity entity, UTinyObject component)
        {
            AddMissingComponent<BoxCollider2D>(entity);
        }

        protected override void OnRemoveBinding(UTinyEntity entity, UTinyObject component)
        {
            RemoveComponent<BoxCollider2D>(entity);
        }

        protected override void OnUpdateBinding(UTinyEntity entity, UTinyObject component)
        {
            OnAddBinding(entity, component);
            var collider = GetComponent<BoxCollider2D>(entity);
            var pivot = component.GetProperty<Vector2>("pivot");
            var width = component.GetProperty<float>("width");
            var height = component.GetProperty<float>("height");

            collider.size = new Vector2(width, height);
            collider.offset = new Vector2(-(pivot.x - 0.5f) * width, -(pivot.y - 0.5f) * height);
        }
        
        protected override void OnAddComponent(UTinyEntity entity, UTinyObject component)
        {
            var spriteRenderer = entity.GetComponent(entity.Registry.GetSprite2DRendererType());
            var sprite = spriteRenderer?.GetProperty<Sprite>("sprite");

            if (null == sprite || !sprite)
            {
                return;
            }

            component.AssignPropertyFrom("width", sprite.bounds.size.x);
            component.AssignPropertyFrom("height", sprite.bounds.size.y);
        }
    }
}
#endif // NET_4_6
