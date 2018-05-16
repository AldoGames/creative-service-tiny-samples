#if NET_4_6
using Unity.Tiny.Extensions;
using UnityEditor;
using UnityEngine;

namespace Unity.Tiny
{
    /// <summary>
    /// This class should ONLY be accessed from OnGUI callbacks.
    /// The access of this class will force the styles to initialize which must be in an OnGUI callback
    /// </summary>
    public static class UTinyStyles
    {
        public static GUIContent ListElementRemoveIcon = EditorGUIUtility.IconContent("Toolbar Minus", "|Remove system from the execution graph.");

        public static GUIStyle ListElementRemoveButton { get; } = new GUIStyle("InvisibleButton")
        {
        };

        public static GUIStyle ListBackground { get; } = new GUIStyle("TE NodeBackground")
        {
            margin = new RectOffset(),
            padding = new RectOffset(1, 1, 1, 0)
        };

        public static GUIStyle ListElementBackground { get; } = new GUIStyle("OL Box")
        {
            overflow = new RectOffset(1, 1, 1, 0)
        };

        public static GUIStyle DropField { get; } = new GUIStyle(EditorStyles.objectFieldThumb)
        {
            overflow = new RectOffset(2, 2, 2, 2),
            normal = {background = null},
            hover = {background = null},
            active = {background = null},
            focused = {background = null}
        };

        public static GUIStyle Header1 { get; } = new GUIStyle(EditorStyles.largeLabel)
        {
            fontStyle = FontStyle.Bold,
            fontSize = 18
        };

        public static GUIStyle Header2 { get; } = new GUIStyle(EditorStyles.largeLabel)
        {
            fontStyle = FontStyle.Bold,
            fontSize = 15
        };

        public static GUIStyle TypeMissingStyle { get; } = new GUIStyle
        {
            normal =
            {
                background = new Texture2D(2, 2) {hideFlags = HideFlags.HideAndDontSave},
                textColor = Color.black
            }
        };

        public static GUIStyle TypeNotFoundStyle { get; } = new GUIStyle
        {
            normal =
            {
                background = new Texture2D(2, 2) {hideFlags = HideFlags.HideAndDontSave},
                textColor = Color.black
            },
        };

        public static GUIStyle TypeOkStyle { get; } = new GUIStyle();
        public static Color TextColor => EditorGUIUtility.isProSkin ? Color.white : Color.black;
        public static Color ErrorTextColor => Color.black;

        public static GUIStyle ComponenHeaderFoldout { get; } = new GUIStyle(EditorStyles.foldout)
        {
            fontStyle = FontStyle.Bold,
            fontSize = 12,
            alignment = TextAnchor.MiddleLeft
        };

        public static GUIStyle ComponenHeaderLabel { get; } = new GUIStyle(EditorStyles.label)
        {
            fontStyle = FontStyle.Bold,
            fontSize = 12,
            alignment = TextAnchor.MiddleLeft
        };

        public static GUIStyle MiddleCenteredLabel { get; } = new GUIStyle(GUI.skin.label)
        {
            alignment = TextAnchor.MiddleCenter
        };

        public static GUIStyle AddComponentStyle { get; } = new GUIStyle("AC Button")
        {
        };

        public static GUIStyle PaneOptionStyle { get; } = new GUIStyle("PaneOptions");

        public static GUIStyle ComponentHeaderStyle { get; } = new GUIStyle(GUI.skin.textField)
        {
            fixedHeight = EditorGUIUtility.singleLineHeight,
            alignment = TextAnchor.MiddleLeft,
            fontSize = 12
        };

        public static GUIStyle LinkLabelStyle { get; } = new GUIStyle(EditorStyles.label)
        {
            normal = {textColor = UTinyColors.Editor.Link}
        };

        static UTinyStyles()
        {
            TypeMissingStyle.normal.background.SetColor(EditorGUIUtility.isProSkin ? Color.yellow * 0.5f : Color.yellow * 0.75f);
            TypeNotFoundStyle.normal.background.SetColor(EditorGUIUtility.isProSkin ? Color.red * 0.75f : Color.red);
        }
    }
}
#endif // NET_4_6