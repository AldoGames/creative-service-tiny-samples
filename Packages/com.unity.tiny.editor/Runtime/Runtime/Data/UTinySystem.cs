#if NET_4_6
using System;
using System.Collections.Generic;
using Unity.Properties;
using UnityEngine;
using Unity.Tiny.Attributes;

namespace Unity.Tiny
{
    [Flags]
    public enum UTinySystemOptions
    {
        None = 0,

        /// <summary>
        /// Is this system enabled
        /// </summary>
        Enabled = 1 << 0,

        /// <summary>
        /// Should the iterator boilerplate be included during export?
        /// </summary>
        IncludeIterator = 1 << 1,
        
        /// <summary>
        /// Is this system defined externally?
        /// </summary>
        External = 1 << 2,

        All = Enabled | IncludeIterator
    }

    /// <inheritdoc cref="UTinyRegistryObjectBase" />
    /// <summary>
    /// </summary>
    public sealed class UTinySystem : UTinyRegistryObjectBase, IPropertyContainer
    {
        private static readonly EnumProperty<UTinySystem, UTinyTypeId> s_TypeIdProperty =
            new EnumProperty<UTinySystem, UTinyTypeId>("$TypeId",
                    /* GET */ c => UTinyTypeId.System,
                    /* SET */ null
                ).WithAttribute(InspectorAttributes.HideInInspector)
                .WithAttribute(InspectorAttributes.Readonly);

        private static readonly EnumProperty<UTinySystem, UTinySystemOptions> s_OptionsProperty =
            new EnumProperty<UTinySystem, UTinySystemOptions>(
                "Options",
                /* GET */ c => c.m_Options,
                /* SET */ (c, v) => c.m_Options = v
            );

        private static readonly Property<UTinySystem, TextAsset> s_TextAssetProperty =
            new Property<UTinySystem, TextAsset>(
                "TextAsset",
                /* GET */ c => c.m_TextAsset,
                /* SET */ (c, v) => c.m_TextAsset = v
            );

        private static readonly MutableContainerListProperty<UTinySystem, List<UTinyType.Reference>, UTinyType.Reference>
            s_ComponentsProperty =
                new MutableContainerListProperty<UTinySystem, List<UTinyType.Reference>, UTinyType.Reference>(
                    "Components",
                    /* GET */ c => c.m_Components ?? (c.m_Components = new List<UTinyType.Reference>()),
                    /* SET */ null
                );

        private static readonly MutableContainerListProperty<UTinySystem, List<Reference>, Reference>
            s_ExecuteAfterProperty =
                new MutableContainerListProperty<UTinySystem, List<Reference>, Reference>(
                    "ExecuteAfter",
                    /* GET */ c => c.m_ExecuteAfter ?? (c.m_ExecuteAfter = new List<Reference>()),
                    /* SET */ null
                );

        private static readonly MutableContainerListProperty<UTinySystem, List<Reference>, Reference>
            s_ExecuteBeforeProperty =
                new MutableContainerListProperty<UTinySystem, List<Reference>, Reference>(
                    "ExecuteBefore",
                    /* GET */ c => c.m_ExecuteBefore ?? (c.m_ExecuteBefore = new List<Reference>()),
                    /* SET */ null
                );

        private static readonly IPropertyBag s_PropertyBag = new PropertyBag(
            s_TypeIdProperty,
            // inherited
            IdProperty,
            NameProperty,
            ExportFlagsProperty,
            DocumentationProperty,
            // end - inherited
            s_OptionsProperty,
            s_TextAssetProperty,
            s_ComponentsProperty,
            s_ExecuteAfterProperty,
            s_ExecuteBeforeProperty);

        private UTinySystemOptions m_Options;
        private TextAsset m_TextAsset;
        private List<UTinyType.Reference> m_Components;
        private List<Reference> m_ExecuteAfter;
        private List<Reference> m_ExecuteBefore;

        public UTinySystemOptions Options
        {
            get { return s_OptionsProperty.GetValue(this); }
            set { s_OptionsProperty.SetValue(this, value); }
        }

