#if NET_4_6
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace Unity.Tiny
{
    public static class UTinyEditorUtility
    {
        static MethodInfo s_SetBoldDefaultFontMethod;

        static UTinyEditorUtility()
        {
            s_SetBoldDefaultFontMethod = typeof(EditorGUIUtility).GetMethod("SetBoldDefaultFont", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
            if (null == s_SetBoldDefaultFontMethod)
            {
                Debug.Log($"{UTinyConstants.ApplicationName}: Could not find the EditorGUIUtility.SetBoldDefaultFont method.");
            }
        }


        public delegate T[] ObjectSelector<out T>(Object[] objects);

        public static T[] DoDropField<T>(Rect position, int id, ObjectSelector<T> selector, GUIStyle style)
        {
            var evt = Event.current;
            var eventType = evt.type;

            if (!GUI.enabled && (Event.current.rawType == EventType.MouseDown))
            {
                eventType = Event.current.rawType;
            }

            switch (eventType)
            {
                case EventType.DragExited:
                    if (GUI.enabled)
                    {
                        HandleUtility.Repaint();
                    }
                    break;

                case EventType.DragUpdated:
                case EventType.DragPerform:
                {
                    if (position.Contains(Event.current.mousePosition) && GUI.enabled)
                    {
                        var validatedObject = selector(DragAndDrop.objectReferences);

                        if (validatedObject.Length > 0)
                        {
                            DragAndDrop.visualMode = DragAndDropVisualMode.Generic;

                            if (eventType == EventType.DragPerform)
                            {
                                GUI.changed = true;
                                DragAndDrop.AcceptDrag();
                                DragAndDrop.activeControlID = 0;
                                Event.current.Use();
                                return validatedObject;
                            }

                            DragAndDrop.activeControlID = id;
                        }
                    }
                }
                    break;

                case EventType.Repaint:
                {
                    style.Draw(position, GUIContent.none, id, DragAndDrop.activeControlID == id);
                }
                    break;
            }

            return null;
        }

        public static void RepaintAllWindows()
        {
            UTinyProjectWindow.RepaintAll();
            UTinyInspector.RepaintAll();
            UTinyHierarchyWindow.RepaintAll();
        }

        public static void SetEditorBoldDefault(bool bold)
        {
            if (null != s_SetBoldDefaultFontMethod)
            {
                s_SetBoldDefaultFontMethod.Invoke(null, new object[] { bold });
            }
        }
    }
}
#endif // NET_4_6
