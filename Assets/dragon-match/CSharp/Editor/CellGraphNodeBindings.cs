#if NET_4_6

namespace Unity.Tiny.Samples.Match3
{
    public class CellGraphNodeBindings : ComponentBinding
    {
        public CellGraphNodeBindings(UTinyType.Reference typeRef)
            :base(typeRef)
        {
        }

        protected override void OnAddBinding(UTinyEntity entity, UTinyObject component)
        {
            AddMissingComponent<CellGraphNode>(entity);
        }

        protected override void OnRemoveBinding(UTinyEntity entity, UTinyObject component)
        {
            RemoveComponent<CellGraphNode>(entity);
        }

        protected override void OnUpdateBinding(UTinyEntity entity, UTinyObject component)
        {
            OnAddBinding(entity, component);
            var graphNode = GetComponent<CellGraphNode>(entity);

            var cell = component["cell"] as UTinyObject;

            if (null == cell)
            {
                return;
            }
            
            cell["x"] = (int)graphNode.Cell.x;
            cell["y"] = (int)graphNode.Cell.y;
        }
    }
}
#endif // NET_4_6
