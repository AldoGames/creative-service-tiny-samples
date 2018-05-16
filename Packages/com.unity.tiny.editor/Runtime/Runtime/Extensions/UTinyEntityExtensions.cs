#if NET_4_6
using UnityEngine.Assertions;

namespace Unity.Tiny.Extensions
{
    using Tiny;

    public static class UTinyTransformExtensions
    {
        public static bool HasTransform(this UTinyEntity self)
        {
            if (null == self)
            {
                return false;
            }
            return null != self.GetComponent(self.Registry.GetTransformType()); 
        }

        public static void SetParent(this UTinyEntity self, UTinyEntity.Reference parentRef)
        {
            var parent = parentRef.Dereference(self.Registry);
            Assert.AreNotEqual(self, parent);
            var transform = self.GetComponent(self.Registry.GetTransformType());
            Assert.IsNotNull(transform);

            // Set new parent
            transform["parent"] = parentRef;

            if (UTinyEntity.Reference.None.Id == parentRef.Id)
            {
                return;
            }

            // Rebind groups
            if (parent.EntityGroup != self.EntityGroup)
            {
                var selfRef = AsReference(self);
                self.EntityGroup.RemoveEntityReference(selfRef); ;
                parent.EntityGroup.AddEntityReference(selfRef);
            }
        }

        public static UTinyEntity.Reference Parent(this UTinyEntity self)
        {
            var transform = self.GetComponent(self.Registry.GetTransformType());
            Assert.IsNotNull(transform);
            return (UTinyEntity.Reference)transform["parent"];
        }

        private static UTinyEntity.Reference AsReference(this UTinyEntity entity)
        {
            if (null == entity)
            {
                return UTinyEntity.Reference.None;
            }
            return (UTinyEntity.Reference)entity;
        }
    }


}
#endif // NET_4_6
