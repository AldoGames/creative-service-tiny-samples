#if NET_4_6
using System.Collections.Generic;

namespace Unity.Tiny.Extensions
{
    using Tiny;

    public static class IRegistryCoreTypeExtensions
    {
        private static readonly Dictionary<string, UTinyId> s_IDCache = new Dictionary<string, UTinyId>();

        #region Core
        public static UTinyModule GetCoreModule(this IRegistry registry)
        {
            return registry?.FindByName<UTinyModule>("UTiny");
        }
        
        public static UTinyType.Reference GetDisplayInfoType(this IRegistry registry)
        {
            return registry.GetType("UTiny.Core2D.DisplayInfo");
        }

        public static UTinyType.Reference GetTransformType(this IRegistry registry)
        {
            return registry.GetType("UTiny.Core2D.Transform");
        }

        public static UTinyType.Reference GetCamera2DType(this IRegistry registry)
        {
            return registry.GetType("UTiny.Core2D.Camera2D");
        }
        
        public static UTinyType.Reference GetCameraClearFlagsType(this IRegistry registry)
        {
            return registry.GetType("UTiny.Core2D.CameraClearFlags");
        }

        public static UTinyType.Reference GetSprite2DType(this IRegistry registry)
        {
            return registry.GetType("UTiny.Core2D.Sprite2D");
        }

        public static UTinyType.Reference GetSprite2DRendererType(this IRegistry registry)
        {
            return registry.GetType("UTiny.Core2D.Sprite2DRenderer");
        }

        public static UTinyType.Reference GetSprite2DRendererTilingType(this IRegistry registry)
        {
            return registry.GetType("UTiny.Core2D.Sprite2DRendererTiling");
        }

        public static UTinyType.Reference GetImage2DType(this IRegistry registry)
        {
            return registry.GetType("UTiny.Core2D.Image2D");
        }

        public static UTinyType.Reference GetColorType(this IRegistry registry)
        {
            return registry.GetType("UTiny.Core2D.Color");
        }

        public static UTinyType.Reference GetGradientType(this IRegistry registry)
        {
            return registry.GetType("UTiny.Core2D.Gradient");
        }

        public static UTinyType.Reference GetCurveType(this IRegistry registry)
        {
            return registry.GetType("UTiny.Core2D.Curve");
        }

        public static UTinyType.Reference GetGradientStopType(this IRegistry registry)
        {
            return registry.GetType("UTiny.Core2D.GradientStop");
        }

        public static UTinyType.Reference GetCurveStopType(this IRegistry registry)
        {
            return registry.GetType("UTiny.Core2D.CurveStop");
        }

        public static UTinyType.Reference GetRectType(this IRegistry regsitry)
        {
            return regsitry.GetType("UTiny.Math.Rect");
        }

        public static UTinyType.Reference GetRangeType(this IRegistry regsitry)
        {
            return regsitry.GetType("UTiny.Math.Range");
        }

        public static UTinyType.Reference GetVector2Type(this IRegistry registry)
        {
            return registry.GetType("UTiny.Math.Vector2");
        }

        public static UTinyType.Reference GetVector3Type(this IRegistry registry)
        {
            return registry.GetType("UTiny.Math.Vector3");
        }

        public static UTinyType.Reference GetVector4Type(this IRegistry registry)
        {
            return registry.GetType("UTiny.Math.Vector4");
        }

        public static UTinyType.Reference GetQuaternionType(this IRegistry registry)
        {
            return registry.GetType("UTiny.Math.Quaternion");
        }

        public static UTinyType.Reference GetMatrix3x3Type(this IRegistry registry)
        {
            return registry.GetType("UTiny.Math.Matrix3x3");
        }

        public static UTinyType.Reference GetMatrix4x4Type(this IRegistry registry)
        {
            return registry.GetType("UTiny.Math.Matrix4x4");
        }

        public static UTinyType.Reference GetParticleEmitterType(this IRegistry registry)
        {
            return registry.GetType("UTiny.Particles.ParticleEmitter");
        }

        public static UTinyType.Reference GetEmitterBoxSourceType(this IRegistry registry)
        {
            return registry.GetType("UTiny.Particles.EmitterBoxSource");
        }

        public static UTinyType.Reference GetEmitterInitialVelocityType(this IRegistry registry)
        {
            return registry.GetType("UTiny.Particles.EmitterInitialVelocity");
        }

        public static UTinyType.Reference GetEmitterInitialRotationType(this IRegistry registry)
        {
            return registry.GetType("UTiny.Particles.EmitterInitialRotation");
        }

        public static UTinyType.Reference GetEmitterInitialScaleType(this IRegistry registry)
        {
            return registry.GetType("UTiny.Particles.EmitterInitialScale");
        }

        public static UTinyType.Reference GetLifetimeColorType(this IRegistry registry)
        {
            return registry.GetType("UTiny.Particles.LifetimeColor");
        }

        public static UTinyType.Reference GetLifetimeAlphaType(this IRegistry registry)
        {
            return registry.GetType("UTiny.Particles.LifetimeAlpha");
        }

        public static UTinyType.Reference GetLifetimeScaleType(this IRegistry registry)
        {
            return registry.GetType("UTiny.Particles.LifetimeScale");
        }

        public static UTinyType.Reference GetLifetimeRotationType(this IRegistry registry)
        {
            return registry.GetType("UTiny.Particles.LifetimeRotation");
        }
        
        public static UTinyType.Reference GetLifetimeVelocityType(this IRegistry registry)
        {
            return registry.GetType("UTiny.Particles.LifetimeVelocity");
        }

        public static UTinyType.Reference GetAudioClipType(this IRegistry registry)
        {
            return registry.GetType("UTiny.Audio.AudioClip");
        }

        public static UTinyType.Reference GetAudioSourceType(this IRegistry registry)
        {
            return registry.GetType("UTiny.Audio.AudioSource");
        }

        public static UTinyType.Reference GetRectColliderType(this IRegistry registry)
        {
            return registry.GetType("UTiny.Physics2D.RectCollider");
        }

        private static UTinyType.Reference GetType(this IRegistry registry, string name)
        {
            if (null == registry)
            {
                return UTinyType.Reference.None;
            }
            UTinyId id;
            if (!s_IDCache.TryGetValue(name, out id))
            {
                s_IDCache[name] = id = UTinyId.Generate(name);
            }
            var type = registry.FindById<UTinyType>(id);
            if (null == type)
            {
                return UTinyType.Reference.None;
            }
            return (UTinyType.Reference)type;
        }
        #endregion
        
        #region Text
        public static UTinyType.Reference GetFontType(this IRegistry registry) 
        {
            // @HACK until the module is moved to the runtime
            var type = registry.FindById<UTinyType>(new UTinyId("e79a7d9b043443d8a6b5207058702290"));
            if (null == type)
            {
                return UTinyType.Reference.None;
            }
            return (UTinyType.Reference)type;
        }
        public static UTinyType.Reference GetTextRendererType(this IRegistry registry) 
        {
            // @HACK until the module is moved to the runtime
            var type = registry.FindById<UTinyType>(new UTinyId("de741c68b67240148cdb8aa42a46bfdf"));
            if (null == type)
            {
                return UTinyType.Reference.None;
            }
            return (UTinyType.Reference)type;
        }
        #endregion
    }
}
#endif // NET_4_6
