#if NET_4_6
using UnityEditor;
using UnityEngine;

namespace Unity.Tiny
{
    public static class UTinyColors
    {
        private static bool ProSkin => EditorGUIUtility.isProSkin;

        public static class Hierarchy 
        {
            public static Color SceneItem { get; } = ProSkin ? new Color32(0x3D, 0x3D, 0x3D, 0xFF) : new Color32(0xDA, 0xDA, 0xDA, 0xFF);
            public static Color SceneSeparator { get; } = ProSkin ? new Color32(0x21, 0x21, 0x21, 0xFF) : new Color32(0x96, 0x96, 0x96, 0xFF);
            public static Color Disabled { get; } =  new Color32(0xFF, 0xFF, 0xFF, 0x93);
        }

        public static class Inspector
        {
            public static Color Background { get; } = ProSkin ? new Color32(0x3D, 0x3D, 0x3D, 0xFF) : new Color32(0xC2, 0xC2, 0xC2, 0xFF);
            public static Color HeaderBackground { get; } = ProSkin ? new Color32(0x41, 0x41, 0x41, 0xFF) : new Color32(0xE1, 0xE1, 0xE1, 0xFF);
            public static Color Separator { get; } = ProSkin ? new Color32(0x2B, 0x2C, 0x2D, 0xFF) : new Color32(0x74, 0x74, 0x74, 0xFF);
        }

        public static class Editor
        {
            public static Color Link { get; } = ProSkin ? new Color32(0x5D, 0xA5, 0xFF, 0xFF) : new Color32(0x07, 0x4A, 0x8D, 0xFF);
        }
    }
}
#endif // NET_4_6
