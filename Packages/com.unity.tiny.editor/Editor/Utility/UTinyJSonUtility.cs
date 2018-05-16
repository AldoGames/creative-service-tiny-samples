#if NET_4_6
using System;

using UnityEngine;

namespace Unity.Tiny
{
    public class UTinyJSonUtility
    {
        public static T[] getJsonArray<T>(string json)
        {
            string newJson = "{ \"array\": " + json + "}";
            Wrapper<T> wrapper = JsonUtility.FromJson<Wrapper<T>>(newJson);
            return wrapper.array;
        }

        [Serializable]
        private class Wrapper<T>
        {
            public T[] array = new T[0];
        }
    }
}
#endif
