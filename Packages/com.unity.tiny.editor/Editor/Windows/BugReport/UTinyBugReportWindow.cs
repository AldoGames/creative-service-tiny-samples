#if NET_4_6
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Mail;
using System.Text;
using Unity.Tiny.Extensions;
using UnityEditor;
using UnityEditor.Experimental.UIElements;

using UnityEngine;
using UnityEngine.Experimental.UIElements;

using Unity.Tiny.Jira;

namespace Unity.Tiny
{
    using ValidateMethod = Func<bool>;
    using Slots = Dictionary<string, VisualElement>;

    public class UTinyBugReportWindow : EditorWindow
    {
        #region Constants
        private const string k_ExportFolderName = UTinyConstants.ApplicationName + "BugReport";

        private const string k_BugReportRootFieldName = "root_bug_report";
        private const string k_OccurrenceFieldName    = "input_occurrence";
        private const string k_PlatformFieldName      = "input_platform";
        private const string k_EmailFieldName         = "input_email";
        private const string k_TitleFieldName         = "input_title";
        private const string k_DetailFieldName        = "input_detail";
        private const string k_ReproStepsFieldName    = "input_repro_steps";
        private const string k_AttachedFilesFieldName = "container_attached_files";
        private const string k_AddFileFieldName       = "button_add_file";
        private const string k_AddFolderFieldName     = "button_add_folder";
        private const string k_SendFieldName          = "button_send";
        private const string k_CancelFieldName        = "button_cancel";

        private const string k_ScrollableContentClass = "scrollable-content";

        private static readonly Color k_Color = Color.red;

        private static readonly string[] k_EmptyPackageableFiles = new string[0];
        private static readonly string[] k_EmptyNonPackageableFiles = new string[0];

        public const string k_BugPackagePath = "Assets/bug-package.unitypackage";
        #endregion

        #region Static
        [MenuItem(UTinyConstants.MenuItemNames.BugReportWindow, priority = 10000)]
        private static void NewBugReportWindow()
        {
            var window = GetWindow<UTinyBugReportWindow>();
            window.Show();
        }
        #endregion

        #region Fields
        private EnumField m_BugOccurrenceField;
        private EnumField m_BugPlatformField;
        private TextField m_EmailField;
        private TextField m_TitleField;
        private TextField m_DetailField;
        private TextField m_ReproStepsField;
        private VisualElement m_AttachedFilesContainer;
        private Button m_AddFileButton;
        private Button m_AddFolderButton;
        private Button m_SendButton;
        private Button m_CancelButton;

        private readonly Dictionary<VisualElement, Color> m_NormalColors = new Dictionary<VisualElement, Color>();
        private readonly HashSet<string> m_Paths = new HashSet<string>();
        private readonly List<string> m_PathsOnSend = new List<string>();

        private List<ValidateMethod> m_FieldValidationMethods;
        private List<ValidateMethod> m_SendMethods;
        private Dictionary<string, string> s_ErrorMessages;
        #endregion

        #region Properties
        public BugOccurrence Occurence => (BugOccurrence)m_BugOccurrenceField.value;
        public BugPlatform Platform => (BugPlatform)m_BugPlatformField.value;
        public string Email => m_EmailField.value;
        public string Title => m_TitleField.value;
        public string Detail => m_DetailField.value;
        public string ReproSteps => m_ReproStepsField.value;

        private string ProjectName { get; set; }
        private static string DataPath { get; set; }
        private static string ProjectPath { get; set; }
        private static string BugReportPath { get; set; }
        private static string BugProjectPath { get; set; }
        private static string ZipFilePath { get; set; }
        #endregion

        #region Unity
        private void OnEnable()
        {
            titleContent.text = UTinyConstants.WindowNames.BugReportingWindow;
            minSize = new Vector2(425.0f, 600.0f);
            EnsureMinSize();

            SetupResources();
            CreateDom();
            // This is delayed so that we *might* have a chance to pick up the current project after a compilation.
            EditorApplication.delayCall += AddRequiredFileAndFolders;
        }
        #endregion

        #region Implementation
        private void EnsureMinSize()
        {
            if (position.size.x < minSize.x || position.size.y < minSize.y)
            {
                var pos = position;
                pos.size = minSize;
                position = pos;
            }
        }

