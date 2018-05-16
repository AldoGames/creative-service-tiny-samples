#if NET_4_6
using System;
using System.IO;

using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.SceneManagement;

using UnityEditor;
using UnityEditor.SceneManagement;

namespace Unity.Tiny
{
    public static class HierarchyHelper
    {
        #region Constants
        private const string k_UTinyTempFolderName = "UTinyTemp";
        private const string k_SceneScratchPadName = "UTinyProject-DoNotEdit.unity";
        #endregion

        #region API
        public static bool GetOrGenerateScratchPad(UTinyEditorContext context) 
        {
            Assert.IsNotNull(context, $"{UTinyConstants.ApplicationName}: Trying to generate a scratch pad from a null editor context.");
            Assert.IsNotNull(context.Project, $"{UTinyConstants.ApplicationName}: Trying to generate a scratch pad from an editor context without any project.");

            var directory = GetOrCreateContextDirectory(context);
            var path = GetScratchPadPath(directory);
            var scene = SceneManager.GetSceneByPath(path);
            if (scene.isLoaded && scene.IsValid())
            {
                return true;
            }
            
            AssetDatabase.ImportAsset(Path.GetDirectoryName(path));
            scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            return EditorSceneManager.SaveScene(scene, path);
        }

        public static bool ReleaseScratchPad(UTinyEditorContext context)
        {
            Assert.IsNotNull(context, $"{UTinyConstants.ApplicationName}: Trying to release a null editor context.");
            Assert.IsNotNull(context.Project, $"{UTinyConstants.ApplicationName}: Trying to release an editor context without any project.");

            var directory = GetOrCreateContextDirectory(context);
            var scenePath = SceneManager.GetSceneByPath(GetScratchPadPath(directory));
            if (!scenePath.IsValid())
            {
                return false;
            }

            if (!DestroyScratchPad(directory))
            {
                return false;
            }

            DestroyDirectory(directory);
            return true;
        }

        public static Scene GetScratchPad(UTinyEditorContext context)
        {
            Assert.IsNotNull(context, $"{UTinyConstants.ApplicationName}: Trying to get the scratch pad of a null editor context.");
            Assert.IsNotNull(context.Project, $"{UTinyConstants.ApplicationName}: Trying to get the scratch pad of an editor context without any project.");

            var scenePath = GetScratchPadPath(GetOrCreateContextDirectory(context));
            return SceneManager.GetSceneByPath(scenePath);
        }
        #endregion

        #region Implementation
        private static DirectoryInfo GetOrCreateContextDirectory(UTinyEditorContext context)
        {
            string directory;
            // [MP] @TODO: Get the actual folder where the project/solution is stored.
            if (context.ContextType == EditorContextType.Project)
            {
                directory = UTinyPersistence.GetLocation(context.Project);
            }
            else
            {
                directory = UTinyPersistence.GetLocation(context.Module);
            }

            return Directory.CreateDirectory(Path.Combine(Path.GetDirectoryName(directory ?? "Assets/"), k_UTinyTempFolderName));
        }

        private static string GetScratchPadPath(DirectoryInfo folder)
        {
            var folderPath = new Uri(folder.FullName);
            var assetPath = new Uri(Application.dataPath);
            return Path.Combine(assetPath.MakeRelativeUri(folderPath).ToString(), k_SceneScratchPadName).Replace("\\", "/");
        }

        private static bool DestroyScratchPad(DirectoryInfo folder)
        {
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            return AssetDatabase.DeleteAsset(GetScratchPadPath(folder));
        }

        private static void DestroyDirectory(DirectoryInfo folder)
        {
            var folderPath = new Uri(folder.FullName);
            var assetPath = new Uri(Application.dataPath);
            AssetDatabase.DeleteAsset(assetPath.MakeRelativeUri(folderPath).ToString());
        }
        #endregion
    }
}
#endif // NET_4_6
