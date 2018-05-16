#if NET_4_6
using NUnit.Framework;

namespace Unity.Tiny.Test
{
    [TestFixture]
    public class DefaultValueTest
    {
        /// <summary>
        /// Tests thats the defaultValue object is correctly initialized when creating new types
        /// </summary>
        [Test]
        public void TypeInitialDefaultValue()
        {
            var registry = new UTinyRegistry();

            var type = registry.CreateType(
                UTinyId.New(),
                "TestStruct",
                UTinyTypeCode.Struct);

            type.CreateField(
                "TestIntField",
                (UTinyType.Reference) UTinyType.Int32);

            type.CreateField(
                "TestFloatField",
                (UTinyType.Reference) UTinyType.Float32);

            var defaultValue = type.DefaultValue as UTinyObject;

            // Assert that we have some default value object that has been created for us
            Assert.IsNotNull(defaultValue);

            // Test the existance and value of the fields
            Assert.AreEqual(0, defaultValue["TestIntField"]);
            Assert.AreEqual(0f, defaultValue["TestFloatField"]);
        }

        /// <summary>
        /// Tests that compound types have the correct default values at creation
        /// </summary>
        [Test]
        public void NestedTypeInitialDefaultValue()
        {
            var registry = new UTinyRegistry();

            // Create a struct with a single int field
            var structType = registry.CreateType(
                UTinyId.New(),
                "TestStruct",
                UTinyTypeCode.Struct);

            structType.CreateField(
                "TestIntField",
                (UTinyType.Reference) UTinyType.Int32);

            // Default the TestStruct.IntField to 7
            structType.DefaultValue = new UTinyObject(registry, (UTinyType.Reference) structType)
            {
                ["TestIntField"] = 7
            };

            // Create a component with a single TestStruct field
            var componentType = registry.CreateType(
                UTinyId.New(),
                "TestComponent",
                UTinyTypeCode.Component);

            componentType.CreateField(
                "TestStructField",
                (UTinyType.Reference) structType);

            // Grab the default value for TestComponent
            var testComponentDefaultValue = componentType.DefaultValue as UTinyObject;

            Assert.IsNotNull(testComponentDefaultValue);

            // Grab the TestComponent.TestStructField FIELD defaultValue
            // NOTE: This is NOT the same as the TestStruct TYPE defaultValue
            var testComponentTestStructFieldDefaultValue = testComponentDefaultValue["TestStructField"] as UTinyObject;

            Assert.IsNotNull(testComponentTestStructFieldDefaultValue);

            Assert.AreNotEqual(testComponentDefaultValue, testComponentTestStructFieldDefaultValue);

            // This value should have been inherited from the type level but CAN be overriden
            Assert.AreEqual(7, testComponentTestStructFieldDefaultValue["TestIntField"]);
        }

        /// <summary>
        /// Tests that changing default values on compound types will correctly reflect to fields of that type
        /// </summary>
        [Test]
        public void NestedTypePropagateDefaultValueChange()
        {
            var registry = new UTinyRegistry();

            // Create a struct with 2 fields
            var testStructType = registry.CreateType(
                UTinyId.New(),
                "TestStructType",
                UTinyTypeCode.Struct);

            testStructType.CreateField(
                "TestIntField",
                (UTinyType.Reference) UTinyType.Int32);

            testStructType.CreateField(
                "TestFloatField",
                (UTinyType.Reference) UTinyType.Float32);

            // Default the TestStruct.IntField to 7 and FloatField to 0.5f
            testStructType.DefaultValue = new UTinyObject(registry, (UTinyType.Reference) testStructType)
            {
                ["TestIntField"] = 7,
                ["TestFloatField"] = 0.5f
            };

            // Create a component with a single TestStruct field
            var testComponentType = registry.CreateType(
                UTinyId.New(),
                "TestComponentType",
                UTinyTypeCode.Component);

            testComponentType.CreateField(
                "TestStructField",
                (UTinyType.Reference) testStructType);

            // Sanity check
            // NOTE: This is covered in other tests
            {
                var testComponentTypeDefaultValue = testComponentType.DefaultValue as UTinyObject;
                Assert.IsNotNull(testComponentTypeDefaultValue);

                var testComponentTypeTestStructFieldDefaultValue = testComponentTypeDefaultValue["TestStructField"] as UTinyObject;
                Assert.IsNotNull(testComponentTypeTestStructFieldDefaultValue);

                // This value should have been inherited from the type level but CAN be overridden
                Assert.AreEqual(7, testComponentTypeTestStructFieldDefaultValue["TestIntField"]);
                Assert.AreEqual(0.5, testComponentTypeTestStructFieldDefaultValue["TestFloatField"]);
            }

            {
                var testComponentTypeDefaultValue = (UTinyObject) testComponentType.DefaultValue;
                Assert.IsNotNull(testComponentTypeDefaultValue);

                var testComponentTypeTestStructFieldDefaultValue = testComponentTypeDefaultValue["TestStructField"] as UTinyObject;
                Assert.IsNotNull(testComponentTypeTestStructFieldDefaultValue);

                // Override the default value of the TestComponent.TestStructField.FloatField to 2.5f
                testComponentTypeTestStructFieldDefaultValue["TestFloatField"] = 2.5f;
            }

            {
                var testStructTypeDefaultValue = (UTinyObject) testStructType.DefaultValue;
                Assert.IsNotNull(testStructTypeDefaultValue);

                // Update the default value of TestStruct.IntField to 10
                testStructTypeDefaultValue["TestIntField"] = 10;
            }

            {
                var testComponentTypeDefaultValue = (UTinyObject) testComponentType.DefaultValue;
                Assert.IsNotNull(testComponentTypeDefaultValue);

                var testComponentTypeTestStructFieldDefaultValue = testComponentTypeDefaultValue["TestStructField"] as UTinyObject;
                Assert.IsNotNull(testComponentTypeTestStructFieldDefaultValue);

                // The IntField should have been correctly updated while the float field should remain overridden
                Assert.AreEqual(10, testComponentTypeTestStructFieldDefaultValue["TestIntField"]);
                Assert.AreEqual(2.5f, testComponentTypeTestStructFieldDefaultValue["TestFloatField"]);
            }
        }

