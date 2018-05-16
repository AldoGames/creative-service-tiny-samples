#if NET_4_6
using System;
using System.Collections.Generic;

namespace Unity.Tiny.Filters
{
    using Tiny;

    public static partial class Filter
    {
        #region API
        public static IEnumerable<UTinyObject> GetAllComponents(this IEnumerable<UTinyEntity> source)
        {
            if (null == source)
            {
                throw new ArgumentNullException(nameof(source));
            }
            return source.GetAllComponentsImpl();
        }

        public static IEnumerable<UTinyObject> GetComponents(this IEnumerable<UTinyEntity> source, UTinyType.Reference typeRef)
        {
            if (null == source)
            {
                throw new ArgumentNullException(nameof(source));
            }

            if (UTinyType.Reference.None.Equals(typeRef))
            {
                throw new ArgumentException(nameof(typeRef));
            }

            return source.GetComponentsImpl(typeRef);
        }

        public static IEnumerable<UTinyObject> GetComponents(this IEnumerable<UTinyEntity> source, UTinyType type)
        {
            return source.GetComponents((UTinyType.Reference)type);
        }

        public static IEnumerable<UTinyEntity> WithComponent(this IEnumerable<UTinyEntity> source, UTinyType.Reference typeRef)
        {
            if (null == source)
            {
                throw new ArgumentNullException(nameof(source));
            }
            
            if (UTinyType.Reference.None.Equals(typeRef))
            {
                throw new ArgumentException(nameof(typeRef));
            }

            return source.WithComponentImpl(typeRef);
        }

        public static IEnumerable<UTinyEntity> WithComponent(this IEnumerable<UTinyEntity> source, UTinyType type)
        {
            return source.WithComponent((UTinyType.Reference)type);
        }

        public static IEnumerable<UTinyEntity> WithoutComponent(this IEnumerable<UTinyEntity> source, UTinyType.Reference typeRef)
        {
            if (null == source)
            {
                throw new ArgumentNullException(nameof(source));
            }

            if (UTinyType.Reference.None.Equals(typeRef))
            {
                throw new ArgumentException(nameof(typeRef));
            }

            return source.WithoutComponentImpl(typeRef);
        }

        public static IEnumerable<UTinyEntity> WithoutComponent(this IEnumerable<UTinyEntity> source, UTinyType type)
        {
            return source.WithoutComponent((UTinyType.Reference)type);
        }
        #endregion // API

        #region Implementation

        private static IEnumerable<UTinyObject> GetAllComponentsImpl(this IEnumerable<UTinyEntity> source)
        {
            foreach(var entity in source)
            {
                foreach(var component in entity.Components)
                {
                    yield return component;
                }
            }
        }

        private static IEnumerable<UTinyObject> GetComponentsImpl(this IEnumerable<UTinyEntity> source, UTinyType.Reference typeRef)
        {
            foreach (var entity in source)
            {
                var component = entity.GetComponent(typeRef);
                if (null != component)
                {
                    yield return component;
                }
            }
        }

        private static IEnumerable<UTinyEntity> WithComponentImpl(this IEnumerable<UTinyEntity> source, UTinyType.Reference typeRef)
        {
            foreach (var entity in source)
            {
                var component = entity.GetComponent(typeRef);
                if (null != component)
                {
                    yield return entity;
                }
            }
        }

        private static IEnumerable<UTinyEntity> WithoutComponentImpl(this IEnumerable<UTinyEntity> source, UTinyType.Reference typeRef)
        {
            foreach (var entity in source)
            {
                var component = entity.GetComponent(typeRef);
                if (null == component)
                {
                    yield return entity;
                }
            }
        }
        #endregion // Implementation
    }

}
#endif // NET_4_6
