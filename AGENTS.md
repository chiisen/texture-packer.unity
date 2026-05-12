# Unity 專案規範

## 日誌規範（Logging Best Practices）

**核心原則：怕會有問題，就加 Log。**

撰寫程式碼時，**預防性日誌**比事後除錯更重要。以下為強制要求：

### 1. 懷疑有風險時立刻加 Log
- 不確定變數值是否正確 → 加 Log
- 懷疑某個分支會不會走到 → 加 Log
- 處理陣列、座標、尺寸時 → 加 Log 確認範圍
- 任何可能失敗的操作（檔案讀寫、Parse、轉型）→ 必須有 Log

### 2. Log 等級使用原則
- `Debug.Log` - 正常流程追蹤（Development only）
- `Debug.LogWarning` - 接近錯誤但可繼續
- `Debug.LogError` - 明確的錯誤，明確失敗

### 3. Log 內容要「有意義」
每一條 Log 都要能回答一個具體問題，例如：
- ✅ `[SpriteUnpacker] Frame 'atk_1.png': rect=(10,20,64,64)`
- ❌ `[SpriteUnpacker] Processing...`

### 4. 實作範例
```csharp
// 壞：沒 Log，出錯只能靠直覺
int[] pixels = atlasTexture.GetPixels32(frameIndex);

// 好：有 Log，出錯有跡可循
int[] pixels = atlasTexture.GetPixels32(frameIndex);
if (pixels == null || pixels.Length == 0)
{
    Debug.LogError($"[SpriteUnpacker] GetPixels32 returned empty for frame '{frame.name}'");
    return;
}
Debug.Log($"[SpriteUnpacker] GetPixels32 for '{frame.name}': {pixels.Length} pixels");
```

### 5. 刪除的時機
- 確認功能穩定後，可將 `Debug.Log` 改為不輸出（或移除）
- `Debug.LogError` **永遠保留**
- 刪除前確認該 Log 沒有替代用途（如效能監測）

---

## 修改程式碼後的驗證流程

修改 Unity C# 腳本後，**必須**執行以下驗證流程：

1. **編譯檢查**：使用 `mcp-unity_recompile_scripts` 確認編譯成功（0 errors）
2. **錯誤檢查**：使用 `mcp-unity_get_console_logs` 查看 Console 是否有新的 error 或 warning
3. **確認無誤後再繼續**：嚴禁在有編譯錯誤的情況下繼續其他操作

```bash
# 驗證流程（每次修改後）
mcp-unity_recompile_scripts
mcp-unity_get_console_logs limit=20 logType=error
```

---

## 座標系統規範（防止 Y 軸方向錯誤）

### 核心原則

**Unity `Texture2D.GetPixels32()` 的 Y=0 在底部；標準 TexturePacker JSON 的 Y=0 在頂部。**

兩端不一致，必須明确轉換，不能假設兩邊相同。

### 規範

1. **打包（SpritePacker）**：輸出 JSON 前，Y 軸必須翻轉
   ```csharp
   int tpY = atlasHeight - y - h;  // bottom-left → top-left
   ```

2. **拆包（SpriteUnpacker）**：讀取 JSON 後，Y 軸必須翻轉
   ```csharp
   int srcY = atlasHeight - y - height;  // top-left → bottom-left
   ```

3. **修改前確認**：任何座標相關的修改，完成後必須實際測試來回拆/封裝是否能正確還原
   - 打包 → 拆包 → 圖案一致
   - 拆包 → 打包 → 圖案一致

4. **禁止**：不要假設「反正座標系相同，不用轉換」——這個假設是 Bug 的根源。