namespace ProbablyStolenZhTW
{
    // Tells the player once, in the main menu's own confirm panel, that saves started in Simplified
    // Chinese keep some of their text.
    internal static class Notice
    {
        private const string SaveKey = "seacucu.zh-tw.notice-shown";
        private const string Text = "正體中文模組啟用成功！\n但需要開新存檔才能完整生效。";

        // Waits for a launch in Traditional Chinese with no other panel open, so the text and its font
        // are the mod's.
        internal static void ShowOnce(MainMenuUIController menu)
        {
            if (menu == null || menu.confirmPanelUIManager == null || ES3.KeyExists(SaveKey) || !Variant.Traditional
                || !ZhTwMod.IsChinese(LocHelper.GetCurrentLocaleCode())
                || (menu.welcomePanel != null && menu.welcomePanel.activeSelf))
                return;
            var panel = menu.confirmPanelUIManager;
            string yes = panel.yesTMP.text;
            panel.OpenPanel(Text, () => panel.yesTMP.text = yes, hideNoButton: true);
            panel.yesTMP.text = LocHelper.GetLocalizedMainMenu("confirm");
            ES3.Save(SaveKey, true);
        }
    }
}
