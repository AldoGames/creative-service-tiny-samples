#if NET_4_6
using Unity.Properties;

using Unity.Tiny.Extensions;

namespace Unity.Tiny.Jira
{
    internal class JiraDataContainer : IPropertyContainer
    {
        private FieldContainer m_Fields = new FieldContainer("fields");

        private static IProperty<JiraDataContainer, FieldContainer> s_FieldsProperty =
            new Property<JiraDataContainer, FieldContainer>("fields".DoubleQuoted(),
                /* GET */ p => p.m_Fields,
                /* SET */ null);

        private static PropertyBag s_Bag = new PropertyBag(s_FieldsProperty);

        public IVersionStorage VersionStorage => PassthroughVersionStorage.Instance;
        public IPropertyBag PropertyBag => s_Bag;
        
        public void Add(IField field)
        {
            s_FieldsProperty.GetValue(this).Value.Add(field);
        }

        public string GetDataAsJSon()
        {
            return m_Fields.ToString().Braced();
        }
    }
}
#endif
