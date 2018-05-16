#if NET_4_6
using UnityEditor;
using UnityEngine;

namespace Unity.Tiny
{
    [CustomEditor(typeof(UTinyEntityView))]
    public class EntityViewEditor : Editor
    {
        public override void OnInspectorGUI()
        {
            UnityEditorInternal.ComponentUtility.MoveComponentUp(target as UTinyEntityView);

            EditorGUILayout.HelpBox($"Editing in {UTinyConstants.ApplicationName} must be done through the {UTinyConstants.ApplicationName} Inspector", MessageType.Info);
            if (GUILayout.Button($"Go to {UTinyConstants.ApplicationName} Inspector"))
            {
                UTinyInspector.FocusOrCreateWindow();
            }
        }
    }
}
#endif // NET_4_6
