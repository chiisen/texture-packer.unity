#if UNITY_EDITOR
using System.IO;
using UnityEngine;

namespace SpriteUnpacker
{
    public static class SpriteUnpackerExporter
    {
        /// <summary>
        /// Exports a Texture2D to a PNG file.
        /// </summary>
        /// <param name="tex">The texture to export.</param>
        /// <param name="outputDir">The output directory path.</param>
        /// <param name="spriteName">The file name without extension.</param>
        /// <returns>True if the export succeeded; otherwise, false.</returns>
        /// <exception cref="IOException">Thrown when an I/O error occurs while writing the file.</exception>
        /// <exception cref="UnauthorizedAccessException">Thrown when the process does not have access to write to the file.</exception>
        public static bool ExportTextureToPng(Texture2D tex, string outputDir, string spriteName)
        {
            if (tex == null || string.IsNullOrEmpty(outputDir) || string.IsNullOrEmpty(spriteName))
                return false;

            string filePath = Path.Combine(outputDir, $"{spriteName}.png");

            byte[] pngData = tex.EncodeToPNG();
            if (pngData == null)
                return false;

            try
            {
                if (!Directory.Exists(outputDir))
                    Directory.CreateDirectory(outputDir);

                File.WriteAllBytes(filePath, pngData);
            }
            catch (IOException ex)
            {
                Debug.LogError($"[SpriteUnpacker] IO error exporting texture: {ex.Message}");
                return false;
            }
            catch (UnauthorizedAccessException ex)
            {
                Debug.LogError($"[SpriteUnpacker] Access denied exporting texture: {ex.Message}");
                return false;
            }
            return true;
        }

        /// <summary>
        /// Exports a Sprite to a PNG file by extracting its pixel data.
        /// </summary>
        /// <param name="sprite">The sprite to export.</param>
        /// <param name="outputDir">The output directory path.</param>
        /// <returns>True if the export succeeded; otherwise, false.</returns>
        /// <exception cref="IOException">Thrown when an I/O error occurs while writing the file.</exception>
        /// <exception cref="UnauthorizedAccessException">Thrown when the process does not have access to write to the file.</exception>
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
