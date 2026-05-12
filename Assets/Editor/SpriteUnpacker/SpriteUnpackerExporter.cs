#if UNITY_EDITOR
using System.IO;
using UnityEngine;

namespace SpriteUnpacker
{
    public static class SpriteUnpackerExporter
    {
        public static bool ExportTextureToPng(Texture2D tex, string outputDir, string spriteName)
        {
            if (tex == null || string.IsNullOrEmpty(outputDir) || string.IsNullOrEmpty(spriteName))
                return false;

            string filePath = Path.Combine(outputDir, $"{spriteName}.png");

            byte[] pngData = tex.EncodeToPNG();
            if (pngData == null)
                return false;

            if (!Directory.Exists(outputDir))
                Directory.CreateDirectory(outputDir);

            File.WriteAllBytes(filePath, pngData);
            return true;
        }

        public static bool ExportSprite(Sprite sprite, string outputDir)
        {
            if (sprite == null || string.IsNullOrEmpty(outputDir))
                return false;

            Texture2D texture = SpriteUnpackerCore.ExtractSpritePixels(sprite);
            if (texture == null)
                return false;

            bool success = ExportTextureToPng(texture, outputDir, sprite.name);
            UnityEngine.Object.DestroyImmediate(texture);
            return success;
        }
    }
}
#endif