        private void SetupResources()
        {
            DataPath = NormalizePath(Application.dataPath);
            var projectFolder = new DirectoryInfo(Application.dataPath).Parent;
            ProjectPath = NormalizePath(projectFolder.FullName);
            ProjectName = projectFolder.Name;
            BugReportPath = $"{ProjectPath}/{k_ExportFolderName}";
            BugProjectPath = $"{BugReportPath}/{ProjectName}";
            ZipFilePath = $"{BugReportPath}/bug-report.zip";

            m_FieldValidationMethods = new List<ValidateMethod>
            {
                ValidateBugOccurrence,
                ValidateEmail,
                ValidateTitle
            };

            m_SendMethods = new List<ValidateMethod>
            {
                ValidatingRequiredFields,
                CreatingBugReportFolders,
                GatheringPackageManifestFile,
                GatheringProjectSettings,
                GatheringAdditionalInfo,
                GatheringDependencies,
                GatheringTinyPackageFiles,
                PackagingDependencies,
                ZippingBugReport,
                SendingBugReport,
            };

            s_ErrorMessages = new Dictionary<string, string>
            {
                { nameof(CreatingBugReportFolders),      "Could not create the necessary folders to export the bug report." },
                { nameof(GatheringPackageManifestFile), "Could not gather the package's  manifest file." },
                { nameof(GatheringProjectSettings),     "Could not gather the project's settings files." },
                { nameof(GatheringAdditionalInfo),      "Could not gather the system's information." },
                { nameof(GatheringDependencies),       $"Could not gather {UTinyConstants.ApplicationName}'s dependencies. Consider manually zipping the project's files and dependencies." },
                { nameof(GatheringTinyPackageFiles),   $"Could not gather the {UTinyConstants.ApplicationName} package"},
                { nameof(PackagingDependencies),        "Could not package the dependencies of the report." },
                { nameof(ZippingBugReport),             "Could not zip the bug report." },
                { nameof(SendingBugReport),             "Failed to send the Bug Report." },
            };
        }

        private BugPlatform GetCurrentPlatform()
        {
            switch (SystemInfo.operatingSystemFamily)
            {
                case OperatingSystemFamily.MacOSX : return BugPlatform.Mac;
                case OperatingSystemFamily.Windows: return BugPlatform.Windows;
                case OperatingSystemFamily.Linux  : return BugPlatform.Linux;
                default                           : return BugPlatform.Windows;
            }
        }
        
        private void CreateDom()
        {
            var root = this.GetRootVisualContainer();
            var template = EditorGUIUtility.Load($"{UTinyConstants.PackageFolder}/Editor Default Resources/UTiny/UXML/BugReportingTemplate.uxml") as VisualTreeAsset;

            var slots = new Dictionary<string, VisualElement>();
            template.CloneTree(root, slots);
            const string stylePath =
                "Packages/com.unity.tiny.editor/Editor Default Resources/UTiny/USS/UTinyBugReportStyle.{0}.uss";
            root.AddStyleSheetPath(string.Format(stylePath, "common"));
            root.AddStyleSheetPath(string.Format(stylePath, EditorGUIUtility.isProSkin ? "dark": "light"));

            var bugRoot = root.Q<VisualElement>(k_BugReportRootFieldName);
            bugRoot.StretchToParentSize();
            bugRoot.style.positionType = UnityEngine.Experimental.UIElements.StyleEnums.PositionType.Absolute;

            m_BugOccurrenceField = CreateField(k_OccurrenceFieldName, BugOccurrence.PleaseSpecify, slots);
            m_BugPlatformField = CreateField(k_PlatformFieldName, GetCurrentPlatform(), slots);

            m_EmailField = root.Q<TextField>(k_EmailFieldName);
            m_TitleField = root.Q<TextField>(k_TitleFieldName);

            m_DetailField = CreateField(k_DetailFieldName, "Describe what happened", slots);
            m_ReproStepsField = CreateField(k_ReproStepsFieldName, "Describe how we can reproduce it using the attached project", slots);

            m_AttachedFilesContainer = new VisualElement();
            m_AttachedFilesContainer.name = "Attached Files Container";

            slots[k_AttachedFilesFieldName].contentContainer.Add(m_AttachedFilesContainer);
            slots[k_AttachedFilesFieldName].contentContainer.AddToClassList(k_ScrollableContentClass);

            root.Q<Button>(k_AddFileFieldName).RegisterCallback<MouseUpEvent>(AddFile);
            root.Q<Button>(k_AddFolderFieldName).RegisterCallback<MouseUpEvent>(AddFolder);
            root.Q<Button>(k_SendFieldName).RegisterCallback<MouseUpEvent>(Send);
            root.Q<Button>(k_CancelFieldName).RegisterCallback<MouseUpEvent>(Cancel);
        }

