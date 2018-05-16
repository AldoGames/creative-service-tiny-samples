#if NET_4_6
using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Properties;
using UnityEngine;
using UnityEngine.Assertions;
using Unity.Tiny.Attributes;
using Unity.Tiny.Filters;

namespace Unity.Tiny
{
    [Flags]
    public enum UTinyModuleOptions
    {
        None = 0,

        /// <summary>
        /// This module should not be exposed in the editor
        /// </summary>
        ReadOnly = 1 << 0,

        /// <summary>
        /// This module MUST be included in ALL projects
        /// </summary>
        Required = 1 << 1,
                   
        /// <summary>
        /// This module is the main module for a project
        /// </summary>
        ProjectModule = 1 << 2
    }
    
    /// <inheritdoc />
    /// <summary>
    /// A module can be thought of as a .csharp file
    /// It should define collections of included types, entityGroups, systems etc.
    /// It currently references component and struct types
    /// </summary>
    public sealed partial class UTinyModule : UTinyRegistryObjectBase, IPersistentObject
    {
        private static readonly EnumProperty<UTinyModule, UTinyTypeId> s_TypeIdProperty = new EnumProperty<UTinyModule, UTinyTypeId>("$TypeId",
            /* GET */ c => UTinyTypeId.Module,
            /* SET */ null
        );
        
        private static readonly Property<UTinyModule, string> s_NamespaceProperty = new Property<UTinyModule, string>("Namespace",
            /* GET */ c => c.m_Namespace,
            /* SET */ (c, v) => c.m_Namespace = v
        );
        
        private static readonly EnumProperty<UTinyModule, UTinyModuleOptions> s_OptionsProperty = new EnumProperty<UTinyModule, UTinyModuleOptions>("Options",
            /* GET */ c => c.m_Options,
            /* SET */ (c, v) => c.m_Options = v
        );

        private static readonly MutableContainerListProperty<UTinyModule, List<Reference>, Reference> s_DependenciesProperty =
            new MutableContainerListProperty<UTinyModule, List<Reference>, Reference>("Dependencies",
                /* GET */ c => c.m_Dependencies,
                /* SET */ null
            );
        
        private static readonly MutableContainerListProperty<UTinyModule, List<UTinyType.Reference>, UTinyType.Reference> s_ConfigurationsProperty =
            new MutableContainerListProperty<UTinyModule, List<UTinyType.Reference>, UTinyType.Reference>("Configurations",
                /* GET */ c => c.m_Configurations,
                /* SET */ null
            );

        private static readonly MutableContainerListProperty<UTinyModule, List<UTinyType.Reference>, UTinyType.Reference> s_ComponentsProperty =
            new MutableContainerListProperty<UTinyModule, List<UTinyType.Reference>, UTinyType.Reference>("Components",
                /* GET */ c => c.m_Components,
                /* SET */ null
            );

        private static readonly MutableContainerListProperty<UTinyModule, List<UTinyType.Reference>, UTinyType.Reference> s_StructsProperty =
            new MutableContainerListProperty<UTinyModule, List<UTinyType.Reference>, UTinyType.Reference>("Structs",
                /* GET */ c => c.m_Structs,
                /* SET */ null
            );

        private static readonly MutableContainerListProperty<UTinyModule, List<UTinyType.Reference>, UTinyType.Reference> s_EnumsProperty =
            new MutableContainerListProperty<UTinyModule, List<UTinyType.Reference>, UTinyType.Reference>("Enums",
                /* GET */ c => c.m_Enums,
                /* SET */ null
            );
        
        private static readonly MutableContainerListProperty<UTinyModule, List<UTinyEntityGroup.Reference>, UTinyEntityGroup.Reference> s_EntityGroupsProperty =
            new MutableContainerListProperty<UTinyModule, List<UTinyEntityGroup.Reference>, UTinyEntityGroup.Reference>("EntityGroups",
                /* GET */ c => c.m_EntityGroups,
                /* SET */ null
            );

        private static readonly MutableContainerListProperty<UTinyModule, List<UTinyScript.Reference>, UTinyScript.Reference> s_ScriptsProperty =
            new MutableContainerListProperty<UTinyModule, List<UTinyScript.Reference>, UTinyScript.Reference>("Scripts",
                /* GET */ c => c.m_Scripts,
                /* SET */ null
            );
        
