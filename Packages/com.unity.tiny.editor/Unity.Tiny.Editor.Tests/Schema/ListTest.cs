#if NET_4_6
using NUnit.Framework;
using UnityEngine;

namespace Unity.Tiny.Test
{
    [TestFixture]
    public class ListTest
    {
        [Test]
        public void PrimitiveList()
        {
            var registry = new UTinyRegistry();

            var list = new UTinyList(registry, (UTinyType.Reference) UTinyType.Int32)
            {
                1, 2, 3
            };
            
            Assert.AreEqual(3, list.Count);
            
            Debug.Log(list);
        }
        
        [Test]
        public void ObjectListVersionChange()
        {
            var registry = new UTinyRegistry();

            var type = registry.CreateType(UTinyId.New(), "TestType", UTinyTypeCode.Struct);
            type.CreateField("TestField", (UTinyType.Reference) UTinyType.Int32);

            var list = new UTinyList(registry, (UTinyType.Reference) type)
            {
                new UTinyObject(registry, (UTinyType.Reference) type)
                {
                    ["TestField"] = 1
                },
                new UTinyObject(registry, (UTinyType.Reference) type)
                {
                    ["TestField"] = 2
                },
                new UTinyObject(registry, (UTinyType.Reference) type)
                {
                    ["TestField"] = 3
                }
            };

            var version = list.Version;
            
            (list[0] as UTinyObject)["TestField"] = 7;
            
            Assert.AreNotEqual(version, list.Version);
            
            Debug.Log(list);
        }
        
        [Test]
        public void ListField()
        {
            var registry = new UTinyRegistry();

            var type = registry.CreateType(UTinyId.New(), "TestType", UTinyTypeCode.Struct);
            type.CreateField("TestField", (UTinyType.Reference) UTinyType.Int32, true);

            var instance = new UTinyObject(registry, (UTinyType.Reference) type)
            {
                ["TestField"] = new UTinyList(registry, (UTinyType.Reference) UTinyType.Int32)
                {
                    1, 2, 3
                }
            };

            Debug.Log(instance);
        }
        
        [Test]
        public void ListFieldPrimitiveAssignment()
        {
            var registry = new UTinyRegistry();

            var type = registry.CreateType(UTinyId.New(), "TestType", UTinyTypeCode.Struct);
            type.CreateField("TestField", (UTinyType.Reference) UTinyType.Int32, true);

            var instance = new UTinyObject(registry, (UTinyType.Reference) type)
            {
                ["TestField"] = new UTinyList(registry, (UTinyType.Reference) UTinyType.Int32)
                {
                    1,
                    2,
                    3
                }
            };

            instance["TestField"] = new UTinyList(registry, (UTinyType.Reference) UTinyType.Int32)
            {
                3,
                6,
                7
            };

            Debug.Log(instance);
        }
    }
}
#endif // NET_4_6
