#if NET_4_6
namespace Unity.Tiny.Jira
{
    // The values here represent the issuetype id of Jira tasks. Do not modify unless the Jira issuetype changed.
    // You can get a list of the issuetype ids by calling the GetAllIssueTypeIds method below.
    internal enum IssueTypeIds
    {
        // Invalid issue id
        Unknown = 0,
        // Standard Jira issue ids
        Bug = 1,
        NewFeature = 2,
        Task = 3,
        Improvement = 4,
        SubTask = 5
    }

    internal class IssueType : FieldContainer
    {
        public static readonly IssueType BugIssue    = new IssueType(IssueTypeIds.Bug);
        public static readonly IssueType NewFeature  = new IssueType(IssueTypeIds.NewFeature);
        public static readonly IssueType Task        = new IssueType(IssueTypeIds.Task);
        public static readonly IssueType Improvement = new IssueType(IssueTypeIds.Improvement);
        public static readonly IssueType SubTask     = new IssueType(IssueTypeIds.SubTask);

        public IssueType(IssueTypeIds type) : base("issuetype")
        {
            Add(new Field<int>("id", (int)type));
        }
    }
}
#endif