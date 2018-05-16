#if NET_4_6
using UnityEditor;

namespace Unity.Tiny
{
    internal class UTinyBugReportUnpacker
    {

        [InitializeOnLoadMethod]
        private static void Register()
        {
            EditorApplication.delayCall += TryUnpackBugReport;
        }

        private static void TryUnpackBugReport()
        {
            var path = UTinyBugReportWindow.k_BugPackagePath;
            var guid = AssetDatabase.AssetPathToGUID(path);
            if (!string.IsNullOrEmpty(guid))
            {
                UTinyBuildPipeline.InstallSamples(false);
                AssetDatabase.ImportPackage(path, false);
                AssetDatabase.DeleteAsset(path);
            }
        }
    }
}
#endif
