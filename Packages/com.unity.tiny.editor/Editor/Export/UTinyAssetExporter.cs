#if NET_4_6
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Unity.Tiny.Extensions;
using Unity.Tiny.Filters;
using Object = UnityEngine.Object;

namespace Unity.Tiny
{
    /// <summary>
    /// Contains information on how an asset was exported
    /// </summary>
    public class UTinyExportInfo
    {
        public readonly UTinyAssetInfo AssetInfo;
        public readonly List<FileInfo> Exported = new List<FileInfo>();

        public UTinyExportInfo(UTinyAssetInfo assetInfo)
        {
            AssetInfo = assetInfo;
        }
    }

    /// <summary>
    /// Replacement for the Filtering API
    /// </summary>
    public static class AssetIterator
    {
        private static readonly Dictionary<Object, UTinyAssetInfo> s_Assets = new Dictionary<Object, UTinyAssetInfo>();

        public static IEnumerable<UTinyAssetInfo> EnumerateAssets(UTinyModule module)
        {
            foreach (var m in module.EnumerateDependencies())
            {
                foreach (var asset in m.Assets)
                {
                    if (!asset.Object || null == asset.Object)
                    {
                        continue;
                    }

                    var assetInfo = GetOrAddAssetInfo(s_Assets, asset.Object, asset.Name);
                    assetInfo.AddExplicitReference((UTinyModule.Reference) module);
                }
            }

            foreach (var entity in module.EnumerateDependencies().Entities())
            {
                foreach (var component in entity.Components)
                {
                    foreach (var @object in EnumerateUnityEngineObjects(component))
                    {
                        if (!@object || null == @object)
                        {
                            continue;
                        }

                        var asset = GetOrAddAssetInfo(s_Assets, @object, @object.name);
                        asset.AddImplicitReference((UTinyEntity.Reference) entity);
                    }
                }
            }

            // Return root assets
            var assets = s_Assets.Values.Where(r => r.Parent == null).ToList();
            s_Assets.Clear();
            return assets;
        }

        /// <summary>
        /// @TODO Move to filtering API
        /// </summary>
        private static IEnumerable<Object> EnumerateUnityEngineObjects(UTinyObject @object)
        {
            return @object.EnumerateProperties().SelectMany(property => Filter(property.Value));
        }

        private static IEnumerable<Object> Filter(object @object)
        {
            if (@object is Object)
            {
                yield return (Object) @object;
            }
            else if (@object is UTinyObject)
            {
                foreach (var v in EnumerateUnityEngineObjects((UTinyObject) @object))
                {
                    yield return v;
                }
            }
            else if (@object is UTinyList)
            {
                foreach (var item in (UTinyList) @object)
                {
                    foreach (var o in Filter(item))
                    {
                        yield return o;
                    }
                }
            }
        }

        private static UTinyAssetInfo GetOrAddAssetInfo(IDictionary<Object, UTinyAssetInfo> assets, Object @object, string name)
        {
            UTinyAssetInfo assetInfo;

            if (assets.TryGetValue(@object, out assetInfo))
            {
                return assetInfo;
            }

            assetInfo = new UTinyAssetInfo(@object, name);

            if (AssetDatabase.IsSubAsset(@object))
            {
                var path = AssetDatabase.GetAssetPath(@object);
                var parentObject = AssetDatabase.LoadMainAssetAtPath(path);
                var parentAsset = GetOrAddAssetInfo(assets, parentObject, parentObject.name);
                parentAsset.AddChild(assetInfo);
                assetInfo.Parent = parentAsset;
            }

            assets.Add(@object, assetInfo);

            return assetInfo;
        }
    }

    public static class UTinyAssetEntityGroupGenerator
    {
        public static string GetAssetEntityPath(Type type)
        {
            if (type == typeof(Texture2D))
            {
                return "assets/textures/";
            }

            if (type == typeof(Sprite))
            {
                return "assets/sprites/";
            }
            
            if (type == typeof(Font))
            {
                return "assets/fonts/";
            }

            if (type == typeof(AudioClip))
            {
                return "assets/audioclips/";
            }

            return string.Empty;
        }

