using UnityEditor;

namespace Unity.Tiny.Configuration
{
	public static class SetupProjectSettings
	{
		[InitializeOnLoadMethod]
		public static void Setup()
		{
			EditorApplication.delayCall += () =>
			{
				if (EditorApplication.isPlaying)
				{
					return;
				}

				if (PlayerSettings.scriptingRuntimeVersion != ScriptingRuntimeVersion.Latest)
				{
					if (EditorUtility.DisplayDialog(
						"Reimport required",
						"Tiny Editor requires .Net runtime version 4. Do you want to change this Player setting and re-import the current project?",
						"Yes", "No"))
					{
						// change runtime version
						PlayerSettings.scriptingRuntimeVersion = ScriptingRuntimeVersion.Latest;
						
						// purge
						EditorApplication.ExecuteMenuItem("Assets/Reimport All");
					}
				}

			};
		}
	}
}
