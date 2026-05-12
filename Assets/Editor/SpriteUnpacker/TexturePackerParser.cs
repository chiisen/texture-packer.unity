#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace SpriteUnpacker
{
    /// <summary>
    /// 代表單一 Sprite 的 frame 資料。
    /// </summary>
    [Serializable]
    public struct SpriteFrame
    {
        /// <summary>
        /// Sprite 的名稱（包含副檔名），例如 "SpecialForces_atk_1_1_1.png"。
        /// </summary>
        public string name;

        /// <summary>
        /// Frame 在大圖中的 X 座標。
        /// </summary>
        public int x;

        /// <summary>
        /// Frame 在大圖中的 Y 座標。
        /// </summary>
        public int y;

        /// <summary>
        /// Frame 的寬度。
        /// </summary>
        public int w;

        /// <summary>
        /// Frame 的高度。
        /// </summary>
        public int h;
    }

    /// <summary>
    /// 代表 TexturePacker JSON 格式的整張 Atlas 資料。
    /// </summary>
    [Serializable]
    public struct TexturePackerAtlas
    {
        /// <summary>
        /// Atlas 圖片檔案名稱，例如 "COC_CHR_SpecialForces.png"。
        /// </summary>
        public string imageName;

        /// <summary>
        /// Atlas 的寬度（pixels）。
        /// </summary>
        public int atlasWidth;

        /// <summary>
        /// Atlas 的高度（pixels）。
        /// </summary>
        public int atlasHeight;

        /// <summary>
        /// 所有 SpriteFrame 的陣列。
        /// </summary>
        public SpriteFrame[] sprites;
    }

    /// <summary>
    /// TexturePacker JSON 格式解析器。
    /// 用於解析 TexturePacker 匯出的 Atlas JSON 檔案。
    /// </summary>
    public static class TexturePackerParser
    {
        /// <summary>
        /// 解析 TexturePacker JSON 檔案並回傳 Atlas 資料。
        /// </summary>
        /// <param name="jsonPath">JSON 檔案的完整路徑或相對於 Assets 的路徑。</param>
        /// <returns>TexturePackerAtlas 物件；若檔案不存在或格式不符則傳回 null。</returns>
        public static TexturePackerAtlas? Parse(string jsonPath)
        {
            if (string.IsNullOrEmpty(jsonPath))
                return null;

            string fullPath = GetFullPath(jsonPath);

            if (!File.Exists(fullPath))
                return null;

            string jsonContent;
            try
            {
                jsonContent = File.ReadAllText(fullPath, System.Text.Encoding.UTF8);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SpriteUnpacker] Failed to read JSON: {ex}");
                return null;
            }

            if (string.IsNullOrWhiteSpace(jsonContent))
                return null;

            TexturePackerAtlas? result = ParseJson(jsonContent);
            return result;
        }

        /// <summary>
        /// 快速取得 JSON 檔案中所有 SpriteFrame 的陣列。
        /// </summary>
        /// <param name="jsonPath">JSON 檔案的路徑。</param>
        /// <returns>SpriteFrame 陣列；若解析失敗則傳回 null。</returns>
        public static SpriteFrame[] GetSpriteFrames(string jsonPath)
        {
            TexturePackerAtlas? atlas = Parse(jsonPath);
            return atlas?.sprites;
        }

        private static string GetFullPath(string path)
        {
            if (Path.IsPathRooted(path))
                return path;

            return Path.Combine(Application.dataPath, path);
        }

        private static TexturePackerAtlas? ParseJson(string jsonContent)
        {
            try
            {
                var jsonDoc = JsonUtility.FromJson<TexturePackerJson>(jsonContent);
                if (jsonDoc == null || jsonDoc.meta == null)
                    return null;

                TexturePackerAtlas atlas = new TexturePackerAtlas
                {
                    imageName = jsonDoc.meta.image ?? string.Empty,
                    atlasWidth = jsonDoc.meta.size?.w ?? 0,
                    atlasHeight = jsonDoc.meta.size?.h ?? 0,
                    sprites = null
                };

                if (jsonDoc.frames != null)
                {
                    List<SpriteFrame> spriteFrames = new List<SpriteFrame>();
                    foreach (var kvp in jsonDoc.frames)
                    {
                        SpriteFrame frame = new SpriteFrame
                        {
                            name = kvp.Key,
                            x = kvp.Value.frame?.x ?? 0,
                            y = kvp.Value.frame?.y ?? 0,
                            w = kvp.Value.frame?.w ?? 0,
                            h = kvp.Value.frame?.h ?? 0
                        };
                        spriteFrames.Add(frame);
                    }
                    atlas.sprites = spriteFrames.ToArray();
                }
                else
                {
                    atlas.sprites = Array.Empty<SpriteFrame>();
                }

                return atlas;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SpriteUnpacker] Failed to parse JSON: {ex}");
                return null;
            }
        }

        [Serializable]
        private class TexturePackerJson
        {
            public Dictionary<string, FrameData> frames;
            public MetaData meta;
        }

        [Serializable]
        private class MetaData
        {
            public string app;
            public string image;
            public string format;
            public SizeData size;
        }

        [Serializable]
        private class SizeData
        {
            public int w;
            public int h;
        }

        [Serializable]
        private class FrameData
        {
            public FrameRect frame;
            public bool rotated;
            public bool trimmed;
            public FrameRect spriteSourceSize;
            public SizeData sourceSize;
        }

        [Serializable]
        private class FrameRect
        {
            public int x;
            public int y;
            public int w;
            public int h;
        }
    }
}
#endif