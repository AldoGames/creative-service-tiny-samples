#if NET_4_6
using System;
using System.Collections.Generic;

using Unity.Tiny.Attributes;
using Unity.Tiny.Extensions;
using static Unity.Tiny.Attributes.EditorInspectorAttributes;

namespace Unity.Tiny
{
    public static class UTinyCoreAttributesHelper
    {
        private delegate void AttributeBinder(IRegistry registry, UTinyType.Reference type);

        private static Dictionary<UTinyId, List<AttributeBinder>> s_Lookup = new Dictionary<UTinyId, List<AttributeBinder>>()
        {
            /* core 2D functionality */
            { UTinyId.Generate("UTiny.Core2D.Color"), new List<AttributeBinder> { AddDrawer<ColorDrawer> } },
            { UTinyId.Generate("UTiny.Core2D.Transform"), new List<AttributeBinder> { AddEditor<TransformEditor>, (r, t) => AddBindings(r, t, type => new TransformBindings(type)) } },
            { UTinyId.Generate("UTiny.Core2D.Sprite2DRenderer"), new List<AttributeBinder> { (r, t) => AddBindings(r, t, type => new Sprite2DRendererBindings(type)) } },
            { UTinyId.Generate("UTiny.Core2D.Sprite2DRendererTiling"), new List<AttributeBinder> { (r, t) => AddBindings(r, t, type => new Sprite2DRendererTilingBindings(type)) } },
            { UTinyId.Generate("UTiny.Core2D.Camera2D"), new List<AttributeBinder> { AddEditor<Camera2DEditor>, (r, t) => AddBindings(r, t, type => new Camera2DBindings(type)) } },
            { UTinyId.Generate("UTiny.Core2D.Gradient"), new List<AttributeBinder> { AddEditor<GradientEditor> } },
            { UTinyId.Generate("UTiny.Core2D.Curve"), new List<AttributeBinder> { AddEditor<CurveEditor> } },
            { UTinyId.Generate("UTiny.Core2D.DisplayInfo"), new List<AttributeBinder> { AddEditor<DisplayInfoEditor> } },
            /* basic Math types */
            { UTinyId.Generate("UTiny.Math.Vector2"), new List<AttributeBinder> { AddDrawer<Vector2Drawer> } },
            { UTinyId.Generate("UTiny.Math.Vector3"), new List<AttributeBinder> { AddDrawer<Vector3Drawer> } },
            { UTinyId.Generate("UTiny.Math.Vector4"), new List<AttributeBinder> { AddDrawer<Vector4Drawer> } },
            { UTinyId.Generate("UTiny.Math.Matrix3x3"), new List<AttributeBinder> { AddDrawer<Matrix3x3Drawer> } },
            { UTinyId.Generate("UTiny.Math.Matrix4x4"), new List<AttributeBinder> { AddDrawer<Matrix4x4Drawer> } },
            { UTinyId.Generate("UTiny.Math.Quaternion"), new List<AttributeBinder> { AddDrawer<QuaternionDrawer> } },
            /* particle system */
            { UTinyId.Generate("UTiny.Particles.ParticleEmitter"), new List<AttributeBinder> { AddEditor<ParticleEmitterEditor>, (r, t) => AddBindings(r, t, type => new ParticleSystemBindings.ParticleEmitter(type)) } },
            { UTinyId.Generate("UTiny.Particles.EmitterBoxSource"), new List<AttributeBinder> { (r, t) => AddBindings(r, t, type => new ParticleSystemBindings.EmitterBoxSource(type)) } },
            { UTinyId.Generate("UTiny.Particles.EmitterInitialRotation"), new List<AttributeBinder> { (r, t) => AddBindings(r, t, type => new ParticleSystemBindings.EmitterInitialRotation(type)) } },
            { UTinyId.Generate("UTiny.Particles.EmitterInitialScale"), new List<AttributeBinder> { (r, t) => AddBindings(r, t, type => new ParticleSystemBindings.EmitterInitialScale(type)) } },
            { UTinyId.Generate("UTiny.Particles.LifetimeAlpha"), new List<AttributeBinder> { AddEditor<LifetimeAlphaEditor>, (r, t) => AddBindings(r, t, type => new ParticleSystemBindings.LifetimeAlphaBindings(type)) } },
            { UTinyId.Generate("UTiny.Particles.LifetimeColor"), new List<AttributeBinder> { (r, t) => AddBindings(r, t, type => new ParticleSystemBindings.LifetimeColorBindings(type)) } },
            { UTinyId.Generate("UTiny.Particles.LifetimeRotation"), new List<AttributeBinder> { AddEditor<LifetimeRotationEditor>, (r, t) => AddBindings(r, t, type => new ParticleSystemBindings.LifetimeRotationBindings(type)) } },
            { UTinyId.Generate("UTiny.Particles.LifetimeScale"), new List<AttributeBinder> { (r, t) => AddBindings(r, t, type => new ParticleSystemBindings.LifetimeScaleBindings(type)) } },

            /* text types */
            { new UTinyId("de741c68b67240148cdb8aa42a46bfdf"), new List<AttributeBinder> { AddEditor<TextRendererEditor>,(r, t) => AddBindings(r, t, type => new TextRendererBindings(type)) } },
            
            /* physics 2D functionality */
            { UTinyId.Generate("UTiny.Physics2D.RectCollider"), new List<AttributeBinder> { (r, t) => AddBindings(r, t, type => new RectColliderBindings(type)) } },
            
            /* hitbox 2D functionality */
            { UTinyId.Generate("UTiny.HitBox2D.RectHitBox2D"), new List<AttributeBinder> { (r, t) => AddBindings(r, t, type => new RectHitBox2DBindings(type)) } },
            { UTinyId.Generate("UTiny.HitBox2D.Sprite2DRendererHitBox2D"), new List<AttributeBinder> { (r, t) => AddBindings(r, t, type => new Sprite2DRendererHitBox2DBindings(type)) } },
        };

        [UnityEditor.InitializeOnLoadMethod]
        private static void Register()
        {
            UTinyEventDispatcher.AddListener<UTinyRegistryEventType, IRegistryObject>(UTinyRegistryEventType.Registered, HandleCoreTypeRegistered);
        }

        private static void HandleCoreTypeRegistered(UTinyRegistryEventType eventType, IRegistryObject obj)
        {
            if (!(obj is UTinyType) || null == obj.Registry)
            {
                return;
            }

            var type = obj as UTinyType;

            List<AttributeBinder> binders;
            if (!s_Lookup.TryGetValue(obj.Id, out binders))
            {
                return;
            }
            foreach(var binder in binders)
            {
                binder(obj.Registry, (UTinyType.Reference)type);
            }
        }

        private static void AddEditor<TEditor>(IRegistry registry, UTinyType.Reference type)
            where TEditor : ComponentEditor, new()
        {
            type.Dereference(registry)?.AddAttribute(CustomEditor(new TEditor()));
        }

        private static void AddDrawer<TDrawer>(IRegistry registry, UTinyType.Reference type)
            where TDrawer : StructDrawer, new()
        {
            type.Dereference(registry)?.AddAttribute(CustomDrawer(new TDrawer()));
        }

        private static void AddBindings<TBinding>(IRegistry registry, UTinyType.Reference type, Func<UTinyType.Reference, TBinding> del)
            where TBinding : IComponentBinding
        {
            type.Dereference(registry)?.AddAttribute(Bindings(del(type)));
        }
    }
}
#endif // NET_4_6
