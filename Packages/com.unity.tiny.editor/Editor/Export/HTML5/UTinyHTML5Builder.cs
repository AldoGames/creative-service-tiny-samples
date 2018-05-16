#if NET_4_6
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Unity.Properties;
using UnityEditor;
using UnityEngine;
using Unity.Tiny.Filters;

namespace Unity.Tiny
{
    internal sealed class UTinyHTML5Builder : IUTinyBuilder
    {
        public const string KEntityGroupNamespace = "entities";
        
        private const string KSettingsFileName = "settings.js";
        private const string KRuntimeFileName = "runtime.js";
        private const string KBindingsFileName = "bindings.js";
        private const string KAssetsFileName = "assets.js";
        private const string KEntityGroupsFileName = "entities.js";
        private const string KCodeFileName = "code.js";
        private const string KSystemsFileName = "systems.js";
        private const string KMainFileName = "main.js";
        private const string KWebSocketClientFileName = "wsclient.js";
        private const string KWebPDecompressorFileName = "libwebp.js";
        private const string KHtmlFileName = "index.html";
        
        /// <summary>
        /// Builds the provided project for the HTML5 platform.
        /// </summary>
        public void Build(UTinyBuildOptions options, UTinyBuildResults results)
        {
            // Final output directory
            results.BinaryFolder = new DirectoryInfo(Path.Combine(options.Destination.FullName, "bin"));
            results.BinaryFolder.Create();
            
            // Package and export all data
            PackageSettings(options, results);
            PackageRuntime(options, results);
            PackageAssets(options, results);
            
            // Generate and write all applicaton code
            GenerateEntityGroups(options, results);
            GenerateBindings(options, results);
            GenerateSystems(options, results);
            GenerateScripts(options, results);
            GenerateMain(options, results);

            // Generate additional appended code
            GenerateWebSocketClient(options, results);
            GenerateWebPDecompressor(options, results);

            // Generate final HTML file
            GenerateHTML(options, results);
            results.PreviewFile = results.BinaryFolder.FullName;

            // Generate build report
            GenerateBuildReport(results);
        }
        
        /// <summary>
        /// Packages settings to `settings.js`
        /// </summary>
        private static void PackageSettings(UTinyBuildOptions options, UTinyBuildResults results)
        {
            var writer = new UTinyCodeWriter(CodeStyle.JavaScript);
            
            var file = new FileInfo(Path.Combine(results.BinaryFolder.FullName, KSettingsFileName));
            
            var settings = options.Project.Settings;
            
            writer.Line($"var Module = {{TOTAL_MEMORY: {settings.MemorySize * 1024 * 1024}}};")
                  .Line();
            
            // <HACK>
            // Workaround for issue `UTINY-1091`
            // Systems will not force binding generation to create namespace objects
            var namespaces = new HashSet<string>();
            foreach (var m in options.Project.Module.Dereference(options.Project.Registry).EnumerateDependencies())
            {
                // If we don't have types our module namespace is not generated automatically
                if (!m.Types.Any())
                {
                    var parts = m.Namespace.Split('.');
                    var name = parts[0];

                    namespaces.Add(name);
                
                    for (var i = 1; i < parts.Length; i++)
                    {
                        name = $"{name}.{parts[i]}";
                        namespaces.Add(name);
                    }
                }
            }

            if (namespaces.Count > 0)
            {
                writer.Line("/*");
                writer.Line(" * Workaround for issue UTINY-1091");
                writer.Line(" */");
            }

            foreach (var n in namespaces)
            {
                writer.Line(!n.Contains('.') ? $"var {n} = {n} || {{}}" : $"{n} = {n} || {{}}");
            }
            // <HACK>

            if (writer.Length <= 0)
            {
                // No settings, nothing to write
                return;
            }

            PrependGeneratedHeader(writer, options.Project.Name);
            File.WriteAllText(file.FullName, writer.ToString(), Encoding.UTF8);
        }
        
