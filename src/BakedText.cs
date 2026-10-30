using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine.Localization.Settings;

namespace ProbablyStolenZhTW
{
    // The game composes some text from string table entries once and keeps it in the save (item names
    // and conditions, bills, news, customer names), so a save started in Simplified Chinese shows that
    // text as saved. When such text reaches a TextMeshPro component it is swapped for the translation of
    // the entry it came from. Only what is shown changes; the game's data and the save keep the original.
    internal static class BakedText
    {
        private const int MinPart = 2;      // entries looked for inside a longer text
        private const int MaxPart = 40;
        private const int MaxCached = 5000;

        private sealed class Template
        {
            public Regex Source;
            public string[] Indices;        // placeholder index of each capture group
            public string Text;
        }

        private static readonly Regex Placeholder = new Regex(@"\{(\d+)\}");

        // zh-CN entry text -> translation. A source that is also some entry's translation is left alone.
        private static readonly Dictionary<string, string> Exact = new Dictionary<string, string>();
        private static readonly HashSet<string> Translations = new HashSet<string>();
        private static readonly List<Template> Templates = new List<Template>();
        private static readonly HashSet<string> TemplateSources = new HashSet<string>();

        // Ideographs that occur in the zh-CN entries but in no translation: text with any of them is
        // still Simplified Chinese.
        private static readonly HashSet<char> SourceChars = new HashSet<char>();
        private static readonly HashSet<char> TranslationChars = new HashSet<char>();
        private static HashSet<char> _simplified = new HashSet<char>();
        private static bool _dirty;

        private static readonly Dictionary<string, string> Cache = new Dictionary<string, string>();

        internal static bool ChineseLocale;

        internal static void UpdateLocale() =>
            ChineseLocale = ZhTwMod.IsChinese(LocalizationSettings.SelectedLocale?.Identifier.Code);

        // Called by Strings.Apply for every entry whose translation applies.
        internal static void Add(string source, string text)
        {
            if (string.IsNullOrEmpty(source) || string.IsNullOrEmpty(text) || source == text)
                return;
            foreach (char c in source)
            {
                if (IsIdeograph(c))
                    SourceChars.Add(c);
            }
            foreach (char c in text)
            {
                if (IsIdeograph(c))
                    TranslationChars.Add(c);
            }
            Translations.Add(text);
            if (Placeholder.IsMatch(source))
                AddTemplate(source, text);
            else
            {
                if (!Exact.ContainsKey(source))
                    Exact[source] = text;
                // Some entries end in a stray space; composed text is matched without it.
                string trimmed = source.Trim();
                if (trimmed.Length > 0 && !Exact.ContainsKey(trimmed))
                    Exact[trimmed] = text.Trim();
            }
            _dirty = true;
            Cache.Clear();
        }

        // Only plain {0}-style placeholders; entries with plural or choose syntax are left out.
        private static void AddTemplate(string source, string text)
        {
            if (TemplateSources.Contains(source) || Placeholder.Replace(source, "").IndexOfAny(new[] { '{', '}' }) >= 0)
                return;
            var indices = new List<string>();
            var pattern = new StringBuilder("^");
            int last = 0;
            foreach (Match m in Placeholder.Matches(source))
            {
                pattern.Append(Regex.Escape(source.Substring(last, m.Index - last))).Append("(.+?)");
                indices.Add(m.Groups[1].Value);
                last = m.Index + m.Length;
            }
            pattern.Append(Regex.Escape(source.Substring(last))).Append('$');
            foreach (Match m in Placeholder.Matches(text))
            {
                if (!indices.Contains(m.Groups[1].Value))
                    return;
            }
            TemplateSources.Add(source);
            Templates.Add(new Template { Source = new Regex(pattern.ToString(), RegexOptions.Singleline), Indices = indices.ToArray(), Text = text });
        }

