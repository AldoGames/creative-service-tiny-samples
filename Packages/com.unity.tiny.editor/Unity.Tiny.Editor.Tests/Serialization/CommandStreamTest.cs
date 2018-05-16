#if NET_4_6
using System.Collections.Generic;
using System.IO;
using NUnit.Framework;
using UnityEngine;
using Unity.Tiny.Serialization.CommandStream;

namespace Unity.Tiny.Test
{
    [TestFixture]
    public class CommandStreamTest
    {
        [SetUp]
        public void SetUp()
        {
        }

        [TearDown]
        public void TearDown()
        {
        }

        /// <summary>
        /// Tests a round trip command stream
        /// </summary>
        [Test]
        public void StreamingRoundTrip()
        {
            var input = new UTinyRegistry();

            var type = input.CreateType(
                UTinyId.New(),
                "TestStruct",
                UTinyTypeCode.Struct);

            type.CreateField("IntField", (UTinyType.Reference) UTinyType.Int32);
            type.CreateField("FloatField", (UTinyType.Reference) UTinyType.Int32);
            type.CreateField("StringField", (UTinyType.Reference) UTinyType.Int32);

            var module = input.CreateModule(
                UTinyId.New(),
                "TestModule");
            
            module.AddStructReference((UTinyType.Reference) type);
            
            using (var command = new MemoryStream())
            {
                // Write the data model to a stream as json
                // mem -> command
                BackEnd.Persist(command,  
                    type, 
                    module);

                command.Position = 0;

                // Create a registry to hold accepted objects
                var output = new UTinyRegistry();

                // Process the command 
                // commands -> mem
                FrontEnd.Accept(command, output);

                Assert.IsNotNull(output.FindById<UTinyType>(type.Id));
                Assert.IsNotNull(output.FindById<UTinyModule>(module.Id));
            }
        }
        
        /// <summary>
        /// Tests a round trip JSON serialization and deserialization
        /// </summary>
        [Test]
        public void CommandStreamEntityPerformance()
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
            
            using (var command = new MemoryStream())
            {
                // Write the data model to a stream as json
                // mem -> command

                // Push the types to the command stream before the accept
                BackEnd.Persist(command, vector3Type, transformType);
                
                {
                    var watch = System.Diagnostics.Stopwatch.StartNew();
                    
                    BackEnd.Persist(command, (IEnumerable<UTinyEntity>) entities);
                    
                    watch.Stop();
                    Debug.Log($"CommandStream.BackEnd.Persist Entities=[{kCount}] {watch.ElapsedMilliseconds}ms Len=[{command.Position}]");
                }
                
                command.Position = 0;

                // Create a registry to hold accepted objects
                var output = new UTinyRegistry();

                // Process the command 
                // commands -> mem
                {
                    var watch = System.Diagnostics.Stopwatch.StartNew();
                    
                    FrontEnd.Accept(command, output);
                    
                    watch.Stop();
                    Debug.Log($"CommandStream.FrontEnd.Accept Entities=[{kCount}] {watch.ElapsedMilliseconds}ms");
                }
            }
        }
    }
}
#endif // NET_4_6
