#if NET_4_6
namespace Unity.Tiny
{
    /// <summary>
    /// TypeId is used as an optimization when consuming data
    /// It is used to inform consumers of the type so the object can be created and loaded upfront
    /// </summary>
    public enum UTinyTypeId : ushort
    {
        Unknown = 0,
        Project = 1,
        Module = 2,
        Type = 3,
        Scene = 4,
        Entity = 5,
        Script = 6,
        System = 7,
        EnumReference = 8,
        EntityReference = 9,
        UnityObject = 10,
    }
}
#endif // NET_4_6
