using TMPro;

namespace ProbablyStolenZhTW
{
    // Tells the player once, in the main menu's own confirm panel, that saves started in Simplified
    // Chinese keep some of their text, and offers to start a new game.
    internal static class Notice
    {
        private const string SaveKey = "seacucu.zh-tw.notice-shown";
        private const string Text = "正體中文模組啟用成功！\n但需要開新存檔才能完整生效。";
        private const string NewGame = "開新遊戲";

        // Waits for a launch in Traditional Chinese with no other panel open, so the text and its font
        // are the mod's.
        internal static void ShowOnce(MainMenuUIController menu)
        {
            if (menu == null || menu.confirmPanelUIManager == null || ES3.KeyExists(SaveKey) || !Variant.Traditional
                || !ZhTwMod.IsChinese(LocHelper.GetCurrentLocaleCode())
                || (menu.welcomePanel != null && menu.welcomePanel.activeSelf))
                return;
            var panel = menu.confirmPanelUIManager;
            // The panel is shared with the game's own questions: put its labels and alignment back on close.
            string yes = panel.yesTMP.text;
            string no = panel.noTMP.text;
            var alignment = panel.bodyTMP.alignment;
            void Restore()
            {
                panel.yesTMP.text = yes;
                panel.noTMP.text = no;
                panel.bodyTMP.alignment = alignment;
            }
            panel.OpenPanel(Text, () => { Restore(); menu.OnNewGameClick(); }, Restore);
            panel.yesTMP.text = NewGame;
            panel.noTMP.text = LocHelper.GetLocalizedMainMenu("load_cancel");
            panel.bodyTMP.alignment = TextAlignmentOptions.Center;
            ES3.Save(SaveKey, true);
        }
    }
}
