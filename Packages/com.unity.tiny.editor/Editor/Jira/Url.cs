#if NET_4_6
namespace Unity.Tiny.Jira
{
    internal struct Url
    {
        public readonly string Value;

        public Url(string url)
        {
            Value = url;
        }
    }
}
#endif