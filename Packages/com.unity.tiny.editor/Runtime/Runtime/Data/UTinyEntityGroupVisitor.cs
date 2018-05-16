#if NET_4_6
namespace Unity.Tiny
{
    public sealed partial class UTinyEntityGroup
    {
        public void Visit(UTinyProject.Visitor visitor)
        {
            visitor.VisitEntityGroup(this);
			
            foreach (var reference in Entities)
            {
                var entity = reference.Dereference(Registry);
				
                visitor.VisitEntity(entity);
                foreach (var component in entity.Components)
                {
                    visitor.VisitComponent(component);
                }
            }
        }
    }
}
#endif // NET_4_6
