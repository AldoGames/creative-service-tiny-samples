#if NET_4_6
using Unity.Tiny.Extensions;

namespace Unity.Tiny.Jira
{
    internal class Field<TValue> : IField
    {
        public readonly string FieldName;
        public readonly TValue Value;

        public Field(string fieldName, TValue value)
        {
            FieldName = fieldName;
            Value = value;
        }

        public override string ToString()
        {
            return $"{DoubleQuoted(FieldName)}:{DoubleQuoted(Value?.ToString() ?? string.Empty)}";
        }

        protected static string Braced(string str)
        {
            return str.Braced();
        }

        protected static string DoubleQuoted(string str)
        {
            return str.DoubleQuoted();
        }
    }
}
#endif