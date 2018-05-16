#if NET_4_6
using System;
using System.Linq;
using System.Collections.Generic;
using Unity.Properties;

namespace Unity.Tiny
{
    public abstract class InspectorBackend : IInspectorBackend
    {
        #region Fields
        protected readonly UTinyInspector m_Inspector;
        protected bool m_Locked;

        private List<IPropertyContainer> m_Targets;

        
        private Dictionary<Type, int> m_TypeToCount = new Dictionary<Type, int>();
        #endregion

        #region Properties
        public InspectMode Mode { get; set; } = InspectMode.Normal;

        public List<IPropertyContainer> Targets
        {
            get
            {
                return m_Targets;
            }
            set
            {
                if (m_Locked)
                {
                    return;
                }
                m_Targets = value;
                m_Targets.RemoveAll(t => null == t);
                m_TypeToCount = Targets.GroupBy(t => t.GetType()).ToDictionary(g => g.Key, g => g.Count());
            }
        }

        public bool Locked
        {
            get { return m_Locked; }
            set { m_Locked = value; }
        }
        #endregion
        protected InspectorBackend(UTinyInspector inspector)
        {
            m_Inspector = inspector;
            Targets = new List<IPropertyContainer>();
        }

        #region API
        public void OnGUI()
        {
            Targets.RemoveAll(container => null == container);
 

            if (IsInspectingDifferentTypes())
            {
                ShowDifferentTypes(m_TypeToCount);
                return;
            }

            ValidateTargets();
            if (m_Targets == null || m_Targets.Count == 0)
            {
                m_Locked = false;
                return;
            }

            Inspect();
        }
        #endregion

        #region Implementation
        protected virtual void ValidateTargets() { }
        public virtual void Build() { }
        protected virtual void Inspect() { }
        protected virtual void ShowDifferentTypes(Dictionary<Type, int> types) { }

        private bool IsInspectingDifferentTypes()
        {
            return m_TypeToCount.Count > 1;
        }

        protected void RestrictToType(Type type)
        {
            Targets.RemoveAll(t => t.GetType() != type);
            Build();
        }

        protected void FlipLocked()
        {
            m_Locked = !m_Locked;
        }
        #endregion
    }
}
#endif // NET_4_6
