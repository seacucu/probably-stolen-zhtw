using System.Collections.Generic;
using System.Text.RegularExpressions;
using HarmonyLib;
using UnityEngine.Localization.Settings;

namespace ProbablyStolenZhTW
{
    // Paragraph indents the game writes as runs of half-width spaces in front of localized text.
    // They don't line up with Chinese characters, so Chinese keeps only the newspaper's, as two ideographic spaces.
    internal static class Indents
    {
        internal static bool Chinese => ZhTwMod.IsChinese(LocalizationSettings.SelectedLocale?.Identifier.Code);

        internal static string News(string indent) => Chinese ? "\u3000\u3000" : indent;

        internal static string Drop(string indent) => Chinese ? "" : indent;

        // Passes the method's first occurrence of the indent literal through News or Drop.
        internal static IEnumerable<CodeInstruction> Wrap(IEnumerable<CodeInstruction> instructions, string indent, string replace)
        {
            var method = AccessTools.Method(typeof(Indents), replace);
            bool found = false;
            foreach (var instruction in instructions)
            {
                yield return instruction;
                if (!found && instruction.operand is string s && s == indent)
                {
                    found = true;
                    yield return new CodeInstruction(System.Reflection.Emit.OpCodes.Call, method);
                }
            }
            if (!found)
                ZhTwMod.Log.Warning("找不到程式加的段首空格，遊戲可能已改寫這段程式");
        }
    }

    [HarmonyPatch(typeof(NewsUIElement), nameof(NewsUIElement.PopulateUI))]
    internal static class NewsIndentPatch
    {
        private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions) =>
            Indents.Wrap(instructions, "     ", nameof(Indents.News));
    }

    // The new-game description of the Fence start.
    [HarmonyPatch(typeof(MainMenuUIController), nameof(MainMenuUIController.OnFenceClick))]
    internal static class FenceIndentPatch
    {
        private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions) =>
            Indents.Wrap(instructions, "   ", nameof(Indents.Drop));
    }

    [HarmonyPatch(typeof(GameOverUIManager), nameof(GameOverUIManager.DisplayUI))]
    internal static class GameOverIndentPatch
    {
        private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions) =>
            Indents.Wrap(instructions, "  ", nameof(Indents.Drop));
    }

    // AddEmptyLine's spacer never gets a line of its own: the tooltip's next line continues after it, so it only shows
    // as a space. Chinese drops the spacer and just starts a new line.
    [HarmonyPatch(typeof(RichTextBuilder), nameof(RichTextBuilder.AddEmptyLine))]
    internal static class AddEmptyLinePatch
    {
        private static bool Prefix(RichTextBuilder __instance, ref RichTextBuilder __result, ref string ___nextBegin)
        {
            if (!Indents.Chinese)
                return true;
            ___nextBegin = "\n";
            __result = __instance;
            return false;
        }
    }

    // Notes keep the text they were created with in the save, so notes from before the translations lost their
    // indents still carry them.
    [HarmonyPatch(typeof(PaperUIManager), nameof(PaperUIManager.OpenUI))]
    internal static class PaperIndentPatch
    {
        private static readonly Regex LineIndent = new Regex(@"^[ \t\u3000]+", RegexOptions.Multiline);

        private static void Prefix(ref string body)
        {
            if (body != null && Indents.Chinese)
                body = LineIndent.Replace(body, "");
        }
    }
}
