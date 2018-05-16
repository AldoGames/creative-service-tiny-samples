#if NET_4_6
using System.IO;

namespace Unity.Tiny
{
    /// <summary>
    /// Underlying data for the tree
    /// </summary>
    public class UTinyBuildReportTreeModel : UTinyTreeModel
    {
        private static int m_IdCounter = 0;

        public int GetNewId
        {
            get { return m_IdCounter++; }
        }

        public UTinyBuildReportTreeModel(IRegistry registry, UTinyModule.Reference mainModule) : base(registry, mainModule)
        {
        }

        public UTinyBuildReport GetBuildReport()
        {
            m_IdCounter = 0;

            var buildDir = UTinyBuildPipeline.GetBuildDirectory(UTinyEditorApplication.Project, UTinyPlatform.HTML5, UTinyEditorApplication.EditorContext.Workspace.BuildConfiguration);
            var jsonFile = new FileInfo(Path.Combine(buildDir, "build-report.json"));
            if (jsonFile.Exists)
            {
                var json = File.ReadAllText(jsonFile.FullName);
                return UTinyBuildReport.FromJson(json);
            }

            return null;
        }
    }
}
#endif // NET_4_6
