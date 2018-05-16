#if NET_4_6
using System.IO;
using NUnit.Framework;
using UnityEngine;

namespace Unity.Tiny.Test
{
    [TestFixture]
    public class FlatJsonSerializationTest
    {
        [Test]
        public void FlatJsonProjectWrite()
        {
            var registry = new UTinyRegistry();

            var project = registry.CreateProject(
                UTinyId.New(),
                "TestProject");

            var json = Serialization.FlatJson.BackEnd.Persist(project);
            Debug.Log(json);
        }
        
        [Test]
        public void FlatJsonProjectRoundTrip()
        {
            var registry = new UTinyRegistry();

            var project = registry.CreateProject(
                UTinyId.New(),
                "TestProject");
            
            using (var json = new MemoryStream())
            using (var command = new MemoryStream())
            {
                // Write the data model to a stream as json
                // mem -> json
                Serialization.FlatJson.BackEnd.Persist(json, project);

                json.Position = 0;

                var reader = new StreamReader(json);
                {
                    Debug.Log(reader.ReadToEnd());
                }
                
                json.Position = 0;

                // Read the data model
                // json -> commands
                Serialization.FlatJson.FrontEnd.Accept(json, command);

                command.Position = 0;

                // Create a registry to hold accepted objects
                var output = new UTinyRegistry();

                // Process the command 
                // commands -> mem
                Serialization.CommandStream.FrontEnd.Accept(command, output);
            }
        }
        
        [Test]
        public void FlatJsonTypeWrite()
        {
            var registry = new UTinyRegistry();

            var type = registry.CreateType(
                UTinyId.New(),
                "TestStruct",
                UTinyTypeCode.Struct);

            type.CreateField("IntField", (UTinyType.Reference) UTinyType.Int32);
            type.CreateField("FloatField", (UTinyType.Reference) UTinyType.Int32);
            type.CreateField("StringField", (UTinyType.Reference) UTinyType.Int32);

            var json = Serialization.FlatJson.BackEnd.Persist(type);
            Debug.Log(json);
        }
        
        [Test]
        public void FlatJsonSceneWrite()
        {
            var registry = new UTinyRegistry();

            var entityGroup = registry.CreateEntityGroup(
                UTinyId.New(),
                "TestEntityGroup");

            var entity = registry.CreateEntity(
                UTinyId.New(),
                "TestEntity");
            
            entityGroup.AddEntityReference((UTinyEntity.Reference) entity);

            var json = Serialization.FlatJson.BackEnd.Persist(entityGroup, entity);
            Debug.Log(json);
        }


        [Test]
        public void FlatJsonEntityRoundTrip()
        {
            var registry = new UTinyRegistry();

            var type = registry.CreateType(
                UTinyId.New(),
                "TestType",
                UTinyTypeCode.Component
            );

            type.CreateField("TestIntField", (UTinyType.Reference) UTinyType.Int32);
            type.CreateField("TestStringField", (UTinyType.Reference) UTinyType.String);

            var entity = registry.CreateEntity(
                UTinyId.New(),
                "TestEntity");

            var component = entity.AddComponent((UTinyType.Reference) type);
            component.Refresh();

            component["TestIntField"] = 10;
            component["TestStringField"] = "Test";
            
            using (var json = new MemoryStream())
            using (var command = new MemoryStream())
            {
                // Write the data model to a stream as json
                // mem -> json
                Serialization.FlatJson.BackEnd.Persist(json, 
                    type,
                    entity);

                json.Position = 0;

                var reader = new StreamReader(json);
                {
                    Debug.Log(reader.ReadToEnd());
                }
                
                json.Position = 0;

                // Read the data model
                // json -> commands
                Serialization.FlatJson.FrontEnd.Accept(json, command);

                command.Position = 0;

                // Create a registry to hold accepted objects
                var output = new UTinyRegistry();

                // Process the command 
                // commands -> mem
                Serialization.CommandStream.FrontEnd.Accept(command, output);
            }
        }

