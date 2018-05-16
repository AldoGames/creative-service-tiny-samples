#if NET_4_6
using System;
using System.Linq;
using System.Collections.Generic;

namespace Unity.Tiny.Filters
{
    using Tiny;

    public static partial class Filter
    {
        #region API
        public static IEnumerable<UTinyModule> Deref(this IEnumerable<UTinyModule.Reference> source, IRegistry registry)
        {
            return source.Cast<IReference<UTinyModule>>().Deref(registry);
        }

        public static IEnumerable<UTinyEntityGroup> Deref(this IEnumerable<UTinyEntityGroup.Reference> source, IRegistry registry)
        {
            return source.Cast<IReference<UTinyEntityGroup>>().Deref(registry);
        }

        public static IEnumerable<UTinyScript> Deref(this IEnumerable<UTinyScript.Reference> source, IRegistry registry)
        {
            return source.Cast<IReference<UTinyScript>>().Deref(registry);
        }

        public static IEnumerable<UTinySystem> Deref(this IEnumerable<UTinySystem.Reference> source, IRegistry registry)
        {
            return source.Cast<IReference<UTinySystem>>().Deref(registry);
        }

        public static IEnumerable<UTinyType> Deref(this IEnumerable<UTinyType.Reference> source, IRegistry registry)
        {
            return source.Cast<IReference<UTinyType>>().Deref(registry);
        }

        public static IEnumerable<UTinyEntity> Deref(this IEnumerable<UTinyEntity.Reference> source, IRegistry registry)
        {
            return source.Cast<IReference<UTinyEntity>>().Deref(registry);
        }

        public static IEnumerable<UTinyModule> MissingRef(this IEnumerable<UTinyModule.Reference> source, IRegistry registry)
        {
            return source.Cast<IReference<UTinyModule>>().MissingRef(registry);
        }

        public static IEnumerable<UTinyEntityGroup> MissingRef(this IEnumerable<UTinyEntityGroup.Reference> source, IRegistry registry)
        {
            return source.Cast<IReference<UTinyEntityGroup>>().MissingRef(registry);
        }

        public static IEnumerable<UTinyScript> MissingRef(this IEnumerable<UTinyScript.Reference> source, IRegistry registry)
        {
            return source.Cast<IReference<UTinyScript>>().MissingRef(registry);
        }

        public static IEnumerable<UTinySystem> MissingRef(this IEnumerable<UTinySystem.Reference> source, IRegistry registry)
        {
            return source.Cast<IReference<UTinySystem>>().MissingRef(registry);
        }

        public static IEnumerable<UTinyType> MissingRef(this IEnumerable<UTinyType.Reference> source, IRegistry registry)
        {
            return source.Cast<IReference<UTinyType>>().MissingRef(registry);
        }

        public static IEnumerable<UTinyEntity> MissingRef(this IEnumerable<UTinyEntity.Reference> source, IRegistry registry)
        {
            return source.Cast<IReference<UTinyEntity>>().MissingRef(registry);
        }

        #endregion // API

        #region Implementation
        private static IEnumerable<T> Deref<T>(this IEnumerable<IReference<T>> source, IRegistry registry) where T : class
        {
            if (null == source)
            {
                throw new ArgumentNullException(nameof(source));
            }

            if (null == registry)
            {
                throw new ArgumentNullException(nameof(registry));
            }
            return source.DerefImpl(registry);
        }

        private static IEnumerable<T> DerefImpl<T>(this IEnumerable<IReference<T>> source, IRegistry registry) where T : class
        {
            foreach (var sRef in source)
            {
                var s = sRef.Dereference(registry);
                if (null != s)
                {
                    yield return s;
                }
            }
        }

        private static IEnumerable<T> MissingRef<T>(this IEnumerable<IReference<T>> source, IRegistry registry) where T : class
        {
            if (null == source)
            {
                throw new ArgumentNullException(nameof(source));
            }

            if (null == registry)
            {
                throw new ArgumentNullException(nameof(registry));
            }
            return source.MissingRefImpl(registry);
        }

        private static IEnumerable<T> MissingRefImpl<T>(this IEnumerable<IReference<T>> source, IRegistry registry) where T : class
        {
            foreach (var sRef in source)
            {
                var s = sRef.Dereference(registry);
                if (null == s)
                {
                    yield return s;
                }
            }
        }
        #endregion // Implementation
    }
}
#endif // NET_4_6
