#if NET_4_6
using System;
using System.Collections.Generic;
using System.Linq;

using UnityEngine;
using Unity.Tiny.Filters;

namespace Unity.Tiny
{
    public class AddSystemWindow : UTinyAnimatedTreeWindow<AddSystemWindow, UTinySystem>
    {
        private IEnumerable<UTinySystem> m_AllowedSystems;
        private Action<UTinySystem.Reference> m_OnSystemClicked;

        public static bool Show(Rect rect, IRegistry registry, IEnumerable<UTinySystem> systems, Action<UTinySystem.Reference> onSystemClicked)
        {
            var window = GetWindow();
            window.m_OnSystemClicked = onSystemClicked;
            window.m_AllowedSystems = systems;
            return Show(rect, registry);
        }

        protected override IEnumerable<UTinySystem> GetItems(UTinyModule module)
        {
            return module.Systems.Deref(Registry);
        }

        protected override void OnItemClicked(UTinySystem system)
        {
            m_OnSystemClicked?.Invoke((UTinySystem.Reference)system);
        }

        protected override bool FilterItem(UTinySystem system)
        {
            return m_AllowedSystems.Contains(system);
        }

        protected override string TreeName()
        {
            return $"{UTinyConstants.ApplicationName} Systems";
        }
    }
}
#endif // NET_4_6
