#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;

namespace SpriteUnpacker
{
    public static class SpriteUnpackerCore
    {
        private static readonly Dictionary<Texture2D, bool> _readableCache = new Dictionary<Texture2D, bool>();

        public static Sprite[] GetAllSpritesFromTexture(string path)
        {
            if (string.IsNullOrEmpty(path))
                return Array.Empty<Sprite>();

            UnityEngine.Object[] assets = AssetDatabase.LoadAllAssetsAtPath(path);
            List<Sprite> sprites = new List<Sprite>();

            foreach (UnityEngine.Object asset in assets)
            {
                if (asset is Sprite sprite)
                {
                    sprites.Add(sprite);
                }
            }

            return sprites.ToArray();
        }

        public static Texture2D ExtractSpritePixels(Sprite sprite)
        {
            if (sprite == null || sprite.texture == null)
                return null;

            Texture2D sourceTexture = sprite.texture;
            Rect textureRect = sprite.textureRect;

            int x = Mathf.RoundToInt(textureRect.x);
            int y = Mathf.RoundToInt(textureRect.y);
            int width = Mathf.RoundToInt(textureRect.width);
            int height = Mathf.RoundToInt(textureRect.height);

            Color32[] pixels = sourceTexture.GetPixels32(x, y, width, height);

            Texture2D result = new Texture2D(width, height, TextureFormat.RGBA32, false);
            result.SetPixels32(pixels);
            result.Apply();

            return result;
        }

        public static void SetReadableIfNeeded(Texture2D tex)
        {
            if (tex == null)
                return;

            if (_readableCache.ContainsKey(tex))
                return;

            string assetPath = AssetDatabase.GetAssetPath(tex);
            if (string.IsNullOrEmpty(assetPath))
                return;

            TextureImporter importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            if (importer == null)
                return;

            bool wasReadable = importer.isReadable;
            _readableCache[tex] = wasReadable;

            if (!wasReadable)
            {
                importer.isReadable = true;
                importer.SaveAndImport();
            }
        }

        public static void RestoreReadable(Texture2D tex)
        {
            if (tex == null)
                return;

            if (!_readableCache.TryGetValue(tex, out bool wasReadable))
                return;

            string assetPath = AssetDatabase.GetAssetPath(tex);
            if (string.IsNullOrEmpty(assetPath))
                return;

            TextureImporter importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            if (importer == null)
                return;

            if (importer.isReadable != wasReadable)
            {
                importer.isReadable = wasReadable;
                importer.SaveAndImport();
            }

            _readableCache.Remove(tex);
        }
    }
}
#endif