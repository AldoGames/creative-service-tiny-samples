#if NET_4_6
using JetBrains.Annotations;
using UnityEditor;
using UnityEngine;
using Unity.Tiny.Extensions;

namespace Unity.Tiny
{
    [UsedImplicitly]
    public class BoxCollider2DInversedBindings : InversedBindingsBase<BoxCollider2D>
    {
        #region Static
        [InitializeOnLoadMethod]
        private static void Register()
        {
            GameObjectTracker.RegisterForComponentModification<BoxCollider2D>(SyncBoxCollider2D);
        }

        public static void SyncBoxCollider2D(BoxCollider2D from, UTinyEntityView view)
        {
            var registry = view.Registry;
            var collider = view.EntityRef.Dereference(registry).GetComponent(registry.GetRectColliderType());
            if (null != collider)
            {
                SyncBoxCollider2D(from, collider);
            }
        }

        public static void SyncBoxCollider2D(BoxCollider2D box, [NotNull] UTinyObject collider)
        {
            AssignIfDifferent(collider, "width", box.size.x);
            AssignIfDifferent(collider, "height", box.size.y);
            AssignIfDifferent(collider, "pivot", -new Vector2(box.offset.x / box.size.x - 0.5f, box.offset.y / box.size.y - 0.5f));
        }
        #endregion

        #region InversedBindingsBase<BoxCollider2D>
        public override void Create(UTinyEntityView view, BoxCollider2D from)
        {
            var collider = new UTinyObject(Registry, GetMainTinyType());
            SyncBoxCollider2D(from, collider);

            var entity = view.EntityRef.Dereference(Registry);
            var rectCollider = entity.GetOrAddComponent(GetMainTinyType());
            rectCollider.CopyFrom(collider);
            BindingsHelper.RunBindings(entity, rectCollider);
        }

        public override UTinyType.Reference GetMainTinyType()
        {
            return Registry.GetRectColliderType();
        }
        #endregion
    }
}
#endif // NET_4_6
