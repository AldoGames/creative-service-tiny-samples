#if NET_4_6
using System;
using System.Collections.Generic;
using System.IO;
using Unity.Properties.Serialization;
using UnityEngine;
using UnityEngine.Assertions;
using Object = UnityEngine.Object;

namespace Unity.Tiny.Serialization.CommandStream
{
    public static class FrontEnd
    {
        private static readonly BinaryObjectReader.PooledObjectFactory s_ObjectFactory = new BinaryObjectReader.PooledObjectFactory();
        private static readonly CommandObjectReader s_CommandObjectReader = new CommandObjectReader(s_ObjectFactory);

        public static void Accept(Stream input, IRegistry registry)
        {
            var reader = new BinaryReader(input);

            var typesDictionaryList = new List<IDictionary<string, object>>();
            var entitiesDictionaryList = new List<IDictionary<string, object>>();
            
            while (input.Position != input.Length)
            {
                var command = reader.ReadByte();
                var size = reader.ReadUInt32();
                var end = input.Position + size;

                switch (command)
                {
                    case CommandType.CreateProject:
                    {
                        var dictionary = s_CommandObjectReader.ReadObject(reader);
                        AcceptProject(dictionary, registry);
                    }
                        break;

                    case CommandType.CreateModule:
                    {
                        var dictionary = s_CommandObjectReader.ReadObject(reader);
                        AcceptModule(dictionary, registry);
                    }
                        break;

                    case CommandType.CreateType:
                    {
                        // Defer type creation until all types are received
                        typesDictionaryList.Add(s_CommandObjectReader.ReadObject(reader));
                    }
                    break;

                    case CommandType.CreateScene:
                    {
                        var dictionary = s_CommandObjectReader.ReadObject(reader);
                        AcceptScene(dictionary, registry);
                    }
                        break;

                    case CommandType.CreateEntity:
                    {
                        // Defer entity creation until all types are initialized
                        entitiesDictionaryList.Add(s_CommandObjectReader.ReadObject(reader));
                    }
                        break;

                    case CommandType.CreateScript:
                    {
                        var dictionary = s_CommandObjectReader.ReadObject(reader);
                        AcceptScript(dictionary, registry);
                    }
                        break;

                    case CommandType.CreateSystem:
                    {
                        var dictionary = s_CommandObjectReader.ReadObject(reader);
                        AcceptSystem(dictionary, registry);
                    }
                        break;

                    case CommandType.Unregister:
                    {
                        var bytes = new byte[16];
                        reader.Read(bytes, 0, 16);
                        var id = new UTinyId(new Guid(bytes));
                        registry.Unregister(id);
                    }
                        break;
                        
                       default: 
                           Debug.LogWarning($"Unhandled command type '{command}'");
                           break;
                }

                input.Position = end;
            }
            
            // Create types
            var types = new Dictionary<UTinyType, IDictionary<string, object>>();
            foreach (var typeDictionary in typesDictionaryList)
            {
                var type = AcceptType(typeDictionary, registry);
                types.Add(type, typeDictionary);
            }
            
            // Initialize type default values
            // Iterate in DepthFirst order to ensure types are created properly
            foreach (var typeReference in UTinyType.Iterator.DepthFirst(types.Keys))
            {
                var type = typeReference.Dereference(registry);
                
                type.Refresh();

                if (!types.ContainsKey(type))
                {
                    continue;
                }
                
                var dictionary = types[type];
    
                IDictionary<string, object> defaultValueDictionary;
                if (!TryGetValue(dictionary, "DefaultValue", out defaultValueDictionary))
                {
                    continue;
                }
                
                var defaultValue = type.DefaultValue as UTinyObject;
                Assert.IsNotNull(defaultValue);
                ParseUTinyObject(registry, defaultValue, defaultValueDictionary);
            }

            // Create entities
            foreach (var entityDictionary in entitiesDictionaryList)
            {
                AcceptEntity(entityDictionary, registry);
            }
        }

