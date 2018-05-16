#if NET_4_6
using System;

namespace Unity.Tiny
{
    public class UTinyModulePanel : UTinyPanel
    {
        [Serializable]
        public class State
        {
            public UTinyTreeState TreeState;
        }

        private IRegistry Registry { get; }
        private UTinyProject.Reference Project { get; }
        private UTinyModule.Reference MainModule { get; }

        public UTinyModulePanel(IRegistry registry, UTinyProject.Reference project, UTinyModule.Reference mainModule, State state)
        {
            Registry = registry;
            Project = project;
            MainModule = mainModule;
            
            if (null == state.TreeState)
            {
                state.TreeState = new UTinyTreeState();
            }

            // @TODO Find a way to move this to the base class
            state.TreeState.Init(UTinyModuleTreeView.CreateMultiColumnHeaderState());
            
            var treeView = new UTinyModuleTreeView(state.TreeState, new UTinyModuleTreeModel(Registry, Project, MainModule));

            // Add an empty toolbar for consistency
            var toolbar = new UTinyToolbar();
            toolbar.Add(new UTinyToolbar.Search
            {
                Alignment = UTinyToolbar.Alignment.Center,
                SearchString = treeView.SearchString,
                Changed = searchString =>
                {
                    treeView.SearchString = searchString;
                }
            });
            
            AddElement(toolbar);
            AddElement(treeView);
        }
    }
}
#endif // NET_4_6
