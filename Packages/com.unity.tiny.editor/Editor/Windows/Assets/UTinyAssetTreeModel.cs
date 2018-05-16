#if NET_4_6
using System.Collections.Generic;

namespace Unity.Tiny
{
	public class UTinyAssetTreeModel
    {
        private readonly Dictionary<int, UTinyAssetInfo> m_Assets;
        
        public IRegistry Registry { get; }
        public UTinyModule.Reference MainModule { get; }
        
        public UTinyAssetTreeModel(IRegistry registry, UTinyModule.Reference mainModule)
        {
            Registry = registry;
            MainModule = mainModule;
            m_Assets = new Dictionary<int, UTinyAssetInfo>();
        }
        
        public IEnumerable<UTinyAssetInfo> GetAssetInfos()
        {
            return AssetIterator.EnumerateAssets(MainModule.Dereference(Registry));
        }
        
        /// <summary>
        /// Generates a unique id for the given asset
        /// </summary>
        public int GenerateId(UTinyAssetInfo assetInfo)
        {
            var id = assetInfo.Object.GetInstanceID();
            m_Assets.Add(id, assetInfo);
            return id;
        }
        
        /// <summary>
        /// Find a asset by id
        /// </summary>
        public UTinyAssetInfo Find(int id)
        {
            UTinyAssetInfo assetInfo;
            m_Assets.TryGetValue(id, out assetInfo);
            return assetInfo;
        }
        
        public void ClearIds()
        {
            m_Assets.Clear();
        }
    }
}
#endif // NET_4_6
