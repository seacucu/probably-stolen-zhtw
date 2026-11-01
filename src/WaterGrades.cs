using System.Collections.Generic;
using System.Runtime.CompilerServices;
using HarmonyLib;

namespace ProbablyStolenZhTW
{
    // The game tells water grades apart by their translated names. Customers compare against names kept in
    // WaterFeatureHelper's static fields, filled once per launch, while a water's own name is looked up again
    // each time or comes from the save, in whatever language it was made in. Once the two differ (a language
    // change, a save started in Simplified Chinese), no water sells. Every grade name the game reads is
    // turned into the static one for its purity, so the comparisons hold whatever the language.
    //
    // WaterFeatureHelper's fields are read only once the game is reading a water grade itself, from the
    // non-inlined methods below. Touching the class any earlier (patching one of its methods, or reading a
    // field when any other list or feature is read) can run its static initializer before the game's
    // localization is ready, and the names are then kept in the table's Simplified Chinese for the session.
    internal static class WaterGrades
    {
        private const string Category = "CATEGORY_WATER_PURITY";

        // Grade name keys -> their index in WaterFeatureHelper.WATER_DISPLAY.
        private static readonly Dictionary<string, int> Keys = new Dictionary<string, int>
        {
            ["mech_condition_pure_water"] = 0,
            ["mech_condition_high_quality_water"] = 1,
            ["mech_condition_basewater"] = 2,
            ["mech_condition_ghostwater"] = 3,
            ["mech_condition_rustwater"] = 4,
            ["mech_condition_gutterflow"] = 5,
        };

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static string NameOf(int purity) => WaterFeatureHelper.WATER_DISPLAY[WaterFeatureHelper.GetPurityArrayIndex(purity)];

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static string NameAt(int grade) => WaterFeatureHelper.WATER_DISPLAY[grade];

        // GetWaterDisplayName reads the grade names through a LocalizedLines, looked up again on each read.
        internal static void Canonical(string table, string key, ref string name)
        {
            if (table == "Mechanic" && key != null && Keys.TryGetValue(key, out int grade))
                name = NameAt(grade);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void Canonical(ItemFeature feature, ItemCondition condition, ref string name)
        {
            if (feature.useCondition && condition != null && condition.category == Category)
                name = NameOf(condition.customValue);
        }
    }

    [HarmonyPatch(typeof(LocalizedLines), "Item", MethodType.Getter)]
    internal static class LocalizedLinesItemPatch
    {
        private static void Postfix(int index, string ___table, string[] ___keys, ref string __result) =>
            WaterGrades.Canonical(___table, ___keys[index], ref __result);
    }

    [HarmonyPatch(typeof(ItemFeature), nameof(ItemFeature.GetBaseRealDisplay))]
    internal static class GetBaseRealDisplayPatch
    {
        private static void Postfix(ItemFeature __instance, ref string __result) =>
            WaterGrades.Canonical(__instance, __instance.realCondition, ref __result);
    }

    [HarmonyPatch(typeof(ItemFeature), nameof(ItemFeature.GetBaseFakeDisplay))]
    internal static class GetBaseFakeDisplayPatch
    {
        private static void Postfix(ItemFeature __instance, ref string __result) =>
            WaterGrades.Canonical(__instance, __instance.fakeCondition, ref __result);
    }
}
