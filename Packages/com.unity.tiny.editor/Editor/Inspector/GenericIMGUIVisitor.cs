#if NET_4_6
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using Unity.Properties;
using System;

namespace Unity.Tiny
{
    using Attributes;
    using static Attributes.InspectorAttributes;
    
    public interface ICustomUIVisit<TValue>
    {
        bool CustomUIVisit<TContainer>(ref TContainer container, ref VisitContext<TValue> context)
            where TContainer : IPropertyContainer;
    }
    
    public interface ICustomUIVisitPrimitives :
        ICustomUIVisit<bool>
        , ICustomUIVisit<sbyte>
        , ICustomUIVisit<byte>
        , ICustomUIVisit<short>
        , ICustomUIVisit<ushort>
        , ICustomUIVisit<int>
        , ICustomUIVisit<uint>
        , ICustomUIVisit<long>
        , ICustomUIVisit<ulong>
        , ICustomUIVisit<float>
        , ICustomUIVisit<double>
        , ICustomUIVisit<char>
        , ICustomUIVisit<string>
    {}

    public class GenericIMGUIVisitor : PropertyVisitorAdapter,
        ICustomUIVisitPrimitives,
        ICustomUIVisit<Texture2D>,
        ICustomUIVisit<Sprite>,
        ICustomUIVisit<AudioClip>,
        ICustomUIVisit<Font>
    {

        #region Static
        private static readonly GUIContent k_Label = new GUIContent();
        private static readonly Stack<bool> k_EnabledStack = new Stack<bool>();
        #endregion

        #region Properties
        public InspectMode Mode { get; set; } = InspectMode.Normal;
        public List<IPropertyContainer> Targets { get; set; } = new List<IPropertyContainer>();
        public HashSet<PropertyPathContext> Changes { get; internal set; } = new HashSet<PropertyPathContext>();
        public Type TargetType { get { return Targets[0].GetType(); } }
        #endregion

        private PropertyPath m_Path = new PropertyPath();
        protected int RemoveAtIndex { get; set; } = -1;

        public void Prepare()
        {
            Changes.Clear();
            OnPrepare();
        }

        public void Complete()
        {
            var hasChanges = Changes.Count > 0;
            
            PropagateChanges();
            OnComplete();
            
            if (hasChanges)
            {
                SceneView.RepaintAll();
            }
        }

        public virtual bool ValidateTarget(IPropertyContainer container) { return true; }

        public virtual void Header() { }

        protected virtual void OnPrepare() {}

        public virtual void Visit()
        {
            if (Targets.Count == 0)
            {
                return;
            }
            Targets[0].Visit(this);
        }

        public virtual void Footer() {}

        protected virtual void OnComplete() { }

        protected IListProperty CurrentListProperty { get; private set; }

        #region IPropertyVisitor

        public override bool ExcludeVisit<TContainer, TValue>(TContainer container, VisitContext<TValue> context)
        {
            var customHandler = this as ICustomUIVisit<TValue>;
            if (customHandler == null)
            {
                return false;
            }

            var previous = context.Value;
            if (false == customHandler.CustomUIVisit(ref container, ref context))
            {
                // early exit - no need to setvalue
                return true;
            }
            
            var next = context.Value;
            var property = context.Property;
            
            // Handle list elements
            if (context.Index >= 0 && null != CurrentListProperty)
            {
                if (ValuesAreDifferent(previous, next))
                {
                    var typedProperty = (IListProperty<TContainer, TValue>) property;
                    typedProperty.SetItemAt(container, context.Index, next);
                    PushChange(container, property);
                }
            }
            else
            {
                var isReadOnly = IsReadOnly(property) || property.IsReadOnly;
                
                if (ValuesAreDifferent(previous, next) && !isReadOnly)
                {
                    var typedProperty = (IProperty<TContainer, TValue>) property;
                    typedProperty.SetValue(container, next);
                    PushChange(container, property);
                }
            }
            
            return true;
        }

        public override bool ExcludeVisit<TContainer, TValue>(ref TContainer container, VisitContext<TValue> context)
        {
            var customHandler = this as ICustomUIVisit<TValue>;
            if (customHandler == null)
            {
                return false;
            }

            var previous = context.Value;
            if (false == customHandler.CustomUIVisit(ref container, ref context))
            {
                // early exit - no need to setvalue
                return true;
            }
            
            var next = context.Value;
            var property = context.Property;
            var isReadOnly = IsReadOnly(property);
            
            if (ValuesAreDifferent(previous, next) && !isReadOnly && !property.IsReadOnly)
            {
                var typedProperty = (IStructProperty<TContainer, TValue>) property;
                typedProperty.SetValue(ref container, next);
                PushChange(container, property);
            }
            return true;
        }

        public override bool BeginList<TContainer, TValue>(TContainer container, VisitContext<TValue> context)
        {
            return BeginList(container, (IListProperty) context.Property);
        }
        
        public override bool BeginList<TContainer, TValue>(ref TContainer container, VisitContext<TValue> context)
        {
            return BeginList(container, (IListProperty) context.Property);
        }

        protected bool BeginList(IPropertyContainer container, IListProperty property)
        {
            CurrentListProperty = property;
            if (ShouldHide(property))
            {
                return false;
            }
            PushEnabledState();
            GUI.enabled &= !IsReadOnly(property);
            if (HasHeader(property))
            {
                DrawHeader(property);
            }

            if (CanDrawAsList(property))
            {
                if (Targets.Count > 1)
                {
                    EditorGUILayout.HelpBox($"{UTinyConstants.ApplicationName}: Editing an array with multiple targets is not supported.", MessageType.Info);
                    return false;
                }
                // [MP] @HACK: Special case for UTinyList where the property name doesn't match the field name.
                var list = container as UTinyList;
                if (list != null)
                {
                    EditorGUILayout.LabelField(list.Name);
                }
                else
                {
                    EditorGUILayout.LabelField(ConstructLabel(property));
                }
                EditorGUILayout.BeginHorizontal();
                GUILayout.Space(EditorGUI.indentLevel * 15.0f);
                EditorGUILayout.BeginVertical(GUI.skin.box);
            }
            return true;
        }

        public override bool BeginContainer<TContainer, TValue>(TContainer container, VisitContext<TValue> context)
        {
            return BeginContainer(context.Property, context.Index);
        }
        
        public override bool BeginContainer<TContainer, TValue>(ref TContainer container, VisitContext<TValue> context)
        {
            return BeginContainer(context.Property, context.Index);
        }

        protected bool BeginContainer(IProperty property, int listIndex)
        {
            m_Path.Push(property.Name, listIndex);
            
            if (ShouldHide(property))
            {
                return false;
            }
            PushEnabledState();
            GUI.enabled &= !IsReadOnly(property);
            if (HasHeader(property))
            {
                DrawHeader(property);
            }
            EditorGUILayout.LabelField(ConstructLabel(property));
            ++EditorGUI.indentLevel;
            return true;
        }
        
        public override void EndList<TContainer, TValue>(TContainer container, VisitContext<TValue> context)
        {
            EndList(container, (IListProperty)context.Property);
        }

        public override void EndList<TContainer, TValue>(ref TContainer container, VisitContext<TValue> context)
        {
            EndList(container, (IListProperty)context.Property);
        }

        protected void EndList(IPropertyContainer container, IListProperty property)
        {
            CurrentListProperty = null;
            if (ShouldHide(property))
            {
                return;
            }
            PopEnabledState();
            
            if (CanDrawAsList(property))
            {
                // Commit any removed items
                if (RemoveAtIndex >= 0)
                {
                    property.RemoveAt(container, RemoveAtIndex);
                    RemoveAtIndex = -1;
                }
                
                if (Targets.Count > 1)
                {
                    return;
                }
                EditorGUILayout.BeginHorizontal();

                GUILayout.FlexibleSpace();
                if (GUILayout.Button("Add", GUILayout.Width(32.0f), GUILayout.Height(16.0f)))
                {
                    property.AddNewItem(container);
                }
                GUILayout.Space(15.0f);

                EditorGUILayout.EndHorizontal();
                EditorGUILayout.EndVertical();
                GUILayout.Space(5.0f);
                EditorGUILayout.EndHorizontal();
            }
        }

        public override void EndContainer<TContainer, TValue>(TContainer container, VisitContext<TValue> context)
        {
            EndContainer(context.Property);
        }

        public override void EndContainer<TContainer, TValue>(ref TContainer container, VisitContext<TValue> context)
        {
            EndContainer(context.Property);
        }

        protected void EndContainer(IProperty property)
        {
            m_Path.Pop();
            
            if (ShouldHide(property))
            {
                return;
            }
            --EditorGUI.indentLevel;
            PopEnabledState();
        }

        public override void Visit<TContainer, TValue>(TContainer container, VisitContext<TValue> context)
        {
            Visit(context.Property);
        }

        public override void Visit<TContainer, TValue>(ref TContainer container, VisitContext<TValue> context)
        {
            Visit(context.Property);
        }

        protected void Visit(IProperty property)
        {
            if (ShouldHide(property))
            {
                return;
            }

            PushEnabledState();
            GUI.enabled &= !IsReadOnly(property);
            if (HasHeader(property))
            {
                DrawHeader(property);
            }

            EditorGUILayout.LabelField(ConstructLabel(property), new GUIContent
            {
                text = $"Type '{property.ValueType.Name}' is not supported."
            });
            PopEnabledState();
        }

        #endregion

        #region ICustomUIVisitPrimitives

        public bool CustomUIVisit<TContainer>(ref TContainer container, ref VisitContext<sbyte> context)
            where TContainer : IPropertyContainer
        {
            return DoField(ref container, ref context,
                (label, val) =>
                    (sbyte) Mathf.Clamp(EditorGUILayout.IntField(label, val), sbyte.MinValue, sbyte.MaxValue));
        }

        public bool CustomUIVisit<TContainer>(ref TContainer container, ref VisitContext<short> context)
            where TContainer : IPropertyContainer
        {
            return DoField(ref container, ref context,
                (label, val) =>
                    (short) Mathf.Clamp(EditorGUILayout.IntField(label, val), short.MinValue, short.MaxValue));
        }

        public bool CustomUIVisit<TContainer>(ref TContainer container, ref VisitContext<int> context)
            where TContainer : IPropertyContainer
        {
            return DoField(ref container, ref context, (label, val) => EditorGUILayout.IntField(label, val));
        }

        public bool CustomUIVisit<TContainer>(ref TContainer container, ref VisitContext<long> context) where TContainer : IPropertyContainer
        {
            return DoField(ref container, ref context, (label, val) => EditorGUILayout.LongField(label, val));
        }

        public bool CustomUIVisit<TContainer>(ref TContainer container, ref VisitContext<byte> context) where TContainer : IPropertyContainer
        {
            return DoField(ref container, ref context, (label, val) => (byte)Mathf.Clamp(EditorGUILayout.IntField(label, val), byte.MinValue, byte.MaxValue));
        }

        public bool CustomUIVisit<TContainer>(ref TContainer container, ref VisitContext<ushort> context)
            where TContainer : IPropertyContainer
        {
            return DoField(ref container, ref context,
                (label, val) =>
                    (ushort) Mathf.Clamp(EditorGUILayout.IntField(label, val), ushort.MinValue, ushort.MaxValue));
        }

        public bool CustomUIVisit<TContainer>(ref TContainer container, ref VisitContext<uint> context)
            where TContainer : IPropertyContainer
        {
            return DoField(ref container, ref context,
                (label, val) =>
                    (uint) Mathf.Clamp(EditorGUILayout.LongField(label, val), uint.MinValue, uint.MaxValue));
        }

        public bool CustomUIVisit<TContainer>(ref TContainer container, ref VisitContext<ulong> context)
            where TContainer : IPropertyContainer
        {
            return DoField(ref container, ref context, (label, val) =>
            {
                var text = EditorGUILayout.TextField(label, val.ToString());
                ulong num;
                ulong.TryParse(text, out num);
                return num;
            });
        }

        public bool CustomUIVisit<TContainer>(ref TContainer container, ref VisitContext<float> context)
            where TContainer : IPropertyContainer
        {
            return DoField(ref container, ref context, (label, val) => EditorGUILayout.FloatField(label, val));
        }

        public bool CustomUIVisit<TContainer>(ref TContainer container, ref VisitContext<double> context)
            where TContainer : IPropertyContainer
        {
            return DoField(ref container, ref context, (label, val) => EditorGUILayout.DoubleField(label, val));
        }

        public bool CustomUIVisit<TContainer>(ref TContainer container, ref VisitContext<bool> context)
            where TContainer : IPropertyContainer
        {
            return DoField(ref container, ref context, (label, val) => EditorGUILayout.Toggle(label, val));
        }

        public bool CustomUIVisit<TContainer>(ref TContainer container, ref VisitContext<char> context)
            where TContainer : IPropertyContainer
        {
            return DoField(ref container, ref context, (label, val) =>
            {
                var text = EditorGUILayout.TextField(label, val.ToString());
                var c = (string.IsNullOrEmpty(text) ? '\0' : text[0]);
                return c;
            });
        }

        public bool CustomUIVisit<TContainer>(ref TContainer container, ref VisitContext<string> context)
            where TContainer : IPropertyContainer
        {
            return DoField(ref container, ref context, (label, val) =>
            {
                EditorGUILayout.BeginHorizontal();
                var oldIndent = EditorGUI.indentLevel;
                try
                {
                    EditorGUILayout.PrefixLabel(label);
                    EditorGUI.indentLevel = 0;
                    return EditorGUILayout.TextArea(val);
                }
                finally
                {
                    EditorGUI.indentLevel = oldIndent;
                    EditorGUILayout.EndHorizontal();
                }
            });
        }

        public bool CustomUIVisit<TContainer>(ref TContainer container, ref VisitContext<Texture2D> context)
            where TContainer : IPropertyContainer
        {
            return DoField(ref container, ref context,
                (label, val) => (Texture2D) EditorGUILayout.ObjectField(label, val, typeof(Texture2D), false));
        }

        public bool CustomUIVisit<TContainer>(ref TContainer container, ref VisitContext<Sprite> context)
            where TContainer : IPropertyContainer
        {
            return DoField(ref container, ref context,
                (label, val) => (Sprite) EditorGUILayout.ObjectField(label, val, typeof(Sprite), false));
        }

        public bool CustomUIVisit<TContainer>(ref TContainer container, ref VisitContext<AudioClip> context)
            where TContainer : IPropertyContainer
        {
            return DoField(ref container, ref context,
                (label, val) => (AudioClip)EditorGUILayout.ObjectField(label, val, typeof(AudioClip), false));
        }
        
        public bool CustomUIVisit<TContainer>(ref TContainer container, ref VisitContext<Font> context)
            where TContainer : IPropertyContainer
        {
            return DoField(ref container, ref context,
                (label, val) => (Font) EditorGUILayout.ObjectField(label, val, typeof(Font), false));
        }
        
        #endregion

        #region Implementation

        protected bool DoField<TContainer, TValue>(ref TContainer container, ref VisitContext<TValue> context,
            Func<GUIContent, TValue, TValue> onGUI)
            where TContainer : IPropertyContainer
        {
            var property = context.Property;

            // Well, we must be in a list.
            if (context.Index >= 0 && null != CurrentListProperty)
            {
                var label = ConstructLabel(CurrentListProperty);
                label.text = context.Index.ToString();

                EditorGUILayout.BeginHorizontal();
                context.Value = onGUI(label, context.Value);
                if (GUILayout.Button("x", GUILayout.Width(16.0f), GUILayout.Height(16.0f)))
                {
                    RemoveAtIndex = context.Index;
                }

                EditorGUILayout.EndHorizontal();
                return true;
            }

            if (property == null || ShouldHide(property))
            {
                return false;
            }

            PushEnabledState();
            var isReadOnly = IsReadOnly(property);
            GUI.enabled &= !isReadOnly;

            if (HasHeader(property))
            {
                DrawHeader(property);
            }

            var mixed = EditorGUI.showMixedValue;

            var isOverridden = (property as Tiny.IUTinyValueProperty)?.IsOverridden(container) ?? true;
            UTinyEditorUtility.SetEditorBoldDefault(isOverridden);

            try
            {
                EditorGUI.showMixedValue = HasMixedValues<TValue>(container, context.Property);
                context.Value = onGUI(ConstructLabel(property), context.Value);
            }
            finally
            {
                EditorGUI.showMixedValue = mixed;
                UTinyEditorUtility.SetEditorBoldDefault(false);
            }

            PopEnabledState();
            return true;
        }

        protected static void PushEnabledState()
        {
            k_EnabledStack.Push(GUI.enabled);
        }

        protected static void PopEnabledState()
        {
            GUI.enabled = k_EnabledStack.Pop();
        }

        private bool ShouldHide(IProperty property)
        {
            if (Mode == InspectMode.DebugInternal)
            {
                return false;
            }

            if (property.HasAttribute<HideInInspectorAttribute>())
            {
                return true;
            }

            if (property.HasAttribute<VisibilityAttribute>())
            {
                return (Mode & property.GetAttribute<VisibilityAttribute>().Mode) == InspectMode.None;
            }

            return Mode == InspectMode.None;
        }

        protected static bool CanDrawAsList(IProperty property)
        {
            if (property is IListProperty)
            {
                return !property.HasAttribute<DontListAttribute>();
            }
            return false;
        }

        protected static bool IsReadOnly(IProperty property)
        {
            if (null == property)
            {
                return true;
            }
            return property.HasAttribute<ReadonlyAttribute>();
        }

        protected static bool HasHeader(IProperty property)
        {
            return property.HasAttribute<InspectorAttributes.HeaderAttribute>();
        }

        protected void DrawHeader(IProperty property)
        {
            if (Mode != InspectMode.Normal)
            {
                return;
            }
            var header = property.GetAttribute<InspectorAttributes.HeaderAttribute>();
            EditorGUILayout.LabelField(header.Text, EditorStyles.boldLabel);
        }

        protected static GUIContent ConstructLabel(IProperty property)
        {
            return ConstructLabel(property?.Name,
                property.HasAttribute<InspectorAttributes.TooltipAttribute>()
                    ? property.GetAttribute<InspectorAttributes.TooltipAttribute>().Text
                    : string.Empty);
        }

        protected static GUIContent ConstructLabel(string text, string tooltip = null, Texture2D texture = null)
        {
            k_Label.text = text;
            k_Label.tooltip = tooltip;
            k_Label.image = texture;
            return k_Label;
        }

        private static bool ValuesAreDifferent<TValue>(TValue left, TValue right)
        {
            if (null == left && null == right)
            {
                return false;
            }
            if (null == left)
            {
                return true;
            }
            return !left.Equals(right);
        }

        protected void PushChange(IPropertyContainer container, IProperty property)
        {
            Changes.Add(new PropertyPathContext { Container = container, Property = property });
        }

        protected bool HasMixedValues<TValue>(IPropertyContainer container, IProperty property)
        {
            if (Targets.Count == 1)
            {
                return false;
            }

            // Check the actual mixed value.
            var visitor = new PropertyPathVisitor(new PropertyPathContext { Container = container, Property = property });

            var target = Targets[0];
            var targetValue = (TValue)property.GetObjectValue(container);
            target.Visit(visitor);
            var paths = visitor.GetPropertyPaths();
            if (paths.Count == 0)
            {
                //throw new Exception($"{UTinyConstants.ApplicationName}: Trying to get a property path for a property that is not part of the targets");
            }

            for(var i = 1; i < Targets.Count; ++i)
            {
                var t = Targets[i];
                var resolution = paths[0].Resolve(t);
                if (resolution.success == false)
                {
                    continue;
                }

                var value = resolution.value;
                if (ValuesAreDifferent(value, targetValue))
                {
                    return true;
                }
            }

            return false;
        }

        private void PropagateChanges()
        {
            if (Targets.Count == 1 || Changes.Count == 0)
            {
                return;
            }

            var visitor = new PropertyPathVisitor(Changes);
            var target = Targets[0];
            target.Visit(visitor);
            Changes.Clear();

            var paths = visitor.GetPropertyPaths();
            foreach (var path in paths)
            {
                // Get the value from the edited property.
                var resolution = path.Resolve(target);
                if (false == resolution.success)
                {
                    continue;
                }

                var value = resolution.value;

                // And propagate to the other properties.
                for (var i = 1; i < Targets.Count; ++i)
                {
                    var otherTarget = Targets[i];
                    var otherResolution = path.Resolve(otherTarget);
                    if (false == otherResolution.success)
                    {
                        continue;
                    }
                    
                    // TODO: not sure if this works in nested struct containers
                    otherResolution.property.SetObjectValue(otherResolution.container, value);
                }
            }
        }
        #endregion
    }
}
#endif // NET_4_6
