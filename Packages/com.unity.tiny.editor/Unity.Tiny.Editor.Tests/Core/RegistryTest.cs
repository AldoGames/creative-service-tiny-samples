#if NET_4_6
using NUnit.Framework;
using Assert = UnityEngine.Assertions.Assert;

namespace Unity.Tiny.Test
{
	[TestFixture]
	public class RegistryTest
	{
		[Test]
		public void Register()
		{
			var registry = new UTinyRegistry();
			var builtInCount = registry.Count;

			var type = registry.CreateType(UTinyId.New(), "TestType", UTinyTypeCode.Component);
			Assert.AreEqual(builtInCount + 1, registry.Count);
			
			registry.Register(type);
			Assert.AreEqual(builtInCount + 1, registry.Count);

			registry.Unregister(type);
			Assert.AreEqual(builtInCount, registry.Count);
		}
		
		[Test]
		public void Clear()
		{
			var registry = new UTinyRegistry();
			var builtInCount = registry.Count;

			var type = registry.CreateType(UTinyId.New(), "TestType", UTinyTypeCode.Component);
			var typeRef = (UTinyType.Reference) type;
			Assert.AreEqual(builtInCount + 1, registry.Count);

			registry.Clear();
			Assert.AreEqual(builtInCount, registry.Count);
			Assert.IsNull(typeRef.Dereference(registry));
		}
		
		[Test]
		public void SourceScope()
		{
			var registry = new UTinyRegistry();
			var builtInCount = registry.Count;
			var sourceId = "test";

			using (registry.SourceIdentifierScope(sourceId))
			{
				registry.CreateType(UTinyId.New(), "TestType", UTinyTypeCode.Component);
			}

			Assert.AreEqual(builtInCount + 1, registry.Count);
			
			registry.UnregisterAllBySource(sourceId);
			Assert.AreEqual(builtInCount, registry.Count);
		}
		
		[Test]
		public void NestedSourceScope()
		{
			var registry = new UTinyRegistry();
			var builtInCount = registry.Count;
			var sourceId = "outer";
			var nestedSourceId = "inner";
			UTinyType testType, testType2;

			using (registry.SourceIdentifierScope(sourceId))
			{
				testType = registry.CreateType(UTinyId.New(), "TestType", UTinyTypeCode.Component);
				using (registry.SourceIdentifierScope(nestedSourceId))
				{
					testType2 = registry.CreateType(UTinyId.New(), "TestType2", UTinyTypeCode.Component);
				}
			}
			
			var testTypeRef = (UTinyType.Reference) testType;
			var testType2Ref = (UTinyType.Reference) testType2;

			Assert.AreEqual(builtInCount + 2, registry.Count);
			
			registry.UnregisterAllBySource(sourceId);
			Assert.AreEqual(builtInCount + 1, registry.Count);

			Assert.IsNull(testTypeRef.Dereference(registry));
			Assert.IsNotNull(testType2Ref.Dereference(registry));
			
			registry.UnregisterAllBySource(nestedSourceId);
			Assert.AreEqual(builtInCount, registry.Count);
			Assert.IsNull(testTypeRef.Dereference(registry));
			Assert.IsNull(testType2Ref.Dereference(registry));
		}
		
	}
}
#endif // NET_4_6
