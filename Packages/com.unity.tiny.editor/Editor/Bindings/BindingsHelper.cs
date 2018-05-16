#if NET_4_6

using Unity.Tiny.Attributes;
using Unity.Tiny.Filters;

using static Unity.Tiny.Attributes.EditorInspectorAttributes;

namespace Unity.Tiny
{
    public static class BindingsHelper
    {
        public static void RunBindings(UTinyEntity entity, UTinyObject component)
        {
            if (!ComponentBinding.ValidateBindingsParams(entity, component))
            {
                return;
            }

            var type = component.Type.Dereference(entity.Registry);
            if (!type.HasAttribute<BindingsAttribute>())
            {
                return;
            }

            var bindings = type.GetAttribute<BindingsAttribute>().Binding;
            bindings.Run(BindingTiming.OnAddBindings, entity, component);
            bindings.Run(BindingTiming.OnUpdateBindings, entity, component);
        }

        public static void RunAllBindings(UTinyEntity entity)
        {
            if (!ComponentBinding.ValidateBindingsParams(entity))
            {
                return;
            }

            foreach (var component in entity.Components)
            {
                RunBindings(entity, component);
            }
        }

        internal static void RemoveAllBindingsOnLoadedEntities()
        {
            CallBindingsOnLoadedEntities(BindingTiming.OnRemoveBindings);
        }

        internal static void RunAllBindingsOnLoadedEntities()
        {
            CallBindingsOnLoadedEntities(BindingTiming.OnAddBindings, BindingTiming.OnUpdateBindings);
        }

        private static void CallBindingsOnLoadedEntities(params BindingTiming[] timings)
        {
            var sceneManager = UTinyEditorApplication.EntityGroupManager;
            var registry = UTinyEditorApplication.Registry;
            if (null == sceneManager || null == registry)
            {
                return;
            }

            foreach (var entity in sceneManager.LoadedEntityGroups.Deref(registry).Entities())
            {
                foreach (var component in entity.Components)
                {
                    var type = component.Type.Dereference(registry);
                    var bindings = type.GetAttribute<BindingsAttribute>()?.Binding;
                    if (null == bindings)
                    {
                        continue;
                    }

                    foreach (var timing in timings)
                    {
                        bindings.Run(timing, entity, component);
                    }
                }
            }
        }
    }
}
#endif // NET_4_6