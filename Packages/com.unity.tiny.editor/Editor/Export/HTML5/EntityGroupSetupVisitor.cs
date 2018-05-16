#if NET_4_6
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Unity.Properties;
using UnityEngine;
using Unity.Tiny.Extensions;

namespace Unity.Tiny
{
    public class EntityGroupSetupVisitor : UTinyProject.Visitor, IDisposable
    {
        public class VisitorContext
        {
            public UTinyProject Project;
            public UTinyModule Module;
            public IRegistry Registry;
            public UTinyCodeWriter Writer;
            public Dictionary<UTinyEntity.Reference, int> EntityIndexMap;
            public int ComponentIndex;
            public int StructIndex;
            public int ArrayIndex;
        }

        /// <summary>
        /// If set to true enums will be exported as thier underlying type
        /// e.g. c1.setEnumField(3);
        /// 
        /// If false enums are exported with thier fully qualified name
        /// e.g. c1.setEnumField(MyEnumType.MyEnumValue);
        /// </summary>
        public static bool ExportEnumAsValue = true;

        public UTinyCodeWriter Writer { private get; set; }
        public UTinyBuildReport.TreeNode Report { private get; set; }
        
        /// <summary>
        /// Flag; if set to true a component with the entityGroup name will be added to each entity
        /// </summary>
        public bool WriteEntityGroupComponent { get; set; } = true;

        /// <summary>
        /// Flag; if set to true a component for each layer will be added to all entities
        /// </summary>
        public bool WriteEntityLayer { get; set; } = true;

        private int m_EntityGroupIndex;

        private static string EscapeJsString(string content)
        {
            return Properties.Serialization.Json.EncodeJsonString(content);
        }
        
        private static string GetFullyQualifiedLayerName(string layerName)
        {
            const string fullNamespace = "ut.layers.";

            layerName = layerName.Replace(" ", "");
            if (layerName.StartsWith(fullNamespace))
            {
                return layerName;
            }
            return fullNamespace + layerName;
        }

        private static string GetDefaultJsValue(UTinyType type)
        {
            if (type.TypeCode == UTinyTypeCode.UnityObject || type.TypeCode == UTinyTypeCode.EntityReference)
            {
                return "ut.Entity.NONE";
            }
            
            // @TODO Handle primitives

            return "null";
        }

        public override void VisitEntityGroup(UTinyEntityGroup entityGroup)
        {
            var begin = Writer.Length; 
            
            Writer.Line($"{UTinyHTML5Builder.KEntityGroupNamespace}.{Module.Namespace}.{entityGroup.Name}.name = {EscapeJsString(entityGroup.Name)};");
            Writer.WriteRaw($"{UTinyHTML5Builder.KEntityGroupNamespace}.{Module.Namespace}.{entityGroup.Name}.load = ");
            WriteEntityGroupSetupFunction(Writer, Project, entityGroup, WriteEntityGroupComponent, WriteEntityLayer);

            Report.AddChild(entityGroup.Name, System.Text.Encoding.ASCII.GetBytes(Writer.Substring(begin)));

#if UNITY_EDITOR_WIN
            Writer.Length -= 2;
#else
            Writer.Length -= 1;
#endif
        }

