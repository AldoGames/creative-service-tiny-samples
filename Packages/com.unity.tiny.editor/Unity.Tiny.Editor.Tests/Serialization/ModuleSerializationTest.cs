#if NET_4_6
using System.IO;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace Unity.Tiny.Test
{
    /// <summary>
    /// These tests are to ensure that UTinyModule value are preserved when passing through the serializaton pipeline
    /// </summary>
    [TestFixture]
    public class ModuleSerializationTest
    {
        private IRegistry m_Registry;
        private UTinyModule m_Module;
        private Texture2D m_Texture2D;
        
        [SetUp]
        public void SetUp()
        {
            m_Registry = new UTinyRegistry();
            m_Module = m_Registry.CreateModule(UTinyId.New(), "TestModule");
        }
        
        [TearDown]
        public void TearDown()
        {
        }

        [Test]
        public void AssetSerializationTest()
        {
            // Create some asset on disc
            File.WriteAllBytes(Application.dataPath + "/TestTexture.png", new Texture2D(32, 32).EncodeToPNG());
            AssetDatabase.ImportAsset("Assets/TestTexture.png");
            m_Texture2D = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/TestTexture.png");
            
            // Reference the asset in the module
            m_Module.AddAsset(m_Texture2D);
            
            Debug.Log(m_Module);

            var registry = new UTinyRegistry();
            using (var json = new MemoryStream())
            using (var command = new MemoryStream())
            {
                // Write the module to the stream
                Serialization.FlatJson.BackEnd.Persist(json, m_Module);
                json.Position = 0;
                
                Serialization.FlatJson.FrontEnd.Accept(json, command);
                command.Position = 0;
                
                Serialization.CommandStream.FrontEnd.Accept(command, registry);
            }

            var module = registry.FindById<UTinyModule>(m_Module.Id);
            Debug.Log(module);
            
            var path = AssetDatabase.GetAssetPath(m_Texture2D);
            AssetDatabase.DeleteAsset(path);
        }
    }
}
#endif // NET_4_6
