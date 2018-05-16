#if NET_4_6
using Unity.Properties;

namespace Unity.Tiny
{
    public class ComponentEditor : UTinyInspectorIMGUIVisitor
    {
        #region Properties
        protected UTinyEntity Target => Targets[0] as UTinyEntity;

        #endregion

        #region Implementation
        public virtual bool VisitComponent(UTinyObject tinyObject)
        {
            tinyObject.Properties.Visit(this);
            return true;
        }
        #endregion
    }
}
#endif // NET_4_6
