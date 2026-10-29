# Probably Stolen 正體中文（台灣）模組

《Probably Stolen》正式版的正體中文化模組，使用遊戲官方的模組系統，可從 Steam 工作坊訂閱。
以官方簡體中文為底本，依台灣用語與語感改寫遊戲文字，並把中文字型換成繁中字型。

- 選單上的簡體中文選項會改為「正體中文」，選了就顯示本模組的譯文。
- 不修改任何遊戲檔與存檔，停用模組就回到原版。
- 試玩版用的安裝程式在 [`demo` 分支](../../tree/demo)與 [v1.1.0](../../releases/tag/v1.1.0)，正式版請用本模組。

![遊戲內的導師筆記與物品說明](docs/screenshot-notebook.png)

<sub>試玩版 1.0 版畫面。現在內文改用源樣黑體，行距也已對齊筆記本格線。</sub>

## 安裝

### Steam 工作坊（建議）

1. 在工作坊訂閱本模組，啟動遊戲。
2. 在主選單的「Mods」畫面啟用「Traditional Chinese (Taiwan)」，再重新啟動遊戲。
3. 在主選單右上角的語言選單選「正體中文」。

### 手動安裝

1. 到 [Releases](../../releases) 下載 `seacucu.zh-tw.zip`。
2. 關閉遊戲，把壓縮檔裡的 `seacucu.zh-tw` 資料夾解壓縮到遊戲資料夾的 `Mods` 底下（變成 `Mods\seacucu.zh-tw\manifest.xml`）。
   遊戲資料夾：Steam 遊戲庫 → 在遊戲上按右鍵 → 管理 → 瀏覽本機檔案。
3. 照上面的第 2、3 步啟用並選擇語言。

更新時先刪掉舊的 `Mods\seacucu.zh-tw` 資料夾再解壓縮。

### 注意

- 請停用其他中文翻譯模組，同時啟用會互相干擾。
- 遊戲更新後，原文有變動的句子會先顯示遊戲原本的簡體中文，等本模組更新後再換成譯文，不會顯示過時的內容。

## 譯文進度

| 來源 | 條數 | 狀態 |
|---|---|---|
| 試玩版譯文（原文未變） | 5,289 | 已逐條潤飾 |
| 正式版新增或修改 | 1,392 | 自動繁化稿（OpenCC），待潤飾 |

歡迎在 [Issues](../../issues) 回報不順或錯誤的譯文。

## 字型

| 用途 | 原字型 | 替換為 |
|---|---|---|
| 內文 | Noto Sans SC | [源樣黑體 GenYo Gothic 2 TW](https://github.com/ButTaiwan/genyog-font) Light（ButTaiwan），字距 +0.05 em，行距沿用西文 Noto Sans |
| 標題 | WDXL Lubrifont SC | [WDXL Lubrifont TC](https://fonts.google.com/specimen/WDXL+Lubrifont+TC) |
| 手寫 | Ma Shan Zheng | [霞鶩文楷 TC](https://github.com/lxgw/LxgwWenkaiTC) |

三套字型皆以 SIL Open Font License 1.1 授權，授權全文見 `mod/fonts/OFL-*.txt`。

## 運作方式

模組（`src/`）透過 Harmony 掛在遊戲的三個地方：

| 位置 | 作用 |
|---|---|
| `LocalizedStringDatabase.GenerateLocalizedString` | 遊戲第一次用到某張簡中字串表時，把表內條目換成譯文。每條譯文附有簡中原文的雜湊，原文對不上就保留原文 |
| `FontLoader.GetFontByRole` | 簡中語系下，把簡中字型資產換成用模組字型檔即時建立的繁中字型資產 |
| `MainMenuUIController.HandleLocaleDropdown` | 語言選單的「Simplified Chinese」改為「正體中文」 |

模組不需要任何額外權限（遊戲的模組掃描不會偵測到檔案、網路等能力），檔案一律透過 `ModHelper` 讀取。

## 建置與上架

需要 .NET SDK（6 以上）與已安裝的遊戲（編譯時參照遊戲的 `Managed` 組件，不會一起打包）。

```
tools\build.ps1             # 編譯並組出 dist\seacucu.zh-tw\
tools\build.ps1 -Install    # 同上，並複製到遊戲的 Mods\ 測試
tools\upload.ps1 -User <Steam 帳號> -ChangeNote "更新說明"   # 用 SteamCMD 上傳到工作坊
```

- 遊戲不在預設位置時，加上 `-GameDir <遊戲資料夾>`。
- `upload.ps1` 需要 [SteamCMD](https://developer.valvesoftware.com/wiki/SteamCMD)（預設 `C:\steamcmd\steamcmd.exe`，可用 `-SteamCmd` 指定），密碼與 Steam Guard 由 SteamCMD 自己詢問。
  第一次上傳會建立私人項目，並把項目 ID 存到 `workshop/publishedfileid.txt`（請 commit）；之後的上傳會更新同一個項目。
  工作坊頁面的標題、說明與預覽圖分別取自 `workshop/title.txt`、`description.txt`、`preview.png`，每次上傳都會覆寫。
- 譯文 `mod/data/strings.json` 由另外的譯文流程產生（內含開發商原文，不放在本 repo）。

## 授權

- 程式碼：MIT License（見 `LICENSE`）
- 字型：SIL Open Font License 1.1
- 譯文（`mod/data/strings.json`）：改寫自遊戲官方文本，遊戲內容版權屬 Questing Goose Studio 所有。

本專案為非官方玩家自製模組，與開發商、發行商無關。
