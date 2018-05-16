#if NET_4_6
using JetBrains.Annotations;

using UnityEditor;
using UnityEngine;

using Unity.Tiny.Extensions;

namespace Unity.Tiny
{
	[UsedImplicitly]
	public class TransformInversedBindings : InversedBindingsBase<Transform>
	{
		#region Static
		[InitializeOnLoadMethod]
		private static void Register()
		{
			GameObjectTracker.RegisterForComponentModification<Transform>(SyncTransform);
		}

		public static void SyncTransform(Transform from, UTinyEntityView view)
		{
			var registry = view.Registry;
			var tinyTransform = view.EntityRef.Dereference(registry).GetComponent(registry.GetTransformType());
			if (null != tinyTransform)
			{
				SyncTransform(from, tinyTransform);
			}
		}
		
		public static void SyncTransform(Transform t, [NotNull] UTinyObject tiny)
		{
			AssignIfDifferent(tiny, "localPosition", t.localPosition);
			AssignIfDifferent(tiny, "localRotation", t.localRotation);
			AssignIfDifferent(tiny, "localScale", t.localScale);
			if (t.parent)
			{
				AssignIfDifferent(tiny, "parent", t.parent.GetComponent<UTinyEntityView>()?.EntityRef ?? UTinyEntity.Reference.None);
			}
			else
			{
				AssignIfDifferent(tiny, "parent", UTinyEntity.Reference.None);
			}
		}
		#endregion

		#region InversedBindingsBase<Transform>
		public sealed override void Create(UTinyEntityView view, Transform t)
		{
			UTinyEntity.Reference parentRef = UTinyEntity.Reference.None;
			if (t.parent)
			{
				parentRef = t.parent.GetComponent<UTinyEntityView>()?.EntityRef ?? UTinyEntity.Reference.None;
			}

			var graph = UTinyHierarchyWindow.GetSceneGraph(
				parentRef.Equals(UTinyEntity.Reference.None) ?
					EntityGroupManager.ActiveEntityGroup
					: (UTinyEntityGroup.Reference)parentRef.Dereference(Registry).EntityGroup);

			if (null == graph)
			{
				return;
			}

			var transform = new UTinyObject(Registry, GetMainTinyType());
			SyncTransform(t, transform);

			var entityNode = graph.CreateFromExisting(t, t.parent);
			var entity = entityNode.Entity.Dereference(Registry);

			var tiny = entity.GetOrAddComponent(GetMainTinyType());
			tiny.CopyFrom(transform);

			BindingsHelper.RunBindings(entity, tiny);
		}

		public sealed override UTinyType.Reference GetMainTinyType()
		{
			return UTinyEditorApplication.Registry.GetTransformType();
		}
		#endregion
	}
}
#endif // NET_4_6
