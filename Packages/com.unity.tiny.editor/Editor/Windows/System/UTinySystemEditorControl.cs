#if NET_4_6
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;
using Unity.Tiny.Filters;

namespace Unity.Tiny
{
    public class UTinySystemEditorControl : IDrawable
    {

        #region Fields
        private const int kMaxChars = 7000;
        private readonly IRegistry m_Registry;
        private UTinyModule.Reference m_MainModule;

        private Vector3 m_Scroll;

        private readonly ReorderableList m_ExecuteBeforeList;
        private readonly ReorderableList m_ExecuteAfterList;
        private readonly ReorderableList m_ComponentList;

        private readonly List<UTinySystem.Reference> m_AvailableSystems = new List<UTinySystem.Reference>();
        private readonly List<UTinyType.Reference> m_AvailableComponentTypes = new List<UTinyType.Reference>();

        #endregion

        #region Properties

        public UTinySystem.Reference System { get; set; }

        public event Action<UTinyRegistryObjectBase> OnRenameEnded;

        #endregion

        #region Public Methods

        public UTinySystemEditorControl(IRegistry registry, UTinyModule.Reference mainModule)
        {
            m_Registry = registry;
            m_MainModule = mainModule;

            m_ExecuteAfterList = new ReorderableList(new ArrayList(), typeof(UTinySystem.Reference))
            {
                displayAdd = true,
                displayRemove = true
            };

            m_ExecuteAfterList.drawHeaderCallback += HandleExecuteAfterListDrawHeader;
            m_ExecuteAfterList.drawElementCallback += HandleExecuteAfterListDrawElement;
            m_ExecuteAfterList.onAddDropdownCallback += HandleExecuteAfterListAddDropdown;
            m_ExecuteAfterList.onRemoveCallback += HandleExecuteAfterListRemove;
            m_ExecuteAfterList.onReorderCallback += RebuildExecuteAfterReferences;

            m_ExecuteBeforeList = new ReorderableList(new ArrayList(), typeof(UTinySystem.Reference))
            {
                displayAdd = true,
                displayRemove = true
            };

            m_ExecuteBeforeList.drawHeaderCallback += HandleExecuteBeforeListDrawHeader;
            m_ExecuteBeforeList.drawElementCallback += HandleExecuteBeforeListDrawElement;
            m_ExecuteBeforeList.onAddDropdownCallback += HandleExecuteBeforeListAddDropdown;
            m_ExecuteBeforeList.onRemoveCallback += HandleExecuteBeforeListRemove;
            m_ExecuteBeforeList.onReorderCallback += RebuildExecuteBeforeReferences;

            m_ComponentList = new ReorderableList(new ArrayList(), typeof(UTinyType.Reference))
            {
                displayAdd = true,
                displayRemove = true
            };

            m_ComponentList.drawHeaderCallback += HandleComponentListDrawHeader;
            m_ComponentList.drawElementCallback += HandleComponentListDrawElement;
            m_ComponentList.onAddDropdownCallback += HandleComponentListAddDropdown;
            m_ComponentList.onRemoveCallback += HandleComponentListRemove;
            m_ComponentList.onReorderCallback += RebuildComponentReferenceList;
        }
        
        private void RebuildExecuteAfterReferences(ReorderableList list)
        {
            var system = System.Dereference(m_Registry);
            
            system.ClearExecuteAfterReferences();
            foreach (var r in (IList<UTinySystem.Reference>) list.list)
            {
                system.AddExecuteAfterReference(r);
            }
        }
        
        private void RebuildExecuteBeforeReferences(ReorderableList list)
        {
            var system = System.Dereference(m_Registry);

            system.ClearExecuteBeforeReferences();
            foreach (var r in (IList<UTinySystem.Reference>) list.list)
            {
                system.AddExecuteBeforeReference(r);
            }
        }

        private void RebuildComponentReferenceList(ReorderableList list)
        {
            var system = System.Dereference(m_Registry);

            system.ClearComponentReferences();
            foreach (var r in (IList<UTinyType.Reference>) list.list)
            {
                system.AddComponentReference(r);
            }
        }

