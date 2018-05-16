#if NET_4_6
using System;
using System.IO;
using Unity.Properties.Serialization;
using UnityEngine.Assertions;
using Unity.Tiny.Serialization.Binary;

namespace Unity.Tiny.Serialization.CommandStream
{
    public class CommandObjectReader : BinaryObjectReader
    {
        private static class UserDefinedObjectReader
        {
            private static readonly byte[] s_GuidBuffer = new byte[16];

            public static object Read(BinaryReader reader)
            {
                var token = (UTinyBinaryToken) reader.ReadByte();

                switch (token)
                {
                    case UTinyBinaryToken.Id:
                        return ReadId(reader);
                    case UTinyBinaryToken.ModuleReference:
                        return new UTinyModule.Reference(ReadId(reader), reader.ReadString());
                    case UTinyBinaryToken.TypeReference:
                        return new UTinyType.Reference(ReadId(reader), reader.ReadString());
                    case UTinyBinaryToken.SceneReference:
                        return new UTinyEntityGroup.Reference(ReadId(reader), reader.ReadString());
                    case UTinyBinaryToken.EntityReference:
                        return new UTinyEntity.Reference(ReadId(reader), reader.ReadString());
                    case UTinyBinaryToken.ScriptReference:
                        return new UTinyScript.Reference(ReadId(reader), reader.ReadString());
                    case UTinyBinaryToken.SystemReference:
                        return new UTinySystem.Reference(ReadId(reader), reader.ReadString());
                    case UTinyBinaryToken.UnityObject:
                        var guid = reader.ReadString();
                        var fileId = reader.ReadInt64();
                        var type = reader.ReadInt32();
                        return UnityObjectSerializer.FromObjectHandle(new UnityObjectHandle {Guid = guid, FileId = fileId, Type = type});
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }

            private static UTinyId ReadId(BinaryReader reader)
            {
                reader.Read(s_GuidBuffer, 0, 16);
                return new UTinyId(new Guid(s_GuidBuffer));
            }
        }
        
        public CommandObjectReader(IObjectFactory objectFactory) : base(objectFactory)
        {
            UserDefinedValueDelegate = UserDefinedObjectReader.Read;
        }

        /// <summary>
        /// WIP attempting toe make a 'fast' path for entitiy deserialization... its currently the same speed or slow than the generic
        /// Makes assumptions about the structure of the data
        /// </summary>
        /// <param name="reader"></param>
        /// <param name="registry"></param>
        public void ReadEntity(BinaryReader reader, IRegistry registry)
        {
            var token = reader.ReadByte();
            Assert.AreEqual(token, BinaryToken.BeginObject);
            reader.ReadUInt32();

            // Read TypeId
            ReadPropertyName(reader);
            ReadPropertyValue(reader);

            // Read Id
            ReadPropertyName(reader);
            var id = ParseId(ReadPropertyValue(reader));

            // Read Name
            ReadPropertyName(reader);
            var name = ReadPropertyValue(reader) as string;

            var entity = registry.CreateEntity(id, name);

            // Read Components
            ReadPropertyName(reader);
            token = reader.ReadByte();
            Assert.AreEqual(BinaryToken.BeginArray, token);

            // Read object size
            // NOTE: This is NOT the length, this is the bytesize from the BeginArray token to the EndArray token
            reader.ReadUInt32();

            while ((token = reader.ReadByte()) != BinaryToken.EndArray)
            {
                Assert.AreEqual(BinaryToken.BeginObject, token);
                reader.ReadUInt32(); // Size
                ReadPropertyName(reader); // "Type"
                var type = ReadTypeReferenceValue(reader);

                var component = entity.AddComponent(type);
                component.Refresh();

                // Read Name
                ReadPropertyName(reader);
                ReadPropertyValue(reader);
                
                ReadPropertyName(reader); // "Properties"
                
                token = reader.ReadByte();
                Assert.AreEqual(BinaryToken.BeginObject, token);
                reader.ReadUInt32(); // Size
                while ((token = reader.ReadByte()) != BinaryToken.EndObject)
                {
                    reader.BaseStream.Position -= 1;
                    ReadUTinyObjectProperty(reader, component);
                }
                token = reader.ReadByte(); // EndObject
                Assert.AreEqual(BinaryToken.EndObject, token);
            }
        }

        private void ReadUTinyObjectProperty(BinaryReader reader, UTinyObject @object)
        {
            var key = ReadPropertyName(reader); // Dynamic property name

            var token = reader.ReadByte();

            switch (token)
            {
                case BinaryToken.Value:
                {
                    @object[key] = ReadValue(reader);
                    return;
                }
                case BinaryToken.BeginObject:
                {
                    reader.ReadUInt32(); // Size
                    ReadPropertyName(reader); // "Type"
                    ReadTypeReferenceValue(reader);

                    // Read Name
                    ReadPropertyName(reader);
                    ReadPropertyValue(reader);
            
                    ReadPropertyName(reader); // "Properties"
            
                    token = reader.ReadByte();
                    Assert.AreEqual(BinaryToken.BeginObject, token);
                    reader.ReadUInt32(); // Size
                    var inner = @object[key] as UTinyObject;
                    while ((token = reader.ReadByte()) != BinaryToken.EndObject)
                    {
                        reader.BaseStream.Position -= 1;
                        ReadUTinyObjectProperty(reader, inner);
                    }
                    token = reader.ReadByte();
                    Assert.AreEqual(BinaryToken.EndObject, token);
                    return;
                }
                case BinaryToken.BeginArray:
                    throw new NotSupportedException();
            }
        }

        private UTinyType.Reference ReadTypeReferenceValue(BinaryReader reader)
        {
            var token = reader.ReadByte();

            switch (token)
            {
                case BinaryToken.Value:
                {
                    return (UTinyType.Reference) ReadValue(reader);
                }

                case BinaryToken.BeginObject:
                {
                    reader.ReadUInt32(); // Size
                    ReadPropertyName(reader); // "Id"
                    var id = ParseId(ReadPropertyValue(reader));
                    ReadPropertyName(reader); // "Name"
                    var name = ReadPropertyValue(reader) as string;
                    reader.ReadByte(); // EndObject
                    return new UTinyType.Reference(id, name);
                }
                case BinaryToken.BeginArray:
                    throw new NotSupportedException();
            }

            return new UTinyType.Reference();
        }

        private static UTinyId ParseId(object obj)
        {
            if (null == obj)
            {
                return UTinyId.Empty;
            }

            if (obj is UTinyId)
            {
                return (UTinyId) obj;
            }

            var s = obj as string;
            return s != null ? new UTinyId(s) : UTinyId.Empty;
        }
    }
}
#endif // NET_4_6
