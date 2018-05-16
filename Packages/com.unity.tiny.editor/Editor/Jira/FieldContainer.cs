#if NET_4_6
using System.Collections.Generic;

namespace Unity.Tiny.Jira
{
    internal class FieldContainer : Field<List<IField>>
    {
        public FieldContainer(string fieldName) : base(fieldName, new List<IField>())
        {
        }

        public void Add(IField field)
        {
            Value.Add(field);
        }

        public override string ToString()
        {
            if (Value.Count == 0)
            {
                return "";
            }

            var values = $":{Braced(string.Join(", ", (object[])Value.ToArray()))}";
            if (string.IsNullOrEmpty(FieldName))
            {
                return values;
            }
            return $"{DoubleQuoted(FieldName)}{values}";
        }
    }
}
#endif