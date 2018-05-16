#if NET_4_6
using System;

namespace Unity.Tiny
{
    [Serializable]
    public class UTinyTypeTreeState : UTinyTreeState
    {
        public bool FilterProjectOnly = true;
        public bool FilterComponents = true;
        public bool FilterStructs = true;
        public bool FilterEnums = true;
    }
}
#endif // NET_4_6
