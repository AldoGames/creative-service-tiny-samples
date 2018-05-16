#if NET_4_6
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using UnityEditor;

using Component = UnityEngine.Component;

namespace Unity.Tiny
{
	internal static class GameObjectTracker
	{
		private struct ComponentViewPair
		{
			public Component Component { get; set; }
			public UTinyEntityView View { get; set; }
		}

		private static List<Action<List<ComponentViewPair>>> InversedBindingsMethods { get; } = new List<Action<List<ComponentViewPair>>>();
		private static HashSet<UTinyEntityView> ActiveViews { get; } = new HashSet<UTinyEntityView>();
		private static HashSet<UTinyEntityGroup.Reference> UnloadingEntityGroups { get; } = new HashSet<UTinyEntityGroup.Reference>();
		private static Action<UTinyTrackerRegistration, UTinyEventHandler<UTinyTrackerRegistration, UTinyEntityView>> AddListener { get; }
			= UTinyEventDispatcher<UTinyTrackerRegistration>.AddListener;

		private static IRegistry Registry => UTinyEditorApplication.Registry;
		private static UTinyEntityGroupManager EntityGroupManager => UTinyEditorApplication.EntityGroupManager;

		[InitializeOnLoadMethod]
		private static void Init()
		{
			AddListener(UTinyTrackerRegistration.Register,   HandleRegistration);
			AddListener(UTinyTrackerRegistration.Unregister, HandleRegistration);
			UTinyEditorApplication.OnLoadProject += HandleProjectLoaded;
			UTinyEditorApplication.OnCloseProject += HandleProjectUnloaded;
		}

		public static void RegisterForComponentModification<TComponent>(Action<TComponent, UTinyEntityView> inversedBindingsMethod)
			where TComponent : Component
		{
			InversedBindingsMethods.Add(pairs =>
			{
				foreach (var pair in pairs.Where(p => p.Component is TComponent))
				{
					inversedBindingsMethod((TComponent)pair.Component, pair.View);
				}
			});
		}

		private static void HandleRegistration(UTinyTrackerRegistration trackerRegistration, UTinyEntityView view)
		{
			switch (trackerRegistration)
			{
				case UTinyTrackerRegistration.Register:  
					Register(view);
					break;
				case UTinyTrackerRegistration.Unregister:
					Unregister(view);
					break;
				default:
					throw new InvalidEnumArgumentException();
			}
		}
		
		private static void Register(UTinyEntityView view)
		{
			if (ActiveViews.Add(view) && ActiveViews.Count == 1)
			{
				Undo.postprocessModifications += HandlePostProcessModification;
			}
		}

		private static void Unregister(UTinyEntityView view)
		{
			if (!ActiveViews.Remove(view))
			{
				return;
			}

			if (ActiveViews.Count == 0)
			{
				Undo.postprocessModifications -= HandlePostProcessModification;
			}

			// From this point, we know that the entity view is being destroyed. What we do not know is if the view is
			// being destroyed because we are unloading the scene or if the user deleted the entity through the hierarchy
			// or the scene view.
			var entity = view.EntityRef.Dereference(Registry);
			if (entity?.EntityGroup == null)
			{
				return;
			}

			var entityGroupRef = (UTinyEntityGroup.Reference) entity.EntityGroup;

			if (UnloadingEntityGroups.Contains(entityGroupRef))
			{
				return;
			}

			entity.View = null;

			var graph = EntityGroupManager.GetSceneGraph(entityGroupRef);
			if (null == graph)
			{
				return;
			}

			graph.Delete(graph.FindNode(view.EntityRef));
			UTinyHierarchyWindow.InvalidateSceneGraph();
		}

		private static void HandleProjectLoaded(UTinyProject project)
		{
			EntityGroupManager.OnWillUnloadEntityGroup += HandleEntityGroupWillUnload;
			EntityGroupManager.OnEntityGroupUnloaded += HandleEntityGroupUnloaded;
		}
		
		private static void HandleProjectUnloaded(UTinyProject project)
		{
			EntityGroupManager.OnWillUnloadEntityGroup -= HandleEntityGroupWillUnload;
			EntityGroupManager.OnEntityGroupUnloaded -= HandleEntityGroupUnloaded;
		}

		private static void HandleEntityGroupWillUnload(UTinyEntityGroup.Reference entityGroupRef)
		{
			UnloadingEntityGroups.Add(entityGroupRef);
		}

		private static void HandleEntityGroupUnloaded(UTinyEntityGroup.Reference entityGroupRef)
		{
			UnloadingEntityGroups.Remove(entityGroupRef);
		}
		
		private static UndoPropertyModification[] HandlePostProcessModification(UndoPropertyModification[] mods)
		{
			var pairs = Pooling.ListPool<ComponentViewPair>.Get();

			try
			{
				// Gather all the modifications that occured on a Component which also have an Entity View. 
				foreach (var component in mods.Select(m => m.currentValue?.target).NotNull().OfType<Component>().Distinct())
				{
					var view = component.GetComponent<UTinyEntityView>();
					if (null == view)
					{
						continue;
					}

					pairs.Add(new ComponentViewPair {Component = component, View = view});
				}

				foreach (var method in InversedBindingsMethods)
				{
					method(pairs);
				}
			}
			finally
			{
				Pooling.ListPool<ComponentViewPair>.Release(pairs);
			}

			return mods;
		}
	}
}
#endif // NET_4_6
