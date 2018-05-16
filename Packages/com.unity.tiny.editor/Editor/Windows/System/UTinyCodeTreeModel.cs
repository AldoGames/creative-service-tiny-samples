#if NET_4_6
using System.Collections.Generic;
using System.Linq;
using Unity.Tiny.Filters;

namespace Unity.Tiny
{
    public class UTinyCodeTreeModel : UTinyTreeModel
    {
        public UTinyCodeTreeModel(IRegistry registry, UTinyModule.Reference mainModule) : base(registry, mainModule)
        {
        }
        
        public IEnumerable<UTinySystem.Reference> GetSystems()
        {
            var module = MainModule.Dereference(Registry);
            return module.EnumerateDependencies().SystemRefs().ToList();
        }
        
        public List<UTinyScript.Reference> GetScripts()
        {
            var module = MainModule.Dereference(Registry);
            return module.EnumerateDependencies().ScriptRefs().ToList();
        }
    }
}
#endif // NET_4_6
