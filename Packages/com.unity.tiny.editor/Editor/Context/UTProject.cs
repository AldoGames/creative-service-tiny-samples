#if NET_4_6
using UnityEditor;
using UnityEditor.Callbacks;
using UnityEngine;

namespace Unity.Tiny
{
    public class UTProject : ScriptableObject
    {
        [OnOpenAsset(0)]
        public static bool OnOpenAsset(int instanceId, int line)
        {
            //HACK: using the selection instead of the instance ID since this hook is not reliable
            var obj = Selection.activeObject;
            
            if (obj is UTProject)
            {
                var window = UTinyProjectWindow.OpenAndShow();
                window.SetTabType(UTinyProjectWindow.TabType.Settings);
                UTinyEditorApplication.LoadProject(AssetDatabase.GetAssetPath(obj));
                return true;
            }
            return false;
        }
    }
}
#endif // NET_4_6
