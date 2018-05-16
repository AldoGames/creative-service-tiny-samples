#if NET_4_6
using System.Linq;
using UnityEngine;
using Unity.Tiny.Extensions;
using Unity.Tiny.Conversions;

namespace Unity.Tiny
{
    public static partial class ParticleSystemBindings
    {
        public class ParticleEmitter : ComponentBinding
        {
            private static readonly Vector3[] s_TempVerts = new Vector3[4];
            private static readonly Vector2[] s_TempUVs = { new Vector2(0, 0), new Vector2(1, 0), new Vector2(1, 1), new Vector2(0, 1) };
            private static readonly int[] s_TempTris = { 0, 2, 1, 0, 3, 2};

            public ParticleEmitter(UTinyType.Reference typeRef)
                : base(typeRef)
            {
                UnityEditor.EditorApplication.delayCall += () =>
                {
                    RegisterForEvent(UTinyEditorApplication.Registry.GetSprite2DRendererType());
                    RegisterForEvent(UTinyEditorApplication.Registry.GetSprite2DType());
                    RegisterForEvent(UTinyEditorApplication.Registry.GetImage2DType());
                    RegisterForEvent(UTinyEditorApplication.Registry.GetTransformType());
                };
            }

            protected override void OnAddBinding(UTinyEntity entity, UTinyObject component)
            {
                AddMissingComponent<ParticleSystem>(entity, (system) =>
                {
                    var renderer = system.GetComponent<ParticleSystemRenderer>();
                    renderer.material = new Material(Shader.Find("UTiny/Particle2D"));
                    renderer.mesh = GenerateQuad();
                    var emission = GetComponent<ParticleSystem>(entity).emission;
                    emission.enabled = null != entity.GetComponent(entity.Registry.GetEmitterBoxSourceType());
                });
            }

            protected override void OnRemoveBinding(UTinyEntity entity, UTinyObject component)
            {
                RemoveComponent<ParticleSystem>(entity);
            }

            protected override  void OnUpdateBinding(UTinyEntity entity, UTinyObject component)
            {
                OnAddBinding(entity, component);
                var registry = entity.Registry;
                var particleSystem = GetComponent<ParticleSystem>(entity);

                var main = particleSystem.main;
                main.maxParticles = (int)component.GetProperty<uint>("maxParticles");
                main.startDelay = component.GetProperty<float>("startDelay");
                // float => bool conversion.
                main.prewarm = component.GetProperty<float>("prewarmPercent") >= 0.5f;
                var lifetime = component.GetProperty<Range>("lifetime");

                main.startLifetime = new ParticleSystem.MinMaxCurve(lifetime.start, lifetime.end);

                var emission = particleSystem.emission;
                emission.rateOverTime = component.GetProperty<float>("emitRate");

                // Renderer settings
                {
                    var renderer = particleSystem.GetComponent<ParticleSystemRenderer>();
                    renderer.renderMode = ParticleSystemRenderMode.Mesh;

                    var particleRef = (UTinyEntity.Reference)component["particle"];
                    var particle = particleRef.Dereference(registry);
                    var sprite2DRenderer = particle?.GetComponent(registry.GetSprite2DRendererType());

                    if (null == particle || null == sprite2DRenderer)
                    {
                        SyncMesh(renderer.mesh, null, Vector3.zero);
                    }
                    else
                    {
                        var particleSpriteRenderer = GetComponent<SpriteRenderer>(particle);
                        var sprite = sprite2DRenderer.GetProperty<Sprite>("sprite");
                        SyncMesh(renderer.mesh, sprite, GetComponent<Transform>(particle)?.localScale ?? Vector3.zero);

                        MaterialPropertyBlock sprite2DBlock = new MaterialPropertyBlock();
                        particleSpriteRenderer?.GetPropertyBlock(sprite2DBlock);

                        MaterialPropertyBlock particle2DBlock = new MaterialPropertyBlock();
                        renderer.GetPropertyBlock(particle2DBlock);
                        particle2DBlock.Clear();

                        if (null != sprite)
                        {
                            particle2DBlock.SetTexture("_MainTex", sprite.texture);
                        }
                        main.startColor = sprite2DBlock.GetColor("_Color");

                        renderer.SetPropertyBlock(particle2DBlock);

                        // Transfer blending mode
                        if (null != particleSpriteRenderer)
                        {
                            var particleMaterial = particleSpriteRenderer.sharedMaterial;
                            renderer.sharedMaterial.SetFloat("_SrcMode", particleMaterial.GetFloat("_SrcMode"));
                            renderer.sharedMaterial.SetFloat("_DstMode", particleMaterial.GetFloat("_DstMode"));
                        }
                    }
                }
            }

            private static void SyncMesh(Mesh particle, Sprite sprite, Vector3 localScale)
            {
                if (null == sprite)
                {
                    particle.triangles = s_TempTris;
                    particle.uv = s_TempUVs;
                    particle.vertices = s_TempVerts;
                }
                else
                {
                    particle.triangles = sprite.triangles.Select(t => (int) t).ToArray();
                    particle.vertices = sprite.vertices.Select(v => new Vector3(v.x * localScale.x, v.y * localScale.y, 0.0f)).ToArray();
                    particle.uv = sprite.uv;
                }
            }

            private static Mesh GenerateQuad()
            {
                return new Mesh
                {
                    vertices = s_TempVerts,
                    triangles = s_TempTris,
                    uv = s_TempUVs
                };
            }
        }
    }
}
#endif // NET_4_6
