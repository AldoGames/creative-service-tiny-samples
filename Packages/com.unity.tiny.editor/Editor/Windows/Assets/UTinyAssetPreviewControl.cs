#if NET_4_6
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Unity.Properties;
using UnityEditor;
using UnityEngine;

namespace Unity.Tiny
{
    public class UTinyAssetPreviewControl : IDrawable
    {
        #region Fields
        
        private readonly IRegistry m_Registry;
        private UTinyProject.Reference m_Project;
        private UTinyModule.Reference m_MainModule;

        private readonly List<UTinyAssetInfo> m_Assets;
        private string m_Path;
        private string m_FileType;
        private long m_FileSize;
        private Object m_PreviewObject;
        private Texture2D m_Preview;

        private bool m_ObjectTypesMatch;

        #endregion

        public UTinyAssetPreviewControl(IRegistry registry, UTinyProject.Reference project, UTinyModule.Reference mainModule)
        {
            m_Registry = registry;
            m_Project = project;
            m_MainModule = mainModule;
            m_Assets = new List<UTinyAssetInfo>();
        }
        
        public void SetAssets(IEnumerable<UTinyAssetInfo> assets)
        {
            m_Assets.Clear();
            m_Assets.AddRange(assets);
            SetAssetPreview(m_Assets.Count == 1 ? m_Assets[0].Object : null);
            m_Path = m_Assets.Count == 1 ? m_Assets[0].AssetPath : "<multiple>";

            if (m_Assets.Count > 0)
            {
                var type = m_Assets[0].Object.GetType();
                m_ObjectTypesMatch = m_Assets.All(a => a.Object.GetType() == type);
            }
            else
            {
                m_ObjectTypesMatch = false;
            }
        }
        
        private void SetAssetPreview(Object @object)
        {
            if (null != @object && @object)
            {
                m_PreviewObject = @object;
                m_Preview = AssetPreview.GetAssetPreview(m_PreviewObject);

                if (m_PreviewObject is Texture2D)
                {
                    var texture = m_PreviewObject as Texture2D;
                        
                    var path = Application.dataPath.Substring(0, Application.dataPath.LastIndexOf('/')) + "/" + AssetDatabase.GetAssetPath(texture);
                    var fileInfo = new FileInfo(path);
                        
                    m_FileType = fileInfo.Extension.Substring(1, fileInfo.Extension.Length - 1).ToUpper();
                    m_FileSize = fileInfo.Length;
                }
            }
            else
            {
                m_PreviewObject = null;
                m_Preview = null;
                m_FileType = string.Empty;
                m_FileSize = 0;
            }
        }
        
        #region Drawing

        public bool DrawLayout()
        {
            if (m_Assets.Count <= 0)
            {
                return false;
            }
            
            using (new GUILayout.VerticalScope(UTinyStyles.ListBackground))
            {
                using (new GUILayout.HorizontalScope())
                {
                    GUILayout.FlexibleSpace();
                    GUILayout.Label(m_Path, GUILayout.ExpandWidth(true));
                    GUILayout.FlexibleSpace();
                }
                
                GUILayout.FlexibleSpace();

                using (new GUILayout.HorizontalScope())
                {
                    GUILayout.FlexibleSpace();

                    if (null != m_Preview)
                    {
                        var rect = GUILayoutUtility.GetRect(
                            GUIContent.none,
                            GUIStyle.none,
                            GUILayout.Width(m_Preview.width),
                            GUILayout.Height(m_Preview.height));

                        GUI.DrawTexture(rect, m_Preview, ScaleMode.ScaleAndCrop);
                    }

                    GUILayout.FlexibleSpace();
                }
                
                GUILayout.FlexibleSpace();

                using (new GUILayout.HorizontalScope())
                {
                    GUILayout.FlexibleSpace();
                    
                    if (null != m_PreviewObject && m_PreviewObject is Texture2D)
                    {
                        var texture = (Texture2D) m_PreviewObject;
                        using (new GUILayout.HorizontalScope())
                        {
                            GUILayout.Label(
                                $"{texture.width}x{texture.height} {m_FileType} {BytesToString(m_FileSize)}",
                                EditorStyles.boldLabel);
                        }
                    }
                    
                    GUILayout.FlexibleSpace();
                }
            }

            // @TODO Can we use a visitor to draw these?
            if (m_ObjectTypesMatch)
            {
                var type = m_Assets[0].Object.GetType();
                
                if (type == typeof(Texture2D))
                {
                    TextureSettingsOverride(m_Assets[0].Object as Texture2D);
                }
                else if (type == typeof(AudioClip))
                {
                    AudioClipSettingsOverride(m_Assets[0].Object as AudioClip);
                }
                else
                {
                    GenericAssetSettingsOverride(m_Assets[0].Object);
                }
            }
            else
            {
                GenericAssetSettingsOverride(m_Assets[0].Object);
            }
            
            return false;
        }

