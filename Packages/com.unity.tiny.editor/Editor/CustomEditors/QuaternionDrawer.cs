#if NET_4_6
using UnityEditor;
using UnityEngine;

using Unity.Properties;

using Unity.Tiny.Extensions;
using Unity.Tiny.Conversions;

namespace Unity.Tiny
{
    public class QuaternionDrawer : StructDrawer
    {
        public override bool VisitStruct(UTinyObject tinyObject, GUIContent label)
        {
            var properties = tinyObject.Properties;

            //For the rotation, we will offer Euler angles to the user.
            var tinyEuler = GetEulerAnglesObject(tinyObject);
            tinyEuler.Refresh();

            if (Screen.width < 400)
            {
                EditorGUIUtility.labelWidth = Mathf.Max(EditorGUIUtility.labelWidth - (400 - Screen.width), 70);
            }

            var mixed = EditorGUI.showMixedValue;
            var indent = EditorGUI.indentLevel;

            UTinyEditorUtility.SetEditorBoldDefault(tinyObject.IsOverridden);
            try
            {
                EditorGUI.showMixedValue =
                    HasMixedValues<float>(properties, properties.PropertyBag.FindProperty("x")) ||
                    HasMixedValues<float>(properties, properties.PropertyBag.FindProperty("y")) ||
                    HasMixedValues<float>(properties, properties.PropertyBag.FindProperty("z")) ||
                    HasMixedValues<float>(properties, properties.PropertyBag.FindProperty("w"));


                EditorGUILayout.BeginHorizontal();
                if (!string.IsNullOrEmpty(label.text))
                {
                    EditorGUILayout.PrefixLabel(label);
                }

                EditorGUIUtility.labelWidth = 15;
                EditorGUIUtility.fieldWidth = 30;
                EditorGUI.indentLevel = 0;

                EditorGUI.BeginChangeCheck();

                Draw(tinyEuler.Properties, tinyEuler.Properties.PropertyBag.FindProperty("x") as IProperty<UTinyObject.PropertiesContainer, float>);
                Draw(tinyEuler.Properties, tinyEuler.Properties.PropertyBag.FindProperty("y") as IProperty<UTinyObject.PropertiesContainer, float>);
                Draw(tinyEuler.Properties, tinyEuler.Properties.PropertyBag.FindProperty("z") as IProperty<UTinyObject.PropertiesContainer, float>);

                if (EditorGUI.EndChangeCheck())
                {
                    SetQuaternionFromEuler(tinyObject, tinyEuler);
                    PushChange(properties, properties.PropertyBag.FindProperty("x"));
                    PushChange(properties, properties.PropertyBag.FindProperty("y"));
                    PushChange(properties, properties.PropertyBag.FindProperty("z"));
                    PushChange(properties, properties.PropertyBag.FindProperty("w"));
                }
            }
            finally
            {
                UTinyEditorUtility.SetEditorBoldDefault(false);
                EditorGUILayout.EndHorizontal();
                EditorGUI.showMixedValue = mixed;
                EditorGUI.indentLevel = indent;
                EditorGUIUtility.fieldWidth = 0;
                EditorGUIUtility.labelWidth = 0;
            }
            return true;
        }

        private static UTinyObject GetEulerAnglesObject(UTinyObject quaternion)
        {
            var localQuat = new Quaternion((float)quaternion["x"], (float)quaternion["y"], (float)quaternion["z"], (float)quaternion["w"]);
            var euler = localQuat.eulerAngles;
            var vector3 = quaternion.Registry.GetVector3Type();
            
            return new UTinyObject(quaternion.Registry, vector3)
            {
                ["x"] = euler.x,
                ["y"] = euler.y,
                ["z"] = euler.z,
                Name = quaternion.Name
            };
        }

        private static void SetQuaternionFromEuler(UTinyObject quaternion, UTinyObject euler)
        {
            var localQuat = new Quaternion();
            localQuat.eulerAngles = euler.As<Vector3>();
            quaternion["x"] = localQuat.x;
            quaternion["y"] = localQuat.y;
            quaternion["z"] = localQuat.z;
            quaternion["w"] = localQuat.w;
        }

        private void Draw(UTinyObject.PropertiesContainer container, IProperty<UTinyObject.PropertiesContainer, float> property)
        {
            var current = property.GetValue(container);
            var value = EditorGUILayout.FloatField(property.Name, current);
            if (value != current)
            {
                property.SetValue(container, value);
            }
        }
    }
}
#endif // NET_4_6