        public static UTinyEntityGroup Generate(IRegistry registry, UTinyProject project)
        {
            var entityGroup = registry.CreateEntityGroup(UTinyId.New(), "Assets_Generated");
            var assets = AssetIterator.EnumerateAssets(project.Module.Dereference(project.Registry));

            foreach (var asset in assets)
            {
                CreateEntityForAsset(registry, project, entityGroup, asset);
            }

            return entityGroup;
        }

        private static void CreateEntityForAsset(IRegistry registry, UTinyProject project, UTinyEntityGroup entityGroup, UTinyAssetInfo asset)
        {
            var @object = asset.Object;

            UTinyEntity entity = null;

            if (@object is Texture2D)
            {
                var texture = @object as Texture2D;
                var path = AssetDatabase.GetAssetPath(texture);
                var importer = (TextureImporter) AssetImporter.GetAtPath(path);
                
                entity = registry.CreateEntity(UTinyId.New(), $"{GetAssetEntityPath(typeof(Texture2D))}{asset.Name}");

                var image2d = entity.AddComponent(registry.GetImage2DType());
                image2d.Refresh();
                
                image2d["imageFile"] = $"ut-asset:{asset.Name}";
                
                var settings = UTinyUtility.GetAssetExportSettings(project, @object) as UTinyTextureSettings;
                if (settings != null && settings.FormatType == TextureFormatType.JPG && UTinyAssetExporter.TextureExporter.ReallyHasAlpha(texture))
                {
                    image2d["maskFile"] = $"ut-asset:{asset.Name}_a";
                }

                image2d["disableSmoothing"] = importer.filterMode == FilterMode.Point;
                
                var sprite = AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().FirstOrDefault();
                
                // @NOTE The `importer.spritePixelsPerUnit` is currently NOT used in the editor... 
                // We ALWAYS draw sprites at 1 pixel to world unit in the editor.
                // When we switch to using SpriteRenderer as our editor drawer we can just pass `sprite.pixelsPerUnit` directly here.
                var pixelsToWorldUnits = sprite ? sprite.pixelsPerUnit : 1;
                image2d["pixelsToWorldUnits"] = 1.0f / pixelsToWorldUnits;
            }
            else if (@object is Sprite)
            {
                var sprite = (Sprite) @object;

                entity = registry.CreateEntity(UTinyId.New(), $"{GetAssetEntityPath(typeof(Sprite))}{asset.Name}");

                var sprite2d = entity.AddComponent(registry.GetSprite2DType());
                sprite2d.Refresh();

                sprite2d["image"] = sprite.texture;
                var region = sprite2d["imageRegion"] as UTinyObject;
                if (null != region)
                {
                    region["x"] = sprite.rect.x / sprite.texture.width;
                    region["y"] = sprite.rect.y / sprite.texture.height;
                    region["width"] = sprite.rect.width / sprite.texture.width;
                    region["height"] = sprite.rect.height / sprite.texture.height;
                }

                var pivot = sprite2d["pivot"] as UTinyObject;
                if (null != pivot)
                {
                    pivot["x"] = sprite.pivot.x / sprite.rect.width;
                    pivot["y"] = sprite.pivot.y / sprite.rect.height;
                }
            } 
            else if (@object is AudioClip)
            {
                entity = registry.CreateEntity(UTinyId.New(), $"{GetAssetEntityPath(typeof(AudioClip))}{asset.Name}");

                var audioClip = entity.AddComponent(registry.GetAudioClipType());
                audioClip.Refresh();
                audioClip["file"] = $"ut-asset:{asset.Name}";
            }           
            else if (@object is Font)
            {
                entity = registry.CreateEntity(UTinyId.New(), $"{GetAssetEntityPath(typeof(Font))}{asset.Name}");

                var fontAsset = entity.AddComponent(registry.GetFontType());
                fontAsset.Refresh();
                fontAsset["file"] = $"ut-asset:{asset.Name}";
            }

            if (null != entity)
            {
                entityGroup.AddEntityReference((UTinyEntity.Reference) entity);
            }

            foreach (var child in asset.Children)
            {
                CreateEntityForAsset(registry, project, entityGroup, child);
            }
        }
    }

    public static class UTinyAssetExporter
    {
        public static string GetAssetName(UTinyProject project, Object @object)
        {
            return GetAssetName(project.Module.Dereference(project.Registry), @object);
        }

