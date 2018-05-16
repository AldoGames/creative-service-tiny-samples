#if NET_4_6
using UnityEditor.IMGUI.Controls;

namespace Unity.Tiny
{
    public class UTinyTreeViewItem : TreeViewItem
    {
        /// <summary>
        /// Registry this item belongs to
        /// </summary>
        public IRegistry Registry { get; }
        
        /// <summary>
        /// The root module for this item
        /// </summary>
        public UTinyModule.Reference MainModule { get; }
        
        /// <summary>
        /// The module that this item resides in
        /// </summary>
        public UTinyModule.Reference Module { get; }
        
        public bool Editable { get; set; }

        protected UTinyTreeViewItem(IRegistry registry, UTinyModule.Reference mainModule, UTinyModule.Reference module)
        {
            Registry = registry;
            MainModule = mainModule;
            Module = module;
        }
    }
}
#endif // NET_4_6