        private static readonly MutableContainerListProperty<UTinyModule, List<UTinySystem.Reference>, UTinySystem.Reference> s_SystemsProperty =
            new MutableContainerListProperty<UTinyModule, List<UTinySystem.Reference>, UTinySystem.Reference>("Systems",
                /* GET */ c => c.m_Systems,
                /* SET */ null
            );
        
        private static readonly ContainerListProperty<UTinyModule, List<UTinyAsset>, UTinyAsset> s_AssetsPropery =
            new ContainerListProperty<UTinyModule, List<UTinyAsset>, UTinyAsset>("Assets",
                /* GET */ c => c.m_Assets,
                /* SET */ null
            );
        
        private static readonly MutableContainerProperty<UTinyModule, UTinyEntityGroup.Reference> s_StartupEntityGroupProperty = 
            new MutableContainerProperty<UTinyModule, UTinyEntityGroup.Reference>("StartupEntityGroup",
                /* GET */ c => c.m_StartupEntityGroup,
                /* SET */ (c, v) => c.m_StartupEntityGroup = v,
                /* REF */ (c, a, v) => a(ref c.m_StartupEntityGroup, v)
            );

        private static readonly IPropertyBag s_PropertyBag = new PropertyBag(
            s_TypeIdProperty,
            // inherited
            IdProperty,
            NameProperty,
            ExportFlagsProperty,
            DocumentationProperty,
            // end - inherited
            s_NamespaceProperty,
            s_OptionsProperty,
            s_DependenciesProperty,
            s_ConfigurationsProperty,
            s_ComponentsProperty,
            s_StructsProperty,
            s_EnumsProperty,
            s_EntityGroupsProperty,
            s_ScriptsProperty,
            s_SystemsProperty,
            s_AssetsPropery,
            s_StartupEntityGroupProperty);

        private string m_Namespace;
        private UTinyModuleOptions m_Options;
        private readonly List<Reference> m_Dependencies = new List<Reference>();
        private readonly List<UTinyType.Reference> m_Configurations = new List<UTinyType.Reference>();
        private readonly List<UTinyType.Reference> m_Components = new List<UTinyType.Reference>();
        private readonly List<UTinyType.Reference> m_Structs = new List<UTinyType.Reference>();
        private readonly List<UTinyType.Reference> m_Enums = new List<UTinyType.Reference>();
        private readonly List<UTinyEntityGroup.Reference> m_EntityGroups = new List<UTinyEntityGroup.Reference>();
        private readonly List<UTinyScript.Reference> m_Scripts = new List<UTinyScript.Reference>();
        private readonly List<UTinySystem.Reference> m_Systems = new List<UTinySystem.Reference>();
        private readonly List<UTinyAsset> m_Assets = new List<UTinyAsset>();
        private UTinyEntityGroup.Reference m_StartupEntityGroup;

        public string Namespace
        {
            get { return s_NamespaceProperty.GetValue(this); }
            set { s_NamespaceProperty.SetValue(this, value); }
        }
        
        public UTinyModuleOptions Options
        {
            get { return s_OptionsProperty.GetValue(this); }
            set { s_OptionsProperty.SetValue(this, value); }
        }

        public bool IsReadOnly => (Options & UTinyModuleOptions.ReadOnly) != 0;
        public bool IsRequired => (Options & UTinyModuleOptions.Required) != 0;
        public bool IsProjectModule => (Options & UTinyModuleOptions.ProjectModule) != 0;
        
        public IReadOnlyList<Reference> Dependencies => s_DependenciesProperty.GetValue(this).AsReadOnly();
        
        public IReadOnlyList<UTinyType.Reference> Configurations => s_ConfigurationsProperty.GetValue(this).AsReadOnly();
        public IReadOnlyList<UTinyType.Reference> Components => s_ComponentsProperty.GetValue(this).AsReadOnly(); 
        public IReadOnlyList<UTinyType.Reference> Structs => s_StructsProperty.GetValue(this).AsReadOnly();
        public IReadOnlyList<UTinyType.Reference> Enums => s_EnumsProperty.GetValue(this).AsReadOnly();
        
        public IEnumerable<UTinyType.Reference> Types
        {
            get
            {
                foreach (var r in s_ConfigurationsProperty.GetValue(this))
                {
                    yield return r;
                }
                foreach (var r in s_ComponentsProperty.GetValue(this))
                {
                    yield return r;
                }
                foreach (var r in s_StructsProperty.GetValue(this))
                {
                    yield return r;
                }
                foreach (var r in s_EnumsProperty.GetValue(this))
                {
                    yield return r;
                }
            }
        }
        
