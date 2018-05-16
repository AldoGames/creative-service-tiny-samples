#if NET_4_6
using System.Linq;

using UnityEngine;

using Unity.Tiny.Extensions;
using Unity.Tiny.Conversions;
using Unity.Tiny.Filters;

namespace Unity.Tiny
{
    public class TransformBindings : ComponentBinding
    {
        public TransformBindings(UTinyType.Reference typeRef)
            : base(typeRef)
        {
        }

        protected override void OnRemoveComponent(UTinyEntity entity, UTinyObject component)
        {
            // We need to set the parent of the associated Unity Object to null.
            // And potentially move them outside of view.

            entity.SetParent(UTinyEntity.Reference.None);
            var children = entity.EntityGroup.Entities
                .Deref(entity.Registry)
                .Where(e =>
                {
                    var t = e.GetComponent(entity.Registry.GetTransformType());
                    return null != t && (t.GetProperty<UTinyEntity.Reference>("parent")).Equals((UTinyEntity.Reference)entity);
                });

            foreach (var child in children)
            {
                child.SetParent(UTinyEntity.Reference.None);
            }
        }

        protected override void OnUpdateBinding(UTinyEntity entity, UTinyObject component)
        {
            var transform = entity.View.transform;
            transform.localPosition = component.GetProperty<Vector3>("localPosition");
            transform.localRotation = component.GetProperty<Quaternion>("localRotation");
            transform.localScale = component.GetProperty<Vector3>("localScale");
        }
    }
}
#endif // NET_4_6
