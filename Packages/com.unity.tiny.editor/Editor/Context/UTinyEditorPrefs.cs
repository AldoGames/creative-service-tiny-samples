#if NET_4_6
using UnityEditor;

namespace Unity.Tiny
{
    public static class UTinyEditorPrefs
    {
        private const string KLastWorkspaceKey = "UTiny.Editor.Workspace.LastWorkspace";
        
        private static string GetWorkspaceKey(string persistenceId)
        {
            const string kWorkspaceKey = "UTiny.Editor.Workspace.{0}";
            return string.Format(kWorkspaceKey, persistenceId);
        }

        /// <summary>
        /// Sets the workspace
        /// </summary>
        /// <param name="workspace">Workspace to save</param>
        /// <param name="persistenceId">The persistenceId for this workspace</param>
        public static void SaveWorkspace(UTinyEditorWorkspace workspace, string persistenceId = null)
        {
            if (string.IsNullOrEmpty(persistenceId))
            {
                persistenceId = "Temp";
            }
            
            EditorPrefs.SetString(GetWorkspaceKey(persistenceId), workspace.ToJson());
            EditorPrefs.SetString(KLastWorkspaceKey, persistenceId);
        }

        /// <summary>
        /// Loads the workspace for the given id
        /// </summary>
        /// <returns>Workspace for the given Id or an empty workspace</returns>
        public static UTinyEditorWorkspace LoadWorkspace(string persistenceId)
        {
            var workspace = new UTinyEditorWorkspace();
            var json = EditorPrefs.GetString($"{GetWorkspaceKey(persistenceId)}", string.Empty);
            workspace.FromJson(json);
            return workspace;
        }

        /// <summary>
        /// Loads the last saved workspace
        /// </summary>
        public static UTinyEditorWorkspace LoadLastWorkspace()
        {
            var workspace = new UTinyEditorWorkspace();
            var persistenceId = EditorPrefs.GetString(KLastWorkspaceKey, string.Empty);
            var json = EditorPrefs.GetString(GetWorkspaceKey(persistenceId), string.Empty);
            workspace.FromJson(json);
            return workspace;
        }
    }
}
#endif // NET_4_6
