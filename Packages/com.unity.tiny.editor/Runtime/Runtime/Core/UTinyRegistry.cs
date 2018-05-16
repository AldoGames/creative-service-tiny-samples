#if NET_4_6
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Assertions;

namespace Unity.Tiny
{
    public enum UTinyRegistryEventType
    {
        Registered = 0,
        Unregistered = 1,
    }

    public interface IRegistryObject : IIdentifiable<UTinyId>, IDescribable
    {
        IRegistry Registry { get; }
        string Name { get; }
        
        void Refresh();
    }

    public interface IRegistry
    {
        int Count { get; }
        
        void Register(IRegistryObject t);
        void Unregister(IRegistryObject t);
        void Unregister(UTinyId id);
        void UnregisterAllBySource(string identifier);
        void Clear();
        T Dereference<TReference, T>(TReference reference) where TReference : IReference<T> where T : class, IRegistryObject;
        UTinyRegistryObjectBase Dereference(IReference reference);

        IDisposable SourceIdentifierScope(string identifier);
        UTinyProject CreateProject(UTinyId id, string name);
        UTinyModule CreateModule(UTinyId id, string name);
        UTinyType CreateType(UTinyId id, string name, UTinyTypeCode typeCode);
        UTinyEntityGroup CreateEntityGroup(UTinyId id, string name);
        UTinyEntity CreateEntity(UTinyId id, string name);
        UTinyScript CreateScript(UTinyId id, string name);
        UTinySystem CreateSystem(UTinyId id, string name);
        
        IRegistryObject FindById(UTinyId id);
        T FindById<T>(UTinyId id) where T : class, IRegistryObject;
        bool TryFindById<T>(UTinyId id, out T t) where T : class, IRegistryObject;
        T FindByName<T>(string name) where T : class, IRegistryObject;
        bool TryFindByName<T>(string name, out T t) where T : class, IRegistryObject;
        IEnumerable<T> FindAllByType<T>() where T : class, IRegistryObject;
        bool HasObjectFromSource(string identifier);
        IEnumerable<IRegistryObject> FindAllBySource(string identifier);
        IEnumerable<IRegistryObject> All();
        IEnumerable<IRegistryObject> AllUnregistered();
        void ClearUnregisteredObjects();
    }
    
    public class UTinyRegistry : IRegistry
    {
        public const string BuiltInSourceIdentifier = "__builtin__";
        public const string DefaultSourceIdentifier = "__default__";
        
        private static int s_RegistryId;
        
        private readonly IDictionary<UTinyId, IRegistryObject> m_Objects = new Dictionary<UTinyId, IRegistryObject>();
        private readonly Dictionary<Type, HashSet<UTinyId>> m_IdsByType = new Dictionary<Type, HashSet<UTinyId>>();
        private readonly int m_Id = s_RegistryId++;

        private readonly Stack<string> m_SourceIdentifierStack = new Stack<string>();
        private readonly Dictionary<string, HashSet<UTinyId>> m_SourceIdentifierMap = new Dictionary<string, HashSet<UTinyId>>();

        private readonly HashSet<IRegistryObject> m_UnregisteredObjects = new HashSet<IRegistryObject>();
        
        /// <summary>
        /// Shared version storage used by registry objects
        /// </summary>
        private readonly UTinyVersionStorage m_VersionStorage;

        public UTinyRegistry(UTinyVersionStorage versionStorage)
        {
            m_SourceIdentifierStack.Push(DefaultSourceIdentifier);
            m_VersionStorage = versionStorage;
            
            RegisterBuiltInTypes();
        }
        
        public UTinyRegistry() : this(new UTinyVersionStorage())
        {
        }

        public int Count => m_Objects.Count;
        
        public string SourceIdentifier => m_SourceIdentifierStack.Peek();

        public override string ToString()
        {
            return $"Registry, ID={m_Id}, Count={m_Objects.Count}";
        }

        private void RegisterBuiltInTypes()
        {
            using (new IdentificationScope(this, BuiltInSourceIdentifier))
            {
                foreach (var type in UTinyType.BuiltInTypes)
                {
                    Register(type);
                }
            }
        }

