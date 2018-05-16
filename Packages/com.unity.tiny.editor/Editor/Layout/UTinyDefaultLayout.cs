#if NET_4_6
using UnityEditor;

namespace Unity.Tiny
{
    public static class UTinyDefaultLayout
    {
        [MenuItem(UTinyConstants.MenuItemNames.UTinyLayout)]
        public static void Load()
        {
            EditorUtility.LoadWindowLayout(UTinyConstants.PackagePath + "Editor/Layout/UTinyDefaultMode.wlt");
        }
    }
}
#endif // NET_4_6
