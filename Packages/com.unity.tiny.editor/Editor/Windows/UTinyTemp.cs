#if NET_4_6
using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Assertions;
using Unity.Tiny.Serialization.Binary;

namespace Unity.Tiny
{
    [InitializeOnLoad]
    public static class UTinyTemp
    {
        private const int KTempVersion = 1;
        private const string KTempFileName = "project";
        private static readonly FileInfo s_TempFileInfo;

        private enum SaveType : byte
        {
            /// <summary>
            /// The file only contains a single PersistenceId reference to the real content
            /// This is an unchanged file that the user was viewing
            /// </summary>
            PersistentUnchanged = 0,
            
            /// <summary>
            /// The file contains a PersistenceId and a FULL serialized copy of the object
            /// @NOTE This can become a diff in the future
            /// </summary>
            PersistentChanged = 1,
            
            /// <summary>
            /// This file contains a FULL serialized object that does NOT have a PersistentId
            /// </summary>
            Temporary = 2
        }

        static UTinyTemp()
        {
            s_TempFileInfo = new FileInfo(Path.Combine(Application.temporaryCachePath, KTempFileName + ".uttemp"));
        }

        private static FileInfo GetTempLocation()
        {
            return s_TempFileInfo; 
        }

        public static void SavePersistentUnchanged(string persistenceId)
        {
            using (var stream = new FileStream(GetTempLocation().FullName, FileMode.Create))
            using (var writer = new BinaryWriter(stream))
            {
                writer.Write(KTempVersion);
                writer.Write((byte) SaveType.PersistentUnchanged);
                writer.Write(persistenceId);
            }
        }

        public static void SavePersistentChanged(IPersistentObject obj)
        {
            using (var stream = new FileStream(GetTempLocation().FullName, FileMode.Create))
            using (var writer = new BinaryWriter(stream))
            {
                writer.Write(KTempVersion);
                writer.Write((byte) SaveType.PersistentChanged);
                writer.Write(obj.PersistenceId);
                writer.Write(ComputeHash(obj.PersistenceId));
                BackEnd.Persist(stream, obj.EnumeratePersistedObjects());
            }
        }

        public static void SaveTemporary(IPersistentObject obj)
        {
            using (var stream = new FileStream(GetTempLocation().FullName, FileMode.Create))
            using (var writer = new BinaryWriter(stream))
            {
                writer.Write(KTempVersion);
                writer.Write((byte) SaveType.Temporary);
                BackEnd.Persist(stream, obj.EnumeratePersistedObjects());
            }
        }

        public static void Delete()
        { 
            var location = GetTempLocation().FullName;
            if (!File.Exists(location))
            {
                return;
            }
            File.Delete(location);
        }

        public static bool Exists()
        {
            return File.Exists(GetTempLocation().FullName);
        }
        
        /// <summary>
        /// @TODO This method has too many conditionals and checks... it should be managed at a higher level
        /// </summary>
        /// <param name="registry"></param>
        /// <param name="persistenceId"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        public static bool Accept(IRegistry registry, out string persistenceId)
        {
            Assert.IsTrue(Exists());
            
            registry.Clear();
            UTinyPersistence.LoadAllModules(registry);
            registry.UnregisterAllBySource(UTinyRegistry.DefaultSourceIdentifier);

            persistenceId = null;
            
            using (var command = new MemoryStream())
            using (var stream = File.OpenRead(GetTempLocation().FullName))
            using (var reader = new BinaryReader(stream))
            using (registry.SourceIdentifierScope(UTinyRegistry.DefaultSourceIdentifier))
            {
                var version = reader.ReadInt32();
                
                Assert.IsTrue(version > 0);
                
                var type = (SaveType) reader.ReadByte();

                switch (type)
                {
                    case SaveType.PersistentUnchanged:
                        persistenceId = reader.ReadString();
                        return false;
                    case SaveType.PersistentChanged:
                        persistenceId = reader.ReadString();
                        
                        var hash = reader.ReadString();
                        if (!string.IsNullOrEmpty(hash) && !string.Equals(hash, ComputeHash(persistenceId)))
                        {
                            // Ask the user if they want to keep their changes or reload from disc
                            if (EditorUtility.DisplayDialog($"{UTinyConstants.ApplicationName} assets changed", $"{UTinyConstants.ApplicationName} assets have changed on disk, would you like to reload the current project?", "Yes", "No"))
                            {
                                return false;
                            }
                        }
                        break;
                    case SaveType.Temporary:
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }

                // This is to handle module editing.
                // We want to unregister it from its current source and re-register it with the persistenceId as the scope
                if (!string.IsNullOrEmpty(persistenceId))
                {
                    registry.UnregisterAllBySource(persistenceId);
                }
                
                FrontEnd.Accept(stream, command);
                
                command.Position = 0;
                Serialization.CommandStream.FrontEnd.Accept(command, registry);
            }
            
            foreach (var project in registry.FindAllBySource(UTinyRegistry.DefaultSourceIdentifier).OfType<UTinyProject>())
            {
                project.PersistenceId = persistenceId;
            }

            return true;
        }

        private static string ComputeHash(string persistenceId)
        {
            var path = AssetDatabase.GUIDToAssetPath(persistenceId);

            if (!File.Exists(path))
            {
                return string.Empty;
            }
            
            var bytes = File.ReadAllBytes(path);
            
            using (var md5 = System.Security.Cryptography.MD5.Create())
            {
                var hash = md5.ComputeHash(bytes);
                return new Guid(hash).ToString("N");
            }
        }
    }
}
#endif // NET_4_6
