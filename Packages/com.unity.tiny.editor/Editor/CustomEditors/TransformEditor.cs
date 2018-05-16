#if NET_4_6
using Unity.Properties;

namespace Unity.Tiny
{
    public class TransformEditor : ComponentEditor
    {
        private static string[] s_Fields = new string[] { "localPosition", "localRotation", "localScale" };
        public override bool VisitComponent(UTinyObject tinyObject)
        {
            var customHandler = this as ICustomUIVisit<UTinyObject>;
            var visitContext = new VisitContext<UTinyObject> {Index = -1, Property = null};
            
            foreach (var fieldName in s_Fields)
            {
                var field = tinyObject[fieldName] as UTinyObject;
                visitContext.Value = field;
                customHandler.CustomUIVisit(ref field, ref visitContext);
            }
            return true;
        }
    }
}
#endif // NET_4_6
