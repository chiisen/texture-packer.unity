#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace SpriteUnpacker
{
    /// <summary>
    /// TexturePacker JSON frame 格式。
    /// </summary>
    [Serializable]
    public struct TpFrame
    {
        public string filename;
        public TpFrameRect frame;
        public bool rotated;
        public bool trimmed;
        public TpSpriteSourceSize spriteSourceSize;
        public TpSize size;
        public Vector2 pivot;
    }

    [Serializable]
    public struct TpFrameRect
    {
        public int x;
        public int y;
        public int w;
        public int h;
    }

    [Serializable]
    public struct TpSpriteSourceSize
    {
        public int x;
        public int y;
        public int w;
        public int h;
    }

    [Serializable]
    public struct TpSize
    {
        public int w;
        public int h;
    }

    [Serializable]
    public struct TpMeta
    {
        public string app;
        public string version;
        public string image;
        public string format;
        public TpSize size;
        public string scale;
        public string smartupdate;
    }

    [Serializable]
    public struct TexturePackerJson
    {
        public List<TpFrame> frames;
        public TpMeta meta;
    }

    /// <summary>
    /// Sprite 打包核心邏輯。
    /// 將多張個別 PNG 打包成一張 Atlas PNG，並輸出 TexturePacker JSON 格式供 SpriteUnpacker 使用。
    /// </summary>
    public static class SpritePackerCore
    {
        public const int DEFAULT_MAX_ATLAS_SIZE = 4096;

        /// <summary>
        /// 將多張個別 PNG 打包成一張 Atlas PNG，並匯出 TexturePacker JSON 格式。
        /// </summary>
        /// <param name="texturePaths">要打包的 PNG 檔案路徑陣列。</param>
        /// <param name="outputFolder">輸出資料夾。</param>
        /// <param name="atlasName">輸出 Atlas 檔名（不含副檔名）。</param>
        /// <param name="maxAtlasSize">最大 Atlas 尺寸（預設 4096）。</param>
        /// <returns>是否成功。</returns>
        public static bool PackAndExport(string[] texturePaths, string outputFolder, string atlasName, int maxAtlasSize = DEFAULT_MAX_ATLAS_SIZE)
        {
            if (texturePaths == null || texturePaths.Length == 0)
            {
                Debug.LogError("[SpritePacker] No textures to pack.");
                return false;
            }

            if (string.IsNullOrEmpty(outputFolder))
            {
                Debug.LogError("[SpritePacker] Output folder is empty.");
                return false;
            }

            if (!Directory.Exists(outputFolder))
            {
                Directory.CreateDirectory(outputFolder);
                Debug.Log($"[SpritePacker] Created output folder: {outputFolder}");
            }

            List<Texture2D> textures = new List<Texture2D>();
            List<string> textureNames = new List<string>();

            foreach (string path in texturePaths)
            {
                if (string.IsNullOrEmpty(path) || !File.Exists(path))
                {
                    Debug.LogWarning($"[SpritePacker] Skipping invalid path: {path}");
                    continue;
                }

                Texture2D tex = LoadTexture(path);
                if (tex == null)
                {
                    Debug.LogWarning($"[SpritePacker] Failed to load texture: {path}");
                    continue;
                }

                textures.Add(tex);
                textureNames.Add(Path.GetFileName(path));
            }

            if (textures.Count == 0)
            {
                Debug.LogError("[SpritePacker] No valid textures to pack.");
                return false;
            }

            Debug.Log($"[SpritePacker] Packing {textures.Count} textures into atlas...");

            Texture2D atlas = new Texture2D(0, 0);
            Rect[] rects = atlas.PackTextures(textures.ToArray(), 0, maxAtlasSize);

            int atlasWidth = atlas.width;
            int atlasHeight = atlas.height;
            Debug.Log($"[SpritePacker] Atlas created: {atlasWidth}x{atlasHeight}");

            TexturePackerJson json = CreateTexturePackerJson(textureNames, rects, atlasWidth, atlasHeight, atlasName);

            string atlasPngPath = Path.Combine(outputFolder, atlasName + ".png");
            string jsonPath = Path.Combine(outputFolder, atlasName + ".txt");

            byte[] pngBytes = atlas.EncodeToPNG();
            File.WriteAllBytes(atlasPngPath, pngBytes);
            Debug.Log($"[SpritePacker] Atlas PNG saved: {atlasPngPath}");

            string jsonContent = CreateJsonString(json);
            File.WriteAllText(jsonPath, jsonContent);
            Debug.Log($"[SpritePacker] TexturePacker JSON saved: {jsonPath}");

            UnityEngine.Object.DestroyImmediate(atlas);
            foreach (Texture2D tex in textures)
            {
                if (tex != null)
                    UnityEngine.Object.DestroyImmediate(tex);
            }

            AssetDatabase.Refresh();

            Debug.Log($"[SpritePacker] Done! Packed {textureNames.Count} textures into {atlasName}.png");
            return true;
        }

        /// <summary>
        /// 載入 PNG 檔案為 Texture2D。
        /// </summary>
        private static Texture2D LoadTexture(string path)
        {
            if (!File.Exists(path))
                return null;

            byte[] bytes = File.ReadAllBytes(path);
            Texture2D tex = new Texture2D(0, 0, TextureFormat.RGBA32, false);
            if (!tex.LoadImage(bytes))
            {
                UnityEngine.Object.DestroyImmediate(tex);
                return null;
            }

            return tex;
        }

        /// <summary>
        /// 根據 PackTextures 返回的 rects（normalized 0-1）建立 TexturePacker JSON 結構。
        /// </summary>
        private static TexturePackerJson CreateTexturePackerJson(List<string> names, Rect[] rects, int atlasWidth, int atlasHeight, string atlasName)
        {
            TexturePackerJson json = new TexturePackerJson
            {
                frames = new List<TpFrame>(),
                meta = new TpMeta
                {
                    app = "Unity SpritePacker",
                    version = "1.0",
                    image = atlasName + ".png",
                    format = "RGBA8888",
                    size = new TpSize { w = atlasWidth, h = atlasHeight },
                    scale = "1",
                    smartupdate = ""
                }
            };

            for (int i = 0; i < names.Count && i < rects.Length; i++)
            {
                int x = Mathf.RoundToInt(rects[i].x * atlasWidth);
                int y = Mathf.RoundToInt(rects[i].y * atlasHeight);
                int w = Mathf.RoundToInt(rects[i].width * atlasWidth);
                int h = Mathf.RoundToInt(rects[i].height * atlasHeight);

                TpFrame frame = new TpFrame
                {
                    filename = names[i],
                    frame = new TpFrameRect { x = x, y = y, w = w, h = h },
                    rotated = false,
                    trimmed = false,
                    spriteSourceSize = new TpSpriteSourceSize { x = 0, y = 0, w = w, h = h },
                    size = new TpSize { w = w, h = h },
                    pivot = new Vector2(0.5f, 0.5f)
                };

                json.frames.Add(frame);
            }

            return json;
        }

        /// <summary>
        /// 將 TexturePackerJson 序列化成 JSON 字串。
        /// </summary>
        private static string CreateJsonString(TexturePackerJson json)
        {
            string jsonString = JsonUtility.ToJson(json, true);
            return jsonString;
        }
    }
}
#endif