#if NET_4_6
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.Rendering;

using Unity.Tiny.Conversions;
using Unity.Tiny.Extensions;

namespace Unity.Tiny
{
    public class Sprite2DRendererBindings : ComponentBinding
    {
        public Sprite2DRendererBindings(UTinyType.Reference typeRef)
            : base(typeRef)
        {
            UnityEditor.EditorApplication.delayCall += () =>
                {
                    RegisterForEvent(UTinyEditorApplication.Registry.GetSprite2DRendererTilingType());
                    RegisterForEvent(UTinyEditorApplication.Registry.GetTransformType());
                };
        }

        // These are the blend modes that match the runtime.
        private static readonly Dictionary<int, Vector2> s_BlendModes = new Dictionary<int, Vector2>
        {
            { 0, new Vector2 ((float)BlendMode.SrcAlpha, (float) BlendMode.OneMinusSrcAlpha) }, // alpha
            { 1, new Vector2 ((float)BlendMode.SrcAlpha, (float) BlendMode.One) },              // add
            { 2, new Vector2 ((float)BlendMode.DstColor, (float) BlendMode.OneMinusSrcAlpha) }  // multiply
        };

        protected override void OnAddBinding(UTinyEntity entity, UTinyObject component)
        {
            AddMissingComponent<SpriteRenderer>(entity, r =>
            {
                r.sharedMaterial= new Material(Shader.Find("UTiny/Sprite2D"));
            });
        }

        protected override void OnRemoveBinding(UTinyEntity entity, UTinyObject component)
        {
            RemoveComponent<SpriteRenderer>(entity);
        }

        protected override void OnUpdateBinding(UTinyEntity entity, UTinyObject sprite2DRenderer) 
        {
#if UNITY_EDITOR
            try
            {
                OnAddBinding(entity, sprite2DRenderer);
                var sprite = sprite2DRenderer.GetProperty<Sprite>("sprite");
                var spriteRenderer = GetComponent<SpriteRenderer>(entity);
                
                var block = new MaterialPropertyBlock();
                spriteRenderer.GetPropertyBlock(block);
                block.Clear();

                SetColorProperty(sprite2DRenderer, block);
                
                if (sprite)
                {
                    spriteRenderer.sprite = sprite;
                    var blending = sprite2DRenderer.GetProperty<UTinyEnum.Reference>("blending").Value;
                    Vector2 blendMode;
                    if (s_BlendModes.TryGetValue(blending, out blendMode))
                    {
                        spriteRenderer.sharedMaterial.SetFloat("_SrcMode", blendMode.x);
                        spriteRenderer.sharedMaterial.SetFloat("_DstMode", blendMode.y);
                    }
                    else
                    {
                        Debug.Log($"{UTinyConstants.ApplicationName}: Unknown blending mode, of value '{blending}'");
                    }

                    block.SetTexture("_MainTex", sprite.texture);
                    SetColorProperty(sprite2DRenderer, block);
                    

                }
                else
                {
                    spriteRenderer.sprite = null;
                }
                
                spriteRenderer.SetPropertyBlock(block);
            }
            finally
            {
                UnityEditor.SceneView.RepaintAll();
            }
#endif
        }

        private static void SetColorProperty(UTinyObject rendererComponent, MaterialPropertyBlock block)
        {
            Color color = Color.white;
            color = rendererComponent.GetProperty<Color>("color");
            block.SetColor("_Color", color);
        }
    }
}
#endif // NET_4_6
