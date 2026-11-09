using HarmonyLib;

namespace ProbablyStolenZhTW
{
    // The game doubles the line width of a machine manual's tooltip. In Chinese that makes 52 characters a line,
    // far wider than every other tooltip (26); manuals keep the width of the other tooltips instead.
    // GameItemElement.GetTooltipBasic, the only caller of GetTooltipWidth, doubles its result for manuals, so the
    // width is halved here first. (A transpiler would compare System.Reflection.Emit opcodes, which the game's
    // mod scanner reports as undeclared dynamic code and refuses to activate.)
    [HarmonyPatch(typeof(PlantGeneticsHelper), nameof(PlantGeneticsHelper.GetTooltipWidth))]
    internal static class ManualWidth
    {
        private static void Postfix(GameItem item, ref int __result)
        {
            if (ZhTwMod.Active && item != null && item.IsTag("MACHINE_MANUAL_TAG"))
                __result /= 2;
        }
    }
}
