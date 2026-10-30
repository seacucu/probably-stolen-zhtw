# 開發說明

## 運作方式

模組（`src/`）透過 Harmony 掛在遊戲的這幾個地方：

| 位置 | 作用 |
|---|---|
| `LocalizedStringDatabase.GenerateLocalizedString` | 遊戲第一次用到某張簡中字串表時，把表內條目換成譯文（同時記下原文，切回簡體中文時換回去）。每條譯文附有簡中原文的雜湊，原文對不上就保留原文 |
| `FontLoader.GetFontByRole` | 選正體中文時，把簡中字型資產換成用模組字型檔即時建立的繁中字型資產 |
| `TMP_Text.text`、`TMP_Text.SetText` | 遊戲先前組好並存進存檔的簡中文字（物品、帳單、新聞、客人名稱），顯示時對照字串表原文換成譯文：整句相同、`{0}` 句型相符，或長句中出現的原文片段。只換畫面上的文字，不改遊戲資料與存檔 |
| `MainMenuUIController.HandleLocaleDropdown`、`OnLocaleChanged` | 在語言選單最後加上「正體中文」。兩個中文選項都用遊戲的中文語系（`zh`），選哪一個決定顯示譯文或原版簡中，並存進遊戲設定 |

模組不需要任何額外權限（遊戲的模組掃描不會偵測到檔案、網路等能力），檔案一律透過 `ModHelper` 讀取。

## 建置與上架

需要 .NET SDK（6 以上）與已安裝的遊戲（編譯時參照遊戲的 `Managed` 組件，不會一起打包）。

```
tools\build.ps1             # 編譯並組出 dist\seacucu.zh-tw\ 與 dist\seacucu.zh-tw.zip
tools\build.ps1 -Install    # 同上，並複製到遊戲的 Mods\ 測試
gh release create v1.3.0 dist\seacucu.zh-tw.zip --notes "更新說明"   # 發佈，並自動上傳到工作坊
```

- 遊戲不在預設位置時，加上 `-GameDir <遊戲資料夾>`。
- 在 Mods 畫面替本機這份模組開啟開發者模式後，遊戲中按 F7 可在正體與簡體中文之間切換，方便對照與截圖。
- 發佈 GitHub Release 時，`.github/workflows/workshop.yml` 會把附檔 `seacucu.zh-tw.zip` 上傳到 `workshop/publishedfileid.txt` 指定的工作坊項目，
  Release 說明會成為工作坊的更新說明。工作坊頁面的標題、說明與預覽圖取自 main 分支的 `workshop/title.txt`、`description.txt`、`preview.png`，每次上傳都會覆寫。
  SteamCMD 只能寫入預設（英文）區，兩區都用同一份中文。工作坊頁面另有正體中文區時，要切換到該語言後把同樣的標題與說明手動貼上。
  正式版（非預先發行）會順便把項目設為公開。
- SteamCMD 也不能設定工作坊標籤。要改標籤時，用項目擁有者的帳號登入 Steam 用戶端，再執行 `dotnet run --project tools\WorkshopTags -- Mod Translation`。
  要重傳某個版本，可在 Actions 頁面手動執行「Workshop upload」並填入標籤。
- 上傳需要兩個 repo secrets：`STEAM_USERNAME`，以及 `STEAM_CONFIG_VDF`（在自己電腦用 [SteamCMD](https://developer.valvesoftware.com/wiki/SteamCMD) 登入成功後，`config/config.vdf` 的 base64）。
  其中存的是登入權杖，不含密碼；權杖失效、上傳顯示登入失敗時，重新登入 SteamCMD 再更新這個 secret。
- 譯文 `mod/data/strings.json` 由另外的譯文流程產生（內含開發商原文，不放在本 repo）。
