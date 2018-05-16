#if NET_4_6
using System.Collections.Generic;

namespace Unity.Tiny
{
    public sealed partial class UTinyType
    {
        public static class Iterator
        {
            public static IEnumerable<Reference> DepthFirst(IEnumerable<UTinyType> types)
            {
                var graph = new AcyclicGraph<Reference>();
                
                // Push all types into the graph
                foreach (var type in types)
                {
                    graph.Add((Reference) type);
                }
                
                // Connect types
                foreach (var type in types)
                {
                    var typeNode = graph.GetOrAdd((Reference) type);
                    
                    foreach (var fieldType in EnumerateFieldTypes(type))
                    {
                        var fieldTypeNode = graph.GetOrAdd(fieldType);
                        graph.AddDirectedConnection(typeNode, fieldTypeNode);
                    }
                }

                var result = new List<Reference>();
                AcyclicGraphIterator.DepthFirst.Execute(graph, type =>
                {
                    result.Add(type);
                });

                return result;
            }

            private static IEnumerable<Reference> EnumerateFieldTypes(UTinyType type)
            {
                return EnumerateFieldTypes(type, new HashSet<Reference>());
            }

            private static IEnumerable<Reference> EnumerateFieldTypes(UTinyType type, ISet<Reference> visited)
            {
                foreach (var field in type.Fields)
                {
                    if (!visited.Add(field.FieldType))
                    {
                        continue;
                    }

                    var fieldType = field.FieldType.Dereference(type.Registry);

                    if (null == fieldType)
                    {
                        continue;
                    }
                    
                    yield return (Reference) fieldType;
                    
                    foreach (var fieldTypeReference in EnumerateFieldTypes(fieldType))
                    {
                        yield return fieldTypeReference;
                    }
                }
            }
        }
    }
}
#endif // NET_4_6
