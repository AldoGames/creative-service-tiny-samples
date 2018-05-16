#if NET_4_6
using UnityEditor;
using UnityEngine;

namespace Unity.Tiny
{
    public class UTinyModuleSettingsPanel : UTinySettingsPanel
    {
        private readonly IRegistry m_Registry;
        private UTinyModule.Reference m_Module;
        private Vector2 m_ScrollPosition;

        public UTinyModuleSettingsPanel(IRegistry registry, UTinyModule.Reference module)
        {
            m_Registry = registry;
            m_Module = module;
        }
        
        public override bool DrawLayout()
        {
            var module = m_Module.Dereference(m_Registry);
            m_ScrollPosition = EditorGUILayout.BeginScrollView(m_ScrollPosition);
            
            try
            {
                GUILayout.Label("Module Settings", UTinyStyles.Header1);
                
                AssetNameField(module);
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
