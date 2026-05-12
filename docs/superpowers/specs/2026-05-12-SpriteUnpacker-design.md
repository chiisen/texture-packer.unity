# 設計文件：Sprite Unpacker Unity 編輯器工具

## 1. 產品目標

提供 Unity 編輯器擴充工具，將已切割的 Sprite Atlas 紋理還原為獨立 `.png` 檔。

## 2. 架構

```
Assets/Editor/SpriteUnpacker/
├── SpriteUnpackerWindow.cs    # EditorWindow，選單入口與 UI
├── SpriteUnpackerCore.cs      # 核心拆解邏輯（靜態方法）
└── SpriteUnpackerExporter.cs  # PNG 編碼與檔案寫入
```

## 3. 功能規格

### 3.1 選單與視窗 (R1)
- 路徑：`Window/Sprite Tool/Unpack Sprite to PNGs`
- 開啟 `SpriteUnpackerWindow` EditorWindow

### 3.2 檔案選擇 (R2)
- 支援多選 `.png` 檔案（使用 Unity 的 `EditorGUIUtility.OpenFilePanelWithFilters` 或自訂多選）
- 選擇輸出資料夾（`EditorUtility.OpenFolderPanel`）
- 路徑使用 `Path.Combine` 處理跨平台

### 3.3 批次處理 (優化)
- 一次處理多個 Atlas
- 使用 `EditorUtility.DisplayProgressBar` 顯示進度

### 3.4 核心拆解邏輯 (R3)
- 使用 `AssetDatabase.LoadAllAssetsAtPath` 讀取 Sprite（無需 Resources 資料夾）
- 根據 `Sprite.textureRect` 取得坐標與尺寸
- 使用 `Texture2D.GetPixels32` 提取像素
- 建立新 Texture2D，設定 `AlphaIsTransparency = true`
- 使用 `EncodeToPNG` + `File.WriteAllBytes` 寫入

### 3.5 權限處理
- 若 Sprite 原始紋理 `isReadable = false`，暫時設定為 `true`（利用 `AssetDatabase.WriteImportSettings` 或類似 API）
- 處理完畢後還原設定

### 3.6 輸出 (優化)
- 檔名與 Unity 內 Sprite 名稱一致
- 保留透明通道

### 3.7 回饋 (R4)
- 每張輸出 `Debug.Log("SpriteName Done!")`
- 全部完成 `EditorUtility.DisplayDialog("完成", "執行完畢", "OK")`

## 4. 異常處理

| 情況 | 處理 |
|------|------|
| 用戶取消選擇 | 靜默退出 |
| 檔案非 Sprite 格式 | `DisplayDialog` 錯誤提示 |
| 輸出檔案已存在 | 直接覆蓋 |

## 5. 技術約束

- Unity 6.4+
- Editor Only腳本，置於 `Editor` 資料夾
- 使用 `AssetDatabase` API（無需 Resources）

## 6. 測試驗證

- 選取含多個 Sprite 的 Atlas → 確認每個Sprite皆正確輸出
- 選取單一圖片（無Sprite）→ 確認錯誤提示
- 取消選擇 → 確認靜默退出