        private static void AcceptProject(IDictionary<string, object> dictionary, IRegistry registry)
        {
            var projectId = ParseId(GetValue(dictionary, "Id"));
            var projectName = GetValue<string>(dictionary, "Name");

            var project = registry.CreateProject(projectId, projectName);
            ParseRegistryObjectBase(project, dictionary);

            project.SerializedVersion = ParseInt(GetValue(dictionary, "Version"));

            IDictionary<string, object> settingsDictionary;
            if (TryGetValue(dictionary, "Settings", out settingsDictionary))
            {
                project.Settings.CanvasWidth = ParseInt(GetValue(settingsDictionary, "CanvasWidth"));
                project.Settings.CanvasHeight = ParseInt(GetValue(settingsDictionary, "CanvasHeight"));
                project.Settings.CanvasAutoResize = ParseBoolean(GetValue(settingsDictionary, "CanvasAutoResize"), true);

                // @HACK Backwards compatibility. Try to load from the formerly serialized name
                // This can be removed after one release
                ParseTextureSettings(project.Settings.DefaultTextureSettings, GetValue(settingsDictionary, "DefaultTextureFormat"));
                ParseTextureSettings(project.Settings.DefaultTextureSettings, GetValue(settingsDictionary, "DefaultTextureSettings"));
                ParseBaseAssetExportSettings(project.Settings.DefaultAudioClipSettings, GetValue(settingsDictionary, "DefaultAudioClipSettings"));
                
                project.Settings.EmbedAssets = ParseBoolean(GetValue(settingsDictionary, "EmbedAssets"), true);
                project.Settings.LocalHTTPServerPort = ParseInt(GetValue(settingsDictionary, "LocalHTTPServerPort"), UTinyProjectSettings.DefaultLocalHTTPServerPort);
                project.Settings.MemorySize = ParseInt(GetValue(settingsDictionary, "MemorySize"), UTinyProjectSettings.DefaultMemorySize);
                project.Settings.SingleFileHtml = ParseBoolean(GetValue(settingsDictionary, "SingleFileHtml"), false);
                project.Settings.IncludeWSClient = ParseBoolean(GetValue(settingsDictionary, "IncludeWSClient"), true);
                project.Settings.IncludeWebPDecompressor = ParseBoolean(GetValue(settingsDictionary, "IncludeWebPDecompressor"), false);
                project.Settings.RunBabel = ParseBoolean(GetValue(settingsDictionary, "RunBabel"), true);
            }

            project.Module = ParseModuleReference(GetValue(dictionary, "Module"));
            project.Configuration = ParseEntityReference(GetValue(dictionary, "Configuration"));
        }

        private static void ParseTextureSettings(UTinyTextureSettings settings, object obj)
        {
            ParseBaseAssetExportSettings(settings, obj);
            
            var dictionary = obj as IDictionary<string, object>;
            if (null == dictionary)
            {
                return;
            }
            
            settings.FormatType = (TextureFormatType) ParseInt(GetValue(dictionary, "FormatType"));
            settings.JpgCompressionQuality = ParseInt(GetValue(dictionary, "JpgCompressionQuality"));
            settings.WebPCompressionQuality = ParseInt(GetValue(dictionary, "WebPCompressionQuality"));
        }
        
        private static void ParseBaseAssetExportSettings(UTinyAssetExportSettings settings, object obj)
        {
            var dictionary = obj as IDictionary<string, object>;
            if (null == dictionary)
            {
                return;
            }
            
            // Backwards compatability
            settings.Embedded |= ParseBoolean(GetValue(dictionary, "Base64Encode"));
            settings.Embedded |= ParseBoolean(GetValue(dictionary, "Embedded"));
            settings.IncludePreviewInDocumentation = ParseBoolean(GetValue(dictionary, "IncludePreviewInDocumentation"));
        }

