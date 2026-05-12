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
        /// Frame 在大圖中的 X 座標（像素）。
        /// </summary>
        public int x;

        /// <summary>
        /// Frame 在大圖中的 Y 座標（像素，Y=0 在左上角）。
        /// </summary>
        public int y;

        /// <summary>
        /// Frame 的寬度（像素）。
        /// </summary>
        public int w;

        /// <summary>
        /// Frame 的高度（像素）。
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

            try
            {
                string jsonContent = File.ReadAllText(fullPath, System.Text.Encoding.UTF8);
                return ParseJson(jsonContent);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SpriteUnpacker] Failed to read JSON: {ex}");
                return null;
            }
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

        /// <summary>
        /// 將 Assets 相對路徑轉換為系統完整路徑。
        /// </summary>
        /// <param name="path">Assets 相對路徑或完整路徑。</param>
        /// <returns>系統完整路徑。</returns>
        private static string GetFullPath(string path)
        {
            // 已是完整路徑，直接回傳
            if (Path.IsPathRooted(path))
                return path;

            string dataPath = Application.dataPath;
            // 移除 "Assets/" 前綴，與 Application.dataPath 拼接
            if (path.StartsWith("Assets/", System.StringComparison.OrdinalIgnoreCase))
                return Path.Combine(dataPath, path.Substring("Assets/".Length));

            return Path.Combine(dataPath, path);
        }

        /// <summary>
        /// 解析 JSON 字串內容為 TexturePackerAtlas 物件。
        /// </summary>
        /// <param name="jsonContent">JSON 字串內容。</param>
        /// <returns>TexturePackerAtlas 物件；若解析失敗則傳回 null。</returns>
        private static TexturePackerAtlas? ParseJson(string jsonContent)
        {
            try
            {
                TexturePackerAtlas atlas = new TexturePackerAtlas();
                List<SpriteFrame> spriteFrames = new List<SpriteFrame>();

                // 定位 "frames" 區塊的起始位置
                int framesStart = jsonContent.IndexOf("\"frames\":", StringComparison.Ordinal);
                if (framesStart < 0)
                {
                    atlas.sprites = Array.Empty<SpriteFrame>();
                    return atlas;
                }

                // 找到 frames 物件的起始大括號
                int framesObjStart = jsonContent.IndexOf("{", framesStart + 8);
                int pos = framesObjStart + 1;

                // 手動解析每個 frame 條目（不使用 Regex 或 JsonUtility，避免支援問題）
                while (pos < jsonContent.Length)
                {
                    // 尋找 PNG 檔名作為 frame 的 key
                    int keyStart = jsonContent.IndexOf("\"", pos);
                    if (keyStart < 0)
                        break;

                    int keyEnd = jsonContent.IndexOf("\":", keyStart, StringComparison.Ordinal);
                    if (keyEnd < 0 || keyEnd - keyStart > 200)
                    {
                        pos++;
                        continue;
                    }

                    string key = jsonContent.Substring(keyStart + 1, keyEnd - keyStart - 1);
                    // 確認是 PNG 檔案
                    if (!key.EndsWith(".png", System.StringComparison.OrdinalIgnoreCase))
                    {
                        pos = keyEnd + 1;
                        continue;
                    }

                    // 找到此 frame 的 frame 物件
                    int frameObjStart = jsonContent.IndexOf("{", keyEnd, StringComparison.Ordinal);
                    int frameObjEnd = FindBraceEnd(jsonContent, frameObjStart);
                    string frameObj = jsonContent.Substring(frameObjStart, frameObjEnd - frameObjStart + 1);

                    // 解析 frame 物件中的 x, y, w, h
                    SpriteFrame frame = ParseFrame(key, frameObj);
                    if (frame.w > 0 && frame.h > 0)
                    {
                        spriteFrames.Add(frame);
                    }

                    pos = frameObjEnd + 1;
                }

                atlas.sprites = spriteFrames.ToArray();
                Debug.Log($"[SpriteUnpacker] Parsed {atlas.sprites.Length} sprites");
                return atlas;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SpriteUnpacker] Failed to parse JSON: {ex}");
                return null;
            }
        }

        /// <summary>
        /// 解析單一 frame 物件，提取 x, y, w, h 欄位。
        /// </summary>
        /// <param name="name">Frame 的名稱（PNG 檔名）。</param>
        /// <param name="frameObj">frame 物件的 JSON 字串。</param>
        /// <returns>SpriteFrame 結構。</returns>
        private static SpriteFrame ParseFrame(string name, string frameObj)
        {
            SpriteFrame frame = new SpriteFrame
            {
                name = name,
                x = GetInt(frameObj, "\"x\":"),
                y = GetInt(frameObj, "\"y\":"),
                w = GetInt(frameObj, "\"w\":"),
                h = GetInt(frameObj, "\"h\":")
            };
            return frame;
        }

        /// <summary>
        /// 從 JSON 字串中取出指定 key 的 int 值。
        /// </summary>
        /// <param name="json">JSON 字串。</param>
        /// <param name="key">要查找的 key，例如 "\"x\":".</param>
        /// <returns>解析出的 int 值；若找不到則回傳 0。</returns>
        private static int GetInt(string json, string key)
        {
            int keyIndex = json.IndexOf(key, StringComparison.Ordinal);
            if (keyIndex < 0)
                return 0;

            int valueStart = keyIndex + key.Length;
            int pos = valueStart;

            // 跳過空白字元
            while (pos < json.Length && (json[pos] == ' ' || json[pos] == '\t' || json[pos] == '\n' || json[pos] == '\r'))
                pos++;

            int valueEnd = pos;
            // 讀取連續的數字
            while (valueEnd < json.Length && char.IsDigit(json[valueEnd]))
                valueEnd++;

            if (valueEnd > pos && int.TryParse(json.Substring(pos, valueEnd - pos), out int result))
                return result;

            return 0;
        }

        /// <summary>
        /// 找到配對的大括號結束位置（考慮字串內的括號）。
        /// </summary>
        /// <param name="json">JSON 字串。</param>
        /// <param name="startBrace">起始大括號的位置。</param>
        /// <returns>結束大括號的位置。</returns>
        private static int FindBraceEnd(string json, int startBrace)
        {
            int braceCount = 1;
            bool inString = false;
            int i = startBrace + 1;

            while (i < json.Length && braceCount > 0)
            {
                char c = json[i];
                char prev = i > 0 ? json[i - 1] : '\0';

                // 處理字串逸出
                if (c == '"' && prev != '\\')
                {
                    inString = !inString;
                }
                else if (!inString)
                {
                    if (c == '{')
                        braceCount++;
                    else if (c == '}')
                        braceCount--;
                }
                i++;
            }

            return i - 1;
        }
    }
}
#endif