#if NET_4_6
using System.Collections.Generic;
using Unity.Properties;
using Unity.Tiny.Serialization;
using Unity.Tiny.Serialization.FlatJson;

namespace Unity.Tiny
{
    using EntityGroupContainerListProperty = MutableContainerListProperty<UTinyEditorWorkspace, IList<UTinyEntityGroup.Reference>, UTinyEntityGroup.Reference>;
    using EntityGroupProperty = Property<UTinyEditorWorkspace, UTinyEntityGroup.Reference>;
    using BuildConfigurationProperty = EnumProperty<UTinyEditorWorkspace, UTinyBuildConfiguration>;
    using BoolProperty = Property<UTinyEditorWorkspace, bool>;

    public class UTinyEditorWorkspace : IPropertyContainer, IVersionStorage, IVersioned
    {
        #region Static
        
        private static readonly EntityGroupContainerListProperty s_OpenedEntityGroupsProperty = new EntityGroupContainerListProperty("OpenedEntityGroups",
            /* GET */ c => c.m_OpenedScenes ?? (c.m_OpenedScenes = new List<UTinyEntityGroup.Reference>()),
            /* SET */ null
        );

        private static readonly EntityGroupProperty s_ActiveEntityGroupProperty = new EntityGroupProperty("ActiveEntityGroup",
            /* GET */ c => c.m_ActiveScene,
            /* SET */ (c, v) => c.m_ActiveScene = v
        );
        
        private static readonly BuildConfigurationProperty s_BuildConfigurationProperty = new BuildConfigurationProperty("BuildConfiguration",
                c => c.m_BuildConfiguration,
                (c, v) => c.m_BuildConfiguration = v);
        
        private static readonly BoolProperty s_PreviewProperty 
            = new BoolProperty("Preview",
                c => c.m_Preview,
                (c, v) => c.m_Preview = v);

        private static readonly IPropertyBag s_PropertyBag = new PropertyBag(
            s_OpenedEntityGroupsProperty,
            s_ActiveEntityGroupProperty,
            s_BuildConfigurationProperty,
            s_PreviewProperty
        );

        #endregion

        #region Fields

        private string m_Target;
        private List<UTinyEntityGroup.Reference> m_OpenedScenes = new List<UTinyEntityGroup.Reference>();
        private UTinyEntityGroup.Reference m_ActiveScene;
        private UTinyBuildConfiguration m_BuildConfiguration = UTinyBuildConfiguration.Development;
        private bool m_Preview = true;

        #endregion

        #region Properties

        public IList<UTinyEntityGroup.Reference> OpenedEntityGroups => s_OpenedEntityGroupsProperty.GetValue(this);

        public UTinyEntityGroup.Reference ActiveEntityGroup
        {
            get { return s_ActiveEntityGroupProperty.GetValue(this); }
            set { s_ActiveEntityGroupProperty.SetValue(this, value); }
        }

        /// <summary>
        /// Build configuration for the project
        /// </summary>
        public UTinyBuildConfiguration BuildConfiguration
        {
            get { return s_BuildConfigurationProperty.GetValue(this); }
            set { s_BuildConfigurationProperty.SetValue(this, value); }
        }
        
        public bool Preview
        {
            get { return s_PreviewProperty.GetValue(this); }
            set { s_PreviewProperty.SetValue(this, value); }
        }

        #endregion

        #region API

        public void AddOpenedEntityGroup(UTinyEntityGroup.Reference entityGroup)
        {
            if (!s_OpenedEntityGroupsProperty.Contains(this, entityGroup))
            {
                s_OpenedEntityGroupsProperty.Add(this, entityGroup);
            }
        }

        public void RemoveOpenedEntityGroup(UTinyEntityGroup.Reference entityGroup)
        {
            s_OpenedEntityGroupsProperty.Remove(this, entityGroup);
        }

        public void ClearOpenedEntityGroups()
        {
            s_OpenedEntityGroupsProperty.Clear(this);
        }

        public string ToJson()
        {
            return BackEnd.Persist(this);
        }

        public void FromJson(string json)
        {
            if (string.IsNullOrEmpty(json))
            {
                return;
            }

            object workspaceObject;
            Properties.Serialization.Json.TryDeserializeObject(json, out workspaceObject);
            var workspaceDictionary = workspaceObject as IDictionary<string, object>;

            if (null != workspaceDictionary)
            {
                ActiveEntityGroup = Parser.ParseSceneReference(Parser.GetValue(workspaceDictionary, s_ActiveEntityGroupProperty.Name));

                object openedScenesObject;
                workspaceDictionary.TryGetValue(s_OpenedEntityGroupsProperty.Name, out openedScenesObject);
                var openedScenesList = openedScenesObject as IList<object>;

                if (null != openedScenesList)
                {
                    foreach (var obj in openedScenesList)
                    {
                        OpenedEntityGroups.Add(Parser.ParseSceneReference(obj));
                    }
                }

                if (workspaceDictionary.ContainsKey(s_PreviewProperty.Name))
                {
                    Preview = TypeConversion.Convert<bool>(Parser.GetValue(workspaceDictionary, s_PreviewProperty.Name));
                }
                
                if (workspaceDictionary.ContainsKey(s_BuildConfigurationProperty.Name))
                {
                    BuildConfiguration = (UTinyBuildConfiguration) TypeConversion.Convert<int>(Parser.GetValue(workspaceDictionary, s_BuildConfigurationProperty.Name));
                }
            }
        }

        #endregion

        #region IPropertyContainer

        public IVersionStorage VersionStorage => this;
        public IPropertyBag PropertyBag => s_PropertyBag;

        public int Version { get; private set; }

        #endregion

        public int GetVersion(IProperty property, IPropertyContainer container)
        {
            return -1;
        }

        public void IncrementVersion(IProperty property, IPropertyContainer container)
        {
            Version++;
        }
    }
}
#endif // NET_4_6
