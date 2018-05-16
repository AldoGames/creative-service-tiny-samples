#if NET_4_6
using NUnit.Framework;
using UnityEngine;

namespace Unity.Tiny.Test
{
    [TestFixture]
    public class UTinyObjectTest
    {
        private IRegistry m_Registry;
        private UTinyType m_TestStruct;
        private UTinyType m_TestStructWithList;
        private UTinyType m_TestComponent;
        
        [SetUp]
        public void SetUp()
        {
            m_Registry = new UTinyRegistry();
            
            m_TestStruct = m_Registry.CreateType(UTinyId.New(), "TestStruct", UTinyTypeCode.Struct);
            m_TestStruct.CreateField("Foo", (UTinyType.Reference) UTinyType.String);
            m_TestStruct.CreateField("Bar", (UTinyType.Reference) UTinyType.Int32);
            
            m_TestStructWithList = m_Registry.CreateType(UTinyId.New(), "TestStructWithList", UTinyTypeCode.Struct);
            m_TestStructWithList.CreateField("Foo", (UTinyType.Reference) UTinyType.String, true);
            m_TestStructWithList.CreateField("Bar", (UTinyType.Reference) UTinyType.Int32, true);
            
            m_TestComponent = m_Registry.CreateType(UTinyId.New(), "TestComponent", UTinyTypeCode.Component);
            m_TestComponent.CreateField("TestStructField", (UTinyType.Reference) m_TestStruct);
        }

        [TearDown]
        public void TearDown()
        {
        }

        [Test]
        public void UTinyObject_Dynamic_PrimitiveValue()
        {
            // Untyped dynamic object
            var @object = new UTinyObject(m_Registry, UTinyType.Reference.None)
            {
                ["Foo"] = "Test",
                ["Bar"] = 10
            };
            
            Assert.AreEqual("Test", @object["Foo"]);
            Assert.AreEqual(10, @object["Bar"]);
            
            Debug.Log(@object.ToString());
        }
        
        
        [Test]
        public void UTinyObject_Dynamic_PrimitiveValuePromotion()
        {
            // Untyped dynamic object
            var @object = new UTinyObject(m_Registry, UTinyType.Reference.None)
            {
                ["Foo"] = "Test",
                ["Bar"] = 10
            };

            // Assign a type to it
            @object.Type = (UTinyType.Reference) m_TestStruct;
            
            // Dynamic values should be promoted to field values
            @object.Refresh();
            
            Assert.AreEqual("Test", @object["Foo"]);
            Assert.AreEqual(10, @object["Bar"]);
            
            Debug.Log(@object.ToString());
        }
        
        [Test]
        public void UTinyObject_FieldRename()
        {
            var @object = new UTinyObject(m_Registry, (UTinyType.Reference) m_TestStruct)
            {
                ["Foo"] = "Test",
                ["Bar"] = 10
            };

            m_TestStruct.Fields[0].Name = "Baz";
            
            // Dynamic values should be promoted to field values
            @object.Refresh();
            
            Assert.AreEqual("Test", @object["Baz"]);
            Assert.AreEqual(10, @object["Bar"]);
            
            Debug.Log(@object.ToString());
        }
        
        [Test]
        public void UTinyObject_Dynamic_NestedObject()
        {
            // Untyped dynamic object
            var @object = new UTinyObject(m_Registry, UTinyType.Reference.None)
            {
                ["TestStructField"] = new UTinyObject(m_Registry, UTinyType.Reference.None)
                {
                    ["Foo"] = "Test",
                    ["Bar"] = 10
                },
                ["Baz"] = 1.3F
            };
            
            Assert.AreEqual("Test", (@object["TestStructField"] as UTinyObject)?["Foo"]);
            Assert.AreEqual(10, (@object["TestStructField"] as UTinyObject)?["Bar"]);
            Assert.AreEqual(1.3F, @object["Baz"]);
            
            Debug.Log(@object.ToString());
        }
        
        [Test]
        public void UTinyObject_Dynamic_NestedObjectPromotion()
        {
            // Untyped dynamic object
            var @object = new UTinyObject(m_Registry, UTinyType.Reference.None)
            {
                ["TestStructField"] = new UTinyObject(m_Registry, UTinyType.Reference.None)
                {
                    ["Dynamic"] = "Value",
                    ["Foo"] = "Test",
                    // Bar should be auto generated in its default state
                },
                ["Baz"] = 1.3f
            };

            @object.Type = (UTinyType.Reference) m_TestComponent;
            @object.Refresh();
            
            // Assert.AreEqual("Value", (@object["TestStructField"] as UTinyObject)?["Dynamic"]);
            Assert.AreEqual("Test", (@object["TestStructField"] as UTinyObject)?["Foo"]);
            Assert.AreEqual(0, (@object["TestStructField"] as UTinyObject)?["Bar"]);
            // Assert.AreEqual(1.3F, @object["Baz"]);
            
            Debug.Log(@object.ToString());
        }
        
        [Test]
        public void UTinyObject_Dynamic_List()
        {
            // Untyped dynamic object
            var @object = new UTinyObject(m_Registry, UTinyType.Reference.None)
            {
                ["Foo"] = new UTinyList(m_Registry, UTinyType.Reference.None)
                {
                    "a", "b", "c"
                },
                ["Bar"] = new UTinyList(m_Registry, UTinyType.Reference.None)
                {
                    3, 6, 9
                }
            };
            
            Debug.Log(@object.ToString());
        }
        
        [Test]
        public void UTinyObject_Dynamic_ListPromotion()
        {
            // Untyped dynamic object
            var @object = new UTinyObject(m_Registry, UTinyType.Reference.None)
            {
                ["Foo"] = new UTinyList(m_Registry, UTinyType.Reference.None)
                {
                    "a", "b", "c"
                },
                ["Bar"] = new UTinyList(m_Registry, UTinyType.Reference.None)
                {
                    3, 6, 9
                }
            };

            @object.Type = (UTinyType.Reference) m_TestStructWithList;
            @object.Refresh();
            
            Debug.Log(@object.ToString());
        }
    }
}
#endif // NET_4_6
