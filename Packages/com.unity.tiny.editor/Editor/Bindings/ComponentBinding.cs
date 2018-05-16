#if NET_4_6
using System;
using System.Collections.Generic;

using UnityEditor;
using UnityEngine;

using Unity.Tiny.Filters;

namespace Unity.Tiny
{
    using Object = UnityEngine.Object;

    public abstract class ComponentBinding : IComponentBinding
    {
        #region Static
        private static readonly Dictionary<UTinyType.Reference, IList<ComponentBinding>> s_Listeners = new Dictionary<UTinyType.Reference, IList<ComponentBinding>>();

        public static bool ValidateBindingsParams(UTinyEntity entity, UTinyObject component)
        {
            return ValidateBindingsParams(entity) &&
                   ValidateBindingsParams(component);
        }

        public static bool ValidateBindingsParams(UTinyEntity entity)
        {
            return null != entity      &&
                   null != entity.View &&
                           entity.View;
        }

        public static bool ValidateBindingsParams(UTinyObject component)
        {
            return null != component;
        }

        [InitializeOnLoadMethod]
        private static void Init()
        {
            UTinyEventDispatcher.AddListener<UTinyRegistryEventType, IRegistryObject>(UTinyRegistryEventType.Registered, HandleCoreTypeRegistered);
        }

        private static void HandleCoreTypeRegistered(UTinyRegistryEventType @event, IRegistryObject obj)
        {
            if (!(obj is UTinyType) || null == obj?.Registry)
            {
                return;
            }
            var type = (UTinyType) obj;
            UTinyEventDispatcher.AddListener<UTinyType.Reference, IEnumerable<UTinyEntity>>((UTinyType.Reference)type, ProcessDependency);
        }

        private static void ProcessDependency(UTinyType.Reference typeRef, IEnumerable<UTinyEntity> entities)
        {
            IList<ComponentBinding> bindings;
            if (!s_Listeners.TryGetValue(typeRef, out bindings))
            {
                return;
            }

            foreach(var binding in bindings)
            {
                Run(binding, BindingTiming.OnUpdateBindings, binding.TypeRef);
            }
        }

        private static void Run(IComponentBinding binding, BindingTiming timing, UTinyType.Reference type)
        {
            var registry = UTinyEditorApplication.Registry;
            foreach(var entity in UTinyEditorApplication.EntityGroupManager.LoadedEntityGroups.Deref(registry).Entities())
            {
                var component = entity.GetComponent(type);
                if (null == component)
                {
                    continue;
                }
                binding.Run(timing, entity, component);
            }
        }
        #endregion

        #region Fields
        private readonly List<Type> m_RequiredComponentTypes = new List<Type>();
        #endregion

        #region Properties
        public UTinyType.Reference TypeRef { get; }
        #endregion

        #region API
        protected ComponentBinding(UTinyType.Reference typeRef)
        {
            TypeRef = typeRef;
        }

        public void Run(BindingTiming timing, UTinyEntity entity, UTinyObject component)
        {
            if (!ValidateBindingsParams(entity, component) ||
                !MatchesRequiredComponentTypes(entity))
            {
                return;
            }

            component.Refresh();

            switch (timing)
            {
                case BindingTiming.OnAddBindings:
                    OnAddBinding(entity, component);
                    break;
                case BindingTiming.OnUpdateBindings:
                    OnUpdateBinding(entity, component);
                    break;
                case BindingTiming.OnRemoveBindings:
                    OnRemoveBinding(entity, component);
                    break;
                case BindingTiming.OnAddComponent:
                    OnAddComponent(entity, component);
                    break;
                case BindingTiming.OnRemoveComponent:
                    OnRemoveComponent(entity, component);
                    break;
                default:
                    break;
            }
        }

        protected void RequireComponentType<TComponent>()
            where TComponent : Component
        {
            var type = typeof(TComponent);
            if (m_RequiredComponentTypes.Contains(type))
            {
                return;
            }
            m_RequiredComponentTypes.Add(type);
        }

        protected void RegisterForEvent(UTinyType.Reference typeref)
        {
            IList<ComponentBinding> bindings;
            if (!s_Listeners.TryGetValue(typeref, out bindings))
            {
                bindings = s_Listeners[typeref] = new List<ComponentBinding>();
            }
            if (!bindings.Contains(this))
            {
                bindings.Add(this);
            }
        }

        protected TComponent GetComponent<TComponent>(UTinyEntity entity)
            where TComponent : Component
        {
            return !ValidateBindingsParams(entity) ? null : entity.View.GetComponent<TComponent>();
        }

        protected TComponent AddComponent<TComponent>(UTinyEntity entity)
            where TComponent : Component
        {
            return AddComponent<TComponent>(entity, null);
        }

        protected TComponent AddComponent<TComponent>(UTinyEntity entity, Action<TComponent> init)
            where TComponent : Component
        {
            if (!ValidateBindingsParams(entity))
            {
                return null;
            }

            var component = entity.View.gameObject.AddComponent<TComponent>();
            if (null != component)
            {
                init?.Invoke(component);
            }
            return component;
        }

        protected TComponent AddMissingComponent<TComponent>(UTinyEntity entity)
            where TComponent : Component
        {
            return AddMissingComponent<TComponent>(entity, null);
        }

        protected TComponent AddMissingComponent<TComponent>(UTinyEntity entity, Action<TComponent> init)
            where TComponent : Component
        {
            var component = GetComponent<TComponent>(entity);
            if (null == component)
            {
                component = AddComponent(entity, init);
            }
            return component;
        }

        protected void RemoveComponent<TComponent>(UTinyEntity entity)
            where TComponent : Component
        {
            var component = GetComponent<TComponent>(entity);
            if (null != component)
            {
                Object.DestroyImmediate(component, false);
            }
        }

        protected virtual void OnAddBinding   (UTinyEntity entity, UTinyObject component) { }
        protected virtual void OnUpdateBinding(UTinyEntity entity, UTinyObject component) { }
        protected virtual void OnRemoveBinding(UTinyEntity entity, UTinyObject component) { }
        protected virtual void OnAddComponent   (UTinyEntity entity, UTinyObject component) { }
        protected virtual void OnRemoveComponent(UTinyEntity entity, UTinyObject component) { }
        #endregion

        #region Implementation
        private bool MatchesRequiredComponentTypes(UTinyEntity entity)
        {
            foreach(var type in m_RequiredComponentTypes)
            {
                if (!Has(entity, type))
                {
                    return false;
                }
            }
            return true;
        }

        private bool Has(UTinyEntity entity, Type type)
        {
            var component = entity.View.GetComponent(type);
            return null != component && component;
        }
        #endregion
    }
}
#endif // NET_4_6
