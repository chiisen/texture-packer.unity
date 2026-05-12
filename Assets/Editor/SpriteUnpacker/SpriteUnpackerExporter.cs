#if UNITY_EDITOR
using System;
using System.IO;
using UnityEngine;

namespace SpriteUnpacker
{
    /// <summary>
    /// PNG 匯出工具類別。
    /// 負責將 Texture2D 編碼為 PNG 格式並寫入磁碟。
    /// </summary>
    public static class SpriteUnpackerExporter
    {
        /// <summary>
        /// 將 Texture2D 匯出為 PNG 檔案。
        /// </summary>
        /// <param name="tex">要匯出的紋理物件。</param>
        /// <param name="outputDir">輸出資料夾的路徑。</param>
        /// <param name="spriteName">輸出檔案的主檔名（不含副檔名）。</param>
        /// <returns>若匯出成功則傳回 true，否則傳回 false。</returns>
        /// <exception cref="IOException">寫入檔案時發生 I/O 錯誤。</exception>
        /// <exception cref="UnauthorizedAccessException">無寫入檔案或資料夾的權限。</exception>
        /// <remarks>
        /// 輸出檔案的路徑為：outputDir/spriteName.png
        /// 若目標資料夾不存在，會自動建立。
        /// 若檔案已存在，會直接覆蓋。
        /// </remarks>
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
                Debug.LogError($"[SpriteUnpacker] IO error exporting texture: {ex}");
                return false;
            }
            catch (UnauthorizedAccessException ex)
            {
                Debug.LogError($"[SpriteUnpacker] Access denied exporting texture: {ex}");
                return false;
            }
            return true;
        }

        /// <summary>
        /// 將 Sprite 匯出為獨立的 PNG 檔案。
        /// </summary>
        /// <param name="sprite">要匯出的 Sprite 物件。</param>
        /// <param name="outputDir">輸出資料夾的路徑。</param>
        /// <returns>若匯出成功則傳回 true，否則傳回 false。</returns>
        /// <exception cref="IOException">寫入檔案時發生 I/O 錯誤。</exception>
        /// <exception cref="UnauthorizedAccessException">無寫入檔案或資料夾的權限。</exception>
        /// <remarks>
        /// 此方法會自動呼叫 SpriteUnpackerCore.ExtractSpritePixels 取得像素資料，
        /// 然後呼叫 ExportTextureToPng 寫入磁碟。處理完畢後會釋放暫存的 Texture2D 資源。
        /// 輸出檔案的名稱會與 Unity 內的 Sprite 名稱一致。
        /// </remarks>
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