#if NET_4_6
using UnityEditor;

namespace Unity.Tiny
{
    public class LifetimeRotationEditor : ComponentEditor
    {
        public override bool VisitComponent(UTinyObject tinyObject)
        {
            EditorGUILayout.HelpBox("This component uses angular velocity in the editor and explicit rotation values in the runtime.", MessageType.Warning);
            return base.VisitComponent(tinyObject);
        }
    }
}
#endif // NET_4_6
