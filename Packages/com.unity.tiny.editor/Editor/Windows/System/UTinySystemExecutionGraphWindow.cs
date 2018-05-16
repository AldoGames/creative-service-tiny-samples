#if NET_4_6
using UnityEditor;
using UnityEngine;

namespace Unity.Tiny
{
	public class UTinySystemExecutionGraphWindow : EditorWindow
	{
		private Vector2 m_Scroll;
		private int m_ModuleVersion;

		public IRegistry Registry { get; set; }
		public UTinyModule.Reference Module { get; set; }

		public static UTinySystemExecutionGraphWindow Instance { get; private set; }

		private void OnEnable()
		{
			Instance = this;

			EditorApplication.update += Update;
		}

		private void OnDisable()
		{
			if (Instance == this)
			{
				Instance = null;
			}

			EditorApplication.update -= Update;
		}
		
		private void Update()
		{
			if (null == Registry)
			{
				return;
			}
			
			var module = Module.Dereference(Registry);

			if (null == module)
			{
				return;
			}

			if (m_ModuleVersion != module.Version)
			{
				Repaint();
				m_ModuleVersion = module.Version;
			}
		}

		private void OnGUI()
		{
			if (null == Registry)
			{
				return;
			}
			
			var module = Module.Dereference(Registry);

			using (var scroll = new GUILayout.ScrollViewScope(m_Scroll, UTinyStyles.ListBackground))
			{
				m_Scroll = scroll.scrollPosition;

				foreach (var reference in module.GetSystemExecutionOrder())
				{
					var system = reference.Dereference(Registry);
					
					using (new GUIColorScope(system != null ? Color.white : Color.red))
					using (new GUILayout.HorizontalScope(UTinyStyles.ListBackground))
					{
						using (new GUIEnabledScope(system.Enabled))
						{
							GUILayout.Label(system?.Name ?? reference.Name, EditorStyles.boldLabel, GUILayout.Height(16));
						}
					}
				}
			}
		}
	}
}
#endif // NET_4_6
