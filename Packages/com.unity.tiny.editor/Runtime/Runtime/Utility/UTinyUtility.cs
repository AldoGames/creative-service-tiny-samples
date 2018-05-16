#if NET_4_6
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Assertions;

namespace Unity.Tiny
{
    public static class UTinyUtility
    {
        public static string GetUniqueName<T>(IEnumerable<T> elements, string name) where T : IReference
        {
            var current = name;
            var index = 1;

            while (true)
            {
                if (elements.All(element => !string.Equals(element.Name, current)))
                {
                    return current;
                }

                current = $"{name}{index++}";
            }
        }
        
        public static string GetUniqueName(IEnumerable<UTinyField> elements, string name)
        {
            var current = name;
            var index = 1;

            while (true)
            {
                if (elements.All(element => !string.Equals(element.Name, current)))
                {
                    return current;
                }

                current = $"{name}{index++}";
            }
        }
        
        public static bool IsValidObjectName(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                return false;
            }

            // The typeName MUST start with a letter or underscore
            if (!(name[0] == '_' || char.IsLetter(name[0])))
            {
                return false;
            }

            // The typeName may contain letters/numbers or underscores
            for (var i = 1; i < name.Length; i++)
            {
                if (!(name[i] == '_' || char.IsLetterOrDigit(name[i])))
                {
                    return false;
                }
            }

            return true;
        }
        
        public static bool IsValidNamespaceName(string name)
        {
            if (string.IsNullOrEmpty(name))
            {
                return false;
            }

            // The typeName MUST start with a letter or underscore
            if (!(name[0] == '_' || char.IsLetter(name[0])))
            {
                return false;
            }

            for (var i = 1; i < name.Length; i++)
            {
                if (!(name[i] == '_' || char.IsLetterOrDigit(name[i]) || name[i] == '.'))
                {
                    return false;
                }
            }

            return true;
        }
        
        public static UTinyAssetExportSettings GetAssetExportSettings(UTinyProject project, Object asset)
        {
            Assert.IsNotNull(asset);
            var registry = project.Registry;
            var module = project.Module.Dereference(registry);
            return module.GetAsset(asset)?.ExportSettings ?? project.Settings.GetDefaultAssetExportSettings(asset.GetType());
        }
        
        public static IEnumerable<UTinyModule> GetModules(UTinySystem system)
        {
            return GetModules(system.Registry, (UTinySystem.Reference) system);
        }
        
        public static IEnumerable<UTinyModule> GetModules(UTinyRegistryObjectBase @object)
        {
            if (@object is UTinyType)
            {
                return GetModules(@object.Registry, (UTinyType.Reference) (UTinyType) @object);
            }
            
            if (@object is UTinyEntityGroup)
            {
                return GetModules(@object.Registry, (UTinyEntityGroup.Reference) (UTinyEntityGroup) @object);
            }
            
            if (@object is UTinySystem)
            {
                return GetModules(@object.Registry, (UTinySystem.Reference) (UTinySystem) @object);
            }

            if (@object is UTinyScript)
            {
                return GetModules(@object.Registry, (UTinyScript.Reference) (UTinyScript) @object);
            }
            
            return Enumerable.Empty<UTinyModule>();
        }

        public static IEnumerable<UTinyModule> GetModules(IRegistry registry, IReference reference)
        {
            if (reference is UTinyType.Reference)
            {
                return GetModules(registry, (UTinyType.Reference) reference);
            }
            
            if (reference is UTinyEntityGroup.Reference)
            {
                return GetModules(registry, (UTinyEntityGroup.Reference) reference);
            }
            
            if (reference is UTinySystem.Reference)
            {
                return GetModules(registry, (UTinySystem.Reference) reference);
            }
            
            if (reference is UTinyScript.Reference)
            {
                return GetModules(registry, (UTinyScript.Reference) reference);
            }

            return Enumerable.Empty<UTinyModule>();
        }
        
        public static IEnumerable<UTinyModule> GetModules(IRegistry registry, UTinyType.Reference reference)
        {
            var modules = registry.FindAllByType<UTinyModule>();
            return modules.Where(module => module.Types.Contains(reference));
        }
        
        public static IEnumerable<UTinyModule> GetModules(IRegistry registry, UTinyEntityGroup.Reference reference)
        {
            var modules = registry.FindAllByType<UTinyModule>();
            return modules.Where(module => module.EntityGroups.Contains(reference));
        }
        
        public static IEnumerable<UTinyModule> GetModules(IRegistry registry, UTinySystem.Reference reference)
        {
            var modules = registry.FindAllByType<UTinyModule>();
            return modules.Where(module => module.Systems.Contains(reference));
        }
        
        public static IEnumerable<UTinyModule> GetModules(IRegistry registry, UTinyScript.Reference reference)
        {
            var modules = registry.FindAllByType<UTinyModule>();
            return modules.Where(module => module.Scripts.Contains(reference));
        }
        
        public static string CalculateChecksum(IEnumerable<byte> bytes)
        {
            var checksum = bytes.Aggregate(0, (current, chData) => current + chData);
            checksum &= 0xff;
            return checksum.ToString("X2");
        }
    }
}
#endif // NET_4_6
