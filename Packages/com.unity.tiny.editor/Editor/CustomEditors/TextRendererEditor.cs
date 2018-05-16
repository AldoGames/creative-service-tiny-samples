#if NET_4_6

using UnityEditor;
using UnityEngine;

using Unity.Properties;

namespace Unity.Tiny
{
    public class TextRendererEditor : ComponentEditor
    {
        public override bool VisitComponent(UTinyObject tinyObject)
        {
            DrawText(tinyObject);
            DoField<int>(tinyObject, "fontSize");
            DoField<bool>(tinyObject, "bold");
            DoField<bool>(tinyObject, "italic");
            DrawEnum(tinyObject, "anchor");
            DoField<UTinyObject>(tinyObject, "color");
            DoField<Font>(tinyObject, "font");
            return true;
        }
        
        private void DoField<T>(UTinyObject tinyObject, string fieldName)
        {
            var container = tinyObject.Properties;
            var prop = container.PropertyBag.FindProperty(fieldName) as IProperty<UTinyObject.PropertiesContainer, T>;
            var context = new VisitContext<T> {Index = -1, Property = prop, Value = prop.GetValue(container)};
            if (!ExcludeVisit(container, context))
            {
                Visit(container, context);
            }
        }

        private void DrawText(UTinyObject tinyObject)
        {
            var textProperty = tinyObject.Properties.PropertyBag.FindProperty("text") as IProperty<UTinyObject.PropertiesContainer, string>;
            EditorGUI.BeginChangeCheck();
            var mixed = EditorGUI.showMixedValue;
            EditorGUI.showMixedValue = HasMixedValues<string>(tinyObject.Properties, textProperty);
            var isOverriden = (textProperty as IUTinyValueProperty)?.IsOverridden(tinyObject.Properties) ?? true;
            UTinyEditorUtility.SetEditorBoldDefault(isOverriden);
            try
            {
                var container = tinyObject.Properties;
                var newText = EditorGUILayout.TextField(textProperty.Name, textProperty.GetValue(container));
                
                if (EditorGUI.EndChangeCheck())
                {
                    textProperty.SetValue(container, newText);
                    PushChange(container, textProperty);
                }
            }
            finally
            {
                UTinyEditorUtility.SetEditorBoldDefault(false);
                EditorGUI.showMixedValue = mixed;
            }
        }
        
        private UTinyEnum.Reference DrawEnum(UTinyObject tinyObject, string fieldName)
        {
            var field = (UTinyEnum.Reference)tinyObject[fieldName];
            var prop = tinyObject.Properties.PropertyBag.FindProperty(fieldName) as IProperty<UTinyObject.PropertiesContainer, UTinyEnum.Reference>;
            var container = tinyObject.Properties;
            var context = new VisitContext<UTinyEnum.Reference> {Index = -1, Property = prop, Value = field};
            if (false == ExcludeVisit(container, context))
            {
                Visit(container, context);
            }
            return field;
        }
    }
}
#endif // NET_4_6
