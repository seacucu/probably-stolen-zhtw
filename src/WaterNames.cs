namespace ProbablyStolenZhTW
{
    // The game keeps the water grade names in static fields filled once, the first time WaterFeatureHelper is
    // used, and compares them with names it looks up again each time. If that first use came before the
    // translation (or in another language), customers stop accepting any water until the game restarts.
    internal static class WaterNames
    {
        internal static void Refresh()
        {
            WaterFeatureHelper.PURE_WATER_DISPLAY = LocHelper.GetLocalizedMechanic("mech_condition_pure_water");
            WaterFeatureHelper.HIGH_QUALITY_WATER_DISPLAY = LocHelper.GetLocalizedMechanic("mech_condition_high_quality_water");
            WaterFeatureHelper.BASE_WATER_DISPLAY = LocHelper.GetLocalizedMechanic("mech_condition_basewater");
            WaterFeatureHelper.GHOSTWATER_DISPLAY = LocHelper.GetLocalizedMechanic("mech_condition_ghostwater");
            WaterFeatureHelper.RUST_WATER_DISPLAY = LocHelper.GetLocalizedMechanic("mech_condition_rustwater");
            WaterFeatureHelper.GUTTERFLOW_WATER_DISPLAY = LocHelper.GetLocalizedMechanic("mech_condition_gutterflow");
            WaterFeatureHelper.WATER_DISPLAY = new[]
            {
                WaterFeatureHelper.PURE_WATER_DISPLAY,
                WaterFeatureHelper.HIGH_QUALITY_WATER_DISPLAY,
                WaterFeatureHelper.BASE_WATER_DISPLAY,
                WaterFeatureHelper.GHOSTWATER_DISPLAY,
                WaterFeatureHelper.RUST_WATER_DISPLAY,
                WaterFeatureHelper.GUTTERFLOW_WATER_DISPLAY,
            };
        }
    }
}
