#if NET_4_6
using UnityEngine;

namespace Unity.Tiny
{
	public interface IInversedBindings
	{
		UTinyType.Reference GetMainTinyType();
		void Create(UTinyEntityView view, Component @from);
	}

	public interface IInversedBindings<TComponent> : IInversedBindings
		where TComponent : Component
	{
		void Create(UTinyEntityView view, TComponent component);
	}
}
#endif // NET_4_6