        /// <summary>
        /// Packages the runtime to `runtime.js`
        /// </summary>
        private static void PackageRuntime(UTinyBuildOptions options, UTinyBuildResults results)
        {
            string buildFolderName;
            switch (options.Configuration)
            {
                case UTinyBuildConfiguration.Debug:
                    buildFolderName = "build-js-debug";
                    break;

                case UTinyBuildConfiguration.Development:
                case UTinyBuildConfiguration.Release:
                    buildFolderName = "build-js-release";
                    break;

                default:
                    throw new ArgumentOutOfRangeException();
            }

            var runtimeVariant = UTinyBuildPipeline.GetJsRuntimeVariant(options);
            var distFolder = UTinyBuildPipeline.GetRuntimeDistFolder();
            var runtimeFiles = new DirectoryInfo(Path.Combine(distFolder.FullName, buildFolderName + "/runtime")).GetFiles(runtimeVariant + "*", SearchOption.TopDirectoryOnly);

            var reportRuntime = results.BuildReport.AddChild(UTinyBuildReport.RuntimeNode);
            foreach (var runtimeFile in runtimeFiles)
            {
                if (runtimeFile.Name.EndsWith(".js.symbols") || runtimeFile.Name.EndsWith(".js.map") || runtimeFile.Name.EndsWith(".dll"))
                {
                    continue;
                }
                var destPath = Path.Combine(results.BinaryFolder.FullName, $"runtime{runtimeFile.Extension}");

                if (runtimeFile.Name == "GeminiRuntime.js")
                {
                    var dependencies = options.Project.Module.Dereference(options.Project.Registry).EnumerateDependencies();
                    var regex = new System.Text.RegularExpressions.Regex(@"\/\*if\(([\s\S]*?)\)\*\/([\s\S]*?)\/\*endif\(([\s\S]*?)\)\*\/");
                    var runtime = File.ReadAllText(runtimeFile.FullName);
                    runtime = regex.Replace(runtime, match => match.Groups[match.Groups[1].Value.Split('|').Any(module => dependencies.WithName("UTiny." + module).Any()) ? 2 : 3].Value);
                    File.WriteAllText(destPath, runtime);
                    reportRuntime.AddChild(new FileInfo(destPath));
                }
                else
                {
                    reportRuntime.AddChild(runtimeFile.CopyTo(destPath));
                }
            }
        }
        
