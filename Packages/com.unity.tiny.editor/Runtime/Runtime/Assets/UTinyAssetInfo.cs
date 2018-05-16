#if NET_4_6
using System.Collections.Generic;
using System.Collections.ObjectModel;
using UnityEngine;

namespace Unity.Tiny
{
    /// <summary>
    /// Editor only representation of an asset
    /// </summary>
    public class UTinyAssetInfo
    {
        private readonly List<UTinyAssetInfo> m_Children = new List<UTinyAssetInfo>();
        private readonly List<UTinyModule.Reference> m_ExplicitReferences = new List<UTinyModule.Reference>();
        private readonly List<UTinyEntity.Reference> m_ImplicitReferences = new List<UTinyEntity.Reference>();

        public string Name { get; }
        
        /// <summary>
        /// The object for this asset
        /// </summary>
        public Object Object { get; }

        /// <summary>
        /// Unique asset path
        /// </summary>
        public string AssetPath
        {
            get
            {
#if UNITY_EDITOR
                return UnityEditor.AssetDatabase.GetAssetPath(Object);
#else
                return Object.name;
#endif
            }
        }

        /// <summary>
        /// Is this asset explicity included by the user
        /// </summary>
        public bool IncludedExplicitly => m_ExplicitReferences.Count > 0;

        /// <summary>
        /// Is this asset implicitly included by a value reference
        /// </summary>
        public bool IncludedImplicitly => m_ImplicitReferences.Count > 0;

        /// <summary>
        /// List of modules that explicitly declare this asset
        /// </summary>
        public ReadOnlyCollection<UTinyModule.Reference> ExplicitReferences => m_ExplicitReferences.AsReadOnly();

        /// <summary>
        /// List of entities that implicitly declare this asset
        /// </summary>
        public ReadOnlyCollection<UTinyEntity.Reference> ImplicitReferences => m_ImplicitReferences.AsReadOnly();

        /// <summary>
        /// Parent for this asset (if any)
        /// </summary>
        public UTinyAssetInfo Parent { get; set; }

        /// <summary>
        /// List of sub assets for this asset (e.g. Sprites for a Texture2D)
        /// </summary>
        public IEnumerable<UTinyAssetInfo> Children => m_Children.AsReadOnly();

        public UTinyAssetInfo(Object @object, string name)
        {
            Object = @object;
            Name = name;
        }

        public void AddExplicitReference(UTinyModule.Reference module)
        {
            m_ExplicitReferences.Add(module);
        }

        public void AddImplicitReference(UTinyEntity.Reference entity)
        {
            m_ImplicitReferences.Add(entity);
        }

        public void AddChild(UTinyAssetInfo assetInfo)
        {
            if (m_Children.Contains(assetInfo))
            {
                return;
            }

            m_Children.Add(assetInfo);
        }

        public static bool operator ==(UTinyAssetInfo a, UTinyAssetInfo b)
        {
            // If both are null, or both are same instance, return true
            if (ReferenceEquals(a, b))
            {
                return true;
            }

            // If one is null, but not both, return false
            if ((object) a == null || (object) b == null)
            {
                return false;
            }

            // Return true if the fields match
            return a.Object == b.Object;
        }

        public static bool operator !=(UTinyAssetInfo a, UTinyAssetInfo b)
        {
            return !(a == b);
        }

        public override bool Equals(object obj)
        {
            if (!(obj is UTinyAssetInfo))
            {
                return false;
            }

            var asset = (UTinyAssetInfo) obj;

            return Object == asset.Object;
        }

        public override int GetHashCode()
        {
            return Object.GetHashCode();
        }
    }
}
#endif // NET_4_6
