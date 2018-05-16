#if NET_4_6
using System;
using System.Collections.Generic;

namespace Unity.Tiny.Filters
{
    using Tiny;

    public static partial class Filter
    {
        #region API
        public static IEnumerable<UTinyType.Reference> ConfigurationTypeRefs(this IEnumerable<UTinyModule> source)
        {
            if (null == source)
            {
                throw new ArgumentNullException(nameof(source));
            }
            return source.ConfigurationTypeRefsImpl();
        }

        public static IEnumerable<UTinyType> ConfigurationTypes(this IEnumerable<UTinyModule> source)
        {
            if (null == source)
            {
                throw new ArgumentNullException(nameof(source));
            }
            return source.ConfigurationTypesImpl();
        }
        
        public static IEnumerable<UTinyType.Reference> ComponentTypeRefs(this IEnumerable<UTinyModule> source)
        {
            if (null == source)
            {
                throw new ArgumentNullException(nameof(source));
            }
            return source.ComponentTypeRefsImpl();
        }

        public static IEnumerable<UTinyType> ComponentTypes(this IEnumerable<UTinyModule> source)
        {
            if (null == source)
            {
                throw new ArgumentNullException(nameof(source));
            }
            return source.ComponentTypesImpl();
        }

        public static IEnumerable<UTinyType.Reference> StructTypeRefs(this IEnumerable<UTinyModule> source)
        {
            if (null == source)
            {
                throw new ArgumentNullException(nameof(source));
            }
            return source.StructTypeRefsImpl();
        }

        public static IEnumerable<UTinyType> StructTypes(this IEnumerable<UTinyModule> source)
        {
            if (null == source)
            {
                throw new ArgumentNullException(nameof(source));
            }
            return source.StructTypesImpl();
        }

        public static IEnumerable<UTinyType.Reference> EnumTypeRefs(this IEnumerable<UTinyModule> source)
        {
            if (null == source)
            {
                throw new ArgumentNullException(nameof(source));
            }
            return source.EnumTypeRefsImpl();
        }

        public static IEnumerable<UTinyType> EnumTypes(this IEnumerable<UTinyModule> source)
        {
            if (null == source)
            {
                throw new ArgumentNullException(nameof(source));
            }
            return source.EnumTypesImpl();
        }

        public static IEnumerable<UTinyEntityGroup.Reference> SceneRefs(this IEnumerable<UTinyModule> source)
        {
            if (null == source)
            {
                throw new ArgumentNullException(nameof(source));
            }
            return source.SceneRefsImpl();
        }

        public static IEnumerable<UTinyEntityGroup> EntityGroups(this IEnumerable<UTinyModule> source)
        {
            if (null == source)
            {
                throw new ArgumentNullException(nameof(source));
            }
            return source.EntityGroupsImpl();
        }

        public static IEnumerable<UTinyEntity.Reference> EntityRefs(this IEnumerable<UTinyModule> source)
        {
            return source.EntityGroups().EntityRefs();
        }

        public static IEnumerable<UTinyEntity> Entities(this IEnumerable<UTinyModule> source)
        {
            return source.EntityGroups().Entities();
        }
        
        public static IEnumerable<UTinySystem.Reference> SystemRefs(this IEnumerable<UTinyModule> source)
        {
            return source.SystemRefsImpl();
        }
        
        public static IEnumerable<UTinySystem> Systems(this IEnumerable<UTinyModule> source)
        {
            if (null == source)
            {
                throw new ArgumentNullException(nameof(source));
            }
            return source.SystemsImpl();
        }
        
        public static IEnumerable<UTinyScript.Reference> ScriptRefs(this IEnumerable<UTinyModule> source)
        {
            return source.ScriptRefsImpl();
        }
        
        public static IEnumerable<UTinyScript> Scripts(this IEnumerable<UTinyModule> source)
        {
            if (null == source)
            {
                throw new ArgumentNullException(nameof(source));
            }
            return source.ScriptsImpl();
        }
        #endregion // API

        #region Implementation
        private static IEnumerable<UTinyType.Reference> ConfigurationTypeRefsImpl(this IEnumerable<UTinyModule> source)
        {
            foreach (var module in source)
            {
                foreach (var typeRef in module.Configurations)
                {
                    if (!UTinyType.Reference.None.Equals(typeRef))
                    {
                        yield return typeRef;
                    }
                }
            }
        }

