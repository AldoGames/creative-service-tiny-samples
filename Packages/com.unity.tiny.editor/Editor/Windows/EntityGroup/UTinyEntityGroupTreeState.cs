#if NET_4_6
using System;

namespace Unity.Tiny
{
    [Serializable]
    public class UTinyEntityGroupTreeState : UTinyTreeState
    {
        public bool FilterProjectOnly = true;
    }
}
#endif // NET_4_6
