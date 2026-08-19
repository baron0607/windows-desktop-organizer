# Windows桌面整理工具（專案藍圖：WinDeskBox 桌面收納箱）

> 本檔為跨 Agent 通用的專案藍圖（AGENTS.md 開放標準）。任何 Agent 的每個 session 都應先讀本檔＋`handoff.md`。

## 專案簡介
為 Windows 11 打造的輕量、高效、免安裝（Portable Self-Contained）桌面管理與分區收納工具（**WinDeskBox 桌面收納箱**）。支援 Fluent Acrylic 毛玻璃質感、自訂資料夾映射收納盒（Folder Portal，支援展示子資料夾與檔案並快速啟動）、分頁收納 (Tabs)、開機自動啟動、手動一鍵整理與 100% 離線資安防護。

## 關鍵時程
<!-- 格式：- 事件名稱：日期（說明）；沒有就留白 -->

## 目標與路線圖
- [x] 專案初始化完成（L1 本地 + L2 GitHub + L3 Obsidian）
- [x] 階段一：需求訪談與軟體規劃書定案（確立 WinDeskBox、.NET 8 LTS、Folder Portal 映射、開機自啟、取消即時監聽、資安審查規範）
- [x] 階段二：Win32 底層核心與收納盒 UI 開發（WorkerW/置底雙模桌面綁定、多螢幕防護、Acrylic 毛玻璃、非同步圖示快取、Roll-up 收合）
- [x] 階段三：自訂資料夾映射與檔案快速啟動（子目錄與檔案清單、Tabs 分頁、雙擊開啟、拖曳存入與同名防呆）
- [x] 階段四：開機自啟管理、設定介面、一鍵手動整理與 Portable 單檔打包（`build/WinDeskBox-Portable/WinDeskBox.exe`）

## 資料夾結構
```
Windows桌面整理工具/
├── .agents/
│   └── AGENTS.md        # 專案藍圖（開放標準）
├── agents.md            # 專案藍圖（根目錄相容）
├── handoff.md           # 跨 Agent / 跨電腦交接檔
├── .gitignore           # Git 忽略設定檔
├── rdq/
│   └── RDQ-spec-windows-desktop-organizer-20260819.md # RDQ 規格卡
├── src/
│   └── WinDeskBox/      # C# .NET 8 (LTS) WPF 完整原始碼
└── build/
    └── WinDeskBox-Portable/ # 免安裝單一執行檔（WinDeskBox.exe）
```

## 同步層級（本專案初始化至第 3 層級）

| 層級 | 平台 | 位置 | 讀取時機 |
|------|------|------|---------|
| L1 | 本地（GDrive） | `agents.md`＋`handoff.md` | 每個 session |
| L2 | GitHub | https://github.com/baron0607/windows-desktop-organizer | 指定時 |
| L3 | Obsidian | 原始資料/AI專案資料夾/Windows桌面整理工具/專案工作流程.md | 有需要時 |

## 工作約定
- 任何 Agent、任何電腦：**開工先讀 `handoff.md`，收工必更新 `handoff.md`**
- 修改共用檔案前先讀最新內容，避免覆蓋其他 Agent 的變更
- 所有回應與文件使用繁體中文
- 修改前先確認計畫，優先保留原有資料結構