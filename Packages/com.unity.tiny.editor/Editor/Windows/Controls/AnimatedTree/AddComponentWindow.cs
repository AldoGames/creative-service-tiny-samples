#if NET_4_6
using System.Collections.Generic;

using UnityEngine;
using Unity.Tiny.Filters;

namespace Unity.Tiny
{
    public class AddComponentWindow : UTinyAnimatedTreeWindow<AddComponentWindow, UTinyType>
    {
        private UTinyEntity[] Entities { get; set; }

        public static bool Show(Rect rect, IRegistry registry, UTinyEntity[] entities)
        {
            var window = GetWindow();
            window.Entities = entities;
            return Show(rect, registry, true);
        }

        protected override IEnumerable<UTinyType> GetItems(UTinyModule module)
        {
            return module.Components.Deref(Registry);
        }

        protected override void OnItemClicked(UTinyType type)
        {
            var module = ValueToModules[type];
            foreach (var entity in Entities)
            {
                entity.GetOrAddComponent((UTinyType.Reference)type);
            }

            if (!IsIncluded(module))
            {
                Debug.Log($"{UTinyConstants.ApplicationName}: The '{module.Name}' module was included to the project because the '{type.Name}' component was added to an entity.");
            }
            MainModule.AddExplicitModuleDependency((UTinyModule.Reference)module);
            UTinyHierarchyWindow.InvalidateDataModel();
            UTinyInspector.RepaintAll();
        }

        protected override bool FilterItem(UTinyType type)
        {
            var typeRef = (UTinyType.Reference)type;
            foreach(var entity in Entities)
            {
                if (null == entity.GetComponent(typeRef))
                {
                    return true;
                }
            }
            return false;
        }

        protected override string TreeName()
        {
            return $"{UTinyConstants.ApplicationName} Components";
        }
    }
}
#endif // NET_4_6
