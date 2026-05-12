#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace SpriteUnpacker
{
    /// <summary>
    /// Sprite Packer 編輯器視窗。
    /// 提供圖形化介面，讓使用者透過拖放選擇多張個別 PNG，
    /// 打包成一張 Atlas PNG 並輸出 TexturePacker JSON 格式。
    /// </summary>
    /// <remarks>
    /// 選單路徑：SpriteUnpacker/Pack to Atlas
    /// 使用方式：
    /// 1. 從選單開啟此視窗
    /// 2. 拖放多張 PNG 檔案到拖放區域
    /// 3. 設定輸出資料夾與 Atlas 檔名
    /// 4. 點擊「Pack & Export」執行打包
    /// </remarks>
    public class SpritePackerWindow : EditorWindow
    {
        /// <summary>
        /// 選單項目：開啟 Sprite Packer 視窗。
        /// </summary>
        [MenuItem("SpriteUnpacker/Pack to Atlas")]
        public static void ShowWindow()
        {
            var window = GetWindow<SpritePackerWindow>("Sprite Packer");
            window.minSize = new Vector2(400, 250);
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
        /// Atlas 檔名（不含副檔名）。
        /// </summary>
        private string _atlasName = "MyAtlas";

        /// <summary>
        /// 最大 Atlas 尺寸。
        /// </summary>
        private int _maxAtlasSize = 4096;

        /// <summary>
        /// 繪製視窗 UI。
        /// </summary>
        private void OnGUI()
        {
            GUILayout.Label("Sprite Packer", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            EditorGUILayout.HelpBox(
                "此工具用於將多張個別 PNG 打包成一張 Atlas PNG，並輸出 TexturePacker JSON 格式。\n\n輸出的 .png + .txt 可使用 SpriteUnpacker 拆解還原。",
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

                            // 拖曳成功後，自動設定預設輸出資料夾與 Atlas 名稱
                            if (_draggedFiles != null && _draggedFiles.Length > 0)
                            {
                                string firstFile = _draggedFiles[0];
                                if (firstFile.EndsWith(".png", System.StringComparison.OrdinalIgnoreCase))
                                {
                                    string dir = Path.GetDirectoryName(firstFile);
                                    string name = Path.GetFileNameWithoutExtension(firstFile);
                                    _outputFolder = dir;
                                    _atlasName = name;
                                    Debug.Log($"[SpritePacker] Default set - Folder: {_outputFolder}, Atlas: {_atlasName}");
                                }
                            }
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

            // Atlas 檔名輸入框
            _atlasName = EditorGUILayout.TextField("Atlas Name", _atlasName);

            // 輸出資料夾輸入框
            _outputFolder = EditorGUILayout.TextField("Output Folder", _outputFolder);

            EditorGUILayout.BeginHorizontal();
            // 瀏覽輸出資料夾按鈕
            if (GUILayout.Button("Browse", GUILayout.Height(25)))
            {
                string folder = EditorUtility.OpenFolderPanel("Select Output Folder", _outputFolder, "");
                if (!string.IsNullOrEmpty(folder))
                    _outputFolder = folder;
            }

            // 執行打包按鈕
            bool canPack = _draggedFiles != null && _draggedFiles.Length > 0 &&
                           !string.IsNullOrEmpty(_outputFolder) &&
                           !string.IsNullOrEmpty(_atlasName);
            GUI.enabled = canPack;
            if (GUILayout.Button("Pack & Export", GUILayout.Height(25)))
            {
                PackSprites(_draggedFiles);
            }
            GUI.enabled = true;
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space();

            // 最大 Atlas 尺寸
            EditorGUILayout.BeginHorizontal();
            GUILayout.Label("Max Atlas Size:", GUILayout.Width(100));
            string[] sizes = { "1024", "2048", "4096" };
            int selectedSize = Mathf.Clamp(Array.IndexOf(sizes, _maxAtlasSize.ToString()), 0, sizes.Length - 1);
            selectedSize = EditorGUILayout.Popup(selectedSize, sizes);
            _maxAtlasSize = int.Parse(sizes[selectedSize]);
            EditorGUILayout.EndHorizontal();
        }

        /// <summary>
        /// 執行 Sprite 打包並匯出作業。
        /// </summary>
        /// <param name="selectedFiles">要打包的檔案路徑陣列。</param>
        private void PackSprites(string[] selectedFiles)
        {
            if (selectedFiles == null || selectedFiles.Length == 0)
                return;

            // 過濾只取 PNG 檔案
            var pngFiles = new System.Collections.Generic.List<string>();
            foreach (string file in selectedFiles)
            {
                if (file.EndsWith(".png", System.StringComparison.OrdinalIgnoreCase))
                {
                    pngFiles.Add(file);
                }
            }

            if (pngFiles.Count == 0)
            {
                EditorUtility.DisplayDialog("錯誤", "沒有有效的 PNG 檔案", "OK");
                return;
            }

            Debug.Log($"[SpritePacker] Starting pack with {pngFiles.Count} textures...");

            bool success = SpritePackerCore.PackAndExport(
                pngFiles.ToArray(),
                _outputFolder,
                _atlasName,
                _maxAtlasSize);

            if (success)
            {
                EditorUtility.DisplayDialog("完成", $"已成功打包 {pngFiles.Count} 張圖片至 {_atlasName}.png", "OK");
                _draggedFiles = null;
            }
            else
            {
                EditorUtility.DisplayDialog("錯誤", "打包失敗，請查看 Console 日誌", "OK");
            }
        }
    }
}
#endif