        private static void AcceptModule(IDictionary<string, object> dictionary, IRegistry registry)
        {
            var moduleId = ParseId(GetValue(dictionary, "Id"));
            var moduleName = GetValue<string>(dictionary, "Name");
            
            var module = registry.CreateModule(moduleId, moduleName);
            ParseRegistryObjectBase(module, dictionary);

            if (dictionary.ContainsKey("Namespace"))
            {
                module.Namespace = GetValue<string>(dictionary, "Namespace");
            }
            module.Options = (UTinyModuleOptions) ParseInt(GetValue(dictionary, "Options"));
            
            IList<object> dependenciesList;
            if (TryGetValue(dictionary, "Dependencies", out dependenciesList))
            {
                foreach (var obj in dependenciesList)
                {
                    module.AddExplicitModuleDependency(ParseModuleReference(obj));
                }
            }
            
            IList<object> componentsList;
            if (TryGetValue(dictionary, "Components", out componentsList))
            {
                foreach (var obj in componentsList)
                {
                    module.AddComponentReference(ParseTypeReference(obj));
                }
            }
            
            IList<object> structsList;
            if (TryGetValue(dictionary, "Structs", out structsList))
            {
                foreach (var obj in structsList)
                {
                    module.AddStructReference(ParseTypeReference(obj));
                }
            }
            
            IList<object> enumsList;
            if (TryGetValue(dictionary, "Enums", out enumsList))
            {
                foreach (var obj in enumsList)
                {
                    module.AddEnumReference(ParseTypeReference(obj));
                }
            }
            
            IList<object> configurationsList;
            if (TryGetValue(dictionary, "Configurations", out configurationsList))
            {
                foreach (var obj in configurationsList)
                {
                    module.AddConfigurationReference(ParseTypeReference(obj));
                }
            }
            
            IList<object> entityGroupList;
            if (TryGetValue(dictionary, "EntityGroups", out entityGroupList))
            {
                foreach (var obj in entityGroupList)
                {
                    module.AddEntityGroupReference(ParseSceneReference(obj));
                }
            }
            // @LEGACY Backwards compat
            if (TryGetValue(dictionary, "Scenes", out entityGroupList))
            {
                foreach (var obj in entityGroupList)
                {
                    module.AddEntityGroupReference(ParseSceneReference(obj));
                }
            }
            
            IList<object> scriptsList;
            if (TryGetValue(dictionary, "Scripts", out scriptsList))
            {
                foreach (var obj in scriptsList)
                {
                    module.AddScriptReference(ParseScriptReference(obj));
                }
            }
            
            IList<object> systemsList;
            if (TryGetValue(dictionary, "Systems", out systemsList))
            {
                foreach (var obj in systemsList)
                {
                    module.AddSystemReference(ParseSystemReference(obj));
                }
            }
            
            IList<object> assetsList;
            if (TryGetValue(dictionary, "Assets", out assetsList))
            {
                foreach (var obj in assetsList)
                {
                    // Auto migrate existing explicit asset references
                    if (IsUnityObject(obj))
                    {
                        var unityObject = ParseUnityObject(obj);
                        
                        if (null == unityObject)
                        {
                            continue;
                        }
                        
                        module.AddAsset(unityObject);
                    }
                    else
                    {
                        var assetDictionary = obj as IDictionary<string, object>;
                        
                        if (null != assetDictionary)
                        {
                            
                            var unityObject = ParseUnityObject(GetValue(assetDictionary, "Object"));

                            if (null == unityObject)
                            {
                                continue;
                            }
                            
                            var asset = module.AddAsset(unityObject);
                            
                            asset.Name = GetValue(assetDictionary, "Name") as string;

                            if (!assetDictionary.ContainsKey("ExportSettings"))
                            {
                                continue;
                            }
                            
                            // @TODO Determine the asset type even if the asset does not exist!
                            if (null != asset.Object)
                            {
                                var exportSettingsDictionary = GetValue(assetDictionary, "ExportSettings") as IDictionary<string, object>;

                                if (null != exportSettingsDictionary && exportSettingsDictionary.Count > 0)
                                {
                                    var type = asset.Object.GetType();
                                    if (typeof(Texture2D).IsAssignableFrom(type))
                                    {
                                        var exportSettings = asset.CreateExportSettings<UTinyTextureSettings>();
                                        ParseTextureSettings(exportSettings, exportSettingsDictionary);
                                    }
                                    else if (typeof(AudioClip).IsAssignableFrom(type))
                                    {
                                        var exportSettings = asset.CreateExportSettings<UTinyAudioClipSettings>();
                                        ParseBaseAssetExportSettings(exportSettings, exportSettingsDictionary);
                                    }
                                    else
                                    {
                                        var exportSettings = asset.CreateExportSettings<UTinyGenericAssetExportSettings>();
                                        ParseBaseAssetExportSettings(exportSettings, exportSettingsDictionary);
                                    }
                                }
                            }
                        }
                    }
                }
            }
            
            // @LEGACY migrate legacy format
            IList<object> textureSettingsList;
            if (TryGetValue(dictionary, "TextureSettings", out textureSettingsList))
            {
                foreach (IDictionary<string, object> textureSettingsDictionary in textureSettingsList)
                {
                    var unityObject = ParseUnityObject(GetValue(textureSettingsDictionary, "Texture")) as Texture2D;
                    var asset = module.GetAsset(unityObject) ?? module.AddAsset(unityObject);
                    var exportSettings = asset.CreateExportSettings<UTinyTextureSettings>();
                    ParseTextureSettings(exportSettings, GetValue(textureSettingsDictionary, "Format"));
                }
            }
            
            module.StartupEntityGroup = ParseSceneReference(GetValue(dictionary, "StartupEntityGroup"));
            
            // @TEMP Backwards compat
            if (module.StartupEntityGroup.Equals(UTinyEntityGroup.Reference.None))
            {
                module.StartupEntityGroup = ParseSceneReference(GetValue(dictionary, "StartupScene"));
            }
        }

