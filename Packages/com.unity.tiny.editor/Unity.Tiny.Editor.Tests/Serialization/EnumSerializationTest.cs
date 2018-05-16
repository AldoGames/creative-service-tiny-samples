#if NET_4_6
using System.IO;
using NUnit.Framework;
using UnityEngine;

namespace Unity.Tiny.Test
{
    /// <summary>
    /// These tests are to ensure that enum value are preserved when passing through the serializaton pipeline
    /// </summary>
    [TestFixture]
    public class EnumSerializationTest
    {
        private IRegistry m_Registry;
        private UTinyType m_EnumType;
        private UTinyType m_ComponentType;
        private UTinyEntity m_Entity;

        [SetUp]
        public void SetUp()
        {
            m_Registry = new UTinyRegistry();

            m_EnumType = m_Registry.CreateType(
                UTinyId.New(),
                "TestEnum",
                UTinyTypeCode.Enum);

            m_EnumType.BaseType = (UTinyType.Reference) UTinyType.Int32;

            m_EnumType.CreateField("A", (UTinyType.Reference) UTinyType.Int32);
            m_EnumType.CreateField("B", (UTinyType.Reference) UTinyType.Int32);
            m_EnumType.CreateField("C", (UTinyType.Reference) UTinyType.Int32);

            m_EnumType.DefaultValue = new UTinyObject(m_Registry, (UTinyType.Reference) m_EnumType)
            {
                // @NOTE We are intentionally starting at 1 to detect 0 case as errors
                ["A"] = 1,
                ["B"] = 2,
                ["C"] = 3
            };

            // Create a component with a single int field
            m_ComponentType = m_Registry.CreateType(
                UTinyId.New(),
                "TestComponent",
                UTinyTypeCode.Component);

            m_ComponentType.CreateField(
                "TestEnumField",
                (UTinyType.Reference) m_EnumType);

            m_Entity = m_Registry.CreateEntity(UTinyId.New(), "TestEntity");
            var component = m_Entity.AddComponent((UTinyType.Reference) m_ComponentType);
            component.Refresh();
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
            var component = m_Entity.GetComponent((UTinyType.Reference) m_ComponentType);
            var enumReference = (UTinyEnum.Reference) component["TestEnumField"];

            // The default value has not been explicitly defined.
            // It should be set the the first field
            Assert.AreEqual(m_EnumType.Fields[0].Id, enumReference.Id);
            Assert.AreEqual(1, enumReference.Value);
        }

        /// <summary>
        /// Ensures enum values can be serialized correctly
        /// </summary>
        [Test]
        public void BinaryDefaultValue()
        {
            // Output registry
            var registry = new UTinyRegistry();

            using (var binary = new MemoryStream())
            using (var command = new MemoryStream())
            {
                Debug.Log(Serialization.FlatJson.BackEnd.Persist(m_EnumType, m_ComponentType, m_Entity));
                
                // Write our data to binary
                Serialization.Binary.BackEnd.Persist(binary, m_EnumType, m_ComponentType, m_Entity);

                binary.Position = 0;

                // Read from binary to command stream
                Serialization.Binary.FrontEnd.Accept(binary, command);

                command.Position = 0;

                // Read from command stream to registry
                Serialization.CommandStream.FrontEnd.Accept(command, registry);
            }

            AssertDefaultValue(registry);
        }
        
        /// <summary>
        /// Ensures enum values can be serialized correctly
        /// </summary>
        [Test]
        public void FlatJsonDefaultValue()
        {
            // Output registry
            var registry = new UTinyRegistry();

            using (var json = new MemoryStream())
            using (var command = new MemoryStream())
            {
                // Write our data to json
                Serialization.FlatJson.BackEnd.Persist(json, m_EnumType, m_ComponentType, m_Entity);

                json.Position = 0;

                // Read from json to command stream
                Serialization.FlatJson.FrontEnd.Accept(json, command);

                command.Position = 0;

                // Read from command stream to registry
                Serialization.CommandStream.FrontEnd.Accept(command, registry);
            }

            AssertDefaultValue(registry);
        }
        
        
        /// <summary>
        /// Ensures enum values can be serialized correctly
        /// </summary>
        [Test]
        public void CommandStreamDefaultValue()
        {
            // Output registry
            var registry = new UTinyRegistry();

            using (var command = new MemoryStream())
            {
                // Write our data to command stream
                Serialization.CommandStream.BackEnd.Persist(command, m_EnumType, m_ComponentType, m_Entity);

                command.Position = 0;

                // Read from command stream to registry
                Serialization.CommandStream.FrontEnd.Accept(command, registry);
            }

            AssertDefaultValue(registry);
        }

