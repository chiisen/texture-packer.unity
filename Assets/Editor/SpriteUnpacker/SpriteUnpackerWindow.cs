#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;

namespace SpriteUnpacker
{
    /// <summary>
    /// Sprite Unpacker 編輯器視窗。
    /// 提供圖形化介面，讓使用者透過拖放或按鈕選擇 Sprite Atlas 檔案，
    /// 並指定輸出資料夾，即可批次將 Atlas 內的每個 Sprite 匯出為獨立的 PNG 檔案。
    /// </summary>
    /// <remarks>
    /// 選單路徑：Window/Sprite Tool/Unpack Sprite to PNGs
    /// 使用方式：
    /// 1. 從選單開啟此視窗
    /// 2. 將多個 .png 檔案拖放到拖放區域，或使用 Legacy 按鈕選擇單一檔案
    /// 3. 設定輸出資料夾
    /// 4. 點擊「Unpack via Drag & Drop」按鈕執行拆解
    /// </remarks>
    public class SpriteUnpackerWindow : EditorWindow
    {
        /// <summary>
        /// 選單項目：開啟 Sprite Unpacker 視窗。
        /// </summary>
        [MenuItem("Window/Sprite Tool/Unpack Sprite to PNGs")]
        public static void ShowWindow()
        {
            var window = GetWindow<SpriteUnpackerWindow>("Sprite Unpacker");
            window.minSize = new Vector2(400, 200);
        }

        /// <summary>
        /// 當視窗啟用時的初始化作業。
        /// </summary>
        private void OnEnable()
        {
            autoRepaintOnSceneChange = true;
        }

        /// <summary>
        /// 拖放區域選中的檔案路徑陣列。
        /// </summary>
        private string[] _draggedFiles;

        /// <summary>
        /// 檔案列表的捲動位置。
        /// </summary>
        private Vector2 _scrollPos;

        /// <summary>
        /// 輸出資料夾路徑。
        /// </summary>
        private string _outputFolder = "";

        /// <summary>
        /// 繪製視窗 UI。
        /// </summary>
        private void OnGUI()
        {
            GUILayout.Label("Sprite Unpacker", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            EditorGUILayout.HelpBox(
                "此工具用於從 Sprite Atlas (.png) 中提取各別 Sprite 並匯出為獨立的 PNG 檔案。\n\n將多個 PNG 檔案拖放到下方，或使用按鈕選擇單一檔案。",
                MessageType.Info);

            EditorGUILayout.Space();

            // 拖放區域
            _scrollPos = EditorGUILayout.BeginScrollView(_scrollPos, GUILayout.Height(120));
            EditorGUILayout.BeginVertical("box");

            Rect dropArea = GUILayoutUtility.GetRect(0, 0, GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));
            GUI.Box(dropArea, "Drop PNG files here", EditorStyles.centeredGreyMiniLabel);

            Event evt = Event.current;
            switch (evt.type)
            {
                case EventType.DragUpdated:
                case EventType.DragPerform:
                    if (dropArea.Contains(evt.mousePosition))
                    {
                        DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
                        if (evt.type == EventType.DragPerform)
                        {
                            DragAndDrop.AcceptDrag();
                            _draggedFiles = DragAndDrop.paths;
                        }
                        GUI.changed = true;
                    }
                    break;
            }

            // 顯示選中的檔案列表
            if (_draggedFiles != null && _draggedFiles.Length > 0)
            {
                EditorGUILayout.Space();
                GUILayout.Label($"Selected ({_draggedFiles.Length} files):", EditorStyles.boldLabel);
                foreach (string file in _draggedFiles)
                {
                    GUILayout.Label($"  {Path.GetFileName(file)}", EditorStyles.miniLabel);
                }
            }

            EditorGUILayout.EndVertical();
            EditorGUILayout.EndScrollView();

            EditorGUILayout.Space();

            // 輸出資料夾輸入框
            _outputFolder = EditorGUILayout.TextField("Output Folder", _outputFolder);

            EditorGUILayout.BeginHorizontal();
            // 瀏覽輸出資料夾按鈕
            if (GUILayout.Button("Browse Output Folder", GUILayout.Height(25)))
            {
                string folder = EditorUtility.OpenFolderPanel("Select Output Folder", _outputFolder, "");
                if (!string.IsNullOrEmpty(folder))
                    _outputFolder = folder;
            }

            // 執行拆解按鈕（需要已選取檔案且已設定輸出資料夾）
            GUI.enabled = _draggedFiles != null && _draggedFiles.Length > 0 && !string.IsNullOrEmpty(_outputFolder);
            if (GUILayout.Button("Unpack via Drag & Drop", GUILayout.Height(25)))
            {
                UnpackSprites(_draggedFiles);
            }
            GUI.enabled = true;
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space();

            // Legacy 單選按鈕（用於相容性）
            GUI.enabled = !string.IsNullOrEmpty(_outputFolder);
            if (GUILayout.Button("Select Single File (Legacy)", GUILayout.Height(25)))
            {
                string selectedFile = EditorUtility.OpenFilePanel(
                    "Select Sprite Atlas PNG",
                    "",
                    "png");

                if (!string.IsNullOrEmpty(selectedFile))
                {
                    UnpackSprites(new string[] { selectedFile });
                }
            }
            GUI.enabled = true;
        }

