#if NET_4_6
using System;

namespace Unity.Tiny
{
    [Serializable]
    public class UTinyCodeTreeState : UTinyTreeState
    {
        public bool FilterSystems = true;
        public bool FilterScripts = true;
        public bool FilterProjectOnly = true;
    }
}
#endif // NET_4_6