        private static UTinyType AcceptType(IDictionary<string, object> dictionary, IRegistry registry)
        {
            var typeId = ParseId(GetValue(dictionary, "Id"));
            var typeName = GetValue<string>(dictionary, "Name");
            var typeCode = ParseTypeCode(GetValue(dictionary, "TypeCode"));

            var type = registry.CreateType(typeId, typeName, typeCode);
            ParseRegistryObjectBase(type, dictionary);
            type.BaseType = ParseTypeReference(GetValue(dictionary, "BaseType"));

            // Fix enums must always have a base type
            if (type.IsEnum && type.BaseType.Equals(UTinyType.Reference.None))
            {
                type.BaseType = (UTinyType.Reference) UTinyType.Int32;
            } 

            IList<object> fieldsList;
            if (TryGetValue(dictionary, "Fields", out fieldsList))
            {
                foreach (IDictionary<string, object> fieldDictionary in fieldsList)
                {
                    var fieldId = ParseId(GetValue(fieldDictionary, "Id"));
                    var fieldName = GetValue<string>(fieldDictionary, "Name");
                    var fieldType = ParseTypeReference(GetValue(fieldDictionary, "FieldType"));
                    var fieldArray = ParseBoolean(GetValue(fieldDictionary, "Array"));

                    type.CreateField(fieldId, fieldName, fieldType, fieldArray);
                }
            }

            return type;
        }

        private static void AcceptScene(IDictionary<string, object> dictionary, IRegistry registry)
        {
            var entityGroupId = ParseId(GetValue(dictionary, "Id"));
            var entityGroupName = GetValue<string>(dictionary, "Name");

            var entityGroup = registry.CreateEntityGroup(entityGroupId, entityGroupName);
            ParseRegistryObjectBase(entityGroup, dictionary);
            
            IList<object> entitiesList;
            if (TryGetValue(dictionary, "Entities", out entitiesList))
            {
                foreach (var obj in entitiesList)
                {
                    entityGroup.AddEntityReference(ParseEntityReference(obj));
                }
            }
        }

        private static void AcceptEntity(IDictionary<string, object> dictionary, IRegistry registry)
        {
            var entityId = ParseId(GetValue(dictionary, "Id"));
            var entityName = GetValue<string>(dictionary, "Name");

            var entity = registry.CreateEntity(entityId, entityName);
            ParseRegistryObjectBase(entity, dictionary);

            entity.Enabled = ParseBoolean(GetValue(dictionary, "Enabled"));
            entity.Layer = ParseInt(GetValue(dictionary, "Layer"));

            IList<object> componentList;
            if (TryGetValue(dictionary, "Components", out componentList))
            {
                foreach (IDictionary<string, object> componentDictionary in componentList)
                {
                    var componentType = ParseTypeReference(GetValue(componentDictionary, "Type"), false);
                    var component = entity.AddComponent(componentType);
                    component.Refresh(null, true);
                    ParseUTinyObject(registry, component, componentDictionary);
                    component.Refresh(null, true);
                }
            }
        }

        private static void AcceptScript(IDictionary<string, object> dictionary, IRegistry registry)
        {
            var scriptId = ParseId(GetValue(dictionary, "Id"));
            var scriptName = GetValue<string>(dictionary, "Name");

            var script = registry.CreateScript(scriptId, scriptName);
            ParseRegistryObjectBase(script, dictionary);

            script.Included = ParseBoolean(GetValue(dictionary, "Included"));
            script.TextAsset = ParseUnityObject(GetValue(dictionary, "TextAsset")) as TextAsset;
        }

