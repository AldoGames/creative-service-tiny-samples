#if NET_4_6
namespace Unity.Tiny
{
    public interface IIdentifiable<out T>
    {
        T Id { get; }
    }
}
#endif // NET_4_6
