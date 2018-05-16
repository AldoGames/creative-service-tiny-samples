#if NET_4_6
using System.IO;
using NUnit.Framework;

namespace Unity.Tiny.Test
{
    /// <summary>
    /// These tests are to ensure that UnityEngine.Object value are preserved when passing through the serializaton pipeline
    /// </summary>
    [TestFixture]
    public class ListSerializationTest
    {
        private IRegistry m_Registry;
        private UTinyType m_IntArrayComponentType;
        private UTinyType m_StructArrayComponentType;
        private UTinyType m_StructType;
        private UTinyEntity m_IntArrayEntity;
        private UTinyEntity m_StructArrayEntity;

        [SetUp]
        public void SetUp()
        {
            m_Registry = new UTinyRegistry();
            
            // Create a component with an int array field
            m_IntArrayComponentType = m_Registry.CreateType(
                UTinyId.New(),
                "TestComponent",
                UTinyTypeCode.Component);

            m_IntArrayComponentType.CreateField(
                "TestIntArrayField",
                (UTinyType.Reference) UTinyType.Int32,
                true);

            m_IntArrayEntity = m_Registry.CreateEntity(UTinyId.New(), "TestEntity");
            var component = m_IntArrayEntity.AddComponent((UTinyType.Reference) m_IntArrayComponentType);
            component.Refresh();
            
            component["TestIntArrayField"] = new UTinyList(m_Registry, (UTinyType.Reference) UTinyType.Int32)
            {
                3, 6, 9
            };
            
            m_StructType = m_Registry.CreateType(
                UTinyId.New(),
                "TestStruct",
                UTinyTypeCode.Struct);
            
            m_StructType.CreateField(
                "TestIntField",
                (UTinyType.Reference) UTinyType.Int32);
            
            m_StructArrayComponentType = m_Registry.CreateType(
                UTinyId.New(),
                "TestComponent2",
                UTinyTypeCode.Component);

            m_StructArrayComponentType.CreateField(
                "TestStructArrayField",
                (UTinyType.Reference) m_StructType,
                true);
            
            m_StructArrayEntity = m_Registry.CreateEntity(UTinyId.New(), "TestEntity2");
            var component2 = m_StructArrayEntity.AddComponent((UTinyType.Reference) m_StructArrayComponentType);
            component2.Refresh();
            component2["TestStructArrayField"] = new UTinyList(m_Registry, (UTinyType.Reference) m_StructType)
            {
                new UTinyObject(m_Registry,  (UTinyType.Reference) m_StructType)
                {
                    ["TestIntField"] = 3
                },
                new UTinyObject(m_Registry,  (UTinyType.Reference) m_StructType)
                {
                    ["TestIntField"] = 6
                },
                new UTinyObject(m_Registry,  (UTinyType.Reference) m_StructType)
                {
                    ["TestIntField"] = 9
                }
            };
        }
        
        [TearDown]
        public void TearDown()
        {
            
        }
        
        /// <summary>
        /// Ensures UnityEngine.Object values can be serialized correctly
        /// </summary>
        [Test]
        public void BinaryListValue()
        {
            // Output registry
            var registry = new UTinyRegistry();
            
            using (var binary = new MemoryStream())
            using (var command = new MemoryStream())
            {
                Serialization.Binary.BackEnd.Persist(binary, m_IntArrayComponentType, m_IntArrayEntity);

                binary.Position = 0;

                Serialization.Binary.FrontEnd.Accept(binary, command);

                command.Position = 0;

                Serialization.CommandStream.FrontEnd.Accept(command, registry);
            }
            
            AssertListValue(registry);
        }
        
        /// <summary>
        /// Ensures UnityEngine.Object values can be serialized correctly
        /// </summary>
        [Test]
        public void FlatJsonListValue()
        {
            // Output registry
            var registry = new UTinyRegistry();
            
            using (var json = new MemoryStream())
            using (var command = new MemoryStream())
            {
                Serialization.FlatJson.BackEnd.Persist(json, m_IntArrayComponentType, m_IntArrayEntity);

                json.Position = 0;

                Serialization.FlatJson.FrontEnd.Accept(json, command);

                command.Position = 0;

                Serialization.CommandStream.FrontEnd.Accept(command, registry);
            }
            
            AssertListValue(registry);
        }
        
        /// <summary>
        /// Ensures UnityEngine.Object values can be serialized correctly
        /// </summary>
        [Test]
        public void FlatJsonStructListValue()
        {
            // Output registry
            var registry = new UTinyRegistry();
            
            using (var json = new MemoryStream())
            using (var command = new MemoryStream())
            {
                Serialization.FlatJson.BackEnd.Persist(json, m_StructType, m_StructArrayComponentType, m_StructArrayEntity);

                json.Position = 0;
                
                var reader = new StreamReader(json);
                {
                    UnityEngine.Debug.Log(reader.ReadToEnd());
                }
                
                json.Position = 0;

                Serialization.FlatJson.FrontEnd.Accept(json, command);

                command.Position = 0;

                Serialization.CommandStream.FrontEnd.Accept(command, registry);
            }
        }

        private void AssertListValue(IRegistry registry)
        {
            var componentType = registry.FindById<UTinyType>(m_IntArrayComponentType.Id);
            var entity = registry.FindById<UTinyEntity>(m_IntArrayEntity.Id);
            var component = entity.GetComponent((UTinyType.Reference) componentType);
            var list = component["TestIntArrayField"] as UTinyList;
            
            Assert.IsNotNull(list);
            Assert.AreEqual(3, list.Count);
            Assert.AreEqual(3, list[0]);
            Assert.AreEqual(6, list[1]);
            Assert.AreEqual(9, list[2]);
        }
    }
}
#endif // NET_4_6
