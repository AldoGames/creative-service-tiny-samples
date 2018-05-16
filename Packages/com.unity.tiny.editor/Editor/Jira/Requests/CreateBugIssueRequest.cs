#if NET_4_6
using System.Text;
using Unity.Properties;
using UnityEditor;

using Unity.Tiny.Extensions;

namespace Unity.Tiny.Jira
{
    internal struct BugIssueData
    {
        public const string NewLine = "\\n";
        public string Title;
        public string Description;
        public string ReproSteps;
        public string Email;
        public BugOccurrence Occurrence;
        public BugPlatform Platform;

        public string GetDataString()
        {
            var builder = new StringBuilder();
            builder.Append($"*Occurrence:* {Occurrence}{NewLine}{NewLine}");
            builder.Append($"*Platform(s) affected:* {ObjectNames.NicifyVariableName(Platform.ToString())}{NewLine}{NewLine}");
            builder.Append($"*Bug Description:*{NewLine}{"quote".Braced()}");
            builder.Append($"{Description}{"quote".Braced()}{NewLine}{NewLine}");
            builder.Append($"*Repro Steps:*{NewLine}{"quote".Braced()}");
            builder.Append($"{ReproSteps}{"quote".Braced()}{NewLine}{NewLine}");
            builder.Append($"*User contact:* {Email}{NewLine}");
            return builder.ToString();
        }
    }
    
    internal class CreateBugIssueRequest : IRequest
    {
        public const string NewLine = "\\n";

        private readonly JiraDataContainer m_Data = new JiraDataContainer();
        protected static IProperty<CreateBugIssueRequest, JiraDataContainer> s_DataProperty = new ContainerProperty<CreateBugIssueRequest, JiraDataContainer>("data",
            /*GET*/ p => p.m_Data,
            /*SET*/ null);

        public CreateBugIssueRequest(BugIssueData data)
        {
            Data.Add(new Project(JiraAPI.ProjectId));
            AddField("summary", data.Title);
            AddField("description", data.GetDataString());
            Data.Add(IssueType.BugIssue);
        }

        private static PropertyBag s_Bag = new PropertyBag(s_DataProperty);

        public IVersionStorage VersionStorage => PassthroughVersionStorage.Instance;
        public IPropertyBag PropertyBag => s_Bag;

        public JiraDataContainer Data => s_DataProperty.GetValue(this);

        private void AddField(string name, string value)
        {
            if (string.IsNullOrEmpty(name) || string.IsNullOrEmpty(value))
            {
                return;
            }

            value = value.Replace("\n", NewLine);
            Data.Add(new Field<string>(name, value));
        }
    }
}
#endif
