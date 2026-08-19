# 交接檔（handoff.md）

> 任何 Agent、任何電腦接手前**必讀**；收工時**必更新**。本檔只放交接必需的精簡資訊，詳細脈絡放 Obsidian（若有 L3）。

## ⏯️ 目前做到哪
完成 Windows 11 桌面管理軟體（WinDeskBox 桌面收納箱）的需求訪談與完整軟體規劃書，正式定名為「WinDeskBox」，各項核心架構決策已定案。

## 🚦 目前狀態
- 產品名稱正式定名為 **WinDeskBox（桌面收納箱）**
- 規劃書（`implementation_plan.md`）與 RDQ 規格卡已完整定案：
  1. 技術棧：C# / .NET 8 (LTS) (WPF + Win32 Shell API)，採 Self-Contained 零依賴單一綠色執行檔發布
  2. 核心功能：自訂資料夾映射收納盒（Folder Portal，支援展示子資料夾與檔案並快速啟動，如 D:\forest\桌面區\執行中、常用捷徑）、Tabs 分頁、Win11 Acrylic 毛玻璃、WorkerW/置底雙模桌面鎖定、多螢幕越界防護、滾動收合、雙擊隱藏
  3. 啟動與觸發：支援隨 Windows 開機自動啟動（Startup 捷徑）；取消即時背景攔截，改為手動一鍵整理與啟動整理；映射目錄唯讀 UI 自動刷新
  4. 資安防護：100% 離線純淨（Zero Telemetry）、目錄授權白名單與 Path Traversal 防禦
- 尚未開始編寫程式碼原始碼（src/）。

## ➡️ 下一步
1. 建立 C# .NET 8 (LTS) WPF 專案架構（`src/WinDeskBox`）與 Self-Contained Single-file 發布配置。
2. 實現 Win32 核心模組（WorkerW 桌面底層鎖定、ScreenBoundsGuard 多螢幕防護、DWM Acrylic 毛玻璃 API 封裝、ShellIconLoader 非同步圖示快取）。
3. 開發自訂資料夾映射收納盒 UI（支援 Tabs 分頁、子資料夾與檔案展示、雙擊開啟、拖曳存入與位置鎖定）。

## ⚠️ 注意事項
- 檔案操作涉及本機目錄與桌面，開發時嚴格遵循 SecurityGuard 安全邊界防禦，所有操作僅限使用者授權之自訂目錄與桌面。

## 🕐 最後更新
- 時間：2026-08-19 16:16:00
- 更新者：Antigravity @ D0A3-PC3132
- Git push：✅ 已推