        public IReadOnlyList<UTinyEntityGroup.Reference> EntityGroups => s_EntityGroupsProperty.GetValue(this).AsReadOnly();
        public IReadOnlyList<UTinyScript.Reference> Scripts => s_ScriptsProperty.GetValue(this).AsReadOnly();
        public IReadOnlyList<UTinySystem.Reference> Systems => s_SystemsProperty.GetValue(this).AsReadOnly();
        public IReadOnlyList<UTinyAsset> Assets => s_AssetsPropery.GetValue(this).AsReadOnly();

        public UTinyEntityGroup.Reference StartupEntityGroup
        {
            get { return s_StartupEntityGroupProperty.GetValue(this); }
            set { s_StartupEntityGroupProperty.SetValue(this, value); }
        }
        
        public override IPropertyBag PropertyBag => s_PropertyBag;

        public UTinyModule(IRegistry registry, IVersionStorage versionStorage) : base(registry, versionStorage)
        {
            
        }
        
        public string PersistenceId { get; set; }

        public IEnumerable<IPropertyContainer> EnumeratePersistedObjects()
        {
            yield return this;
            
            var reg = Registry;
            foreach (var r in Types)
            {
                var o = r.Dereference(reg);
                if (null == o) continue;
                yield return o;
            }
            foreach (var r in EntityGroups)
            {
                var o = r.Dereference(reg);
                if (null == o) continue;
                yield return o;
                foreach (var er in o.Entities)
                {
                    var e = er.Dereference(reg);
                    if (null == e) continue;
                    yield return e;
                }
            }
            foreach (var r in Scripts)
            {
                var o = r.Dereference(reg);
                if (null == o) continue;
                yield return o;
            }
            foreach (var r in Systems)
            {
                var o = r.Dereference(reg);
                if (null == o) continue;
                yield return o;
            }
        }

        public override void Refresh()
        {
            for (var i = 0; i < m_Dependencies.Count; i++)
            {
                var s = m_Dependencies[i].Dereference(Registry);
                if (null != s)
                {
                    m_Dependencies[i] = (Reference) s;
                }
            } 
            
            for (var i = 0; i < m_Configurations.Count; i++)
            {
                var s = m_Configurations[i].Dereference(Registry);
                if (null != s)
                {
                    m_Configurations[i] = (UTinyType.Reference) s;
                }
            }
            
            for (var i = 0; i < m_Components.Count; i++)
            {
                var s = m_Components[i].Dereference(Registry);
                if (null != s)
                {
                    m_Components[i] = (UTinyType.Reference) s;
                }
            } 
            
            for (var i = 0; i < m_Structs.Count; i++)
            {
                var s = m_Structs[i].Dereference(Registry);
                if (null != s)
                {
                    m_Structs[i] = (UTinyType.Reference) s;
                }
            } 
            
            for (var i = 0; i < m_Enums.Count; i++)
            {
                var s = m_Enums[i].Dereference(Registry);
                if (null != s)
                {
                    m_Enums[i] = (UTinyType.Reference) s;
                }
            } 
            
            for (var i = 0; i < m_EntityGroups.Count; i++)
            {
                var s = m_EntityGroups[i].Dereference(Registry);
                if (null != s)
                {
                    m_EntityGroups[i] = (UTinyEntityGroup.Reference) s;
                }
            } 
            
            for (var i = 0; i < m_Systems.Count; i++)
            {
                var s = m_Systems[i].Dereference(Registry);
                if (null != s)
                {
                    m_Systems[i] = (UTinySystem.Reference) s;
                }
            }
            
            for (var i = 0; i < m_Scripts.Count; i++)
            {
                var s = m_Scripts[i].Dereference(Registry);
                if (null != s)
                {
                    m_Scripts[i] = (UTinyScript.Reference) s;
                }
            }
        }
        
        public void AddExplicitModuleDependency(Reference module)
        {
            if (s_DependenciesProperty.Contains(this, module))
            {
                return;
            }
            
            s_DependenciesProperty.Add(this, module);
        }
        
        public bool ContainsExplicitModuleDependency(Reference module)
        {
            return s_DependenciesProperty.Contains(this, module);
        }
        
        public void RemoveExplicitModuleDependency(Reference module)
        {
            s_DependenciesProperty.Remove(this, module);
        }
        
        public void AddConfigurationReference(UTinyType.Reference type)
        {
            s_ConfigurationsProperty.Add(this, type);
        }
        
