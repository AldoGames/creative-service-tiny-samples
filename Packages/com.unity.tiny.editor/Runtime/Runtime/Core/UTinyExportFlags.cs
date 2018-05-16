#if NET_4_6
namespace Unity.Tiny
{
    [System.Flags]
    public enum UTinyExportFlags
    {
        /// <summary>
        /// No special case
        /// </summary>
        None = 0,

        /// <summary>
        /// (no-op)
        /// This object is already included by the runtime
        /// During export is will be ignored
        /// </summary>
        RuntimeIncluded = 1
    }
}
#endif // NET_4_6
