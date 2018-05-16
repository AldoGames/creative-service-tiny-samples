#if NET_4_6
using System.Collections.Generic;
using Unity.Properties;

namespace Unity.Tiny
{
    public interface IPersistentObject : IRegistryObject
    {
        new string Name { get; set; }
        string PersistenceId { get; set; }
        IEnumerable<IPropertyContainer> EnumeratePersistedObjects();
    }
}
#endif // NET_4_6
