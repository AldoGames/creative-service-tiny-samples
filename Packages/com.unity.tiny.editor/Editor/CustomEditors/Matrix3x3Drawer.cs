#if NET_4_6
using UnityEditor;
using UnityEngine;

using Unity.Properties;

namespace Unity.Tiny
{
    public class Matrix3x3Drawer : StructDrawer
    {
        private static string[] s_Matrix3x3Names = new string[]
        {
            "m00", "m01", "m02", "m10", "m11", "m12", "m20", "m21", "m22"
        };

        public override bool VisitStruct(UTinyObject tinyObject, GUIContent label)
        {
            if (!string.IsNullOrEmpty(label.text))
            {
                EditorGUILayout.PrefixLabel(label);
            }

            var properties = tinyObject.Properties;

            if (Screen.width < 400)
            {
                EditorGUIUtility.labelWidth = Mathf.Max(EditorGUIUtility.labelWidth - (400 - Screen.width), 70);
            }

            var indent = EditorGUI.indentLevel;
            try
            {
                for (int i = 0; i < s_Matrix3x3Names.Length; i += 3)
                {
                    EditorGUILayout.BeginHorizontal();

                    EditorGUI.indentLevel = 0;
                    GUILayout.Space(30.0f);
                    EditorGUIUtility.labelWidth = 30;
                    EditorGUIUtility.fieldWidth = 15;

                    Draw(properties, properties.PropertyBag.FindProperty(s_Matrix3x3Names[i  ]) as IProperty<UTinyObject.PropertiesContainer, float>);
                    Draw(properties, properties.PropertyBag.FindProperty(s_Matrix3x3Names[i+1]) as IProperty<UTinyObject.PropertiesContainer, float>);
                    Draw(properties, properties.PropertyBag.FindProperty(s_Matrix3x3Names[i+2]) as IProperty<UTinyObject.PropertiesContainer, float>);

                    EditorGUILayout.EndHorizontal();
                }
            }
            finally
            {
                EditorGUI.indentLevel = indent;
                EditorGUIUtility.fieldWidth = 0;
                EditorGUIUtility.labelWidth = 0;
            }
            return true;
        }

        private void Draw(UTinyObject.PropertiesContainer container, IProperty<UTinyObject.PropertiesContainer, float> property)
        {
            var isOverridden = (property as IUTinyValueProperty)?.IsOverridden(container) ?? true;
            UTinyEditorUtility.SetEditorBoldDefault(isOverridden);
            var current = property.GetValue(container);
            var value = EditorGUILayout.FloatField(property.Name, current);
            UTinyEditorUtility.SetEditorBoldDefault(false);
            if (value != current)
            {
                property.SetValue(container, value);
                PushChange(container, property);
            }
        }
    }
}
#endif // NET_4_6
