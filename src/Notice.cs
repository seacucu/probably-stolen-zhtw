using TMPro;
using UnityEngine;
using UnityEngine.Localization.Components;
using UnityEngine.UI;

namespace ProbablyStolenZhTW
{
    // Tells the player, in the main menu's own confirm panel, that saves started in Simplified Chinese
    // keep some of their text, and offers to start a new game. Shown at each launch until the player
    // leaves "don't show again" ticked.
    internal static class Notice
    {
        private const string SaveKey = "seacucu.zh-tw.notice-shown";
        private const string Text = "正體中文模組啟用成功！\n但需要開新存檔才能完整生效。";
        private const string NewGame = "開新遊戲";
        private const string DontShowAgain = "下次不再顯示";

        // The panel is 450×250 with 120×40 buttons; it grows to make room for the checkbox.
        private const float ExtraHeight = 60f;
        private const float ButtonWidth = 170f;
        private const float ButtonGap = 20f;
        private const float ButtonY = -110f;
        private const float ToggleY = -55f;
        private const float LabelWidth = 160f;

        // Waits for a launch in Traditional Chinese with no other panel open, so the text and its font
        // are the mod's.
        internal static void ShowOnce(MainMenuUIController menu)
        {
            if (menu == null || menu.confirmPanelUIManager == null || ES3.KeyExists(SaveKey) || !Variant.Traditional
                || !ZhTwMod.IsChinese(LocHelper.GetCurrentLocaleCode())
                || (menu.welcomePanel != null && menu.welcomePanel.activeSelf))
                return;
            var panel = menu.confirmPanelUIManager;
            var box = (RectTransform)panel.panel.transform;
            var yesButton = (RectTransform)panel.yesTMP.GetComponentInParent<Button>(true).transform;
            var noButton = (RectTransform)panel.noTMP.GetComponentInParent<Button>(true).transform;

            // The panel is shared with the game's own questions: put everything back on close.
            string yes = panel.yesTMP.text;
            string no = panel.noTMP.text;
            var alignment = panel.bodyTMP.alignment;
            var boxSize = box.sizeDelta;
            var yesRect = (yesButton.sizeDelta, yesButton.anchoredPosition);
            var noRect = (noButton.sizeDelta, noButton.anchoredPosition);
            var toggle = CreateToggle(menu.skipToggle, box, panel.bodyTMP.font);

            void Close()
            {
                if (toggle == null || toggle.isOn)
                    ES3.Save(SaveKey, true);
                if (toggle != null)
                    Object.Destroy(toggle.gameObject);
                panel.yesTMP.text = yes;
                panel.noTMP.text = no;
                panel.bodyTMP.alignment = alignment;
                box.sizeDelta = boxSize;
                (yesButton.sizeDelta, yesButton.anchoredPosition) = yesRect;
                (noButton.sizeDelta, noButton.anchoredPosition) = noRect;
            }

            panel.OpenPanel(Text, () => { Close(); menu.OnNewGameClick(); }, Close);
            panel.yesTMP.text = NewGame;
            panel.noTMP.text = LocHelper.GetLocalizedMainMenu("load_cancel");
            panel.bodyTMP.alignment = TextAlignmentOptions.Center;
            box.sizeDelta = boxSize + new Vector2(0f, ExtraHeight);
            float x = (ButtonWidth + ButtonGap) / 2f;
            yesButton.sizeDelta = new Vector2(ButtonWidth, yesButton.sizeDelta.y);
            yesButton.anchoredPosition = new Vector2(-x, ButtonY);
            noButton.sizeDelta = new Vector2(ButtonWidth, noButton.sizeDelta.y);
            noButton.anchoredPosition = new Vector2(x, ButtonY);
        }

        // A copy of the new game menu's "Skip Intro" checkbox, ticked, with its own label and none of
        // the original's listeners, localization or tooltip.
        private static Toggle CreateToggle(Toggle template, RectTransform parent, TMP_FontAsset font)
        {
            if (template == null)
                return null;
            var toggle = Object.Instantiate(template, parent, false);
            toggle.gameObject.SetActive(true);
            toggle.onValueChanged = new Toggle.ToggleEvent();
            foreach (var c in toggle.GetComponentsInChildren<Component>(true))
            {
                if (c is LocalizeStringEvent || c is TooltipContainer || c.GetType().Name == "TMPLocalizer")
                    Object.DestroyImmediate(c);
            }
            toggle.isOn = true;

            var label = toggle.GetComponentInChildren<TextMeshProUGUI>(true);
            var rect = (RectTransform)toggle.transform;
            // The box sits at the left edge; the label follows it, and the pair is centered in the panel.
            float boxWidth = 34f;
            rect.sizeDelta = new Vector2(boxWidth + LabelWidth, rect.sizeDelta.y);
            rect.anchoredPosition = new Vector2(0f, ToggleY);
            if (label != null)
            {
                label.text = DontShowAgain;
                label.font = font;
                label.fontSize = 20f;
                label.alignment = TextAlignmentOptions.Left;
                var labelRect = (RectTransform)label.transform;
                labelRect.sizeDelta = new Vector2(LabelWidth, labelRect.sizeDelta.y);
                labelRect.anchoredPosition = new Vector2(boxWidth + LabelWidth / 2f - rect.sizeDelta.x / 2f + 8f, labelRect.anchoredPosition.y);
            }
            return toggle;
        }
    }
}
