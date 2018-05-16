#if NET_4_6
using System;
using System.Collections.Generic;
using Unity.Properties;
using Unity.Tiny.Attributes;

namespace Unity.Tiny
{
	public sealed partial class UTinyEntityGroup : UTinyRegistryObjectBase
	{
		private static readonly EnumProperty<UTinyEntityGroup, UTinyTypeId> s_TypeIdProperty =
			new EnumProperty<UTinyEntityGroup, UTinyTypeId>("$TypeId",
				/* GET */ c => UTinyTypeId.Scene,
				/* SET */ null
			);

		private static readonly MutableContainerListProperty<UTinyEntityGroup, IList<UTinyEntity.Reference>, UTinyEntity.Reference>
			EntitiesProperty = new MutableContainerListProperty<UTinyEntityGroup, IList<UTinyEntity.Reference>, UTinyEntity.Reference>(
				"Entities",
				/* GET */ c => c.m_Entities ?? (c.m_Entities = new List<UTinyEntity.Reference>()),
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
			EntitiesProperty);

		private List<UTinyEntity.Reference> m_Entities;

		public IList<UTinyEntity.Reference> Entities => EntitiesProperty.GetValue(this);
		public override IPropertyBag PropertyBag => s_PropertyBag;
		
		public UTinyEntityGroup(IRegistry registry, IVersionStorage versionStorage) 
			: base(registry, versionStorage)
		{
			
		}
		
		public void AddEntityReference(UTinyEntity.Reference entity)
		{
			EntitiesProperty.Add(this, entity);
		}

        public void RemoveEntityReference(UTinyEntity.Reference entity)
        {
            EntitiesProperty.Remove(this, entity);
        }

        public void ClearEntityReferences()
        {
            EntitiesProperty.Clear(this);
        }

		public override void Refresh()
		{
			if (null == m_Entities)
			{
				return;
			}
			
			for (var i = 0; i < m_Entities.Count; i++)
			{
				var s = m_Entities[i].Dereference(Registry);
				if (null != s)
				{
					m_Entities[i] = (UTinyEntity.Reference) s;
				}
			}
		}
		
		/// <inheritdoc cref="IPropertyContainer"/>
        /// <summary>
        /// Weak reference to a type that can be serialized
        /// </summary>
        public struct Reference : IReference<UTinyEntityGroup>, IPropertyContainer, IEquatable<Reference>
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

            public UTinyEntityGroup Dereference(IRegistry registry)
            {
                return registry.Dereference<Reference, UTinyEntityGroup>(this);
            }

            public static explicit operator Reference(UTinyEntityGroup @object)
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
