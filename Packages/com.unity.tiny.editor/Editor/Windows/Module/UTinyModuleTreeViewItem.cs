#if NET_4_6
using System.Linq;

namespace Unity.Tiny
{
    /// <summary>
    /// Item representing a module in the TreeView
    /// </summary>
    public class UTinyModuleTreeViewItem : UTinyTreeViewItem
    {
        #region Types

        public enum StatusType
        {
            Self = 0,
            Excluded = 1,
            IncludedRequired = 2,
            IncludedImplicit = 3,
            IncludedExplicit = 4
        }

        #endregion

        #region Fields

        private bool m_Included;

        #endregion

        #region Properties

        /// <summary>
        /// Is this module explicitly included as a dependency for the Main module
        /// </summary>
        public bool Included
        {
            get
            {
                return Module.Equals(MainModule) || m_Included;
            }
            set
            {
                if (Module.Equals(MainModule))
                {
                    return;
                }

                if (m_Included != value)
                {
                    var mainModule = MainModule.Dereference(Registry);
                    if (value)
                    {
                        mainModule.AddExplicitModuleDependency(Module);
                    }
                    else
                    {
                        mainModule.RemoveExplicitModuleDependency(Module);
                    }
                }

                m_Included = value;
            }
        }

        public StatusType Status
        {
            get
            {
                if (Module.Equals(MainModule))
                {
                    return StatusType.Self;
                }

                var mainModule = MainModule.Dereference(Registry);
                var module = Module.Dereference(Registry);
                
                if (null != module && module.IsRequired)
                {
                    return StatusType.IncludedRequired;
                }
                return Included
                    ? StatusType.IncludedExplicit
                    : mainModule.EnumerateRefDependencies().Contains(Module)
                        ? StatusType.IncludedImplicit
                        : StatusType.Excluded;
            }
        }
        
        public override string displayName => Module.Dereference(Registry)?.Name ?? Module.Name;

        #endregion

        #region Public Methods

        public UTinyModuleTreeViewItem(IRegistry registry, UTinyModule.Reference mainModule, UTinyModule.Reference module) : base(registry, mainModule, module)
        {
            m_Included = mainModule.Dereference(registry).ContainsExplicitModuleDependency(module);
        }

        #endregion
    }
}
#endif // NET_4_6
