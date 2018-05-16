#if NET_4_6

using UnityEngine;
using UnityEditor;

using static Unity.Tiny.UTinyEditorApplication;

namespace Unity.Tiny
{
    public static class HierarchyContextMenus
    {
        public static void ShowEntityGroupContextMenu(this HierarchyTree tree, UTinyEntityGroup.Reference entityGroupRef)
        {
            if (UTinyEntityGroup.Reference.None.Id == entityGroupRef.Id)
            {
                entityGroupRef = EntityGroupManager.ActiveEntityGroup;
            }

            var menu = new GenericMenu();
            if (IsEntityGroupActive(entityGroupRef))
            {
                menu.AddDisabledItem(new GUIContent("Set Active EntityGroup"));
            }
            else
            {
                menu.AddItem(new GUIContent("Set Active EntityGroup"), false, () =>
                {
                    SetEntityGroupActive(entityGroupRef);
                });
            }

            if (EntityGroupManager.LoadedEntityGroups.IndexOf(entityGroupRef) > 0)
            {
                menu.AddItem(new GUIContent("Move EntityGroup Up"), false, () =>
                {
                    EntityGroupManager.MoveUp(entityGroupRef);
                });
            }
            else
            {
                menu.AddDisabledItem(new GUIContent("Move EntityGroup Up"));
            }

            if (EntityGroupManager.LoadedEntityGroups.IndexOf(entityGroupRef) < EntityGroupManager.LoadedEntityGroupCount - 1)
            {
                menu.AddItem(new GUIContent("Move EntityGroup Down"), false, () =>
                {
                    EntityGroupManager.MoveDown(entityGroupRef);
                });
            }
            else
            {
                menu.AddDisabledItem(new GUIContent("Move EntityGroup Down"));
            }

            menu.AddSeparator("");

            if (EntityGroupManager.LoadedEntityGroupCount == 1)
            {
                menu.AddDisabledItem(new GUIContent("Unload EntityGroup"));
                menu.AddDisabledItem(new GUIContent("Unload Other EntityGroups"));
            }
            else
            {
                menu.AddItem(new GUIContent("Unload EntityGroup"), false, () =>
                {
                    EntityGroupManager.UnloadEntityGroup(entityGroupRef);
                });
                menu.AddItem(new GUIContent("Unload Other EntityGroups"), false, () =>
                {
                    EntityGroupManager.UnloadAllEntityGroupsExcept(entityGroupRef);
                });
            }

            menu.AddItem(new GUIContent("New EntityGroup"), false, () =>
            {
                var context = EditorContext;
                if (null == context)
                {
                    return;
                }
                var registry = context.Registry;
                var project = context.Project;
                CreateNewEntityGroup(project.Module.Dereference(registry));
            });

            menu.AddSeparator("");

            menu.AddItem(new GUIContent("Create Entity"), false, () =>
            {
                tree.CreateEntity(entityGroupRef);
            });

            menu.AddItem(new GUIContent("Create Static Entity"), false, () =>
            {
                tree.CreateStaticEntity(entityGroupRef);
            });

            menu.ShowAsContext();
        }

        public static void ShowEntityContextMenu(this HierarchyTree tree, UTinyEntity.Reference entityRef)
        {
            if (UTinyEntity.Reference.None.Id == entityRef.Id)
            {
                return;
            }

            var menu = new GenericMenu();

            menu.AddItem(new GUIContent("Create Entity"), false, () =>
            {
                tree.CreateEntityFromSelection();
            });

            menu.AddItem(new GUIContent("Rename"), false, () =>
            {
                tree.Rename(entityRef);
            });

            menu.AddItem(new GUIContent("Duplicate"), false, () =>
            {
                tree.DuplicateSelection();
            });

            menu.AddItem(new GUIContent("Delete"), false, () =>
            {
                tree.DeleteSelection();
            });

            menu.AddSeparator("");

            menu.AddItem(new GUIContent("New EntityGroup"), false, () =>
            {
                var context = EditorContext;
                if (null == context)
                {
                    return;
                }
                var registry = context.Registry;
                var project = context.Project;
                CreateNewEntityGroup(project.Module.Dereference(registry));
            });

            menu.ShowAsContext();
        }

        #region Implementation
        private static bool IsEntityGroupActive(UTinyEntityGroup.Reference entityGroupRef)
        {
            return EntityGroupManager.ActiveEntityGroup.Equals(entityGroupRef);
        }

        private static void SetEntityGroupActive(UTinyEntityGroup.Reference entityGroupRef)
        {
            EntityGroupManager.SetActiveEntityGroup(entityGroupRef, true);
        }

        private static void CreateNewEntityGroup(UTinyModule module)
        {
            EntityGroupManager.CreateNewEntityGroup();
        }
        #endregion
    }
}
#endif // NET_4_6
