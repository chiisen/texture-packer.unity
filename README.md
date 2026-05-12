# texture-packer.unity
TexturePacker Unity 3D 版

## 資源目錄結構

| 目錄 | 用途 |
|------|------|
| `Assets/Sprites/` | PNG 圖片資源（Atlas/Sprite） |
| `Assets/Editor/SpriteUnpacker/` | 編輯器工具腳本 |

### 腳本說明

#### SpriteUnpacker（拆解工具）

| 腳本 | 用途 |
|------|------|
| `SpriteUnpackerWindow.cs` | 編輯器視窗與 UI，選單入口 (`SpriteUnpacker/Unpack to PNGs`)，處理檔案拖放與使用者互動 |
| `SpriteUnpackerCore.cs` | 核心拆解邏輯，讀取 Sprite、提取像素、設定紋理可讀取屬性 |
| `SpriteUnpackerExporter.cs` | PNG 匯出工具，將 Texture2D 編碼為 PNG 並寫入磁碟 |
| `TexturePackerParser.cs` | TexturePacker JSON 格式解析器 |

#### SpritePacker（打包工具）

| 腳本 | 用途 |
|------|------|
| `SpritePackerWindow.cs` | 編輯器視窗與 UI，選單入口 (`SpriteUnpacker/Pack to Atlas`)，處理多張 PNG 拖放與打包 |
| `SpritePackerCore.cs` | 核心打包邏輯，使用 Unity `PackTextures` 將多張圖合并為 Atlas，並輸出 TexturePacker JSON |

---

## SpritePacker（打包工具）

### 功能

將多張個別 PNG 打包成一張 Atlas PNG，並輸出 TexturePacker JSON 格式（`.txt`）。

輸出的 `.png` + `.txt` 可使用 SpriteUnpacker 拆解還原。

### 使用方式

1. 選單 `SpriteUnpacker/Pack to Atlas` 開啟工具
2. 拖放多張 PNG 檔案到視窗
3. **Atlas Name** 自動填入第一個檔案名稱（可自行修改）
4. **Output Folder** 自動填入第一個檔案所在目錄（可自行修改）
5. 選擇 **Max Atlas Size**（1024 / 2048 / 4096）
6. 點擊「Pack & Export」執行打包

### 輸出格式

- `AtlasName.png` - 打包後的 Atlas 圖片
- `AtlasName.txt` - TexturePacker JSON 格式設定檔

---

## SpriteUnpacker（拆解工具）

### 支援模式

SpriteUnpacker 自動偵測並支援兩種模式：

| 模式 | 說明 |
|------|------|
| **TexturePacker JSON** | 拖放 PNG 時，自動尋找同名的 `.txt` 設定檔（如 `COC_CHR_SpecialForces.png` → `COC_CHR_SpecialForces.txt`），解析後拆解。若 Atlas 名為 `COC_CHR_SpecialForces.png`，預設輸出資料夾為 `COC_CHR_SpecialForces/` |
| **Unity Sprite Metadata** | 使用 Unity 預先設定的 Sprite 中介資料（需在 Sprite Editor 中切割過） |

### 使用方式

1. 將拼好的 Atlas PNG 與對應的 TexturePacker JSON 檔案放在同一目錄
2. 選單 `SpriteUnpacker/Unpack to PNGs` 開啟工具
3. 拖放 PNG 檔案到視窗，**Output Folder 會自動填入**（預設為 PNG 檔案同目錄下的子資料夾，資料夾名稱與 PNG 檔名相同）
4. 若要改變輸出位置，可直接修改 Output Folder 或點擊「Browse Output Folder」選擇新目錄
5. 點擊「Unpack via Drag & Drop」執行拆解

---

## Bug Log

### 2026-05-13: Unpack 輸出異常（輸出 PNG 小於 200 bytes）

**原因**：座標系統不一致

- **SpritePacker**（打包）：`PackTextures` 回傳的 Rect 是 **bottom-left origin**（Unity 紋理標準）
- **SpriteUnpacker**（拆包）：假設 JSON 的 Y=0 是 **top-left**（標準 TexturePacker 格式）

兩者沒有對齊。打包輸出 JSON 時，Y 軸沒有翻轉，導致拆包時 Y 軸方向錯誤，取得的像素範圍完全錯誤，輸出幾乎是空白的 PNG（140 bytes 左右）。

**修復**：
- `SpritePackerCore.cs`：輸出 JSON 時，將 Y 軸翻轉為 top-left
  ```csharp
  int tpY = atlasHeight - y - h;
  ```
- `SpriteUnpackerCore.cs`：保持 Y 軸翻換邏輯（`srcY = atlasHeight - y - height`）

**預防**：日後修改座標相關邏輯時，必須確認打包/拆包兩端的座標系統一致，修改後需實際測試來回拆/封裝是否能正確還原。