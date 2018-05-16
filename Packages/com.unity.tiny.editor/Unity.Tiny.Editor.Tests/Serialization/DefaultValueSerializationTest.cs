#if NET_4_6
using System.IO;
using NUnit.Framework;
using UnityEngine;

namespace Unity.Tiny.Test
{
    /// <summary>
    /// These tests are to ensure that default values and overrides are preserved when passing through the serializaton pipeline
    /// </summary>
    [TestFixture]
    public class DefaultValueSerializationTest
    {
        private IRegistry m_Registry;
        private UTinyType m_ComponentType;
        private UTinyEntity m_DefaultEntity;
        private UTinyEntity m_OverridenEntity;

        private const int KTestFieldDefaultValue = 7;
        private const int KTestFieldOverrideValue = 7;
        
        [SetUp]
        public void SetUp()
        {
            m_Registry = new UTinyRegistry();

            // Create a component with a single int field
            m_ComponentType = m_Registry.CreateType(
                UTinyId.New(),
                "TestComponent",
                UTinyTypeCode.Component);

            m_ComponentType.CreateField(
                "TestIntField",
                (UTinyType.Reference) UTinyType.Int32);

            // Default the TestStruct.IntField to 7
            m_ComponentType.DefaultValue = new UTinyObject(m_Registry, (UTinyType.Reference) m_ComponentType)
            {
                ["TestIntField"] = KTestFieldDefaultValue
            };

            // Create an entity with our test component
            m_DefaultEntity = m_Registry.CreateEntity(UTinyId.New(), "DefaultEntity");

            {
                var c = m_DefaultEntity.AddComponent((UTinyType.Reference) m_ComponentType);
                c.Refresh();
            }
            
            // Create another entity with our test component
            m_OverridenEntity = m_Registry.CreateEntity(UTinyId.New(), "OverridenEntity");
            {
                var c = m_OverridenEntity.AddComponent((UTinyType.Reference) m_ComponentType);
                c.Refresh();
                c["TestIntField"] = KTestFieldOverrideValue;
            }
        }

        [TearDown]
        public void TearDown()
        {
            
        }

        /// <summary>
        /// Ensure our default values pass basic tests
        /// @NOTE This is covered more thoroughly in "DefaultValueTest.cs"
        /// </summary>
        [Test]
        public void DefaultValueSanityCheck()
        {
            {
                var c = m_DefaultEntity.GetComponent((UTinyType.Reference) m_ComponentType);
                
                // TestIntField should be defaulted to 7 and marked flagged as unchanged
                Assert.IsFalse(c.IsOverridden);
                Assert.AreEqual(KTestFieldDefaultValue, c["TestIntField"]);
            }
            
            {
                var c = m_OverridenEntity.GetComponent((UTinyType.Reference) m_ComponentType);
                
                // TestIntField should be overriden to 10 and marked flagged as changed
                Assert.IsTrue(c.IsOverridden);
                Assert.AreEqual(KTestFieldOverrideValue, c["TestIntField"]);
            }
        }

        /// <summary>
        /// Write to Binary and read back
        /// </summary>
        [Test]
        public void BinaryDefaultValues()
        {
            // Output registry
            var registry = new UTinyRegistry();

            using (var binary = new MemoryStream())
            using (var command = new MemoryStream())
            {
                // Write our data to binary
                Serialization.Binary.BackEnd.Persist(binary, m_ComponentType, m_DefaultEntity, m_OverridenEntity);

                binary.Position = 0;

                // Read from binary to command stream
                Serialization.Binary.FrontEnd.Accept(binary, command);

                command.Position = 0;

                // Read from command stream to registry
                Serialization.CommandStream.FrontEnd.Accept(command, registry);
            }

            AssertSerializationDefaultValues(registry);
        }
        
        /// <summary>
        /// Write to FlatJson and read back
        /// </summary>
        [Test]
        public void FlatJsonDefaultValues()
        {
            // Output registry
            var registry = new UTinyRegistry();
            
            using (var json = new MemoryStream())
            using (var command = new MemoryStream())
            {
                Debug.Log(Serialization.FlatJson.BackEnd.Persist(m_ComponentType, m_DefaultEntity, m_OverridenEntity));
                
                // Write our data to binary
                Serialization.FlatJson.BackEnd.Persist(json, m_ComponentType, m_DefaultEntity, m_OverridenEntity);

                json.Position = 0;
                
                // Read from binary to command stream
                Serialization.FlatJson.FrontEnd.Accept(json, command);
                
                command.Position = 0;
                
                // Read from command stream to registry
                Serialization.CommandStream.FrontEnd.Accept(command, registry);
            }
            
            AssertSerializationDefaultValues(registry);
        }
        
        /// <summary>
        /// Write to CommandsStream and read back
        /// </summary>
        [Test]
        public void CommandStreamDefaultValues()
        {
            // Output registry
            var registry = new UTinyRegistry();
            
            using (var command = new MemoryStream())
            {
                // Write from memory directly to the command stream
                Serialization.CommandStream.BackEnd.Persist(command, m_ComponentType, m_DefaultEntity, m_OverridenEntity);
                
                command.Position = 0;
                
                // Read from command stream to registry
                Serialization.CommandStream.FrontEnd.Accept(command, registry);
            }
            
            AssertSerializationDefaultValues(registry);
        }
        
        /// <summary>
        /// Helper method to make assertions based on the {FORMAT}SerializationDefaultValues tests
        /// </summary>
        /// <param name="registry"></param>
        private void AssertSerializationDefaultValues(IRegistry registry)
        {
            // Make sure the component type is transfered with its default values
            var componentType = registry.FindById<UTinyType>(m_ComponentType.Id);
            {
                // Ensure we are dealing with the new type
                Assert.AreNotEqual(m_ComponentType, componentType);

                var defaultValue = componentType.DefaultValue as UTinyObject;
                
                Assert.IsNotNull(defaultValue);
                Assert.AreEqual(KTestFieldDefaultValue, defaultValue["TestIntField"]);
            }

            var defaultEntity = registry.FindById<UTinyEntity>(m_DefaultEntity.Id);
            {
                // Ensure we are dealing with the transfered entity
                Assert.AreNotEqual(m_DefaultEntity, defaultEntity);

                var c = defaultEntity.GetComponent((UTinyType.Reference) componentType);
                
                // TestIntField should be defaulted to 7 and marked flagged as unchanged
                Assert.IsFalse(c.IsOverridden);
                Assert.AreEqual(KTestFieldDefaultValue, c["TestIntField"]);
            }
            
            var overrideEntity = registry.FindById<UTinyEntity>(m_OverridenEntity.Id);
            {
                // Ensure we are dealing with the transfered entity
                Assert.AreNotEqual(m_OverridenEntity, overrideEntity);

                var c = overrideEntity.GetComponent((UTinyType.Reference) componentType);
                
                // TestIntField should be overriden to 10 and marked flagged as changed
                Assert.IsTrue(c.IsOverridden);
                Assert.AreEqual(KTestFieldOverrideValue, c["TestIntField"]);
            }
        }
    }
}
#endif // NET_4_6