        public static void WriteEntityGroupSetupFunction(UTinyCodeWriter writer, UTinyProject project, UTinyEntityGroup entityGroup, bool writeEntityGroupComponent = true, bool writeEntityLayer = true)
        {
            var entityIndex = 0;
            var entityIndexMap = new Dictionary<UTinyEntity.Reference, int>();
            
            using (writer.Scope("function(w)"))
            {
                entityIndexMap.Clear();

                foreach (var reference in entityGroup.Entities)
                {
                    var entity = reference.Dereference(entityGroup.Registry);
                    ++entityIndex;
                    entityIndexMap[reference] = entityIndex;
                    writer.Line($"var e{entityIndex} = w.create({EscapeJsString(entity.Name)});");
                }
    
                if (writeEntityGroupComponent)
                {
                    foreach (var reference in entityGroup.Entities)
                    {
                        var index = entityIndexMap[reference];
                        writer.Line($"e{index}.addComponent(this.Component);");
                    }
                }
    
                if (writeEntityLayer)
                {
                    foreach (var reference in entityGroup.Entities)
                    {
                        var index = entityIndexMap[reference];
                        var entity = reference.Dereference(entityGroup.Registry);
                        writer.Line($"e{index}.addComponent({GetFullyQualifiedLayerName(LayerMask.LayerToName(entity.Layer))});");
                    }
                }

                var context = new VisitorContext
                {
                    Project = project,
                    Module = project.Module.Dereference(project.Registry),
                    Registry = project.Registry,
                    Writer = writer,
                    EntityIndexMap = entityIndexMap
                };

                entityIndex = 0;
                foreach (var reference in entityGroup.Entities)
                {
                    var entity = reference.Dereference(entityGroup.Registry);
                    ++entityIndex;
    
                    foreach (var component in entity.Components)
                    {
                        var type = component.Type.Dereference(component.Registry);

                        if (null == type)
                        {
                            Debug.LogError($"{UTinyConstants.ApplicationName}: Missing component type, ComponentType=[{component.Type.Name}] Entity=[{entity.Name}] Group=[{entityGroup.Name}]");
                            continue;
                        }
                        
                        var index = ++context.ComponentIndex;
                        writer.Line($"var c{index} = e{entityIndex}.addComponent({UTinyBuildPipeline.GetJsTypeName(type)});");
                        component.Properties.Visit(new ComponentVisitor
                        {
                            VisitorContext = context,
                            Path = $"c{index}",
                            Entity = $"e{entityIndex}"
                        });
                    }
                }
                
                writer.WriteIndent();
                writer.WriteRaw("return [");
    
                for (var i = 0; i < entityIndex; i++)
                {
                    writer.WriteRaw(i != 0 ? $", e{i + 1}" : $"e{i + 1}");
                }
    
                writer.WriteRaw("];\n");
            }

            writer.Line().Line();
        }


        private void EndEntityGroup()
        {
        }

        public void Dispose()
        {
            EndEntityGroup();
        }

