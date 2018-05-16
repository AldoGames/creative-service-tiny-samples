#if NET_4_6
using System.Collections.Generic;
using System.Linq;
using Unity.Properties;
using UnityEngine;

namespace Unity.Tiny
{
    /// <summary>
    /// Originiator, this interface is implemented by any objects that require state save/restore functionality
    /// </summary>
    public interface IOriginator : IVersioned, IIdentifiable<UTinyId>
    {
        /// <summary>
        /// Saves the internal state of the object and returns a memento object
        /// </summary>
        /// <returns>Current state as a memento</returns>
        IMemento Save();

        /// <summary>
        /// Restores the internal object state based on the given memento
        /// </summary>
        /// <param name="memento">Previous state as a memento</param>
        void Restore(IMemento memento);
    }

    /// <summary>
    /// Memento interface. The structure is internal the the object itself
    /// </summary>
    public interface IMemento
    {
        int Version { get; }
    }

    public interface ICaretaker
    {
        void Update();
    }

    public delegate void CaretakerEventHandler(IOriginator originator, IMemento memento);

    public class UTinyCaretaker : ICaretaker
    {
        private readonly UTinyVersionStorage m_VersionStorage;
        
        /// <summary>
        /// Tracked versions for ALL objects
        /// </summary>
        private readonly Dictionary<UTinyId, int> m_Versions = new Dictionary<UTinyId, int>();

        public bool HasChanges => m_VersionStorage.Changed.Any();
        
        public event CaretakerEventHandler OnObjectChanged;

        public UTinyCaretaker(UTinyVersionStorage versionStorage)
        {
            m_VersionStorage = versionStorage;
        }

        public void Update()
        {
            Update(m_VersionStorage.Changed);
            m_VersionStorage.ResetChanged();
        }

        private void Update(IEnumerable<IPropertyContainer> objects)
        {
            foreach (var @object in objects)
            {
                var registryObject = @object as IRegistryObject;
                var originator = registryObject as IOriginator;

                if (originator == null || registryObject.Registry == null)
                {
                    continue;
                }
                
                Save(registryObject.Id, originator);
            }
        }

        private void Save(UTinyId id, IOriginator originator)
        {
            int version;
            if (!m_Versions.TryGetValue(id, out version))
            {
                m_Versions.Add(id, originator.Version);
            }

            if (version == originator.Version)
            {
                return;
            }

            // Skip memento generation
            if (null == OnObjectChanged)
            {
                return;
            }

            var memento = originator.Save();
            OnObjectChanged.Invoke(originator, memento);
        }

        public void Print()
        {
            foreach (var kvp in m_Versions)
            {
                Debug.Log($"{kvp.Key}: {kvp.Value}");
            }
        }
    }
}
#endif // NET_4_6