        /// <summary>
        /// Packages assets to `assets.js` or `Assets/*.*`
        /// </summary>
        private static void PackageAssets(UTinyBuildOptions options, UTinyBuildResults results)
        {
            var buildFolder = options.Destination;
            var binFolder = results.BinaryFolder;
            
            // Export assets to the build directory
            var buildAssetsFolder = new DirectoryInfo(Path.Combine(buildFolder.FullName, "Assets"));
            buildAssetsFolder.Create();
            var export = UTinyAssetExporter.Export(options.Project, buildAssetsFolder);

            // copy assets to bin AND/OR encode assets to 'assets.js'
            var binAssetsFolder = new DirectoryInfo(Path.Combine(binFolder.FullName, "Assets"));
            binAssetsFolder.Create();
            
            var assetsFile = new FileInfo(Path.Combine(binFolder.FullName, KAssetsFileName));
            
            var writer = new UTinyCodeWriter();
            
            PrependGeneratedHeader(writer, options.Project.Name);

            var reportAssets = results.BuildReport.AddChild(UTinyBuildReport.AssetsNode);
            var reportJavaScript = reportAssets.AddChild("JavaScript");

            using (var jsdoc = new UTinyJsdoc.Writer(writer))
            {
                jsdoc.Type("object");
                jsdoc.Desc("Map containing URLs for all assets.  If assets are included as base64 blobs, these will be data URLs.");
                jsdoc.Line("@example var assetUrl = UT_ASSETS[\"MyCustomAsset\"]");
            }

            long totalBase64Size = 0;
            using (writer.Scope("var UT_ASSETS ="))
            {
                var i = 0;
                foreach (var info in export)
                {
                    var reportAsset = reportAssets.AddChild(info.AssetInfo.AssetPath, 0, info.AssetInfo.Object);

                    var settings = UTinyUtility.GetAssetExportSettings(options.Project, info.AssetInfo.Object);
                    if (settings.Embedded)
                    {
                        foreach (var file in info.Exported)
                        {
                            var buffer = File.ReadAllBytes(file.FullName);
                            var base64 = Convert.ToBase64String(buffer);
                            var fileExtension = Path.GetExtension(file.FullName).ToLower();
                            
                            string mimeType;
                            switch (fileExtension)
                            {
                                case ".png":
                                    mimeType = "image/png";
                                    break;
                                case ".jpg":
                                case ".jpeg":
                                    mimeType = "image/jpeg";
                                    break;
                                case ".webp":
                                    mimeType = "image/webp";
                                    break;
                                case ".mp3":
                                    mimeType = "audio/mpeg";
                                    break;
                                case ".wav":
                                    mimeType = "audio/wav";
                                    break;
                                case ".json":
                                    mimeType = "application/json";
                                    break;
                                case ".ttf":
                                    mimeType = "font/truetype";
                                    break;
                                default:
                                    Debug.LogWarningFormat("Asset {0} has unknown extension, included as text/plain in assets", file);
                                    mimeType = "text/plain";
                                    break;
                            }

                            var comma = i != 0 ? "," : "";
                            writer.Line($"{comma}\"{Path.GetFileNameWithoutExtension(file.Name)}\": \"data:{mimeType};base64,{base64}\"");
                            i++;

                            reportAsset.AddChild(UTinyBuildPipeline.GetRelativePath(file), Encoding.ASCII.GetBytes(base64), info.AssetInfo.Object);
                            totalBase64Size += base64.Length;

                            file.Delete();
                        }
                    }
                    else
                    {
                        foreach (var file in info.Exported)
                        {
                            var comma = i != 0 ? "," : "";
                            writer.Line($"{comma}\"{Path.GetFileNameWithoutExtension(file.Name)}\": \"Assets/{file.Name}\"");
                            i++;

                            reportAsset.AddChild(file, info.AssetInfo.Object);
                        }
                    }
                }
            }

            writer.Line();

            writer.WriteRaw("var UT_ASSETS_SETUP = ");
            {
                var registry = new UTinyRegistry();
                UTinyPersistence.LoadAllModules(registry);
                var entityGroup = UTinyAssetEntityGroupGenerator.Generate(registry, options.Project);
                EntityGroupSetupVisitor.WriteEntityGroupSetupFunction(writer, options.Project, entityGroup, false, false);
            }
            
            // Write `assets.js`
            File.WriteAllText(assetsFile.FullName, writer.ToString());

            reportJavaScript.Item.Size = assetsFile.Length - totalBase64Size;

            // Remaining assets are binplaced
            foreach (var info in export)
            {
                foreach (var file in info.Exported)
                {
                    if (!file.Exists)
                    {
                        // this asset has been packaged already
                        continue;
                    }

                    file.MoveTo(Path.Combine(binAssetsFolder.FullName, file.Name));
                }
            }
            
            // Clean up the build directory
            buildAssetsFolder.Delete(true);

            // if we have no standalone assets, cleanup
            if (binAssetsFolder.GetFiles().Length <= 0)
            {
                binAssetsFolder.Delete();
            }
        }

        /// <summary>
        /// Writes components, structs and enums `bindings.js`
        /// </summary>
        private static void GenerateBindings(UTinyBuildOptions options, UTinyBuildResults results)
        {
            var file = new FileInfo(Path.Combine(results.BinaryFolder.FullName, KBindingsFileName));
            
            // @NOTE `bind-generated.js` is the exported name from the `BindGen.exe` application
            File.Copy(Path.Combine(results.OutputFolder.FullName, "bind-generated.js"), file.FullName, true);
            results.BuildReport.GetOrAddChild("Code").AddChild(file);
        }
        
