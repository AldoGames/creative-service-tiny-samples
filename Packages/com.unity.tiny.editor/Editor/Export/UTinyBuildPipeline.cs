#if NET_4_6
using System;
using System.IO;
using System.Linq;
using UnityEngine.Assertions;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;
using Unity.Tiny.Filters;

namespace Unity.Tiny
{
    public enum UTinyPlatform
    {
        HTML5
    }
    
    public interface IUTinyBuilder
    {
        void Build(UTinyBuildOptions options, UTinyBuildResults results);
    }

    public class UTinyBuildOptions
    {
        public UTinyProject Project { get; set; }
        public UTinyPlatform Platform { get; set; }
        public UTinyBuildConfiguration Configuration { get; set; }
        public DirectoryInfo Destination { get; set; }
    }

    public class UTinyBuildResults
    {
        #region Fields

        private readonly UTinyBuildReport m_BuildReport = new UTinyBuildReport(UTinyBuildReport.ProjectNode);

        #endregion

        #region Properties

        public DirectoryInfo OutputFolder { get; set; }

        public DirectoryInfo BinaryFolder { get; set; }

        public string PreviewFile { get; set; }

        public UTinyBuildReport.TreeNode BuildReport
        {
            get { return m_BuildReport.Root; }
        }

        #endregion
    }