        private EnumField CreateField(string name, Enum initialValue, Slots slots)
        {
            var field = new EnumField(initialValue);
            field.name = name;
            slots[name].Add(field);
            return field;
        }

        private TextField CreateField(string name, string initialValue, Slots slots)
        {
            var field = new TextField();
            field.name = k_DetailFieldName;
            field.multiline = true;
            field.doubleClickSelectsWord = true;
            field.tripleClickSelectsLine = true;
            field.value = initialValue;
            slots[name].contentContainer.AddToClassList(k_ScrollableContentClass);
            slots[name].contentContainer.Add(field);
            return field;
        }

        private void AddRequiredFileAndFolders()
        {
            var project = UTinyEditorApplication.Project;
            if (null == project)
            {
                return;
            }
            AddFileElements(UTinyPersistence.GetLocation(project), false);
        }

        private void AddFile(MouseUpEvent evt)
        {
            AddFileOrFolder("Select a File", EditorUtility.OpenFilePanel);
        }

        private void AddFolder(MouseUpEvent evt)
        {
            AddFileOrFolder("Select a Folder", EditorUtility.OpenFolderPanel);
        }

        private void AddFileOrFolder(string display, Func<string, string, string, string> panelMethod)
        {
            var path = panelMethod(display, Application.dataPath, null);
            if (path?.Length != 0)
            {
                AddFileElements(path);
            }
        }

        private void Send(MouseUpEvent evt)
        {
            if (Directory.Exists(BugReportPath))
            {
                UTinyBuildUtilities.PurgeDirectory(new DirectoryInfo(BugReportPath));
            }
            EditorUtility.DisplayProgressBar($"{UTinyConstants.ApplicationName} Bug Report", "Validating Fields", 0.0f);

            m_PathsOnSend.AddRange(m_Paths);
            try
            {
                if (!RunUntilFailure(m_SendMethods, true))
                {
                    EditorUtility.ClearProgressBar();
                    // Failed to send the bug report to Jira, but successfully created the zip file.
                    if (File.Exists(ZipFilePath))
                    {
                        Debug.Log($"{UTinyConstants.ApplicationName}: Could not send the bug report. you can send it manually by emailing the '{RelativeToProjectFolder(ZipFilePath)}' file to 'tiny-qa-reporter@unity3d.com'" +
                                  "\nNo need to provide additional description, it is all packed in the zip file itself.");
                        EditorUtility.RevealInFinder(ZipFilePath);
                        Close();
                    }
                }
                else
                {
                    Debug.Log($"{UTinyConstants.ApplicationName}: bug sent successfully - thanks!");
                    Close();
                }
            }
            finally
            {
                CleanUp();
                m_PathsOnSend.Clear();
                EditorUtility.ClearProgressBar();
            }
        }

        private void Cancel(MouseUpEvent evt)
        {
            Close();
        }

        private void HighlightErrorUntilClicked(VisualElement elem)
        {
            SetErrorColorBackground(elem);
            elem.RegisterCallback<MouseDownEvent>(OnGainedFocus);
        }

        private void OnGainedFocus(MouseDownEvent evt)
        {
            var field = evt.target as VisualElement;
            SetNormalColorBackground(field);
            field.UnregisterCallback<MouseDownEvent>(OnGainedFocus);
        }

        private void SetErrorColorBackground(VisualElement elem)
        {
            if (elem.style.backgroundColor != k_Color)
            {
                m_NormalColors[elem] = elem.style.backgroundColor;
            }

            elem.style.backgroundColor = k_Color;
        }

        private void SetNormalColorBackground(VisualElement elem)
        {
            Color normal;
            if (m_NormalColors.TryGetValue(elem, out normal))
            {
                elem.style.backgroundColor = normal;
            }
        }

        private void AddFileElements(string path, bool canRemove = true)
        {
            if (string.IsNullOrEmpty(path))
            {
                return;
            }

            path = NormalizePath(new FileInfo(path).FullName);

            if (!m_Paths.Add(path))
            {
                return;
            }
            var rootElement = new VisualElement();
            rootElement.AddToClassList("row");
            rootElement.AddToClassList("file");
            rootElement.Add(new Label(RelativeToProjectFolder(path)));

            if (canRemove)
            {
                var removeElement = new Button(() =>
                {
                    m_AttachedFilesContainer.Remove(rootElement);
                    m_Paths.Remove(path);
                });

                removeElement.AddToClassList("remove-button");
                removeElement.text = "X";
                rootElement.Add(removeElement);
            }

            m_AttachedFilesContainer.Add(rootElement);
        }

