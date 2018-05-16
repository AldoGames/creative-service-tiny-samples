#if NET_4_6
using System.IO;
using Unity.Tiny.Serialization.Binary;

namespace Unity.Tiny.Serialization.CommandStream
{
    public class CommandStreamWriter : BinaryWriter
    {
        public void Unregister(UTinyId id)
        {
            Write(CommandType.Unregister);
            Write((uint) 16);
            this.Write(id.ToGuid());
        }
    }
}
#endif // NET_4_6
