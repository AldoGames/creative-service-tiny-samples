#if NET_4_6
using JetBrains.Annotations;

using UnityEditor;
using UnityEngine;

using Unity.Tiny.Extensions;

namespace Unity.Tiny
{
	[UsedImplicitly]
	public class TextMeshInversedBindings : InversedBindingsBase<TextMesh>
	{
		#region Static
		[InitializeOnLoadMethod]
		private static void Register()
		{
			GameObjectTracker.RegisterForComponentModification<TextMesh>(SyncTextMesh);
		}

		public static void SyncTextMesh(TextMesh from, UTinyEntityView view)
		{
			var registry = view.Registry;
			var entity = view.EntityRef.Dereference(registry);

			var tetRenderer = entity.GetComponent(registry.GetTextRendererType());
			if (null != tetRenderer)
			{
				SyncTextMesh(from, tetRenderer);
			}
		}

		public static void SyncTextMesh(TextMesh from, [NotNull] UTinyObject textRenderer)
		{
			from.GetComponent<MeshRenderer>().sharedMaterial = new Material(Shader.Find("GUI/Text Shader"));
			from.characterSize = 10;
			from.lineSpacing = 1;
			from.richText = false;
			from.alignment = TextAlignment.Left;

			AssignIfDifferent(textRenderer, "text", from.text);
			AssignIfDifferent(textRenderer, "fontSize", from.fontSize);
			AssignIfDifferent(textRenderer, "bold", (from.fontStyle & FontStyle.Bold) == FontStyle.Bold);
			AssignIfDifferent(textRenderer, "italic", (from.fontStyle & FontStyle.Italic) == FontStyle.Italic);
			AssignIfDifferent(textRenderer, "color", from.color);
			AssignIfDifferent(textRenderer, "font", from.font);
			AssignIfDifferent(textRenderer, "anchor", from.anchor);
		}

		#endregion

		#region InversedBindingsBase<TextMesh>
		public override void Create(UTinyEntityView view, TextMesh from)
		{
			var tr = new UTinyObject(Registry, GetMainTinyType());
			SyncTextMesh(from, tr);

			var entity = view.EntityRef.Dereference(Registry);
			var textRenderer = entity.GetOrAddComponent(GetMainTinyType());
			textRenderer.CopyFrom(tr);
			BindingsHelper.RunBindings(entity, textRenderer);
		}

		public override UTinyType.Reference GetMainTinyType()
		{
			return Registry.GetTextRendererType();
		}
		#endregion
	}
}
#endif // NET_4_6