        public class ComponentVisitor : PropertyVisitor,
            IExcludeVisit<UTinyObject>,
            ICustomVisit<UTinyObject>,
            IExcludeVisit<UTinyList>,
            ICustomVisit<UTinyList>,
            ICustomVisit<UTinyEnum.Reference>,
            IExcludeVisit<UTinyEntity.Reference>,
            ICustomVisit<UTinyEntity.Reference>,
            IExcludeVisit<int>,
            ICustomVisit<int>,
            IExcludeVisit<float>,
            ICustomVisit<float>,
            IExcludeVisit<double>,
            ICustomVisit<double>,
            IExcludeVisit<bool>,
            ICustomVisit<bool>,
            ICustomVisit<char>,
            IExcludeVisit<string>,
            ICustomVisit<string>,
            ICustomVisit<Texture2D>,
            ICustomVisit<Sprite>,
            ICustomVisit<AudioClip>,
            ICustomVisit<Font>
        {
            public VisitorContext VisitorContext { private get; set; }
            public string Path { private get; set; }
            public string Entity { private get; set; }

            private readonly StructVisitor m_StructVisitor = new StructVisitor();
            private readonly ListVisitor m_ListVisitor = new ListVisitor();

            private IPropertyContainer m_Container;

            protected override void VisitSetup<TContainer, TValue>(ref TContainer container, ref VisitContext<TValue> context)
            {
                base.VisitSetup(ref container, ref context);
                m_Container = container;
            }

            protected override bool ExcludeVisit<TValue>(TValue value)
            {
                return Equals(value, default(TValue));
            }

            protected override void Visit<TValue>(TValue value)
            {
                VisitorContext.Writer.Line($"{PropertySetter(Property.Name)}({value.ToString()});");
            }

            bool IExcludeVisit<UTinyObject>.ExcludeVisit(UTinyObject value)
            {
                return value.Properties.PropertyBag.PropertyCount == 0;
            }

            void ICustomVisit<UTinyObject>.CustomVisit(UTinyObject value)
            {
                var index = ++VisitorContext.StructIndex;
                VisitorContext.Writer.Line($"var s{index} = {Path}.{Property.Name}();");
                m_StructVisitor.VisitorContext = VisitorContext;
                m_StructVisitor.Path = $"s{index}";
                value.Properties.Visit(m_StructVisitor);
                VisitorContext.Writer.Line($"{PropertySetter(Property.Name)}(s{index});");
            }
            
            bool IExcludeVisit<UTinyList>.ExcludeVisit(UTinyList value)
            {
                return value.Count == 0;
            }

            void ICustomVisit<UTinyList>.CustomVisit(UTinyList value)
            {
                var index = ++VisitorContext.ArrayIndex;
                var defaultJsValue = GetDefaultJsValue(value.Type.Dereference(VisitorContext.Registry));
                VisitorContext.Writer.Line($"var a{index} = [{string.Join(", ", Enumerable.Range(0, value.Count).Select(x => defaultJsValue).ToArray())}];");
                m_ListVisitor.VisitorContext = VisitorContext;
                m_ListVisitor.Path = $"a{index}";
                value.Visit(m_ListVisitor);
                VisitorContext.Writer.Line($"{PropertySetter(Property.Name)}(a{index});");
            }

            void ICustomVisit<UTinyEnum.Reference>.CustomVisit(UTinyEnum.Reference value)
            {
                var type = value.Type.Dereference(VisitorContext.Registry);
                var normalized = ExportEnumAsValue ? (type.DefaultValue as UTinyObject)?[value.Name] : $"{UTinyBuildPipeline.GetJsTypeName(type)}.{value.Name}";
                VisitorContext.Writer.Line($"{PropertySetter(Property.Name)}({normalized});");
            }

            bool IExcludeVisit<UTinyEntity.Reference>.ExcludeVisit(UTinyEntity.Reference value)
            {
                return value.Equals(UTinyEntity.Reference.None);
            }

            void ICustomVisit<UTinyEntity.Reference>.CustomVisit(UTinyEntity.Reference value)
            {
                int index;
                    
                if (Property.Name == "parent" &&
                    m_Container is UTinyObject.PropertiesContainer &&
                    ((UTinyObject.PropertiesContainer) m_Container).ParentObject.Type.Equals(VisitorContext.Registry
                        .GetTransformType()))
                {
                    if (!VisitorContext.EntityIndexMap.TryGetValue(value, out index))
                    {
                        return;
                    }

                    var transformTypeName =
                        UTinyBuildPipeline.GetJsTypeName(VisitorContext.Registry.GetTransformType()
                            .Dereference(VisitorContext.Registry));

                    var line = $"e{index}.getComponent({transformTypeName}).appendChild({Entity});";
                    VisitorContext.Writer.Line(line);
                }
                else
                {
                    if (!VisitorContext.EntityIndexMap.TryGetValue(value, out index))
                    {
                        return;
                    }

                    VisitorContext.Writer.Line($"{PropertySetter(Property.Name)}(e{index});");
                }
            }

            bool IExcludeVisit<int>.ExcludeVisit(int value)
            {
                if (Property.Name == "layerMask")
                {
                    return false;
                }
                return value == 0;
            }

            void ICustomVisit<int>.CustomVisit(int value)
            {
                // HACK: Create custom component exporter
                // In this case, we have a double hack (TM)
                //   1- We need to convert Unity layer to a component-based layering system.
                //   2- We need to set the cullingMode to be Any
                
                if (Property.Name == "layerMask")
                {
                    var layer = value;
                    var exportedLayers = new List<string>();
                    for (var i = 0; i < 32; ++i)
                    {
                        // if bit is set
                        if ((layer & 1 << i) == 1 << i)
                        {
                            var layerName = LayerMask.LayerToName(i);
                            if (string.IsNullOrEmpty(layerName))
                            {
                                continue;
                            }
                            exportedLayers.Add($"{GetFullyQualifiedLayerName(layerName)}.cid");
                        }
                    }
                    VisitorContext.Writer.Line($"{Path}.setCullingMask([{string.Join(", ", exportedLayers.ToArray())}]);");
                    VisitorContext.Writer.Line($"{Path}.setCullingMode(2);");
                    return;
                }
                VisitorContext.Writer.Line($"{PropertySetter(Property.Name)}({value});");
            }

            bool IExcludeVisit<float>.ExcludeVisit(float value)
            {
                return Math.Abs(value) <= float.Epsilon;
            }

            void ICustomVisit<float>.CustomVisit(float value)
            {
                VisitorContext.Writer.Line($"{PropertySetter(Property.Name)}({value.ToString(CultureInfo.InvariantCulture)});");
            }

            bool IExcludeVisit<double>.ExcludeVisit(double value)
            {
                return Math.Abs(value) <= double.Epsilon;
            }

            void ICustomVisit<double>.CustomVisit(double value)
            {
                VisitorContext.Writer.Line($"{PropertySetter(Property.Name)}({value.ToString(CultureInfo.InvariantCulture)});");
            }

            bool IExcludeVisit<bool>.ExcludeVisit(bool value)
            {
                return !value;
            }

            void ICustomVisit<bool>.CustomVisit(bool value)
            {
                VisitorContext.Writer.Line($"{PropertySetter(Property.Name)}({(value ? "true" : "false")});");
            }

            void ICustomVisit<char>.CustomVisit(char value)
            {
                VisitorContext.Writer.Line($"{PropertySetter(Property.Name)}({EscapeJsString(string.Empty + value)});");
            }

            bool IExcludeVisit<string>.ExcludeVisit(string value)
            {
                return string.IsNullOrEmpty(value);
            }

            void ICustomVisit<string>.CustomVisit(string value)
            {
                VisitorContext.Writer.Line($"{PropertySetter(Property.Name)}({EscapeJsString(value)});");
            }

            private string PropertySetter(string name)
            {
                return $"{Path}.set{char.ToUpperInvariant(name[0]) + name.Substring(1)}";
            }

            void ICustomVisit<Texture2D>.CustomVisit(Texture2D value)
            {
                VisitObjectEntity(value);
            }

            void ICustomVisit<Sprite>.CustomVisit(Sprite value)
            {
                VisitObjectEntity(value);
            }

            void ICustomVisit<AudioClip>.CustomVisit(AudioClip value)
            { 
                VisitObjectEntity(value);
            }
            
            void ICustomVisit<Font>.CustomVisit(Font value)
            {
                VisitObjectEntity(value);
            }
            
            private void VisitObjectEntity<TValue>(TValue value) where TValue : UnityEngine.Object
            {
                var assetName = UTinyAssetExporter.GetAssetName(VisitorContext.Module, value);
                VisitorContext.Writer.Line($"{PropertySetter(Property.Name)}(w.getByName('{UTinyAssetEntityGroupGenerator.GetAssetEntityPath(typeof(TValue))}{assetName}'));");
            }
        }

