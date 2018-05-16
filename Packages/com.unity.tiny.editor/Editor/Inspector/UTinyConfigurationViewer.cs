#if NET_4_6
using System.Collections.Generic;
using System.Linq;
using Unity.Properties;
using Unity.Tiny.Pooling;
using UnityEditor;
using UnityEngine;

namespace Unity.Tiny
{
    public class UTinyConfigurationViewer : ScriptableObject, IPropertyContainer
    {
        [InitializeOnLoadMethod]
        private static void Register()
        {
            Selection.selectionChanged += HandleSelectionChanged;
        }

        private static void HandleSelectionChanged()
        {
            EditorApplication.delayCall += () =>
            {
                // Try to release instances of the viewer.
                var selection = Selection.instanceIDs
                    .Select(EditorUtility.InstanceIDToObject)
                    .Where(obj => obj is UTinyConfigurationViewer)
                    .Cast<UTinyConfigurationViewer>()
                    .ToList();
                var toDestroy = ListPool<UTinyConfigurationViewer>.Get();
                try
                {
                    foreach (var instance in Instances)
                    {
                        if (selection.Contains(instance) || UTinyInspector.IsBeingInspected(instance))
                        {
                            continue;
                        }

                        toDestroy.Add(instance);
                    }

                    foreach (var viewer in toDestroy)
                    {
                        Instances.Remove(viewer);
                        DestroyImmediate(viewer, false);
                    }
                }
                finally
                {
                    ListPool<UTinyConfigurationViewer>.Release(toDestroy);
                }
            };
        }

        private static List<UTinyConfigurationViewer> Instances { get; } = new List<UTinyConfigurationViewer>();

        private static UTinyConfigurationViewer GetInstance()
        {
            var viewer = CreateInstance<UTinyConfigurationViewer>();
            Instances.Add(viewer);
            return viewer;
        }

        private static readonly Property<UTinyConfigurationViewer, UTinyEntity.Reference> s_EntityRefProperty = new Property<UTinyConfigurationViewer, UTinyEntity.Reference>("EntityRef",
            /* GET */ c => c.m_EntityRef,
            /* SET */ (c, v) => c.m_EntityRef = v
        );

        private static readonly Property<UTinyConfigurationViewer, UTinyEntity> s_EntityProperty = new Property<UTinyConfigurationViewer, UTinyEntity>("Entity",
            /* GET */ c => null != c.Registry ? c.m_EntityRef.Dereference(c.Registry) : null,
            /* SET */ null
        );

        private IRegistry Registry { get; set; }

        private static readonly PropertyBag s_Bag = new PropertyBag(s_EntityProperty);
        private UTinyEntity.Reference m_EntityRef;

        public UTinyEntity Entity => s_EntityProperty.GetValue(this);

        public UTinyEntity.Reference EntityRef
        {
            get { return s_EntityRefProperty.GetValue(this); }
            private set { s_EntityRefProperty.SetValue(this, value); }
        }

        public IPropertyBag PropertyBag => s_Bag;
        public IVersionStorage VersionStorage => DefaultVersionStorage.Instance;

        public static void SetEntity(UTinyEntity entity, bool additive = false)
        {
            var instance = GetInstance();
            instance.Registry = entity.Registry;
            instance.EntityRef = (UTinyEntity.Reference) entity;

            if (!additive)
            {
                Selection.activeInstanceID = instance.GetInstanceID();
            }
            else
            {
                if (!Selection.instanceIDs.Contains(instance.GetInstanceID()))
                {
                    var selection = Selection.instanceIDs.ToList();
                    selection.Add(instance.GetInstanceID());
                    Selection.instanceIDs = selection.ToArray();
                }
            }
        }
    }
}
#endif // NET_4_6