        private bool ValidatingRequiredFields()
        {
            return RunAll(m_FieldValidationMethods);
        }

        private bool CreatingBugReportFolders()
        {
            if (Directory.Exists(BugReportPath))
            {
                Directory.Delete(BugReportPath, true);
            }
            var exportFolder = Directory.CreateDirectory(BugReportPath);
            var bugProjectFolder = exportFolder.CreateSubdirectory(ProjectName);
            bugProjectFolder.CreateSubdirectory("Assets");
            bugProjectFolder.CreateSubdirectory("ProjectSettings");
            bugProjectFolder.CreateSubdirectory("Packages");
            return true;
        }

        private bool GatheringPackageManifestFile()
        {
            const string manifestFilename = "manifest.json";
            var manifestPath = $"{ProjectPath}/Packages/{manifestFilename}";
            if (!File.Exists(manifestPath))
            {
                return false;
            }
            var target = $"{BugProjectPath}/Packages/{manifestFilename}";

            File.Copy(manifestPath, target, true);
            return true;
        }

        private bool GatheringProjectSettings()
        {
            var target = $"{BugProjectPath}/ProjectSettings";
            foreach (var file in new DirectoryInfo($"{ProjectPath}/ProjectSettings").GetFiles())
            {
                File.Copy(file.FullName, $"{target}/{file.Name}", true);
            }
            return true;
        }

        private bool GatheringDependencies()
        {
            var registry = UTinyEditorApplication.Registry;
            foreach (var path in m_PathsOnSend)
            {
                if (File.Exists(path))
                {
                    ProcessFileDependencies(registry, path);
                }
                else if (Directory.Exists(path))
                {
                    ProcessDirectoryDependencies(registry, path);
                }
            }
            return true;
        }

        // Once we switch back to using the Package Manager, we should remove this method.
        private bool GatheringTinyPackageFiles()
        {
            if (TinyPackageUtility.IsTinyPackageEmbedded)
            {
                TinyIOUtilities.CopyFolder($"{ProjectPath}/{UTinyConstants.PackageFolder}", $"{BugProjectPath}/{UTinyConstants.PackageFolder}");
                File.Delete($"{BugProjectPath}/{UTinyConstants.PackageFolder}/tiny-runtime-dist.zip");
                File.Delete($"{BugProjectPath}/{UTinyConstants.PackageFolder}/tiny-runtime-dist.zip.meta");
            }
            return true;
        }

        private bool GatheringAdditionalInfo()
        {
            var builder = new StringBuilder();
            if (TinyPackageUtility.IsTinyPackageEmbedded)
            {
                builder.AppendLine($"WARNING: The {UTinyConstants.ApplicationName} package is embedeed in the manifest file, you might need to be manually embed it.");
                builder.AppendLine();
            }

            builder.AppendLine($"{nameof(SystemInfo.operatingSystemFamily)} : {SystemInfo.operatingSystemFamily}");
            builder.AppendLine($"{nameof(SystemInfo.operatingSystem)}       : {SystemInfo.operatingSystem}");
            builder.AppendLine($"{nameof(SystemInfo.processorType)}         : {SystemInfo.processorType}");
            builder.AppendLine($"{nameof(SystemInfo.processorCount)}        : {SystemInfo.processorCount}");
            builder.AppendLine($"{nameof(SystemInfo.processorFrequency)}    : {SystemInfo.processorFrequency}");
            builder.AppendLine($"{nameof(SystemInfo.graphicsDeviceType)}    : {SystemInfo.graphicsDeviceType}");


            var filename = "Assets/additional-info.txt";
            File.WriteAllText(filename, builder.ToString(), Encoding.UTF8);
            AssetDatabase.ImportAsset(filename, ImportAssetOptions.ImportRecursive | ImportAssetOptions.ForceSynchronousImport);

            File.Move(filename, $"{BugProjectPath}/{filename}");
            File.Move($"{filename}.meta", $"{BugProjectPath}/{filename}.meta");

            // Pack the issue-info, in case something goes wrong.
            var issueDataFile = "Assets/bug-issue-info.txt";
            var issueDataString = new BugIssueData
            {
                Occurrence = Occurence,
                Platform = Platform,
                Email = Email,
                Title = Title,
                Description = Detail,
                ReproSteps = ReproSteps
            }.GetDataString().Replace("\\n", "\n");
            File.WriteAllText(issueDataFile, issueDataString, Encoding.UTF8);
            AssetDatabase.ImportAsset(issueDataFile, ImportAssetOptions.ForceSynchronousImport);

            File.Move(issueDataFile, $"{BugProjectPath}/{issueDataFile}");
            File.Move($"{issueDataFile}.meta", $"{BugProjectPath}/{issueDataFile}.meta");
            return true;
        }

