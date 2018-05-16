#if NET_4_6
using System.Linq;

using UnityEditor;

using Unity.Tiny.Extensions;

namespace Unity.Tiny
{
    public class LifetimeAlphaEditor : ComponentEditor
    {
        public override bool VisitComponent(UTinyObject tinyObject)
        {
            if (TargetType == typeof(UTinyEntity) && UsesLifetimeColor(Targets.OfType<UTinyEntity>().FirstOrDefault()))
            {
                EditorGUILayout.HelpBox("This component will be ignore since the entity also has a LifetimeColor component.", MessageType.Info);
            }
            return base.VisitComponent(tinyObject);
        }

        private bool UsesLifetimeColor(UTinyEntity entity)
        {
            if (null == entity)
            {
                return false;
            }
            var registry = entity.Registry;
            var lifetimeColor = entity.GetComponent(registry.GetLifetimeColorType());
            if (null == lifetimeColor)
            {
                return false;
            }

            var entityRef = (UTinyEntity.Reference)lifetimeColor["gradient"];
            if (entityRef.Equals(UTinyEntity.Reference.None))
            {
                return false;
            }

            var gradientEntity = entityRef.Dereference(registry);
            if (null == gradientEntity)
            {
                return false;
            }

            var colorGradient = gradientEntity.GetComponent(registry.GetGradientType());
            return null != colorGradient;
        }
    }
}
#endif // NET_4_6
