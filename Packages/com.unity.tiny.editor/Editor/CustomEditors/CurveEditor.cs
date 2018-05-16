#if NET_4_6
using UnityEngine;
using UnityEditor;

using Unity.Tiny.Conversions;

namespace Unity.Tiny
{
    public class CurveEditor : ComponentEditor
    {
        public override bool VisitComponent(UTinyObject tinyObject)
        {
            var curve = tinyObject.As<AnimationCurve>();
            EditorGUI.BeginChangeCheck();
            curve = EditorGUILayout.CurveField("curve", curve);
            if (EditorGUI.EndChangeCheck())
            {
                tinyObject.AssignFrom(curve);
                var container = tinyObject.Properties;
                PushChange(container, container.PropertyBag.FindProperty("mode"));
                PushChange(container, container.PropertyBag.FindProperty("stops"));
            }
            return true;
        }
    }
}
#endif // NET_4_6