        public void Register(IRegistryObject t)
        {
            Assert.IsNotNull(t);
            
            IRegistryObject oldObject;
            if (m_Objects.TryGetValue(t.Id, out oldObject))
            {
                if (oldObject == t)
                {
                    return;
                }
                Unregister(oldObject);
            }
            
            Assert.AreNotEqual(UTinyId.Empty, t.Id);
            
            m_Objects[t.Id] = t;
            var type = t.GetType();
            HashSet<UTinyId> typeIds;
            if (!m_IdsByType.TryGetValue(type, out typeIds))
            {
                m_IdsByType[type] = typeIds = new HashSet<UTinyId>();
            }
            
            SetSourceIdentifier(t);

            if (typeIds.Add(t.Id))
            {
                UTinyEventDispatcher.Dispatch(UTinyRegistryEventType.Registered, t);
            }
        }
        
        public void Unregister(IRegistryObject t)
        {
            if (t == null)
            {
                return;
            }

            if (!m_Objects.Remove(t.Id))
            {
                return;
            }

            m_UnregisteredObjects.Add(t);

            var type = t.GetType();
            HashSet<UTinyId> typeIds;
            if (m_IdsByType.TryGetValue(type, out typeIds))
            {
                if (typeIds.Remove(t.Id))
                {
                    UTinyEventDispatcher.Dispatch(UTinyRegistryEventType.Unregistered, t);
                }
                if (typeIds.Count == 0)
                {
                    m_IdsByType.Remove(type);
                }
            }
        }
        
        public void Unregister(UTinyId id)
        {
            Unregister(FindById(id));
        }
        
        public void UnregisterAllBySource(string identifier)
        {
            if (string.IsNullOrEmpty(identifier))
            {
                identifier = DefaultSourceIdentifier;
            }
            
            foreach (var obj in FindAllBySource(identifier))
            {
                Unregister(obj);
            }

            m_SourceIdentifierMap.Remove(identifier);
        }

        public void Clear()
        {
            m_Objects.Clear();
            m_IdsByType.Clear();
            m_SourceIdentifierMap.Clear();
            
            RegisterBuiltInTypes();
        }
        
        public T Dereference<TReference, T>(TReference reference)
            where TReference : IReference<T>
            where T : class, IRegistryObject
        {
            T obj;
            return !TryFindById(reference.Id, out obj) ? null : obj;
        }
        
        public UTinyRegistryObjectBase Dereference(IReference reference)
        {
            UTinyRegistryObjectBase obj;
            return !TryFindById(reference.Id, out obj) ? null : obj;
        }

        private class IdentificationScope : IDisposable
        {
            private readonly UTinyRegistry m_Registry;
            
            public IdentificationScope(UTinyRegistry registry, string identifier)
            {
                m_Registry = registry;
                m_Registry.m_SourceIdentifierStack.Push(identifier);
            }

            public void Dispose()
            {
                m_Registry.m_SourceIdentifierStack.Pop();
            }
        }

        /// <summary>
        /// When active, identification scopes can be used to associate resource identifiers to newly
        /// created registry objects.
        /// 
        /// These scopes can be nested.
        /// </summary>
        /// <returns>A Disposable object. Dispose</returns>
        public IDisposable SourceIdentifierScope(string identifier)
        {
            if (identifier == BuiltInSourceIdentifier)
            {
                throw new Exception($"The built-in source identifier \"{BuiltInSourceIdentifier}\" cannot be used");
            }

            if (string.IsNullOrEmpty(identifier))
            {
                identifier = DefaultSourceIdentifier;
            }
            return new IdentificationScope(this, identifier);
        }

        private void SetSourceIdentifier(IRegistryObject obj)
        {
            var identifier = SourceIdentifier;
            if (string.IsNullOrEmpty(identifier))
            {
                return;
            }
            HashSet<UTinyId> ids;
            if (!m_SourceIdentifierMap.TryGetValue(identifier, out ids))
            {
                ids = m_SourceIdentifierMap[identifier] = new HashSet<UTinyId>();
            }

            ids.Add(obj.Id);
        }

        public bool HasObjectFromSource(string identifier)
        {
            return m_SourceIdentifierMap.ContainsKey(identifier);
        }

