#if NET_4_6
using UnityEngine;
using Unity.Tiny.Conversions;

namespace Unity.Tiny
{
    public class TextRendererBindings : ComponentBinding
    {
        public TextRendererBindings(UTinyType.Reference typeRef) 
            : base(typeRef)
        {
        }
        
        protected override void OnAddBinding(UTinyEntity entity, UTinyObject component)
        {
            AddMissingComponent<MeshRenderer>(entity, renderer =>
            {
                renderer.sharedMaterial = new Material(Shader.Find("GUI/Text Shader"));
            });
            AddMissingComponent<TextMesh>(entity);
        }

        protected override void OnRemoveBinding(UTinyEntity entity, UTinyObject component)
        {
            RemoveComponent<TextMesh>(entity);
            RemoveComponent<MeshRenderer>(entity);
        }
        
        protected override void OnUpdateBinding(UTinyEntity entity, UTinyObject textRenderer) 
        {
            OnAddBinding(entity, textRenderer);
#if UNITY_EDITOR
            
            try
            {
                var textMesh = GetComponent<TextMesh>(entity);

                textMesh.text = textRenderer.GetProperty<string>("text");
                textMesh.fontSize = textRenderer.GetProperty<int>("fontSize");

                var bold = textRenderer.GetProperty<bool>("bold");
                var italic = textRenderer.GetProperty<bool>("italic");

                if (bold && italic)
                {
                    textMesh.fontStyle = FontStyle.BoldAndItalic;
                }
                else if (bold)
                {
                    textMesh.fontStyle = FontStyle.Bold;
                }
                else if (italic)
                {
                    textMesh.fontStyle = FontStyle.Italic;
                }
                else
                {
                    textMesh.fontStyle = FontStyle.Normal;
                }

                textMesh.characterSize = 10;
                textMesh.lineSpacing = 1;
                textMesh.richText = false;
                textMesh.alignment = TextAlignment.Left;
                textMesh.anchor = textRenderer.GetProperty<TextAnchor>("anchor");
                textMesh.color = textRenderer.GetProperty<Color>("color");
                textMesh.font = textRenderer.GetProperty<Font>("font");

                if (textMesh.font)
                {
                    var renderer = GetComponent<MeshRenderer>(entity);
                    var block = new MaterialPropertyBlock();
                    renderer.GetPropertyBlock(block);
                    block.Clear();
                    block.SetTexture("_MainTex", textMesh.font.material.mainTexture);
                    renderer.SetPropertyBlock(block);
                }
            }
            finally
            {
                UnityEditor.SceneView.RepaintAll();
            }
#endif
        }
    }
}
#endif // NET_4_6