        private class StructVisitor : PropertyVisitor,
            IExcludeVisit<UTinyObject>,
            ICustomVisit<UTinyObject>,
            IExcludeVisit<UTinyList>,
            ICustomVisit<UTinyList>,
            ICustomVisit<UTinyEnum.Reference>,
            IExcludeVisit<UTinyEntity.Reference>,
            ICustomVisit<UTinyEntity.Reference>,
            ICustomVisit<int>,
            IExcludeVisit<float>,
            ICustomVisit<float>,
            IExcludeVisit<double>,
            ICustomVisit<double>,
            ICustomVisit<bool>,
            IExcludeVisit<char>,
            ICustomVisit<char>,
            IExcludeVisit<string>,
            ICustomVisit<string>,
            ICustomVisit<Texture2D>,
            ICustomVisit<Sprite>,
            ICustomVisit<AudioClip>,
            ICustomVisit<Font>
        {
            public VisitorContext VisitorContext { private get; set; }
            public string Path { private get; set; }

            protected override bool ExcludeVisit<TValue>(TValue value)
            {
                return Equals(value, default(TValue));
            }

            protected override void Visit<TValue>(TValue value)
            {
                VisitorContext.Writer.Line($"{Path}.{Property.Name} = {value.ToString()};");
            }

            bool IExcludeVisit<UTinyObject>.ExcludeVisit(UTinyObject value)
            {
                return value.Properties.PropertyBag.PropertyCount == 0;
            }

            void ICustomVisit<UTinyObject>.CustomVisit(UTinyObject value)
            {
                value.Properties.Visit(new StructVisitor { VisitorContext = VisitorContext, Path = $"{Path}.{Property.Name}" });
            }

            bool IExcludeVisit<UTinyList>.ExcludeVisit(UTinyList value)
            {
                return value.Count > 0;
            }

            void ICustomVisit<UTinyList>.CustomVisit(UTinyList value)
            {
                var index = ++VisitorContext.ArrayIndex;
                var defaultJsValue = GetDefaultJsValue(value.Type.Dereference(VisitorContext.Registry));
                VisitorContext.Writer.Line($"var a{index} = [{string.Join(", ", Enumerable.Range(0, value.Count).Select(x => defaultJsValue).ToArray())}];");
                value.Visit(new ListVisitor {VisitorContext = VisitorContext, Path = $"a{index}"});
                VisitorContext.Writer.Line($"{Path}.{Property.Name} = a{index};");
            }

            void ICustomVisit<UTinyEnum.Reference>.CustomVisit(UTinyEnum.Reference value)
            {
                var type = value.Type.Dereference(VisitorContext.Registry);
                var normalized = ExportEnumAsValue ? (type.DefaultValue as UTinyObject)?[value.Name] : $"{UTinyBuildPipeline.GetJsTypeName(type)}.{value.Name}";
                VisitorContext.Writer.Line($"{Path}.{Property.Name} = {normalized};");
            }

            bool IExcludeVisit<UTinyEntity.Reference>.ExcludeVisit(UTinyEntity.Reference value)
            {
                return value.Equals(UTinyEntity.Reference.None);
            }

            void ICustomVisit<UTinyEntity.Reference>.CustomVisit(UTinyEntity.Reference value)
            {
                int index;
                if (!VisitorContext.EntityIndexMap.TryGetValue(value, out index))
                {
                    return;
                }
                VisitorContext.Writer.Line($"{Path}.{Property.Name} = e{index};");
            }

            void ICustomVisit<int>.CustomVisit(int value)
            {
                VisitorContext.Writer.Line($"{Path}.{Property.Name} = {value};");
            }

            bool IExcludeVisit<float>.ExcludeVisit(float value)
            {
                return Math.Abs(value) <= float.Epsilon;
            }

            void ICustomVisit<float>.CustomVisit(float value)
            {
                VisitorContext.Writer.Line($"{Path}.{Property.Name} = {value.ToString(CultureInfo.InvariantCulture)};");
            }

            bool IExcludeVisit<double>.ExcludeVisit(double value)
            {
                return Math.Abs(value) <= double.Epsilon;
            }

            void ICustomVisit<double>.CustomVisit(double value)
            {
                VisitorContext.Writer.Line($"{Path}.{Property.Name} = {value.ToString(CultureInfo.InvariantCulture)};");
            }

            void ICustomVisit<bool>.CustomVisit(bool value)
            {
                VisitorContext.Writer.Line($"{Path}.{Property.Name} = {(value ? "true" : "false")};");
            }

            bool IExcludeVisit<char>.ExcludeVisit(char value)
            {
                return false;
            }

            void ICustomVisit<char>.CustomVisit(char value)
            {
                VisitorContext.Writer.Line($"{Path}.{Property.Name} = {EscapeJsString(string.Empty + value)};");
            }

            bool IExcludeVisit<string>.ExcludeVisit(string value)
            {
                return string.IsNullOrEmpty(value);
            }

            void ICustomVisit<string>.CustomVisit(string value)
            {
                VisitorContext.Writer.Line($"{Path}.{Property.Name} = {EscapeJsString(value)};");
            }
            
            void ICustomVisit<Texture2D>.CustomVisit(Texture2D value)
            {
                VisitObjectEntity(value);
            }

            void ICustomVisit<Sprite>.CustomVisit(Sprite value)
            {
                VisitObjectEntity(value);
            }

            void ICustomVisit<AudioClip>.CustomVisit(AudioClip value)
            {
                VisitObjectEntity(value);
            }

            void ICustomVisit<Font>.CustomVisit(Font value)
            {
                VisitObjectEntity(value);
            }
            
            private void VisitObjectEntity<TValue>(TValue value) where TValue : UnityEngine.Object
            {
                var assetName = UTinyAssetExporter.GetAssetName(VisitorContext.Module, value);
                VisitorContext.Writer.Line($"{Path}.{Property.Name} = w.getByName('{UTinyAssetEntityGroupGenerator.GetAssetEntityPath(typeof(TValue))}{assetName}');");
            }
        }

