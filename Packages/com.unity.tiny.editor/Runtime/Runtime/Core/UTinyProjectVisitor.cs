#if NET_4_6
using System;

namespace Unity.Tiny
{
    public sealed partial class UTinyProject
    {
        public abstract class Visitor
        {
            public UTinyProject Project { get; set; }
            public UTinyModule Module { get; set; }
            
            public virtual void BeginModule(UTinyModule module) { }
            public virtual void EndModule(UTinyModule module) { }
            public virtual void VisitSystem(UTinySystem system) { }
            public virtual void VisitType(UTinyType type) { }
            public virtual void VisitEntityGroup(UTinyEntityGroup entityGroup) { }
            public virtual void VisitScript(UTinyScript script) { }
            public virtual void VisitEntity(UTinyEntity entity) { }
            public virtual void VisitComponent(UTinyObject component) { }
        }

        public void Visit(Visitor visitor)
        {
            visitor.Project = this;
            visitor.Module = Module.Dereference(Registry);
            
            foreach (var dependency in visitor.Module.EnumerateDependencies())
            {
                visitor.BeginModule(dependency);
                dependency.Visit(visitor);
                visitor.EndModule(dependency);
            }
        }
        
        public class GenericVisitorAdapter : Visitor
        {
            private Action<UTinyType> m_Type;
            private Action<UTinySystem> m_System;
            private Action<UTinyScript> m_Script;
            private Action<UTinyEntityGroup> m_Scene;
            private Action<UTinyEntity> m_Entity;
        
            public GenericVisitorAdapter ForEachType(Action<UTinyType> action)
            {
                m_Type += action;
                return this;
            }
            
            public GenericVisitorAdapter ForEachSystem(Action<UTinySystem> action)
            {
                m_System += action;
                return this;
            }
            
            public GenericVisitorAdapter ForEachScript(Action<UTinyScript> action)
            {
                m_Script += action;
                return this;
            }
            
            public GenericVisitorAdapter ForEachScene(Action<UTinyEntityGroup> action)
            {
                m_Scene += action;
                return this;
            }
            
            public GenericVisitorAdapter ForEachEntity(Action<UTinyEntity> action)
            {
                m_Entity += action;
                return this;
            }
            
            public override void VisitType(UTinyType type)
            {
                m_Type?.Invoke(type);
            }
            public override void VisitSystem(UTinySystem system)
            {
                m_System?.Invoke(system);
            }

            public override void VisitScript(UTinyScript script)
            {
                m_Script?.Invoke(script);
            }

            public override void VisitEntityGroup(UTinyEntityGroup entityGroup)
            {
                m_Scene?.Invoke(entityGroup);
            }

            public override void VisitEntity(UTinyEntity entity)
            {
                m_Entity?.Invoke(entity);
            }
        }
    }
}
#endif // NET_4_6