        /// <summary>
        /// Packages entity group objects to `entities.js`
        /// 
        /// Since we don't have a scene format, groups are written as setup functions
        /// </summary>
        private static void GenerateEntityGroups(UTinyBuildOptions options, UTinyBuildResults results)
        {
            var writer = new UTinyCodeWriter(CodeStyle.JavaScript);
            var report = results.BuildReport.GetOrAddChild(UTinyBuildReport.CodeNode).AddChild();
            
            PrependGeneratedHeader(writer, options.Project.Name);
            
            // @NOTE Namespaces are generated through through `BindGen.exe`
            // e.g. `{ENTITY_GROUPS}.{PROJECT_NAMESPACE}.{GROUP_NAME}` will already exist as a component
            
            using (var visitor = new EntityGroupSetupVisitor {Writer = writer, Report = report})
            {
                options.Project.Visit(visitor);
            }
            
            var file = new FileInfo(Path.Combine(results.BinaryFolder.FullName, KEntityGroupsFileName));
            File.WriteAllText(file.FullName, writer.ToString(), Encoding.UTF8);
            report.Reset(file);
        }
        
        /// <summary>
        /// Writes user code to `code.js`
        /// 
        /// Any free standing code written by users is written to this file
        /// </summary>
        private static void GenerateScripts(UTinyBuildOptions options, UTinyBuildResults results)
        {
            var project = options.Project;
            var registry = project.Registry;
            var module = project.Module.Dereference(registry);
            var report = results.BuildReport.GetOrAddChild(UTinyBuildReport.CodeNode).AddChild();

            var writer = new UTinyCodeWriter(CodeStyle.JavaScript);
            
            PrependGeneratedHeader(writer, options.Project.Name);
            
            foreach (var script in module.EnumerateDependencies().Scripts())
            {
                if (null == script.TextAsset)
                {
                    continue;
                }

                var reportSystemPos = writer.Length;
                writer.WriteRaw(script.TextAsset.text)
                      .Line();
                report.AddChild(AssetDatabase.GetAssetPath(script.TextAsset), Encoding.ASCII.GetBytes(writer.Substring(reportSystemPos)), script.TextAsset);
            }
            
            var file = new FileInfo(Path.Combine(results.BinaryFolder.FullName, KCodeFileName));
            File.WriteAllText(file.FullName, writer.ToString(), Encoding.UTF8);
            report.Reset(file);
        }
        
        /// <summary>
        /// Packages system objects to `systems.js`
        /// 
        /// All systems and system dependencies are written to this file
        /// </summary>
        private static void GenerateSystems(UTinyBuildOptions options, UTinyBuildResults results)
        {
            var project = options.Project;
            var report = results.BuildReport.GetOrAddChild(UTinyBuildReport.CodeNode).AddChild();
            
            var file = new FileInfo(Path.Combine(results.BinaryFolder.FullName, KSystemsFileName));
            var writer = new UTinyCodeWriter(CodeStyle.JavaScript);
            
            PrependGeneratedHeader(writer, options.Project.Name);
            
            foreach (var reference in project.Module.Dereference(project.Registry).GetSystemExecutionOrder()) 
            {
                var system = reference.Dereference(project.Registry);
                
                if (system == null) 
                {
                    Debug.LogWarning($"Can't resolve system named '{reference.Name}' with ID {reference.Id} -- ignoring, you should delete this system");
                    continue;
                }

                if (system.External)
                {
                    continue;
                }
             
                // Fetch the module this system belongs to
                var systemModule = UTinyUtility.GetModules(system).FirstOrDefault();
                
                if (system.IsRuntimeIncluded)
                {
                    continue;
                }
                
                var reportSystemPos = writer.Length;

                UTinyJsdoc.WriteSystem(writer, system);

                writer.Line($"{UTinyBuildPipeline.GetJsTypeName(systemModule, system)}.update = {UTinyBuildPipeline.GenerateSystemPrefix(system)}");
                writer.IncrementIndent();
                
                if (system.IncludeIterator)
                {
                    writer.Line(UTinyBuildPipeline.GenerateSystemIteratorPrefix(system));
                    writer.IncrementIndent();
                }

                var text = system.TextAsset ? system.TextAsset.text : string.Empty;

                if (!string.IsNullOrEmpty(text))
                {
                    var lines = text.Split('\n');
                    
                    foreach (var line in lines)
                    {
                        writer.Line(line);
                    }
                }

                if (system.IncludeIterator)
                {
                    writer.DecrementIndent();
                    writer.Line("});");
                }

                writer.DecrementIndent();
                writer.Line(UTinyBuildPipeline.GenerateSystemSuffix(system));
                
                report.AddChild(AssetDatabase.GetAssetPath(system.TextAsset), Encoding.ASCII.GetBytes(writer.Substring(reportSystemPos)), system.TextAsset);
            }
            
            File.WriteAllText(file.FullName, writer.ToString(), Encoding.UTF8);
            report.Reset(file);
        }