        private void CreateAssetExportInfo<TSettings>(IEnumerable<UTinyAssetInfo> assetInfos) 
            where TSettings : UTinyAssetExportSettings, ICopyable<TSettings>, new()
        {
            var project = m_Project.Dereference(m_Registry);
            var module = m_MainModule.Dereference(m_Registry);
            
            foreach (var assetInfo in assetInfos)
            {
                CreateAssetExportInfo<TSettings>(project, module, assetInfo);
            }
        }
        
        private static void CreateAssetExportInfo<TSettings>(UTinyProject project, UTinyModule module, UTinyAssetInfo assetInfo) 
            where TSettings : UTinyAssetExportSettings, ICopyable<TSettings>, new()
        {
            var asset = module.GetAsset(assetInfo.Object) ?? module.AddAsset(assetInfo.Object);
            var settings = asset.CreateExportSettings<TSettings>();
            settings.CopyFrom(UTinyUtility.GetAssetExportSettings(project, assetInfo.Object) as TSettings);
        }

        private void ClearAssetExportSettings(IEnumerable<UTinyAssetInfo> assetInfos)
        {
            var module = m_MainModule.Dereference(m_Registry);

            foreach (var assetInfo in assetInfos)
            {
                var asset = module.GetAsset(assetInfo.Object);
                asset?.ClearExportSettings();;
            }
        }

        private void TextureSettingsOverride(Texture2D texture)
        {
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                var module = m_MainModule.Dereference(m_Registry);
                var asset = module.GetAsset(texture);
                var settings = asset?.ExportSettings as UTinyTextureSettings;
                
                EditorGUI.BeginChangeCheck();
                
                var @override = EditorGUILayout.ToggleLeft("Override for HTML5", null != settings);

                if (EditorGUI.EndChangeCheck())
                {
                    if (null == settings && @override)
                    {
                        // Create an asset for each selected object
                        CreateAssetExportInfo<UTinyTextureSettings>(m_Assets);
                        
                        // Pull the newly created settings
                        settings = asset?.ExportSettings as UTinyTextureSettings;
                    }
                    else if (null != asset && !@override)
                    {
                        // Clear assets for each selected object
                        ClearAssetExportSettings(m_Assets);
                        settings = null;
                    }
                }

                // @TODO Visitor for generic drawing
                using (new GUIEnabledScope(@override))
                {
                    EditorGUILayout.Space();

                    if (null != settings)
                    {
                        EditorGUI.BeginChangeCheck();
                        
                        settings.FormatType = (TextureFormatType) EditorGUILayout.EnumPopup("Texture Format", settings.FormatType);
                        
                        if (EditorGUI.EndChangeCheck())
                        {
                            SetPropertyValue(module, m_Assets, "FormatType", settings.FormatType);
                        }
                        
                        if (settings.FormatType == TextureFormatType.JPG)
                        {
                            EditorGUI.BeginChangeCheck();

                            settings.JpgCompressionQuality = EditorGUILayout.IntSlider("Compression Quality", settings.JpgCompressionQuality, 1, 100);
                            
                            if (EditorGUI.EndChangeCheck())
                            {
                                SetPropertyValue(module, m_Assets, "JpgCompressionQuality", settings.JpgCompressionQuality);
                            }
                        }
                        else if (settings.FormatType == TextureFormatType.WebP)
                        {
                            EditorGUI.BeginChangeCheck();

                            settings.WebPCompressionQuality = EditorGUILayout.IntSlider("Compression Quality", settings.WebPCompressionQuality, 1, 100);
                            
                            if (EditorGUI.EndChangeCheck())
                            {
                                SetPropertyValue(module, m_Assets, "WebPCompressionQuality", settings.WebPCompressionQuality);
                            }
                        }
                        
                        EditorGUI.BeginChangeCheck();

                        settings.Embedded = EditorGUILayout.Toggle("Embedded", settings.Embedded);
                    
                        if (EditorGUI.EndChangeCheck())
                        {
                            SetPropertyValue(module, m_Assets, "Embedded", settings.Embedded);
                        }
                    }
                }
            }
        }

