#if NET_4_6
namespace Unity.Tiny
{
    public class UTinySystemTreeViewItem : UTinyTreeViewItem
    {
        public UTinySystem.Reference System { get; }
        public override string displayName => System.Dereference(Registry)?.Name ?? System.Name;

        public bool Schedule
        {
            get
            {
                var system = System.Dereference(Registry);
                return null != system && system.Enabled;
            }
            set
            {
                
                var system = System.Dereference(Registry);
                if (null != system)
                {
                    system.Enabled = value;
                }
            }
        }

        public UTinySystemTreeViewItem(IRegistry registry, UTinyModule.Reference mainModule, UTinyModule.Reference module, UTinySystem.Reference system) 
            : base(registry, mainModule, module)
        {
            System = system;
        }
    }
}
#endif // NET_4_6
