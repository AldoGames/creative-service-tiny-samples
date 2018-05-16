#if NET_4_6
using System;
using System.IO;
using System.Text;
using Unity.Tiny.Extensions;

namespace Unity.Tiny.Jira
{
    internal static class JiraAPI
    {
        #region Constants
        public const string ProjectId = "12413";
        #endregion

        #region API

        public static bool CreateBugIssue(BugIssueData data, params string[] attachments)
        {
            var createIssueRequest = new CreateBugIssueRequest(data);
            var requestPayload = createIssueRequest.Data.GetDataAsJSon();
            
            var bugRequest = new FileInfo("bug-request.json");
            var bugArchive = new FileInfo(attachments[0]);
            var bugReportTool = new DirectoryInfo(UTinyBuildPipeline.GetToolDirectory("bugreport"));
            
            File.WriteAllText(bugRequest.FullName, requestPayload, Encoding.UTF8);

            try
            {
                return UTinyBuildUtilities.RunNode(bugReportTool, "index.js",
                    bugRequest.FullName.DoubleQuoted(),
                    bugArchive.FullName.DoubleQuoted());
            }
            finally
            {
                if (bugRequest.Exists)
                {
                    bugRequest.Delete();
                }
            }
        }
        #endregion
    }
}
#endif