using System.Collections.Generic;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.Localization.Components;
using UnityEngine.Localization.Tables;

namespace ProbablyStolenZhTW
{
    // Replaces entries of the game's Simplified Chinese string tables with the Traditional Chinese text.
    internal static class Strings
    {
        private const string DataFile = "data/strings.json";

        // Table collection -> key -> [FNV-1a hash of the zh-CN source, zh-TW text].
        private static Dictionary<string, Dictionary<string, string[]>> _tables;

        private struct Swap
        {
            public StringTableEntry Entry;
            public string Source;
            public string Text;
        }

        // Table objects already patched (a reloaded table is a new object) -> their translated entries,
        // kept with both texts so the language menu can switch between them.
        private static readonly Dictionary<StringTable, List<Swap>> Patched = new Dictionary<StringTable, List<Swap>>();

        internal static void Load()
        {
            string json = ModHelper.ReadModFileText(ZhTwMod.ModId, DataFile);
            if (json == null)
                throw new System.IO.FileNotFoundException("找不到 " + DataFile);
            _tables = JsonConvert.DeserializeObject<Dictionary<string, Dictionary<string, string[]>>>(json);
            int count = 0;
            foreach (var rows in _tables.Values)
                count += rows.Count;
            ZhTwMod.Log.Log($"譯文 {count} 條（{_tables.Count} 張字串表）");
        }

        // Called before the game formats any entry of the table (Patches.GenerateLocalizedString).
        internal static void Apply(StringTable table)
        {
            if (_tables == null || table == null || !ZhTwMod.IsChinese(table.LocaleIdentifier.Code))
                return;
            if (Patched.ContainsKey(table))
                return;
            var swaps = new List<Swap>();
            Patched[table] = swaps;
            if (!_tables.TryGetValue(table.TableCollectionName, out var rows))
                return;
            int applied = 0, changed = 0;
            foreach (StringTableEntry entry in table.Values)
            {
                if (entry == null || entry.Key == null || !rows.TryGetValue(entry.Key, out var row))
                    continue;
                // An entry whose source text changed in a game update keeps the game's text until the
                // translation is updated, rather than showing an outdated line.
                if (Hash(entry.Value) == row[0])
                {
                    swaps.Add(new Swap { Entry = entry, Source = entry.Value, Text = row[1] });
                    if (Variant.Traditional)
                        entry.Value = row[1];
                    applied++;
                }
                else
                {
                    changed++;
                }
            }
            ZhTwMod.Log.Log($"字串表 {table.TableCollectionName}：套用 {applied} 條" + (changed > 0 ? $"，原文已變動而略過 {changed} 條" : ""));
        }

        // Switches every patched entry between the translation and the game's text.
        internal static void Show(bool traditional)
        {
            var unloaded = new List<StringTable>();
            foreach (var pair in Patched)
            {
                if (pair.Key == null)
                {
                    unloaded.Add(pair.Key);
                    continue;
                }
                foreach (var swap in pair.Value)
                    swap.Entry.Value = traditional ? swap.Text : swap.Source;
            }
            foreach (var table in unloaded)
                Patched.Remove(table);
            RefreshShownText();
            // What the game itself redraws on a language change (the locale stays zh, so it does not).
            foreach (var d in Resources.FindObjectsOfTypeAll<DropdownLocalizer>())
            {
                if (d.isActiveAndEnabled)
                    d.UpdateOptions();
            }
        }

        internal static void RefreshShownText()
        {
            foreach (var e in Resources.FindObjectsOfTypeAll<LocalizeStringEvent>())
            {
                if (e.isActiveAndEnabled)
                    e.RefreshString();
            }
        }

        // FNV-1a 32-bit over UTF-16 code units; scripts/build_full.py writes the same hash.
        private static string Hash(string text)
        {
            uint h = 0x811C9DC5;
            foreach (char c in text ?? "")
            {
                h ^= c;
                h *= 0x01000193;
            }
            return h.ToString("x8");
        }
    }
}
