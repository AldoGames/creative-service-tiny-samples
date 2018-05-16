#if NET_4_6
namespace Unity.Tiny
{
    [System.Flags]
    public enum InspectMode
    {
        None = 0,
        Normal = 1,
        Debug = 2,
        DebugInternal = 4,
    }

    public enum InspectLocationType
    {
        Header = 0,
        Body = 1,
        Footer = 2
    }
}
#endif // NET_4_6
