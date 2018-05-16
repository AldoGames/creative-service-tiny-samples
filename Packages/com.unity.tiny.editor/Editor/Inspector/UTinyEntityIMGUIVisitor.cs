#if NET_4_6
using System.Linq;

using UnityEditor;
using UnityEngine;
using Unity.Properties;
using Unity.Tiny.Attributes;

using static Unity.Tiny.Attributes.EditorInspectorAttributes;

namespace Unity.Tiny
{
    public class UTinyEntityIMGUIVisitor : UTinyInspectorIMGUIVisitor
    {
        #region GenericIMGUIVisitor
        public override bool ValidateTarget(IPropertyContainer container)
        {
            var entity = container as UTinyEntity;
            return null != entity && null != entity.View && entity.View;
        }

        protected override void OnPrepare()
        {
            base.OnPrepare();
            var registry = Registry;
            Targets = Targets
                .Cast<UTinyEntity>()
                .Select(t => registry.FindById<UTinyEntity>(t.Id))
                .Where(t => null != t)
                .Cast<IPropertyContainer>().ToList();

            foreach (var component in Targets.Cast<UTinyEntity>().SelectMany(e => e.Components))
            {
                component.Refresh();
            }
        }

        protected override void OnComplete()
        {
            foreach (var type in s_Bindings)
            {
                var typeRef = (UTinyType.Reference)type;

                if (type.HasAttribute<BindingsAttribute>())
                {
                    foreach (var entity in Targets.OfType<UTinyEntity>())
                    {
                        var component = entity.GetComponent(typeRef);
                        if (null == component)
                        {
                            continue;
                        }
                        type.GetAttribute<BindingsAttribute>().Binding.Run(BindingTiming.OnUpdateBindings, entity, component);
                    }
                }

                UTinyEventDispatcher.Dispatch(typeRef, Targets.OfType<UTinyEntity>());
            }
        }

        public override void Header()
        {
            var entities = Targets.Cast<UTinyEntity>();

            var firstEntity = entities.FirstOrDefault();
            if (null == firstEntity)
            {
                return;
            }

            UTinyGUI.BackgroundColor(new Rect(0, 0, Screen.width, 15 + 2 * UTinyGUIUtility.SingleLineAndSpaceHeight), UTinyColors.Inspector.HeaderBackground);
            GUILayout.Space(10);
            var name = firstEntity.Name;

            name = entities.All(entity => entity.Name == name) ? name : "-";
            var enabled = firstEntity.Enabled;
            var sameEnabled = entities.All(tiny => tiny.Enabled == enabled);
            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUI.BeginChangeCheck();
                var mixed = EditorGUI.showMixedValue;
                EditorGUI.showMixedValue = !sameEnabled;
                enabled = EditorGUILayout.ToggleLeft(GUIContent.none, enabled, GUILayout.Width(EditorGUIUtility.singleLineHeight));
                EditorGUI.showMixedValue = mixed;
                if (EditorGUI.EndChangeCheck())
                {
                    foreach (var entity in entities)
                    {
                        entity.Enabled = enabled;
                        entity.View.gameObject.SetActive(enabled);
                    }
                    UTinyHierarchyWindow.InvalidateDataModel();
                    UTinyEditorUtility.RepaintAllWindows();
                }
                EditorGUI.BeginChangeCheck();
                name = EditorGUILayout.DelayedTextField(name, UTinyStyles.ComponentHeaderStyle);
                if (EditorGUI.EndChangeCheck())
                {
                    foreach (var entity in entities)
                    {
                        entity.Name = name;
                    }
                    UTinyHierarchyWindow.InvalidateDataModel();
                    UTinyEditorUtility.RepaintAllWindows();
                }
                GUILayout.Space(EditorGUIUtility.singleLineHeight);
            }

            var layer = firstEntity.Layer;
            var sameLayer = entities.All(tiny => tiny.Layer == layer);
            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.Space(50);
                EditorGUILayout.LabelField("Layer", GUILayout.Width(50));
                EditorGUI.BeginChangeCheck();
                var mixed = EditorGUI.showMixedValue;
                EditorGUI.showMixedValue = !sameLayer;
                layer = EditorGUILayout.LayerField(layer);
                if (EditorGUI.EndChangeCheck())
                {
                    foreach (var entity in entities)
                    {
                        entity.Layer = layer;
                        entity.View.gameObject.layer = layer;
                    }
                    UTinyHierarchyWindow.InvalidateDataModel();
                    UTinyEditorUtility.RepaintAllWindows();
                }
                GUILayout.Space(EditorGUIUtility.singleLineHeight);
                EditorGUI.showMixedValue = mixed;
            }
            GUILayout.Space(5);
            UTinyGUILayout.Separator(UTinyColors.Inspector.Separator, UTinyGUIUtility.ComponentHeaderSeperatorHeight);
        }

        public override void Visit()
        {
            base.Visit();
            GUILayout.Space(5);
            EditorGUILayout.BeginHorizontal();
            GUILayout.FlexibleSpace();

            var content = new GUIContent("Add Tiny Component");

            Rect rect = GUILayoutUtility.GetRect(content, UTinyStyles.AddComponentStyle);
            if (EditorGUI.DropdownButton(rect, content, FocusType.Passive, UTinyStyles.AddComponentStyle))
            {
                var targets = Targets.Cast<UTinyEntity>();
                AddComponentWindow.Show(rect, targets.First().Registry, Targets.Cast<UTinyEntity>().ToArray());
            }
            GUILayout.FlexibleSpace();
            EditorGUILayout.EndHorizontal();
        }
        #endregion

        #region UTinyInspectorIMGUIVisitor

        protected override bool ValidateObject(UTinyObject tiny, UTinyType type)
        {
            if (type.TypeCode == UTinyTypeCode.Component)
            {
                return Targets.Cast<UTinyEntity>().All(e => e.Components.Any(c => c.Type.Id == tiny.Type.Id));
            }
            return base.ValidateObject(tiny, type);
        }

        #endregion
    }
}
#endif // NET_4_6
