using System.Collections.Generic;
using Newtonsoft.Json;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Components;
using UnityEngine.Localization.Settings;
using UnityEngine.Localization.Tables;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace ProbablyStolenZhTW
{
    // Replaces entries of the game's Simplified Chinese string tables with the Traditional Chinese text.
    internal static class Strings
    {
        private const string DataFile = "data/strings.json";

        // Table collection -> key -> [FNV-1a hash of the English source, zh-TW text]. The translation follows
        // the English meaning, so only an English change makes it outdated; the Simplified Chinese wording
        // changes back and forth between game updates without changing the meaning.
        private static Dictionary<string, Dictionary<string, string[]>> _tables;

        // Table collection -> keys whose English source still matches the translation. The English text does
        // not change while the game runs, so each collection is checked once, when localization is up.
        private static readonly Dictionary<string, HashSet<string>> Current = new Dictionary<string, HashSet<string>>();

        // English tables still loading for the check.
        private static readonly Dictionary<string, AsyncOperationHandle<StringTable>> Loading =
            new Dictionary<string, AsyncOperationHandle<StringTable>>();

        // Table objects already patched (a reloaded table is a new object).
        private static readonly HashSet<StringTable> Patched = new HashSet<StringTable>();

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

        // Starts loading the English tables for the check; called once localization is initialized.
        internal static void CheckSources()
        {
            if (English == null)
            {
                ZhTwMod.Log.Warning("找不到英文語系，譯文全部改由簡體護欄轉換");
                return;
            }
            foreach (var name in _tables.Keys)
                Loading[name] = LocalizationSettings.StringDatabase.GetTableAsync(name, English);
        }

        // Checks the English tables that finished loading, outside the localization system's callbacks, where
        // loading or releasing a table is safe. Redraws the shown text once every collection is checked.
        internal static void Update()
        {
            if (Loading.Count == 0)
                return;
            foreach (var name in new List<string>(Loading.Keys))
            {
                var handle = Loading[name];
                // A language switch releases every loaded table, this one included.
                if (!handle.IsValid())
                {
                    Loading[name] = LocalizationSettings.StringDatabase.GetTableAsync(name, English);
                    continue;
                }
                if (!handle.IsDone)
                    continue;
                Loading.Remove(name);
                Current[name] = Check(name, handle.Result);
                // The English table was loaded only for the check; the game loads it again if English is chosen.
                if (LocalizationSettings.SelectedLocale != English)
                    LocalizationSettings.StringDatabase.ReleaseTable(name, English);
            }
            if (Loading.Count != 0)
                return;
            int current = 0;
            foreach (var keys in Current.Values)
                current += keys.Count;
            ZhTwMod.Log.Log($"英文原文比對完成：{current} 條譯文可套用");
            RefreshShownText();
        }

        private static Locale English => LocalizationSettings.AvailableLocales.GetLocale("en");

        private static HashSet<string> Check(string name, StringTable english)
        {
            var current = new HashSet<string>();
            if (english == null)
            {
                ZhTwMod.Log.Warning($"找不到英文字串表 {name}，這張表改由簡體護欄轉換");
                return current;
            }
            foreach (var row in _tables[name])
            {
                if (Hash(english.GetEntry(row.Key)?.Value) == row.Value[0])
                    current.Add(row.Key);
            }
            int changed = _tables[name].Count - current.Count;
            if (changed > 0)
                ZhTwMod.Log.Log($"字串表 {name}：英文原文已變動、改由簡體護欄轉換 {changed} 條");
            return current;
        }

        // Called before the game formats any entry of the table (Patches.GenerateLocalizedString). A table whose
        // English check has not finished is left as it is and patched on a later call.
        internal static void Apply(StringTable table)
        {
            if (_tables == null || table == null || !ZhTwMod.IsChinese(table.LocaleIdentifier.Code))
                return;
            if (!_tables.TryGetValue(table.TableCollectionName, out var rows)
                || !Current.TryGetValue(table.TableCollectionName, out var current))
                return;
            if (!Patched.Add(table))
                return;
            // An entry whose English source changed in a game update keeps the game's text until the translation
            // is updated, rather than showing an outdated line; Guard converts it on screen.
            foreach (StringTableEntry entry in table.Values)
            {
                if (entry != null && entry.Key != null && current.Contains(entry.Key))
                    entry.Value = rows[entry.Key][1];
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

        // FNV-1a 32-bit over UTF-16 code units, skipping spaces, tabs and line breaks (the game tidies those in
        // the English text without changing it); scripts/build_full.py writes the same hash.
        private static string Hash(string text)
        {
            uint h = 0x811C9DC5;
            foreach (char c in text ?? "")
            {
                if (c == ' ' || c == '\t' || c == '\r' || c == '\n')
                    continue;
                h ^= c;
                h *= 0x01000193;
            }
            return h.ToString("x8");
        }
    }
}
