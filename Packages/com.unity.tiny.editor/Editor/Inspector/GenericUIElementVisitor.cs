#if NET_4_6
using Unity.Properties;

namespace Unity.Tiny
{
    public class GenericUIElementVisitor : PropertyVisitorAdapter
    {
        public InspectMode Mode { get; set; } = InspectMode.Normal;

        public override void Visit<TContainer, TValue>(TContainer container, VisitContext<TValue> context)
        {
        }

        public override void Visit<TContainer, TValue>(ref TContainer container, VisitContext<TValue> context)
        {
        }
    }
}
#endif // NET_4_6
