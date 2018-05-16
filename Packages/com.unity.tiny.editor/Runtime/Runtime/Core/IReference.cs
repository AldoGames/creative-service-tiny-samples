#if NET_4_6
namespace Unity.Tiny
{
    public interface IReference : IIdentifiable<UTinyId>
    {
        string Name { get; }
    }

    public interface IReference<out T> : IReference where T : class
    {
        T Dereference(IRegistry registry);
    }
    
}
#endif // NET_4_6
