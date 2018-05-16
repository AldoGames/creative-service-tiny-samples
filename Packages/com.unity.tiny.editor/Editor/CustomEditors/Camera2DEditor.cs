#if NET_4_6
using System.Collections.Generic;

using UnityEditor;
using UnityEngine;

using Unity.Properties;

using Unity.Tiny.Conversions;

namespace Unity.Tiny
{
    public class Camera2DEditor : ComponentEditor
    {
        public override bool VisitComponent(UTinyObject tinyObject)
        {
            DrawClearFlags(tinyObject);
            DrawCullingMask(tinyObject);
            DoField<float>(tinyObject, "halfVerticalSize");
            DrawSubObject(tinyObject, "rect");
            DoField<float>(tinyObject, "depth");
            return true;
        }

        private void DrawClearFlags(UTinyObject tinyObject)
        {
            var fieldName = "clearFlags";
            DrawEnum(tinyObject, fieldName);
            if (tinyObject.GetProperty<CameraClearFlags>(fieldName) == CameraClearFlags.Color)
            {
                DrawSubObject(tinyObject, "backgroundColor");
            }
        }

        private void DoField<T>(UTinyObject tinyObject, string fieldName)
        {
            var container = tinyObject.Properties;
            var prop = container.PropertyBag.FindProperty(fieldName) as IProperty<UTinyObject.PropertiesContainer, T>;
            var context = new VisitContext<T> {Index = -1, Property = prop, Value = prop.GetValue(container)};
            if (false == ExcludeVisit(container, context))
            {
                Visit(container, context);
            }
        }

        private void DrawCullingMask(UTinyObject tinyObject)
        {
            var cullingMask = tinyObject.Properties.PropertyBag.FindProperty("layerMask") as IProperty<UTinyObject.PropertiesContainer, int>;
            EditorGUI.BeginChangeCheck();
            var mixed = EditorGUI.showMixedValue;
            EditorGUI.showMixedValue = HasMixedValues<int>(tinyObject.Properties, cullingMask);
            var isOverriden = (cullingMask as IUTinyValueProperty)?.IsOverridden(tinyObject.Properties) ?? true;
            UTinyEditorUtility.SetEditorBoldDefault(isOverriden);
            try
            {
                var container = tinyObject.Properties;
                var layerNames = GetLayerNames();
                var newLayer = EditorGUILayout.MaskField("cullingMask", GetCurrentEditorLayer(layerNames, cullingMask.GetValue(container)), layerNames.ToArray());
                if (EditorGUI.EndChangeCheck())
                {
                    cullingMask.SetValue(container, GetLayers(layerNames, newLayer));
                    PushChange(container, cullingMask);
                }
            }
            finally
            {
                UTinyEditorUtility.SetEditorBoldDefault(false);
                EditorGUI.showMixedValue = mixed;
            }
        }

        private void DrawEnum(UTinyObject tinyObject, string fieldName)
        {
            var field = (UTinyEnum.Reference)tinyObject[fieldName];
            var prop = tinyObject.Properties.PropertyBag.FindProperty(fieldName) as IProperty<UTinyObject.PropertiesContainer, UTinyEnum.Reference>;
            var container = tinyObject.Properties;
            var context = new VisitContext<UTinyEnum.Reference> {Index = -1, Property = prop, Value = field};
            if (false == ExcludeVisit(container, context))
            {
                Visit(container, context);
            }
        }

        private void DrawSubObject(UTinyObject tinyObject, string fieldName)
        {
            var field = tinyObject[fieldName] as UTinyObject;
            var container = tinyObject.Properties;
            var prop = container.PropertyBag.FindProperty(fieldName) as IProperty<UTinyObject.PropertiesContainer, UTinyObject>;
            var context = new VisitContext<UTinyObject> {Index = -1, Property = prop, Value = field};
            if (false == ExcludeVisit(container, context))
            {
                Visit(container, context);
            }
        }

        private static List<string> GetLayerNames()
        {
            List<string> names = new List<string>();
            for (int i = 0; i < 32; ++i)
            {
                var name = LayerMask.LayerToName(i);
                if (string.IsNullOrEmpty(name))
                {
                    continue;
                }

                names.Add(name);
            }
            return names;
        }

        private static int GetLayers(List<string> layerNames, int editorMask)
        {
            if (editorMask < 0)
            {
                return editorMask;
            }

            int layer = 0;

            if (editorMask != 0)
            {
                for (int i = 0; i < 32; ++i)
                {
                    if ((editorMask & 1 << i) == (1 << i))
                    {
                        layer |= 1 << LayerMask.NameToLayer(layerNames[i]);
                    }
                }
            }
            return layer;
        }

        private static int GetCurrentEditorLayer(List<string> layerNames, int cullingMask)
        {
            if (cullingMask < 0)
            {
                return cullingMask;
            }
            int layer = 0;
            for (int i = 0; i < 32; ++i)
            {
                if ((cullingMask & 1 << i) == 1 << i)
                {
                    var index = layerNames.FindIndex(s => s == LayerMask.LayerToName(i));
                    layer |= 1 << index;
                }
            }
            return layer;
        }
    }
}
#endif // NET_4_6
