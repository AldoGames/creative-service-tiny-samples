using System;
using Unity.Tiny.Attributes;
using UnityEditor;
using static Unity.Tiny.Attributes.EditorInspectorAttributes;

namespace Unity.Tiny.Samples.Match3
{
    public static class BindingRegistration
    {
        [InitializeOnLoadMethod]
        private static void Register()
        {
            UTinyEventDispatcher.AddListener<UTinyRegistryEventType, IRegistryObject>(UTinyRegistryEventType.Registered, HandleGraphTypeRegistered);
        }
        
        private static void HandleGraphTypeRegistered(UTinyRegistryEventType eventType, IRegistryObject obj)
        {
            if (!(obj is UTinyType) || null == obj.Registry)
            {
                return;
            }

            var type = (UTinyType) obj;
            var registry = obj.Registry;

            var cellGraphType = obj.Registry.FindByName<UTinyType>("CellGraph");
            if (null != cellGraphType && type.Id.Equals(cellGraphType.Id))
            {
                AddBindings(registry, (UTinyType.Reference) type, t => new CellGraphBindings(t));
                return;
            }

            var cellGraphNodeType = obj.Registry.FindByName<UTinyType>("CellGraphNode");
            if (null != cellGraphNodeType && type.Id.Equals(cellGraphNodeType.Id))
            {
                AddBindings(registry, (UTinyType.Reference)type, t => new CellGraphNodeBindings(t));
            }
        }
        
        private static void AddBindings<TBinding>(IRegistry registry, UTinyType.Reference type, Func<UTinyType.Reference, TBinding> del)
            where TBinding : IComponentBinding
        {
            type.Dereference(registry)?.AddAttribute(Bindings(del(type)));
        }
    }
}

