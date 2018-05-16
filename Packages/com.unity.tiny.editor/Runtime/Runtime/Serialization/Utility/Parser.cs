#if NET_4_6
using System;
using System.Collections.Generic;
using Object = UnityEngine.Object;

namespace Unity.Tiny.Serialization
{
    public static class Parser
    {
        public static object GetValue(IDictionary<string, object> dictionary, string key)
        {
            object obj;
            return !dictionary.TryGetValue(key, out obj) ? null : obj;
        }

        public static T GetValue<T>(IDictionary<string, object> dictionary, string key)
        {
            object obj;
            if (!dictionary.TryGetValue(key, out obj))
            {
                return default(T);
            }
            return (T)obj;
        }

        public static int ParseInt(object obj)
        {
            if (null == obj)
            {
                return 0;
            }

            if (obj is int)
            {
                return (int)obj;
            }

            var convertible = obj as IConvertible;
            if (convertible != null)
            {
                return Convert.ToInt32(convertible);
            }

            var s = obj as string;

            int intValue;
            return int.TryParse(s, out intValue) ? intValue : 0;
        }

        public static long ParseLong(object obj)
        {
            if (null == obj)
            {
                return 0;
            }

            if (obj is long)
            {
                return (long)obj;
            }

            var convertible = obj as IConvertible;
            if (convertible != null)
            {
                return Convert.ToInt64(convertible);
            }

            var s = obj as string;

            long longValue;
            return long.TryParse(s, out longValue) ? longValue : 0;
        }

        public static Object ParseUnityObject(object obj)
        {
            var o = obj as Object;
            if (o != null)
            {
                return o;
            }

            var dictionary = obj as IDictionary<string, object>;
            if (null == dictionary)
            {
                return null;
            }

            var guid = GetValue<string>(dictionary, "Guid");
            var fileId = ParseLong(GetValue(dictionary, "FileId"));
            var type = ParseInt(GetValue(dictionary, "Type"));

            return UnityObjectSerializer.FromObjectHandle(new UnityObjectHandle { Guid = guid, FileId = fileId, Type = type });
        }

        public static UTinyEntityGroup.Reference ParseSceneReference(object obj)
        {
            if (obj is UTinyEntityGroup.Reference)
            {
                return (UTinyEntityGroup.Reference) obj;
            }

            var dictionary = obj as IDictionary<string, object>;
            return dictionary != null ? new UTinyEntityGroup.Reference(ParseId(dictionary["Id"]), dictionary["Name"] as string) : new UTinyEntityGroup.Reference();
        }
        
        public static UTinyId ParseId(object obj)
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
