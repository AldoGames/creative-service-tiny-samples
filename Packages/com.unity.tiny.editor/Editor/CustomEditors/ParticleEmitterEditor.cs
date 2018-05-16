#if NET_4_6
using System;
using System.Collections.Generic;
using System.Linq;

using UnityEditor;
using UnityEngine;

using Unity.Tiny.Extensions;

namespace Unity.Tiny
{
    public class ParticleEmitterEditor : ComponentEditor
    {
        private class ComponentInfo
        {
            public string ButtonName;
            public Func<IRegistry, UTinyType.Reference> GetTypeRef;
            public bool Enabled;

            public ComponentInfo(string name, Func<IRegistry, UTinyType.Reference> getType, bool enabled = true)
            {
                ButtonName = name;
                GetTypeRef = getType;
                Enabled = enabled;
            }
        }

        private static readonly List<ComponentInfo> s_SourceComponents = new List<ComponentInfo>
        {
            new ComponentInfo( "Box", r => r.GetEmitterBoxSourceType())
        };

        private static readonly List<ComponentInfo> s_InitialValuesComponents = new List<ComponentInfo>
        {
            new ComponentInfo("Size", r => r.GetEmitterInitialScaleType()),
            new ComponentInfo("Rotation", r => r.GetEmitterInitialRotationType()),
            new ComponentInfo("Velocity", r => r.GetEmitterInitialVelocityType(), false)
        };

        private static readonly List<ComponentInfo> s_LifetimeValuesComponents = new List<ComponentInfo>
        {
            new ComponentInfo("Color", r => r.GetLifetimeColorType()),
            new ComponentInfo("Alpha", r => r.GetLifetimeAlphaType()),
            new ComponentInfo("Scale", r => r.GetLifetimeScaleType()),
            new ComponentInfo("Rotation", r => r.GetLifetimeRotationType()),
            new ComponentInfo("Velocity", r => r.GetLifetimeVelocityType(), false)
        };

        public override bool VisitComponent(UTinyObject tinyObject)
        {
            try
            {
                return base.VisitComponent(tinyObject);
            }
            finally
            {
                if (TargetType == typeof(UTinyEntity))
                {
                    ShowSection("Source", s_SourceComponents);
                    ShowSection("Initial Values", s_InitialValuesComponents);
                    ShowSection("Lifetime Values", s_LifetimeValuesComponents);
                }
            }
        }

        private bool AnyComponentMissing(IEnumerable<ComponentInfo> types)
        {
            foreach(var type in types)
            {
                if (null == Target.GetComponent(type.GetTypeRef(Registry)))
                {
                    return true;
                }
            }

            return false;
        }

        private void ShowSection(string sectionName, List<ComponentInfo> relatedComponents)
        {
            if (AnyComponentMissing(relatedComponents))
            {
                EditorGUILayout.BeginHorizontal();
                GUILayout.Space(15.0f);
                EditorGUILayout.BeginVertical(GUI.skin.box);
                EditorGUILayout.LabelField(sectionName);
                foreach (var related in relatedComponents)
                {
                    AddMissingComponent(related);
                }
                EditorGUILayout.EndVertical();
                EditorGUILayout.EndHorizontal();
            }
        }

        private void AddMissingComponent(ComponentInfo info)
        {
            var enabled = GUI.enabled;
            GUI.enabled &= info.Enabled;
            if (null == Target.GetComponent(info.GetTypeRef(Registry)))
            {
                EditorGUILayout.BeginHorizontal();
                GUILayout.Space(15.0f);
                if (GUILayout.Button("+", GUILayout.Width(20)))
                {
                    foreach (var entity in Targets.Cast<UTinyEntity>())
                    {
                        entity.GetOrAddComponent(info.GetTypeRef(Registry));
                    }
                }
                EditorGUILayout.LabelField(info.ButtonName);

                EditorGUILayout.EndHorizontal();
            }
            GUI.enabled = enabled;
        }
    }
}
#endif // NET_4_6
