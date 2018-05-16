#if NET_4_6
namespace Unity.Tiny
{
    public enum BugPlatform
    {
        All             = 0 << 1,
        Windows         = 1 << 1,
        Mac             = 2 << 1,
        Linux           = 3 << 1,
        WindowsAndMac   = 4 << 1,
        WindowsAndLinux = 5 << 1,
        MacAndLinux     = 6 << 1,
    }
}
#endif