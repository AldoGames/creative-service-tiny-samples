#if NET_4_6
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

#pragma warning disable 649
#pragma warning disable 414

namespace Unity.Tiny
{
    [InitializeOnLoad]
    public static class TinyPackageUtility
    {
        private static readonly PackageInfo s_Pkg;
        private static readonly bool s_Registered;
        private const string k_EmbeddedPath = UTinyConstants.PackagePath + "package.json";

        static TinyPackageUtility()
        {
            if (s_Registered)
            {
                return;
            }
            try
            {
                var realPath = Path.GetFullPath(k_EmbeddedPath);

                var packageJson = File.ReadAllText(realPath);

                var package = new PackageJson();
                EditorJsonUtility.FromJsonOverwrite(packageJson, package);

                // TODO: would be nice to get this information from Unity.PackageManager
                s_Pkg = new PackageInfo()
                {
                    version = package.version,

                    preview =
                        package.version.StartsWith("0.") ||
                        package.version.Contains("preview") ||
                        package.version.Contains("experimental"),

                    embedded = !realPath.Contains(UTinyConstants.PackageName + "@")
                };
            }
            catch (Exception e)
            {
                TraceError(e.ToString());

                s_Pkg = new PackageInfo()
                {
                    version = "error",
                    preview = false,
                    embedded = false
                };
            }

            s_Registered = true;
        }

        public static bool IsTinyPackageEmbedded => s_Pkg.embedded;
        public static string EmbeddedPath => k_EmbeddedPath;
        public static PackageInfo Package => s_Pkg;


        [Serializable]
        private class PackageJson
        {
            public string name;
            public string displayName;
            public string version;
            public string unity;
            public string description;
            public string[] keywords;
            public Dictionary<string, string> dependencies;
        }

        private static void TraceError(string message)
        {
            message = "Tiny: " + message;
#if UTINY_INTERNAL
            Debug.LogError(message);
#else
            Console.WriteLine(message);
            #endif
        }

        [Serializable]
        public struct PackageInfo
        {
            public string version;
            public bool preview;
            public bool embedded;

            public static PackageInfo Default => s_Pkg;
        }
    }
}
#endif // NET_4_6
