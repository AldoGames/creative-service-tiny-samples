#if NET_4_6
namespace Unity.Tiny
{
    /*
    public static class UTinyUnityObject
    {
        public struct Reference : IReference, IPropertyContainer
        {
            private static readonly Property<Reference, UTinyId> s_IdProperty = new Property<Reference, UTinyId>("Id",
                (ref Reference c) => c.m_Id,
                null
            ).WithAttribute(Readonly);
            
            private static readonly StringProperty<Reference> s_NameProperty = new StringProperty<Reference>("Name",
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

            public UnityEngine.Object Dereference(IAssetDatabase database)
            {
                return database.Dereference(ref this);
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
    */
}
#endif // NET_4_6