        public static string GetAssetName(UTinyModule module, Object @object)
        {
            if (!@object)
            {
                return string.Empty;
            }

            var asset = module.EnumerateDependencies().Select(m => m.GetAsset(@object)).FirstOrDefault();

            if (!string.IsNullOrEmpty(asset?.Name))
            {
                return asset.Name;
            }

            return @object.name;
        }

        public static IList<UTinyExportInfo> Export(UTinyProject project, DirectoryInfo assetsFolder)
        {
            var module = project.Module.Dereference(project.Registry);
            return AssetIterator.EnumerateAssets(module).Select(asset => Export(project, assetsFolder.FullName, asset)).ToList();
        }

        private static UTinyExportInfo Export(UTinyProject project, string path, UTinyAssetInfo assetInfo)
        {
            var export = new UTinyExportInfo(assetInfo);
            var assetName = GetAssetName(project, assetInfo.Object);
            var isDebug = UTinyEditorApplication.EditorContext.Workspace.BuildConfiguration == UTinyBuildConfiguration.Debug;

            var texture = assetInfo.Object as Texture2D;
            if (texture != null)
            {
                var settings = UTinyUtility.GetAssetExportSettings(project, texture) as UTinyTextureSettings;
                TextureExporter.Export(path, assetName, texture, !isDebug, settings, export.Exported);
                return export;
            }

            var audioClip = assetInfo.Object as AudioClip;
            if (audioClip != null)
            {
                FileExporter.Export(path, assetName, audioClip, export.Exported);
                return export;
            }

            var font = assetInfo.Object as Font;
            if (font != null)
            {
                FontExporter.Export(path, assetName, font, export.Exported);
                return export;
            }

            // Export the object as is
            FileExporter.Export(path, assetName, assetInfo.Object, export.Exported);
            return export;
        }

        public static class FileExporter
        {
            public static void Export(string path, string name, Object @object, ICollection<FileInfo> output)
            {
                var assetPath = AssetDatabase.GetAssetPath(@object);
                var srcFile = new FileInfo(Path.Combine(Path.Combine(Application.dataPath, ".."), assetPath));
                var dstFile = new FileInfo(Path.Combine(path, name + Path.GetExtension(srcFile.Name)));
                srcFile.CopyTo(dstFile.FullName, true);
                output.Add(dstFile);
            }
        }