        /// <summary>
        /// 執行 Sprite 拆解並匯出作業。
        /// </summary>
        /// <param name="selectedFiles">要處理的檔案路徑陣列。</param>
        /// <remarks>
        /// 此方法會：
        /// 1. 若未設定輸出資料夾，彈出資料夾選擇對話框
        /// 2. 批次處理每個 Atlas 檔案
        /// 3. 顯示進度條
        /// 4. 處理完畢後顯示完成提示對話框
        /// </remarks>
        private void UnpackSprites(string[] selectedFiles)
        {
            if (selectedFiles == null || selectedFiles.Length == 0)
                return;

            string outputFolder = _outputFolder;
            if (string.IsNullOrEmpty(outputFolder))
            {
                outputFolder = EditorUtility.OpenFolderPanel("Select Output Folder", "", "");
                if (string.IsNullOrEmpty(outputFolder))
                    return;
                _outputFolder = outputFolder;
            }

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
                    Debug.LogError($"[SpriteUnpacker] Error processing {filePath}: {ex}");
                }
            }

            EditorUtility.ClearProgressBar();
            _draggedFiles = null;
            EditorUtility.DisplayDialog("完成", "執行完畢", "OK");
        }

        /// <summary>
        /// 處理單一張 Sprite Atlas 圖片，將其內的所有 Sprite 匯出為獨立的 PNG 檔案。
        /// 自動偵測使用 TexturePacker JSON 模式或 Unity Sprite Metadata 模式。
        /// </summary>
        /// <param name="atlasPath">Atlas 圖片檔案的路徑。</param>
        /// <param name="outputFolder">輸出資料夾的路徑。</param>
        private void ProcessSpriteAtlas(string atlasPath, string outputFolder)
        {
            if (!atlasPath.EndsWith(".png", System.StringComparison.OrdinalIgnoreCase))
            {
                EditorUtility.DisplayDialog("錯誤", $"非 Sprite 檔案：{Path.GetFileName(atlasPath)}", "OK");
                return;
            }

            string jsonPath = SpriteUnpackerCore.GetAtlasJsonPath(atlasPath);
            if (SpriteUnpackerCore.HasTexturePackJson(atlasPath))
            {
                ProcessTexturePackerAtlas(atlasPath, jsonPath, outputFolder);
            }
            else
            {
                ProcessUnitySpriteAtlas(atlasPath, outputFolder);
            }
        }

        /// <summary>
        /// 使用 TexturePacker JSON 模式處理 Atlas。
        /// </summary>
        /// <param name="atlasPath">Atlas 圖片檔案的路徑。</param>
        /// <param name="jsonPath">TexturePacker JSON 檔案的路徑。</param>
        /// <param name="outputFolder">輸出資料夾的路徑。</param>
        private void ProcessTexturePackerAtlas(string atlasPath, string jsonPath, string outputFolder)
        {
            SpriteFrame[] frames = TexturePackerParser.GetSpriteFrames(jsonPath);
            if (frames == null || frames.Length == 0)
            {
                Debug.LogWarning($"[SpriteUnpacker] No frames found in JSON: {jsonPath}");
                return;
            }

            Texture2D atlasTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(atlasPath);
            if (atlasTexture == null)
            {
                Debug.LogError($"[SpriteUnpacker] Failed to load atlas texture: {atlasPath}");
                return;
            }

            SpriteUnpackerCore.SetReadableIfNeeded(atlasTexture);

            int frameIndex = 0;
            foreach (SpriteFrame frame in frames)
            {
                frameIndex++;
                float frameProgress = (float)frameIndex / frames.Length;
                EditorUtility.DisplayProgressBar(
                    $"Extracting Sprites ({frameIndex}/{frames.Length})",
                    $"{frame.name} ({Mathf.RoundToInt(frameProgress * 100)}%)",
                    frameProgress);

                Texture2D extractedTex = SpriteUnpackerCore.ExtractFromTexturePackJson(atlasTexture, frame);
                if (extractedTex != null)
                {
                    string spriteName = Path.GetFileNameWithoutExtension(frame.name);
                    SpriteUnpackerExporter.ExportTextureToPng(extractedTex, outputFolder, spriteName);
                    UnityEngine.Object.DestroyImmediate(extractedTex);
                    Debug.Log($"[SpriteUnpacker] {spriteName} Done! (TexturePacker mode)");
                }
            }

            SpriteUnpackerCore.RestoreReadable(atlasTexture);
        }

        /// <summary>
        /// 使用 Unity Sprite Metadata 模式處理 Atlas。
        /// </summary>
        /// <param name="atlasPath">Atlas 圖片檔案的路徑。</param>
        /// <param name="outputFolder">輸出資料夾的路徑。</param>
        private void ProcessUnitySpriteAtlas(string atlasPath, string outputFolder)
        {
            Sprite[] sprites = SpriteUnpackerCore.GetAllSpritesFromTexture(atlasPath);

            if (sprites == null || sprites.Length == 0)
            {
                EditorUtility.DisplayDialog("警告", $"無 Sprite 可拆解：{Path.GetFileName(atlasPath)}", "OK");
                return;
            }

            int spriteIndex = 0;
            foreach (Sprite sprite in sprites)
            {
                if (sprite == null)
                    continue;

                spriteIndex++;
                float spriteProgress = (float)spriteIndex / sprites.Length;
                EditorUtility.DisplayProgressBar(
                    $"Extracting Sprites ({spriteIndex}/{sprites.Length})",
                    $"{sprite.name} ({Mathf.RoundToInt(spriteProgress * 100)}%)",
                    spriteProgress);

                Texture2D texture = sprite.texture;
                if (texture == null)
                    continue;

                SpriteUnpackerCore.SetReadableIfNeeded(texture);
                SpriteUnpackerExporter.ExportSprite(sprite, outputFolder);
                SpriteUnpackerCore.RestoreReadable(texture);

                Debug.Log($"[SpriteUnpacker] {sprite.name} Done! (Unity mode)");
            }
        }
    }
}
#endif