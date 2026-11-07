using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using UnityEngine;
using UnityEngine.UI;

namespace ProbablyStolenZhTW
{
    // The mod's version history (data/changelog.txt) in a dialog built with the game's own mod UI, the
    // same kind as the mod screen's warnings. It opens once after each mod update (or first install),
    // after the game's own welcome panel, and again from the main menu's thank-you button.
    internal static class Changelog
    {
        private const string DataFile = "data/changelog.txt";
        private const string SeenKey = "seacucu.zh-tw.changelog-seen";
        private const float Width = 760f;
        private const float ScrollHeight = 480f;
        private const float ItemSizeStep = 4f;
        private static readonly Color BackdropColor = new Color(0f, 0f, 0f, 0.7f);

        private sealed class Entry
        {
            public string Version;
            public string Heading;
            public string Text;     // the items; null for a version without any
        }

        private static List<Entry> _entries;
        private static string _version;
        private static GameObject _backdrop;
        private static bool _menuReady;

        internal static void Load(string version)
        {
            string text = ModHelper.ReadModFileText(ZhTwMod.ModId, DataFile);
            if (text == null)
                throw new System.IO.FileNotFoundException("找不到 " + DataFile);
            _version = version;
            _entries = new List<Entry>();
            var items = new List<string>();
            string heading = null;
            void Flush()
            {
                if (heading != null)
                    _entries.Add(new Entry
                    {
                        Version = heading.Split(' ')[0],
                        Heading = $"<b>v{heading}</b>",
                        Text = items.Count == 0 ? null : string.Join("\n", items.Select(i => "・" + i)),
                    });
                items.Clear();
            }
            foreach (string raw in text.Split('\n'))
            {
                string line = raw.TrimEnd('\r');
                if (line.Length == 0 || line.StartsWith("#"))
                    continue;
                if (line.StartsWith("- "))
                {
                    items.Add(line.Substring(2));
                    continue;
                }
                Flush();
                heading = line;
            }
            Flush();
            if (_entries.Count == 0 || _entries[0].Version != version)
                throw new System.IO.InvalidDataException($"{DataFile} 沒有 {version} 的紀錄");
        }

        // The main menu has run its own start-up panels (HandleWelcomePanel); a new menu scene starts over.
        internal static void MenuReady(MainMenuUIController menu)
        {
            _menuReady = true;
            ThanksButton.Add(menu);
        }

        // Called every frame: opens the dialog once the player sees the mod's text and nothing else is open.
        internal static void Update()
        {
            var menu = MainMenuUIController.Instance;
            if (!_menuReady || menu == null || _entries == null || _backdrop != null
                || !ZhTwMod.IsChinese(LocHelper.GetCurrentLocaleCode()))
                return;
            string seen = ES3.KeyExists(SeenKey) ? ES3.Load<string>(SeenKey) : null;
            if (seen == _version || IsBusy(menu))
                return;
            // Every version since the last one seen; a first install sees the latest one with items.
            int count = _entries.FindIndex(e => e.Version == seen);
            var shown = count > 0
                ? _entries.Take(count).Where(e => e.Text != null).ToList()
                : _entries.Where(e => e.Text != null).Take(1).ToList();
            if (shown.Count == 0)
                ES3.Save(SeenKey, _version);
            else
                Show(menu, seen == null ? $"正體中文模組 {_version} 版已啟用" : $"正體中文模組已更新到 {_version} 版", shown);
        }

        internal static void ShowAll(MainMenuUIController menu)
        {
            if (_backdrop == null && _entries != null)
                Show(menu, "感謝使用海餐翻譯的正體中文模組", _entries.Where(e => e.Text != null).ToList());
        }

        private static bool IsBusy(MainMenuUIController menu)
        {
            bool Open(GameObject panel) => panel != null && panel.activeInHierarchy;
            return Open(menu.welcomePanel) || Open(menu.newGamePanel) || Open(menu.creditPanel) || Open(menu.modPanel)
                || (menu.confirmPanelUIManager != null && Open(menu.confirmPanelUIManager.panel))
                || ModWarningUI.IsOpen || ModEnablePromptUI.IsOpen || ModRefusalUI.IsOpen;
        }

        private static void Show(MainMenuUIController menu, string title, List<Entry> entries)
        {
            var ui = CustomUIManager.Instance;
            var canvas = menu.confirmPanelUIManager != null ? menu.confirmPanelUIManager.panel.GetComponentInParent<Canvas>(true) : null;
            if (ui == null || canvas == null)
                return;

            // Laid out like the game's ModWarningUI: a dimmed backdrop and a framed panel in the middle.
            _backdrop = new GameObject("zh-tw_changelog", typeof(RectTransform), typeof(Image));
            var rect = (RectTransform)_backdrop.transform;
            rect.SetParent(canvas.rootCanvas.transform, false);
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            rect.SetAsLastSibling();
            var image = _backdrop.GetComponent<Image>();
            image.color = BackdropColor;
            image.raycastTarget = true;

            var content = new GameObject("content", typeof(RectTransform));
            var contentRect = (RectTransform)content.transform;
            contentRect.SetParent(rect, false);
            contentRect.anchorMin = contentRect.anchorMax = contentRect.pivot = new Vector2(0.5f, 0.5f);
            contentRect.sizeDelta = new Vector2(Width, 0f);
            content.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            var builder = ui.BuildInto(contentRect, true, "zh-tw_changelog");
            builder.BeginPanel(Width, -1f, TextAnchor.UpperLeft, 24f, 12f, "frame");
            builder.AddHeader(title, "title");
            bool scroll = entries.Count > 1;
            if (scroll)
                builder.BeginScroll(ScrollHeight);
            foreach (var entry in entries)
            {
                builder.BeginColumn(2f);
                builder.AddLabel(entry.Heading);
                builder.AddLabel(entry.Text);
                // Items two sizes below the version line.
                builder.Last.text.fontSize -= ItemSizeStep;
                builder.End();
            }
            if (scroll)
                builder.End();
            builder.BeginRow(0f, TextAnchor.MiddleCenter);
            builder.AddFlexibleSpace();
            builder.AddButton("知道了", () => Close(), "confirm");
            builder.Last.SetFixedWidth(260f);
            builder.AddFlexibleSpace();
            builder.End();
            builder.End();
            menu.OnAnyButtonClick();
        }

        private static void Close()
        {
            ES3.Save(SeenKey, _version);
            if (_backdrop != null)
                Object.Destroy(_backdrop);
            _backdrop = null;
        }
    }

    [HarmonyPatch(typeof(MainMenuUIController), nameof(MainMenuUIController.HandleWelcomePanel))]
    internal static class HandleWelcomePanelPatch
    {
        private static void Postfix(MainMenuUIController __instance) => Changelog.MenuReady(__instance);
    }
}
