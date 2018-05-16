#if NET_4_6
using UnityEditor;
using UnityEngine;

namespace Unity.Tiny
{
    public class GUIEnabledScope : GUI.Scope
    {
        private readonly bool m_Enabled;
        
        public GUIEnabledScope(bool enabled)
        {
            m_Enabled = GUI.enabled;
            GUI.enabled = enabled;
        }
        
        protected override void CloseScope()
        {
            GUI.enabled = m_Enabled;
        }
    }
    
    public class GUIColorScope : GUI.Scope
    {
        private readonly Color m_Color;
        
        public GUIColorScope(Color color)
        {
            m_Color = GUI.color;
            GUI.color = color;
        }
        
        protected override void CloseScope()
        {
            GUI.color = m_Color;
        }
    }

    public static class UTinyGUIUtility
    {
        public static float ComponentHeaderSeperatorHeight { get; } = 2.0f;
        public static float ComponentSeperatorHeight { get; } = 4.0f;

        public static float SingleLineAndSpaceHeight => EditorGUIUtility.singleLineHeight + EditorGUIUtility.standardVerticalSpacing;

    }
}
#endif // NET_4_6