        /// <summary>
        /// Generates entry point for the applicaton `main.js`
        /// This script will contain the system scheduling, window setup and initial group loading
        /// </summary>
        private static void GenerateMain(UTinyBuildOptions options, UTinyBuildResults results)
        {
			var project = options.Project;
            var registry = project.Registry;
            var module = project.Module.Dereference(registry);
            
            var file = new FileInfo(Path.Combine(results.BinaryFolder.FullName, KMainFileName));

            var writer = new UTinyCodeWriter();
            
            PrependGeneratedHeader(writer, options.Project.Name);

            var distVersionFile = new FileInfo("UTiny/version.txt");
            var versionString = "internal";
            if (distVersionFile.Exists)
            {
                versionString = File.ReadAllText(distVersionFile.FullName);
            }
            writer.LineFormat("console.log('runtime version: {0}');", versionString)
                  .Line();

            var namespaces = new Dictionary<string, string>();
            foreach (var m in module.EnumerateDependencies())
            {
                if (string.IsNullOrEmpty(m.Namespace))
                {
                    continue;
                }

                if (m.IsRuntimeIncluded) {
                    writer.Line($"ut.importModule({m.Namespace});");
                    continue;
                }

                string content;
                namespaces.TryGetValue(m.Namespace, out content);
                content += m.Documentation.Summary;
                namespaces[m.Namespace] = content;
            }

            UTinyJsdoc.WriteType(writer, "ut.World", "Singleton world instance");
            writer.Line("var world;");
            using (writer.Scope("ut.main = function()"))
            {
                // Create and setup the world
                writer
                    .Line("world = new ut.World();")
                    .Line("var options = WorldSetup(world);");
                    
                // Write configurations
                var context = new EntityGroupSetupVisitor.VisitorContext
                {
                    Project = project,
                    Module = project.Module.Dereference(project.Registry),
                    Registry = project.Registry,
                    Writer = writer,
                    EntityIndexMap = null
                };
                
                var configuration = project.Configuration.Dereference(registry);
                foreach (var component in configuration.Components)
                {
                    var moduleContainingType = registry.FindAllByType<UTinyModule>().First(m => m.Types.Contains(component.Type));
                    if (!module.EnumerateDependencies().Contains(moduleContainingType))
                    {
                        // Silently ignore components if the module is not included.
                        // This is by design to preserve user data
                        continue;
                    }
                    
                    var type = component.Type.Dereference(component.Registry);
                    var index = ++context.ComponentIndex;
                    writer.Line($"var c{index} = world.config({UTinyBuildPipeline.GetJsTypeName(type)});");
                    component.Properties.Visit(new EntityGroupSetupVisitor.ComponentVisitor
                    {
                        VisitorContext = context,
                        Path = $"c{index}",
                    });
                }

                // Setup the scheduler
                writer.Line("var scheduler = world.scheduler();");
                
                // Schedule all systems
                var systems = project.Module.Dereference(project.Registry).GetSystemExecutionOrder();
                foreach (var reference in systems) 
                {
                    var system = reference.Dereference(project.Registry);
                
                    if (system == null) 
                    {
                        Debug.LogWarning($"Can't resolve system named '{reference.Name}' with ID {reference.Id} -- ignoring, you should delete this system");
                        continue;
                    }
                    
                    var systemModule = UTinyUtility.GetModules(system).FirstOrDefault();
                    var systemName = UTinyBuildPipeline.GetJsTypeName(systemModule, system);
                    writer.LineFormat("scheduler.schedule({0});", systemName);
                }

                // Enable/disable systems
                foreach (var reference in systems)
                {
                    var system = reference.Dereference(project.Registry);
                    
                    // By default systems are enabled when scheduled, nothing to write
                    if (system == null || system.Enabled)
                    {
                        continue;
                    }
                    
                    var systemModule = UTinyUtility.GetModules(system).FirstOrDefault();
                    var systemName = UTinyBuildPipeline.GetJsTypeName(systemModule, system);
                    
                    // @NOTE Disable currently accepts a string and NOT the `ut.System` object
                    writer.LineFormat("scheduler.disable({0});", EscapeJsString(systemName));
                }

                writer.Line("try { ut.Runtime.Service.run(world); } catch (e) { if (e !== 'SimulateInfiniteLoop') throw e; }");
            }

            writer.Line();

            using (writer.Scope("function WorldSetup(world)"))
            {
                writer.LineFormat("UT_ASSETS_SETUP(world);");
                
                var startupEntityGroup = module.StartupEntityGroup.Dereference(module.Registry);
                
                if (null != startupEntityGroup) 
                {
                    writer.Line($"{KEntityGroupNamespace}.{module.Namespace}[\"{module.StartupEntityGroup.Dereference(module.Registry).Name}\"].load(world);");
                }
                else
                {
                    Debug.LogError($"{UTinyConstants.ApplicationName}: BuildError - No startup group has been set");
                }
                
                using (writer.Scope("return"))
                {
                    writer
                        .LineFormat("canvasWidth: {0},", project.Settings.CanvasWidth)
                        .LineFormat("canvasHeight: {0},", project.Settings.CanvasHeight)
                        .LineFormat("canvasAutoResize: {0},", project.Settings.CanvasAutoResize ? "true" : "false");
                }

#if UNITY_EDITOR_WIN
                writer.Length -= 2;
#else
                writer.Length -= 1;
#endif
                writer.WriteRaw(";").Line();
            }            
            
            File.WriteAllText(file.FullName, writer.ToString(), Encoding.UTF8);
            results.BuildReport.GetOrAddChild(UTinyBuildReport.CodeNode).AddChild(file);
        }

