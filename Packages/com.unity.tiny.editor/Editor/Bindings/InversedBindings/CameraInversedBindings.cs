#if NET_4_6
using JetBrains.Annotations;

using UnityEditor;
using UnityEngine;

using Unity.Tiny.Extensions;

namespace Unity.Tiny
{
    [UsedImplicitly]
    public class CameraInversedBindings : InversedBindingsBase<Camera>
    {
        #region Static
        [InitializeOnLoadMethod]
        private static void Register()
        {
            GameObjectTracker.RegisterForComponentModification<Camera>(SyncCamera);
        }

        public static void SyncCamera(Camera from, UTinyEntityView view)
        {
            var registry = view.Registry;
            var camera = view.EntityRef.Dereference(registry).GetComponent(registry.GetCamera2DType());
            if (null != camera)
            {
                SyncCamera(from, camera);
            }
        }

        public static void SyncCamera(Camera from, [NotNull] UTinyObject camera)
        {
            switch (from.clearFlags)
            {
                case CameraClearFlags.Color:
                case CameraClearFlags.Skybox:
                    from.clearFlags = CameraClearFlags.SolidColor;
                    break;
                case CameraClearFlags.Nothing:
                case CameraClearFlags.Depth:
                    from.clearFlags = CameraClearFlags.Nothing;
                    break;
            }

            from.orthographic = true;
            from.nearClipPlane = 0;
            from.useOcclusionCulling = false;
            from.allowHDR = false;
            from.allowMSAA = false;
#if UNITY_2017_3_OR_NEWER
            from.allowDynamicResolution = false;
#endif
            AssignIfDifferent(camera, "clearFlags", from.clearFlags);
            AssignIfDifferent(camera, "backgroundColor", from.backgroundColor);
            AssignIfDifferent(camera, "layerMask", from.cullingMask);
            AssignIfDifferent(camera, "halfVerticalSize", from.orthographicSize);
            AssignIfDifferent(camera, "rect", from.rect);
            AssignIfDifferent(camera, "depth", from.depth);
        }
        #endregion

        #region InversedBindingsBase<Camera>
        public override void Create(UTinyEntityView view, Camera @from)
        {
            var camera = new UTinyObject(Registry, GetMainTinyType());
            SyncCamera(from, camera);

            var entity = view.EntityRef.Dereference(Registry);
            var tiny = entity.GetOrAddComponent(GetMainTinyType());
            tiny.CopyFrom(camera);
            BindingsHelper.RunBindings(entity, tiny);
        }

        public override UTinyType.Reference GetMainTinyType()
        {
            return Registry.GetCamera2DType();
        }
        #endregion
    }
}
#endif // NET_4_6