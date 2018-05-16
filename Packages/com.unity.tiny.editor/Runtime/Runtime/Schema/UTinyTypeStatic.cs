#if NET_4_6
using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Properties;

namespace Unity.Tiny
{
    public partial class UTinyType
    {
        public static UTinyType NewComponentType { get; }
        public static UTinyType Int8 { get; }
        public static UTinyType Int16 { get; }
        public static UTinyType Int32 { get; }
        public static UTinyType Int64 { get; }
        public static UTinyType UInt8 { get; }
        public static UTinyType UInt16 { get; }
        public static UTinyType UInt32 { get; }
        public static UTinyType UInt64 { get; }
        public static UTinyType Float32 { get; }
        public static UTinyType Float64 { get; }
        public static UTinyType Boolean { get; }
        public static UTinyType Char { get; }
        public static UTinyType String { get; }
        public static UTinyType EntityReference { get; }
        
        // Asset entity reference type
        // These types will map to an entity in the runtime `world.getByName('assets/{assetType}/{assetName}')`
        public static UTinyType Texture2DEntity { get; }
        public static UTinyType SpriteEntity { get; }
        public static UTinyType AudioClipEntity { get; }
        public static UTinyType FontEntity { get; }
        
        public static IList<UTinyType> BuiltInTypes { get; }

        static UTinyType()
        {
            NewComponentType = new UTinyType(null, PassthroughVersionStorage.Instance) {Id = new UTinyId("696b7de3df0f4887abca1fb6aa7f2615"), Name = "NewComponentType", TypeCode = UTinyTypeCode.Component};

            // @NOTE: Primitives do not belong to a single registry and are shared across all registries
            Int8 = new UTinyType(null, PassthroughVersionStorage.Instance) {Id = new UTinyId("27c155635ccb4ab2bcb79ef5aaf129ec"), Name = "Int8", TypeCode = UTinyTypeCode.Int8};
            Int16 = new UTinyType(null, PassthroughVersionStorage.Instance) {Id = new UTinyId("2aa56ce081e14e8a93d276da72d813bc"), Name = "Int16", TypeCode = UTinyTypeCode.Int16};
            Int32 = new UTinyType(null, PassthroughVersionStorage.Instance) {Id = new UTinyId("9633c95a0a68473682f09ed6a01194b4"), Name = "Int32", TypeCode = UTinyTypeCode.Int32};
            Int64 = new UTinyType(null, PassthroughVersionStorage.Instance) {Id = new UTinyId("37695933217a49f68ce15db33f63cdf9"), Name = "Int64", TypeCode = UTinyTypeCode.Int64};
            UInt8 = new UTinyType(null, PassthroughVersionStorage.Instance) {Id = new UTinyId("7112767112f747e2a340404a5ceb31b5"), Name = "UInt8", TypeCode = UTinyTypeCode.UInt8};
            UInt16 = new UTinyType(null, PassthroughVersionStorage.Instance) {Id = new UTinyId("86fa32ad22614762afdacbf4dba8180f"), Name = "UInt16", TypeCode = UTinyTypeCode.UInt16};
            UInt32 = new UTinyType(null, PassthroughVersionStorage.Instance) {Id = new UTinyId("1da58c8ba95a4c85a2b5920bd0663f70"), Name = "UInt32", TypeCode = UTinyTypeCode.UInt32};
            UInt64 = new UTinyType(null, PassthroughVersionStorage.Instance) {Id = new UTinyId("574059163cda44b3ade6ea7b2daf67f2"), Name = "UInt64", TypeCode = UTinyTypeCode.UInt64};
            Float32 = new UTinyType(null, PassthroughVersionStorage.Instance) {Id = new UTinyId("67325dccf2f047c19c7ef4a045354e67"), Name = "Float32", TypeCode = UTinyTypeCode.Float32};
            Float64 = new UTinyType(null, PassthroughVersionStorage.Instance) {Id = new UTinyId("74cf32c2744342b7903871f8feb2fdd7"), Name = "Float64", TypeCode = UTinyTypeCode.Float64};
            Boolean = new UTinyType(null, PassthroughVersionStorage.Instance) {Id = new UTinyId("2b477f505af74487b7092b5617d88d3f"), Name = "Boolean", TypeCode = UTinyTypeCode.Boolean};
            Char = new UTinyType(null, PassthroughVersionStorage.Instance) {Id = new UTinyId("e9c46111504a470885988383d2091dc2"), Name = "Char", TypeCode = UTinyTypeCode.Char};
            String = new UTinyType(null, PassthroughVersionStorage.Instance) {Id = new UTinyId("1bff5adddd7c41de98d3329c7c641208"), Name = "String", TypeCode = UTinyTypeCode.String};
            EntityReference = new UTinyType(null, PassthroughVersionStorage.Instance) {Id = new UTinyId("5a182d9d039d4dfd8fa96132d05f9ee7"), Name = "EntityReference", TypeCode = UTinyTypeCode.EntityReference};
            
            // Asset entity reference types
            Texture2DEntity = new UTinyType(null, PassthroughVersionStorage.Instance) {Id = new UTinyId("373ed9034ede4f84829bf01ed265f6ee"), Name = "Texture2DEntity", TypeCode = UTinyTypeCode.UnityObject};
            SpriteEntity = new UTinyType(null, PassthroughVersionStorage.Instance) {Id = new UTinyId("cf54a635a25248ab87f2563bb840ed5b"), Name = "SpriteEntity", TypeCode = UTinyTypeCode.UnityObject};
            AudioClipEntity = new UTinyType(null, PassthroughVersionStorage.Instance) {Id = new UTinyId("1ae8c073dc444f4fb2d3120e5e618326"), Name = "AudioClipEntity", TypeCode = UTinyTypeCode.UnityObject};
            FontEntity = new UTinyType(null, PassthroughVersionStorage.Instance) {Id = new UTinyId("4b1f918c1c564e42a04a0cb8f4ee0665"), Name = "FontEntity", TypeCode = UTinyTypeCode.UnityObject};

            BuiltInTypes = new List<UTinyType>
            {
                Int8, Int16, Int32, Int64,
                UInt8, UInt16, UInt32, UInt64,
                Float32, Float64, 
                Boolean, Char, String,
                EntityReference,
                Texture2DEntity, SpriteEntity, AudioClipEntity, FontEntity
            };
        }

