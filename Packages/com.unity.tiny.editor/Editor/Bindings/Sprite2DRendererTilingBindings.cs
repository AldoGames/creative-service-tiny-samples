#if NET_4_6
using UnityEngine;
using Unity.Tiny.Conversions;
using Unity.Tiny.Extensions;

namespace Unity.Tiny
{
    public enum TileMode
    {
        Tile,
        Stretch
    }

    public class Sprite2DRendererTilingBindings : ComponentBinding
    {
        public Sprite2DRendererTilingBindings(UTinyType.Reference typeRef) : base(typeRef)
        {
            RequireComponentType<SpriteRenderer>();
            UnityEditor.EditorApplication.delayCall += () =>
            {
                RegisterForEvent(UTinyEditorApplication.Registry.GetSprite2DRendererType());
                RegisterForEvent(UTinyEditorApplication.Registry.GetTransformType());
            };
        }

        protected override void OnAddBinding(UTinyEntity entity, UTinyObject component)
        {
            var renderer = GetComponent<SpriteRenderer>(entity);

            renderer.drawMode = Translate(component.GetProperty<TileMode>("mode"));
            if (component.GetProperty<Vector2>("size") == Vector2.zero)
            {
                component.AssignPropertyFrom("size", renderer.size);
            }
        }

        protected override void OnRemoveComponent(UTinyEntity entity, UTinyObject component)
        {
            var renderer = GetComponent<SpriteRenderer>(entity);
            renderer.drawMode = SpriteDrawMode.Simple;
        }

        protected override void OnUpdateBinding(UTinyEntity entity, UTinyObject component)
        {
            var renderer = GetComponent<SpriteRenderer>(entity);
            renderer.size = component.GetProperty<Vector2>("size");
            renderer.drawMode = Translate(component.GetProperty<TileMode>("mode"));
        }

        private SpriteDrawMode Translate(TileMode mode)
        {
            switch (mode)
            {
                case TileMode.Tile   : return SpriteDrawMode.Tiled;
                case TileMode.Stretch: return SpriteDrawMode.Sliced;
                default: throw new System.ArgumentOutOfRangeException(nameof(mode));
            }
        }

        private TileMode Translate(SpriteDrawMode mode)
        {
            switch (mode)
            {
                case SpriteDrawMode.Sliced: return TileMode.Stretch;
                case SpriteDrawMode.Tiled:  return TileMode.Tile;

                case SpriteDrawMode.Simple: default: throw new System.ArgumentOutOfRangeException(nameof(mode));
            }
        }
    }
}
#endif // NET_4_6
