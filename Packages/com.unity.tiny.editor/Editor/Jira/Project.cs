#if NET_4_6
namespace Unity.Tiny.Jira
{
    internal class Project : FieldContainer
    {
        public Project(string id) : base("project")
        {
            Add(new Field<string>("id", id));
        }
    }
}
#endif