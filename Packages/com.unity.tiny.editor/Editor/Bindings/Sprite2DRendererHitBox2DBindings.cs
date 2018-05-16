#if NET_4_6
using Unity.Tiny.Conversions;
using Unity.Tiny.Extensions;
using UnityEngine;

namespace Unity.Tiny
{
    public class Sprite2DRendererHitBox2DBindings : ComponentBinding
    {
        public Sprite2DRendererHitBox2DBindings(UTinyType.Reference typeRef)
            : base(typeRef)
        {
            UnityEditor.EditorApplication.delayCall += () =>
            {
                RegisterForEvent(UTinyEditorApplication.Registry.GetSprite2DRendererType());
            };
        }

        protected override void OnAddBinding(UTinyEntity entity, UTinyObject component)
        {
            AddMissingComponent<Sprite2DRendererHitBox2D>(entity);
        }

        protected override void OnRemoveBinding(UTinyEntity entity, UTinyObject component)
        {
            RemoveComponent<Sprite2DRendererHitBox2D>(entity);
        }

        protected override void OnUpdateBinding(UTinyEntity entity, UTinyObject component)
        {
            OnAddBinding(entity, component);

            var spriteRenderer = entity.GetComponent(entity.Registry.GetSprite2DRendererType());
            var sprite = spriteRenderer?.GetProperty<Sprite>("sprite");

            var behaviour = GetComponent<Sprite2DRendererHitBox2D>(entity);
            behaviour.Sprite = sprite;
        }
    }
}
#endif // NET_4_6
