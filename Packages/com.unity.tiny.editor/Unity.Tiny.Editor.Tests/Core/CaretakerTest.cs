#if NET_4_6
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;

namespace Unity.Tiny.Test
{
    [TestFixture]
    public class CaretakerTest
    {
        UTinyContext context;
        UTinyRegistry registry;
        UTinyCaretaker caretaker;

        [SetUp]
        public void Setup()
        {
            context = new UTinyContext();
            registry = context.Registry;
            caretaker = context.Caretaker;
        }

        [TearDown]
        public void Teardown()
        {
            context = null;
            registry = null;
            caretaker = null;
        }

        [Test]
        public void DetectTypeChanges()
        {
            // Create two new types
            var testStructType = registry.CreateType(UTinyId.New(), "TestStructType", UTinyTypeCode.Struct);
            registry.CreateType(UTinyId.New(), "TestComponentType", UTinyTypeCode.Struct);

            // Update to get the initial state; flush changes
            caretaker.Update();

            {
                // Make some changes to the data model
                // NOTE: We can make as many changes as we want with no callbacks being invoked. It is simply a version increment
                testStructType.CreateField("TestIntField", (UTinyType.Reference) UTinyType.Int32);
                testStructType.CreateField("TestStringField", (UTinyType.Reference) UTinyType.String);
                testStructType.Name = "OtherTestStructType";

                var count = 0;
                
                // Register for changed events
                caretaker.OnObjectChanged += (originator, memento) =>
                {
                    count++;
                    Assert.AreEqual(testStructType, originator);
                };
                
                // Invoke update
                // This will detect any changes that were made between now and the last Update.
                caretaker.Update();

                // We should be notified that one object was changed
                Assert.AreEqual(1, count);
            }
        }
        
        [Test]
        public void RestoreTest_Simple()
        {
            // Create a type
            var testStructType = registry.CreateType(UTinyId.New(), "TestStructType", UTinyTypeCode.Struct);
            var testRef = (UTinyType.Reference) testStructType;

            IMemento initialState = null;
            
            // Register for changed events
            caretaker.OnObjectChanged += (originator, memento) =>
            {
                initialState = memento;
            };
            
            // Update to get the initial state; flush changes
            caretaker.Update();
            
            Assert.NotNull(initialState);

            {
                // Make some changes to the created type
                testStructType.Name = "OtherTestStructType";
                Assert.AreEqual(testStructType.Name, "OtherTestStructType");

                // revert them
                testStructType.Restore(initialState);

                testStructType = testRef.Dereference(context.Registry);
                Assert.NotNull(testStructType);
                
                Assert.AreEqual(testStructType.Name, "TestStructType");
            }
        }

