#if NET_4_6
using UnityEngine;
using UnityEngine.Experimental.UIElements;
using UnityEditor.Experimental.UIElements;

namespace Unity.Tiny
{
    // @TODO: Implement this eventually.
    public sealed class UIElementsBackend : InspectorBackend
    {
        public UIElementsBackend(UTinyInspector inspector) : base(inspector)
        {
        }

        public override void Build()
        {
            var root = m_Inspector.GetRootVisualContainer();
            root.Clear();
            var elem = new Label()
            {
                text = "The UIElements backend is not implemented yet."
            };
            elem.style.textColor = Color.gray;
            elem.style.fontSize = 15;
            root.Add(elem);
        }
    }
}
#endif // NET_4_6
