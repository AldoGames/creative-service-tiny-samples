#if NET_4_6
using System.Linq;

using UnityEngine;
using UnityEditor;
using Unity.Properties;
using Unity.Tiny.Attributes;

using static Unity.Tiny.Attributes.EditorInspectorAttributes;

namespace Unity.Tiny
{
    public class UTinyTypeIMGUIVisitor : UTinyInspectorIMGUIVisitor
    {
        public override bool ValidateTarget(IPropertyContainer container)
        {
            var type = (container as UTinyTypeViewer).Type;
            return null != type && null != Registry.FindById<UTinyType>(type.Id);
        }

        protected override void OnPrepare()
        {
            base.OnPrepare();
            var viewers = Targets.Cast<UTinyTypeViewer>();

            // ReSharper disable once PossibleMultipleEnumeration
            foreach (var viewer in viewers)
            {
                viewer.Type?.Refresh();
            }
            var type = viewers.First().TypeRef;
            var project = UTinyEditorApplication.Project;
            var module = project.Module.Dereference(Registry);

            if (!module.Types.Contains(type))
            {
                GUI.enabled = false;
            }
        }

        protected override void OnComplete()
        {
            base.OnComplete();
            if (Changes.Count == 0)
            {
                return;
            }

            var type = Targets.Cast<UTinyTypeViewer>().First().Type;
            var binding = type.GetAttribute<BindingsAttribute>()?.Binding;
            if (null == binding)
            {
                return;
            }
            var typeRef = (UTinyType.Reference)type;

            foreach (var entityGroupRef in UTinyEditorApplication.EntityGroupManager.LoadedEntityGroups)
            {
                var entityGroup = entityGroupRef.Dereference(Registry);
                if (null == entityGroup)
                {
                    continue;
                }

                foreach(var entityRef in entityGroup.Entities)
                {
                    var entity = entityRef.Dereference(Registry);
                    var component = entity?.GetComponent(typeRef);
                    
                    if (component == null)
                    {
                        continue;
                    }
                    
                    component.Refresh(null, true);
                    binding.Run(BindingTiming.OnUpdateBindings, entity, component);
                }
            }
            GUI.enabled = true;
        }

        public override void Header()
        {
            EditorGUILayout.LabelField("(Initial Value)", EditorStyles.boldLabel, GUILayout.Height(25.0f));
        }

        protected override bool AreListReadOnly => true;

        protected override bool AreEntityReferencesReadOnly => true;
    }
}
#endif // NET_4_6