        public void AddComponentReference(UTinyType.Reference type)
        {
            s_ComponentsProperty.Add(this, type);
        }
        
        public void AddStructReference(UTinyType.Reference type)
        {
            s_StructsProperty.Add(this, type);
        }
        
        public void AddEnumReference(UTinyType.Reference type)
        {
            s_EnumsProperty.Add(this, type);
        }

        public void RemoveTypeReference(UTinyType.Reference type)
        {
            s_ConfigurationsProperty.Remove(this, type);
            s_ComponentsProperty.Remove(this, type);
            s_StructsProperty.Remove(this, type);
            s_EnumsProperty.Remove(this, type);
        }
        
        public void AddEntityGroupReference(UTinyEntityGroup.Reference entityGroup)
        {
            s_EntityGroupsProperty.Add(this, entityGroup);
            
            if (StartupEntityGroup.Equals(UTinyEntityGroup.Reference.None))
            {
                StartupEntityGroup = entityGroup;
            }
        }
        
        public void RemoveEntityGroupReference(UTinyEntityGroup.Reference entityGroup)
        {
            if (s_EntityGroupsProperty.Contains(this, entityGroup))
            {
                s_EntityGroupsProperty.Remove(this, entityGroup);
            }

            if (StartupEntityGroup.Equals(entityGroup))
            {
                StartupEntityGroup = m_EntityGroups.FirstOrDefault();
            }
        }
        
        public void AddSystemReference(UTinySystem.Reference system)
        {
            s_SystemsProperty.Add(this, system);
        }
        
        public void RemoveSystemReference(UTinySystem.Reference system)
        {
            if (s_SystemsProperty.Contains(this, system))
            {
                s_SystemsProperty.Remove(this, system);
            }
        }
        
        public void AddScriptReference(UTinyScript.Reference script)
        {
            s_ScriptsProperty.Add(this, script);
        }
        
        public void RemoveScriptReference(UTinyScript.Reference script)
        {
            if (s_ScriptsProperty.Contains(this, script))
            {
                s_ScriptsProperty.Remove(this, script);
            }
        }
        
        public UTinyAsset AddAsset(UnityEngine.Object @object)
        {
            Assert.IsNotNull(@object);
            var asset = new UTinyAsset(this) {Object = @object};

            var index = s_AssetsPropery.IndexOf(this, asset);
            if (index != -1)
            {
                // return the original instance
                return s_AssetsPropery.GetItemAt(this, index);
            }
            
            s_AssetsPropery.Add(this, asset);
            return asset;
        }
        
        public void RemoveAsset(UnityEngine.Object @object)
        {
            var index = m_Assets.FindIndex(a => a.Object == @object);
            if (index < 0)
            {
                return;
            }
            s_AssetsPropery.RemoveAt(this, index);
        }

        public UTinyAsset GetAsset(UnityEngine.Object @object)
        {
            return m_Assets.Find(a => a.Object == @object);
        }

        public UTinyAsset GetOrAddAsset(UnityEngine.Object @object)
        {
            return GetAsset(@object) ?? AddAsset(@object);
        }

        /// <summary>
        /// @NOTE Includes self in the returned elements
        /// </summary>
        /// <returns></returns>
        public IEnumerable<UTinyModule> EnumerateDependencies()
        {
            return EnumerateRefDependencies().Deref(Registry);
        } 

        public IEnumerable<Reference> EnumerateRefDependencies()
        {
            var visited = new HashSet<Reference>();

            foreach (var dependency in EnumerateRefDependencies(visited))
            {
                yield return dependency;
            }
        }

        private IEnumerable<Reference> EnumerateRefDependencies(ISet<Reference> visited)
        {
            if (visited.Add((Reference)this))
            {
                yield return (Reference)this;
            }
            else
            {
                yield break;
            }
            
            foreach (var reference in Dependencies)
            {
                var module = reference.Dereference(Registry);

                if (null == module)
                {
                    continue;
                }

                foreach (var dependency in module.EnumerateRefDependencies(visited))
                {
                    yield return dependency;
                }
            }
        }
        
        /// <summary>
        /// Returns a list of modules that explicitly depend on the given module
        /// </summary>
        public static IEnumerable<UTinyModule> GetExplicitDependantModules(IRegistry registry, Reference module)
        {
            return registry.FindAllByType<UTinyModule>().Where(m => m.ContainsExplicitModuleDependency(module)).ToList();
        }
        
