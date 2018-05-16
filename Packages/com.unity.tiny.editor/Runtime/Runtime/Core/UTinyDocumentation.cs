#if NET_4_6
using Unity.Properties;

namespace Unity.Tiny
{
    public class UTinyDocumentation : IPropertyContainer
    {
        private static readonly Property<UTinyDocumentation, string> s_SummaryProperty 
            = new Property<UTinyDocumentation, string>("Summary",
            c => c.m_Summary,
            (c, v) => c.m_Summary = v
        );

        private static readonly IPropertyBag s_PropertyBag = new PropertyBag(
            s_SummaryProperty
        );

        private string m_Summary = string.Empty;

        public string Summary
        {
            get { return s_SummaryProperty.GetValue(this); }
            set { s_SummaryProperty.SetValue(this, value); }
        }

        public IPropertyBag PropertyBag => s_PropertyBag;
        public IVersionStorage VersionStorage { get; }

        public UTinyDocumentation(IVersionStorage versionStorage)
        {
            VersionStorage = versionStorage;
        }
    }
}
#endif // NET_4_6
