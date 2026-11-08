using System;
using System.Collections.Generic;
using HarmonyLib;

namespace ProbablyStolenZhTW
{
    // The game writes a machine's manual into the save when the machine is made, so a manual keeps the wording
    // of the version it was made with. On load, every manual that sits in its machine is rewritten from that
    // machine's own manual, in the current language and wording, and gets the manual tag older saves lack.
    [HarmonyPatch(typeof(SaveManager), nameof(SaveManager.DecodeNodes))]
    internal static class MachineManuals
    {
        // Machine identifier to the manual the game puts in it (PreBuiltItemHelper.Create*).
        private static readonly Dictionary<string, Func<GameItem>> Manuals = new Dictionary<string, Func<GameItem>>
        {
            ["wine_rack"] = MachineWineRack.CreateNote,
            ["water_purifier"] = MachinePurifier.CreateNote,
            ["hydroponic"] = MachineHydroponic.CreateNote,
            ["plant_dna_manipulator"] = MachineDnaManipulator.CreateNote,
            ["printer"] = MachinePrinter.CreateNote,
            ["moisture_farm"] = MachineMoistureFarm.CreateNote,
            ["furnace"] = MachineFurnace.CreateNote,
            ["security_alarm"] = MachineAlarm.CreateNote,
            ["mirage_projector"] = MachineProjector.CreateNote,
            ["deal_maker"] = MachineDealMaker.CreateNote,
            ["evaporator"] = MachineEvaporator.CreateNote,
        };

        private static void Postfix(GameItem __result)
        {
            if (__result == null)
                return;
            foreach (var item in __result.FindAllChildItems())
            {
                // Manuals made before the game tagged them are known only as an important post-it in a machine.
                if (item.identifier != "postit" || !item.IsTag("IMPORTANT_TAG"))
                    continue;
                var machine = item.parentInventory?.GetParentItem();
                if (machine == null || !Manuals.TryGetValue(machine.identifier, out var create))
                    continue;
                var manual = create();
                item.name = manual.name;
                item.flavorText = manual.flavorText;
                item.shortDescription = manual.shortDescription;
                item.EnableTag(MachineHelper.MANUAL_TAG);
            }
        }
    }
}
