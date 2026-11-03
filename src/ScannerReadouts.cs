using System.Collections.Generic;
using System.Text.RegularExpressions;
using HarmonyLib;
using TMPro;

namespace ProbablyStolenZhTW
{
    // The scanners' readouts stack the tag, the purity and the grade on lines of their own, which leaves Chinese too
    // little height to be legible. Chinese gives the test result a heading and one line of grade then purity, like
    // the contaminant list below it, and puts the label on a single line.
    [HarmonyPatch(typeof(TMP_Text), nameof(TMP_Text.text), MethodType.Setter)]
    internal static class ScannerReadoutPatch
    {
        private const string TestedHeading = "檢驗結果";
        private const string LabelTag = "[標籤]";

        private static readonly Regex Tag = new Regex(@"^\[[^\]]*\]");
        private static readonly Regex Percent = new Regex(@"^[\d.,]+%$");

        private static void Prefix(TMP_Text __instance, ref string value)
        {
            bool tested = IsTested(__instance);
            if (!tested && !IsLabel(__instance))
                return;
            if (string.IsNullOrEmpty(value) || !Indents.Chinese)
                return;
            // Matching the label copies the test result into it, so either form can arrive in either box.
            string body;
            if (value.StartsWith(TestedHeading))
                body = value.Substring(TestedHeading.Length);
            else if (Tag.IsMatch(value))
                body = Tag.Replace(value, "");
            else
                return;
            var parts = new List<string>();
            var percents = new List<string>();
            foreach (string line in body.Split('\n'))
            {
                string part = line.Trim();
                if (part.Length > 0)
                    (Percent.IsMatch(part) ? percents : parts).Add(part);
            }
            parts.AddRange(percents);
            string result = string.Join(" ", parts);
            value = tested ? TestedHeading + "\n" + result : LabelTag + " " + result;
        }

        private static bool IsTested(TMP_Text text)
        {
            var water = WaterScannerUIManager.Instance;
            var chem = ChemScannerUIManager.Instance;
            var strip = WaterStripUIManager.Instance;
            return (water is object && ReferenceEquals(text, water.testedTMP))
                || (chem is object && ReferenceEquals(text, chem.testedTMP))
                || (strip is object && ReferenceEquals(text, strip.testedTMP));
        }

        private static bool IsLabel(TMP_Text text)
        {
            var water = WaterScannerUIManager.Instance;
            var chem = ChemScannerUIManager.Instance;
            var strip = WaterStripUIManager.Instance;
            return (water is object && ReferenceEquals(text, water.labelTMP))
                || (chem is object && ReferenceEquals(text, chem.labelTMP))
                || (strip is object && ReferenceEquals(text, strip.labelTMP));
        }
    }
}
