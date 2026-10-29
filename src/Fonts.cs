using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEngine;
using UnityEngine.Localization.Settings;
using UnityEngine.TextCore.LowLevel;

namespace ProbablyStolenZhTW
{
    // Swaps the game's Simplified Chinese TMP fonts for Traditional Chinese ones built from the mod's font files.
    internal static class Fonts
    {
        private sealed class Spec
        {
            public string File;
            public float Spacing;      // TMP normalSpacingOffset, in 1/100 em
            public bool WesternMetrics;
        }

        // Game font asset name -> replacement.
        private static readonly Dictionary<string, Spec> Specs = new Dictionary<string, Spec>
        {
            // Body text. At the game's small sizes Regular reads too heavy, and glyphs need a little air.
            ["NotoSansSC-VariableFont_wght"] = new Spec { File = "GenYoGothic2TW-L.otf", Spacing = 5, WesternMetrics = true },
            ["WDXLLubrifontSC-Regular SDF"] = new Spec { File = "WDXLLubrifontTC-Regular.ttf" },
            ["MaShanZheng-Regular SDF"] = new Spec { File = "LXGWWenKaiTC-Regular.ttf" },
        };

        private const string BodyFont = "NotoSansSC-VariableFont_wght";

        // The UI (e.g. the notebook's ruled lines) is laid out for Noto Sans; Source Han-style metrics are
        // ~6% taller, so Chinese lines drift off the rules unless the body font takes these line metrics.
        private const string MetricsFrom = "NotoSans-Medium SDF";

        private static readonly Dictionary<string, TMP_FontAsset> Built = new Dictionary<string, TMP_FontAsset>();

        // Built asset -> the game asset it replaces, for switching back to Simplified Chinese.
        private static readonly Dictionary<TMP_FontAsset, TMP_FontAsset> Originals = new Dictionary<TMP_FontAsset, TMP_FontAsset>();

        private static string _folder;

        internal static void Load()
        {
            _folder = ModHelper.GetModFolder(ZhTwMod.ModId);
            foreach (var spec in Specs.Values)
            {
                if (!ModHelper.ModFileExists(ZhTwMod.ModId, "fonts/" + spec.File))
                    throw new FileNotFoundException("找不到字型 fonts/" + spec.File);
            }
            foreach (var asset in Resources.FindObjectsOfTypeAll<TMP_FontAsset>())
                Replacement(asset);
            // Western fonts show the language menu entry and mod UI; let them fall back to the body font.
            if (Built.TryGetValue(BodyFont, out var body) && !TMP_Settings.fallbackFontAssets.Contains(body))
                TMP_Settings.fallbackFontAssets.Add(body);
        }

        // The Traditional Chinese asset for a game font asset, or null if the asset is not one we replace.
        internal static TMP_FontAsset Replacement(TMP_FontAsset original)
        {
            if (original == null || _folder == null || !Specs.TryGetValue(original.name, out var spec))
                return null;
            if (Built.TryGetValue(original.name, out var built))
                return built;
            try
            {
                built = Build(spec, original);
            }
            catch (System.Exception e)
            {
                ZhTwMod.Log.Error($"建立字型 {spec.File} 失敗，保留原字型：{e}");
                built = null;
            }
            Built[original.name] = built;
            if (built != null)
            {
                Originals[built] = original;
                if (Variant.Traditional)
                    ReplaceInFallbacks(original, built);
            }
            return built;
        }

        private static TMP_FontAsset Build(Spec spec, TMP_FontAsset original)
        {
            var font = new Font(Path.Combine(_folder, "fonts", spec.File)) { hideFlags = HideFlags.DontUnloadUnusedAsset };
            int pointSize = (int)original.faceInfo.pointSize;
            var asset = TMP_FontAsset.CreateFontAsset(font, pointSize, original.atlasPadding, original.atlasRenderMode,
                original.atlasWidth, original.atlasHeight, AtlasPopulationMode.Dynamic, true);
            if (asset == null)
                throw new System.InvalidOperationException("TMP 無法讀取字型檔");
            asset.name = Path.GetFileNameWithoutExtension(spec.File) + " (zh-TW)";
            asset.hideFlags = HideFlags.DontUnloadUnusedAsset;
            asset.normalSpacingOffset = spec.Spacing;

            // Keep the game's material settings (outline, softness, ...) with the new atlas.
            var material = new Material(original.material) { hideFlags = HideFlags.DontUnloadUnusedAsset };
            material.SetTexture(ShaderUtilities.ID_MainTex, asset.atlasTexture);
            asset.material = material;
            asset.atlasTexture.hideFlags = HideFlags.DontUnloadUnusedAsset;

            if (spec.WesternMetrics)
                CopyLineMetrics(asset);
            ZhTwMod.Log.Log($"字型 {spec.File} 取代 {original.name}（取樣 {pointSize}、行高 {asset.faceInfo.lineHeight:0.#}）");
            return asset;
        }

        private static void CopyLineMetrics(TMP_FontAsset asset)
        {
            TMP_FontAsset reference = null;
            foreach (var a in Resources.FindObjectsOfTypeAll<TMP_FontAsset>())
            {
                if (a.name == MetricsFrom)
                    reference = a;
            }
            if (reference == null)
            {
                ZhTwMod.Log.Warning($"找不到 {MetricsFrom}，內文字型沿用原本的行高");
                return;
            }
            var face = asset.faceInfo;
            var from = reference.faceInfo;
            float scale = face.pointSize / from.pointSize;
            face.lineHeight = from.lineHeight * scale;
            face.ascentLine = from.ascentLine * scale;
            face.descentLine = from.descentLine * scale;
            // TMP_FontAsset.faceInfo has no public setter; overwrite the serialized field instead.
            JsonUtility.FromJsonOverwrite("{\"m_FaceInfo\":" + JsonUtility.ToJson(face) + "}", asset);
        }

        private static void ReplaceInFallbacks(TMP_FontAsset from, TMP_FontAsset to)
        {
            foreach (var asset in Resources.FindObjectsOfTypeAll<TMP_FontAsset>())
            {
                var table = asset.fallbackFontAssetTable;
                int i = table?.IndexOf(from) ?? -1;
                if (i >= 0)
                    table[i] = to;
            }
            int g = TMP_Settings.fallbackFontAssets?.IndexOf(from) ?? -1;
            if (g >= 0)
                TMP_Settings.fallbackFontAssets[g] = to;
        }

        // Switches fallbacks and shown texts between the built fonts and the game's.
        internal static void Show(bool traditional)
        {
            foreach (var pair in Originals)
            {
                if (traditional)
                    ReplaceInFallbacks(pair.Value, pair.Key);
                else
                    ReplaceInFallbacks(pair.Key, pair.Value);
            }
            SwapTexts();
        }

        // Texts that were given the other variant's font before the mod loaded or the variant changed,
        // or that set it in the scene.
        internal static void SwapTexts()
        {
            if (!ZhTwMod.IsChinese(LocalizationSettings.SelectedLocale?.Identifier.Code))
                return;
            foreach (var text in Resources.FindObjectsOfTypeAll<TMP_Text>())
            {
                TMP_FontAsset replacement = null;
                if (Variant.Traditional)
                    replacement = Replacement(text.font);
                else if (text.font != null)
                    Originals.TryGetValue(text.font, out replacement);
                if (replacement != null)
                    text.font = replacement;
                else if (text.isActiveAndEnabled)
                    text.SetAllDirty();
            }
        }
    }
}