        private void AudioClipSettingsOverride(AudioClip clip)
        {
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                var module = m_MainModule.Dereference(m_Registry);
                
                var asset = module.GetAsset(clip);
                var settings = asset?.ExportSettings as UTinyAudioClipSettings;
                
                EditorGUI.BeginChangeCheck();

                var @override = EditorGUILayout.ToggleLeft("Override for HTML5", null != settings);

                if (EditorGUI.EndChangeCheck())
                {
                    if (null == settings && @override)
                    {
                        // Create an asset for each selected object
                        CreateAssetExportInfo<UTinyAudioClipSettings>(m_Assets);
                        
                        // Pull the newly created settings
                        settings = asset?.ExportSettings as UTinyAudioClipSettings;
                    }
                    else if (null != asset && !@override)
                    {
                        // Clear assets for each selected object
                        ClearAssetExportSettings(m_Assets);
                        settings = null;
                    }
                }
                
                using (new GUIEnabledScope(@override))
                {
                    EditorGUILayout.Space();

                    if (null != settings)
                    {
                        EditorGUI.BeginChangeCheck();

                        settings.Embedded = EditorGUILayout.Toggle("Embedded", settings.Embedded);
                        
                        if (EditorGUI.EndChangeCheck())
                        {
                            SetPropertyValue(module, m_Assets, "Embedded", settings.Embedded);
                        }
                    }
                }
            }
        }

        private void GenericAssetSettingsOverride(Object @object)
        {
            using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
            {
                var project = m_Project.Dereference(m_Registry);
                var module = m_MainModule.Dereference(m_Registry);
                
                var asset = module.GetAsset(@object);
                var settings = asset?.ExportSettings;
                
                EditorGUI.BeginChangeCheck();

                var @override = EditorGUILayout.ToggleLeft("Override for HTML5", null != settings);

                if (EditorGUI.EndChangeCheck())
                {
                    if (null == settings && @override)
                    {
                        // Create an asset for each selected object
                        foreach (var assetInfo in m_Assets)
                        {
                            if (assetInfo.Object is Texture2D)
                            {
                                CreateAssetExportInfo<UTinyTextureSettings>(project, module, assetInfo);
                            }
                            else if (assetInfo.Object is AudioClip)
                            {
                                CreateAssetExportInfo<UTinyAudioClipSettings>(project, module, assetInfo);
                            }
                            else
                            {
                                CreateAssetExportInfo<UTinyGenericAssetExportSettings>(project, module, assetInfo);
                            }
                        }
                        
                        // Pull the newly created settings
                        settings = asset?.ExportSettings;
                    }
                    else if (null != asset && !@override)
                    {
                        // Clear assets for each selected object
                        ClearAssetExportSettings(m_Assets);
                        settings = null;
                    }
                }
                
                using (new GUIEnabledScope(@override))
                {
                    EditorGUILayout.Space();

                    if (null != settings)
                    {
                        EditorGUI.BeginChangeCheck();

                        settings.Embedded = EditorGUILayout.Toggle("Embedded", settings.Embedded);
                        
                        if (EditorGUI.EndChangeCheck())
                        {
                            SetPropertyValue(module, m_Assets, "Embedded", settings.Embedded);
                        }
                    }
                }
            }
        }
        
        private static void SetPropertyValue(UTinyModule module, IEnumerable<UTinyAssetInfo> assetInfos, string property, object value)
        {
            foreach (var assetInfo in assetInfos)
            {
                var a = module.GetAsset(assetInfo.Object);
                var settings = a?.ExportSettings as IPropertyContainer;
                settings?.PropertyBag.FindProperty(property)?.SetObjectValue(settings, value);
            }
        }
        
        private static string BytesToString(long byteCount)
        {
            string[] suffix = { "B", "KB", "MB", "GB", "TB", "PB", "EB" }; // Longs run out around EB
            
            if (byteCount == 0)
            {
                return "0" + suffix[0];
            }
            
            var bytes = System.Math.Abs(byteCount);
            var place = System.Convert.ToInt32(System.Math.Floor(System.Math.Log(bytes, 1024)));
            var num = System.Math.Round(bytes / System.Math.Pow(1024, place), 1);
            return System.Math.Sign(byteCount) * num + suffix[place];
        }
        
        #endregion
    }
}
#endif // NET_4_6
