#if NET_4_6
using System.Collections.Generic;
using System.IO;
using Unity.Properties;

namespace Unity.Tiny.Serialization.CommandStream
{
    /// <summary>
    /// Writes objects as commands to a stream
    /// </summary>
    public static class BackEnd
    {
        public static void Persist(Stream output, params IPropertyContainer[] objects)
        {
            Persist(output, (IEnumerable<IPropertyContainer>) objects);
        }

        public static void Persist(Stream output, IEnumerable<IPropertyContainer> objects)
        {
            var commandStreamWriter = new BinaryWriter(output);
            
            using (var memory = new MemoryStream())
            {
                foreach (var obj in objects)
                {
                    (obj as IRegistryObject)?.Refresh();
                    
                    var container = obj;
                    
                    var typeId = (UTinyTypeId) obj.PropertyBag.FindProperty("$TypeId").GetObjectValue(container);
                    commandStreamWriter.Write(CommandType.GetCreateCommandType(typeId));

                    // Use binary serialization protocol
                    Binary.BackEnd.Persist(memory, obj);
                    
                    // Write the payload size
                    commandStreamWriter.Write((uint) memory.Position);
                    
                    // Write the payload
                    commandStreamWriter.Write(memory.GetBuffer(), 0, (int) memory.Position);
                    memory.Position = 0;
                }
            }
        }
    }
}
#endif // NET_4_6
