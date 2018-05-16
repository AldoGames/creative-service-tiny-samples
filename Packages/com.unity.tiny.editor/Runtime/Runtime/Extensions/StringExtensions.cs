#if NET_4_6
namespace Unity.Tiny.Extensions
{
    public static class StringExtensions
    {
        public static string SingleQuoted(this string str)
        {
            return $"'{str}'";
        }

        public static string DoubleQuoted(this string str)
        {
            return $"\"{str}\"";
        }

        public static string Braced(this string str)
        {
            return $"{{{str}}}";
        }
    }
}
#endif