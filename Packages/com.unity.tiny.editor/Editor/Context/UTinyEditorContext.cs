#if NET_4_6
using Unity.Properties;

namespace Unity.Tiny
{
    using ProjectProperty = Property<UTinyEditorContext, UTinyProject>;
    using RegistryProperty = Property<UTinyEditorContext, IRegistry>;
    using WorkspaceProperty = Property<UTinyEditorContext, UTinyEditorWorkspace>;

    public enum EditorContextType
    {
        /// <summary>
        /// No project is open
        /// </summary>
        None,
        
        /// <summary>
        /// The editor is setup to work on a user project
        /// </summary>
        Project,
        
        /// <summary>
        /// The editor is setup to work on standalone modules
        /// </summary>
        Module
    }

    public class UTinyEditorContext : IPropertyContainer
    {
        #region Static
        private static readonly ProjectProperty s_ProjectProperty = new ProjectProperty("Project",
            /* GET */ c => c.Project,
            /* SET */ null
        );

        private static readonly RegistryProperty s_RegistryProperty = new RegistryProperty("Registry",
            /* GET */ c => c.Registry,
            /* SET */ null
        );

        private static readonly WorkspaceProperty s_WorkspaceProperty = new WorkspaceProperty("Workspace",
            /* GET */ c => c.Workspace,
            /* SET */ null
        );

        private static readonly IPropertyBag s_PropertyBag = new PropertyBag(
            s_ProjectProperty, s_RegistryProperty, s_WorkspaceProperty
        );

        private UTinyProject.Reference m_Project;

        #endregion

        #region Properties
        private UTinyContext Context { get; }
        public UTinyProject Project => m_Project.Dereference(Registry);
        public UTinyModule Module => m_Project.Dereference(Registry)?.Module.Dereference(Registry);
        public UTinyRegistry Registry => Context.Registry;
        public UTinyCaretaker Caretaker => Context.Caretaker;
        public UTinyEditorWorkspace Workspace { get; }
        public UTinyUndo Undo { get; }
        public UTinyEntityGroupManager EntityGroupManager { get; }
        public EditorContextType ContextType { get; }
        public IPersistentObject PersistentObject => ContextType == EditorContextType.Project ? (IPersistentObject) Project : Module;
        #endregion

        public UTinyEditorContext(UTinyProject.Reference project, EditorContextType type, UTinyContext context, UTinyEditorWorkspace workspace)
        {
            m_Project = project;
            ContextType = type;
            Context = context ?? new UTinyContext(); 
            Workspace = workspace ?? new UTinyEditorWorkspace();
            Undo = new UTinyUndo(Registry, Caretaker);
            EntityGroupManager = new UTinyEntityGroupManager(this);
        }

        #region API
        
        internal void Load()
        {
            EntityGroupManager.Load();
        }
        internal void Unload()
        {
            Undo.Unload();
            EntityGroupManager.Unload();
        }
        #endregion

        #region IPropertyContainer
        public IVersionStorage VersionStorage => PassthroughVersionStorage.Instance;
        public IPropertyBag PropertyBag => s_PropertyBag;
        #endregion
    }
}
#endif // NET_4_6
