#if NET_4_6
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace Unity.Tiny
{
    public static class TinyPreferences
    {
        private static string DefaultNodePath()
        {
#if UNITY_EDITOR_WIN
            return Path.GetFullPath(Path.Combine(EditorApplication.applicationContentsPath, "Tools/nodejs"));
#else
            return Path.GetFullPath(Path.Combine(EditorApplication.applicationContentsPath, "Tools/nodejs/bin"));
#endif
        }
        
        private static string DefaultMonoPath()
        {
            return Path.GetFullPath(Path.Combine(EditorApplication.applicationContentsPath, "MonoBleedingEdge/bin"));
        }

        public static string Default7zPath()
        {
            var path = Path.GetFullPath(Path.Combine(EditorApplication.applicationContentsPath, "Tools/7z"));
#if UNITY_EDITOR_WIN
            path += ".exe";
#else
            path += "a";
#endif
            return path;
        }

        private class ProgramPath
        {
            private bool m_Initialized;
            private string m_Name, m_Key, m_Value, m_DefaultValue, m_VersionCommand, m_Version, m_DefaultVersion;

            public string Name => m_Name;

            public string Value
            {
                get { Initialize(); return m_Value ?? m_DefaultValue; }
                set
                {
                    Initialize();
                    
                    if (string.Equals(value, m_Value, StringComparison.Ordinal))
                    {
                        return;
                    }
                    
                    if (string.IsNullOrEmpty(value) || 
                        string.Equals(value, m_DefaultValue, StringComparison.Ordinal))
                    {
                        Reset();
                        return;
                    }

                    if (TryGetVersion(value, m_VersionCommand, out m_Version))
                    {
                        m_Value = value;
                        EditorPrefs.SetString(m_Key, m_Value);
                    }
                    else
                    {
                        Reset();
                    }
                }
            }

            public string Version => m_Version ?? m_DefaultVersion;

            public ProgramPath(string name, string key, string versionCommand, string defaultValue)
            {
                m_Name = name;
                m_Key = key;
                m_DefaultValue = defaultValue;
                m_VersionCommand = versionCommand;
            }

            private void Initialize()
            {
                if (m_Initialized)
                    return;
                
                if (!TryGetVersion(m_DefaultValue, m_VersionCommand, out m_DefaultVersion))
                {
                    Debug.LogError($"Tiny: Could not find '{Name}' at default location '{m_DefaultValue}'");
                }
                m_Value = EditorPrefs.GetString(m_Key);
                if (m_Value == string.Empty || m_Value == m_DefaultValue)
                {
                    Reset();
                }

                if (m_Value != null)
                {
                    TryGetVersion(m_Value, m_VersionCommand, out m_Version);
                }
                m_Initialized = true;
            }

            public void Reset()
            {
                if (m_Value != null)
                {
                    m_Value = null;
                    m_Version = null;
                    EditorPrefs.DeleteKey(m_Key);
                }
            }

            public void Draw()
            {
                Value = EditorGUILayout.DelayedTextField($"{Name} Path", Value);
                EditorGUILayout.SelectableLabel(Version);
            }

            private static bool TryGetVersion(string directory, string command, out string version)
            {
                version = null;
                
                if (string.IsNullOrEmpty(command))
                    return false;
                
                if (!Directory.Exists(directory))
                    return false;
                
                var output = UTinyShell.RunInShell(command, new ShellProcessArgs()
                {
                    ExtraPaths = directory.AsEnumerable(),
                    ThrowOnError = false
                });

                if (!output.Succeeded)
                    return false;

                version = output.CommandOutput;
                return true;
            }
        }

        private static readonly ProgramPath s_NodeDir = new ProgramPath(
            "Node.js",
            "TINY_NODE_DIR",
            "node --version",
            DefaultNodePath());
        
        private static readonly ProgramPath s_MonoDir = new ProgramPath(
            "Mono",
            "TINY_MONO_DIR",
            "mono --version", // very verbose
            DefaultMonoPath());
        
        public static string NodeDirectory => s_NodeDir.Value;
        public static string MonoDirectory => s_MonoDir.Value;
        
        [PreferenceItem("Tiny Unity")]
        private static void OnGUI()
        {
            s_NodeDir.Draw();
            s_MonoDir.Draw();
        }
    }
}
#endif
