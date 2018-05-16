#if NET_4_6
using System;
using UnityEditor;
using UnityEngine;

namespace Unity.Tiny
{
    public class UTinyScriptEditorControl : IDrawable
    {
        private const int kMaxChars = 7000;
        private readonly IRegistry m_Registry;
        private Vector3 m_Scroll;
        
        public UTinyScript.Reference Script { private get; set; }
        
        public event Action<UTinyRegistryObjectBase> OnRenameEnded;

        public UTinyScriptEditorControl(IRegistry registry)
        {
            m_Registry = registry;
        }

        public bool DrawLayout()
        {
            var script = Script.Dereference(m_Registry);

            if (null == script || script.IsRuntimeIncluded)
            {
                return false;
            }

            using (var scroll = new GUILayout.ScrollViewScope(m_Scroll, GUILayout.ExpandWidth(true)))
            {
                m_Scroll = scroll.scrollPosition;

                EditorGUI.BeginChangeCheck();
                script.Name = EditorGUILayout.DelayedTextField("Name", script.Name);
                if (EditorGUI.EndChangeCheck())
                {
                    OnRenameEnded?.Invoke(script);
                }

                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.PrefixLabel("Description");
                    script.Documentation.Summary = EditorGUILayout.TextArea(script.Documentation.Summary, GUILayout.Height(50));
                }
                
                EditorGUILayout.Space();
                
                script.TextAsset = (TextAsset) EditorGUILayout.ObjectField("Source", script.TextAsset, typeof(TextAsset), false);

                EditorGUILayout.Space();

                if (null != script.TextAsset)
                {
                    using (new GUIEnabledScope(false))
                    {
                        var text = script.TextAsset.text;
                        if (text.Length > kMaxChars)
                        {
                            text = text.Substring(0, kMaxChars) + "...\n\n<...etc...>";
                        }
                        GUILayout.TextArea(text);
                    }
                }

                EditorGUILayout.Space();
            }

            return false;
        }
    }
}
#endif // NET_4_6
