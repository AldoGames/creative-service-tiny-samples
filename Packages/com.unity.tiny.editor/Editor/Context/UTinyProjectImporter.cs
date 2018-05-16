#if NET_4_6
using System.IO;
using UnityEditor.Experimental.AssetImporters;
using UnityEngine;

namespace Unity.Tiny
{
    [ScriptedImporter(7, new [] {UTinyPersistence.ProjectFileImporterExtension})]
    public class UTinyProjectImporter : ScriptedImporter
    {
        public override void OnImportAsset(AssetImportContext ctx)
        {
            var asset = ScriptableObject.CreateInstance<UTProject>();
            ctx.AddObjectToAsset("asset", asset);
            ctx.SetMainObject(asset);
            
            UTinyPersistence.MarkAssetChanged(ctx.assetPath);
        }
    }
}
#endif // NET_4_6
