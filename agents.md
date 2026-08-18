# Windows桌面整理工具（專案藍圖）

> 本檔為跨 Agent 通用的專案藍圖（AGENTS.md 開放標準）。任何 Agent 的每個 session 都應先讀本檔＋handoff.md。

## 專案簡介
自動化分類整理 Windows 桌面上散亂的檔案、資料夾與捷徑，提供靈活自訂的整理規則、定時/一鍵歸檔與乾淨清爽的桌面環境。

## 關鍵時程
<!-- 格式：- 事件名稱：日期（說明）；沒有就留白 -->

## 目標與路線圖
- [x] 專案初始化完成（L1 本地 + L2 GitHub + L3 Obsidian）
- [ ] 階段一：需求確認與分類規則規劃（依副檔名、時間、自訂標籤或關鍵字歸檔）
- [ ] 階段二：核心整理模組與安全防護機制開發（支援還原與白名單保護）
- [ ] 階段三：自動化排程或便利執行方式建置

## 資料夾結構
`
Windows桌面整理工具/
├── .agents/
│   └── AGENTS.md        # 專案藍圖（開放標準）
├── agents.md            # 專案藍圖（根目錄相容）
├── handoff.md           # 跨 Agent / 跨電腦交接檔
└── .gitignore           # Git 忽略設定檔
`

## 同步層級（本專案初始化至第 3 層級）

| 層級 | 平台 | 位置 | 讀取時機 |
|------|------|------|---------|
| L1 | 本地（GDrive） | gents.md＋handoff.md | 每個 session |
| L2 | GitHub | https://github.com/baron0607/windows-desktop-organizer | 指定時 |
| L3 | Obsidian | 原始資料/AI專案資料夾/Windows桌面整理工具/專案工作流程.md | 有需要時 |

## 工作約定
- 任何 Agent、任何電腦：**開工先讀 handoff.md，收工必更新 handoff.md**
- 修改共用檔案前先讀最新內容，避免覆蓋其他 Agent 的變更
- 所有回應與文件使用繁體中文
- 修改前先確認計畫，優先保留原有資料結構