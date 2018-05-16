#if NET_4_6
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Unity.Tiny
{
    public static class InversedBindingsTracker
    {
        private static IRegistry Registry { get; set; }
        private static UTinyEntityGroupManager EntityGroupManager { get; set; }
        private static UTinyUndo TinyUndo { get; set; }
        
        [InitializeOnLoadMethod]
        private static void Init()
        {
            UTinyEditorApplication.OnLoadProject += OnProjectLoaded;
            UTinyEditorApplication.OnCloseProject += OnProjectClosed;
        }

        private static void OnProjectLoaded(UTinyProject project)
        {
            EditorApplication.hierarchyChanged += HierarchyChanged;
            Registry = UTinyEditorApplication.Registry;
            EntityGroupManager = UTinyEditorApplication.EntityGroupManager;
            TinyUndo = UTinyEditorApplication.Undo;
            TinyUndo.OnRedoPerformed += HierarchyChanged;
        }
        
        private static void OnProjectClosed(UTinyProject project)
        {
            EditorApplication.hierarchyChanged -= HierarchyChanged;
            Registry = null;
            EntityGroupManager = null;
            TinyUndo.OnRedoPerformed -= HierarchyChanged;
            TinyUndo = null;
        }

        private static void HierarchyChanged()
        {
            var changed = false;
            for (var i = 0; i < SceneManager.sceneCount; ++i)
            {
                var scene = SceneManager.GetSceneAt(i);
                if (!scene.isLoaded || !scene.IsValid())
                {
                    continue;
                }

                var graph = UTinyHierarchyWindow.GetSceneGraph(EntityGroupManager?.ActiveEntityGroup ?? UTinyEntityGroup.Reference.None);
                if (null == graph)
                {
                    continue;
                }

                var transforms = scene.GetRootGameObjects()
                    .SelectMany(root => root.GetComponentsInChildren<Transform>(true))
                    .NotNull()
                    // Dealing with prefabs
                    .Where(t =>  (t.root.gameObject.hideFlags & HideFlags.HideInHierarchy) != HideFlags.HideInHierarchy)
                    .GroupBy(t => null == t.GetComponent<UTinyEntityView>());

                foreach (var group in transforms)
                {
                    // Without an entity view
                    // We will try to create an entity from the components of the object.
                    // If we couldn't (or if it is a prefab), we just display a dialog box and delete the GameObjects
                    if (group.Key)
                    {
                        changed = ProcessNewGameObjects(group);
                    }
                    // With an entity view
                    else
                    {
                        foreach (var t in group)
                        {
                            var view = t.GetComponent<UTinyEntityView>();
                            view.DestroyIfUnlinked();
                        }
                    }
                }
            }

            if (changed)
            {
                UTinyHierarchyWindow.InvalidateSceneGraph();
            }
        }

        private static bool ProcessNewGameObjects(IEnumerable<Transform> transforms)
        {
            bool changed = false;
            foreach (var t in transforms)
            {
                var type = PrefabUtility.GetPrefabType(t.gameObject);
                if (type != PrefabType.None)
                {
                    t.gameObject.AddComponent<UTinyEntityView>();
                    if (t.root == t)
                    {
                        EditorUtility.DisplayDialog($"{UTinyConstants.ApplicationName}",
                            $"Dragging prefabs are not currently supported in {UTinyConstants.ApplicationName}", "OK");
                    }
                }
                else
                {
                    var view = t.gameObject.AddComponent<UTinyEntityView>();
                    view.Registry = Registry;

                    var componentList = Pooling.ListPool<Component>.Get();
                    var inversedBindings = Pooling.ListPool<IInversedBindings>.Get();
                    try
                    {
                        t.GetComponents(componentList);
                        // Check if we can actually convert the components
                        // We will actually create an object if and only if we can convert all the components.
                        foreach (var c in componentList)
                        {
                            var cType = c.GetType();
                            var creator = InversedBindingsHelper.GetInversedBindings(cType);
                            if (null == creator)
                            {
                                EditorUtility.DisplayDialog($"{UTinyConstants.ApplicationName}",
                                    $"Component {cType.Name} is not currently supported in {UTinyConstants.ApplicationName}",
                                    "OK");
                                inversedBindings.Clear();
                                break;
                            }

                            inversedBindings.Add(creator);
                        }

                        if (inversedBindings.Count > 0)
                        {
                            for (var index = 0; index < inversedBindings.Count; ++index)
                            {
                                inversedBindings[index].Create(view, componentList[index]);
                            }

                            changed = true;
                        }
                    }
                    finally
                    {
                        Pooling.ListPool<Component>.Release(componentList);
                        Pooling.ListPool<IInversedBindings>.Release(inversedBindings);
                    }
                }
            }

            return changed;
        }
    }
}
#endif // NET_4_6