        private bool PackagingDependencies()
        {
            IEnumerable<string> packagableFiles = k_EmptyPackageableFiles;
            IEnumerable<string> nonPackagableFiles = k_EmptyNonPackageableFiles;

            foreach (var group in m_Paths.GroupBy(p => p.StartsWith(DataPath)))
            {
                var inAssetFolder = group.Key;
                if (inAssetFolder)
                {
                    packagableFiles = group.Select(RelativeToProjectFolder);
                }
                else
                {
                    nonPackagableFiles = group;
                }
            }

            if (nonPackagableFiles.Any())
            {
                string zipFilename = $"{DataPath}/external-files.zip";
                UTinyBuildUtilities.ZipPaths(nonPackagableFiles.ToArray(), zipFilename);
                var relativeZipPath = RelativeToProjectFolder(zipFilename);
                AssetDatabase.ImportAsset(relativeZipPath, ImportAssetOptions.ForceSynchronousImport);
                File.Move(zipFilename, $"{BugProjectPath}/{relativeZipPath}");
                File.Move($"{zipFilename}.meta", $"{BugProjectPath}/{relativeZipPath}.meta");
            }

            if (packagableFiles.Any())
            {
                var toPackage = packagableFiles.ToArray();
                var targetPackagePath = $"{BugProjectPath}/{k_BugPackagePath}";
                AssetDatabase.ExportPackage(toPackage, targetPackagePath, ExportPackageOptions.IncludeDependencies | ExportPackageOptions.Recurse);
            }

            return true;
        }

        private bool ZippingBugReport()
        {
            return UTinyBuildUtilities.ZipFolder(new DirectoryInfo(BugProjectPath), ZipFilePath);
        }

        private bool SendingBugReport()
        {
            var data = new BugIssueData
            {
                Occurrence = Occurence,
                Platform = Platform,
                Email = Email,
                Title = Title,
                Description = Detail,
                ReproSteps = ReproSteps
            };
            return JiraAPI.CreateBugIssue(data, new [] { ZipFilePath });
        }

        private void CleanUp()
        {
            // We will keep the bug-report zip file, but clean up everything-else.
            if (Directory.Exists(BugProjectPath))
            {
                UTinyBuildUtilities.PurgeDirectory(new DirectoryInfo(BugProjectPath));
            }
        }

        #endregion

        #region Validation
        private bool ValidateBugOccurrence()
        {
            if (Occurence == BugOccurrence.PleaseSpecify)
            {
                HighlightErrorUntilClicked(m_BugOccurrenceField);
                return false;
            }
            return true;
        }

        private bool ValidateEmail()
        {
            try
            {
                new MailAddress(Email);
                return true;
            }
            catch (Exception)
            {
                HighlightErrorUntilClicked(m_EmailField);
                return false;
            }
        }

        private bool ValidateTitle()
        {
            if (string.IsNullOrEmpty(Title) || string.IsNullOrWhiteSpace(Title))
            {
                HighlightErrorUntilClicked(m_TitleField);
                return false;
            }
            return true;
        }


        #endregion

        #region Helpers
        private bool RunAll(List<ValidateMethod> methods, bool showProgressBar = false)
        {
            return Run(methods, false, showProgressBar);
        }

        private bool RunUntilFailure(List<ValidateMethod> methods, bool showProgressBar = false)
        {
            return Run(methods, true, showProgressBar);
        }

        private bool Run(List<ValidateMethod> methods, bool returnOnFail, bool showProgressBar)
        {
            var result = true;

            for (int i = 0; i < methods.Count; ++i)
            {
                var method = methods[i];
                if (showProgressBar)
                {
                    EditorUtility.DisplayProgressBar($"{UTinyConstants.ApplicationName} Bug Report", ObjectNames.NicifyVariableName(method.Method.Name), i / (float)methods.Count);
                }
                if (!SafeRun(method))
                {
                    string errorMessage;
                    if (s_ErrorMessages.TryGetValue(method.Method.Name, out errorMessage))
                    {
                        EditorUtility.DisplayDialog($"{UTinyConstants.ApplicationName} Bug Report", errorMessage, "OK");
                    }
                    if (returnOnFail)
                    {
                        return false;
                    }
                    result = false;
                }
            }
            return result;
        }

