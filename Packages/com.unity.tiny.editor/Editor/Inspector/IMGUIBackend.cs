#if NET_4_6
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using Unity.Properties;

namespace Unity.Tiny
{
    public sealed class IMGUIBackend : InspectorBackend
    {
        #region Static
        private static readonly Dictionary<Type, GenericIMGUIVisitor> s_TypeToVisitor = new Dictionary<Type, GenericIMGUIVisitor>();
        private static readonly GenericIMGUIVisitor m_GenericVisitor = new GenericIMGUIVisitor();

        // @TODO: Find a better way to do this..
        [InitializeOnLoadMethod]
        public static void RegisterVisitorForType()
        {
            s_TypeToVisitor[typeof(UTinyEntity)] = new UTinyEntityIMGUIVisitor();
            s_TypeToVisitor[typeof(UTinyTypeViewer)] = new UTinyTypeIMGUIVisitor();
            s_TypeToVisitor[typeof(UTinyConfigurationViewer)] = new UTinyConfigurationIMGUIVisitor();
        }
        #endregion

        #region Fields
        private Vector2 m_BodyScroll = Vector2.zero;
        #endregion

        public IMGUIBackend(UTinyInspector inspector) : base(inspector) { }

        #region Implementation
        private GenericIMGUIVisitor Visitor
        {
            get
            {
                GenericIMGUIVisitor visitor;
                if (!s_TypeToVisitor.TryGetValue(Targets[0].GetType(), out visitor))
                {
                    visitor = m_GenericVisitor;
                }
                return visitor;
            }
        }

        protected override void ValidateTargets()
        {
            var toRemove = Pooling.ListPool<IPropertyContainer>.Get();
            try
            {
                foreach (var target in Targets)
                {
                    if (!Visitor.ValidateTarget(target))
                    {
                        toRemove.Add(target);
                    }
                }
            }
            finally
            {
                Targets.RemoveAll(t => toRemove.Contains(t));
                Pooling.ListPool<IPropertyContainer>.Release(toRemove);
            }
        }

        protected override void Inspect()
        {
            var visitor = Visitor;
            visitor.Mode = Mode;
            visitor.Targets = Targets;

            visitor.Prepare();
            visitor.Header();
            m_BodyScroll = EditorGUILayout.BeginScrollView(m_BodyScroll);
            try
            {
                visitor.Visit();
            }
            finally
            {
                EditorGUILayout.EndScrollView();
            }
            visitor.Footer();
            visitor.Complete();
        }

        protected override void ShowDifferentTypes(Dictionary<Type, int> types)
        {
            EditorGUILayout.LabelField("Narrow the selection:");
            foreach (var kvp in types)
            {
                EditorGUILayout.BeginHorizontal();
                try
                {
                    // I would use EditorGUI.indent, but it is ignored with Buttons..
                    GUILayout.Space(20);
                    if (GUILayout.Button($"{kvp.Value} {kvp.Key.Name}", GUI.skin.label))
                    {
                        RestrictToType(kvp.Key);
                        return;
                    }
                }
                finally
                {
                    EditorGUILayout.EndHorizontal();
                }
            }
        }
        #endregion
    }
}
#endif // NET_4_6
