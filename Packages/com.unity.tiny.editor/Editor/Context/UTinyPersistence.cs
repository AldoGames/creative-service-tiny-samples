#if NET_4_6
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Unity.Properties;
using UnityEditor;
using UnityEngine;
using UnityEngine.Assertions;

namespace Unity.Tiny
{
    public static class UTinyPersistence
    {
        public const string ProjectFileImporterExtension = "utproject";
        public const string ProjectFileExtension = "." + ProjectFileImporterExtension;
        public const string ModuleFileImporterExtension = "utmodule";
        public const string ModuleFileExtension = "." + ModuleFileImporterExtension;

        public static IEnumerable<string> FindAllProjectFiles()
        {
            return FindAllAssetsOfType(ProjectFileImporterExtension);
        }

        public static IEnumerable<string> FindAllModuleFiles()
        {
            return FindAllAssetsOfType(ModuleFileImporterExtension);
        }

        private static IEnumerable<string> FindAllAssetsOfType(string typeName)
        {
            foreach (var guid in FindAllAssetsGuidsOfType(typeName))
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                if (!string.IsNullOrEmpty(path))
                {
                    yield return path;
                }
            }
        }
        
#if !UNITY_2018_2_OR_NEWER
        private static IEnumerable<string> FindPackageAssetGuids(string extension)
        {
            const string dbPath = UTinyConstants.PackagePath + "Runtime/Modules/";
            var realPath = Path.GetFullPath(dbPath);
            
            var dir = new DirectoryInfo(realPath);
            if (dir.Exists)
            {
                var prefixLen = realPath.Length;
                foreach (var f in dir.GetFiles("*." + extension, SearchOption.AllDirectories))
                {
                    var fileName = f.FullName.Replace('\\', '/');
                    var assetPath = dbPath + fileName.Substring(prefixLen, fileName.Length - prefixLen);
                    var guid = AssetDatabase.AssetPathToGUID(assetPath);
                    if (string.IsNullOrEmpty(guid))
                    {
                        Debug.LogError($"No AssetDatabase GUID found for file: {f.FullName}, with path: {assetPath}");
                        continue;
                    }

                    yield return guid;
                }
            }
        }
        
        // ASSUMPTION: Package content is immutable, so this can be cached
        private static readonly Dictionary<string, string[]> k_PackageAssetsMap = new Dictionary<string, string[]>();
#endif
        
        private static IEnumerable<string> FindAllAssetsGuidsOfType(string typeName)
        {
            foreach (var guid in AssetDatabase.FindAssets("t:" + typeName))
            {
                yield return guid;
            }

            // FindAssets is expected to work with Packages in 2018.2
            #if !UNITY_2018_2_OR_NEWER
            
            string[] packageAssets;
            if (!k_PackageAssetsMap.TryGetValue(typeName, out packageAssets))
            {
                packageAssets = FindPackageAssetGuids(typeName).ToArray();
                k_PackageAssetsMap[typeName] = packageAssets;
            }

            foreach (var guid in packageAssets)
            {
                yield return guid;
            }
            
            #endif
        }

        private static IEnumerable<T> AsEnumerable<T>(T o)
        {
            yield return o;
        }

        private static void LoadJson(string jsonFilePath, IRegistry registry, string identifier)
        {
            LoadJsonFiles(registry, identifier, AsEnumerable(jsonFilePath));
        }

        private static void LoadJsonFiles(IRegistry registry, string identifier, IEnumerable<string> jsonFilePaths)
        {
            using (var command = new MemoryStream())
            {
                foreach (var jsonFile in jsonFilePaths)
                {
                    using (var json = File.OpenRead(jsonFile))
                    {
                        Serialization.FlatJson.FrontEnd.Accept(json, command);
                    }
                }

                using (registry.SourceIdentifierScope(identifier))
                {
                    command.Position = 0;
                    Serialization.CommandStream.FrontEnd.Accept(command, registry);
                }
            }
        }

