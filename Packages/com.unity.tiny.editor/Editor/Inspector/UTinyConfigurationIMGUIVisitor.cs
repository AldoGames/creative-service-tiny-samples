#if NET_4_6
using System.Linq;
using Unity.Properties;
using UnityEditor;
using UnityEngine;

namespace Unity.Tiny
{
    public class UTinyConfigurationIMGUIVisitor : UTinyInspectorIMGUIVisitor
    {
        public override bool ValidateTarget(IPropertyContainer container)
        {
            var entity = (container as UTinyConfigurationViewer)?.Entity;
            return null != entity && null != Registry.FindById<UTinyEntity>(entity.Id);
        }

        protected override void OnPrepare()
        {
            base.OnPrepare();
            var viewers = Targets.Cast<UTinyConfigurationViewer>();

            // ReSharper disable once PossibleMultipleEnumeration
            foreach (var viewer in viewers)
            {
                viewer.Entity?.Refresh();
            }
        }

        protected override void OnComplete()
        {
        }

        public override void Header()
        {
            EditorGUILayout.LabelField("(Configurations)", EditorStyles.boldLabel, GUILayout.Height(25.0f));
            UTinyGUILayout.Separator(UTinyColors.Inspector.Separator, UTinyGUIUtility.ComponentHeaderSeperatorHeight);
        }
        
        public override void Visit()
        {
            var entity = Targets.Cast<UTinyConfigurationViewer>().FirstOrDefault()?.Entity;

            if (null == entity)
            {
                return;
            }

            IPropertyContainer c = entity;
            
            c.Visit(this);
        }
    }
}
#endif // NET_4_6
