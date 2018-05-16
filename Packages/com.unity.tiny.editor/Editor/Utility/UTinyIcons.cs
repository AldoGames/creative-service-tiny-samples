#if NET_4_6
using UnityEditor;
using UnityEngine;

namespace Unity.Tiny
{
    public static class UTinyIcons
    {
        #region Properties
        
        public static Texture2D Export { get; private set; }
        public static Texture2D EntityGroup { get; private set; }
        public static Texture2D ActiveEntityGroup { get; private set; }
        public static Texture2D Variable { get; private set; }
        public static Texture2D Function { get; private set; }
        public static Texture2D Library { get; private set; }
        public static Texture2D Component { get; private set; }
        public static Texture2D Struct { get; private set; }
        public static Texture2D Enum { get; private set; }
        public static Texture2D Add { get; private set; }
        public static Texture2D Expand { get; private set; }
        public static Texture2D Collapse { get; private set; }
        public static Texture2D Visible { get; private set; }
        public static Texture2D Invisible { get; private set; }
        public static Texture2D Module { get; private set; }
        public static Texture2D Warning { get; private set; }
        public static Texture2D Trash { get; private set; }
        public static Texture2D Array { get; private set; }
        public static Texture2D System { get; private set; }
        public static Texture2D SeparatorVertical { get; private set; }
        public static Texture2D SeparatorHorizontal { get; private set; }
        public static Texture2D FoldoutOn { get; private set; }
        public static Texture2D FoldoutOff { get; private set; }
        public static Texture2D Locked { get; private set; }
        public static Texture2D Unlocked { get; private set; }
        public static Texture2D X_Icon_8 { get; private set; }
        public static Texture2D X_Icon_16 { get; private set; }
        public static Texture2D X_Icon_32 { get; private set; }
        public static Texture2D X_Icon_64 { get; private set; }
        #endregion
        
        #region Private Methods

        private static Texture2D Load(string path)
        {
            return AssetDatabase.LoadAssetAtPath<Texture2D>(UTinyConstants.PackagePath + "Editor Default Resources/" + path);
        }

        static UTinyIcons()
        {
            var proSkin = EditorGUIUtility.isProSkin;
            Export = Load("UTiny/Export.png");
            EntityGroup = Load($"UTiny/EntityGroup_icon{(EditorGUIUtility.isProSkin ? "" : "_personal")}.png");
            ActiveEntityGroup = Load($"UTiny/EntityGroup_icon_active{(EditorGUIUtility.isProSkin ? "" : "_personal")}.png");
            Variable = Load("UTiny/Variable.png");
            Function = Load("UTiny/Function.png");
            Library = Load("UTiny/Library.png");
            Component = Load("UTiny/Component.png");
            Struct = Load("UTiny/Class.png");
            Enum = Load("UTiny/Enum.png");
            Add = Load("UTiny/Add.png");
            Expand = Load("UTiny/Expand.png");
            Collapse = Load("UTiny/Collapse.png");
            Visible = Load("UTiny/Visible.png");
            Invisible = Load("UTiny/Invisible.png");
            Module = Load("UTiny/Module.png");
            Warning = Load("UTiny/Warning.psd");
            Trash = Load("UTiny/Trash.png");
            Array = Load("UTiny/Array.png");
            System = Load("UTiny/System.png");
            SeparatorVertical = Load("UTiny/SeparatorHorizontal.png");
            SeparatorHorizontal = Load("UTiny/SeparatorVertical.png");
            FoldoutOn = Load("UTiny/Foldout_On.png");
            FoldoutOff = Load("UTiny/Foldout_Off.png");
            Locked = Load("UTiny/Locked.png");
            Unlocked = Load("UTiny/Unlocked.png");
            X_Icon_8 = Load($"UTiny/x_icon_8{(proSkin? "":"_personal")}.png");
            X_Icon_16 = Load($"UTiny/x_icon_16{(proSkin? "":"_personal")}.png");
            X_Icon_32 = Load($"UTiny/x_icon_32{(proSkin? "":"_personal")}.png");
            X_Icon_64 = Load($"UTiny/x_icon_64{(proSkin? "":"_personal")}.png");

        }
        
        #endregion
    }
}
#endif // NET_4_6
