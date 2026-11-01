using System.Collections.Generic;
using HarmonyLib;
using UnityEngine.Localization.Settings;

namespace ProbablyStolenZhTW
{
    // Names the game writes in English in its code instead of looking them up in a string table.
    internal static class HardcodedNames
    {
        private static readonly Dictionary<string, string> Names = new Dictionary<string, string>
        {
            // The Hydroponic Farmer's second starting machine; the first is mech_item_old_hydroponic (舊水耕系統).
            ["Decrepit Hydroponic System"] = "破舊水耕系統",
        };

        internal static string Translate(string name) =>
            ZhTwMod.IsChinese(LocalizationSettings.SelectedLocale?.Identifier.Code) && Names.TryGetValue(name, out var text) ? text : name;

        // Passes each listed English literal in the method through Translate.
        internal static IEnumerable<CodeInstruction> Wrap(IEnumerable<CodeInstruction> instructions)
        {
            var translate = AccessTools.Method(typeof(HardcodedNames), nameof(Translate));
            bool found = false;
            foreach (var instruction in instructions)
            {
                yield return instruction;
                if (instruction.operand is string s && Names.ContainsKey(s))
                {
                    found = true;
                    yield return new CodeInstruction(System.Reflection.Emit.OpCodes.Call, translate);
                }
            }
            if (!found)
                ZhTwMod.Log.Warning("找不到程式寫死的英文名稱，遊戲可能已改寫這段程式");
        }
    }

    [HarmonyPatch(typeof(NewGameData), nameof(NewGameData.HandleInitialItem))]
    internal static class HandleInitialItemPatch
    {
        private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions) =>
            HardcodedNames.Wrap(instructions);
    }
}
