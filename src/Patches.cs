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
            if (locale != null && ZhTwMod.IsChinese(locale.Identifier.Code))
                __result = Fonts.Replacement(__result) ?? __result;
        }
    }

    [HarmonyPatch(typeof(MainMenuUIController), nameof(MainMenuUIController.HandleLocaleDropdown))]
    internal static class HandleLocaleDropdownPatch
    {
        private static void Postfix(MainMenuUIController __instance) => LanguageMenu.Rename(__instance);
    }

    // The game's Simplified Chinese option now shows Traditional Chinese, so it is labelled as such.
    internal static class LanguageMenu
    {
        private const string Original = "Simplified Chinese";
        private const string Label = "正體中文";

        internal static void Rename(MainMenuUIController menu)
        {
            var dropdown = menu != null ? menu.localeDropdown : null;
            if (dropdown == null)
                return;
            foreach (var option in dropdown.options)
            {
                if (option.text == Original)
                    option.text = Label;
            }
            dropdown.RefreshShownValue();
        }
    }
}
