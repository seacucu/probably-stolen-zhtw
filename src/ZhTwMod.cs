using System;
using UnityEngine;
using UnityEngine.Localization.Settings;
using UnityEngine.SceneManagement;

namespace ProbablyStolenZhTW
{
    // Entry point the game's ModLoader instantiates. Harmony patches (Patches.cs) are applied by the loader.
    public sealed class ZhTwMod : IMod
    {
        internal const string ModId = "seacucu.zh-tw";

        internal static ModLog Log;

        public void Init(ModManifest manifest)
        {
            Log = new ModLog(manifest);
            Variant.Load();
            var host = new GameObject("ZhTW");
            UnityEngine.Object.DontDestroyOnLoad(host);
            host.AddComponent<Runner>();
        }

        public void OnEnable()
        {
        }

        public void OnDisable()
        {
        }

        internal static bool IsChinese(string localeCode) => localeCode != null && localeCode.StartsWith("zh", StringComparison.Ordinal);
    }

    // Whether the zh locale shows the mod's Traditional Chinese or the game's own Simplified Chinese.
    // Picked in the language menu and saved with the game's settings, like its own locale choice.
    internal static class Variant
    {
        private const string SaveKey = "seacucu.zh-tw.traditional";

        internal static bool Traditional { get; private set; } = true;

        internal static void Load()
        {
            if (ES3.KeyExists(SaveKey))
                Traditional = ES3.Load<bool>(SaveKey);
        }

        internal static void Set(bool traditional)
        {
            if (traditional == Traditional)
                return;
            Traditional = traditional;
            ES3.Save(SaveKey, traditional);
            Strings.Show(traditional);
            Fonts.Show(traditional);
            ZhTwMod.Log.Log(traditional ? "改用正體中文" : "改用遊戲原本的簡體中文");
        }
    }

    // The mod folder is only known to ModHelper after Init returns, so data loads on the first frame.
    internal sealed class Runner : MonoBehaviour
    {
        // For comparing the two Chinese texts while working on the translation (F8 and F9 are the game's
        // console and debug panel, F12 is Steam's screenshot key).
        private const KeyCode SwitchKey = KeyCode.F7;

        private bool _refreshed;
        private bool _switchKey;

        private void Start()
        {
            try
            {
                Strings.Load();
                Fonts.Load();
            }
            catch (Exception e)
            {
                ZhTwMod.Log.Error("載入失敗，本模組停用：" + e);
                Destroy(this);
                return;
            }
            SceneManager.sceneLoaded += (scene, mode) => Fonts.SwapTexts();
            // Developer mode is the Mods screen switch for a local copy of the mod.
            _switchKey = ModTrust.Get(ZhTwMod.ModId)?.developerMode == true;
            if (_switchKey)
                ZhTwMod.Log.Log($"開發者模式：按 {SwitchKey} 切換正體／簡體中文");
        }

        private void Update()
        {
            if (!_refreshed)
            {
                // Text shown before the data loaded is still Simplified Chinese: redraw it once localization is up.
                if (!LocalizationSettings.InitializationOperation.IsDone)
                    return;
                _refreshed = true;
                Strings.RefreshShownText();
                Fonts.SwapTexts();
                LanguageMenu.AddOption(MainMenuUIController.Instance);
                Notice.ShowOnce(MainMenuUIController.Instance);
            }
            if (_switchKey && Input.GetKeyDown(SwitchKey) && !StoreUIManager.IsTextFieldFocused()
                && ZhTwMod.IsChinese(LocalizationSettings.SelectedLocale?.Identifier.Code))
            {
                Variant.Set(!Variant.Traditional);
                LanguageMenu.Sync(MainMenuUIController.Instance);
            }
        }
    }
}