        /// <summary>
        /// Generates `wsclient.js` to handle live-linking
        /// </summary>
        private static void GenerateWebSocketClient(UTinyBuildOptions options, UTinyBuildResults results)
        {
            if (!options.Project.Settings.IncludeWSClient)
            {
                return;
            }

            // Put local http server address into the wsclient script
            var content = File.ReadAllText(Path.Combine(UTinyBuildPipeline.GetToolDirectory("wsclient"), KWebSocketClientFileName));
            content = content.Replace("{{IPADDRESS}}", UTinyServer.Instance.LocalIPAddress);

            // Write wsclient to binary dir
            var file = new FileInfo(Path.Combine(results.BinaryFolder.FullName, KWebSocketClientFileName));
            File.WriteAllText(file.FullName, content, Encoding.UTF8);

            results.BuildReport.GetOrAddChild(UTinyBuildReport.CodeNode).AddChild(file);
        }

        /// <summary>
        /// Generates `libwebp.js` to handle WebP decompressor
        /// </summary>
        private static void GenerateWebPDecompressor(UTinyBuildOptions options, UTinyBuildResults results)
        {
            // Check if project use WebP texture format
            var module = options.Project.Module.Dereference(options.Project.Registry);
            var webpUsed = AssetIterator.EnumerateAssets(module)
                .Select(a => a.Object)
                .OfType<Texture2D>()
                .Select(t => UTinyUtility.GetAssetExportSettings(options.Project, t))
                .OfType<UTinyTextureSettings>()
                .Any(s => s.FormatType == TextureFormatType.WebP);

            // Warn about WebP usages
            if (options.Project.Settings.IncludeWebPDecompressor)
            {
                if (!webpUsed)
                {
                    Debug.LogWarning("This project does not uses the WebP texture format, but includes the WebP decompressor code. To reduce build size, it is recommended to disable \"Include WebP Decompressor\" in project settings.");
                }
            }
            else // WebP decompressor not included, do not copy to binary dir
            {
                if (webpUsed)
                {
                    Debug.LogWarning("This project uses the WebP texture format, but does not include the WebP decompressor code. The content will not load in browsers that do not natively support the WebP format. To ensure maximum compatibility, enable \"Include WebP Decompressor\" in project settings.");
                }
                return;
            }

            // Copy libwebp to binary dir
            var srcFile = Path.Combine(UTinyBuildPipeline.GetToolDirectory("libwebp"), KWebPDecompressorFileName);
            var dstFile = Path.Combine(results.BinaryFolder.FullName, KWebPDecompressorFileName);
            File.Copy(srcFile, dstFile);

            results.BuildReport.GetOrAddChild(UTinyBuildReport.CodeNode).AddChild(new FileInfo(dstFile));
        }

