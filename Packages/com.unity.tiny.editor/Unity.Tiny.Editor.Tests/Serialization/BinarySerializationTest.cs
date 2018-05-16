#if NET_4_6
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using Unity.Properties;
using UnityEngine;

namespace Unity.Tiny.Test
{
    [TestFixture]
    public class BinarySerializationTest
    {
        [SetUp]
        public void SetUp()
        {
        }

        [TearDown]
        public void TearDown()
        {
        }

        [Test]
        public void SimpleBinaryRoundTrip()
        {
            var registry = new UTinyRegistry();
            var entity = registry.CreateEntity(UTinyId.New(), "Entity");
            var entities = new IPropertyContainer[] {entity};
                
            using (var memory = new MemoryStream())
            {
                Serialization.Binary.BackEnd.Persist(memory, entities);
                memory.Position = 0;
                
                using (var commands = new MemoryStream())
                {
                    Serialization.Binary.FrontEnd.Accept(memory, commands);
                    commands.Position = 0;
                    
                    var output = new UTinyRegistry();
                    Serialization.CommandStream.FrontEnd.Accept(commands, output);

                    var readEntity = output.FindById<UTinyEntity>(entity.Id);
                    Assert.NotNull(readEntity);
                }
            }
        }
        
        /// <summary>
        /// Tests a round trip JSON serialization and deserialization
        /// </summary>
        [Test]
        public void BinaryEntityPerformance()
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
            
            using (var binary = new MemoryStream())
            using (var command = new MemoryStream())
            {
                // Write the data model to a stream as json
                // mem -> command

                {
                    var watch = System.Diagnostics.Stopwatch.StartNew();
                    
                    Serialization.Binary.BackEnd.Persist(binary, (IEnumerable<UTinyEntity>) entities);
                    
                    watch.Stop();
                    Debug.Log($"Binary.BackEnd.Persist Entities=[{kCount}] {watch.ElapsedMilliseconds}ms Len=[{binary.Position}]");
                }
                
                binary.Position = 0;
                
                // Push the types to the command stream before the entities
                Serialization.CommandStream.BackEnd.Persist(command, vector3Type, transformType);
                
                {
                    var watch = System.Diagnostics.Stopwatch.StartNew();
                    
                    Serialization.Binary.FrontEnd.Accept(binary, command);
                    
                    watch.Stop();
                    Debug.Log($"Binary.FrontEnd.Accept Entities=[{kCount}] {watch.ElapsedMilliseconds}ms Len=[{command.Position}]");
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