        public bool Enabled
        {
            get { return (Options & UTinySystemOptions.Enabled) != 0; }
            set
            {
                if (value)
                {
                    Options |= UTinySystemOptions.Enabled;
                }
                else
                {
                    Options &= ~UTinySystemOptions.Enabled;
                }
            }
        }

        public bool IncludeIterator
        {
            get { return (Options & UTinySystemOptions.IncludeIterator) != 0; }
            set
            {
                if (value)
                {
                    Options |= UTinySystemOptions.IncludeIterator;
                }
                else
                {
                    Options &= ~UTinySystemOptions.IncludeIterator;
                }
            }
        }
        
        public bool External
        {
            get { return (Options & UTinySystemOptions.External) != 0; }
            set
            {
                if (value)
                {
                    Options |= UTinySystemOptions.External;
                }
                else
                {
                    Options &= ~UTinySystemOptions.External;
                }
            }
        }

        public TextAsset TextAsset
        {
            get { return s_TextAssetProperty.GetValue(this); }
            set { s_TextAssetProperty.SetValue(this, value); }
        }

        public IReadOnlyList<UTinyType.Reference> Components => s_ComponentsProperty.GetValue(this).AsReadOnly();
        public IReadOnlyList<Reference> ExecuteAfter => s_ExecuteAfterProperty.GetValue(this).AsReadOnly();
        public IReadOnlyList<Reference> ExecuteBefore => s_ExecuteBeforeProperty.GetValue(this).AsReadOnly();
        public override IPropertyBag PropertyBag => s_PropertyBag;

        public UTinySystem(IRegistry registry, IVersionStorage versionStorage) : base(registry, versionStorage)
        {
        }

        public override void Refresh()
        {
            if (null != m_Components)
            {
                for (var i = 0; i < m_Components.Count; i++)
                {
                    var s = m_Components[i].Dereference(Registry);
                    if (null != s)
                    {
                        m_Components[i] = (UTinyType.Reference) s;
                    }
                }
            }

            if (null != m_ExecuteAfter)
            {
                for (var i = 0; i < m_ExecuteAfter.Count; i++)
                {
                    var s = m_ExecuteAfter[i].Dereference(Registry);
                    if (null != s)
                    {
                        m_ExecuteAfter[i] = (Reference) s;
                    }
                }
            }

            if (null != m_ExecuteBefore)
            {
                for (var i = 0; i < m_ExecuteBefore.Count; i++)
                {
                    var s = m_ExecuteBefore[i].Dereference(Registry);
                    if (null != s)
                    {
                        m_ExecuteBefore[i] = (Reference) s;
                    }
                }
            }
        }

        public void AddComponentReference(UTinyType.Reference type)
        {
            s_ComponentsProperty.Add(this, type);
        }

        public void ClearComponentReferences()
        {
            s_ComponentsProperty.Clear(this);
        }

        public void AddExecuteAfterReference(Reference system)
        {
            s_ExecuteAfterProperty.Add(this, system);
        }

        public void ClearExecuteAfterReferences()
        {
            s_ExecuteAfterProperty.Clear(this);
        }

        public void AddExecuteBeforeReference(Reference system)
        {
            s_ExecuteBeforeProperty.Add(this, system);
        }

        public void ClearExecuteBeforeReferences()
        {
            s_ExecuteBeforeProperty.Clear(this);
        }

        /// <inheritdoc cref="IPropertyContainer"/>
        /// <summary>
        /// Weak reference to a type that can be serialized
        /// </summary>
        public struct Reference : IReference<UTinySystem>, IPropertyContainer, IEquatable<Reference>
        {
            private static readonly StructProperty<Reference, UTinyId> s_IdProperty =
                new StructProperty<Reference, UTinyId>("Id",
                        (ref Reference c) => c.m_Id,
                        null
                    ).WithAttribute(InspectorAttributes.HideInInspector)
                    .WithAttribute(InspectorAttributes.Readonly);

            private static readonly StructProperty<Reference, string> s_NameProperty =
                new StructProperty<Reference, string>("Name",
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

            public UTinySystem Dereference(IRegistry registry)
            {
                return registry.Dereference<Reference, UTinySystem>(this);
            }

            public static explicit operator Reference(UTinySystem @object)
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
