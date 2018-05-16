#if NET_4_6
using System.Linq;
using System.Reflection;

using UnityEditor;
using UnityEngine;

using Unity.Tiny.Conversions;

namespace Unity.Tiny
{
    public class GradientEditor : ComponentEditor
    {
        public override bool VisitComponent(UTinyObject tinyObject)
        {
            var gradient = tinyObject.As<Gradient>();
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PrefixLabel("gradient");
            var rect = EditorGUILayout.GetControlRect();
            EditorGUI.BeginChangeCheck();
            var method = typeof(EditorGUI)
                     .GetMethods(BindingFlags.NonPublic | BindingFlags.Static)
                     .First(t => t.Name == "GradientField");
            gradient = (Gradient) method.Invoke(null, new object[] { rect, gradient });
            EditorGUILayout.EndHorizontal();
            if (EditorGUI.EndChangeCheck())
            {
                tinyObject.AssignFrom(gradient);
                var container = tinyObject.Properties;
                PushChange(container, container.PropertyBag.FindProperty("mode"));
                PushChange(container, container.PropertyBag.FindProperty("stops"));
            }

            return true;
        }
    }
}
#endif // NET_4_6