        public bool DrawLayout()
        {
            var module = m_MainModule.Dereference(m_Registry);
            var system = System.Dereference(m_Registry);

            if (null == system || system.IsRuntimeIncluded)
            {
                return false;
            }
            
            m_AvailableSystems.Clear();
            m_AvailableSystems.AddRange(module.EnumerateDependencies().SystemRefs());

            m_AvailableComponentTypes.Clear();
            m_AvailableComponentTypes.AddRange(module.EnumerateDependencies().ComponentTypeRefs().Select(r => (UTinyType.Reference) r.Dereference(m_Registry)));

            EditorGUI.BeginChangeCheck();

            using (var scroll = new GUILayout.ScrollViewScope(m_Scroll, GUILayout.ExpandWidth(true)))
            {
                m_Scroll = scroll.scrollPosition;

                EditorGUI.BeginChangeCheck();
                system.Name = EditorGUILayout.DelayedTextField("Name", system.Name);
                if (EditorGUI.EndChangeCheck())
                {
                    OnRenameEnded?.Invoke(system);
                }
        
                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.PrefixLabel("Description");
                    system.Documentation.Summary = EditorGUILayout.TextArea(system.Documentation.Summary, GUILayout.Height(50));
                }

                m_ExecuteAfterList.list = new List<UTinySystem.Reference>(system.ExecuteAfter);
                m_ExecuteAfterList.DoLayoutList();

                m_ExecuteBeforeList.list = new List<UTinySystem.Reference>(system.ExecuteBefore);
                m_ExecuteBeforeList.DoLayoutList();

                m_ComponentList.list = new List<UTinyType.Reference>(system.Components);
                m_ComponentList.DoLayoutList();

                system.External = EditorGUILayout.Toggle(new GUIContent("External", "Use this to define systems externally"), system.External);

                if (system.External)
                {
                    EditorGUILayout.HelpBox($"This system is assumed to be defined in any included script with the following signature", MessageType.Info);
                    var name = UTinyBuildPipeline.GetJsTypeName(system);
                    EditorGUILayout.SelectableLabel($"{name}.update = function(s,w) {{ /* ... */ }}", "TextArea");
                }
                else
                {
                    system.IncludeIterator = EditorGUILayout.Toggle("Include Iterator", system.IncludeIterator);

                    EditorGUILayout.Space();

                    using (new GUIEnabledScope(false))
                    {
                        var systemPrefix = UTinyBuildPipeline.GenerateSystemPrefix(system);
                        if (system.IncludeIterator)
                        {
                            systemPrefix += UTinyBuildPipeline.GenerateSystemIteratorPrefix(system);
                        }
                    
                        EditorGUILayout.TextArea(systemPrefix);
                    }

                    EditorGUILayout.Space();
                
                    system.TextAsset = (TextAsset) EditorGUILayout.ObjectField("Source", system.TextAsset, typeof(TextAsset), false);

                    EditorGUILayout.Space();

                    if (null != system.TextAsset)
                    {
                        using (new GUIEnabledScope(false))
                        {
                            var text = system.TextAsset.text;
                            if (text.Length > kMaxChars)
                            {
                                text = text.Substring(0, kMaxChars) + "...\n\n<...etc...>";
                            }
                            GUILayout.TextArea(text);
                        }
                    }

                    EditorGUILayout.Space();

                    using (new GUIEnabledScope(false))
                    {
                        var systemSuffix = UTinyBuildPipeline.GenerateSystemSuffix(system);
                        if (system.IncludeIterator)
                        {
                            systemSuffix = UTinyBuildPipeline.GenerateSystemIteratorSuffix(system) + systemSuffix;
                        }
                        EditorGUILayout.TextArea(systemSuffix);
                    }
                }
            }

            return EditorGUI.EndChangeCheck();
        }

        #endregion

        #region Event Handlers

        private static void HandleExecuteAfterListDrawHeader(Rect rect)
        {
            EditorGUI.LabelField(rect, "Execute After");
        }

        private static void HandleExecuteBeforeListDrawHeader(Rect rect)
        {
            EditorGUI.LabelField(rect, "Execute Before");
        }

        private void HandleExecuteAfterListDrawElement(Rect rect, int index, bool isActive, bool isFocused)
        {
            DrawSystemListElement(rect, (UTinySystem.Reference) m_ExecuteAfterList.list[index]);
        }

        private void HandleExecuteBeforeListDrawElement(Rect rect, int index, bool isActive, bool isFocused)
        {
            DrawSystemListElement(rect, (UTinySystem.Reference) m_ExecuteBeforeList.list[index]);
        }
        
