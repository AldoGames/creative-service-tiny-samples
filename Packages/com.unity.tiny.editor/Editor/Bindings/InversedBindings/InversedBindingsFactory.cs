#if NET_4_6
using System;
using System.Linq;

using UnityEditor;

namespace Unity.Tiny
{
	internal static class InversedBindingsFactory
	{
		[InitializeOnLoadMethod]
		public static void RegisterConverters()
		{
			var assembly = typeof(InversedBindingsFactory).Assembly;
			var creatorType = typeof(IInversedBindings<>);

			foreach (var type in assembly.GetTypes())
			{
				if (type.IsAbstract || type.ContainsGenericParameters)
				{
					continue;
				}

				Register(type, creatorType);
			}
		}

		private static void Register(Type type, Type converterType)
		{
			var converter = type.GetInterfaces()
				.FirstOrDefault(t => t.IsGenericType && t.GetGenericTypeDefinition() == converterType);

			if (null == converter)
			{
				return;
			}

			var componentType = converter.GetGenericArguments()[0];
			var componentCreator = typeof(InversedBindingsHelper);
			var registerMethod = componentCreator.GetMethod("Register");
			var genericMethod = registerMethod?.MakeGenericMethod(componentType);

			if (null != genericMethod)
			{
				genericMethod.Invoke(null, new [] {Activator.CreateInstance(type)});
			}
		}
	}
}
#endif // NET_4_6
