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

    // The game and the mod both assign fonts through this setter, so every text that takes a growing font passes here.
    [HarmonyPatch(typeof(TMP_Text), nameof(TMP_Text.font), MethodType.Setter)]
    internal static class FontSetterPatch
    {
        private static void Postfix(TMP_Text __instance) => TextGrow.FontChanged(__instance);
    }

    [HarmonyPatch(typeof(TMP_Text), nameof(TMP_Text.fontSize), MethodType.Setter)]
    internal static class FontSizeSetterPatch
    {
        private static void Postfix(TMP_Text __instance) => TextGrow.SizeChanged(__instance);
    }

    [HarmonyPatch(typeof(MainMenuUIController), nameof(MainMenuUIController.HandleLocaleDropdown))]
    internal static class HandleLocaleDropdownPatch
    {
        private static void Postfix(MainMenuUIController __instance) => LanguageMenu.RenameOption(__instance);
    }

    // The game's zh locale shows the mod's text, so its menu entry is renamed to match.
    internal static class LanguageMenu
    {
        private const string Chinese = "Simplified Chinese";
        private const string Label = "正體中文";

        internal static void RenameOption(MainMenuUIController menu)
        {
            var dropdown = menu != null ? menu.localeDropdown : null;
            if (dropdown == null)
                return;
            var option = dropdown.options.Find(o => o.text == Chinese);
            if (option == null)
                return;
            option.text = Label;
            dropdown.RefreshShownValue();
        }
    }
}
