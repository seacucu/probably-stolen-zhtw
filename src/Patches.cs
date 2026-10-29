using HarmonyLib;
using TMPro;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;
using UnityEngine.Localization.Tables;

namespace ProbablyStolenZhTW
{
    // Every localized string the game shows is formatted here, whether it comes from LocHelper,
    // LocalizedString or LocalizeStringEvent.
    [HarmonyPatch(typeof(LocalizedStringDatabase), "GenerateLocalizedString")]
    internal static class GenerateLocalizedStringPatch
    {
        private static void Prefix(StringTable table) => Strings.Apply(table);
    }

    // Every localized text component gets its font from here.
    [HarmonyPatch(typeof(FontLoader), nameof(FontLoader.GetFontByRole))]
    internal static class GetFontByRolePatch
    {
        private static void Postfix(Locale locale, ref TMP_FontAsset __result)
        {
            if (Variant.Traditional && locale != null && ZhTwMod.IsChinese(locale.Identifier.Code))
                __result = Fonts.Replacement(__result) ?? __result;
        }
    }

    [HarmonyPatch(typeof(MainMenuUIController), nameof(MainMenuUIController.HandleLocaleDropdown))]
    internal static class HandleLocaleDropdownPatch
    {
        private static void Postfix(MainMenuUIController __instance) => LanguageMenu.AddOption(__instance);
    }

    [HarmonyPatch(typeof(MainMenuUIController), nameof(MainMenuUIController.OnLocaleChanged))]
    internal static class OnLocaleChangedPatch
    {
        private static void Prefix(ref int localeIndex) => LanguageMenu.Select(ref localeIndex);
    }

    // Adds a Traditional Chinese option after the game's languages. Both Chinese options use the game's
    // zh locale; which one is picked decides whether it shows the mod's text or the game's own.
    internal static class LanguageMenu
    {
        private const string Chinese = "Simplified Chinese";
        private const string Label = "正體中文";
        private static int _chinese = -1;
        private static int _added = -1;

        internal static void AddOption(MainMenuUIController menu)
        {
            var dropdown = menu != null ? menu.localeDropdown : null;
            if (dropdown == null)
                return;
            var options = dropdown.options;
            _chinese = options.FindIndex(o => o.text == Chinese);
            if (_chinese < 0)
                return;
            _added = options.FindIndex(o => o.text == Label);
            if (_added < 0)
            {
                _added = options.Count;
                options.Add(new TMP_Dropdown.OptionData(Label));
            }
            if (dropdown.value == _chinese && Variant.Traditional)
                dropdown.SetValueWithoutNotify(_added);
            dropdown.RefreshShownValue();
        }

        // The game ignores indices past its own list, so the added option selects the zh locale.
        internal static void Select(ref int index)
        {
            if (_chinese < 0)
                return;
            if (index == _added)
            {
                Variant.Set(true);
                index = _chinese;
            }
            else if (index == _chinese)
            {
                Variant.Set(false);
            }
        }
    }
}
