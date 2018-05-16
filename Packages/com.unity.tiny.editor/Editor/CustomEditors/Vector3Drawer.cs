#if NET_4_6
using UnityEngine;
using UnityEditor;

using Unity.Properties;

namespace Unity.Tiny
{
    public class Vector3Drawer : StructDrawer
    {
        public override bool VisitStruct(UTinyObject tinyObject, GUIContent label)
        {
            EditorGUILayout.BeginHorizontal();

            if (Screen.width < 400)
            {
                EditorGUIUtility.labelWidth = Mathf.Max(EditorGUIUtility.labelWidth - (400 - Screen.width), 70);
            }

            if (!string.IsNullOrEmpty(label.text))
            {
                EditorGUILayout.PrefixLabel(label);
            }

            var indent = EditorGUI.indentLevel;
            try
            {
                EditorGUIUtility.labelWidth = 15;
                EditorGUIUtility.fieldWidth = 30;
                EditorGUI.indentLevel = 0;

                tinyObject.Properties.Visit(this);
            }
            finally
            {
                EditorGUILayout.EndHorizontal();
                EditorGUI.indentLevel = indent;
                EditorGUIUtility.fieldWidth = 0;
                EditorGUIUtility.labelWidth = 0;
            }
            return true;
        }
    }
}
#endif // NET_4_6
