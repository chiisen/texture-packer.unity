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
                "This tool extracts individual sprites from a Sprite Atlas (.png) and exports them as separate PNG files.\n\nDrag & Drop multiple PNG files below, or use the button to select a single file.",
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
            if (GUILayout.Button("Select Single File (Legacy)", GUILayout.Height(20)))
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
                    Debug.LogError($"[SpriteUnpacker] Error processing {filePath}: {ex.Message}");
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
                SpriteUnpackerCore.SetReadableIfNeeded(texture);
                SpriteUnpackerExporter.ExportSprite(sprite, outputFolder);
                SpriteUnpackerCore.RestoreReadable(texture);

                Debug.Log($"{sprite.name} Done!");
            }
        }
    }
}
#endif
