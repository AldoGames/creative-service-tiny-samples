#if NET_4_6
using System.Linq;

using UnityEngine;
using UnityEditor;

using Unity.Tiny.Filters;

namespace Unity.Tiny
{
    public class UTinyTexturePostProcessor : AssetPostprocessor
    {
        private void OnPostprocessTexture(Texture2D texture)
        {
            EditorApplication.delayCall += RunAllTheBidings;
        }

        private void OnPostprocessSprites(Texture2D texture, Sprite[] sprites)
        {
            EditorApplication.delayCall += RunAllTheBidings;
        }

        private void RunAllTheBidings()
        {
            var entityGroupManager = UTinyEditorApplication.EntityGroupManager;
            var registry = UTinyEditorApplication.Registry;
            if (null == entityGroupManager || null == registry)
            {
                return;
            }
            foreach(var entity in entityGroupManager.LoadedEntityGroups.Deref(registry).SelectMany(s => s.Entities).Deref(registry))
            {
                BindingsHelper.RunAllBindings(entity);
            }
        }
    }
}
#endif // NET_4_6