        /// <summary>
        /// Test:
        ///     1- Create a component with a single field of type int.
        ///     2- Change the field type to be of EntityReference.
        ///     3- Restore it back to its initial field type (Undo).
        ///     4- Restore the field type change (Redo).
        /// </summary>
        [Test]
        public void RestoreTest_FieldTypeChanged()
        {
            //////////////////////////////////////////////////////////////
            // Setup for this specific test.
            //////////////////////////////////////////////////////////////
            var initialFieldType = registry.FindByName<UTinyType>("Int32");
            var expectedInitialType = typeof(int);
            var changedFieldType = registry.FindByName<UTinyType>("EntityReference");
            var expectedChangedType = typeof(UTinyEntity.Reference);

            IMemento state = null;

            // Register for changed events
            caretaker.OnObjectChanged += (originator, memento) =>
            {
                state = memento;
            };

            //////////////////////////////////////////////////////////////
            // 1. Create a component with a single field of type int.
            //////////////////////////////////////////////////////////////
            var componentType = registry.CreateType(UTinyId.New(), "Component", UTinyTypeCode.Component);
            var field = componentType.CreateField(UTinyId.New(), "Field", (UTinyType.Reference)initialFieldType);
            componentType.Refresh();

            // Check default value
            {
                var defaultValue = componentType.DefaultValue as UTinyObject;
                Assert.IsNotNull(defaultValue);
                Assert.IsTrue(defaultValue.IsDefaultValue);
                Assert.IsTrue(field.FieldType.Equals((UTinyType.Reference)initialFieldType));
                Assert.AreEqual(expectedInitialType, defaultValue["Field"].GetType());
                Assert.AreEqual(defaultValue["Field"], 0);
            }

            Debug.Log($"Initial State: {componentType.Id}: {(componentType.DefaultValue as UTinyObject)["Field"]}");

            // Update to get the initial state; flush changes
            caretaker.Update();
            IMemento initialState = state;
            Assert.NotNull(initialState);

            //////////////////////////////////////////////////////////////
            // 2- Change the field type to be of EntityReference.
            //////////////////////////////////////////////////////////////
            field.FieldType = (UTinyType.Reference)changedFieldType;
            componentType.Refresh();

            // Check default value
            {
                var defaultValue = componentType.DefaultValue as UTinyObject;
                Assert.IsNotNull(defaultValue);
                Assert.IsTrue(defaultValue.IsDefaultValue);
                Assert.IsTrue(field.FieldType.Equals((UTinyType.Reference)changedFieldType));
                Assert.AreEqual(expectedChangedType, defaultValue["Field"].GetType());
                Assert.AreEqual(defaultValue["Field"], UTinyEntity.Reference.None);
            }

            Debug.Log($"Changed State: {componentType.Id}: {(componentType.DefaultValue as UTinyObject)["Field"]}");

            // Update to get the changed state; flush changes
            caretaker.Update();
            IMemento changedState = state;
            Assert.NotNull(changedState);
            Assert.AreNotEqual(initialState, changedState);
            Assert.IsTrue(initialState.Version < changedState.Version);

            //////////////////////////////////////////////////////////////
            // 3 - Restore it back to its initial field type (Undo).
            //////////////////////////////////////////////////////////////
            Debug.Log("Undo");
            Debug.Log($"Before: {componentType.Id}: {(componentType.DefaultValue as UTinyObject)["Field"]}");
            componentType.Restore(initialState);
            // Note: Restoring is not in-place, so we need to re-set the references
            componentType = registry.FindById<UTinyType>(componentType.Id);
            componentType.Refresh();
            field = componentType.Fields[0];
            Debug.Log($"After: {componentType.Id}: {(componentType.DefaultValue as UTinyObject)["Field"]}");
            // Check default value
            {
                var defaultValue = componentType.DefaultValue as UTinyObject;
                Assert.IsNotNull(defaultValue);
                Assert.IsTrue(defaultValue.IsDefaultValue);
                Assert.IsTrue(field.FieldType.Equals((UTinyType.Reference)initialFieldType));
                Assert.AreEqual(expectedInitialType, defaultValue["Field"].GetType());
                Assert.AreEqual(defaultValue["Field"], 0);
            }

            // Update to get the changed state; flush changes
            caretaker.Update();

            //////////////////////////////////////////////////////////////
            // 4- Restore the field type change (Redo).
            //////////////////////////////////////////////////////////////
            Debug.Log("Redo");
            Debug.Log($"Before: {componentType.Id}: {(componentType.DefaultValue as UTinyObject)["Field"]}");
            componentType.Restore(changedState);
            // Note: Restoring is not in-place, so we need to re-set the references
            componentType = registry.FindById<UTinyType>(componentType.Id);
            componentType.Refresh();
            field = componentType.Fields[0];
            Debug.Log($"After: {componentType.Id}: {(componentType.DefaultValue as UTinyObject)["Field"]}");
            // Check default value
            {
                var defaultValue = componentType.DefaultValue as UTinyObject;
                Assert.IsNotNull(defaultValue);
                Assert.IsTrue(defaultValue.IsDefaultValue);
                Assert.IsTrue(field.FieldType.Equals((UTinyType.Reference)changedFieldType));
                Assert.AreEqual(expectedChangedType, defaultValue["Field"].GetType());
                Assert.AreEqual(defaultValue["Field"], UTinyEntity.Reference.None);
            }
        }

        [Test]
        public void RestoreTest_UTinyEntity()
        {
            var compType = registry.CreateType(UTinyId.New(), "TestComponent", UTinyTypeCode.Component);
            var compTypeRef = (UTinyType.Reference) compType;
            
            var testStructType = registry.CreateType(UTinyId.New(), "TestStruct", UTinyTypeCode.Struct);
            testStructType.CreateField(UTinyId.New(), "IntField", (UTinyType.Reference) UTinyType.Int32);
            
            compType.CreateField(UTinyId.New(), "TestStructField", (UTinyType.Reference) testStructType);
            
            var undo = new Dictionary<UTinyId, IMemento>();
            caretaker.OnObjectChanged += (originator, memento) =>
            {
                undo[originator.Id] = memento;
            };

            var entity = registry.CreateEntity(UTinyId.New(), "TestEntity");
            var entityRef = (UTinyEntity.Reference) entity;
            
            var testCompInstance = entity.AddComponent(compTypeRef);
            testCompInstance.Refresh();

            var obj = new UTinyObject(registry, (UTinyType.Reference) testStructType)
            {
                ["IntField"] = 0
            };
            testCompInstance["TestStructField"] = obj;
            var item = (UTinyObject)testCompInstance["TestStructField"];
            
            // Update to get the initial state; flush changes
            caretaker.Update();
            
            item["IntField"] = 123;
            Assert.AreEqual(123, item["IntField"]);

            // UNDO
            entity.Restore(undo[entity.Id]);

            entity = entityRef.Dereference(context.Registry);
            Assert.NotNull(entity);
            testCompInstance = entity.GetComponent(compTypeRef);
            Assert.NotNull(testCompInstance);
            item = (UTinyObject) testCompInstance["TestStructField"];
            Assert.NotNull(item);
            
            // make sure IntField was restored
            Assert.AreEqual(0, item["IntField"]);
        }

