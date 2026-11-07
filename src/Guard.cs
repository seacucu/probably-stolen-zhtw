using System.Collections.Generic;
using System.Text;
using HarmonyLib;
using Newtonsoft.Json;
using TMPro;
using UnityEngine;
using UnityEngine.Localization;

namespace ProbablyStolenZhTW
{
    // Last line of defence: Simplified Chinese that reaches the screen anyway (text from a game update the
    // translation does not cover yet, names the game saved in Simplified, text built in code) is converted
    // with OpenCC s2tw right before TextMeshPro draws it. Only lines containing a character the translation
    // never uses are touched, so the translation itself is drawn exactly as written; scripts/s2tw.py builds
    // the tables and checks that at build time. Every mapping keeps the length, and only TextMeshPro's
    // drawing buffer changes: the game still reads back its own text.
    internal static class Guard
    {
        private const string DataFile = "data/s2tw.json";

        private static char[] _chars;
        private static Dictionary<string, string> _phrases;
        private static int _longest;
        private static bool _active;

        internal static void Load()
        {
            string json = ModHelper.ReadModFileText(ZhTwMod.ModId, DataFile);
            if (json == null)
                throw new System.IO.FileNotFoundException("找不到 " + DataFile);
            var data = JsonConvert.DeserializeObject<Dictionary<string, Dictionary<string, string>>>(json);
            var chars = new char[char.MaxValue + 1];
            foreach (var pair in data["chars"])
                chars[pair.Key[0]] = pair.Value[0];
            _phrases = data["phrases"];
            foreach (string key in _phrases.Keys)
                _longest = Mathf.Max(_longest, key.Length);
            _chars = chars;
            ZhTwMod.Log.Log($"簡體護欄：{data["chars"].Count} 字、{_phrases.Count} 詞");
        }

        // Follows the selected language; turning on redraws what is already on screen.
        internal static void LocaleChanged(Locale locale)
        {
            _active = locale != null && ZhTwMod.IsChinese(locale.Identifier.Code);
            if (!_active)
                return;
            foreach (var text in Resources.FindObjectsOfTypeAll<TMP_Text>())
            {
                if (text.isActiveAndEnabled)
                    text.ForceMeshUpdate(false, true);
            }
        }

        private static bool NeedsConversion(char c) => _chars[c] != '\0';

        internal static string Convert(string text)
        {
            if (!_active || _chars == null || text == null)
                return text;
            int i = 0;
            while (i < text.Length && !NeedsConversion(text[i]))
                i++;
            if (i == text.Length)
                return text;

            var output = text.ToCharArray();
            for (i = 0; i < text.Length;)
            {
                int n = Mathf.Min(_longest, text.Length - i);
                for (; n > 1; n--)
                {
                    if (_phrases.TryGetValue(text.Substring(i, n), out string phrase))
                    {
                        phrase.CopyTo(0, output, i, n);
                        break;
                    }
                }
                if (n > 1)
                {
                    i += n;
                    continue;
                }
                if (NeedsConversion(text[i]))
                    output[i] = _chars[text[i]];
                i++;
            }
            return new string(output);
        }

        internal static bool NeedsConversion(StringBuilder text)
        {
            if (!_active || _chars == null || text == null)
                return false;
            for (int i = 0; i < text.Length; i++)
            {
                if (NeedsConversion(text[i]))
                    return true;
            }
            return false;
        }

        internal static bool NeedsConversion(char[] text, int start, int length)
        {
            if (!_active || _chars == null || text == null)
                return false;
            int end = Mathf.Min(text.Length, start + length);
            for (int i = Mathf.Max(0, start); i < end; i++)
            {
                if (NeedsConversion(text[i]))
                    return true;
            }
            return false;
        }
    }

    // TextMeshPro copies every text it is given into its drawing buffer here, whichever API set it.
    [HarmonyPatch(typeof(TMP_Text), "PopulateTextBackingArray", typeof(string))]
    internal static class PopulateStringPatch
    {
        private static void Prefix(ref string __0) => __0 = Guard.Convert(__0);
    }

    [HarmonyPatch(typeof(TMP_Text), "PopulateTextBackingArray", typeof(string), typeof(int), typeof(int))]
    internal static class PopulateStringRangePatch
    {
        private static void Prefix(ref string __0) => __0 = Guard.Convert(__0);
    }

    [HarmonyPatch(typeof(TMP_Text), "PopulateTextBackingArray", typeof(StringBuilder), typeof(int), typeof(int))]
    internal static class PopulateStringBuilderPatch
    {
        private static void Prefix(ref StringBuilder __0)
        {
            if (Guard.NeedsConversion(__0))
                __0 = new StringBuilder(Guard.Convert(__0.ToString()));
        }
    }

    [HarmonyPatch(typeof(TMP_Text), "PopulateTextBackingArray", typeof(char[]), typeof(int), typeof(int))]
    internal static class PopulateCharArrayPatch
    {
        private static void Prefix(ref char[] __0, int __1, int __2)
        {
            if (Guard.NeedsConversion(__0, __1, __2))
                __0 = Guard.Convert(new string(__0)).ToCharArray();
        }
    }
}
