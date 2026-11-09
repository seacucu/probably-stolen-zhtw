# 開發說明

## 運作方式

模組（`src/`）透過 Harmony 掛在遊戲的這幾個地方：

| 位置 | 作用 |
|---|---|
| `LocalizedStringDatabase.GenerateLocalizedString` | 遊戲第一次用到某張簡中字串表時，把表內條目換成譯文。每條譯文附有英文原文的雜湊（不計空白與換行）；本地化系統就緒後，模組以非同步方式載入英文字串表逐條比對，比完即釋放。英文改了就保留遊戲原文、交給簡體護欄轉換。譯文依英文意思寫成，官方簡中只換說法時不影響（刻意不比對簡中：官方簡中在幾次更新間改來改去，比對簡中會讓譯文跟著失效） |
| `FontLoader.GetFontByRole` | 選中文時，把簡中字型資產換成用模組字型檔即時建立的繁中字型資產 |
| `MainMenuUIController.HandleLocaleDropdown` | 把語言選單的「Simplified Chinese」改名為「正體中文」。模組沿用遊戲的中文語系（`zh`），選中文即顯示譯文 |

模組不需要任何額外權限（遊戲的模組掃描不會偵測到檔案、網路等能力），檔案一律透過 `ModHelper` 讀取。

## 建置與上架

需要 .NET SDK（6 以上）與已安裝的遊戲（編譯時參照遊戲的 `Managed` 組件，不會一起打包）。

```
tools\build.ps1             # 編譯並組出 dist\seacucu.zh-tw\ 與 dist\seacucu.zh-tw.zip
tools\build.ps1 -Install    # 同上，並複製到遊戲的 Mods\ 測試
gh release create v1.5.1 dist\seacucu.zh-tw.zip --notes-file dist/release-notes.md   # 發佈，並自動上傳到工作坊
```

- 遊戲不在預設位置時，加上 `-GameDir <遊戲資料夾>`。
- 更新紀錄只寫在 `mod/data/changelog.txt`（新版本在最上面，沒有項目的版本不列出）。建置時會寫進 `workshop/description.txt` 的「更新紀錄」與 `dist/release-notes.md`；模組更新後也會在遊戲主選單跳出這次的紀錄，主選單的感謝按鈕可以看全部紀錄。
- 發佈 GitHub Release 時，`.github/workflows/workshop.yml` 會把附檔 `seacucu.zh-tw.zip` 上傳到 `workshop/publishedfileid.txt` 指定的工作坊項目，
  Release 說明會成為工作坊的更新說明。工作坊頁面的標題、說明與預覽圖取自 main 分支的 `workshop/title.txt`、`description.txt`、`preview.png`，每次上傳都會覆寫。
  SteamCMD 只能寫入預設（英文）區，兩區都用同一份中文。工作坊頁面另有正體中文區時，要切換到該語言後把同樣的標題與說明手動貼上。
  正式版（非預先發行）會順便把項目設為公開。
- SteamCMD 也不能設定工作坊標籤。要改標籤時，用項目擁有者的帳號登入 Steam 用戶端，再執行 `dotnet run --project tools\WorkshopTags -- Mod Translation`。
  要重傳某個版本，可在 Actions 頁面手動執行「Workshop upload」並填入標籤。
- 上傳需要兩個 repo secrets：`STEAM_USERNAME`，以及 `STEAM_CONFIG_VDF`（在自己電腦用 [SteamCMD](https://developer.valvesoftware.com/wiki/SteamCMD) 登入成功後，`config/config.vdf` 的 base64）。
  其中存的是登入權杖，不含密碼；權杖失效、上傳顯示登入失敗時，重新登入 SteamCMD 再更新這個 secret。
- 譯文 `mod/data/strings.json` 由另外的譯文流程產生（內含開發商原文，不放在本 repo）。
