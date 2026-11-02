using System.Runtime.CompilerServices;
using TMPro;
using UnityEngine;

namespace ProbablyStolenZhTW
{
    // Enlarges texts set in a replacement font that asks for it (Fonts.Spec.Grow, Fonts.Spec.MinSize), as far as
    // each text's box allows: TMP auto-size between the size the game chose and the enlarged size. Texts that leave
    // the font, or no longer need enlarging, get the game's sizing back.
    internal static class TextGrow
    {
        private sealed class Sizing
        {
            public bool AutoSizing;
            public float Min, Max, Size;
        }

        // Texts currently grown -> the game's own sizing.
        private static readonly ConditionalWeakTable<TMP_Text, Sizing> Grown = new ConditionalWeakTable<TMP_Text, Sizing>();

        internal static void FontChanged(TMP_Text text)
        {
            bool grown = Grown.TryGetValue(text, out var sizing);
            if (!grown)
            {
                sizing = new Sizing
                {
                    AutoSizing = text.enableAutoSizing,
                    Min = text.fontSizeMin,
                    Max = text.fontSizeMax,
                    Size = text.fontSize,
                };
            }
            float max = GrownMax(text, sizing);
            if (max <= GameMax(sizing))
            {
                if (grown)
                    Restore(text, sizing);
                return;
            }
            if (!grown)
                Grown.Add(text, sizing);
            Apply(text, sizing, max);
        }

        // The game resized a grown text: grow from the new size.
        internal static void SizeChanged(TMP_Text text)
        {
            if (!Grown.TryGetValue(text, out var sizing) || sizing.AutoSizing)
                return;
            sizing.Size = text.fontSize;
            FontChanged(text);
        }

        // A text's size in pixels on a 1080p screen, or 0 where that has no meaning (world-space canvases).
        internal static float PixelsAt1080(TMP_Text text, float size)
        {
            var canvas = text.canvas != null ? text.canvas.rootCanvas : null;
            if (canvas == null || canvas.renderMode == RenderMode.WorldSpace)
                return 0;
            return size * canvas.scaleFactor * 1080f / Screen.height;
        }

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
                max = Mathf.Max(max, gameMax * spec.MinSize / pixels);
            return max;
        }

        private static void Apply(TMP_Text text, Sizing sizing, float max)
        {
            text.fontSizeMin = sizing.AutoSizing ? sizing.Min : sizing.Size;
            text.fontSizeMax = max;
            text.enableAutoSizing = true;
        }

        private static void Restore(TMP_Text text, Sizing sizing)
        {
            Grown.Remove(text);
            text.enableAutoSizing = sizing.AutoSizing;
            text.fontSizeMin = sizing.Min;
            text.fontSizeMax = sizing.Max;
            text.fontSize = sizing.Size;
        }
    }
}