        [Test]
        public void RestoreTest_Lists_Containers()
        {
            // Create two new types
            var testStructType = registry.CreateType(UTinyId.New(), "TestStructType", UTinyTypeCode.Struct);
            var testStructTypeRef = (UTinyType.Reference) testStructType;
            
            var listField = testStructType.CreateField(UTinyId.New(), "TestListField", (UTinyType.Reference)UTinyType.Float32, true);

            var undo = new Dictionary<UTinyId, IMemento>();
            
            // Register for changed events
            caretaker.OnObjectChanged += (originator, memento) =>
            {
                undo[originator.Id] = memento;
            };
            
            // Update to get the initial state; flush changes
            caretaker.Update();
            // note: UTinyField proxies version storage onto UTinyType, so there should be only 1 memento
            Assert.AreEqual(1, undo.Count);
            
            {
                // Make some changes to the created type
                testStructType.Name = "OtherTestStructType";
                Assert.AreEqual(testStructType.Name, "OtherTestStructType");
                listField.Name = "RevertMe";
                Assert.AreEqual(listField.Name, "RevertMe");

                // revert changes
                var kvp = undo.First();
                var obj = registry.FindById<UTinyType>(kvp.Key);
                
                Assert.NotNull(obj);
                Assert.IsTrue(ReferenceEquals(obj, testStructType));
                
                obj.Restore(kvp.Value);

                obj = testStructTypeRef.Dereference(context.Registry);
                Assert.NotNull(obj);
                
                // the field was detached from the list and re-created
                Assert.AreEqual(1, obj.Fields.Count);
                var newListField = obj.Fields[0];
                
                Assert.AreEqual("TestStructType", obj.Name);
                Assert.AreEqual("TestListField", newListField.Name);
                Assert.AreEqual(listField.Id, newListField.Id);
                Assert.AreEqual(listField.DeclaringType.Id, newListField.DeclaringType.Id);
            }
        }

        [Test]
        public void DetectEntityChanges()
        {
            // Create a type and an entity
            var componentType = registry.CreateType(UTinyId.New(), "TestStructType", UTinyTypeCode.Component);
            var entity = registry.CreateEntity(UTinyId.New(), "TestEntity");

            // Update to get the initial state; flush changes
            caretaker.Update();

            {
                // Snapshot the initial version
                var entityVersion = entity.Version;

                // Make some changes to the data model
                // NOTE: We can make as many changes as we want with no callbacks being invoked. It is simply a version increment
                entity.AddComponent((UTinyType.Reference) componentType);
                entity.Name = "NewEntityName";

                var count = 0;
                
                // Register for changed events
                caretaker.OnObjectChanged += (originator, memento) =>
                {
                    count++;
                    Assert.AreEqual(originator, entity);
                };
                
                // Invoke update
                // This will detect any changes that were made between now and the last Update.
                caretaker.Update();

                Assert.AreNotEqual(entityVersion, entity.Version);

                // We should be notified that one object was changed
                Assert.AreEqual(1, count);
            }
        }
        
        [Test]
        public void DetectComponentChanges()
        {
            // Create a type and an entity
            var componentType = registry.CreateType(UTinyId.New(), "TestComponentType", UTinyTypeCode.Component);
            var testField = componentType.CreateField("TestField", (UTinyType.Reference) UTinyType.Int32);
            var entity = registry.CreateEntity(UTinyId.New(), "TestEntity");
            var component = entity.AddComponent((UTinyType.Reference) componentType);
            component.Refresh();
            
            // Update to get the initial state; flush changes
            caretaker.Update();
                
            // Register for changed events
            caretaker.OnObjectChanged += (originator, memento) =>
            {
                Debug.Log(memento);
            };

            {
                (componentType.DefaultValue as UTinyObject)["TestField"] = 5;
                componentType.Refresh();
                
                // Invoke update
                // This will detect any changes that were made between now and the last Update.
                Debug.Log("-------------------- UPDATE --------------------");
                caretaker.Update();

                component["TestField"] = 10;
                
                // Invoke update
                // This will detect any changes that were made between now and the last Update.
                Debug.Log("-------------------- UPDATE --------------------");
                caretaker.Update();

                testField.FieldType = (UTinyType.Reference) UTinyType.String;
                component.Refresh();
                
                // Invoke update
                // This will detect any changes that were made between now and the last Update.
                Debug.Log("-------------------- UPDATE --------------------");
                caretaker.Update();
            }
        }

        [Test]
        public void DetectSceneChanges()
        {

        }

        [Test]
        public void PerformanceTest()
        {
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
                Debug.Log($"Create Entities=[{kCount}] {watch.ElapsedMilliseconds}ms");
            }
            
            caretaker.OnObjectChanged += (originiator, memento) =>
            {
                // Force the callback
            };
            
            {
                var watch = System.Diagnostics.Stopwatch.StartNew();

                caretaker.Update();
                
                watch.Stop();
                Debug.Log($"Caretaker.Update Entities=[{kCount}] {watch.ElapsedMilliseconds}ms");
            }
            
            {
                var watch = System.Diagnostics.Stopwatch.StartNew();

                caretaker.Update();
                
                watch.Stop();
                Debug.Log($"Caretaker.Update Entities=[{kCount}] {watch.ElapsedMilliseconds}ms");
            }
        }
    }
}
#endif // NET_4_6
