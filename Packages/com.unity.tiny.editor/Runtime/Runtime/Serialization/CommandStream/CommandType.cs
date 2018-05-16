#if NET_4_6
using System;

namespace Unity.Tiny.Serialization.CommandStream
{
    /// <summary>
    /// Command Types
    /// 
    /// The format of a command is 
    /// 
    /// [2 bytes ] [2 bytes ] [n bytes]
    /// 
    /// [CMD_TYPE] [DATA_LEN] [DATA...]
    /// 
    /// We reserve 2 bytes for built in command types
    /// </summary>
    public static class CommandType
    {
        public const byte None = 0;
        public const byte User = 1;
        
        public const byte CreateProject = 2;
        public const byte CreateModule = 3;
        public const byte CreateType = 4;
        public const byte CreateScene = 5;
        public const byte CreateEntity = 6;
        public const byte CreateScript = 7;
        public const byte CreateSystem = 8;
        public const byte Unregister = 9;

        public static byte GetCreateCommandType(UTinyTypeId typeId)
        {
            switch (typeId)
            {
                case UTinyTypeId.Unknown:
                    return None;
                case UTinyTypeId.Project:
                    return CreateProject;
                case UTinyTypeId.Module:
                    return CreateModule;
                case UTinyTypeId.Type:
                    return CreateType;
                case UTinyTypeId.Scene:
                    return CreateScene;
                case UTinyTypeId.Entity:
                    return CreateEntity;
                case UTinyTypeId.Script:
                    return CreateScript;
                case UTinyTypeId.System:
                    return CreateSystem;
                case UTinyTypeId.EnumReference:
                case UTinyTypeId.EntityReference:
                case UTinyTypeId.UnityObject:
                    return None;
                default:
                    throw new ArgumentOutOfRangeException(nameof(typeId), typeId, null);
            }
        }
    }
}
#endif // NET_4_6