        private static void AcceptSystem(IDictionary<string, object> dictionary, IRegistry registry)
        {
            var systemId = ParseId(GetValue(dictionary, "Id"));
            var systemName = GetValue<string>(dictionary, "Name");

            var system = registry.CreateSystem(systemId, systemName);
            ParseRegistryObjectBase(system, dictionary);

            system.Options = (UTinySystemOptions) ParseInt(GetValue(dictionary, "Options"));
            system.TextAsset = ParseUnityObject(GetValue(dictionary, "TextAsset")) as TextAsset;
            
            IList<object> componentList;
            if (TryGetValue(dictionary, "Components", out componentList))
            {
                foreach (var obj in componentList)
                {
                    system.AddComponentReference(ParseTypeReference(obj));
                }
            }
            
            IList<object> executeAfterList;
            if (TryGetValue(dictionary, "ExecuteAfter", out executeAfterList))
            {
                foreach (var obj in executeAfterList)
                {
                    system.AddExecuteAfterReference(ParseSystemReference(obj));
                }
            }
            
            IList<object> executeBeforeList;
            if (TryGetValue(dictionary, "ExecuteBefore", out executeBeforeList))
            {
                foreach (var obj in executeBeforeList)
                {
                    system.AddExecuteBeforeReference(ParseSystemReference(obj));
                }
            }
        }

        private static bool TryGetValue<T>(IDictionary<string, object> dictionary, string key, out T value)
        {
            object obj;
            if (!dictionary.TryGetValue(key, out obj))
            {
                value = default(T);
                return false;
            }

            value = (T) obj;
            return true;
        }

        private static T GetValue<T>(IDictionary<string, object> dictionary, string key)
        {
            object obj;
            if (!dictionary.TryGetValue(key, out obj))
            {
                return default(T);
            }

            return (T) obj;
        }

        private static object GetValue(IDictionary<string, object> dictionary, string key)
        {
            object obj;
            return !dictionary.TryGetValue(key, out obj) ? null : obj;
        }
        
        private static void ParseRegistryObjectBase(UTinyRegistryObjectBase obj, IDictionary<string, object> dictionary)
        {
            obj.ExportFlags = (UTinyExportFlags) ParseInt(GetValue(dictionary, "ExportFlags"));

            IDictionary<string, object> documentationDictionary;
            if (TryGetValue(dictionary, "Documentation", out documentationDictionary))
            {
                obj.Documentation.Summary = GetValue<string>(documentationDictionary, "Summary");
            }
        }
        
        private static UTinyId ParseId(object obj)
        {
            if (null == obj)
            {
                return UTinyId.Empty;
            }

            if (obj is UTinyId)
            {
                return (UTinyId) obj;
            }

            var s = obj as string;
            return s != null ? new UTinyId(s) : UTinyId.Empty;
        }

        private static UTinyTypeCode ParseTypeCode(object obj)
        {
            if (null == obj)
            {
                return UTinyTypeCode.Unknown;
            }
            
            if (obj is UTinyTypeCode)
            {
                return (UTinyTypeCode) obj;
            }

            var s = obj as string;

            if (s != null)
            {
                return (UTinyTypeCode) Enum.Parse(typeof(UTinyTypeCode), s);
            }

            return (UTinyTypeCode) obj;
        }
        
        private static int ParseInt(object obj, int defaultValue = 0)
        {
            if (null == obj)
            {
                return defaultValue;
            }
            
            if (obj is int)
            {
                return (int) obj;
            }

            if (obj is double)
            {
                return (int) (double) obj;
            }

            var s = obj as string;

            int intValue;
            return int.TryParse(s, out intValue) ? intValue : 0;
        }
        
        private static long ParseLong(object obj, long defaultValue = 0)
        {
            if (null == obj)
            {
                return defaultValue;
            }
            
            if (obj is int)
            {
                return (int) obj;
            }

            if (obj is double)
            {
                return (long) (double) obj;
            }

            var s = obj as string;

            long value;
            return long.TryParse(s, out value) ? value : 0;
        }
        
        private static bool ParseBoolean(object obj, bool defaultValue = false)
        {
            if (null == obj)
            {
                return defaultValue;
            }

            if (obj is bool)
            {
                return (bool) obj;
            }

            if (obj is int)
            {
                return (int) obj != 0;
            }

            if (obj is double)
            {
                return Math.Abs((double) obj) > double.Epsilon;
            }

            var s = obj as string;

            int intValue;
            if (int.TryParse(s, out intValue))
            {
                return intValue != 0;
            }
            
            bool boolValue;
            return bool.TryParse(s, out boolValue) && boolValue;
        }

