#if NET_4_6
using System;
using System.Collections.Generic;
using System.Linq;

using UnityEditor;
using UnityEngine;
using UnityEngine.Assertions;
using Unity.Properties;
using Unity.Tiny.Attributes;
using static Unity.Tiny.Attributes.InspectorAttributes;

namespace Unity.Tiny
{
    public class UTinyInspectorIMGUIVisitor : GenericIMGUIVisitor,
        ICustomUIVisit<UTinyObject>,
        ICustomUIVisit<UTinyList>,
        ICustomUIVisit<UTinyEntity.Reference>,
        ICustomUIVisit<UTinyEnum.Reference>,
        ICustomUIVisit<UTinyType>
    {
        #region Static
        private static readonly Dictionary<UTinyObject, bool> s_FoldoutCache = new Dictionary<UTinyObject, bool>();
        private static readonly ComponentEditor s_DefaultComponentVisitor = new ComponentEditor();
        private static readonly StructDrawer s_DefaultStructVisitor = new StructDrawer();
        protected static readonly List<UTinyType> s_Bindings = new List<UTinyType>();

        private static int s_Depth;
        protected static bool IsRoot { get { return s_Depth == 1; } }
        #endregion

        #region Fields
        public IRegistry Registry { get { return UTinyEditorApplication.Registry; } }
        #endregion

        #region GenericIMGUIVisitor

        protected override void OnPrepare()
        {
            EditorGUI.indentLevel = 0;
            s_Depth = 0;
            s_Bindings.Clear();
            UTinyGUI.BackgroundColor(new Rect(0, 0, Screen.width, Screen.height), UTinyColors.Inspector.Background);
        }

        #endregion

        protected virtual bool ValidateObject(UTinyObject tiny, UTinyType type)
        {
            return true;
        }

        protected virtual bool AreListReadOnly { get; set; }

        protected virtual bool AreEntityReferencesReadOnly { get; set; }

        public bool CustomUIVisit<TContainer>(ref TContainer container, ref VisitContext<UTinyObject> context) where TContainer : IPropertyContainer
        {
            Assert.IsNotNull(context.Value);
            var tiny = context.Value;
            var version = tiny.Version;
            var type = tiny.Type.Dereference(tiny.Registry);

            // Make sure the type  exists.
            if (null == type)
            {
                ShowTypeNotFound(tiny, IsRoot);
                return true;
            }

            type.Refresh();

            // Make sure the type is included in the project.
            var editorContext = UTinyEditorApplication.EditorContext;
            var mainModuleRef = editorContext.Project.Module;
            var mainModule = mainModuleRef.Dereference(editorContext.Registry);
            
            var moduleContainingType = editorContext.Registry.FindAllByType<UTinyModule>().First(m => m.Types.Contains(tiny.Type));
            
            if (!moduleContainingType.Equals(mainModule) && !mainModule.Dependencies.Contains((UTinyModule.Reference)moduleContainingType))
            {
                if (type.IsConfiguration)
                {
                    // Silently exit. This is a design choice, we want to preserve any configuration data on the entity but not show or remove the component
                    // This may be changed in the future as the number of configuration components increases.
                    return true;
                }
                
                ShowTypeMissing(tiny, IsRoot, mainModule, moduleContainingType);
                return true;
            }
            
            // Make sure that the object itself should be allowed to be shown.
            if (!ValidateObject(tiny, type))
            {
                return true;
            }

            PushEnabledState();
            ++s_Depth;
            try
            {
                return VisitTinyObject(ref container, tiny, type, context.Index);
            }
            finally
            {
                PopEnabledState();
                --s_Depth;
                // If version changed, we need to run the bindings 
                if (version != tiny.Version && type.TypeCode == UTinyTypeCode.Component)
                {
                    s_Bindings.Add(type);
                }
            }
        }
        
        public bool CustomUIVisit<TContainer>(ref TContainer container, ref VisitContext<UTinyList> context) where TContainer : IPropertyContainer
        {
            PushEnabledState();
            try
            {
                GUI.enabled &= !AreListReadOnly;

                if (Targets.Count > 1)
                {
                    EditorGUILayout.HelpBox($"{UTinyConstants.ApplicationName}: Editing an array with multiple targets is not supported.", MessageType.Info);
                    return true;
                }

                context.Value.Visit(this);
                return true;
            }
            finally
            {
                PopEnabledState();
            }
        }
        
        public bool CustomUIVisit<TContainer>(ref TContainer container, ref VisitContext<UTinyEntity.Reference> context) where TContainer : IPropertyContainer
        {
            PushEnabledState();
            try
            {
                GUI.enabled &= !AreEntityReferencesReadOnly;
                DoField(ref container, ref context, (label, val) =>
                {
                    var entityRef = val;
                    var entity = entityRef.Dereference(Registry);
                    var view = entity?.View;
                    view?.RefreshName();
                    EditorGUI.BeginChangeCheck();
                    var newView = (UTinyEntityView)EditorGUILayout.ObjectField(label, view, typeof(UTinyEntityView), true);
                    if (EditorGUI.EndChangeCheck())
                    {
                        if (null != newView)
                        {
                            return newView.EntityRef;
                        }
                        else
                        {
                            return UTinyEntity.Reference.None;
                        }
                    }
                    return entityRef;
                });
            }
            finally
            {
                PopEnabledState();
            }
            return true;
        }
        
        public bool CustomUIVisit<TContainer>(ref TContainer container, ref VisitContext<UTinyEnum.Reference> context) where TContainer : IPropertyContainer
        {
            var value = context.Value;
            DoField(ref container, ref context, (label, val) =>
            {
                var type = value.Type.Dereference(Registry);
                var field = type.FindFieldById(val.Id);
                var names = type.Fields.Select(f => new GUIContent(f.Name)).ToArray();
                var index = Mathf.Clamp(type.Fields.IndexOf(field), 0, names.Length);

                if (names.Length == 0)
                {
                    EditorGUILayout.Popup(label, -1, names);
                    return val;
                }

                EditorGUI.BeginChangeCheck();
                index = EditorGUILayout.Popup(label, index, names);
                if (EditorGUI.EndChangeCheck())
                {
                    var newType = type.Fields[index];
                    return new UTinyEnum.Reference(type, newType.Id);
                }
                return val;
            });

            return true;
        }
        
        public bool CustomUIVisit<TContainer>(ref TContainer container, ref VisitContext<UTinyType> context) where TContainer : IPropertyContainer
        {
            var type = context.Value;

            var defaultValue = (UTinyObject)type.DefaultValue;
            var defaultValueType = defaultValue.Type.Dereference(Registry);
            if (null == defaultValueType)
            {
                return true;
            }

            var typeCode = defaultValueType.TypeCode;
            switch (typeCode)
            {
                case UTinyTypeCode.Configuration:
                case UTinyTypeCode.Component:
                case UTinyTypeCode.Struct:
                case UTinyTypeCode.Enum:
                    var objectContext =
                        new VisitContext<UTinyObject>
                        {
                            Index = context.Index,
                            Property = null,
                            Value = defaultValue
                        };
                    return CustomUIVisit(ref container, ref objectContext);
                default:
                    throw new InvalidOperationException();
            }
        }
        
        private void TransferState(UTinyInspectorIMGUIVisitor other)
        {
            other.Targets = Targets;
            other.Changes = Changes;
            other.Mode = Mode;
            other.AreEntityReferencesReadOnly = AreEntityReferencesReadOnly;
            other.AreListReadOnly = AreListReadOnly;
        }

        protected static bool GetFoldoutFromCache(UTinyObject tiny)
        {
            bool value;
            if (!s_FoldoutCache.TryGetValue(tiny, out value))
            {
                value = s_FoldoutCache[tiny] = true;
            }
            return value;
        }

        protected static bool UpdateFoldout(UTinyObject tiny, bool foldout)
        {
            s_FoldoutCache[tiny] = foldout;
            return foldout;
        }

        private bool Foldout(bool foldout, string name, bool showArrow, UTinyObject tiny)
        {
            if (showArrow)
            {
                foldout = EditorGUILayout.Foldout(foldout, "  " + name, true, UTinyStyles.ComponenHeaderFoldout);
            }
            else
            {
                GUILayout.Space(24);
                EditorGUILayout.LabelField(name, UTinyStyles.ComponenHeaderLabel);
            }

            var rect = GUILayoutUtility.GetLastRect();
            if (Event.current.type == EventType.MouseDown && rect.Contains(Event.current.mousePosition) && Event.current.button == 1 && null != tiny && TargetType == typeof(UTinyEntity))
            {
                var menu = new GenericMenu();
                menu.AddItem(new GUIContent("Inspect Initial Values.."), false, () => UTinyTypeViewer.SetType(tiny.Type.Dereference(tiny.Registry)));
                menu.AddItem(new GUIContent("Reset Initial Values.."), false, () => tiny.Reset());
                menu.ShowAsContext();
            }
            return foldout;
        }

        private void ShowTypeNotFound(UTinyObject tinyObject, bool isRoot)
        {
            using (new EditorGUILayout.HorizontalScope(UTinyStyles.TypeNotFoundStyle))
            {
                if (isRoot)
                {
                    Foldout(false, tinyObject.Name + " (Missing)", false, null);
                }
                else
                {
                    EditorGUILayout.LabelField($"Type of field '{tinyObject.Type.Name}' is missing.");
                }
                ShowRemoveComponent(tinyObject.Type);
            }
            GUILayout.Space(5.0f);
        }

        private void ShowTypeMissing(UTinyObject tinyObject, bool isRoot, UTinyModule mainModule, UTinyModule moduleContainingType)
        {
            using (new EditorGUILayout.HorizontalScope(UTinyStyles.TypeMissingStyle))
            {

                if (isRoot)
                {
                    Foldout(false, tinyObject.Name + " (Missing)", false, null);
                }
                else
                {
                    EditorGUILayout.LabelField($"Type '{tinyObject.Type.Name}' is missing.");
                }

                ShowRemoveComponent(tinyObject.Type);
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                GUILayout.FlexibleSpace();
                if (GUILayout.Button($"Add '{moduleContainingType.Name}' module"))
                {
                    mainModule.AddExplicitModuleDependency((UTinyModule.Reference)moduleContainingType);
                }
                GUILayout.FlexibleSpace();
            }
            GUILayout.Space(5.0f);
        }

        public bool VisitTinyObject<TContainer>(ref TContainer container, UTinyObject tiny, UTinyType type, int index) where TContainer : IPropertyContainer
        {
            bool showProperties = true;

            // Root is [component|struct|enum], and recursive calls are [struct|enum] 
            if (IsRoot)
            {
                GUILayout.Space(5);
                // Header
                showProperties = DoRootObject(tiny, type);
            }

            try
            {
                if (EditorGUILayout.BeginFadeGroup(showProperties ? 1.0f : 0.0f))
                {
                    GUI.enabled &= !type.HasAttribute<ReadonlyAttribute>();
                    var indent = EditorGUI.indentLevel;
                    EditorGUI.indentLevel = s_Depth - 1;

                    try
                    {
                        switch (type.TypeCode)
                        {
                            case UTinyTypeCode.Configuration:
                            case UTinyTypeCode.Component:
                                return VisitComponent(tiny, type);
                            case UTinyTypeCode.Struct:
                            case UTinyTypeCode.Enum:
                                return VisitStructOrEnum(ref container, tiny, type, index);
                            default:
                                return false;
                        }
                    }
                    finally
                    {
                        EditorGUI.indentLevel = indent;
                        if (IsRoot && tiny.Properties.PropertyBag.PropertyCount > 0)
                        {
                            GUILayout.Space(5);
                        }
                        EditorGUILayout.EndFadeGroup();
                    }
                }
            }
            finally
            {
                if (IsRoot)
                {
                    UTinyGUILayout.Separator(UTinyColors.Inspector.Separator, UTinyGUIUtility.ComponentSeperatorHeight);
                }
            }
            return true;
        }

        private bool VisitComponent(UTinyObject tiny, UTinyType type)
        {
            ++EditorGUI.indentLevel;
            try
            {
                var editor = Mode == InspectMode.Normal && type.HasAttribute<EditorInspectorAttributes.CustomComponentEditor>() ?
                        type.GetAttribute<EditorInspectorAttributes.CustomComponentEditor>().Visitor :
                        s_DefaultComponentVisitor;
                TransferState(editor);
                return editor.VisitComponent(tiny);
            }
            finally
            {
                --EditorGUI.indentLevel;
            }
        }

        private bool VisitStructOrEnum<TContainer>(ref TContainer container, UTinyObject tiny, UTinyType type, int index) where TContainer : IPropertyContainer
        {
            var drawer = Mode == InspectMode.Normal && type.HasAttribute<EditorInspectorAttributes.CustomDrawerAttribute>() ?
                                        type.GetAttribute<EditorInspectorAttributes.CustomDrawerAttribute>().Visitor :
                                        s_DefaultStructVisitor;
            TransferState(drawer);
            var label = ConstructLabel(index >= 0 ? index.ToString() : tiny.Name);

            // We are in a list
            if (index >= 0)
            {
                EditorGUILayout.BeginHorizontal();
                EditorGUILayout.BeginVertical();
            }
            try
            {
                return drawer.VisitStruct(tiny, label);
            }
            finally
            {
                if (index >= 0)
                {
                    EditorGUILayout.EndVertical();
                    if (GUILayout.Button("x", GUILayout.Width(16.0f), GUILayout.Height(16.0f)))
                    {
                        RemoveAtIndex = index;
                    }
                    EditorGUILayout.EndHorizontal();
                }
            }
        }

        private bool DoRootObject(UTinyObject tiny, UTinyType type)
        {
            var foldout = GetFoldoutFromCache(tiny);
            EditorGUILayout.BeginHorizontal();
            foldout = UpdateFoldout(tiny, Foldout(foldout, type.Name, tiny.Properties.PropertyBag.PropertyCount > 0, tiny));
            ShowRemoveComponent((UTinyType.Reference)type);
            EditorGUILayout.EndHorizontal();
            GUILayout.Space(5.0f);

            return foldout;
        }

        private void ShowRemoveComponent(UTinyType.Reference typeRef)
        {
            var type = typeRef.Dereference(Registry);
            if ((null == type || type.TypeCode == UTinyTypeCode.Component) && TargetType == typeof(UTinyEntity))
            {
                var rect = EditorGUILayout.GetControlRect(false, GUILayout.Width(16.0f));
                if (GUI.Button(rect, UTinyIcons.X_Icon_8, UTinyStyles.MiddleCenteredLabel))
                {
                    var targets = Targets.Cast<UTinyEntity>().ToList();
                    EditorApplication.delayCall += () =>
                    {
                        foreach (var entity in targets.Cast<UTinyEntity>())
                        {
                            entity.RemoveComponent(typeRef);
                        }
                        UTinyHierarchyWindow.InvalidateDataModel();
                        UTinyInspector.RepaintAll();
                    };
                }
                GUILayout.Space(5.0f);
            }
        }
    }
}
#endif // NET_4_6
