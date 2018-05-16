#if NET_4_6

using UnityEditor;
using UnityEngine;

namespace Unity.Tiny
{
    public static class UTinyGUI
    {
        public static void BackgroundColor(Rect rect, Color color)
        {
            var oldColor = GUI.color;
            GUI.color = color;

            GUI.DrawTexture(rect, EditorGUIUtility.whiteTexture);
            GUI.color = oldColor;
        }
    }
}
#endif // NET_4_6
