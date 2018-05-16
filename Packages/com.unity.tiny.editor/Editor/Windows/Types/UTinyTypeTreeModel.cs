#if NET_4_6
using System.Collections.Generic;
using System.Linq;

namespace Unity.Tiny
{
    /// <inheritdoc />
    /// <summary>
    /// Underlying data for the tree
    /// </summary>
    public class UTinyTypeTreeModel : UTinyTreeModel
    {
        public UTinyTypeTreeModel(IRegistry registry, UTinyModule.Reference mainModule) : base(registry, mainModule)
        {
            
        }

        /// <summary>
        /// Returns all Components and Structs in the project
        /// </summary>
        public List<UTinyType.Reference> GetTypes()
        {
            var types = new List<UTinyType.Reference>();

            foreach (var dependency in MainModule.Dereference(Registry).EnumerateDependencies())
            {
                types.AddRange(dependency.Components);
                types.AddRange(dependency.Structs);
                types.AddRange(dependency.Enums);
            }

            return types;
        }
        
        /// <summary>
        /// Returns all types that can be assigned to a field
        /// </summary>
        public List<UTinyType.Reference> GetAvailableFieldTypes()
        {
            var types = new List<UTinyType.Reference>();
                   
            // Include built in types
            foreach (var type in UTinyType.BuiltInTypes)
            {
                types.Add((UTinyType.Reference) type);
            }
            
            // Include any structs or enums
            foreach (var dependency in  MainModule.Dereference(Registry).EnumerateDependencies())
            {
                types.AddRange(dependency.Structs.Select(r => r.Dereference(Registry)).Where(t => null != t).Select(t=>(UTinyType.Reference) t));
                types.AddRange(dependency.Enums.Select(r => r.Dereference(Registry)).Where(t => null != t).Select(t=>(UTinyType.Reference) t));
            }

            return types;
        }
    }
}
#endif // NET_4_6