        /// <summary>
        /// Returns a depth first search of the System execution graph
        /// </summary>
        /// <returns></returns>
        public IEnumerable<UTinySystem.Reference> GetSystemExecutionOrder()
        {
            var systems = new List<UTinySystem.Reference>();

            var graph = GetSystemGraph();

            DetectCycle(graph);

            AcyclicGraphIterator.DepthFirst.Execute(GetSystemGraph(), system => systems.Add(system));

            return systems;
        }

        private static void DetectCycle(AcyclicGraph<UTinySystem.Reference> graph)
        {
            var count = 0;
            var error = string.Empty;

            AcyclicGraphIterator.DetectCycle.Execute(graph,
                () =>
                {
                    error += $"[{UTinyConstants.ApplicationName}] SystemExecutionGraph detected cyclic reference (";
                }, 
                () =>
                {
                    error += ")";
                }, 
                system =>
                {
                    if (count != 0)
                    {
                        error += ", ";
                    }
                    
                    error += system.Name;
                    
                    count++;
                });

            if (!string.IsNullOrEmpty(error))
            {
                Debug.LogError(error);
            }
        }

        /// <summary>
        /// Builds an AcyclicGraph from the included systems
        /// </summary>
        private AcyclicGraph<UTinySystem.Reference> GetSystemGraph()
        {
            var graph = new AcyclicGraph<UTinySystem.Reference>();

            // Add all systems
            foreach (var module in EnumerateDependencies())
            {
                foreach (var systemRef in module.Systems)
                {
                    graph.Add(systemRef);
                }
            }

            // Connect all first level dependencies
            foreach (var node in graph.Nodes.ToList())
            {
                var systemRef = node.Data;
                var system = systemRef.Dereference(Registry);

                foreach (var dependencyRef in system.ExecuteAfter)
                {
                    var depedencyNode = graph.GetOrAdd(dependencyRef);
                    graph.AddDirectedConnection(node, depedencyNode);
                }

                foreach (var dependencyRef in system.ExecuteBefore)
                {
                    var depedencyNode = graph.GetOrAdd(dependencyRef);
                    graph.AddDirectedConnection(depedencyNode, node);
                }
            }

            return graph;
        }

        public override string ToString()
        {
            return Serialization.FlatJson.BackEnd.Persist(this);
        }

        /// <inheritdoc cref="IPropertyContainer"/>
        /// <summary>
        /// Weak reference to a type that can be serialized
        /// </summary>
        public struct Reference : IReference<UTinyModule>, IPropertyContainer, IEquatable<Reference>
        {
            private static readonly StructProperty<Reference, UTinyId> s_IdProperty = new StructProperty<Reference, UTinyId>("Id",
                    (ref Reference c) => c.m_Id,
                    null
                ).WithAttribute(InspectorAttributes.HideInInspector)
                .WithAttribute(InspectorAttributes.Readonly);

            private static readonly StructProperty<Reference, string> s_NameProperty = new StructProperty<Reference, string>("Name",
                (ref Reference c) => c.m_Name,
                (ref Reference c, string v) => c.m_Name = v
            );

            private static readonly IPropertyBag s_PropertyBag = new PropertyBag(
                s_IdProperty,
                s_NameProperty);

            public static Reference None { get; } = new Reference(UTinyId.Empty, string.Empty);

            private readonly UTinyId m_Id;
            private string m_Name;

            public UTinyId Id => s_IdProperty.GetValue(ref this);
            public string Name => s_NameProperty.GetValue(ref this);
            public IPropertyBag PropertyBag => s_PropertyBag;
            public IVersionStorage VersionStorage => PassthroughVersionStorage.Instance;

            public Reference(UTinyId id, string name)
            {
                m_Id = id;
                m_Name = name;
            }

            public UTinyModule Dereference(IRegistry registry)
            {
                return registry.Dereference<Reference, UTinyModule>(this);
            }

            public static explicit operator Reference(UTinyModule @object)
            {
                return new Reference(@object.Id, @object.Name);
            }

            public override string ToString()
            {
                return "Reference " + Name;
            }

            public bool Equals(Reference other)
            {
                return m_Id.Equals(other.m_Id);
            }

            public override bool Equals(object obj)
            {
                if (ReferenceEquals(null, obj)) return false;
                return obj is Reference && Equals((Reference) obj);
            }

            public override int GetHashCode()
            {
                return m_Id.GetHashCode();
            }
        }
    }
}
#endif // NET_4_6