        /// <summary>
        /// Tests a round trip JSON serialization and deserialization
        /// </summary>
        [Test]
        public void FlatJsonRoundTrip()
        {
            var input = new UTinyRegistry();

            var structType = input.CreateType(
                UTinyId.New(),
                "TestStruct",
                UTinyTypeCode.Struct);

            structType.CreateField("IntField", (UTinyType.Reference) UTinyType.Int32);
            structType.CreateField("FloatField", (UTinyType.Reference) UTinyType.Int32);
            structType.CreateField("StringField", (UTinyType.Reference) UTinyType.Int32);

            var module = input.CreateModule(
                UTinyId.New(),
                "TestModule");
            
            module.AddStructReference((UTinyType.Reference) structType);

            using (var json = new MemoryStream())
            using (var command = new MemoryStream())
            {
                // Write the data model to a stream as json
                // mem -> json
                Serialization.FlatJson.BackEnd.Persist(json, 
                    structType, 
                    module);

                json.Position = 0;

                var reader = new StreamReader(json);
                {
                    Debug.Log(reader.ReadToEnd());
                }
                
                json.Position = 0;

                // Read the data model
                // json -> commands
                Serialization.FlatJson.FrontEnd.Accept(json, command);

                command.Position = 0;

                // Create a registry to hold accepted objects
                var output = new UTinyRegistry();

                // Process the command 
                // commands -> mem
                Serialization.CommandStream.FrontEnd.Accept(command, output);

                Assert.IsNotNull(output.FindById<UTinyType>(structType.Id));
                Assert.IsNotNull(output.FindById<UTinyModule>(module.Id));
            }
        }

        /// <summary>
        /// Tests a round trip JSON serialization and deserialization
        /// </summary>
        [Test]
        public void FlatJsonEntityPerformance()
        {
            var registry = new UTinyRegistry();

            var vector3Type = registry.CreateType(
                UTinyId.New(),
                "Vector3",
                UTinyTypeCode.Struct);

            vector3Type.CreateField("X", (UTinyType.Reference) UTinyType.Float32);
            vector3Type.CreateField("Y", (UTinyType.Reference) UTinyType.Float32);
            vector3Type.CreateField("Z", (UTinyType.Reference) UTinyType.Float32);
            
            var transformType = registry.CreateType(
                UTinyId.New(),
                "Transform",
                UTinyTypeCode.Component);
            
            transformType.CreateField("Position", (UTinyType.Reference) vector3Type);
            transformType.CreateField("Scale", (UTinyType.Reference) vector3Type);
            
            const int kCount = 1000;
            var entities = new UTinyEntity[kCount];
            var transformTypeReference = (UTinyType.Reference) transformType;

            {
                var watch = System.Diagnostics.Stopwatch.StartNew();

                for (var i = 0; i < kCount; i++)
                {
                    entities[i] = registry.CreateEntity(UTinyId.New(), "Entity_" + i);
                    var transform = entities[i].AddComponent(transformTypeReference);
                
                    // if (i < kCount)
                    {
                        transform.Refresh(null, true);
                    
                        var position = transform["Position"] as UTinyObject;
                        position["X"] = i * 2f;
                    }
                }
                
                watch.Stop();
                Debug.Log($"Create Objects Entities=[{kCount}] {watch.ElapsedMilliseconds}ms");
            }
            
            using (var json = new MemoryStream())
            using (var command = new MemoryStream())
            {
                // Write the data model to a stream as json
                // mem -> json

                {
                    var watch = System.Diagnostics.Stopwatch.StartNew();
                    
                    Serialization.FlatJson.BackEnd.Persist(json, entities);
                    
                    watch.Stop();
                    Debug.Log($"FlatJson.BackEnd.Persist Entities=[{kCount}] {watch.ElapsedMilliseconds}ms Len=[{json.Position}]");
                }
                
                json.Position = 0;
                
                // Push the types to the command stream before the accept
                Serialization.CommandStream.BackEnd.Persist(command, vector3Type, transformType);

                {
                    var watch = System.Diagnostics.Stopwatch.StartNew();
                    
                    // Read the data model
                    // json -> commands
                    Serialization.FlatJson.FrontEnd.Accept(json, command);
                    
                    watch.Stop();
                    Debug.Log($"FlatJson.FrontEnd.Accept Entities=[{kCount}] {watch.ElapsedMilliseconds}ms Len=[{command.Position}]");
                }

                command.Position = 0;
                
                // Create a registry to hold accepted objects
                var output = new UTinyRegistry();

                // Process the command 
                // commands -> mem
                {
                    var watch = System.Diagnostics.Stopwatch.StartNew();
                    
                    Serialization.CommandStream.FrontEnd.Accept(command, output);
                    
                    watch.Stop();
                    Debug.Log($"CommandStream.FrontEnd.Accept Entities=[{kCount}] {watch.ElapsedMilliseconds}ms");
                }
            }
        }
    }
}
#endif // NET_4_6
