#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;

namespace SpriteUnpacker
{
    public class SpriteUnpackerWindow : EditorWindow
    {
        [MenuItem("Window/Sprite Tool/Unpack Sprite to PNGs")]
        public static void ShowWindow()
        {
            var window = GetWindow<SpriteUnpackerWindow>("Sprite Unpacker");
            window.minSize = new Vector2(400, 200);
        }

        private void OnEnable()
        {
            autoRepaintOnSceneChange = true;
        }

        private void OnGUI()
        {
            GUILayout.Label("Sprite Unpacker", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            EditorGUILayout.HelpBox(
                "This tool extracts individual sprites from a Sprite Atlas (.png) and exports them as separate PNG files.",
                MessageType.Info);

            EditorGUILayout.Space();

            if (GUILayout.Button("Unpack Sprites", GUILayout.Height(30)))
            {
                UnpackSprites();
            }
        }

        private void UnpackSprites()
        {
            string[] selectedFiles = EditorUtility.OpenFilePanelWithFilters(
                "Select Sprite Atlas PNG",
                "",
                new string[] { "PNG Images", "png", "All Files", "*" });

            if (selectedFiles == null || selectedFiles.Length == 0)
                return;

            string outputFolder = EditorUtility.OpenFolderPanel(
                "Select Output Folder",
                "",
                "");

            if (string.IsNullOrEmpty(outputFolder))
                return;

            int totalFiles = selectedFiles.Length;
            int processedFiles = 0;

            foreach (string filePath in selectedFiles)
            {
                processedFiles++;
                float progress = (float)processedFiles / totalFiles;
                int percent = Mathf.RoundToInt(progress * 100);

                EditorUtility.DisplayProgressBar(
                    $"Processing Sprites ({processedFiles}/{totalFiles})",
                    $"{Path.GetFileName(filePath)} - {percent}%",
                    progress);

                try
                {
                    ProcessSpriteAtlas(filePath, outputFolder);
                }
                catch (System.Exception ex)
                {
                    Debug.LogError($"[SpriteUnpacker] Error processing {filePath}: {ex.Message}");
                }
            }

            EditorUtility.ClearProgressBar();
            EditorUtility.DisplayDialog("完成", "執行完畢", "OK");
        }

        private void ProcessSpriteAtlas(string atlasPath, string outputFolder)
        {
            if (!atlasPath.EndsWith(".png", System.StringComparison.OrdinalIgnoreCase))
            {
                EditorUtility.DisplayDialog("錯誤", $"非 Sprite 檔案：{Path.GetFileName(atlasPath)}", "OK");
                return;
            }

            Sprite[] sprites = SpriteUnpackerCore.GetAllSpritesFromTexture(atlasPath);

            if (sprites == null || sprites.Length == 0)
            {
                EditorUtility.DisplayDialog("警告", $"無 Sprite 可拆解：{Path.GetFileName(atlasPath)}", "OK");
                return;
            }

            foreach (Sprite sprite in sprites)
            {
                if (sprite == null)
                    continue;

                Texture2D texture = sprite.texture;
                SpriteUnpackerCore.SetReadableIfNeeded(texture);
                SpriteUnpackerExporter.ExportSprite(sprite, outputFolder);
                SpriteUnpackerCore.RestoreReadable(texture);

                Debug.Log($"{sprite.name} Done!");
            }
        }
    }
}
#endif