        private static bool SafeRun(ValidateMethod method)
        {
            try
            {
                return method();
            }
            catch (Exception ex)
            {
                #if UTINY_INTERNAL
                Debug.LogException(ex);
                #else
                Console.WriteLine(ex.ToString());
                #endif
                return false;
            }
        }

        private static string NormalizePath(string path)
        {
            return path.Replace(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        }

        private static string RelativeToProjectFolder(string path)
        {
            path = NormalizePath(path);
            if (path.StartsWith(ProjectPath) && path.Length != ProjectPath.Length)
            {
                return path.Remove(0, ProjectPath.Length + 1);
            }
            return path;
        }

        private void ProcessFileDependencies(IRegistry registry, string filePath)
        {
            TryProcessFileDependenciesAs<UTinyProject>(registry, filePath, UTinyPersistence.ProjectFileExtension, UTinyPersistence.LoadProject);
            TryProcessFileDependenciesAs<UTinyModule>(registry, filePath, UTinyPersistence.ModuleFileExtension, UTinyPersistence.LoadModule);
        }

        private void ProcessDirectoryDependencies(IRegistry registry, string directoryPath)
        {
            var directory = new DirectoryInfo(directoryPath);
            foreach (var file in directory.EnumerateFiles())
            {
                ProcessFileDependencies(registry, file.FullName);
            }
            foreach (var subDirectory in directory.EnumerateDirectories())
            {
                ProcessDirectoryDependencies(registry, subDirectory.FullName);
            }
        }

        private void TryProcessFileDependenciesAs<TRegistryObject>(IRegistry registry, string path, string expectedExtension, Action<string, IRegistry> loadMethod) where TRegistryObject : class, IRegistryObject
        {
            if (Path.GetExtension(path) != expectedExtension)
            {
                return;
            }
            var filename = Path.GetFileNameWithoutExtension(path);

            var registryObject = registry?.FindByName<TRegistryObject>(filename);
            if (null != registryObject)
            {
                AddRegistryObjectDependency(registryObject);
            }
            else
            {
                path = RelativeToProjectFolder(path);
                // Load
                var projectRegistry = new UTinyRegistry();
                loadMethod(path, projectRegistry);
                AddRegistryObjectDependency(projectRegistry.FindByName<TRegistryObject>(filename));
            }
        }

        private void AddRegistryObjectDependency<TRegistryObject>(TRegistryObject registryObject) where TRegistryObject : class, IRegistryObject
        {
            if (registryObject is UTinyProject)
            {
                AddRegistryObjectDependency(registryObject as UTinyProject);
            }
            else if (registryObject is UTinyModule)
            {
                AddRegistryObjectDependency(registryObject as UTinyModule);
            }
        }

        private bool ShouldPackage(string path)
        {
            if (string.IsNullOrEmpty(path))
            {
                return false;
            }
            return !path.StartsWith(UTinyConstants.PackageFolder)   &&
                   !path.Contains(UTinyConstants.SamplesFolderName) &&
                   !path.StartsWith("Library")                      &&
                   !path.StartsWith("Resources/unity_builtin_extra");
        }

        private void AddRegistryObjectDependency(UTinyProject project)
        {
            if (null == project)
            {
                return;
            }

            var path = UTinyPersistence.GetLocation(project);
            // Core or sample module/project don't need to be packaged, they will be included from the manifest.
            if (ShouldPackage(path))
            {
                AddFileElements(path, false);
            }

            foreach (var module in project.Module.Dereference(project.Registry).EnumerateDependencies())
            {
                AddRegistryObjectDependency(module);
            }
        }

        private void AddRegistryObjectDependency(UTinyModule module)
        {
            if (null == module)
            {
                return;
            }

            foreach (var m in module.EnumerateDependencies())
            {
                var path = UTinyPersistence.GetLocation(m);

                // Core or sample module/project don't need to be packaged, they will be included from the manifest.
                if (ShouldPackage(path))
                {
                    AddFileElements(path, false);
                }

                foreach (var asset in AssetIterator.EnumerateAssets(m))
                {
                    if (ShouldPackage(asset.AssetPath))
                    {
                        AddFileElements(asset.AssetPath, false);
                    }
                }
            }
        }
        #endregion
    }
}
#endif
