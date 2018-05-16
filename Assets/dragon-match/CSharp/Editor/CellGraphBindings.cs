#if NET_4_6
using UnityEngine;

namespace Unity.Tiny.Samples.Match3
{
    public class CellGraphBindings : ComponentBinding
    {
        public CellGraphBindings(UTinyType.Reference typeRef)
            : base(typeRef)
        {
        }

        protected override void OnAddBinding(UTinyEntity entity, UTinyObject component) 
        {
            AddMissingComponent<CellGraph>(entity);
        }

        protected override void OnRemoveBinding(UTinyEntity entity, UTinyObject component)
        {
            RemoveComponent<CellGraph>(entity);
        }

        protected override void OnUpdateBinding(UTinyEntity entity, UTinyObject component)
        {
            OnAddBinding(entity, component);
            var graph = GetComponent<CellGraph>(entity);
            
            var layout = component["layout"] as UTinyObject;

            var size = layout?["size"] as UTinyObject;

            if (size != null)
            {
                graph.Layout = new CellLayout
                {
                    CellSize = new Vector2((float)size["x"], (float)size["y"])
                };
            }

            graph.Width = (int)component["width"];
            graph.Height = (int)component["height"];
        }
    }
}
#endif // NET_4_6
