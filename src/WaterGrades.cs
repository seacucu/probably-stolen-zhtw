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
    // Nothing here may patch a WaterFeatureHelper method, or let a patch be inlined into one: compiling such a
    // method runs WaterFeatureHelper's static initializer while the mod loads, before the game's localization
    // is ready, and the names are then kept in the table's Simplified Chinese for the whole session. Its
    // fields are read only from the non-inlined methods below, which run during play.
    internal static class WaterGrades
    {
        private const string Category = "CATEGORY_WATER_PURITY";

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static string NameOf(int purity) => WaterFeatureHelper.WATER_DISPLAY[WaterFeatureHelper.GetPurityArrayIndex(purity)];

        // GetWaterDisplayName reads the grade names through this list, looked up again on each read.
        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void Canonical(LocalizedLines lines, int index, ref string name)
        {
            if (ReferenceEquals(lines, WaterFeatureHelper.WATER_DISPLAY_LOCALIZED))
                name = WaterFeatureHelper.WATER_DISPLAY[index];
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
        private static void Postfix(LocalizedLines __instance, int index, ref string __result) =>
            WaterGrades.Canonical(__instance, index, ref __result);
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
