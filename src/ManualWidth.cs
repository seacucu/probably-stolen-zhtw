using System.Collections.Generic;
using System.Reflection.Emit;
using HarmonyLib;

namespace ProbablyStolenZhTW
{
    // The game doubles the line width of a machine manual's tooltip. In Chinese that makes 52 characters a line,
    // far wider than every other tooltip (26); manuals keep the width of the other tooltips instead.
    [HarmonyPatch(typeof(GameItemElement), "GetTooltipBasic")]
    internal static class ManualWidth
    {
        private static float Factor(float game) =>
            ZhTwMod.Active ? 1f : game;

        private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            var factor = AccessTools.Method(typeof(ManualWidth), nameof(Factor));
            bool afterManualTag = false, found = false;
            foreach (var instruction in instructions)
            {
                yield return instruction;
                if (instruction.operand is string s)
                    afterManualTag = s == "MACHINE_MANUAL_TAG";
                // The first float constant after the manual tag check is the manual's width factor.
                else if (afterManualTag && instruction.opcode == OpCodes.Ldc_R4)
                {
                    afterManualTag = false;
                    found = true;
                    yield return new CodeInstruction(OpCodes.Call, factor);
                }
            }
            if (!found)
                ZhTwMod.Log.Warning("找不到說明書提示框的寬度設定，遊戲可能已改寫這段程式");
        }
    }
}
