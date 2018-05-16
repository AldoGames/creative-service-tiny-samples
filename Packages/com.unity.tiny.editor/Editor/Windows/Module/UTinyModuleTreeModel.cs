#if NET_4_6
using System.Collections.Generic;
using System.Linq;

namespace Unity.Tiny
{
    /// <inheritdoc />
    /// <summary>
    /// Underlying data for the tree
    /// </summary>
    public class UTinyModuleTreeModel : UTinyTreeModel
    {
        public UTinyProject.Reference Project { get; }

        #region Public Methods

        public UTinyModuleTreeModel(IRegistry registry, UTinyProject.Reference project, UTinyModule.Reference mainModule) : base(registry, mainModule)
        {
            Project = project;
        }

        /// <summary>
        /// Returns a list of all project modules
        /// @TODO Optimization
        /// </summary>
        public IEnumerable<UTinyModule.Reference> GetModules()
        {
            var modules = Registry.FindAllByType<UTinyModule>();
            var mainModule = MainModule.Dereference(Registry);
            
            // @HACK
            var list = modules.Where(module => module.Name != "Main").Select(module => (UTinyModule.Reference) module).ToList();

            // Append all direct dependencies of our Main module that have not been found in the project (these will show up as missing)
            foreach (var dependency in mainModule.Dependencies)
            {
                var id = dependency.Id;

                if (list.All(m => m.Id != id))
                {
                    list.Add(dependency);
                }
            }

            // Append all direct dependencies for each module (these will show up as missing)
            foreach (var reference in list.ToArray())
            {
                var module = reference.Dereference(Registry);
                
                if (null == module)
                {
                    continue;
                }

                foreach (var dependency in module.Dependencies)
                {
                    var id = dependency.Id;

                    if (list.All(m => m.Id != id))
                    {
                        list.Add(dependency);
                    }
                }
            }

            if (!list.Contains(MainModule))
            {
                list.Add(MainModule);
            }

            for (var i = 0; i < list.Count; i++)
            {
                var module = list[i].Dereference(Registry);
                
                if (null == module)
                {
                    continue;
                }
                
                list[i] = (UTinyModule.Reference) module;
            }

            return list;
        }

        #endregion
    }
}
#endif // NET_4_6
