# Probably Stolen 正體中文（台灣）模組

《Probably Stolen》正式版的正體中文化模組，使用遊戲官方的模組系統，可從 Steam 工作坊訂閱。
以官方簡體中文為底本，依台灣用語與語感改寫遊戲文字，並把中文字型換成繁中字型。

- 語言選單會多出「正體中文」選項，選了就顯示本模組的譯文；原本的「Simplified Chinese」仍是遊戲原版的簡體中文。
- 不修改任何遊戲檔與存檔，停用模組就回到原版。
- 試玩版用的安裝程式在 [`demo` 分支](../../tree/demo)與 [v1.1.0](../../releases/tag/v1.1.0)，正式版請用本模組。

![主選單](docs/main-menu.png)

### 改寫前後對照

| 操作提示（上：原版簡體／下：本模組） | 導師筆記（左：本模組／右：原版簡體） |
|---|---|
| ![操作提示對照](docs/compare-controls.png) | ![導師筆記對照](docs/compare-mentor-note.png) |

| 香菸真偽鑑定手冊（左：原版簡體／右：本模組） | 房租提醒（上：原版簡體／下：本模組） |
|---|---|
| ![鑑定手冊對照](docs/compare-cigarette-manual.png) | ![房租提醒對照](docs/compare-rent.png) |

## 安裝

### Steam 工作坊（建議）

1. 在[工作坊](https://steamcommunity.com/sharedfiles/filedetails/?id=3829248318)訂閱本模組，啟動遊戲。
2. 在主選單的「改裝件」（Mods）畫面啟用「Traditional Chinese (Taiwan)」，再重新啟動遊戲。
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
- 用簡體中文玩過的舊存檔可以直接沿用（v1.4.0 起）：遊戲寫進存檔的物品名稱、帳單、新聞等文字，會在顯示時換成譯文，存檔本身不會被修改。

## 譯文進度

| 來源 | 條數 | 狀態 |
|---|---|---|
| 試玩版譯文（原文未變） | 5,288 | 已逐條潤飾 |
| 正式版新增或修改 | 1,393 | 已逐條潤飾 |

歡迎在 [Issues](../../issues) 回報不順或錯誤的譯文。

## 字型

| 用途 | 原字型 | 替換為 |
|---|---|---|
| 內文 | Noto Sans SC | [源樣黑體 GenYo Gothic 2 TW](https://github.com/ButTaiwan/genyog-font) Light（ButTaiwan），字距 +0.05 em，行距沿用西文 Noto Sans |
| 標題 | WDXL Lubrifont SC | [WDXL Lubrifont TC](https://fonts.google.com/specimen/WDXL+Lubrifont+TC) |
| 手寫 | Ma Shan Zheng | [霞鶩文楷 TC](https://github.com/lxgw/LxgwWenkaiTC) |

三套字型皆以 SIL Open Font License 1.1 授權，授權全文見 `mod/fonts/OFL-*.txt`。

## 開發

運作方式、建置與發佈流程見 [DEVELOPMENT.md](DEVELOPMENT.md)。

## 授權

- 程式碼：MIT License（見 `LICENSE`）
- 字型：SIL Open Font License 1.1
- 譯文（`mod/data/strings.json`）：改寫自遊戲官方文本，遊戲內容版權屬 Questing Goose Studio 所有。

本專案為非官方玩家自製模組，與開發商、發行商無關。