        /// <summary>
        /// Returns the built in type based on the given typeCode
        /// </summary>
        public static UTinyType GetType(UTinyTypeCode typeCode)
        {
            switch (typeCode)
            {
                case UTinyTypeCode.Int8:
                    return Int8;
                case UTinyTypeCode.Int16:
                    return Int16;
                case UTinyTypeCode.Int32:
                    return Int32;
                case UTinyTypeCode.Int64:
                    return Int64;
                case UTinyTypeCode.UInt8:
                    return UInt8;
                case UTinyTypeCode.UInt16:
                    return UInt16;
                case UTinyTypeCode.UInt32:
                    return UInt32;
                case UTinyTypeCode.UInt64:
                    return UInt64;
                case UTinyTypeCode.Float32:
                    return Float32;
                case UTinyTypeCode.Float64:
                    return Float64;
                case UTinyTypeCode.Boolean:
                    return Boolean;
                case UTinyTypeCode.Char:
                    return Char;
                case UTinyTypeCode.String:
                    return String;
                case UTinyTypeCode.EntityReference:
                    return EntityReference;
                default:
                    return null;
            }
        }

        public static Reference GetTypeReference(UTinyTypeCode typeCode)
        {
            var type = GetType(typeCode);
            
            if (null == type)
            {
                return new Reference();
            }
            
            return (Reference) type;
        }
        
        /// <summary>
        /// Determines if the provided System.Type maps to a built-in UTinyType.
        /// </summary>
        public static bool TryGetType(Type type, out UTinyType uTinyType)
        {
            uTinyType = GetType(type);
            return null != uTinyType;
        }

