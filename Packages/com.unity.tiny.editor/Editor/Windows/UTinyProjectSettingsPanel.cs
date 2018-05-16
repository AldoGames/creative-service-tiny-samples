#if NET_4_6
using UnityEditor;
using UnityEngine;

namespace Unity.Tiny
{
    public class UTinyProjectSettingsPanel : UTinySettingsPanel
    {
        private readonly IRegistry m_Registry;
        private UTinyProject.Reference m_Project;
        private Vector2 m_ScrollPosition;
        private Vector2 m_PreviousCanvasSize = -Vector2.one;

        public UTinyProjectSettingsPanel(IRegistry registry, UTinyProject.Reference project)
        {
            m_Registry = registry;
            m_Project = project;
        }

        public override bool DrawLayout()
        {
            var project = m_Project.Dereference(m_Registry);
            m_ScrollPosition = EditorGUILayout.BeginScrollView(m_ScrollPosition);

            try
            {
                GUILayout.Label("Project Settings", UTinyStyles.Header1);
                
                AssetNameField(project);

                EditorGUILayout.Space();

                GUILayout.Label("HTML5", UTinyStyles.Header2);

                EditorGUI.BeginChangeCheck();
                project.Settings.CanvasAutoResize = EditorGUILayout.Toggle("Auto-Resize Canvas", project.Settings.CanvasAutoResize);
                EditorGUI.BeginDisabledGroup(project.Settings.CanvasAutoResize);
                project.Settings.CanvasWidth = EditorGUILayout.DelayedIntField("Canvas Width", project.Settings.CanvasWidth);
                project.Settings.CanvasHeight = EditorGUILayout.DelayedIntField("Canvas Height", project.Settings.CanvasHeight);
                Vector2 canvasSize = project.Settings.CanvasAutoResize ? Vector2.zero : new Vector2(project.Settings.CanvasWidth, project.Settings.CanvasHeight);
                if (EditorGUI.EndChangeCheck() || canvasSize != m_PreviousCanvasSize)
                {
                    if (project.Settings.CanvasAutoResize)
                    {
                        GameViewUtility.SetFreeAspect();
                    }
                    else
                    {
                        GameViewUtility.SetSize(project.Settings.CanvasWidth, project.Settings.CanvasHeight);
                    }

                    m_PreviousCanvasSize = canvasSize;
                }

                EditorGUI.EndDisabledGroup();

                EditorGUILayout.Space();

                var workspace = UTinyEditorApplication.EditorContext.Workspace;
                if (workspace.BuildConfiguration == UTinyBuildConfiguration.Release)
                {
                    project.Settings.SingleFileHtml = EditorGUILayout.Toggle(new GUIContent("Single File Output", "Embed JavaScript code in index.html. Release builds only."), project.Settings.SingleFileHtml);
                }
                else
                {
                    EditorGUI.BeginDisabledGroup(true);
                    EditorGUILayout.Toggle(new GUIContent("Single File Output", "Embed JavaScript code in index.html. Release builds only."), false);
                    EditorGUI.EndDisabledGroup();
                }

                project.Settings.IncludeWSClient = EditorGUILayout.Toggle(new GUIContent("Include WebSocket Client", "Include WebSocket client code in build. Required for live-link connection."), project.Settings.IncludeWSClient);
                project.Settings.IncludeWebPDecompressor = EditorGUILayout.Toggle(new GUIContent("Include WebP Decompressor", "Include WebP decompressor code in build. Required for browsers that does not support WebP image format."), project.Settings.IncludeWebPDecompressor);
                project.Settings.RunBabel = EditorGUILayout.Toggle(new GUIContent("Transpile to ECMAScript 5", "Transpile user code to ECMAScript 5 for greater compatibility across browsers."), project.Settings.RunBabel);

                EditorGUILayout.Space();

                project.Settings.LocalHTTPServerPort = EditorGUILayout.DelayedIntField(new GUIContent("Local HTTP Server Port", "Port used by the local HTTP server for hosting project."), project.Settings.LocalHTTPServerPort);
                project.Settings.MemorySize = EditorGUILayout.DelayedIntField(new GUIContent("Memory Size", "Total memory size pre-allocated for the entire project."), project.Settings.MemorySize);

                EditorGUILayout.Space();

                GUILayout.Label("Assets", UTinyStyles.Header2);
                
                project.Settings.EmbedAssets = EditorGUILayout.Toggle(new GUIContent("Embedded Assets", "Assets are embedded as base64 (this will increase asset size by approx 34%)."), project.Settings.EmbedAssets);

                EditorGUILayout.Space();
                
                TextureSettingsField(project.Settings.DefaultTextureSettings);

                GUILayout.Label("Module Settings", UTinyStyles.Header1);

                var module = project.Module.Dereference(project.Registry);
                
                module.Namespace = NamespaceField("Javascript Namespace", module.Namespace);
                module.Documentation.Summary = DescriptionField("Description", module.Documentation.Summary);

                EditorGUILayout.Space();
                
                return false;
            }
            finally
            {
                EditorGUILayout.EndScrollView();
            }
        }
    }
}
#endif // NET_4_6
