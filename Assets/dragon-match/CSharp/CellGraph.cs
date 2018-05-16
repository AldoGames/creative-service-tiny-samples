#if NET_4_6
using UnityEngine;

namespace Unity.Tiny.Samples.Match3
{
    public class CellGraph : MonoBehaviour
    {
        #region Properties
        public int Width { get; set; }
        public int Height { get; set; }

        public CellLayout Layout { get; set; }
        #endregion

        #region Public Methods
        public Vector3 Snap(Vector3 position)
        {
            var gridPosition = WorldToGridSpace(position);
            return GridToWorldSpace(gridPosition);
        }

        public Vector2 WorldToGridSpace(Vector3 position)
        {
            var cellSize = Layout.CellSize;

            // Convert to local space
            var p = position - transform.position;

            if (Width % 2 != 0)
            {
                p.x += cellSize.x * 0.5f;
            }

            if (Height % 2 != 0)
            {
                p.y += cellSize.y * 0.5f;
            }

            p.x += Width * cellSize.x * 0.5f;
            p.y += Height * cellSize.y * 0.5f;

            var x = Mathf.FloorToInt(p.x / cellSize.x);
            var y = Mathf.FloorToInt(p.y / cellSize.y);

            return new Vector2(x, y);
        }

        public Vector2 GridToWorldSpace(Vector2 position)
        {
            var cellSize = Layout.CellSize;

            var p = new Vector3(position.x * cellSize.x, position.y * cellSize.y, 0);

            if (Width % 2 == 0)
            {
                p.x += cellSize.x * 0.5f;
            }

            if (Height % 2 == 0)
            {
                p.y += cellSize.y * 0.5f;
            }

            p.x -= Width * cellSize.x * 0.5f;
            p.y -= Height * cellSize.y * 0.5f;

            p += transform.position;

            return p;
        }
        #endregion

        #region Unity Methods
        private void OnDrawGizmos()
        {
            var matrix = Gizmos.matrix;

            var cellSize = Layout.CellSize;

            Gizmos.matrix =
                Matrix4x4.TRS(
                    transform.position - new Vector3(Width * cellSize.x * 0.5f, Height * cellSize.y * 0.5f, 0),
                    Quaternion.identity, new Vector3(cellSize.x, cellSize.y, 1));

            for (var x = 0; x < Width + 1; x++)
            {
                Gizmos.DrawLine(new Vector3(x, 0, 0), new Vector3(x, Height, 0));

                for (var y = 0; y < Height + 1; y++)
                {
                    Gizmos.DrawLine(new Vector3(0, y, 0), new Vector3(Width, y, 0));
                }
            }

            Gizmos.matrix = matrix;
        }
        #endregion
    }
}
#endif // NET_4_6
