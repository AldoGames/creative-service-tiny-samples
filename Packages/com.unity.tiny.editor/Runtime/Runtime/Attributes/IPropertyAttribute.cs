#if NET_4_6
using Unity.Properties;

namespace Unity.Tiny.Attributes
{
    public interface IPropertyAttribute { }
    public interface IAttributable { }

    // @TODO: Unify the two following classes. Best way would be to add the IAttributable on the IProperty and get rid
    //        of the IPropertyAttribyteExtensions.
    public static class IPropertyAttributeExtensions
    {
        public static void AddAttribute<TAttribute>(this IProperty property, TAttribute attribute) where TAttribute : class, IPropertyAttribute
        {
            if (null == property)
            {
                return;
            }
            UTinyPropertyMetaData<IProperty>.RegisterAttribute(property, attribute);
        }

        public static void RemoveAttribute<TAttribute>(this IProperty property, TAttribute attribute) where TAttribute : class, IPropertyAttribute
        {
            if (null == property)
            {
                return;
            }
            UTinyPropertyMetaData<IProperty>.UnregisterAttribute(property, attribute);
        }

        public static bool HasAttribute<TAttribute>(this IProperty property) where TAttribute : class, IPropertyAttribute
        {
            if (null == property)
            {
                return false;
            }
            return UTinyPropertyMetaData<IProperty>.HasAttribute<TAttribute>(property);
        }

        public static TAttribute GetAttribute<TAttribute>(this IProperty property) where TAttribute : class, IPropertyAttribute
        {
            if (null == property)
            {
                return null;
            }
            return UTinyPropertyMetaData<IProperty>.GetAttribute<TAttribute>(property);
        }

        public static TProperty WithAttribute<TAttribute, TProperty>(this TProperty property, TAttribute attribute)
             where TAttribute : class, IPropertyAttribute
             where TProperty : IProperty
        {
            property.AddAttribute(attribute); 
            return property;
        }
    }

    public static class AttributableExtensions
    {
        public static void AddAttribute<TAttribute>(this IAttributable attributable, TAttribute attribute) where TAttribute : class, IPropertyAttribute
        {
            if (null == attributable)
            {
                return;
            }
            UTinyPropertyMetaData<IAttributable>.RegisterAttribute(attributable, attribute);
        }

        public static void RemoveAttribute<TAttribute>(this IAttributable attributable, TAttribute attribute) where TAttribute : class, IPropertyAttribute
        {
            if (null == attributable)
            {
                return;
            }
            UTinyPropertyMetaData<IAttributable>.UnregisterAttribute(attributable, attribute);
        }

        public static bool HasAttribute<TAttribute>(this IAttributable attributable) where TAttribute : class, IPropertyAttribute
        {
            if (null == attributable)
            {
                return false;
            }
            return UTinyPropertyMetaData<IAttributable>.HasAttribute<TAttribute>(attributable);
        }

        public static TAttribute GetAttribute<TAttribute>(this IAttributable attributable) where TAttribute : class, IPropertyAttribute
        {
            if (null == attributable)
            {
                return null;
            }
            return UTinyPropertyMetaData<IAttributable>.GetAttribute<TAttribute>(attributable);
        }

        public static IAttributable WithAttribute<TAttribute>(this IAttributable attributable, TAttribute attribute)
            where TAttribute : class, IPropertyAttribute
        {
            attributable.AddAttribute(attribute);
            return attributable;
        }
    }
}
#endif // NET_4_6
