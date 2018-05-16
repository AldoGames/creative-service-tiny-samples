#if NET_4_6
using System;
using System.Collections.Generic;

namespace Unity.Tiny.Filters
{
    using Tiny;

    public static partial class Filter
    {
        #region API
        public static IEnumerable<UTinyEntity.Reference> EntityRefs(this IEnumerable<UTinyEntityGroup> source)
        {
            if (null == source)
            {
                throw new ArgumentNullException(nameof(source));
            }
            return source.EntityRefsImpl();
        }

        public static IEnumerable<UTinyEntity> Entities(this IEnumerable<UTinyEntityGroup> source)
        {
            if (null == source)
            {
                throw new ArgumentNullException(nameof(source));
            }
            return source.EntitiesImpl();
        }
        #endregion // API

        #region Implementation
        private static IEnumerable<UTinyEntity.Reference> EntityRefsImpl(this IEnumerable<UTinyEntityGroup> source)
        {
            foreach(var entityGroup in source)
            {
                foreach(var entityRef in entityGroup.Entities)
                {
                    if (!UTinyEntity.Reference.None.Equals(entityRef))
                    {
                        yield return entityRef;
                    }
                }
            }
        }

        private static IEnumerable<UTinyEntity> EntitiesImpl(this IEnumerable<UTinyEntityGroup> source)
        {
            foreach (var entityGroup in source)
            {
                foreach (var entityRef in entityGroup.Entities)
                {
                    var entity = entityRef.Dereference(entityGroup.Registry);
                    if (null != entity)
                    {
                        yield return entity;
                    }
                }
            }
        }
        #endregion // Implementation
    }

}
#endif // NET_4_6
