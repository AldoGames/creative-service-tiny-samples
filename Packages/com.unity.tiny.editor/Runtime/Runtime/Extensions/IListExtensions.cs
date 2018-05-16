#if NET_4_6
using System.Collections.Generic;
using UnityEngine.Assertions;

namespace Unity.Tiny
{
    public static class IListExtensions
    {
        public static void Swap<T>(this IList<T> list, int indexA, int indexB)
        {
            Assert.IsTrue(IsValidIndex(list, indexA));
            Assert.IsTrue(IsValidIndex(list, indexB));
            Assert.IsFalse(indexA == indexB);

            T tmp = list[indexA];
            list[indexA] = list[indexB];
            list[indexB] = tmp;
        }

        private static bool IsValidIndex<T>(IList<T> list, int index)
        {
            return index >= 0 && index < list.Count;
        }
    }
}
#endif // NET_4_6