        public static bool IsUnityObject(object obj)
        {
            if (obj is Object)
            {
                return true;
            }
            
            var dictionary = obj as IDictionary<string, object>;
            if (null == dictionary)
            {
                return false;
            }

            return dictionary.ContainsKey("Guid") && dictionary.ContainsKey("FileId");
        }

        private static Object ParseUnityObject(object obj)
        {
            var o = obj as Object;
            if (o != null)
            {
                return o;
            }
            
            var dictionary = obj as IDictionary<string, object>;
            if (null == dictionary)
            {
                return null;
            }
            
            var guid = GetValue<string>(dictionary, "Guid");
            var fileId = ParseLong(GetValue(dictionary, "FileId"));
            var type = ParseInt(GetValue(dictionary, "Type"));
            
            return UnityObjectSerializer.FromObjectHandle(new UnityObjectHandle { Guid = guid, FileId = fileId, Type = type });
        }

        private static void ParseUTinyObject(IRegistry registry, UTinyObject uTinyObject, IDictionary<string, object> dictionary)
        {
            IDictionary<string, object> propertiesDictionary;
            if (!TryGetValue(dictionary, "Properties", out propertiesDictionary))
            {
                UTinyUpdater.UpdateObject(uTinyObject);
                return;
            }

            var type = uTinyObject.Type.Dereference(registry);
            
            foreach (var kvp in propertiesDictionary)
            {
                var valueDictionary = kvp.Value as IDictionary<string, object>;
            
                if (valueDictionary != null)
                {
                    if (valueDictionary.ContainsKey("Properties"))
                    {
                        if (uTinyObject.HasProperty(kvp.Key))
                        {
                            ParseUTinyObject(registry, uTinyObject[kvp.Key] as UTinyObject, valueDictionary);
                        }
                        else
                        {
                            var typeReference = ParseTypeReference(GetValue(valueDictionary, "Type"), false);
                            var obj = new UTinyObject(registry, typeReference, null, false);
                            obj.Refresh(null, true);
                            ParseUTinyObject(registry,  obj, valueDictionary);
                            obj.Refresh(null, true);
                            uTinyObject[kvp.Key] = obj;
                        }
                    }
                    else if (valueDictionary.ContainsKey("Items"))
                    {
                        if (uTinyObject.HasProperty(kvp.Key))
                        {
                            ParseUTinyList(registry, uTinyObject[kvp.Key] as UTinyList, valueDictionary);
                        }
                        else
                        {
                            var obj = new UTinyList(registry, ParseTypeReference(GetValue(valueDictionary, "Type"), false));
                            ParseUTinyList(registry,  obj, valueDictionary);
                            uTinyObject[kvp.Key] = obj;
                        }
                    }
                    else if (valueDictionary.ContainsKey("$TypeId"))
                    {
                        var value = ParseCustomObjectType(registry, valueDictionary);

                        var field = type?.FindFieldByName(kvp.Key);
                        if (field != null)
                        {
                            value = UTinyUpdater.UpdateField(field.Id, value);
                        }

                        uTinyObject[kvp.Key] = value;
                    }
                }
                else
                {
                    var value = kvp.Value;
                    
                    var field = type?.FindFieldByName(kvp.Key);
                    if (field != null)
                    {
                        value = UTinyUpdater.UpdateField(field.Id, value);
                    }

                    uTinyObject[kvp.Key] = value;
                }
            }

            UTinyUpdater.UpdateObject(uTinyObject);
        }

        private static object ParseCustomObjectType(IRegistry registry, IDictionary<string, object> dictionary)
        {
            // Specialized object type
            var typeId = (UTinyTypeId) ParseInt(dictionary["$TypeId"]);

            switch (typeId)
            {
                case UTinyTypeId.EnumReference:
                    return ParseEnumReference(registry, dictionary);
                case UTinyTypeId.EntityReference:
                    return ParseEntityReference(dictionary);
                case UTinyTypeId.UnityObject:
                    return ParseUnityObject(dictionary);
                default:
                    return null;
            }
        }