        public static class TextureExporter
        {
            public static void Export(string path, string name, Texture2D texture, bool forRelease, UTinyTextureSettings settings, List<FileInfo> output)
            {
                switch (settings.FormatType)
                {
                    case TextureFormatType.Source:
                        // Use the basic file exporter
                        FileExporter.Export(path, name, texture, output);
                        break;
                    case TextureFormatType.PNG:
                        ExportPng(path, name, texture, output);
                        break;
                    case TextureFormatType.JPG:
                        if (forRelease)
                            ExportJpgOptimized(path, name, texture, settings.JpgCompressionQuality, output);
                        else
                            ExportJpg(path, name, texture, settings.JpgCompressionQuality, output);
                        break;
                    case TextureFormatType.WebP:
                        ExportWebP(path, name, texture, settings.WebPCompressionQuality, output);
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }

            private static void ExportPng(string path, string name, Texture2D texture, ICollection<FileInfo> output)
            {
                var hasAlpha = ReallyHasAlpha(texture);
                var outputTexture = CopyTexture(texture, hasAlpha ? TextureFormat.RGBA32 : TextureFormat.RGB24);
                var bytes = outputTexture.EncodeToPNG();
                var dstFile = $"{Path.Combine(path, name)}.png";

                using (var stream = new FileStream(dstFile, FileMode.Create))
                {
                    stream.Write(bytes, 0, bytes.Length);
                }

                output.Add(new FileInfo(dstFile));
            }

            private static void ExportJpg(string path, string name, Texture2D texture, int quality, ICollection<FileInfo> output)
            {
                var outputColorTexture = CopyTexture(texture, TextureFormat.RGB24);
                var colorFilePath = $"{Path.Combine(path, name)}.jpg";

                using (var stream = new FileStream(colorFilePath, FileMode.Create))
                {
                    var bytes = outputColorTexture.EncodeToJPG(quality);
                    stream.Write(bytes, 0, bytes.Length);
                }

                output.Add(new FileInfo(colorFilePath));

                if (ReallyHasAlpha(texture))
                {
                    // @TODO Optimization by reusing the texture above
                    var outputAlphaTexture = CopyTexture(texture, TextureFormat.RGBA32);

                    var pixels = outputAlphaTexture.GetPixels32();

                    // broadcast alpha to color
                    for (var i = 0; i < pixels.Length; i++)
                    {
                        pixels[i].r = pixels[i].a;
                        pixels[i].g = pixels[i].a;
                        pixels[i].b = pixels[i].a;
                        pixels[i].a = 255;
                    }


                    outputAlphaTexture.SetPixels32(pixels);
                    outputAlphaTexture.Apply();
                    // kill alpha channel
                    outputAlphaTexture = CopyTexture(outputAlphaTexture, TextureFormat.RGB24);


                    var alphaFilePath = $"{Path.Combine(path, name)}_a.png";

                    using (var stream = new FileStream(alphaFilePath, FileMode.Create))
                    {
                        var bytes = outputAlphaTexture.EncodeToPNG();
                        stream.Write(bytes, 0, bytes.Length);
                    }

                    output.Add(new FileInfo(alphaFilePath));
                }
            }

            private static string ImageUtilsPath()
            {
                var path = UTinyBuildPipeline.GetToolDirectory("images");
#if UNITY_EDITOR_WIN
                path = Path.Combine(path, "win");
#else
		        path = Path.Combine(path, "osx");
#endif
                return new DirectoryInfo(path).FullName;
            }

            private static void ExportJpgOptimized(string path, string name, Texture2D texture, int quality, ICollection<FileInfo> output)
            {
                var colorFilePath = $"{Path.Combine(path, name)}.jpg";

                var outputTexture = CopyTexture(texture, TextureFormat.RGB24);
                //var tempPngInputPath = Path.Combine(Application.temporaryCachePath, "webp-input.png");
                var tempPngInputPath = $"{Path.Combine(path, name)}-input.png";
                File.WriteAllBytes(tempPngInputPath, outputTexture.EncodeToPNG());

                quality = Math.Max(0, Math.Min(100, quality));

                // this will build progressive jpegs by default; -baseline stops this.
                // progressive results in better compression
                UTinyBuildUtilities.RunInShell(
                    $"moz-cjpeg -quality {quality} -outfile \"{colorFilePath}\" \"{tempPngInputPath}\"",
                    new ShellProcessArgs()
                    {
                        ExtraPaths = ImageUtilsPath().AsEnumerable()
                    });

                File.Delete(tempPngInputPath);

                output.Add(new FileInfo(colorFilePath));

                if (ReallyHasAlpha(texture))
                {
                    // @TODO Optimization by reusing the texture above
                    var outputAlphaTexture = CopyTexture(texture, TextureFormat.RGBA32);

                    var pixels = outputAlphaTexture.GetPixels32();

                    // broadcast alpha to color
                    for (var i = 0; i < pixels.Length; i++)
                    {
                        pixels[i].r = pixels[i].a;
                        pixels[i].g = pixels[i].a;
                        pixels[i].b = pixels[i].a;
                        pixels[i].a = 255;
                    }

                    outputAlphaTexture.SetPixels32(pixels);
                    outputAlphaTexture.Apply();

                    //var pngCrushInputPath = Path.Combine(Application.temporaryCachePath, "alpha-input.png");
                    var pngCrushInputPath = $"{Path.Combine(path, name)}_a-input.png";
                    var alphaFilePath = $"{Path.Combine(path, name)}_a.png";

                    using (var stream = new FileStream(pngCrushInputPath, FileMode.Create))
                    {
                        var bytes = outputAlphaTexture.EncodeToPNG();
                        stream.Write(bytes, 0, bytes.Length);
                    }

                    // convert to 8-bit grayscale png
                    UTinyBuildUtilities.RunInShell(
                        $"pngcrush -s -c 0 \"{pngCrushInputPath}\" \"{alphaFilePath}\"",
                        new ShellProcessArgs()
                        {
                            ExtraPaths = ImageUtilsPath().AsEnumerable()
                        });

                    output.Add(new FileInfo(alphaFilePath));
                }
            }


            private static void ExportWebP(string path, string name, Texture2D texture, int quality, ICollection<FileInfo> output)
            {
                var hasAlpha = ReallyHasAlpha(texture);
                var outputTexture = CopyTexture(texture, hasAlpha ? TextureFormat.RGBA32 : TextureFormat.RGB24);

                var tempPngInputPath = Path.Combine(Application.temporaryCachePath, "webp-input.png");
                File.WriteAllBytes(tempPngInputPath, outputTexture.EncodeToPNG());

                quality = Math.Max(0, Math.Min(100, quality));

                var dstFile = new FileInfo(Path.Combine(path, name + ".webp"))
                    .FullName;

                UTinyBuildUtilities.RunInShell(
                    $"cwebp -quiet -q {quality} \"{tempPngInputPath}\" -o \"{dstFile}\"",
                    new ShellProcessArgs()
                    {
                        ExtraPaths = ImageUtilsPath().AsEnumerable()
                    });

                File.Delete(tempPngInputPath);

                output.Add(new FileInfo(dstFile));
            }

            public static bool ReallyHasAlpha(Texture2D texture)
            {
                bool hasAlpha = HasAlpha(texture.format);
                if (!hasAlpha)
                    return false;

                if (texture.format == TextureFormat.ARGB4444 ||
                    texture.format == TextureFormat.ARGB32 ||
                    texture.format == TextureFormat.RGBA32)
                {
                    var copy = CopyTexture(texture, TextureFormat.ARGB32);
                    Color32[] pix = copy.GetPixels32();
                    for (int i = 0; i < pix.Length; ++i)
                    {
                        if (pix[i].a != 255)
                        {
                            return true;
                        }
                    }

                    // image has alpha channel, but every alpha value is opaque
                    return false;
                }

                return true;
            }

            public static bool HasAlpha(TextureFormat format)
            {
                return format == TextureFormat.Alpha8 ||
                       format == TextureFormat.ARGB4444 ||
                       format == TextureFormat.ARGB32 ||
                       format == TextureFormat.RGBA32 ||
                       format == TextureFormat.DXT5 ||
                       format == TextureFormat.PVRTC_RGBA2 ||
                       format == TextureFormat.PVRTC_RGBA4 ||
                       format == TextureFormat.ETC2_RGBA8;
            }

            private static Texture2D CopyTexture(Texture texture, TextureFormat format)
            {
                // Create a temporary RenderTexture of the same size as the texture
                var tmp = RenderTexture.GetTemporary(
                    texture.width,
                    texture.height,
                    0,
                    RenderTextureFormat.Default,
                    RenderTextureReadWrite.sRGB);

                // Blit the pixels on texture to the RenderTexture
                Graphics.Blit(texture, tmp);

                // Backup the currently set RenderTexture
                var previous = RenderTexture.active;

                // Set the current RenderTexture to the temporary one we created
                RenderTexture.active = tmp;

                // Create a new readable Texture2D to copy the pixels to it
                var result = new Texture2D(texture.width, texture.height, format, false);

                // Copy the pixels from the RenderTexture to the new Texture
                result.ReadPixels(new Rect(0, 0, tmp.width, tmp.height), 0, 0);
                result.Apply();

                // Reset the active RenderTexture
                RenderTexture.active = previous;

                // Release the temporary RenderTexture
                RenderTexture.ReleaseTemporary(tmp);

                return result;
            }
        }

        private static class FontExporter
        {
            /// <summary>
            /// @HACK This should be platform specific
            /// </summary>
            private static readonly List<string> s_WebFonts = new List<string>
            {
                "Arial",
            };

            private static bool IncludedByTargetPlatform(Font font)
            {
                return s_WebFonts.Intersect(font.fontNames).Any();
            }

            public static void Export(string path, string name, Font font, ICollection<FileInfo> output)
            {
                if (IncludedByTargetPlatform(font))
                {
                    return;
                }
                
                FileExporter.Export(path, name, font, output);
            }
        }
    }
}
#endif // NET_4_6
