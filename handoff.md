# 交接檔（handoff.md）

> 任何 Agent、任何電腦接手前**必讀**；收工時**必更新**。本檔只放交接必需的精簡資訊，詳細脈絡放 Obsidian（若有 L3）。

## ⏯️ 目前做到哪
完成 Windows 11 桌面管理軟體（WinDeskBox 桌面收納箱）全功能開發、Win32 底層圖層與資安守衛整合、編譯測試與免安裝 Single-file 綠色單檔發布打包（`build/WinDeskBox-Portable/WinDeskBox.exe`）。

## 🚦 目前狀態
- **產品名稱**：WinDeskBox（桌面收納箱）v1.0.0
- **程式原始碼與編譯**：
  1. 完整 C# .NET 8 (LTS) WPF 原始碼位於 `src/WinDeskBox/`。
  2. 編譯 0 警告 0 錯誤，已成功發布為 Self-Contained 零依賴單一綠色執行檔：`build/WinDeskBox-Portable/WinDeskBox.exe`。
- **已實現核心模組**：
  - `FolderPortal`：自訂映射任意本機目錄（支援子資料夾與各類檔案展示、雙擊開啟、拖曳存入、同名防呆對話框、唯讀 UI 即時刷新）。
  - `Tabs`：單一收納盒支援多分頁切換不同目錄。
  - `DesktopWorker`：WorkerW 與 `HWND_BOTTOM` 雙模置底鎖定，相容 Win+D 顯示桌面。
  - `ScreenBoundsGuard`：多螢幕與解析度自適應防護。
  - `StartupManager`：免管理者權限之個人啟動區開機自啟管理。
  - `SafetyManager` & `SecurityGuard`：100% 離線純淨（Zero Telemetry）、系統目錄黑名單邊界防護與資源回收筒安全刪除。
  - `SettingsWindow` & `TrayIconManager`：完整收納盒自訂介面與系統匣右鍵選單。

## ➡️ 下一步
- 歡迎使用者雙擊執行 `build/WinDeskBox-Portable/WinDeskBox.exe` 進行實機體驗。
- 根據使用者後續回饋進行細微 UI 樣式微調或追加新功能。

## ⚠️ 注意事項
- 程式發布為 Self-Contained x64 單檔，任何全新 Windows 11 / 10 電腦皆可隨身碟即插即用，免安裝任何 .NET Runtime。

## 🕐 最後更新
- 時間：2026-08-19 16:26:00
- 更新者：Antigravity @ D0A3-PC3132
- Git push：待推