        /// <summary>
        /// Tests that an object can be reset to it's default values
        /// </summary>
        [Test]
        public void ResetObjectToDefaultValues()
        {
            var registry = new UTinyRegistry();

            // Create a type
            var type = registry.CreateType(
                UTinyId.New(),
                "TestStructType",
                UTinyTypeCode.Struct);

            type.CreateField(
                "TestIntField",
                (UTinyType.Reference) UTinyType.Int32);

            type.CreateField(
                "TestFloatField",
                (UTinyType.Reference) UTinyType.Float32);

            // Default the TestStruct.IntField to 7 and FloatField to 0.5f
            type.DefaultValue = new UTinyObject(registry, (UTinyType.Reference) type)
            {
                ["TestIntField"] = 7,
                ["TestFloatField"] = 0.5f
            };

            var @object = new UTinyObject(registry, (UTinyType.Reference) type);

            Assert.AreEqual(7, @object["TestIntField"]);
            Assert.AreEqual(0.5f, @object["TestFloatField"]);

            @object["TestIntField"] = 1;
            @object["TestFloatField"] = 7.9f;

            Assert.AreEqual(1, @object["TestIntField"]);
            Assert.AreEqual(7.9f, @object["TestFloatField"]);

            @object.Reset();

            Assert.AreEqual(7, @object["TestIntField"]);
            Assert.AreEqual(0.5f, @object["TestFloatField"]);
        }

        [Test]
        public void EnumDefaultValue()
        {
            var registry = new UTinyRegistry();

            var enumType = registry.CreateType(UTinyId.New(), "TestEnum", UTinyTypeCode.Enum);
            enumType.BaseType = (UTinyType.Reference) UTinyType.Int32;
            enumType.CreateField("A", (UTinyType.Reference) UTinyType.Int32);
            enumType.CreateField("B", (UTinyType.Reference) UTinyType.Int32);
            enumType.CreateField("C", (UTinyType.Reference) UTinyType.Int32);
            enumType.DefaultValue = new UTinyObject(registry, (UTinyType.Reference) enumType)
            {
                ["A"] = 1,
                ["B"] = 2,
                ["C"] = 3,
            };

            var structType = registry.CreateType(UTinyId.New(), "TestStruct", UTinyTypeCode.Struct);
            structType.CreateField("EnumField", (UTinyType.Reference) enumType);
            structType.DefaultValue = new UTinyObject(registry, (UTinyType.Reference) structType)
            {
                ["EnumField"] = new UTinyEnum.Reference(enumType, "B")
            };
            
            var instance = new UTinyObject(registry, (UTinyType.Reference) structType);
            Assert.AreEqual(2, ((UTinyEnum.Reference) instance["EnumField"]).Value);
            Assert.AreEqual("B", ((UTinyEnum.Reference) instance["EnumField"]).Name);
        }
    }
}
#endif // NET_4_6
