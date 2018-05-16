#if NET_4_6
using UnityEngine;

namespace Unity.Tiny
{
    public static class GameObjectExtensions
    {
        public static TComponent AddMissingComponent<TComponent>(this GameObject go) where TComponent: Component
        {
            var component = go.GetComponent<TComponent>();
            if (null == component)
            {
                component = go.AddComponent<TComponent>();
            }
            return component;
        }
    }
}
#endif // NET_4_6
