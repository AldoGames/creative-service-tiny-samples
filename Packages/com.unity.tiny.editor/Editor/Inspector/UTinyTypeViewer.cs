#if NET_4_6
using System.Collections.Generic;
using System.Linq;

using UnityEditor;
using UnityEngine;

using Unity.Properties;

namespace Unity.Tiny
{
    public class UTinyTypeViewer : ScriptableObject, IPropertyContainer
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
                                         .Where(obj => obj is UTinyTypeViewer)
                                         .Cast<UTinyTypeViewer>()
                                         .ToList();
                var toDestroy = Pooling.ListPool<UTinyTypeViewer>.Get();
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
                    Pooling.ListPool<UTinyTypeViewer>.Release(toDestroy);
                }
            };
        }

        private static List<UTinyTypeViewer> Instances
        {
            get;
        } = new List<UTinyTypeViewer>();

        private static UTinyTypeViewer GetInstance()
        {
            var viewer = CreateInstance<UTinyTypeViewer>();
            Instances.Add(viewer);
            return viewer;
        }

        private static readonly Property<UTinyTypeViewer, UTinyType.Reference> s_TypeRefProperty = new Property<UTinyTypeViewer, UTinyType.Reference>("TypeRef",
            /* GET */ c => c.m_TypeRef,
            /* SET */ (c, v) => c.m_TypeRef = v
        );

        private static readonly Property<UTinyTypeViewer, UTinyType> s_TypeProperty = new Property<UTinyTypeViewer, UTinyType>("Type",
            /* GET */ c =>  null != c.Registry? c.m_TypeRef.Dereference(c.Registry) : null,
            /* SET */ null 
        );

        private IRegistry Registry { get; set; }

        private static readonly PropertyBag s_Bag = new PropertyBag(s_TypeProperty);
        private UTinyType.Reference m_TypeRef;

        public UTinyType Type => s_TypeProperty.GetValue(this);

        public UTinyType.Reference TypeRef
        {
            get { return s_TypeRefProperty.GetValue(this); }
            private set { s_TypeRefProperty.SetValue(this, value); }
        }

        public IPropertyBag PropertyBag => s_Bag;

        public IVersionStorage VersionStorage => DefaultVersionStorage.Instance;

        public static void SetType(UTinyType type, bool additive = false)
        {
            var instance = GetInstance();
            instance.Registry = type.Registry; 
            instance.TypeRef = UTinyType.Reference.None;
            instance.TypeRef = (UTinyType.Reference)type;

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
