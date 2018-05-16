#if NET_4_6
using System.Collections.Generic;
using System.IO;
using System.Text;
using Unity.Properties;
using Unity.Properties.Serialization;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Unity.Tiny.Serialization.FlatJson
{
    /// <summary>
    /// Writes objects as JSON to a stream
    /// </summary>
    public static class BackEnd
    {
        private static PropertyVisitor Visitor => new PropertyVisitor();

        /// <summary>
        /// Writes the given property container to a JSON string and any asset references to the given storage
        /// </summary>
        /// <param name="container">Generic property container</param>
        /// <returns>JSON stringified object</returns>
        public static string Persist(IPropertyContainer container)
        {
            return JsonPropertyContainerWriter.Write(container, Visitor);
        }

        /// <summary>
        /// Writes the given property containers to a JSON string and any asset references to the given storage
        /// @NOTE return string is in the format "[{..}, {..}]"
        /// </summary>
        /// <param name="objects">Generic property containers to write</param>
        /// <returns>JSON stringified objects</returns>
        public static string Persist(params IPropertyContainer[] objects)
        {
            var sb = new StringBuilder();
            sb.Append("[");

            var first = true;

            foreach (var obj in objects)
            {
                if (!first)
                {
                    sb.Append(",\n");
                }
                else
                {
                    first = false;
                }

                sb.Append(JsonPropertyContainerWriter.Write(obj, Visitor));
            }

            sb.Append("]");
            return sb.ToString();
        }

        public static void Persist(string path, IEnumerable<IPropertyContainer> objects)
        {
            using (var stream = new FileStream(path, FileMode.Create))
            using (var writer = new StreamWriter(stream, Encoding.UTF8, 1024, true))
            {
                Persist(writer, objects);
            }
        }

        public static void Persist(Stream output, params IPropertyContainer[] objects)
        {
            Persist(output, (IEnumerable<IPropertyContainer>) objects);
        }

        public static void Persist(Stream output, IEnumerable<IPropertyContainer> objects)
        {
            using (var writer = new StreamWriter(output, Encoding.UTF8, 1024, true))
            {
                Persist(writer, objects);
            }
        }

        /// <summary>
        /// NOTE: This method is not optimal. It must type check and cast objects
        /// @TODO Write specialized methods for our use cases
        /// </summary>
        /// <param name="writer"></param>
        /// <param name="objects">Objects to pack</param>
        private static void Persist(TextWriter writer, IEnumerable<IPropertyContainer> objects)
        {
            writer.Write("[");

            var first = true;

            foreach (var obj in objects)
            {
                if (null == obj)
                {
                    continue;
                }

                if (!first)
                {
                    writer.Write(",\n");
                }
                else
                {
                    first = false;
                }

                (obj as IRegistryObject)?.Refresh();

                if (obj is UTinyProject)
                {
                    writer.Write(JsonPropertyContainerWriter.Write((UTinyProject) obj, Visitor));
                }
                else if (obj is UTinyModule)
                {
                    writer.Write(JsonPropertyContainerWriter.Write((UTinyModule) obj, Visitor));
                }
                else if (obj is UTinyType)
                {
                    writer.Write(JsonPropertyContainerWriter.Write((UTinyType) obj, Visitor));
                }
                else if (obj is UTinyEntityGroup)
                {
                    writer.Write(JsonPropertyContainerWriter.Write((UTinyEntityGroup) obj, Visitor));
                }
                else if (obj is UTinyEntity)
                {
                    writer.Write(JsonPropertyContainerWriter.Write((UTinyEntity) obj, Visitor));
                }
                else if (obj is UTinyObject)
                {
                    writer.Write(JsonPropertyContainerWriter.Write((UTinyObject) obj, Visitor));
                }
                else if (obj is UTinyScript)
                {
                    writer.Write(JsonPropertyContainerWriter.Write((UTinyScript) obj, Visitor));
                }
                else if (obj is UTinySystem)
                {
                    writer.Write(JsonPropertyContainerWriter.Write((UTinySystem) obj, Visitor));
                }
                else
                {
                    writer.Write(JsonPropertyContainerWriter.Write(obj, Visitor));
                }
            }

            writer.Write("]");
        }

        private class PropertyVisitor : JsonPropertyVisitor,
            ICustomVisit<UTinyTypeCode>,
            ICustomVisit<UTinyModule.Reference>,
            ICustomVisit<UTinyType.Reference>,
            ICustomVisit<UTinyEntityGroup.Reference>,
            ICustomVisit<UTinyEntity.Reference>,
            ICustomVisit<UTinyScript.Reference>,
            ICustomVisit<UTinySystem.Reference>,
            ICustomVisit<Object>,
            ICustomVisit<TextAsset>,
            ICustomVisit<Texture2D>,
            ICustomVisit<Sprite>,
            ICustomVisit<AudioClip>,
            ICustomVisit<Font>,
            IExcludeVisit<UTinyObject>,
            IExcludeVisit<UTinyObject.PropertiesContainer>,
            IExcludeVisit<UTinyDocumentation>,
            IExcludeVisit<string>
        {
            private void VisitReference<TReference>(TReference value)
                where TReference : IReference
            {
                if (value.Id == UTinyId.Empty)
                {
                    return;
                }

                StringBuffer.Append(' ', Indent * Style.Space);

                if (IsListItem)
                {
                    StringBuffer.Append("{ \"Id\": \"");
                    StringBuffer.Append(value.Id);
                    StringBuffer.Append("\", \"Name\": \"");
                    StringBuffer.Append(value.Name);
                    StringBuffer.Append("\" },\n");
                }
                else
                {
                    StringBuffer.Append("\"");
                    StringBuffer.Append(Property.Name);
                    StringBuffer.Append("\": { \"Id\": \"");
                    StringBuffer.Append(value.Id);
                    StringBuffer.Append("\", \"Name\": \"");
                    StringBuffer.Append(value.Name);
                    StringBuffer.Append("\" },\n");
                }
            }

            private void VisitReference<TReference>(TReference value, int typeId)
                where TReference : IReference
            {
                if (value.Id == UTinyId.Empty)
                {
                    return;
                }

                StringBuffer.Append(' ', Indent * Style.Space);

                if (IsListItem)
                {
                    StringBuffer.Append("{ \"$TypeId\": ");
                    StringBuffer.Append(typeId);

                    if (value.Id != UTinyId.Empty)
                    {
                        StringBuffer.Append(", \"Id\": \"");
                        StringBuffer.Append(value.Id);
                        StringBuffer.Append("\", \"Name\": \"");
                        StringBuffer.Append(value.Name);
                        StringBuffer.Append("\" },\n");
                    }
                    else
                    {
                        StringBuffer.Append(" },\n");
                    }
                }
                else
                {
                    StringBuffer.Append("\"");
                    StringBuffer.Append(Property.Name);
                    StringBuffer.Append("\": { \"$TypeId\": ");
                    StringBuffer.Append(typeId);

                    if (value.Id != UTinyId.Empty)
                    {
                        StringBuffer.Append(", \"Id\": \"");
                        StringBuffer.Append(value.Id);
                        StringBuffer.Append("\", \"Name\": \"");
                        StringBuffer.Append(value.Name);
                        StringBuffer.Append("\" },\n");
                    }
                    else
                    {
                        StringBuffer.Append(" },\n");
                    }
                }
            }
            
            private void VisitObject(UnityEngine.Object value)
            {
                var handle = UnityObjectSerializer.ToObjectHandle(value);

                StringBuffer.Append(' ', Style.Space * Indent);

                if (false == IsListItem)
                {
                    StringBuffer.Append("\"");
                    StringBuffer.Append(Property.Name);
                    StringBuffer.Append("\": ");
                }

                if (string.IsNullOrEmpty(handle.Guid))
                {
                    StringBuffer.Append("{ \"$TypeId\": ");
                    StringBuffer.Append((int) UTinyTypeId.UnityObject);
                    StringBuffer.Append(" },\n");
                }
                else
                {
                    StringBuffer.Append("{ \"$TypeId\": ");
                    StringBuffer.Append((int) UTinyTypeId.UnityObject);
                    StringBuffer.Append(", \"Guid\": \"");
                    StringBuffer.Append(handle.Guid);
                    StringBuffer.Append("\", \"FileId\": ");
                    StringBuffer.Append(handle.FileId);
                    StringBuffer.Append(", \"Type\": ");
                    StringBuffer.Append(handle.Type);
                    StringBuffer.Append(" },\n");
                }
            }

            void ICustomVisit<UTinyTypeCode>.CustomVisit(UTinyTypeCode value)
            {
                // custom override to avoid the default Enum-to-Int32 serialization and use the name instead
                AppendPrimitive(Json.EncodeJsonString(value.ToString()));
            }
            
            void ICustomVisit<UTinyModule.Reference>.CustomVisit(UTinyModule.Reference value)
            {
                VisitReference(value);
            }

            void ICustomVisit<UTinyType.Reference>.CustomVisit(UTinyType.Reference value)
            {
                VisitReference(value);
            }

            void ICustomVisit<UTinyEntityGroup.Reference>.CustomVisit(UTinyEntityGroup.Reference value)
            {
                VisitReference(value);
            }

            void ICustomVisit<UTinyEntity.Reference>.CustomVisit(UTinyEntity.Reference value)
            {
                VisitReference(value, (int) UTinyTypeId.EntityReference);
            }

            void ICustomVisit<UTinyScript.Reference>.CustomVisit(UTinyScript.Reference value)
            {
                VisitReference(value);
            }

            void ICustomVisit<UTinySystem.Reference>.CustomVisit(UTinySystem.Reference value)
            {
                VisitReference(value);
            }

            void ICustomVisit<Object>.CustomVisit(Object value)
            {
                VisitObject(value);
            }

            void ICustomVisit<TextAsset>.CustomVisit(TextAsset value)
            {
                VisitObject(value);
            }

            void ICustomVisit<Texture2D>.CustomVisit(Texture2D value)
            {
                VisitObject(value);
            }

            void ICustomVisit<Sprite>.CustomVisit(Sprite value)
            {
                VisitObject(value);
            }

            void ICustomVisit<AudioClip>.CustomVisit(AudioClip value)
            {
                VisitObject(value);
            }
            
            void ICustomVisit<Font>.CustomVisit(Font value)
            {
                VisitObject(value);
            }

            private static bool IsSkipped<TValue>(TValue value, IPropertyContainer container, IProperty property)
            {
                // skip null containers
                // TODO: fix in property API
                if (typeof(IPropertyContainer).IsAssignableFrom(typeof(TValue)) && value == null)
                {
                    return true;
                }
                
                // special case for lists
                var listProperty = property as IListProperty;
                if (listProperty != null)
                {
                    // skip empty lists
                    // always write list elements, we dont handle `IsOverridden` or default values for lists
                    return listProperty.Count(container) == 0;
                }
                
                // skip default property values
                var valueProperty = property as IUTinyValueProperty;
                if (valueProperty != null)
                {
                    return !valueProperty.IsOverridden(container);
                }
                
                // skip default primitives
                if (Equals(value, default(TValue)))
                {
                    return true;
                }
                
                // filter out fake nulls
                if (typeof(UnityEngine.Object).IsAssignableFrom(typeof(TValue)))
                {
                    return (false == ((UnityEngine.Object) (object) value));
                }

                return false;
            }

            public override bool ExcludeVisit<TContainer, TValue>(TContainer container, VisitContext<TValue> context)
            {
                return IsSkipped(context.Value, container, context.Property) || base.ExcludeVisit(container, context);
            }
            
            public override bool ExcludeVisit<TContainer, TValue>(ref TContainer container, VisitContext<TValue> context)
            {
                return IsSkipped(context.Value, container, context.Property) || base.ExcludeVisit(ref container, context);
            }

            bool IExcludeVisit<UTinyObject>.ExcludeVisit(UTinyObject value)
            {
                return false;
                //return !IsListItem && (false == value.IsOverridden);
            }

            bool IExcludeVisit<UTinyObject.PropertiesContainer>.ExcludeVisit(UTinyObject.PropertiesContainer value)
            {
                return (false == value.IsOverridden);
            }

            bool IExcludeVisit<UTinyDocumentation>.ExcludeVisit(UTinyDocumentation value)
            {
                return string.IsNullOrEmpty(value.Summary);
            }

            bool IExcludeVisit<string>.ExcludeVisit(string value)
            {
                return string.IsNullOrEmpty(value);
            }
        }
    }
}
#endif // NET_4_6