        /// <summary>
        /// Fast access to built in types
        /// </summary>
        public static UTinyType GetType(Type type)
        {
            if (type.IsEnum)
            {
                return null;
            }
            
            var typeCode = Type.GetTypeCode(type);
            
            switch (typeCode)
            {
                case System.TypeCode.SByte:
                    return Int8;
                case System.TypeCode.Int16:
                    return Int16;
                case System.TypeCode.Int32:
                    return Int32;
                case System.TypeCode.Int64:
                    return Int64;
                case System.TypeCode.Byte:
                    return UInt8;
                case System.TypeCode.UInt16:
                    return UInt16;
                case System.TypeCode.UInt32:
                    return UInt32;
                case System.TypeCode.UInt64:
                    return UInt64;
                case System.TypeCode.Single:
                    return Float32;
                case System.TypeCode.Double:
                    return Float64;
                case System.TypeCode.Boolean:
                    return Boolean;
                case System.TypeCode.Char:
                    return Char;
                case System.TypeCode.String:
                    return String;
            }

            if (typeof(UnityEngine.Texture2D).IsAssignableFrom(type))
            {
                return Texture2DEntity;
            }
            
            if (typeof(UnityEngine.Sprite).IsAssignableFrom(type))
            {
                return SpriteEntity;
            }

            if (typeof(UnityEngine.AudioClip).IsAssignableFrom(type))
            {
                return AudioClipEntity;
            }

            if (typeof(UnityEngine.Font).IsAssignableFrom(type))
            {
                return FontEntity;
            }

            return null;
        }
        /// <summary>
        /// Creates a runtime instance of the given type
        /// 
        /// NOTE: For primitives this returns the default .NET value
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public static object CreateInstance(UTinyType type)
        {            
            var typeCode = type.TypeCode;

            switch (typeCode)
            {
                case UTinyTypeCode.Int8:
                    return default(sbyte);
                case UTinyTypeCode.Int16:
                    return default(short);
                case UTinyTypeCode.Int32:
                    return default(int);
                case UTinyTypeCode.Int64:
                    return default(long);
                case UTinyTypeCode.UInt8:
                    return default(byte);
                case UTinyTypeCode.UInt16:
                    return default(ushort);
                case UTinyTypeCode.UInt32:
                    return default(uint);
                case UTinyTypeCode.UInt64:
                    return default(ulong);
                case UTinyTypeCode.Float32:
                    return default(float);
                case UTinyTypeCode.Float64:
                    return default(double);
                case UTinyTypeCode.Boolean:
                    return default(bool);
                case UTinyTypeCode.Char:
                    return default(char);
                case UTinyTypeCode.String:
                    return string.Empty;
                default:
                    return null;
            }
        }
        
        public static IList CreateListInstance(UTinyType type)
        {            
            var typeCode = type.TypeCode;

            switch (typeCode)
            {
                case UTinyTypeCode.Int8:
                    return new List<sbyte>();
                case UTinyTypeCode.Int16:
                    return new List<short>();
                case UTinyTypeCode.Int32:
                    return new List<int>();
                case UTinyTypeCode.Int64:
                    return new List<long>();
                case UTinyTypeCode.UInt8:
                    return new List<byte>();
                case UTinyTypeCode.UInt16:
                    return new List<ushort>();
                case UTinyTypeCode.UInt32:
                    return new List<uint>();
                case UTinyTypeCode.UInt64:
                    return new List<ulong>();
                case UTinyTypeCode.Float32:
                    return new List<float>();
                case UTinyTypeCode.Float64:
                    return new List<double>();
                case UTinyTypeCode.Boolean:
                    return new List<bool>();
                case UTinyTypeCode.Char:
                    return new List<char>();
                case UTinyTypeCode.String:
                    return new List<string>();
                case UTinyTypeCode.Configuration:
                case UTinyTypeCode.Component:
                case UTinyTypeCode.Struct:
                    return new List<UTinyObject>();
                case UTinyTypeCode.Enum:
                    return new List<UTinyEnum.Reference>();
                case UTinyTypeCode.EntityReference:
                    return new List<UTinyEntity.Reference>();
                case UTinyTypeCode.UnityObject:
                    if (type.Id == Texture2DEntity.Id)
                    {
                        return new List<UnityEngine.Texture2D>();
                    }
                    else if (type.Id == SpriteEntity.Id)
                    {
                        return new List<UnityEngine.Sprite>();
                    }
                    else if (type.Id == AudioClipEntity.Id)
                    {
                        return new List<UnityEngine.AudioClip>();
                    }
                    else if (type.Id == FontEntity.Id)
                    {
                        return new List<UnityEngine.Font>();
                    }
                    else
                    {
                        return new List<UnityEngine.Object>();
                    }
                default:
                    throw new ArgumentOutOfRangeException(nameof(type.TypeCode), type.TypeCode, null);
            }
        }
    }
}
#endif // NET_4_6