        private class ListVisitor : PropertyVisitor,
            IExcludeVisit<UTinyObject>,
            ICustomVisit<UTinyObject>,
            IExcludeVisit<UTinyList>,
            ICustomVisit<UTinyList>,
            ICustomVisit<UTinyEnum.Reference>,
            IExcludeVisit<UTinyEntity.Reference>,
            ICustomVisit<UTinyEntity.Reference>,
            ICustomVisit<int>,
            IExcludeVisit<float>,
            ICustomVisit<float>,
            IExcludeVisit<double>,
            ICustomVisit<double>,
            ICustomVisit<bool>,
            IExcludeVisit<char>,
            ICustomVisit<char>,
            IExcludeVisit<string>,
            ICustomVisit<string>,
            ICustomVisit<Texture2D>,
            ICustomVisit<Sprite>,
            ICustomVisit<AudioClip>,
            ICustomVisit<Font>
        {
            public VisitorContext VisitorContext { private get; set; }
            public string Path { private get; set; }

            protected override bool ExcludeVisit<TValue>(TValue value)
            {
                return Equals(value, default(TValue));
            }

            protected override void Visit<TValue>(TValue value)
            {
                if (!IsListItem)
                {
                    return;
                }

                VisitorContext.Writer.Line($"{Path}[{ListIndex}] = {value.ToString()};");
            }

            bool IExcludeVisit<UTinyObject>.ExcludeVisit(UTinyObject value)
            {
                return value.Properties.PropertyBag.PropertyCount == 0;
            }

            void ICustomVisit<UTinyObject>.CustomVisit(UTinyObject value)
            {
                if (!IsListItem)
                {
                    return;
                }

                var type = value.Type.Dereference(value.Registry);

                var index = ++VisitorContext.StructIndex;
                VisitorContext.Writer.Line($"var s{index} = new {UTinyBuildPipeline.GetJsTypeName(type)}();");
                value.Properties.Visit(new StructVisitor {VisitorContext = VisitorContext, Path = $"s{index}"});
                VisitorContext.Writer.Line($"{Path}[{ListIndex}] = s{index};");
            }

            bool IExcludeVisit<UTinyList>.ExcludeVisit(UTinyList value)
            {
                return !IsListItem;
            }

            void ICustomVisit<UTinyList>.CustomVisit(UTinyList value)
            {
                throw new NotImplementedException();
            }

            void ICustomVisit<UTinyEnum.Reference>.CustomVisit(UTinyEnum.Reference value)
            {
                if (!IsListItem)
                {
                    return;
                }

                var type = value.Type.Dereference(VisitorContext.Registry);
                var normalized = ExportEnumAsValue
                    ? (type.DefaultValue as UTinyObject)?[value.Name]
                    : $"{UTinyBuildPipeline.GetJsTypeName(type)}.{value.Name}";
                VisitorContext.Writer.Line($"{Path}[{ListIndex}] = {normalized};");
            }

            bool IExcludeVisit<UTinyEntity.Reference>.ExcludeVisit(UTinyEntity.Reference value)
            {
                return value.Equals(UTinyEntity.Reference.None);
            }

            void ICustomVisit<UTinyEntity.Reference>.CustomVisit(UTinyEntity.Reference value)
            {
                if (!IsListItem)
                {
                    return;
                }

                int index;
                if (!VisitorContext.EntityIndexMap.TryGetValue(value, out index))
                {
                    return;
                }
                VisitorContext.Writer.Line($"{Path}[{ListIndex}] = e{index};");
            }

            void ICustomVisit<int>.CustomVisit(int value)
            {
                if (!IsListItem)
                {
                    return;
                }

                VisitorContext.Writer.Line($"{Path}[{ListIndex}] = {value};");
            }

            bool IExcludeVisit<float>.ExcludeVisit(float value)
            {
                return Math.Abs(value) <= float.Epsilon;
            }

            void ICustomVisit<float>.CustomVisit(float value)
            {
                if (!IsListItem)
                {
                    return;
                }

                VisitorContext.Writer.Line($"{Path}[{ListIndex}] = {value.ToString(CultureInfo.InvariantCulture)};");
            }

            bool IExcludeVisit<double>.ExcludeVisit(double value)
            {
                return Math.Abs(value) <= double.Epsilon;
            }

            void ICustomVisit<double>.CustomVisit(double value)
            {
                if (!IsListItem)
                {
                    return;
                }

                VisitorContext.Writer.Line($"{Path}[{ListIndex}] = {value.ToString(CultureInfo.InvariantCulture)};");
            }

            void ICustomVisit<bool>.CustomVisit(bool value)
            {
                if (!IsListItem)
                {
                    return;
                }

                VisitorContext.Writer.Line($"{Path}[{ListIndex}] = {(value ? "true" : "false")};");
            }

            bool IExcludeVisit<char>.ExcludeVisit(char value)
            {
                return false;
            }

            void ICustomVisit<char>.CustomVisit(char value)
            {
                if (!IsListItem)
                {
                    return;
                }

                VisitorContext.Writer.Line($"{Path}[{ListIndex}] = {EscapeJsString(string.Empty + value)};");
            }

            bool IExcludeVisit<string>.ExcludeVisit(string value)
            {
                return string.IsNullOrEmpty(value);
            }

            void ICustomVisit<string>.CustomVisit(string value)
            {
                if (!IsListItem)
                {
                    return;
                }

                VisitorContext.Writer.Line($"{Path}[{ListIndex}] = {EscapeJsString(value)};");
            }
            
            void ICustomVisit<Texture2D>.CustomVisit(Texture2D value)
            {
                VisitObjectEntity(value);
            }

            void ICustomVisit<Sprite>.CustomVisit(Sprite value)
            {
                VisitObjectEntity(value);
            }

            void ICustomVisit<AudioClip>.CustomVisit(AudioClip value)
            {
                VisitObjectEntity(value);
            }

            void ICustomVisit<Font>.CustomVisit(Font value)
            {
                VisitObjectEntity(value);
            }
            
            private void VisitObjectEntity<TValue>(TValue value) where TValue : UnityEngine.Object
            {
                var assetName = UTinyAssetExporter.GetAssetName(VisitorContext.Module, value);
                VisitorContext.Writer.Line($"{Path}[{ListIndex}] = w.getByName('{UTinyAssetEntityGroupGenerator.GetAssetEntityPath(typeof(TValue))}{assetName}');");
            }
        }
    }
}
#endif // NET_4_6
