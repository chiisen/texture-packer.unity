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

        private string[] _draggedFiles;
        private Vector2 _scrollPos;
        private string _outputFolder = "";

        private void OnGUI()
        {
            GUILayout.Label("Sprite Unpacker", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            EditorGUILayout.HelpBox(
                "此工具用於從 Sprite Atlas (.png) 中提取各別 Sprite 並匯出為獨立的 PNG 檔案。\n\n將多個 PNG 檔案拖放到下方，或使用按鈕選擇單一檔案。",
                MessageType.Info);

            EditorGUILayout.Space();

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

            _outputFolder = EditorGUILayout.TextField("Output Folder", _outputFolder);

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("Browse Output Folder", GUILayout.Height(25)))
            {
                string folder = EditorUtility.OpenFolderPanel("Select Output Folder", _outputFolder, "");
                if (!string.IsNullOrEmpty(folder))
                    _outputFolder = folder;
            }

            GUI.enabled = _draggedFiles != null && _draggedFiles.Length > 0 && !string.IsNullOrEmpty(_outputFolder);
            if (GUILayout.Button("Unpack via Drag & Drop", GUILayout.Height(25)))
            {
                UnpackSprites(_draggedFiles);
            }
            GUI.enabled = true;
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space();

            GUI.enabled = !string.IsNullOrEmpty(_outputFolder);
            EditorGUI.BeginFadeGroup(0f);
            if (GUILayout.Button("Select Single File (Legacy)", GUILayout.Height(25)))
            {
                string[] selectedFiles = EditorUtility.OpenFilePanelWithFilters(
                    "Select Sprite Atlas PNG",
                    "",
                    new string[] { "PNG Images", "png", "All Files", "*" });

                if (selectedFiles != null && selectedFiles.Length > 0)
                {
                    UnpackSprites(selectedFiles);
                }
            }
            EditorGUI.EndFadeGroup();
            GUI.enabled = true;
        }

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
                if (texture == null)
                    continue;

                SpriteUnpackerCore.SetReadableIfNeeded(texture);
                SpriteUnpackerExporter.ExportSprite(sprite, outputFolder);
                SpriteUnpackerCore.RestoreReadable(texture);

                Debug.Log($"[SpriteUnpacker] {sprite.name} Done!");
            }
        }
    }
}
#endif
