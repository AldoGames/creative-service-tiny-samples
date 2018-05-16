#if NET_4_6

using UnityEngine;

namespace Unity.Tiny
{
    public static class UTinyGUILayout
    {
        public static void Separator(Color color, float height)
        {
            var rect = GUILayoutUtility.GetRect(0, height);
            UTinyGUI.BackgroundColor(rect, color);
        }
    }
}
#endif // NET_4_6
