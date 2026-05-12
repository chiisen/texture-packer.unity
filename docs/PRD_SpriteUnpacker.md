# 產品需求文件 (PRD)：Unity Sprite 拆解工具 (Sprite Unpacker)

## 1. 產品目標
提供一個 Unity 編輯器擴充工具，自動將已切割的 Sprite Atlas（圖集）紋理還原為獨立的 `.png` 圖檔，用於資產備份、修改或跨專案移植。

## 2. 目標用戶
- Unity UI 工程師
- 遊戲美術設計師
- 需要從舊專案提取資源的開發者

## 3. 功能需求 (Functional Requirements)

### 3.1 編輯器選單接入 (R1)
- **需求**：在 Unity 選單列新增入口。
- **參考路徑**：`Window/Sprite Tool/Unpack Sprite to PNGs`。
- **觸發動作**：點擊後啟動拆解流程。

### 3.2 檔案與路徑選擇 (R2)
- **來源選擇**：彈出檔案選擇對話框，限定選擇 `.png` 格式。
- **輸出路徑**：彈出資料夾選擇對話框，指定拆解後小圖的存放位置。
- **路徑處理**：工具須能自動處理 Windows/Unix 路徑符號 (`\` vs `/`) 的相容性。

### 3.3 核心拆解邏輯 (R3)
- **資源加載**：讀取選中圖檔內所有的 `Sprite` 子資源。
- **像素提取**：
  - 根據 `Sprite.textureRect` 獲取每個子圖在原始大圖中的坐標與尺寸。
  - 使用 `Texture2D.GetPixels` 提取對應範圍的像素數據。
- **重建紋理**：創建新的 `Texture2D` 實例，將提取的像素填充進去。
- **匯出格式**：將 `Texture2D` 透過 `EncodeToPNG()` 編碼，並使用 `File.WriteAllBytes` 寫入磁碟。
- **檔名保留**：產出的 `.png` 檔名必須與該 Sprite 在 Unity 內的名稱一致。

### 3.4 使用者回饋 (R4)
- **進度回饋**：每拆解一張圖需在 Console 輸出 Log（例如：`SpriteName Done!`）。
- **完成提示**：全部處理完成後彈出 `EditorUtility.DisplayDialog` 提示「執行完畢」。

## 4. 技術細節與約束 (Technical Specifications)

- **環境要求**：Unity 6.4+ (Editor Only 腳本，需放在 `Editor` 資料夾下)。
- **權限處理**：
  - 原始紋理必須開啟 `Read/Write Enabled` 選項（或是工具在運行時自動開啟並於結束後還原）。
  - 若 `Mesh Type` 為 `Tight`，需注意像素提取邊界。
- **依賴限制**：原版依賴 `Resources.LoadAll`，建議新版本改為使用 `AssetDatabase.LoadAllAssetsAtPath` 以消除「必須放在 Resources 資料夾」的限制。

## 5. 異常處理 (Edge Cases)
- **選取取消**：若用戶在選擇檔案或資料夾時點擊取消，工具應靜默退出而不報錯。
- **非 Sprite 檔案**：若選取的圖片未設定為 `Sprite (2D and UI)` 模式，應跳出錯誤提示。
- **同名覆蓋**：若輸出目錄已有同名檔案，預設執行覆蓋操作。

## 6. 優化建議 (AI Agent 重寫時可加入)
- **批次處理**：支援一次選取多個 Atlas 進行拆解。
- **進度條**：使用 `EditorUtility.DisplayProgressBar` 顯示處理進度。
- **自動透明度處理**：確保匯出的小圖保留原始透明通道資訊。
- **移除目錄限制**：透過 `AssetDatabase` 路徑轉換，支援專案內任意位置的圖片。
