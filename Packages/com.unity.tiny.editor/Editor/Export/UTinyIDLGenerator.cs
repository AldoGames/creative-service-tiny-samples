#if NET_4_6
using System;
using System.IO;
using System.Linq;
using System.Text;

namespace Unity.Tiny
{
    public static class UTinyIDLGenerator
    {
        /// <summary>
        /// Generates the platform-agnostic IDL file from the given UTinyProject.
        /// </summary>
        public static void GenerateIDL(UTinyProject project, FileInfo destination)
        {
            var writer = new UTinyCodeWriter
            {
                CodeStyle = CodeStyle.CSharp
            };
            
            writer.Line("using UTiny;");

            var mainModule = project.Module.Dereference(project.Registry);
            foreach (var dep in mainModule.EnumerateDependencies()) {
                if (dep.Equals(mainModule))
                    continue;
                writer.Line($"using {dep.Name};");
            }

            project.Visit(new GenerateIDLVisitor {Writer = writer});
            File.WriteAllText(destination.FullName, writer.ToString(), Encoding.UTF8);
        }

        private class GenerateIDLVisitor : UTinyProject.Visitor
        {
            public UTinyCodeWriter Writer { private get; set; }

            private UTinyModule m_Module;

            private Scope m_NamespaceScope;

            public override void BeginModule(UTinyModule module)
            {
                m_Module = module;
                
                if (module.EntityGroups.Count > 0)
                {
                    // The idea here is we want to pack the component within the runtime `entityGroup` javascript object
                    // Each group resolves to a javascript object like so `{ENTITY_GROUPS}.{NAMESPACE}.{GROUP_NAME}`
                    //
                    // The group component lives in this object with the name `Component`
                    //
                    // e.g.
                    //
                    // - "MyGroup" becomes the runtime object `entities.game.MyGroup = {}`
                    // - The generated component becomes `entities.game.MyGroup.Component = function(w, e) {...}`
                    Writer.Line();
                    Writer.Line($"/*");
                    Writer.Line($" * !!! TEMP UNITL PROPER SCENE FORMAT !!!");
                    Writer.Line($" */");
                    using (Writer.Scope($"namespace {UTinyHTML5Builder.KEntityGroupNamespace}.{module.Namespace}"))
                    {
                        foreach (var entityGroup in module.EntityGroups)
                        {
                            using (Writer.Scope($"namespace {entityGroup.Dereference(module.Registry).Name}"))
                            {
                                VisitType(new UTinyType(null, null)
                                {
                                    TypeCode = UTinyTypeCode.Component,
                                    Name = "Component"
                                });
                            }
                        }
                    }

                }
                
                Writer.Line();
                m_NamespaceScope = Writer.Scope($"namespace {module.Namespace}");
            }

            public override void EndModule(UTinyModule module)
            {
                m_NamespaceScope.Dispose();
                m_NamespaceScope = null;
            }

            public override void VisitType(UTinyType type)
            {
                if (type.IsRuntimeIncluded)
                {
                    // @HACK
                    if (type.Name == "Camera2D")
                    {
                        GenerateLayerComponents();
                    }
                    
                    return;
                }

                if (type.IsEnum)
                {
                    // @TODO Handle underlying type - for now we assume C#-friendly types
                    using (Writer.Scope($"enum {type.Name}"))
                    {
                        var defaultValue = type.DefaultValue as UTinyObject;
                        var first = true;
                        foreach (var field in type.Fields)
                        {
                            var value = defaultValue[field.Name];
                            Writer.Line($"{(first ? "" : ", ")}{field.Name} = {value}");
                            first = false;
                        }
                    }
                }
                else
                {
                    WriteType(type);
                }
            }
            
            private void GenerateLayerComponents()
            {
                using (Writer.Scope("namespace layers"))
                {
                    
                    for (var i = 0; i < 32; ++i)
                    {
                        var name = UnityEngine.LayerMask.LayerToName(i);
                        if (string.IsNullOrEmpty(name))
                        {
                            continue;
                        }

                        VisitType(new UTinyType(null, null)
                        {
                            TypeCode = UTinyTypeCode.Component,
                            Name = name.Replace(" ", "")
                        });
                    }
                }
            }

            public override void VisitEntityGroup(UTinyEntityGroup entityGroup)
            {
                
            }


            public override void VisitSystem(UTinySystem system)
            {
                if (system.IsRuntimeIncluded)
                {
                    return;
                }

                foreach (var d in system.ExecuteBefore)
                {
                    var dep = d.Dereference(m_Module.Registry);
                    Writer.Line($"[UpdateBefore(typeof({dep.Name}))]");
                }
                
                foreach (var d in system.ExecuteAfter)
                {
                    var dep = d.Dereference(m_Module.Registry);
                    Writer.Line($"[UpdateAfter(typeof({dep.Name}))]");
                }
                
                using (Writer.Scope($"public class {system.Name} : IComponentSystem"))
                {
                    
                }
            }

            private void WriteType(UTinyType type)
            {
                var typePost = "";
                if (type.TypeCode == UTinyTypeCode.Component) {
                    typePost = " : IComponentData";
                }

                using (Writer.Scope($"public struct {type.Name}{typePost}"))
                {
                    foreach (var field in type.Fields)
                    {
                        Writer.LineFormat(field.Array ? "public DynamicArray<{0}> {1};" : "public {0} {1};", FieldTypeToIDL(type.Registry, field), field.Name);
                    }
                }
            }

            /// <summary>
            /// Returns the module that this enum type belongs to
            /// </summary>
            private static UTinyModule GetEnumModule(IRegistry registry, UTinyType.Reference type)
            {
                // @TODO Optimization/direct lookup... this is really bad
                var modules = registry.FindAllByType<UTinyModule>();
                return modules.FirstOrDefault(module => module.Enums.Contains(type));
            }

            private static string FieldTypeToIDL(IRegistry registry, UTinyField field)
            {
                // @TODO Use UTinyBuildUtility.GetCSharpTypeName
                var code = field.FieldType.Dereference(registry).TypeCode;
                
                switch (code)
                {
                    case UTinyTypeCode.Boolean: return "bool";
                    case UTinyTypeCode.Int8: return "sbyte";
                    case UTinyTypeCode.Int16: return "short";
                    case UTinyTypeCode.Int32: return "int";
                    case UTinyTypeCode.Int64: return "long";
                    case UTinyTypeCode.UInt8: return "byte";
                    case UTinyTypeCode.UInt16: return "ushort";
                    case UTinyTypeCode.UInt32: return "uint";
                    case UTinyTypeCode.UInt64: return "ulong";
                    case UTinyTypeCode.Float32: return "float";
                    case UTinyTypeCode.Float64: return "double";
                    case UTinyTypeCode.Char: return "char";
                    case UTinyTypeCode.String: return "string";
                    case UTinyTypeCode.EntityReference: return "Entity";
                    case UTinyTypeCode.UnityObject: return "Entity";
                    case UTinyTypeCode.Struct:
                    case UTinyTypeCode.Enum:
                        var name = field.FieldType.Name;
                        var module = GetEnumModule(registry, field.FieldType);
                        if (!string.IsNullOrEmpty(module?.Namespace) && !module.Namespace.Equals("ut"))
                        {
                            name = $"{module.Namespace}.{name}";
                        }
                        return name;
                    default:
                        throw new NotSupportedException($"UTinyTypeCode '{code.ToString()}' is not supported in IDL at the moment");
                }
            }
        }
    }
}
#endif // NET_4_6