        public static void LoadAllModules(IRegistry registry)
        {
            var guids = FindAllAssetsGuidsOfType(ModuleFileImporterExtension);

            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                LoadJson(path, registry, guid);

                foreach (var module in registry.FindAllBySource(guid).OfType<UTinyModule>())
                {
                    module.PersistenceId = guid;
                }
            }
        }

        public static void ReloadObject(IRegistry registry, string guid)
        {
            var path = AssetDatabase.GUIDToAssetPath(guid);
            registry.UnregisterAllBySource(guid);
            LoadJson(path, registry, guid);

            foreach (var obj in registry.FindAllBySource(guid).OfType<IPersistentObject>())
            {
                obj.PersistenceId = guid;
            }
        }

        public static void LoadProject(string projectPath, IRegistry registry)
        {
            registry.Clear();
            LoadAllModules(registry);
            
            // the project itself has no identifier
            registry.UnregisterAllBySource(UTinyRegistry.DefaultSourceIdentifier);
            LoadJson(projectPath, registry, UTinyRegistry.DefaultSourceIdentifier);
            
            foreach (var project in registry.FindAllBySource(UTinyRegistry.DefaultSourceIdentifier).OfType<UTinyProject>())
            {
                project.PersistenceId = AssetDatabase.AssetPathToGUID(projectPath);
                UTinyUpdater.UpdateProject(project);
            }
        }

        public static void LoadModule(string modulePath, IRegistry registry)
        {
            registry.Clear();
            LoadAllModules(registry);
            
            var guid = AssetDatabase.AssetPathToGUID(modulePath);
            registry.UnregisterAllBySource(guid);
            LoadJson(modulePath, registry, UTinyRegistry.DefaultSourceIdentifier);
            
            foreach (var module in registry.FindAllBySource(UTinyRegistry.DefaultSourceIdentifier).OfType<UTinyModule>())
            {
                module.PersistenceId = AssetDatabase.AssetPathToGUID(modulePath);
            }
        }

        public static string GetLocation(IPersistentObject p)
        {
            var guid = p.PersistenceId;
            if (string.IsNullOrEmpty(guid))
            {
                return null;
            }

            var path = AssetDatabase.GUIDToAssetPath(guid);
            return string.IsNullOrEmpty(path) ? null : path;
        }

        /// <summary>
        /// Returns the file name with extension for the given persistent object
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        /// <exception cref="NotSupportedException"></exception>
        private static string GetFileName(IPersistentObject obj)
        {
            if (obj is UTinyProject)
                return obj.Name + ProjectFileExtension;
            if (obj is UTinyModule)
                return obj.Name + ModuleFileExtension;
            throw new NotSupportedException($"Persitent object of type {obj.GetType()} has no known file extension");
        }
        
        /// <summary>
        /// Returns the extension for the given persistent object
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        /// <exception cref="NotSupportedException"></exception>
        public static string GetFileExtension(IPersistentObject obj)
        {
            if (obj is UTinyProject)
                return ProjectFileExtension;
            if (obj is UTinyModule)
                return ModuleFileExtension;
            throw new NotSupportedException($"Persitent object of type {obj.GetType()} has no known file extension");
        }

        private static string FullPathToAssetPath(string path)
        {
            var rootPath = Path.GetFullPath(".").Replace('\\', '/') + "/";
            var assetPath = path.Replace("\\", "/").Replace(rootPath, string.Empty);
            return assetPath;
        }
        
        public static string PersistObject(IPersistentObject obj)
        {
            var path = GetLocation(obj);
            
            // No persistent path exists for this object
            // Default to `Assets/{NAME}`
            if (string.IsNullOrEmpty(path))
            {
                path = Path.Combine(Application.dataPath, GetFileName(obj));
            }
            
            return PersistObjectAs(obj, path);
        }

        public static string PersistObjectAs(IPersistentObject obj, string path)
        {
            path = PersistObjectsAs(obj.EnumeratePersistedObjects(), path);
            
            var assetPath = FullPathToAssetPath(path);
            AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceSynchronousImport);
            s_ChangedAssetPaths.Remove(assetPath);
            var assetGuid = AssetDatabase.AssetPathToGUID(assetPath);
            obj.PersistenceId = assetGuid;

            return path;
        }
        
        private static string PersistObjectsAs(IEnumerable<IPropertyContainer> objects, string path)
        {
            Assert.IsFalse(string.IsNullOrEmpty(path));

            var fileName = Path.GetFileName(path);
            
            Assert.IsFalse(string.IsNullOrEmpty(fileName));
            
            var directory = new FileInfo(path).Directory;
            
            Assert.IsNotNull(directory);
            
            // Ensure the directory exists
            directory.Create();

            // Save to a temp file to avoid corruption during serialiation
            var tempFile = new FileInfo(Path.Combine(Application.temporaryCachePath, Guid.NewGuid() + ".uttemp"));
            Serialization.FlatJson.BackEnd.Persist(tempFile.FullName, objects);

            // Overwrite in place
            var file = new FileInfo(path);
            File.Copy(tempFile.FullName, file.FullName, true);
            File.Delete(tempFile.FullName);
            
            return file.FullName;
        }

        private static bool s_DeletionDetected;
        private static readonly HashSet<string> s_ChangedAssetPaths = new HashSet<string>();
        private static readonly HashSet<string> s_MovedAssetPaths = new HashSet<string>();

        public static void MarkAssetChanged(string assetPath)
        {
            if (string.IsNullOrEmpty(assetPath))
            {
                return;
            }
            
            if (File.Exists(assetPath))
            {
                s_ChangedAssetPaths.Add(assetPath);
            }
            else
            {
                s_DeletionDetected = true;
            }
        }

        public static void MarkAssetMoved(string assetPath)
        {
            if (string.IsNullOrEmpty(assetPath))
            {
                return;
            }

            s_MovedAssetPaths.Add(assetPath);
        }

        public struct Changes
        {
            public bool changesDetected;
            public HashSet<string> createdSources;
            public HashSet<string> changedSources;
            public HashSet<string> deletedSources;
            public HashSet<string> movedSources;
        }

        /// <summary>
        /// Call this method as necessary to gather persistence changes in the given registry sources.
        /// </summary>
        /// <param name="registry">The registry to test the last changes against.</param>
        /// <param name="clearChanges">Set to false if you want to test the last state against multiple registries. Set to true when testing the last registry.</param>
        /// <returns>A RefreshResult struct. changesDetected will be true if any of the detected changes affect the given registry.</returns>
        public static Changes DetectChanges(IRegistry registry, bool clearChanges = true)
        {
            var result = new Changes
            {
                changesDetected = s_DeletionDetected || s_ChangedAssetPaths.Count > 0 || s_MovedAssetPaths.Count > 0
            };
            
            if (!result.changesDetected)
            {
                return result;
            }

            var createdSources = new HashSet<string>();
            var changedSources = new HashSet<string>();
            var deletedSources = new HashSet<string>();
            var movedSources = new HashSet<string>();
            
            // gather new modules even when not referenced in the registry
            foreach (var path in s_ChangedAssetPaths)
            {
                if (!path.EndsWith(ModuleFileExtension))
                {
                    continue;
                }

                if (File.Exists(path))
                {
                    changedSources.Add(AssetDatabase.AssetPathToGUID(path));
                }
            }
            
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            
            // capture any deleted assets
            foreach (var obj in registry.FindAllByType<IPersistentObject>().Where(obj => !string.IsNullOrEmpty(obj.PersistenceId)))
            {
                var path = AssetDatabase.GUIDToAssetPath(obj.PersistenceId);
                var asset = AssetDatabase.LoadMainAssetAtPath(path);
                
                if (!asset || null == asset)
                {
                    deletedSources.Add(obj.PersistenceId);
                }  
            }

            var objects = registry.FindAllByType<IPersistentObject>().ToList();

            foreach (var path in s_ChangedAssetPaths)
            {
                var guid = AssetDatabase.AssetPathToGUID(path);
                if (objects.Any(obj => string.Equals(obj.PersistenceId, guid)))
                {
                    changedSources.Add(guid);
                }
                else
                {
                    createdSources.Add(guid);
                }
            }

            foreach (var path in s_MovedAssetPaths)
            {
                var guid = AssetDatabase.AssetPathToGUID(path);
                movedSources.Add(guid);
            }

            result.createdSources = createdSources;
            result.changedSources = changedSources;
            result.deletedSources = deletedSources;
            result.movedSources = movedSources;

            if (clearChanges)
            {
                ClearChanges();
            }

            return result;
        }

        public static void ClearChanges()
        {
            s_DeletionDetected = false;
            s_ChangedAssetPaths.Clear();
            s_MovedAssetPaths.Clear();
        }

        public static void ReloadChanges(IRegistry registry, Changes changes)
        {
            if (false == changes.changesDetected)
            {
                return;
            }
            
            // How should we handle newly discovered objects
            /*
            foreach (var createdId in changes.createdSources)
            {
                var path = AssetDatabase.GUIDToAssetPath(createdId);
                
                if (string.IsNullOrEmpty(path))
                {
                    continue;
                }
                
                LoadJson(path, registry, createdId);
            }
            */

            foreach (var changedId in changes.changedSources)
            {
                var path = AssetDatabase.GUIDToAssetPath(changedId);
                
                if (string.IsNullOrEmpty(path))
                {
                    continue;
                }
                
                registry.UnregisterAllBySource(changedId);
                LoadJson(path, registry, changedId);
            }

            foreach (var deletedId in changes.deletedSources)
            {
                registry.UnregisterAllBySource(deletedId);
            }
        }
    }

    internal class UTPostProcessor : AssetPostprocessor
    {
        private static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets,
            string[] movedFromAssetPaths)
        {
            // scripted importers take care of importedAssets
            // we don't care about moved assets, only deleted ones
            foreach (var a in deletedAssets)
            {
                if (a.EndsWith(UTinyPersistence.ProjectFileExtension) ||
                    a.EndsWith(UTinyPersistence.ModuleFileExtension))
                {
                    UTinyPersistence.MarkAssetChanged(a);
                }
            }

            foreach (var a in movedAssets)
            {
                if (a.EndsWith(UTinyPersistence.ProjectFileExtension) ||
                    a.EndsWith(UTinyPersistence.ModuleFileExtension))
                {
                    UTinyPersistence.MarkAssetMoved(a);
                }
            }
        }
    }
}
#endif // NET_4_6