        public IEnumerable<IRegistryObject> FindAllBySource(string identifier)
        {
            if (string.IsNullOrEmpty(identifier))
            {
                identifier = DefaultSourceIdentifier;
            }
            
            HashSet<UTinyId> ids;
            if (!m_SourceIdentifierMap.TryGetValue(identifier, out ids))
            {
                yield break;
            }

            foreach (var id in ids)
            {
                IRegistryObject obj;
                if (m_Objects.TryGetValue(id, out obj))
                {
                    yield return obj;
                }
            }
        }

        public UTinyProject CreateProject(UTinyId id, string name)
        {
            var project = new UTinyProject(this, m_VersionStorage)
            {
                Id = id,
                Name = name,
            };
            
            m_VersionStorage.MarkAsChanged(project);
            
            Register(project);

            return project;
        }

        public UTinyModule CreateModule(UTinyId id, string name)
        {
            var module = new UTinyModule(this, m_VersionStorage)
            {
                Id = id,
                Name = name,
            };
            
            m_VersionStorage.MarkAsChanged(module);
            
            Register(module);

            return module;
        }
        
        public UTinyType CreateType(UTinyId id, string name, UTinyTypeCode typeCode)
        {
            var type = new UTinyType(this, m_VersionStorage)
            {
                Id = id,
                Name = name,
                TypeCode = typeCode
            };
            
            m_VersionStorage.MarkAsChanged(type);

            Register(type);

            return type;
        }
        
        public UTinyEntity CreateEntity(UTinyId id, string name)
        {
            var entity = new UTinyEntity(this, m_VersionStorage)
            {
                Id = id,
                Name = name
            };
            
            m_VersionStorage.MarkAsChanged(entity);
            
            Register(entity);

            return entity;
        }


        public UTinyEntityGroup CreateEntityGroup(UTinyId id, string name)
        {
            var entityGroup = new UTinyEntityGroup(this, m_VersionStorage)
            {
                Id = id,
                Name = name
            };
            
            m_VersionStorage.MarkAsChanged(entityGroup);
            
            
            Register(entityGroup);

            return entityGroup;
        }


        public UTinyScript CreateScript(UTinyId id, string name)
        {
            var script = new UTinyScript(this, m_VersionStorage)
            {
                Id = id,
                Name = name
            };
            
            m_VersionStorage.MarkAsChanged(script);
            
            Register(script);

            return script;
        }

        public UTinySystem CreateSystem(UTinyId id, string name)
        {
            var system = new UTinySystem(this, m_VersionStorage)
            {
                Id = id,
                Name = name,
                Options = UTinySystemOptions.All
            };
            
            m_VersionStorage.MarkAsChanged(system);
            
            Register(system);

            return system;
        }

        public IRegistryObject FindById(UTinyId id)
        {
            IRegistryObject t;
            m_Objects.TryGetValue(id, out t);
            return t;
        }

        public T FindById<T>(UTinyId id) where T : class, IRegistryObject
        {
            IRegistryObject t;
            m_Objects.TryGetValue(id, out t);
            return t as T;
        }

        public bool TryFindById<T>(UTinyId id, out T t) where T : class, IRegistryObject
        {
            IRegistryObject o;
            m_Objects.TryGetValue(id, out o);
            t = o as T;
            return true;
        }
        
        public T FindByName<T>(string name) where T : class, IRegistryObject
        {
            return FindAllByType<T>().FirstOrDefault(obj => string.Equals(obj.Name, name));
        }

        public bool TryFindByName<T>(string name, out T t) where T : class, IRegistryObject
        {
            t = FindByName<T>(name);
            return null != t;
        }

        public IEnumerable<T> FindAllByType<T>() where T : class, IRegistryObject
        {
            foreach (var typeKvp in m_IdsByType)
            {
                if (typeof(T).IsAssignableFrom(typeKvp.Key))
                {
                    foreach (var id in typeKvp.Value)
                    {
                        var obj = m_Objects[id];
                        Debug.Assert(obj is T, $"Cannot cast from {obj.GetType()} to {typeof(T)}");
                        yield return (T) obj;
                    }
                }
            }
        }

        public IEnumerable<IRegistryObject> All()
        {
            return m_Objects.Values;
        }

        public IEnumerable<IRegistryObject> AllUnregistered()
        {
            return m_UnregisteredObjects;
        }

        public void ClearUnregisteredObjects()
        {
            m_UnregisteredObjects.Clear();
        }
    }
}
#endif // NET_4_6
