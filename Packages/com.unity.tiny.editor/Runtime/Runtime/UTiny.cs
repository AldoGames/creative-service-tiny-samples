#if NET_4_6
using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("Assembly-CSharp-Editor")]
namespace Unity.Tiny
{
    public static class UTinyConstants
    {
        private const string Sep = "/";

        public const string ApplicationName = "Tiny";
        public const string PackageName = "com.unity.tiny.editor";

        public const string PackageFolder = "Packages" + Sep + PackageName;
        public const string SamplesFolderName = "UTinySamples";
        public const string PackagePath = "Packages/" + PackageName + "/";

        public static class MenuItemNames
        {
            private const string Edit = "Edit";
            public const string CreateEntity           = ApplicationName + Sep + Edit + Sep + "Create Entity %#E";
            public const string CreateStaticEntity     = ApplicationName + Sep + Edit + Sep + "Create Entity (Static) %#R";
            public const string DuplicateSelection     = ApplicationName + Sep + Edit + Sep + "Duplicate Selection %#D";
            public const string DeleteSelection        = ApplicationName + Sep + Edit + Sep + "Delete Selection";

            private const string Window = "Window";
            public const string HierarchyWindow        = ApplicationName + Sep + Window + Sep + "Hierarchy";
            public const string InspectorWindow        = ApplicationName + Sep + Window + Sep + "Inspector";
            public const string EditorWindow           = ApplicationName + Sep + Window + Sep + "Editor";

            private const string Layout = "Layouts";
            public const string UTinyLayout            = ApplicationName + Sep + Layout + Sep + ApplicationName + " Mode";
            public const string UnityLayout            = "Window/Layouts" + Sep + ApplicationName + " Mode";

            private const string Help = "Help";
            public const string BugReportWindow        = ApplicationName + Sep + Help + Sep + "Report a Bug...";
        }

        public static class WindowNames
        {
            public const string HierarchyWindow    = ApplicationName + " Hierarchy";
            public const string InspectorWindow    = ApplicationName + " Inspector";
            public const string ProjectWindow      = ApplicationName + " Editor";
            public const string BugReportingWindow = ApplicationName + " Bug";
        }
    }
}
#endif // NET_4_6
