#if NET_4_6
using UnityEngine;
using Unity.Tiny.Conversions;

namespace Unity.Tiny
{


    public class Camera2DBindings : ComponentBinding
    {
        public Camera2DBindings(UTinyType.Reference typeRef)
            :base(typeRef)
        {
        }

        protected override void OnAddBinding(UTinyEntity entity, UTinyObject component)
        {
            AddMissingComponent<Camera>(entity);
        }

        protected override  void OnRemoveBinding(UTinyEntity entity, UTinyObject component)
        {
            RemoveComponent<Camera>(entity);
        }

        protected override  void OnUpdateBinding(UTinyEntity entity, UTinyObject component)
        {
            component.Refresh();
            OnAddBinding(entity, component);
            var camera = entity.View.gameObject.GetComponent<Camera>();
            var clearFlagsRef = component.GetProperty<UTinyEnum.Reference>("clearFlags") ;

            if (clearFlagsRef.Name == "SolidColor")
            {
                camera.clearFlags = CameraClearFlags.SolidColor;
            }
            else
            {
                camera.clearFlags = CameraClearFlags.Depth;
            }
            var backgroundColor = component.GetProperty<Color>("backgroundColor");
            camera.backgroundColor = backgroundColor;
            camera.orthographic = true;
            camera.orthographicSize = component.GetProperty<float>("halfVerticalSize");
            camera.nearClipPlane = 0;
            camera.depth = -101.0f;
            camera.useOcclusionCulling = false;
            camera.allowHDR = false;
            camera.allowMSAA = false;
#if UNITY_2017_3_OR_NEWER
            camera.allowDynamicResolution = false;
#endif
            camera.cullingMask = component.GetProperty<int>("layerMask");
            camera.rect = component.GetProperty<Rect>("rect");
            camera.depth = component.GetProperty<float>("depth");
        }
    }
}
#endif // NET_4_6
