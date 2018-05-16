#if NET_4_6
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEditor;

using UnityEditor.Experimental.UIElements;
using Object = UnityEngine.Object;

namespace Unity.Tiny
{
    using Unity.Properties;

    public class UTinyInspector : EditorWindow, IHasCustomMenu
    {
        #region Static
        private static readonly List<UTinyInspector> s_ActiveWindows = new List<UTinyInspector>();

        public static bool IsBeingInspected(IPropertyContainer container)
        {
            foreach (var window in s_ActiveWindows)
            {
                if (window.Backend.Targets.Contains(container))
                {
                    return true;
                }
            }
            return false;
        }
        public static void RepaintAll()
        {
            foreach (var window in s_ActiveWindows)
            {
                window.Repaint();
            }
        }

        [InitializeOnLoadMethod]
        private static void Register()
        {
            UTinyEditorApplication.OnLoadProject += AddUndoRedoCallbacks;
        }
        #endregion
        

        /// <summary>
        /// These are the currently inspected targets. They might be of different types.
        /// </summary>
        private List<IPropertyContainer> m_Targets;

        [SerializeField]
        private InspectMode m_Mode = InspectMode.Normal;

        [SerializeField]
        private InspectorBackendType m_BackendType = InspectorBackendType.IMGUI;

        [SerializeField]
        private IInspectorBackend m_Backend;

        public IInspectorBackend Backend
        {
            get
            {
                if (null == m_Backend)
                {
                    SwitchToBackend(m_BackendType, true);
                }
                return m_Backend;
            }
        }

        [MenuItem(UTinyConstants.MenuItemNames.InspectorWindow)]
        public static void CreateNewInspector()
        {
            var window = CreateInstance<UTinyInspector>();
            window.Show();
            window.ShowTab();
        }

        #region Unity
        private void OnEnable()
        {
            s_ActiveWindows.Add(this);
            titleContent.text = $"{UTinyConstants.ApplicationName} Inspector";
            minSize = new Vector2(275.0f, 50.0f);
            autoRepaintOnSceneChange = true;
            OnSelectionChange();
            Selection.selectionChanged += OnSelectionChange;
            SwitchToBackend(m_BackendType, true);
        }

        private void OnDisable()
        {
            m_Targets.Clear();
            Selection.selectionChanged -= OnSelectionChange;
            s_ActiveWindows.Remove(this);
        }

        private void OnGUI()
        {
            if (null == Backend)
            {
                EditorGUILayout.LabelField($"Backend not supported for backend type '{m_BackendType.ToString()}'");
                return;
            }

            try
            {
                Backend.Targets = m_Targets;
                Backend.Mode = m_Mode;

                Backend.OnGUI();

                m_Targets = Backend.Targets;
            }
            catch (ExitGUIException)
            {
                throw;
            }
            catch (Exception e)
            {
                TinyEditorAnalytics.SendExceptionOnce("Inspector.OnGUI", e);
                throw;
            }
        }

        private static bool Valid(Object obj)
        {
            return obj is GameObject || obj is IPropertyContainer;
        }

        private void OnSelectionChange()
        {
            var validObjects = Selection.instanceIDs
                .Select(EditorUtility.InstanceIDToObject)
                .Where(Valid);

            m_Targets = validObjects
                .Where(obj => obj is GameObject)
                .Cast<GameObject>()
                .Select(go => go.GetComponent<UTinyEntityView>())
                .Where(view => null!= view && view && null != view.Registry)
                .Select(view => view.EntityRef.Dereference(view.Registry))
                .Cast<IPropertyContainer>()
                .Concat(
                    validObjects
                        .Where(obj => obj is IPropertyContainer)
                        .Cast<IPropertyContainer>())
                .ToList();

            Repaint();
        }
        #endregion

        private IInspectorBackend GetBackend(InspectorBackendType type)
        {
            var root = this.GetRootVisualContainer();
            root.Clear();

            IInspectorBackend backend = null;
            switch (type)
            {
                case InspectorBackendType.IMGUI:
                    backend = new IMGUIBackend(this);
                    break;
                case InspectorBackendType.UIElements:
                    backend = new UIElementsBackend(this);
                    break;
                default:
                    throw new ArgumentException("Unknown InspectorBackendType", nameof(type));
            }
            backend.Mode = m_Mode;
            backend.Build();
            return backend;
        }

        public void AddItemsToMenu(GenericMenu menu)
        {
            menu.AddItem(new GUIContent("Backend/IMGUI"), 
                m_BackendType == InspectorBackendType.IMGUI,
                () => SwitchToBackend(InspectorBackendType.IMGUI));

#if UTINY_INTERNAL
            // Not really supported yet.
            menu.AddItem(new GUIContent("Backend/UI Elements"),
                m_BackendType == InspectorBackendType.UIElements,
                () => SwitchToBackend(InspectorBackendType.UIElements));
#endif

            menu.AddItem(new GUIContent("Mode/Normal"),
                m_Mode == InspectMode.Normal,
                () => SwitchToMode(InspectMode.Normal));

            menu.AddItem(new GUIContent("Mode/Debug"),
                m_Mode == InspectMode.Debug, 
                () => SwitchToMode(InspectMode.Debug));

#if UTINY_INTERNAL
            menu.AddItem(new GUIContent("Mode/Debug Internal"),
                m_Mode == InspectMode.DebugInternal,
                () => SwitchToMode(InspectMode.DebugInternal));
#endif
            menu.AddSeparator("");
            if (null != m_Backend)
            {
                menu.AddItem(new GUIContent("Lock"),
                    m_Backend.Locked,
                    () => m_Backend.Locked = !m_Backend.Locked);
            }
        }

        public void SwitchToMode(InspectMode mode, bool force = false)
        {
            if (mode == m_Mode && !force)
            {
                return;
            }
            m_Mode = mode;
        }

        public void SwitchToBackend(InspectorBackendType type, bool force = false)
        {
            if (type == m_BackendType && !force)
            {
                return;
            }
            m_BackendType = type;
            m_Backend = GetBackend(m_BackendType);
        }


        private static void AddUndoRedoCallbacks(UTinyProject project)
        {
            if (null == project)
            {
                return;
            }

            UTinyEditorApplication.Undo.OnUndoPerformed += RepaintAll;
            UTinyEditorApplication.Undo.OnRedoPerformed += RepaintAll;
        }

        public static void FocusOrCreateWindow()
        {
            if (s_ActiveWindows.Count == 0)
            {
                CreateNewInspector();
            }
            else
            {
                EditorWindow.FocusWindowIfItsOpen<UTinyInspector>();
            }
        }
    }
}
#endif // NET_4_6
