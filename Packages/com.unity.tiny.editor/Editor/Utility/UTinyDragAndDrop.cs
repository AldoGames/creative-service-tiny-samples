#if NET_4_6
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Unity.Tiny
{
    [InitializeOnLoad]
    public static class UTinyDragAndDrop
    {
        /// <inheritdoc />
        /// <summary>
        /// A unity object instance that will always exist that can be used to attach data
        /// </summary>
        private class UnityObject : ScriptableObject
        {
        }

        private static readonly UnityObject m_Object;

        static UTinyDragAndDrop()
        {
            m_Object = Resources.FindObjectsOfTypeAll<UnityObject>().FirstOrDefault();

            if (!m_Object || m_Object == null)
            {
                m_Object = ScriptableObject.CreateInstance<UnityObject>();
                m_Object.hideFlags = HideFlags.DontSave;
            }
        }
        
        public static object[] ObjectReferences { get; set; }

        public static void StartDrag(string title)
        {
            DragAndDrop.objectReferences = new Object[] { m_Object };
            DragAndDrop.StartDrag(title);
        }
    }
}
#endif // NET_4_6