        private static IEnumerable<UTinyType> ConfigurationTypesImpl(this IEnumerable<UTinyModule> source)
        {
            foreach (var module in source)
            {
                foreach (var typeRef in module.Configurations)
                {
                    var type = typeRef.Dereference(module.Registry);
                    if (null != type)
                    {
                        yield return type;
                    }
                }
            }
        }
        private static IEnumerable<UTinyType.Reference> ComponentTypeRefsImpl(this IEnumerable<UTinyModule> source)
        {
            foreach (var module in source)
            {
                foreach (var typeRef in module.Components)
                {
                    if (!UTinyType.Reference.None.Equals(typeRef))
                    {
                        yield return typeRef;
                    }
                }
            }
        }

        private static IEnumerable<UTinyType> ComponentTypesImpl(this IEnumerable<UTinyModule> source)
        {
            foreach (var module in source)
            {
                foreach (var typeRef in module.Components)
                {
                    var type = typeRef.Dereference(module.Registry);
                    if (null != type)
                    {
                        yield return type;
                    }
                }
            }
        }

        private static IEnumerable<UTinyType.Reference> StructTypeRefsImpl(this IEnumerable<UTinyModule> source)
        {
            foreach (var module in source)
            {
                foreach (var typeRef in module.Structs)
                {
                    if (!UTinyType.Reference.None.Equals(typeRef))
                    {
                        yield return typeRef;
                    }
                }
            }
        }

        private static IEnumerable<UTinyType> StructTypesImpl(this IEnumerable<UTinyModule> source)
        {
            foreach (var module in source)
            {
                foreach (var typeRef in module.Structs)
                {
                    var type = typeRef.Dereference(module.Registry);
                    if (null != type)
                    {
                        yield return type;
                    }
                }
            }
        }

        private static IEnumerable<UTinyType.Reference> EnumTypeRefsImpl(this IEnumerable<UTinyModule> source)
        {
            foreach (var module in source)
            {
                foreach (var typeRef in module.Enums)
                {
                    if (!UTinyType.Reference.None.Equals(typeRef))
                    {
                        yield return typeRef;
                    }
                }
            }
        }

        private static IEnumerable<UTinyType> EnumTypesImpl(this IEnumerable<UTinyModule> source)
        {
            foreach (var module in source)
            {
                foreach (var typeRef in module.Enums)
                {
                    var type = typeRef.Dereference(module.Registry);
                    if (null != type)
                    {
                        yield return type;
                    }
                }
            }
        }

        private static IEnumerable<UTinyEntityGroup.Reference> SceneRefsImpl(this IEnumerable<UTinyModule> source)
        {
            foreach(var module in source)
            {
                foreach(var entityGroupRefs in module.EntityGroups)
                {
                    if (!UTinyEntityGroup.Reference.None.Equals(entityGroupRefs))
                    {
                        yield return entityGroupRefs;
                    }
                }
            }
        }

        private static IEnumerable<UTinyEntityGroup> EntityGroupsImpl(this IEnumerable<UTinyModule> source)
        {
            foreach (var module in source)
            {
                foreach (var entityGroupRef in module.EntityGroups)
                {
                    var entityGroup = entityGroupRef.Dereference(module.Registry);
                    if (null != entityGroup)
                    {
                        yield return entityGroup;
                    }
                }
            }
        }
        
        private static IEnumerable<UTinySystem.Reference> SystemRefsImpl(this IEnumerable<UTinyModule> source)
        {
            foreach (var module in source)
            {
                foreach (var systemRef in module.Systems)
                {
                    if (!UTinySystem.Reference.None.Equals(systemRef))
                    {
                        yield return systemRef;
                    }
                }
            }
        }
        
        private static IEnumerable<UTinySystem> SystemsImpl(this IEnumerable<UTinyModule> source)
        {
            foreach (var module in source)
            {
                foreach (var systemRef in module.Systems)
                {
                    var system = systemRef.Dereference(module.Registry);
                    if (null != system)
                    {
                        yield return system;
                    }
                }
            }
        }
        
        private static IEnumerable<UTinyScript.Reference> ScriptRefsImpl(this IEnumerable<UTinyModule> source)
        {
            foreach (var module in source)
            {
                foreach (var scriptRef in module.Scripts)
                {
                    if (!UTinyScript.Reference.None.Equals(scriptRef))
                    {
                        yield return scriptRef;
                    }
                }
            }
        }
        
        private static IEnumerable<UTinyScript> ScriptsImpl(this IEnumerable<UTinyModule> source)
        {
            foreach (var module in source)
            {
                foreach (var scriptRef in module.Scripts)
                {
                    var script = scriptRef.Dereference(module.Registry);
                    if (null != script)
                    {
                        yield return script;
                    }
                }
            }
        }
        #endregion // Implementation
    }

}
#endif // NET_4_6