        /// <summary>
        /// Outputs the final `index.html` file
        /// </summary>
        private static void GenerateHTML(UTinyBuildOptions options, UTinyBuildResults results)
        {
            var project = options.Project;

            var settingsFile = new FileInfo(Path.Combine(results.BinaryFolder.FullName, KSettingsFileName));
            var runtimeFile = new FileInfo(Path.Combine(results.BinaryFolder.FullName, KRuntimeFileName));
            var bindingsFile = new FileInfo(Path.Combine(results.BinaryFolder.FullName, KBindingsFileName));
            var assetsFile = new FileInfo(Path.Combine(results.BinaryFolder.FullName, KAssetsFileName));
            var entityGroupsFile = new FileInfo(Path.Combine(results.BinaryFolder.FullName, KEntityGroupsFileName));
            var systemsFile = new FileInfo(Path.Combine(results.BinaryFolder.FullName, KSystemsFileName));
            var codeFile = new FileInfo(Path.Combine(results.BinaryFolder.FullName, KCodeFileName));
            var mainFile = new FileInfo(Path.Combine(results.BinaryFolder.FullName, KMainFileName));
            var webSocketClientFile = new FileInfo(Path.Combine(results.BinaryFolder.FullName, KWebSocketClientFileName));
            var webpDecompressorFile = new FileInfo(Path.Combine(results.BinaryFolder.FullName, KWebPDecompressorFileName));

            // nb: this writer is not HTML-friendly
            var writer = new UTinyCodeWriter()
            {
                CodeStyle = new CodeStyle()
                {
                    BeginBrace = string.Empty,
                    EndBrace = string.Empty,
                    BraceLayout = BraceLayout.EndOfLine,
                    Indent = "  ",
                    NewLine = Environment.NewLine
                }
            };
            writer.Line("<!DOCTYPE html>");
            using (writer.Scope("<html>"))
            {
                using (writer.Scope("<head>"))
                {
                    writer.Line("<meta charset=\"UTF-8\">");
                    if (UsesAdSupport(project))
                    {
                        writer.Line("<script src=\"mraid.js\"></script>");
                    }

                    if (project.Settings.RunBabel)
                    {
                        // Babelize user code
                        var title = $"{UTinyConstants.ApplicationName} Build";
                        const string messageFormat = "Transpiling {0} to ECMAScript 5";
                        
                        EditorUtility.DisplayProgressBar(title, "Transpiling to ECMAScript 5", 0.0f);
                        try
                        {
                            // We only need to transpile user authored code
                            var userCode = new [] {systemsFile, codeFile};
                            var babelDir = new DirectoryInfo(UTinyBuildPipeline.GetToolDirectory("babel"));
                            for (var i = 0; i < userCode.Length; i++)
                            {
                                var file = userCode[i];
                                EditorUtility.DisplayProgressBar(title, string.Format(messageFormat, file.Name), i / (float) userCode.Length);
                                UTinyBuildUtilities.RunNode(babelDir, "index.js", $"\"{file.FullName}\" \"{file.FullName}\"");
                            }
                        }
                        finally
                        {
                            EditorUtility.ClearProgressBar();
                        }
                    }

                    // Gather all game files (order is important)
                    var files = new List<FileInfo>
                    {
                        settingsFile,
                        runtimeFile,
                        bindingsFile,
                        assetsFile,
                        entityGroupsFile,
                        systemsFile,
                        codeFile,
                        mainFile,
                        webSocketClientFile,
                        webpDecompressorFile
                    }.Where(file => file != null && file.Exists).ToList();

                    // Extra steps for Release config
                    if (options.Configuration == UTinyBuildConfiguration.Release)
                    {
                        // Minify JavaScript
                        var gameFile = new FileInfo(Path.Combine(results.BinaryFolder.FullName, "game.js"));
                        EditorUtility.DisplayProgressBar($"{UTinyConstants.ApplicationName} Build", "Minifying JavaScript code...", 0.0f);
                        try
                        {
                            var minifyDir = new DirectoryInfo(UTinyBuildPipeline.GetToolDirectory("minify"));
                            UTinyBuildUtilities.RunNode(minifyDir, "index.js", $"\"{gameFile.FullName}\" {String.Join(" ", files.Select(file => '"' + file.FullName + '"'))}");
                            files.ForEach(file => file.Delete());
                        }
                        finally
                        {
                            EditorUtility.ClearProgressBar();
                        }

                        // Package as single html file
                        if (project.Settings.SingleFileHtml)
                        {
                            writer.Line("<script type=\"text/javascript\">");
                            writer.WriteRaw(File.ReadAllText(gameFile.FullName));
                            writer.Line();
                            writer.Line("</script>");
                            gameFile.Delete();
                        }
                        else
                        {
                            writer.LineFormat("<script src=\"{0}\"></script>", gameFile.Name);
                        }
                    }
                    else
                    {
                        files.ForEach(file => writer.LineFormat("<script src=\"{0}\"></script>", file.Name));
                    }
                    writer.LineFormat("<title>{0}</title>", project.Name);
                    writer.CodeStyle.EndBrace = "</head>";
                }
                using (writer.Scope("<body>"))
                {
                    writer.CodeStyle.EndBrace = "</body>";
                }
                writer.CodeStyle.EndBrace = "</html>";
            }

            // Write final index.html file
            var htmlFile = new FileInfo(Path.Combine(results.BinaryFolder.FullName, KHtmlFileName));
            File.WriteAllText(htmlFile.FullName, writer.ToString(), Encoding.UTF8);
        }