        private void AssertDefaultValue(IRegistry registry)
        {
            var componentType = registry.FindById<UTinyType>(m_ComponentType.Id);

            var entity = registry.FindById<UTinyEntity>(m_Entity.Id);
            var component = entity.GetComponent((UTinyType.Reference) componentType);

            var enumReference = (UTinyEnum.Reference) component["TestEnumField"];

            // The default value has not been explicitly defined.
            // It should be set the the first field
            Assert.AreEqual(m_EnumType.Fields[0].Id, enumReference.Id);
            Assert.AreEqual(1, enumReference.Value);
        }
        
        /// <summary>
        /// Ensures enum values can be serialized correctly
        /// </summary>
        [Test]
        public void BinarySerializationOverrideValue()
        {
            // Output registry
            var registry = new UTinyRegistry();

            var component = m_Entity.GetComponent((UTinyType.Reference) m_ComponentType);
            component["TestEnumField"] = new UTinyEnum.Reference(m_EnumType, 3);

            using (var binary = new MemoryStream())
            using (var command = new MemoryStream())
            {
                // Write our data to binary
                Serialization.Binary.BackEnd.Persist(binary, m_EnumType, m_ComponentType, m_Entity);

                binary.Position = 0;

                // Read from binary to command stream
                Serialization.Binary.FrontEnd.Accept(binary, command);

                command.Position = 0;

                // Read from command stream to registry
                Serialization.CommandStream.FrontEnd.Accept(command, registry);
            }

            AssertOverrideValue(registry);
        }
        
        /// <summary>
        /// Ensures enum values can be serialized correctly
        /// </summary>
        [Test]
        public void FlatJsonOverrideValue()
        {
            // Output registry
            var registry = new UTinyRegistry();

            var component = m_Entity.GetComponent((UTinyType.Reference) m_ComponentType);
            component["TestEnumField"] = new UTinyEnum.Reference(m_EnumType, 3);

            using (var json = new MemoryStream())
            using (var command = new MemoryStream())
            {
                // Write our data to json
                Serialization.FlatJson.BackEnd.Persist(json, m_EnumType, m_ComponentType, m_Entity);

                json.Position = 0;

                // Read from json to command stream
                Serialization.FlatJson.FrontEnd.Accept(json, command);

                command.Position = 0;

                // Read from command stream to registry
                Serialization.CommandStream.FrontEnd.Accept(command, registry);
            }

            AssertOverrideValue(registry);
        }
        
        
        /// <summary>
        /// Ensures enum values can be serialized correctly
        /// </summary>
        [Test]
        public void CommandStreamOverrideValue()
        {
            // Output registry
            var registry = new UTinyRegistry();

            var component = m_Entity.GetComponent((UTinyType.Reference) m_ComponentType);
            component["TestEnumField"] = new UTinyEnum.Reference(m_EnumType, 3);

            using (var command = new MemoryStream())
            {
                // Write our data to command stream
                Serialization.CommandStream.BackEnd.Persist(command, m_EnumType, m_ComponentType, m_Entity);

                command.Position = 0;

                // Read from command stream to registry
                Serialization.CommandStream.FrontEnd.Accept(command, registry);
            }

            AssertOverrideValue(registry);
        }
        
        private void AssertOverrideValue(IRegistry registry)
        {
            var componentType = registry.FindById<UTinyType>(m_ComponentType.Id);

            var entity = registry.FindById<UTinyEntity>(m_Entity.Id);
            var component = entity.GetComponent((UTinyType.Reference) componentType);
            component.Refresh();

            var enumReference = (UTinyEnum.Reference) component["TestEnumField"];

            // The default value has not been overriden
            // It should be set the the last field
            Assert.AreEqual(m_EnumType.Fields[2].Id, enumReference.Id);
            Assert.AreEqual(3, enumReference.Value);
        }
    }
}
#endif // NET_4_6
