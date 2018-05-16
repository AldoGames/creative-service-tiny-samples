#if NET_4_6
using System;

namespace Unity.Tiny
{
    public class UTinyBuildReportPanel : UTinyPanel
    {
        [Serializable]
        public class State
        {
            public UTinyTreeState TreeState;
        }

        private IRegistry Registry { get; }

        private UTinyModule.Reference MainModule { get; }

        public UTinyBuildReportPanel(IRegistry registry, UTinyModule.Reference mainModule, State state)
        {
            Registry = registry;
            MainModule = mainModule;

            if (state.TreeState == null)
            {
                state.TreeState = new UTinyTreeState();
            }

            // @TODO Find a way to move this to the base class
            state.TreeState.Init(UTinyBuildReportTreeView.CreateMultiColumnHeaderState());

            var treeView = new UTinyBuildReportTreeView(state.TreeState, new UTinyBuildReportTreeModel(registry, mainModule));
            AddElement(treeView);
        }
    }
}
#endif // NET_4_6
