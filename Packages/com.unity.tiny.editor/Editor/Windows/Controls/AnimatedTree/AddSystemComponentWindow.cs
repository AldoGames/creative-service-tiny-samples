#if NET_4_6
using System;
using System.Collections.Generic;

using UnityEngine;

using Unity.Tiny.Filters;

namespace Unity.Tiny
{
    public class AddSystemComponentWindow : UTinyAnimatedTreeWindow<AddSystemComponentWindow, UTinyType>
    {
        private List<UTinyType> m_AllowedTypes;
        private Action<UTinyType.Reference> m_OnTypeClicked;

        public static bool Show(Rect rect, IRegistry registry, List<UTinyType> allowedTypes, Action<UTinyType.Reference> onTypeClicked)
        {
            var window = GetWindow();
            window.m_AllowedTypes = allowedTypes;
            window.m_OnTypeClicked = onTypeClicked;
            return Show(rect, registry);
        }

        protected override IEnumerable<UTinyType> GetItems(UTinyModule module)
        {
            return module.Components.Deref(Registry);
        }

        protected override void OnItemClicked(UTinyType type)
        {
            m_OnTypeClicked?.Invoke((UTinyType.Reference)type);
        }

        protected override bool FilterItem(UTinyType type)
        {
            return m_AllowedTypes.Contains(type);
        }

        protected override string TreeName()
        {
            return $"{UTinyConstants.ApplicationName} Components";
        }
    }
}
#endif // NET_4_6
