#if NET_4_6
using UnityEngine;

namespace Unity.Tiny
{
    public class DisplayInfoEditor : ComponentEditor
    {
        public override bool VisitComponent(UTinyObject tinyObject)
        {
            // Until we move the settings. Display info is read-only
            var enabled = GUI.enabled;
            GUI.enabled = false;
            var result = base.VisitComponent(tinyObject);
            GUI.enabled = enabled;
            return result;
        }
    }
}
#endif // NET_4_6
