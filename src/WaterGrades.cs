using HarmonyLib;

namespace ProbablyStolenZhTW
{
    // The game tells water grades apart by their translated names. Customers compare against names kept in
    // WaterFeatureHelper's static fields, filled once per launch, while a water's own name is looked up again
    // each time or comes from the save, in whatever language it was made in. Once the two differ (a language
    // change, a save started in Simplified Chinese), no water sells. Every grade name the game reads is
    // turned into the static one for its purity, so the comparisons hold whatever the language.
    internal static class WaterGrades
    {
        private const string Category = "CATEGORY_WATER_PURITY";

        internal static string NameOf(int purity) => WaterFeatureHelper.WATER_DISPLAY[WaterFeatureHelper.GetPurityArrayIndex(purity)];

        internal static void Canonical(ItemFeature feature, ItemCondition condition, ref string name)
        {
            if (feature.useCondition && condition != null && condition.category == Category)
                name = NameOf(condition.customValue);
        }
    }

    [HarmonyPatch(typeof(WaterFeatureHelper), nameof(WaterFeatureHelper.GetWaterDisplayName), typeof(int))]
    internal static class GetWaterDisplayNamePatch
    {
        private static void Postfix(int purity, ref string __result)
        {
            // Below the lowest grade the game shows "unknown water", which is no grade.
            var purities = WaterFeatureHelper.WATER_PURITIES;
            if (purity >= purities[purities.Length - 1])
                __result = WaterGrades.NameOf(purity);
        }
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
