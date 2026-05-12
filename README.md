# texture-packer.unity
TexturePacker Unity 3D 版

## 資源目錄結構

| 目錄 | 用途 |
|------|------|
| `Assets/Sprites/` | PNG 圖片資源（Atlas/Sprite） |
| `Assets/Editor/SpriteUnpacker/` | 編輯器工具腳本 |

### SpriteUnpacker 腳本說明

| 腳本 | 用途 |
|------|------|
| `SpriteUnpackerWindow.cs` | 編輯器視窗與 UI，選單入口 (`SpriteUnpacker/Unpack to PNGs`)，處理檔案拖放與使用者互動 |
| `SpriteUnpackerCore.cs` | 核心拆解邏輯，讀取 Sprite、提取像素、設定紋理可讀取屬性 |
| `SpriteUnpackerExporter.cs` | PNG 匯出工具，將 Texture2D 編碼為 PNG 並寫入磁碟 |
| `TexturePackerParser.cs` | TexturePacker JSON 格式解析器 |

### 支援模式

SpriteUnpacker 自動偵測並支援兩種模式：

| 模式 | 說明 |
|------|------|
| **TexturePacker JSON** | 拖放 PNG 時，自動尋找同名的 `.txt` 設定檔（如 `COC_CHR_SpecialForces.png` → `COC_CHR_SpecialForces.txt`），解析後拆解。若 Atlas 名為 `COC_CHR_SpecialForces.png`，預設輸出資料夾為 `COC_CHR_SpecialForces/` |
| **Unity Sprite Metadata** | 使用 Unity 預先設定的 Sprite 中介資料（需在 Sprite Editor 中切割過） |

### 使用方式

1. 將拼好的 Atlas PNG 與對應的 TexturePacker JSON 檔案放在同一目錄
2. 選單 `Window/Sprite Tool/Unpack Sprite to PNGs` 開啟工具
3. 拖放 PNG 檔案到視窗，**Output Folder 會自動填入**（預設為 PNG 檔案同目錄下的子資料夾，資料夾名稱與 PNG 檔名相同）
4. 若要改變輸出位置，可直接修改 Output Folder 或點擊「Browse Output Folder」選擇新目錄
5. 點擊「Unpack via Drag & Drop」執行拆解 |
