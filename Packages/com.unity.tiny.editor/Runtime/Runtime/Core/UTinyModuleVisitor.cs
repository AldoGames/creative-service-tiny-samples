#if NET_4_6
namespace Unity.Tiny
{
    public sealed partial class UTinyModule
    {
        public void Visit(UTinyProject.Visitor visitor)
        {
            foreach (var reference in m_Components)
            {
                var obj = reference.Dereference(Registry);
                if (null != obj) 
                {
                    visitor.VisitType(obj);
                }
            }
            
            foreach (var reference in m_Structs)
            {
                var obj = reference.Dereference(Registry);
                if (null != obj) 
                {
                    visitor.VisitType(obj);
                }
            }
            
            foreach (var reference in m_Enums)
            {
                var obj = reference.Dereference(Registry);
                if (null != obj) 
                {
                    visitor.VisitType(obj);
                }
            }
            
            foreach (var reference in m_Systems)
            {
                var obj = reference.Dereference(Registry);
                if (null != obj) 
                {
                    visitor.VisitSystem(obj);
                }
            }
            
            foreach (var reference in m_Scripts)
            {
                var obj = reference.Dereference(Registry);
                if (null != obj) 
                {
                    visitor.VisitScript(obj);
                }
            }
            
            foreach (var reference in m_EntityGroups)
            {
                var obj = reference.Dereference(Registry);
                if (null != obj)
                {
                    obj.Visit(visitor);
                }
            }
        }
    }
}
#endif // NET_4_6
