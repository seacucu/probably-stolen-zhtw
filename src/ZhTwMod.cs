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
        internal static string Version;

        public void Init(ModManifest manifest)
        {
            Log = new ModLog(manifest);
            Version = manifest.ModVersion;
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

        // Whether the selected language is the one the mod translates.
        internal static bool Active => IsChinese(LocalizationSettings.SelectedLocale?.Identifier.Code);
    }

    // The mod folder is only known to ModHelper after Init returns, so data loads on the first frame.
    internal sealed class Runner : MonoBehaviour
    {
        private bool _refreshed;

        private void Start()
        {
            try
            {
                Strings.Load();
                Guard.Load();
                Fonts.Load();
                Changelog.Load(ZhTwMod.Version);
            }
            catch (Exception e)
            {
                ZhTwMod.Log.Error("載入失敗，本模組停用：" + e);
                Destroy(this);
                return;
            }
            SceneManager.sceneLoaded += (scene, mode) => Fonts.SwapTexts();
        }

        private void Update()
        {
            Changelog.Update();
            Strings.Update();
            // Text shown before the data loaded is still Simplified Chinese: redraw it once localization is up.
            if (_refreshed || !LocalizationSettings.InitializationOperation.IsDone)
                return;
            _refreshed = true;
            Strings.CheckSources();
            LocalizationSettings.SelectedLocaleChanged += Guard.LocaleChanged;
            Guard.LocaleChanged(LocalizationSettings.SelectedLocale);
            Strings.RefreshShownText();
            Fonts.SwapTexts();
            LanguageMenu.RenameOption(MainMenuUIController.Instance);
        }
    }
}