        /// <summary>
        /// Generates the `build-report.json` file
        /// </summary>
        private static void GenerateBuildReport(UTinyBuildResults results)
        {
            // Output build report data as json
            File.WriteAllText(Path.Combine(results.OutputFolder.FullName, "build-report.json"), results.BuildReport.ToString(), Encoding.UTF8);
        }

        private static void PrependGeneratedHeader(UTinyCodeWriter writer, string name)
        {
            var builder = new StringBuilder();
            builder.AppendLine($"/**");
            builder.AppendLine($" * {UTinyConstants.ApplicationName.ToUpperInvariant()} GENERATED CODE, DO NOT EDIT BY HAND");
            builder.AppendLine($" * @project {name}");
            builder.AppendLine($" */");
            builder.AppendLine();
            writer.Prepend(builder.ToString());
        }
        
        private static string EscapeJsString(string content)
        {
            return Unity.Properties.Serialization.Json.EncodeJsonString(content);
        }
        
        private static bool UsesAdSupport(UTinyProject project)
        {
            var registry = project.Registry;
            var module = project.Module.Dereference(registry);
            foreach (var m in module.EnumerateDependencies())
            {
                if (m.Namespace == "ut.AdSupport")
                    return true;
            }

            return false;
        }
    }
}
#endif // NET_4_6
