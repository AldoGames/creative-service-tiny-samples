#if NET_4_6
using System.Collections.Generic;
using Unity.Properties;
using Assert = UnityEngine.Assertions.Assert;

namespace Unity.Tiny
{
    /// <summary>
    /// Shared version storage
    /// </summary>
    public class UTinyVersionStorage : IVersionStorage
    {
        private const int UnknownVersion = -1;
        
        private readonly Dictionary<IPropertyContainer, int> m_Versions = new Dictionary<IPropertyContainer, int>();
        private readonly HashSet<IPropertyContainer> m_Changed = new HashSet<IPropertyContainer>();

        public IEnumerable<IPropertyContainer> Changed => m_Changed;

        public int GetVersion(IProperty property, IPropertyContainer container)
        {
            int version;
            return !m_Versions.TryGetValue(container, out version) ? UnknownVersion : version;
        }

        public void IncrementVersion(IProperty property, IPropertyContainer container)
        {
            Assert.IsTrue(container is IRegistryObject, $"{UTinyConstants.ApplicationName}: VersionStorage.IncrementVersion should only be called with a IRegistryObject container. Actual type is { container.GetType() }");
            
            int version;
            m_Versions.TryGetValue(container, out version);
            m_Versions[container] = ++version;
            m_Changed.Add(container);
        }

        public void MarkAsChanged(IPropertyContainer container)
        {
            m_Changed.Add(container);
        }

        public void ResetChanged()
        {
            m_Changed.Clear();
        }
    }
}
#endif // NET_4_6
