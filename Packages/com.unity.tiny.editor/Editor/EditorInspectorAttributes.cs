#if NET_4_6
namespace Unity.Tiny.Attributes
{
    public static class EditorInspectorAttributes
    {
        public class CustomDrawerAttribute : IPropertyAttribute
        {
            public StructDrawer Visitor { get; set; }
        }

        public class CustomComponentEditor : IPropertyAttribute
        {
            public ComponentEditor Visitor { get; set; }
        }

        public class BindingsAttribute : IPropertyAttribute
        {
            public IComponentBinding Binding { get; set; }
        }

        public static CustomDrawerAttribute CustomDrawer(StructDrawer visitor) { return new CustomDrawerAttribute { Visitor = visitor }; }
        public static CustomComponentEditor CustomEditor(ComponentEditor visitor) { return new CustomComponentEditor { Visitor = visitor }; }
        public static BindingsAttribute Bindings(IComponentBinding binding) { return new BindingsAttribute { Binding = binding }; }

    }
}
#endif // NET_4_6
