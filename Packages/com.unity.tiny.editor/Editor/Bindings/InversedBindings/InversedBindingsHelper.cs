#if NET_4_6
using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using UnityEngine;

namespace Unity.Tiny
{
	public static class InversedBindingsHelper
	{
		private static readonly Dictionary<Type, IInversedBindings> s_InversedBindings = new Dictionary<Type, IInversedBindings>();

		public static IInversedBindings GetInversedBindings(Type type)
		{
			IInversedBindings bindings = null;
			s_InversedBindings.TryGetValue(type, out bindings);
			return bindings;
		}

		[UsedImplicitly]
		public static void Register<TComponent>(IInversedBindings<TComponent> inversedBindings) where TComponent : Component
		{
			var type = typeof(TComponent);
			if (s_InversedBindings.ContainsKey(type))
			{
				Debug.LogError($"{UTinyConstants.ApplicationName}: Inversed bindings for class {typeof(TComponent).Name} is already defined.");
			}
			else
			{
				s_InversedBindings[type] = inversedBindings;
			}
		}
	}
}
#endif // NET_4_6
