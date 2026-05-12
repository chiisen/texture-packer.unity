#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEditor;

namespace SpriteUnpacker
{
    /// <summary>
    /// Sprite 拆解核心邏輯提供者。
    /// 負責讀取 Sprite、提取像素資料、以及管理紋理的可讀取屬性。
    /// </summary>
    public static class SpriteUnpackerCore
    {
        /// <summary>
        /// 快取資料夾：用於記錄每個 Texture2D 原始的 isReadable 設定，
        /// 以便在處理完畢後還原其屬性。
        /// </summary>
        private static readonly Dictionary<Texture2D, bool> _readableCache = new Dictionary<Texture2D, bool>();

        /// <summary>
        /// 從指定路徑的紋理檔案中載入所有 Sprite 子資源。
        /// </summary>
        /// <param name="path">紋理檔案在 AssetDatabase 中的路徑（相對於專案根目錄）。</param>
        /// <returns>所有 Sprite 物件的陣列；若無 Sprite 則傳回空陣列。</returns>
        /// <remarks>
        /// 此方法使用 AssetDatabase.LoadAllAssetsAtPath，可正確載入位於任意位置的圖片，
        /// 不受限於 Resources 資料夾的約束。
        /// </remarks>
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

        /// <summary>
        /// 從指定的 Sprite 提取像素資料，並建立新的 Texture2D。
        /// 新的紋理會設定 AlphaIsTransparency 以確保透明度正確保留。
        /// </summary>
        /// <param name="sprite">要提取像素的 Sprite 物件。</param>
        /// <returns>包含該 Sprite 像素資料的新 Texture2D；若提取失敗則傳回 null。</returns>
        /// <remarks>
        /// 提取時會根據 Sprite.textureRect 取得其在原始大圖中的坐標與尺寸，
        /// 然後使用 GetPixels32 取出對應範圍的像素資料。
        /// </remarks>
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

            // 取出指定範圍的像素資料
            Color32[] pixels = sourceTexture.GetPixels32();

            // 建立新的 Texture2D 並填入像素
            Texture2D result = new Texture2D(width, height, TextureFormat.RGBA32, false);
            result.SetPixels32(pixels);
            result.Apply();
            // 設定透明度旗標，確保輸出 PNG 時正確保留透明通道
            result.alphaIsTransparency = true;

            return result;
        }

        /// <summary>
        /// 若指定的紋理尚未開啟 Read/Write Enabled，則自動開啟並快取原始設定。
        /// </summary>
        /// <param name="tex">要處理的 Texture2D 物件。</param>
        /// <remarks>
        /// 此方法會檢查紋理的 isReadable 屬性，若為 false 則設為 true 並寫入 AssetDatabase。
        /// 原始設定會被儲存於 _readableCache，以便後續還原。
        /// </remarks>
        public static void SetReadableIfNeeded(Texture2D tex)
        {
            if (tex == null)
                return;

            // 已被快取過的紋理不重複處理
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
                AssetDatabase.ImportAsset(assetPath);
            }
        }

        /// <summary>
        /// 還原紋理的原始 isReadable 設定（若先前有異動）。
        /// </summary>
        /// <param name="tex">要還原設定的 Texture2D 物件。</param>
        /// <remarks>
        /// 從 _readableCache 中取出該紋理原始的 isReadable 設定，
        /// 若目前設定與原始設定不同，則寫回 AssetDatabase 並移除快取紀錄。
        /// </remarks>
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
                AssetDatabase.ImportAsset(assetPath);
            }

            _readableCache.Remove(tex);
        }

        /// <summary>
        /// 根據 SpriteFrame 的 x, y, w, h 從 atlasTexture 裁切像素，建立新的 Texture2D。
        /// 新的紋理會設定 AlphaIsTransparency 以確保透明度正確保留。
        /// </summary>
        /// <param name="atlasTexture">Atlas 大圖。</param>
        /// <param name="frame">SpriteFrame 資料。</param>
        /// <returns>裁切後的新 Texture2D；若輸入無效則傳回 null。</returns>
        public static Texture2D ExtractFromTexturePackJson(Texture2D atlasTexture, SpriteFrame frame)
        {
            if (atlasTexture == null || frame.w <= 0 || frame.h <= 0)
                return null;

            int x = frame.x;
            int y = frame.y;
            int width = frame.w;
            int height = frame.h;

            if (x + width > atlasTexture.width || y + height > atlasTexture.height)
            {
                Debug.LogError($"[SpriteUnpacker] Frame out of bounds: {frame.name}");
                return null;
            }

            // TexturePacker Y軸原點在左上，Unity GetPixels32 原點在左下
            // 需要轉換：unityY = atlasHeight - frame.y - frame.h
            int unityY = atlasTexture.height - y - height;

            Color32[] allPixels = atlasTexture.GetPixels32();
            Color32[] croppedPixels = new Color32[width * height];

            for (int py = 0; py < height; py++)
            {
                for (int px = 0; px < width; px++)
                {
                    int srcIndex = (unityY + py) * atlasTexture.width + (x + px);
                    int dstIndex = py * width + px;
                    croppedPixels[dstIndex] = allPixels[srcIndex];
                }
            }

            Texture2D result = new Texture2D(width, height, TextureFormat.RGBA32, false);
            result.SetPixels32(croppedPixels);
            result.Apply();
            result.alphaIsTransparency = true;

            return result;
        }

        /// <summary>
        /// 根據 atlasPath（PNG 檔案路徑），推算同名的 .txt (TexturePacker JSON) 路徑。
        /// </summary>
        /// <param name="atlasPath">Atlas 圖片檔案路徑。</param>
        /// <returns>對應的 .txt 檔案路徑。</returns>
        public static string GetAtlasJsonPath(string atlasPath)
        {
            if (string.IsNullOrEmpty(atlasPath))
                return string.Empty;

            return Path.ChangeExtension(atlasPath, ".txt");
        }

        /// <summary>
        /// 檢查同名 .txt 檔案是否存在。
        /// </summary>
        /// <param name="atlasPath">Atlas 圖片檔案路徑。</param>
        /// <returns>若同名 .txt 檔案存在則傳回 true。</returns>
        public static bool HasTexturePackJson(string atlasPath)
        {
            if (string.IsNullOrEmpty(atlasPath))
                return false;

            string jsonPath = GetAtlasJsonPath(atlasPath);
            string fullPath = jsonPath;

            if (!Path.IsPathRooted(fullPath))
            {
                string dataPath = Application.dataPath;
                if (fullPath.StartsWith("Assets/", System.StringComparison.OrdinalIgnoreCase))
                    fullPath = Path.Combine(dataPath, fullPath.Substring("Assets/".Length));
                else
                    fullPath = Path.Combine(dataPath, fullPath);
            }

            bool exists = File.Exists(fullPath);
            Debug.Log($"[SpriteUnpacker] HasTexturePackJson: {atlasPath} -> {fullPath} = {exists}");
            return exists;
        }
    }
}
#endif