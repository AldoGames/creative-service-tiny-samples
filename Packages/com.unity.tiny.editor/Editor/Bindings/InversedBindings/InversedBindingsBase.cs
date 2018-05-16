#if NET_4_6
using UnityEngine;
using UnityEngine.Assertions;

using Unity.Tiny.Conversions;

namespace Unity.Tiny
{
	public abstract class InversedBindingsBase<TComponent> : IInversedBindings<TComponent>
		where TComponent : Component
	{
		protected IRegistry Registry => UTinyEditorApplication.Registry;
		protected UTinyEntityGroupManager EntityGroupManager => UTinyEditorApplication.EntityGroupManager;

		public abstract void Create(UTinyEntityView view, TComponent from);

		public abstract UTinyType.Reference GetMainTinyType();

		public void Create(UTinyEntityView view, Component from)
		{
			var c = (TComponent) from;
			Assert.IsNotNull(c);
			Create(view, c);
		}

		protected static void AssignIfDifferent<TValue>(UTinyObject tiny, string propertyName, TValue value)
		{
			var current = tiny.GetProperty<TValue>(propertyName);
			if (!current.Equals(value))
			{
				tiny.AssignPropertyFrom(propertyName, value);
			}
		}

		protected static void AssignIfDifferent(UTinyObject tiny, string propertyName, Sprite value)
		{
			var current = tiny.GetProperty<Sprite>(propertyName);
			if (current != value)
			{
				tiny.AssignPropertyFrom(propertyName, value);
			}
		}

		protected static void AssignIfDifferent(UTinyObject tiny, string propertyName, AudioClip value)
		{
			var current = tiny.GetProperty<AudioClip>(propertyName);
			if (current != value)
			{
				tiny.AssignPropertyFrom(propertyName, value);
			}
		}

		protected static void AssignIfDifferent(UTinyObject tiny, string propertyName, Vector2 value)
		{
			var v = tiny[propertyName] as UTinyObject;
			AssignIfDifferent(v, "x", value.x);
			AssignIfDifferent(v, "y", value.y);
		}

		protected static void AssignIfDifferent(UTinyObject tiny, string propertyName, Vector3 value)
		{
			var v = tiny[propertyName] as UTinyObject;
			AssignIfDifferent(v, "x", value.x);
			AssignIfDifferent(v, "y", value.y);
			AssignIfDifferent(v, "z", value.z);
		}

		protected static void AssignIfDifferent(UTinyObject tiny, string propertyName, Vector4 value)
		{
			var v = tiny[propertyName] as UTinyObject;
			AssignIfDifferent(v, "x", value.x);
			AssignIfDifferent(v, "y", value.y);
			AssignIfDifferent(v, "z", value.z);
			AssignIfDifferent(v, "w", value.w);
		}

		protected static void AssignIfDifferent(UTinyObject tiny, string propertyName, Quaternion value)
		{
			var v = tiny[propertyName] as UTinyObject;
			AssignIfDifferent(v, "x", value.x);
			AssignIfDifferent(v, "y", value.y);
			AssignIfDifferent(v, "z", value.z);
			AssignIfDifferent(v, "w", value.w);
		}

		protected static void AssignIfDifferent(UTinyObject tiny, string propertyName, Rect value)
		{
			var v = tiny[propertyName] as UTinyObject;
			AssignIfDifferent(v, "x", value.x);
			AssignIfDifferent(v, "y", value.y);
			AssignIfDifferent(v, "width", value.width);
			AssignIfDifferent(v, "height", value.height);
		}
	}
}
#endif // NET_4_6
