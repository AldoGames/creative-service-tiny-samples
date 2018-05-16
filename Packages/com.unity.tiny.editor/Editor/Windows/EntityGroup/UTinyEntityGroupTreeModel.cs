#if NET_4_6
using System.Collections.Generic;
using System.Linq;
using Unity.Tiny.Filters;

namespace Unity.Tiny
{
    public class UTinyEntityGroupTreeModel : UTinyTreeModel
    {
        public UTinyEntityGroupTreeModel(IRegistry registry, UTinyModule.Reference mainModule) : base(registry, mainModule)
        {
        }

        public List<UTinyEntityGroup.Reference> GetEntityGroups()
        {
            return MainModule.Dereference(Registry).EnumerateDependencies().SceneRefs().ToList();
        }
    }
}
#endif // NET_4_6
