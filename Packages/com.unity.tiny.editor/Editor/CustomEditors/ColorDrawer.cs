#if NET_4_6
using UnityEditor;
using UnityEngine;

using Unity.Tiny.Conversions;

namespace Unity.Tiny
{
    public class ColorDrawer : StructDrawer
    {
        public override bool VisitStruct(UTinyObject tinyObject, GUIContent label)
        {
            var value = tinyObject.As<Color>();
            EditorGUI.BeginChangeCheck();

            UTinyEditorUtility.SetEditorBoldDefault(tinyObject.IsOverridden);
            value = EditorGUILayout.ColorField(string.IsNullOrEmpty(label.text) ? GUIContent.none : label, value);
            UTinyEditorUtility.SetEditorBoldDefault(false);
            if (EditorGUI.EndChangeCheck())
            {
                tinyObject.AssignFrom(value);
                foreach (var prop in tinyObject.Properties.PropertyBag.Properties)
                {
                    PushChange(tinyObject.Properties, prop);
                }
            }
            return true;
        }
    }
}
#endif // NET_4_6