        private static void ParseUTinyList(IRegistry registry, UTinyList uTinyList, IDictionary<string, object> dictionary)
        {
            var typeReference = ParseTypeReference(GetValue(dictionary, "Type"), false);
            
            IList<object> itemsList;
            if (TryGetValue(dictionary, "Items", out itemsList))
            {
                foreach (var obj in itemsList)
                {
                    var valueDictionary = obj as IDictionary<string, object>;
                    if (valueDictionary != null)
                    {
                        if (valueDictionary.ContainsKey("$TypeId"))
                        {
                            uTinyList.Add(ParseCustomObjectType(registry, valueDictionary));
                        }
                        // @HACK We assume an object `Properties` OR `Type` keys is a `UTinyObject`
                        else if (valueDictionary.ContainsKey("Properties") || valueDictionary.ContainsKey("Type"))
                        {
                            var @object = new UTinyObject(registry, typeReference, null, false);
                            @object.Refresh(null, true);
                            ParseUTinyObject(registry, @object, valueDictionary);
                            uTinyList.Add(@object);
                        }
                        else
                        {
                            throw new NotImplementedException("CommandStream.FrontEnd Failed to deserialize UTinyList item");
                        }
                    }
                    else
                    {
                        uTinyList.Add(obj);
                    }
                }
            }

            uTinyList.Type = UTinyUpdater.UpdateReference(uTinyList.Type);
        }

        private static bool TryParseReference(object obj, out UTinyId id, out string name)
        {
            var dictionary = obj as IDictionary<string, object>;
            if (dictionary == null) {
                id = UTinyId.Empty;
                name = null;
                return false;
            }

            name = dictionary["Name"] as string;
            id = ParseId(dictionary["Id"]);

            return true;
        }

        private static UTinyModule.Reference ParseModuleReference(object obj)
        {
            if (obj is UTinyModule.Reference) {
                return (UTinyModule.Reference) obj;
            }

            UTinyId id;
            string name;
            return TryParseReference(obj, out id, out name) ? new UTinyModule.Reference(id, name) : new UTinyModule.Reference();
        }

        private static UTinyType.Reference ParseTypeReference(object obj, bool updateReference = true)
        {
            if (obj is UTinyType.Reference)
            {
                return (UTinyType.Reference) obj;
            }

            UTinyId id;
            string name;
            var reference = TryParseReference(obj, out id, out name) ? new UTinyType.Reference(id, name) : new UTinyType.Reference();
            return updateReference ? UTinyUpdater.UpdateReference(reference) : reference;
        }
        
        private static UTinyEntityGroup.Reference ParseSceneReference(object obj)
        {
            if (obj is UTinyEntityGroup.Reference)
            {
                return (UTinyEntityGroup.Reference) obj;
            }

            UTinyId id;
            string name;
            return TryParseReference(obj, out id, out name) ? new UTinyEntityGroup.Reference(id, name) : new UTinyEntityGroup.Reference();
        }
        
        private static UTinyEntity.Reference ParseEntityReference(object obj)
        {
            if (obj is UTinyEntity.Reference)
            {
                return (UTinyEntity.Reference) obj;
            }

            var dictionary = obj as IDictionary<string, object>;
            return dictionary != null ? new UTinyEntity.Reference(ParseId(dictionary["Id"]), dictionary["Name"] as string) : new UTinyEntity.Reference();
        }

        private static UTinyScript.Reference ParseScriptReference(object obj)
        {
            if (obj is UTinyScript.Reference)
            {
                return (UTinyScript.Reference) obj;
            }

            UTinyId id;
            string name;
            return TryParseReference(obj, out id, out name) ? new UTinyScript.Reference(id, name) : new UTinyScript.Reference();
        }

        private static UTinySystem.Reference ParseSystemReference(object obj)
        {
            if (obj is UTinySystem.Reference)
            {
                return (UTinySystem.Reference) obj;
            }

            UTinyId id;
            string name;
            var reference = TryParseReference(obj, out id, out name) ? new UTinySystem.Reference(id, name) : new UTinySystem.Reference();
            return UTinyUpdater.UpdateReference(reference);
        }

        private static UTinyEnum.Reference ParseEnumReference(IRegistry registry, IDictionary<string, object> dictionary)
        {
            var type = ParseTypeReference(GetValue(dictionary, "Type"));
            var id = ParseId(GetValue(dictionary, "Id"));
            return new UTinyEnum.Reference(type.Dereference(registry), id);
        }
    }
}
#endif // NET_4_6
