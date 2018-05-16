#if NET_4_6
using System.Collections.Generic;
using System.Linq;

namespace Unity.Tiny
{
    /// <summary>
    /// Data provider for a UTinyTree
    /// </summary>
    public class UTinyTreeModel
    {
        private readonly Dictionary<int, object> m_Objects;
        
        public IRegistry Registry { get; }
        public UTinyModule.Reference MainModule { get; }

        protected UTinyTreeModel(IRegistry registry, UTinyModule.Reference mainModule)
        {
            m_Objects = new Dictionary<int, object>();
            Registry = registry;
            MainModule = mainModule;
        }
        
        /// <summary>
        /// Registers an instance id for the given reference
        /// </summary>
        public void Register(int id, object reference)
        {
            if (m_Objects.ContainsKey(id))
            {
                return;
            }
            m_Objects.Add(id, reference);
        }

        /// <summary>
        /// Find a reference by instance id
        /// </summary>
        public object FindByInstanceId(int id)
        {
            object reference;
            return !m_Objects.TryGetValue(id, out reference) ? null : reference;
        }
        
        public List<object> FindByInstanceId(IEnumerable<int> ids)
        {
            return ids.Select(FindByInstanceId).ToList();
        }

        public void ClearIds()
        {
            m_Objects.Clear();
        }
    }
}
#endif // NET_4_6
