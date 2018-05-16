#if NET_4_6
using UnityEngine;

namespace Unity.Tiny.Samples.Match3
{
    [ExecuteInEditMode]
    public class CellGraphNode : MonoBehaviour
    {
        #region Properties

        public Vector2 Cell { get; private set; }
        public CellGraph Graph { get; private set; }

        #endregion

        #region MonoBehaviour

        private void OnEnable()
        {
            Graph = FindObjectOfType<CellGraph>();

            SnapToGrid();

#if UNITY_EDITOR

            m_LastLocalPosition = transform.localPosition;

#endif
        }

        #endregion

        #region Public Methods

        public void SnapToGrid()
        {
            if (null == Graph)
            {
                return;
            }

            transform.position = Graph.Snap(transform.position);
            Cell = Graph.WorldToGridSpace(transform.position);
        }

        #endregion

#if UNITY_EDITOR

        private Vector3 m_LastLocalPosition;

        private void Update()
        {
            // If our local position changes; re-snap
            if (m_LastLocalPosition != transform.localPosition)
            {
                SnapToGrid();
            }

            m_LastLocalPosition = transform.localPosition;
        }

#endif
    }
}
#endif // NET_4_6
