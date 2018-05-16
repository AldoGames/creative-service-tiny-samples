#if NET_4_6
using System;
using System.Collections.Generic;

namespace Unity.Tiny.Filters
{
    public enum Comparison
    {
        Contains = 0,
        StartsWith = 1,
        EndsWith = 2,
        Exact = 3,
        DoesNotContain = 4,
        DoesNotStartWith = 5,
        DoesNotEndWith = 6,
        NotExact = 7,
    }

    public static partial class Filter
    {
        #region API

        public static IEnumerable<T> WithName<T>(this IEnumerable<T> source, string name, Comparison comparator = Comparison.Exact) where T : IRegistryObject
        {
            if (null == source)
            {
                throw new ArgumentNullException(nameof(source));
            }

            if (null == name)
            {
                throw new ArgumentNullException(nameof(name));
            }
            return source.WithNameImpl(name, comparator);
        }

        public static IEnumerable<UTinyObject> WithName(this IEnumerable<UTinyObject> source, string name, Comparison comparator = Comparison.Exact)
        {
            if (null == source)
            {
                throw new ArgumentNullException(nameof(source));
            }

            if (null == name)
            {
                throw new ArgumentNullException(nameof(name));
            }
            return source.WithNameImpl(name, comparator);
        }
        #endregion // API

        #region Implementation
        private static IEnumerable<T> WithNameImpl<T>(this IEnumerable<T> entities, string name, Comparison comparator) where T : IRegistryObject
        {
            foreach (var entity in entities)
            {
                if (Compare(entity.Name, name, comparator))
                {
                    yield return entity;
                }
            }
        }

        private static IEnumerable<UTinyObject> WithNameImpl(this IEnumerable<UTinyObject> entities, string name, Comparison comparator)
        {
            foreach (var entity in entities)
            {
                if (Compare(entity.Name, name, comparator))
                {
                    yield return entity;
                }
            }
        }

        private static bool Compare(string source, string value, Comparison comparator)
        {
            switch (comparator)
            {
                case Comparison.Contains:         return  source.IndexOf(value, StringComparison.InvariantCultureIgnoreCase) >= 0;
                case Comparison.StartsWith:       return  source.StartsWith(value, StringComparison.InvariantCultureIgnoreCase);
                case Comparison.EndsWith:         return  source.EndsWith(value, StringComparison.InvariantCultureIgnoreCase);
                case Comparison.Exact:            return  source.Equals(value, StringComparison.InvariantCultureIgnoreCase);
                case Comparison.DoesNotContain:   return  source.IndexOf(value, StringComparison.InvariantCultureIgnoreCase) < 0;
                case Comparison.DoesNotStartWith: return !source.StartsWith(value, StringComparison.InvariantCultureIgnoreCase);
                case Comparison.DoesNotEndWith:   return !source.EndsWith(value, StringComparison.InvariantCultureIgnoreCase);
                default:
                    throw new InvalidOperationException();
            }
        }
        #endregion // Implementation
    }
}
#endif // NET_4_6
