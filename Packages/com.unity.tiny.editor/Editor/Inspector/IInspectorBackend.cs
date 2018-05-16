#if NET_4_6
using System.Collections.Generic;
using Unity.Properties;

namespace Unity.Tiny
{
    public interface IInspectorBackend
    {
        InspectMode Mode { get; set; }
        List<IPropertyContainer> Targets { get; set; }

        bool Locked { get; set; }

        void OnGUI();

        void Build();
    }
}
#endif // NET_4_6
