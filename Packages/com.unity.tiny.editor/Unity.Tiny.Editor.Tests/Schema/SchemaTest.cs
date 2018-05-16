#if NET_4_6
using NUnit.Framework;
using UnityEngine;

namespace Unity.Tiny.Test
{
    [TestFixture]
    public class SchemaTest
    {
        /// <summary>
        /// Simple struct type creation with a single int field
        /// </summary>
        [Test]
        public void StructType()
        {
            var registry = new UTinyRegistry();

            var type = registry.CreateType(
                UTinyId.New(),
                "TestStruct",
                UTinyTypeCode.Struct
            );
            
            type.CreateField(
                "TestField",
                (UTinyType.Reference) UTinyType.Int32);
        
            Assert.AreEqual(type.Fields.Count, 1);
        }
        
        /// <summary>
        /// Create type based UnityEngine.Object (e.g. Texture2D, Mesh)
        /// </summary>
        [Test]
        public void FieldTest()
        {
            var registry = new UTinyRegistry();
            
            var @enum = registry.CreateType(
                UTinyId.New(),
                "TestEnum",
                UTinyTypeCode.Enum
            );

            @enum.BaseType = (UTinyType.Reference) UTinyType.Int32;
            @enum.CreateField("A", (UTinyType.Reference) UTinyType.Int32);

            var type = registry.CreateType(
                UTinyId.New(),
                "TestStruct",
                UTinyTypeCode.Struct
            );
            
            type.CreateField(
                "TextureReference",
                (UTinyType.Reference) UTinyType.Texture2DEntity);
            
            type.CreateField(
                "EntityReference",
                (UTinyType.Reference) UTinyType.EntityReference);
            
            type.CreateField(
                "EnumReference",
                (UTinyType.Reference) @enum);
            
            type.Refresh();
            
            Debug.Log(type);
        }

        [Test]
        public void NameChangeTest()
        {
            var registry = new UTinyRegistry();
            
            var type = registry.CreateType(
                UTinyId.New(),
                "TestStruct",
                UTinyTypeCode.Struct
            );

            var module = registry.CreateModule(
                UTinyId.New(),
                "TestModule"
            );

            module.AddStructReference((UTinyType.Reference) type);
            module.Refresh();

            type.Name = "NewStruct";
            
            Debug.Log(module.ToString());
        }
    }
}
#endif // NET_4_6