    internal class UTinyAssetPostProcessor : AssetPostprocessor
    {
        private static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths)
        {
            if (!InternalEditorUtility.inBatchMode)
            {
                UTinyBuildPipeline.InstallRuntimeOnLoad();
            }
        }
    }

    public static class UTinyBuildPipeline
    {
        private const string ProgressBarTitle = UTinyConstants.ApplicationName + " Build";

        public static readonly FileInfo RuntimeDefsAssemblyPath =
            new FileInfo(Path.Combine(Application.dataPath, "../UTiny/Dist/build-js-release/runtime/RuntimeFull-Defs.dll"));

        [InitializeOnLoadMethod]
        internal static void InstallRuntimeOnLoad()
        {
#if !UTINY_INTERNAL
            InstallRuntime(false, true);
#endif
        }

        private static void InstallRuntime(bool force, bool silent)
        {
            try
            {
                var installLocation = new DirectoryInfo("UTiny");
                var versionFile = new FileInfo(Path.Combine(installLocation.FullName, "lastUpdate.txt"));
                var sourcePackage = new FileInfo(UTinyConstants.PackagePath + "tiny-runtime-dist.zip");
                var shouldUpdate = sourcePackage.Exists && (!versionFile.Exists || versionFile.LastWriteTimeUtc < sourcePackage.LastWriteTimeUtc);

                if (!force && !shouldUpdate)
                {
                    if (!silent)
                    {
                        Debug.Log("Tiny: Runtime is already up to date");
                    }
                    return;
                }

                if (!sourcePackage.Exists)
                {
                    if (!silent)
                    {
                        Debug.LogError($"Tiny: could not find {sourcePackage.FullName}");
                    }
                    return;
                }
                
                if (installLocation.Exists)
                {
                    EditorUtility.DisplayProgressBar($"{UTinyConstants.ApplicationName} Runtime", "Removing old runtime...", 0.0f);
                    UTinyBuildUtilities.PurgeDirectory(installLocation);
                }
                EditorUtility.DisplayProgressBar($"{UTinyConstants.ApplicationName} Runtime", "Installing new runtime...", 0.5f);
                UTinyBuildUtilities.UnzipFile(sourcePackage.FullName, installLocation.Parent);
                File.WriteAllText(versionFile.FullName, $"{sourcePackage.FullName} install time: {DateTime.UtcNow.ToString()}");
                
#if UNITY_EDITOR_OSX
                // TODO: figure out why UnzipFile does not preserve executable bits in some cases
                // chmod +x any native executables here
                UTinyBuildUtilities.RunInShell("chmod +x cwebp moz-cjpeg pngcrush",
                    new ShellProcessArgs()
                    {
                        WorkingDirectory = new DirectoryInfo(GetToolDirectory("images/osx")),
                        ExtraPaths = "/bin".AsEnumerable(), // adding this folder just in case, but should be already in $PATH
                        ThrowOnError = false
                    });
#endif
                
                Debug.Log($"Installed {UTinyConstants.ApplicationName} runtime at: {installLocation.FullName}");
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }
        }

#if !UTINY_INTERNAL
        [MenuItem(UTinyConstants.ApplicationName + "/Import Samples...")]
        internal static void InstallSamples()
        {
            InstallSamples(true);
        }
        
        [MenuItem(UTinyConstants.ApplicationName + "/Update Runtime")]
        private static void InstallRuntimeMenuItem()
        {
            InstallRuntime(true, false);
        }
#endif

        internal static void InstallSamples(bool interactive)
        {
            var packagePath = Path.GetFullPath(UTinyConstants.PackagePath + "tiny-samples.unitypackage");
            AssetDatabase.ImportPackage(packagePath, interactive);
        }

        public static void Export(UTinyProject project)
        {
            var workspace = UTinyEditorApplication.EditorContext.Workspace;
            
            var results = Build(new UTinyBuildOptions()
            {
                Project = project,
                Configuration = workspace.BuildConfiguration,
                Platform = UTinyPlatform.HTML5,
                Destination = new DirectoryInfo("UTinyExport/" + project.Name)
            });

            if (workspace.Preview)
            {
                try
                {
                    EditorUtility.DisplayProgressBar($"{UTinyConstants.ApplicationName} Preview", "Starting local http server...", 0.0f);
                    UTinyServer.Instance.ReloadOrOpen(results.PreviewFile);
                }
                finally
                {
                    EditorUtility.ClearProgressBar();
                }
            }
        }

        public static UTinyBuildResults Build(UTinyBuildOptions options)
        {
            if (options?.Project == null || options.Destination == null)
            {
                throw new ArgumentException($"{UTinyConstants.ApplicationName}: invalid build options provided", nameof(options));
            }

            var buildStart = DateTime.Now;
            
            var results = new UTinyBuildResults();
            IUTinyBuilder builder = null;

            switch (options.Platform)
            {
                case UTinyPlatform.HTML5:
                    builder = new UTinyHTML5Builder();
                    break;
                default:
                    throw new ArgumentException($"{UTinyConstants.ApplicationName}: build platform not supported", nameof(options));
            }

            try
            {
                EditorUtility.DisplayProgressBar(ProgressBarTitle, "Build started for " + options.Platform.ToString(),
                    0.0f);

                var destFolder = options.Destination;
                destFolder.Create();

                // BUILD = <DEST>/PLATFORM/CONFIG
                var buildFolder = new DirectoryInfo(GetBuildDirectory(options.Project, options.Platform, options.Configuration));

                results.OutputFolder = buildFolder;

                UTinyBuildUtilities.PurgeDirectory(buildFolder);
                buildFolder.Create();

                options.Destination = results.BinaryFolder = buildFolder;

                var idlFile = new FileInfo(Path.Combine(buildFolder.FullName, "generated.cs"));
                UTinyIDLGenerator.GenerateIDL(options.Project, idlFile);

                var distFolder = GetRuntimeDistFolder();

                var bindGem = new FileInfo(Path.Combine(
                    distFolder.FullName, "bindgem/BindGem/bin/Release/BindGem.exe"));

                var exeName = "\"" + bindGem.FullName + "\"";

                // always call bindgem with mono for consistency
                exeName = "mono " + exeName;

                // reference the core runtime file
                var bindReferences = $"-r \"{RuntimeDefsAssemblyPath}\"";

                UTinyBuildUtilities.RunInShell(
                    $"{exeName} -j {bindReferences} -o bind-generated {idlFile.Name}",
                    new ShellProcessArgs()
                    {
                        WorkingDirectory = buildFolder,
                        ExtraPaths = TinyPreferences.MonoDirectory.AsEnumerable()
                    });

                // @TODO Perform a full refresh before building

                builder.Build(options, results);

                results.BuildReport.Update();

                Debug.Log($"{UTinyConstants.ApplicationName} project generated at: {results.BinaryFolder.FullName}");

                TinyEditorAnalytics.SendBuildEvent(options.Project, results, DateTime.Now - buildStart);
                return results;
            }
            catch (Exception ex)
            {
                TinyEditorAnalytics.SendException("BuildPipeline.Build", ex);
                throw;
            }
            finally
            {
                EditorUtility.ClearProgressBar();
                UTinyEditorUtility.RepaintAllWindows();
            }
        }

        public static string GetRelativePath(FileInfo file)
        {
            var root = new DirectoryInfo(".");
            var index = file.FullName.IndexOf(root.FullName);
            var relative = index >= 0 ? file.FullName.Substring(root.FullName.Length + 1) : file.FullName;
            return relative.Replace("\\", "/");
        }

        public static string GetToolDirectory(string toolName)
        {
            if (string.IsNullOrEmpty(toolName))
            {
                throw new ArgumentException("tool");
            }
            return Path.Combine("UTiny", "Tools", toolName);
        }

        public static string GetBuildDirectory(UTinyProject project, UTinyPlatform platform, UTinyBuildConfiguration configuration)
        {
            return Path.Combine("UTinyExport", project.Name, platform.ToString(), configuration.ToString());
        }

        public static DirectoryInfo GetRuntimeDistFolder()
        {
            return new DirectoryInfo(Path.Combine(Application.dataPath, "../UTiny/Dist"));
        }
        
        public static bool IncludesModule(UTinyProject project, string moduleName)
        {
            return project.Module.Dereference(project.Registry).EnumerateDependencies().WithName(moduleName).Any();
        }

        public const string BuiltInPhysicsModule = "UTiny.Physics2D";

        public static string GetJsRuntimeVariant(UTinyBuildOptions options)
        {
            return options.Configuration == UTinyBuildConfiguration.Release ? "GeminiRuntime" : IncludesModule(options.Project, BuiltInPhysicsModule) ? "RuntimeFull" : "RuntimeStripped";
        }

        public static string GetJsTypeName(UTinyRegistryObjectBase @object)
        {
            Assert.IsNotNull(@object);
            return GetJsTypeName(UTinyUtility.GetModules(@object).FirstOrDefault(), @object);
        }

        public static string GetJsTypeName(UTinyModule module, UTinyRegistryObjectBase @object)
        {
            var name = @object.Name;
            
            if (!string.IsNullOrEmpty(module?.Namespace))
            {
                name = module.Namespace + "." + name;
            }

            var type = @object as UTinyType;
            if (type != null)
            {
                switch (type.TypeCode)
                {
                    case UTinyTypeCode.Unknown:
                        break;
                    case UTinyTypeCode.Int8:
                    case UTinyTypeCode.Int16:
                    case UTinyTypeCode.Int32:
                    case UTinyTypeCode.Int64:
                    case UTinyTypeCode.UInt8:
                    case UTinyTypeCode.UInt16:
                    case UTinyTypeCode.UInt32:
                    case UTinyTypeCode.UInt64:
                    case UTinyTypeCode.Float32:
                    case UTinyTypeCode.Float64:
                    case UTinyTypeCode.Boolean:
                    case UTinyTypeCode.Char:
                    case UTinyTypeCode.String:
                        return name.ToLower();
                    case UTinyTypeCode.EntityReference:
                        // @TODO remove the magic value
                        return "ut.Entity";
                    case UTinyTypeCode.Configuration:
                    case UTinyTypeCode.Component:
                    case UTinyTypeCode.Struct:
                    case UTinyTypeCode.Enum:
                    case UTinyTypeCode.UnityObject:
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
            
            return name;
        }

        public static string GenerateSystemPrefix(UTinySystem system, UTinyPlatform forPlatform = UTinyPlatform.HTML5)
        {
            if (forPlatform != UTinyPlatform.HTML5)
            {
                throw new ArgumentException("Platform not supported");
            }
            return "function (sched, world) {";
        }

        public static string GenerateSystemIteratorPrefix(UTinySystem system, UTinyPlatform forPlatform = UTinyPlatform.HTML5)
        {
            if (forPlatform != UTinyPlatform.HTML5)
            {
                throw new ArgumentException("Platform not supported");
            }

            return system.Components.Count == 0 
                ? "world.forEachEntity([], function (entity) {" 
                : $"world.forEachEntity([{string.Join(", ", system.Components.Select(p => GetJsTypeName(p.Dereference(system.Registry))).ToArray())}], \nfunction (entity, {string.Join(", ", system.Components.Select(p => p.Name.ToLower()).ToArray())}) {{";
        }

        public static string GenerateSystemIteratorSuffix(UTinySystem system, UTinyPlatform forPlatform = UTinyPlatform.HTML5)
        {
            if (forPlatform != UTinyPlatform.HTML5)
            {
                throw new ArgumentException("Platform not supported");
            }
            return "});" + Environment.NewLine;
        }

        public static string GenerateSystemSuffix(UTinySystem system, UTinyPlatform forPlatform = UTinyPlatform.HTML5)
        {
            if (forPlatform != UTinyPlatform.HTML5)
            {
                throw new ArgumentException("Platform not supported");
            }
            return "}";
        }
    }
}
#endif // NET_4_6
