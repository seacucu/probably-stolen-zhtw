using System.Collections.Generic;
using System.Runtime.CompilerServices;
using TMPro;
using UnityEngine;

namespace ProbablyStolenZhTW
{
    // Enlarges texts set in a replacement font that asks for it (Fonts.Spec.Grow, Fonts.Spec.MinSize), as far as
    // each text's box allows: TMP auto-size between the size the game chose and the enlarged size, tightening the
    // line spacing first where the box is too short. Texts that leave the font, or no longer need enlarging, get the
    // game's sizing back.
    internal static class TextGrow
    {
        private sealed class Sizing
        {
            public bool AutoSizing;
            public float Min, Max, Size, LineSpacingMax;

            public Sizing Copy() => (Sizing)MemberwiseClone();
        }

        // Texts currently grown -> the game's own sizing.
        private static readonly ConditionalWeakTable<TMP_Text, Sizing> Grown = new ConditionalWeakTable<TMP_Text, Sizing>();

        // Auto-size range given to grown texts -> the game's sizing it replaced. A copy the game makes of a grown text
        // carries that range but isn't in Grown; this gives it the game's sizing back instead of growing it twice.
        private static readonly Dictionary<(float, float), Sizing> GrownRanges = new Dictionary<(float, float), Sizing>();

        internal static void FontChanged(TMP_Text text)
        {
            bool grown = Grown.TryGetValue(text, out var sizing);
            Sizing original = null;
            bool copied = !grown && text.enableAutoSizing && GrownRanges.TryGetValue((text.fontSizeMin, text.fontSizeMax), out original);
            if (copied)
                sizing = original.Copy();
            else if (!grown)
            {
                sizing = new Sizing
                {
                    AutoSizing = text.enableAutoSizing,
                    Min = text.fontSizeMin,
                    Max = text.fontSizeMax,
                    Size = text.fontSize,
                    LineSpacingMax = text.lineSpacingAdjustment,
                };
            }
            float max = GrownMax(text, sizing);
            if (max <= GameMax(sizing))
            {
                if (grown || copied)
                    Restore(text, sizing);
                return;
            }
            if (!grown)
                Grown.Add(text, sizing);
            Apply(text, sizing, max);
        }

        // The game resized a grown text: grow from the new size. Turning auto-size off first means the game sizes the
        // text itself from now on.
        internal static void SizeChanged(TMP_Text text)
        {
            if (!Grown.TryGetValue(text, out var sizing))
                return;
            if (!text.enableAutoSizing)
                sizing.AutoSizing = false;
            else if (sizing.AutoSizing)
                return;
            sizing.Size = text.fontSize;
            FontChanged(text);
        }

        // A text's size in pixels on a 1080p screen, or 0 where that has no meaning (world-space canvases).
        // Counts the scale of every transform between the text and its canvas, which the game uses to enlarge pixel-art UI.
        internal static float PixelsAt1080(TMP_Text text, float size)
        {
            var canvas = text.canvas != null ? text.canvas.rootCanvas : null;
            if (canvas == null || canvas.renderMode == RenderMode.WorldSpace)
                return 0;
            float pixels = size * text.transform.lossyScale.y / canvas.transform.lossyScale.y * canvas.scaleFactor * 1080f / Screen.height;
            return float.IsNaN(pixels) || float.IsInfinity(pixels) ? 0 : pixels;
        }

        // Caps MinSize growth: a text measured mid-animation (scaled down) would otherwise fill its whole box.
        private const float MaxMinSizeGrow = 2;

        // How far auto-size may tighten a grown text's line spacing (em/100) before it shrinks the font: the body
        // font's line height of 1.36 em down to about 1.15 em.
        private const float LineSpacingTighten = -21;

        private static float GameMax(Sizing sizing) => sizing.AutoSizing ? sizing.Max : sizing.Size;

        private static float GrownMax(TMP_Text text, Sizing sizing)
        {
            var spec = Fonts.SpecOf(text.font);
            float gameMax = GameMax(sizing);
            if (spec == null)
                return gameMax;
            float max = gameMax * spec.Grow;
            float pixels = PixelsAt1080(text, gameMax);
            if (spec.MinSize > 0 && pixels > 0 && pixels < spec.MinSize)
                max = Mathf.Max(max, gameMax * Mathf.Min(spec.MinSize / pixels, MaxMinSizeGrow));
            return max;
        }

        private static void Apply(TMP_Text text, Sizing sizing, float max)
        {
            float min = sizing.AutoSizing ? sizing.Min : sizing.Size;
            GrownRanges[(min, max)] = sizing;
            text.fontSizeMin = min;
            text.fontSizeMax = max;
            text.lineSpacingAdjustment = Mathf.Min(sizing.LineSpacingMax, LineSpacingTighten);
            text.enableAutoSizing = true;
        }

        private static void Restore(TMP_Text text, Sizing sizing)
        {
            Grown.Remove(text);
            text.enableAutoSizing = sizing.AutoSizing;
            text.fontSizeMin = sizing.Min;
            text.fontSizeMax = sizing.Max;
            text.lineSpacingAdjustment = sizing.LineSpacingMax;
            text.fontSize = sizing.Size;
        }
    }
}
