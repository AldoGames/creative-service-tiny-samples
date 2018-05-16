#if NET_4_6
using System;
using System.Collections.Generic;
using System.IO;
using Unity.Properties;
using Unity.Properties.Serialization;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Unity.Tiny.Serialization.Binary
{
    public enum UTinyBinaryToken : byte
    {
        Id = 0,
        ModuleReference = 1,
        TypeReference = 2,
        SceneReference = 3,
        EntityReference = 4,
        ScriptReference = 5,
        SystemReference = 6,
        UnityObject = 7
    }
    
    /// <summary>
    /// Writes objects as binary to a stream
    /// </summary>
    public static class BackEnd
    {
        private static readonly PropertyVisitor s_PropertyVisitor = new PropertyVisitor();

        public static void Persist(string path, params IPropertyContainer[] objects)
        {
            Persist(path, (IEnumerable<IPropertyContainer>) objects);
        }
        
        public static void Persist(Stream output, params IPropertyContainer[] objects)
        {
            Persist(output, (IEnumerable<IPropertyContainer>) objects);
        }

        public static void Persist(string path, IEnumerable<IPropertyContainer> objects)
        {
            using (var stream = new FileStream(path, FileMode.Create))
            {
                Persist(stream, objects);
            }
        }

        public static void Persist(Stream output, IEnumerable<IPropertyContainer> objects)
        {
            using (var memory = new MemoryStream())
            using (var writer = new BinaryWriter(memory))
            {
                writer.Write(BinaryToken.BeginArray);
                writer.Write((uint) 0);
                foreach (var obj in objects)
                {
                    (obj as IRegistryObject)?.Refresh();
                    BinaryPropertyContainerWriter.Write(memory, obj, s_PropertyVisitor);
                }
                writer.Write(BinaryToken.EndArray);

                const int start = 5;
                var end = memory.Position;
                var size = end - start;
                memory.Position = start - sizeof(uint);
                writer.Write((uint) size);
                output.Write(memory.GetBuffer(), 0, (int) end);
            }
        }

        public static void Persist(Stream output, IEnumerable<UTinyEntity> entities)
        {
            using (var memory = new MemoryStream())
            using (var writer = new BinaryWriter(memory))
            {
                writer.Write(BinaryToken.BeginArray);
                writer.Write((uint) 0);
                foreach (var obj in entities)
                {
                    BinaryPropertyContainerWriter.Write(memory, obj, s_PropertyVisitor);
                }
                writer.Write(BinaryToken.EndArray);

                const int start = 5;
                var end = memory.Position;
                var size = end - start;
                memory.Position = start - sizeof(uint);
                writer.Write((uint) size);
                output.Write(memory.GetBuffer(), 0, (int) end);
            }
        }
        
        public static void Persist<TContainer>(Stream output, TContainer container) where TContainer : class, IPropertyContainer
        {
            BinaryPropertyContainerWriter.Write(output, container, s_PropertyVisitor);
        }

        private class PropertyVisitor : BinaryPropertyVisitor,
            ICustomVisit<UTinyId>,
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
            IExcludeVisit<UTinyObject.PropertiesContainer>
        {
            private void VisitReference(UTinyBinaryToken token, IReference value)
            {
                WriteValuePropertyHeader(TypeCode.Object);
                Writer.Write((byte) token);
                Writer.Write(value.Id.ToGuid());
                Writer.Write(value.Name ?? string.Empty);
            }
            
            private void VisitObject(Object value)
            {
                var handle = UnityObjectSerializer.ToObjectHandle(value);
                WriteValuePropertyHeader(TypeCode.Object);
                Writer.Write((byte) UTinyBinaryToken.UnityObject);
                Writer.Write(handle.Guid ?? string.Empty);
                Writer.Write(handle.FileId);
                Writer.Write(handle.Type);
            }

            void ICustomVisit<UTinyId>.CustomVisit(UTinyId value)
            {
                WriteValuePropertyHeader(TypeCode.Object);
                Writer.Write((byte) UTinyBinaryToken.Id);
                Writer.Write(value.ToGuid());
            }

            void ICustomVisit<UTinyModule.Reference>.CustomVisit(UTinyModule.Reference value)
            {
                VisitReference(UTinyBinaryToken.ModuleReference, value);
            }

            void ICustomVisit<UTinyType.Reference>.CustomVisit(UTinyType.Reference value)
            {
                VisitReference(UTinyBinaryToken.TypeReference, value);
            }

            void ICustomVisit<UTinyEntityGroup.Reference>.CustomVisit(UTinyEntityGroup.Reference value)
            {
                VisitReference(UTinyBinaryToken.SceneReference, value);
            }

            void ICustomVisit<UTinyEntity.Reference>.CustomVisit(UTinyEntity.Reference value)
            {
                VisitReference(UTinyBinaryToken.EntityReference, value);
            }

            void ICustomVisit<UTinyScript.Reference>.CustomVisit(UTinyScript.Reference value)
            {
                VisitReference(UTinyBinaryToken.ScriptReference, value);
            }

            void ICustomVisit<UTinySystem.Reference>.CustomVisit(UTinySystem.Reference value)
            {
                VisitReference(UTinyBinaryToken.SystemReference, value);
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
                
                // skip empty lists
                var listProperty = property as IListProperty;
                if (listProperty != null && listProperty.Count(container) == 0)
                {
                    return true;
                }
                
                // skip default values
                var valueProperty = property as IUTinyValueProperty;
                if (valueProperty != null)
                {
                    if (!valueProperty.IsOverridden(container))
                    {
                        // skip the visit if the property is in its default value
                        return true;
                    }
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
        }
    }
}
#endif // NET_4_6
