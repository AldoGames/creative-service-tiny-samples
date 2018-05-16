#if NET_4_6
using UnityEditor;
using UnityEngine;
using Unity.Properties;

namespace Unity.Tiny
{
    public class StructDrawer : UTinyInspectorIMGUIVisitor
    {
        #region Implementation
        public virtual bool VisitStruct(UTinyObject tinyObject, GUIContent label)
        {
            var showPropperties = true;

            if (!string.IsNullOrEmpty(label.text))
            {
                showPropperties = UpdateFoldout(tinyObject, EditorGUILayout.Foldout(GetFoldoutFromCache(tinyObject), label, true));
            }

            if (showPropperties)
            {
                ++EditorGUI.indentLevel;
                try
                {
                    tinyObject.Properties.Visit(this);
                }
                finally
                {
                    --EditorGUI.indentLevel;
                }
            }
            return true;
        }
        #endregion
    }
}
#endif // NET_4_6
