using System.Runtime.CompilerServices;

namespace ProbablyStolenZhTW
{
    // Names the game looks up once, the first time its class is used, and keeps in static fields for the rest of
    // the launch: the water grades (WaterFeatureHelper) and the liquids a water scanner lists (Liquid). They stay in
    // whatever language was selected then, so a launch that started in another language, or a class first used
    // before the translation was in place, shows them in that language on every scanner and tooltip. Strings looks
    // them up again whenever the selected language's tables are ready.
    //
    // The water grade names also serve as the keys the game compares waters by (see WaterGrades); every comparison
    // reads the fields at the time it runs, so replacing all six together keeps them consistent.
    internal static class CachedNames
    {
        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void Refresh()
        {
            WaterFeatureHelper.PURE_WATER_DISPLAY = LocHelper.GetLocalizedMechanic("mech_condition_pure_water");
            WaterFeatureHelper.HIGH_QUALITY_WATER_DISPLAY = LocHelper.GetLocalizedMechanic("mech_condition_high_quality_water");
            WaterFeatureHelper.BASE_WATER_DISPLAY = LocHelper.GetLocalizedMechanic("mech_condition_basewater");
            WaterFeatureHelper.GHOSTWATER_DISPLAY = LocHelper.GetLocalizedMechanic("mech_condition_ghostwater");
            WaterFeatureHelper.RUST_WATER_DISPLAY = LocHelper.GetLocalizedMechanic("mech_condition_rustwater");
            WaterFeatureHelper.GUTTERFLOW_WATER_DISPLAY = LocHelper.GetLocalizedMechanic("mech_condition_gutterflow");
            // Same order as the game's initializer; the array is kept, as other code may hold it.
            var grades = WaterFeatureHelper.WATER_DISPLAY;
            grades[0] = WaterFeatureHelper.PURE_WATER_DISPLAY;
            grades[1] = WaterFeatureHelper.HIGH_QUALITY_WATER_DISPLAY;
            grades[2] = WaterFeatureHelper.BASE_WATER_DISPLAY;
            grades[3] = WaterFeatureHelper.GHOSTWATER_DISPLAY;
            grades[4] = WaterFeatureHelper.RUST_WATER_DISPLAY;
            grades[5] = WaterFeatureHelper.GUTTERFLOW_WATER_DISPLAY;

            // The game's own liquid list, built again only to read its names; the liquids in use keep everything else.
            foreach (var fresh in Liquid.InitLiquid())
            {
                if (Liquid.LiquidDict.TryGetValue(fresh.identifier, out var liquid))
                    liquid.displayName = fresh.displayName;
            }
        }
    }
}