        // The text to show for a value the game sets on a TextMeshPro component.
        internal static string Convert(string value)
        {
            if (!Variant.Traditional || !ChineseLocale || string.IsNullOrEmpty(value))
                return value;
            // Item flavor text reaches the tooltip with a leading space and can span several lines.
            if (WholeEntry(value) is string whole)
                return whole;
            bool simplified = HasSimplified(value, 0, value.Length);
            // Without Simplified characters, only composed text can hold a whole entry to replace.
            if (!simplified && value.IndexOf('\n') < 0 && value.IndexOf('<') < 0)
                return value;
            if (Cache.TryGetValue(value, out var cached))
                return cached;
            string result = (simplified ? FromTemplate(value) : null) ?? ReplaceParts(value);
            // Unchanged composed text (counters, timers) changes often and is not worth keeping.
            if (simplified || result != value)
            {
                if (Cache.Count >= MaxCached)
                    Cache.Clear();
                Cache[value] = result;
            }
            if (Unmatched.Count < MaxUnmatched && HasSimplified(result, 0, result.Length) && Unmatched.Add(result))
                ZhTwMod.Log.Log("仍是簡體：" + (result.Length > 120 ? result.Substring(0, 120) + "…" : result).Replace("\n", "⏎"));
            return result;
        }

        // Text still Simplified after matching, logged once each so that missing entries can be found.
        private const int MaxUnmatched = 50;
        private static readonly HashSet<string> Unmatched = new HashSet<string>();

        private static string FromTemplate(string value)
        {
            foreach (var t in Templates)
            {
                var m = t.Source.Match(value);
                if (!m.Success)
                    continue;
                return Placeholder.Replace(t.Text, p =>
                {
                    int i = System.Array.IndexOf(t.Indices, p.Groups[1].Value);
                    return Convert(m.Groups[i + 1].Value);
                });
            }
            return null;
        }

        // Composed text (a tooltip with name, tags and description) is split at rich text tags and line
        // breaks; a run that is a whole entry is replaced even if its characters are the same in both
        // scripts (3D打印耗材), anything else only where it is still Simplified.
        private static string ReplaceParts(string value)
        {
            var sb = new StringBuilder(value.Length);
            int i = 0;
            while (i < value.Length)
            {
                if (value[i] == '<')
                {
                    int close = value.IndexOf('>', i);
                    if (close > i)
                    {
                        sb.Append(value, i, close - i + 1);
                        i = close + 1;
                        continue;
                    }
                }
                if (value[i] == '\n')
                {
                    sb.Append('\n');
                    i++;
                    continue;
                }
                int end = i;
                while (end < value.Length && value[end] != '\n' && !(value[end] == '<' && value.IndexOf('>', end) > end))
                    end++;
                AppendRun(sb, value.Substring(i, end - i));
                i = end;
            }
            return sb.ToString();
        }

        private static void AppendRun(StringBuilder sb, string run)
        {
            string trimmed = run.Trim();
            if (WholeEntry(run) is string whole)
                sb.Append(whole);
            else if (!HasSimplified(run, 0, run.Length))
                sb.Append(run);
            else if (FromTemplate(trimmed) is string line)
                sb.Append(run.Replace(trimmed, line));
            else
                ReplaceSimplified(sb, run);
        }

        // The translation of a text that is a whole entry apart from surrounding whitespace, which is kept.
        private static string WholeEntry(string value)
        {
            string trimmed = value.Trim();
            if (trimmed.Length == 0 || !Exact.TryGetValue(trimmed, out var text) || Translations.Contains(trimmed))
                return null;
            int start = value.IndexOf(trimmed, System.StringComparison.Ordinal);
            return value.Substring(0, start) + text + value.Substring(start + trimmed.Length);
        }

        // Longest known entry at each position, only where it is still Simplified.
        private static void ReplaceSimplified(StringBuilder sb, string value)
        {
            int i = 0;
            while (i < value.Length)
            {
                int found = 0;
                string text = null;
                for (int len = System.Math.Min(MaxPart, value.Length - i); len >= MinPart; len--)
                {
                    if (Exact.TryGetValue(value.Substring(i, len), out text) && HasSimplified(value, i, len))
                    {
                        found = len;
                        break;
                    }
                }
                if (found > 0)
                {
                    sb.Append(text);
                    i += found;
                }
                else
                {
                    sb.Append(value[i]);
                    i++;
                }
            }
        }

        private static bool HasSimplified(string value, int start, int length)
        {
            if (_dirty)
            {
                _simplified = new HashSet<char>(SourceChars);
                _simplified.ExceptWith(TranslationChars);
                _dirty = false;
            }
            for (int i = start; i < start + length; i++)
            {
                if (_simplified.Contains(value[i]))
                    return true;
            }
            return false;
        }

        private static bool IsIdeograph(char c) => (c >= '一' && c <= '鿿') || (c >= '㐀' && c <= '䶿');
    }
}
