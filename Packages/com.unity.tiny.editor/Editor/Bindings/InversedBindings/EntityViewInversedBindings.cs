#if NET_4_6
using JetBrains.Annotations;

namespace Unity.Tiny
{
    [UsedImplicitly]
    public class EntityViewInversedBindings : InversedBindingsBase<UTinyEntityView>
    {
        #region InversedBindingsBase<UTinyEntityView>
        public override void Create(UTinyEntityView view, UTinyEntityView @from)
        {
            // Nothing to do..
        }

        public override UTinyType.Reference GetMainTinyType()
        {
            return UTinyType.Reference.None;
        }
        #endregion
    }
}
#endif // NET_4_6
