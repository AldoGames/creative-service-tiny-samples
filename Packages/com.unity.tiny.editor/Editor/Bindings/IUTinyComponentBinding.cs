#if NET_4_6
namespace Unity.Tiny
{
    public enum BindingTiming
    {
        OnAddBindings = 0,
        OnUpdateBindings = 1,
        OnRemoveBindings = 2,
        OnAddComponent = 3,
        OnRemoveComponent = 4
    }

    public interface IComponentBinding
    {
        UTinyType.Reference TypeRef { get; }

        void Run(BindingTiming timing, UTinyEntity entity, UTinyObject component);
    }
}
#endif // NET_4_6