        private void DrawSystemListElement(Rect rect, UTinySystem.Reference reference)
        {
            var system = reference.Dereference(m_Registry);
            using (new GUIColorScope(null == system ? Color.red : !m_AvailableSystems.Contains(reference) ? Color.yellow : Color.white))
            {
                GUI.DrawTexture(new Rect(rect.x, rect.y, 20, 20), UTinyIcons.System);
                EditorGUI.LabelField(new Rect(rect.x + 20, rect.y, rect.width, rect.height), system?.Name ?? reference.Name, EditorStyles.label);
            }
        }

        private void HandleExecuteAfterListAddDropdown(Rect buttonRect, ReorderableList list)
        {
            var module = m_MainModule.Dereference(m_Registry);
            var system = System.Dereference(m_Registry);

            var systemRefs = new List<UTinySystem.Reference>();
            
            systemRefs.AddRange(module.GetSystemExecutionOrder());
            systemRefs.Remove(System);
            foreach (var type in system.ExecuteAfter)
            {
                systemRefs.Remove(type);
            }
            
            var systems = systemRefs.Deref(m_Registry);

            AddSystemWindow.Show(buttonRect, m_Registry, systems, sys => system.AddExecuteAfterReference(sys));
        }

        private void HandleExecuteAfterListRemove(ReorderableList list)
        {
            var system = System.Dereference(m_Registry);

            system.ClearExecuteAfterReferences();
            var items = ((IList<UTinySystem.Reference>) list.list);
            for (var i = 0; i < items.Count; i++)
            {
                if (i == list.index)
                {
                    continue;
                }
                system.AddExecuteAfterReference(items[i]);
            }
        }

        private void HandleExecuteBeforeListAddDropdown(Rect buttonRect, ReorderableList list)
        {
            var module = m_MainModule.Dereference(m_Registry);
            var system = System.Dereference(m_Registry);

            var systemRefs = new List<UTinySystem.Reference>();

            systemRefs.AddRange(module.GetSystemExecutionOrder());
            systemRefs.Remove(System);
            foreach (var type in system.ExecuteBefore)
            {
                systemRefs.Remove(type);
            }

            var systems = systemRefs.Deref(m_Registry);

            AddSystemWindow.Show(buttonRect, m_Registry, systems, sys => system.AddExecuteBeforeReference(sys));
        }

        private void HandleExecuteBeforeListRemove(ReorderableList list)
        {
            var system = System.Dereference(m_Registry);

            system.ClearExecuteBeforeReferences();
            var items = ((IList<UTinySystem.Reference>) list.list);
            for (var i = 0; i < items.Count; i++)
            {
                if (i == list.index)
                {
                    continue;
                }
                system.AddExecuteBeforeReference(items[i]);
            }
        }


        private static void HandleComponentListDrawHeader(Rect rect)
        {
            EditorGUI.LabelField(rect, "Component Types");
        }

        private void HandleComponentListDrawElement(Rect rect, int index, bool isActive, bool isFocused)
        {
            var reference = (UTinyType.Reference) m_ComponentList.list[index];
            var component = reference.Dereference(m_Registry);

            using (new GUIColorScope(null == component ? Color.red : !m_AvailableComponentTypes.Contains(reference) ? Color.yellow : Color.white))
            {
                GUI.DrawTexture(new Rect(rect.x, rect.y, 20, 20), UTinyIcons.Component);
                EditorGUI.LabelField(new Rect(rect.x + 20, rect.y, rect.width, rect.height), component?.Name ?? reference.Name, EditorStyles.label);
            }
        }

        private void HandleComponentListAddDropdown(Rect buttonRect, ReorderableList list)
        {
            var system = System.Dereference(m_Registry);
            var types = new List<UTinyType.Reference>();
            
            types.AddRange(m_AvailableComponentTypes);

            foreach (var type in system.Components)
            {
                types.Remove(type);
            }

            AddSystemComponentWindow.Show(buttonRect, m_Registry, types.Deref(m_Registry).ToList(), t => system.AddComponentReference(t));
        }

        private void HandleComponentListRemove(ReorderableList list)
        {
            var system = System.Dereference(m_Registry);

            system.ClearComponentReferences();
            var items = ((IList<UTinyType.Reference>) list.list);
            for (var i = 0; i < items.Count; i++)
            {
                if (i == list.index)
                {
                    continue;
                }
                system.AddComponentReference(items[i]);
            }
        }

        #endregion
    }
}
#endif // NET_4_6
