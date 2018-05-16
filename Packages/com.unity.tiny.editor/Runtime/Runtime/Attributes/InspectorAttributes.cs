#if NET_4_6
namespace Unity.Tiny.Attributes
{
    public static class InspectorAttributes
    {
        public class HideInInspectorAttribute : IPropertyAttribute { }
        public class ReadonlyAttribute : IPropertyAttribute { }
        public class TooltipAttribute : IPropertyAttribute
        {
            public string Text { get; set; } = string.Empty;
        }
        public class VisibilityAttribute : IPropertyAttribute
        {
            public InspectMode Mode { get; set; } = InspectMode.Normal;
        }

        public class HeaderAttribute : IPropertyAttribute
        {
            public string Text { get; set; } = string.Empty;
        }


        public class DontListAttribute : IPropertyAttribute { }

        public static readonly HideInInspectorAttribute HideInInspector = new HideInInspectorAttribute();
        public static readonly ReadonlyAttribute Readonly = new ReadonlyAttribute();
        public static TooltipAttribute Tooltip(string text) { return new TooltipAttribute { Text = text }; }
        public static VisibilityAttribute Visibility(InspectMode mode) { return new VisibilityAttribute{ Mode = mode }; }
        public static HeaderAttribute Header(string text) { return new HeaderAttribute { Text = text }; }
        public static readonly DontListAttribute DontList = new DontListAttribute();
    }
}
#endif // NET_4_6
