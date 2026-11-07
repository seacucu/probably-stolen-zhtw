using TMPro;
using UnityEngine;
using UnityEngine.Localization.Components;
using UnityEngine.UI;

namespace ProbablyStolenZhTW
{
    // A copy of the main menu's "Join our Discord" button, stacked right above it, that opens the mod's
    // version history. The Discord logo becomes the mod's logo, and the button's Discord blue becomes the
    // logo's yellow.
    internal static class ThanksButton
    {
        private const string Name = "ZhTwThanksButton";
        private const string Text = "感謝使用\n海餐翻譯模組！";
        private const string LogoFile = "images/thanks-logo.png";
        // The logo's yellow; the Discord button's blue is anything clearly bluer than it is red or green.
        private static readonly Color Yellow = new Color32(0xFD, 0xE3, 0x69, 0xFF);
        private const float MinBlueness = 0.25f;
        private const float LogoSize = 40f;
        private const float TextGap = 6f;

        private static Sprite _logo;
        private static Sprite _background;

        internal static void Add(MainMenuUIController menu)
        {
            var discord = menu.discordButton;
            if (discord == null || !discord.activeSelf || discord.transform.parent.Find(Name) != null)
                return;
            var copy = Object.Instantiate(discord, discord.transform.parent, false);
            copy.name = Name;
            // The buttons sit in a layout group that stacks them in sibling order.
            copy.transform.SetSiblingIndex(discord.transform.GetSiblingIndex());

            var button = copy.GetComponent<Button>();
            button.onClick = new Button.ButtonClickedEvent();
            button.onClick.AddListener(() =>
            {
                menu.OnAnyButtonClick();
                Changelog.ShowAll(menu);
            });

            // TMPLocalizer requires LocalizeStringEvent, so it goes first.
            foreach (var c in copy.GetComponentsInChildren<Component>(true))
            {
                if (c.GetType().Name == "TMPLocalizer")
                    Object.DestroyImmediate(c);
            }
            foreach (var c in copy.GetComponentsInChildren<LocalizeStringEvent>(true))
                Object.DestroyImmediate(c);

            var background = copy.GetComponent<Image>();
            if (_background == null)
                _background = Recolor(background.sprite);
            background.sprite = _background;

            // The logo is round, so it is centered on the Discord icon's middle and in the button's height,
            // with a margin inside the frame.
            RectTransform logo = null;
            foreach (var image in copy.GetComponentsInChildren<Image>(true))
            {
                if (image == background)
                    continue;
                image.sprite = _logo != null ? _logo : (_logo = LoadLogo());
                image.preserveAspect = true;
                logo = (RectTransform)image.transform;
                float centerX = logo.anchoredPosition.x - logo.sizeDelta.x * (logo.pivot.x - 0.5f);
                logo.anchorMin = logo.anchorMax = new Vector2(1f, 0.5f);
                logo.pivot = new Vector2(0.5f, 0.5f);
                logo.sizeDelta = new Vector2(LogoSize, LogoSize);
                logo.anchoredPosition = new Vector2(centerX, 0f);
            }

            var label = copy.GetComponentInChildren<TextMeshProUGUI>(true);
            label.text = Text;
            label.color = Yellow;
            copy.AddComponent<Align>().Init(discord.GetComponentInChildren<TextMeshProUGUI>(true), label, logo);
        }

        // Starts the text where the Discord button's text starts, which is only known once that text is drawn.
        private sealed class Align : MonoBehaviour
        {
            private TextMeshProUGUI _discord;
            private TextMeshProUGUI _label;
            private RectTransform _logo;

            internal void Init(TextMeshProUGUI discord, TextMeshProUGUI label, RectTransform logo)
            {
                _discord = discord;
                _label = label;
                _logo = logo;
            }

            private void LateUpdate()
            {
                if (_discord == null || _label == null)
                {
                    Destroy(this);
                    return;
                }
                _discord.ForceMeshUpdate();
                if (_discord.textInfo.characterCount == 0)
                    return;
                var rect = _label.rectTransform;
                var offsetMin = rect.offsetMin;
                var offsetMax = rect.offsetMax;
                offsetMin.x += _discord.textBounds.min.x - _discord.rectTransform.rect.xMin;
                if (_logo != null)
                {
                    float logoLeft = _logo.localPosition.x + _logo.rect.xMin;
                    float labelRight = rect.localPosition.x + rect.rect.xMax;
                    offsetMax.x += logoLeft - TextGap - labelRight;
                }
                rect.offsetMin = offsetMin;
                rect.offsetMax = offsetMax;
                _label.alignment = TextAlignmentOptions.Left;
                _label.enableAutoSizing = true;
                _label.fontSizeMax = _discord.fontSize;
                _label.fontSizeMin = 1f;
                Destroy(this);
            }
        }

        private static Sprite LoadLogo()
        {
            var bytes = ModHelper.ReadModFileBytes(ZhTwMod.ModId, LogoFile);
            var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false) { hideFlags = HideFlags.DontUnloadUnusedAsset };
            if (bytes == null || !texture.LoadImage(bytes))
                throw new System.IO.FileNotFoundException("找不到 " + LogoFile);
            var sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f));
            sprite.hideFlags = HideFlags.DontUnloadUnusedAsset;
            return sprite;
        }

        // The button's pixel art with its blue turned yellow, shade for shade. The game's texture is not
        // readable, so it is copied through a render texture first.
        private static Sprite Recolor(Sprite source)
        {
            var original = source.texture;
            var rt = RenderTexture.GetTemporary(original.width, original.height, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.sRGB);
            var active = RenderTexture.active;
            Graphics.Blit(original, rt);
            RenderTexture.active = rt;
            var texture = new Texture2D(original.width, original.height, TextureFormat.RGBA32, false)
            {
                filterMode = original.filterMode,
                wrapMode = TextureWrapMode.Clamp,
                hideFlags = HideFlags.DontUnloadUnusedAsset,
            };
            texture.ReadPixels(new Rect(0, 0, original.width, original.height), 0, 0);
            RenderTexture.active = active;
            RenderTexture.ReleaseTemporary(rt);

            var pixels = texture.GetPixels();
            for (int i = 0; i < pixels.Length; i++)
            {
                var p = pixels[i];
                if (p.b - Mathf.Max(p.r, p.g) < MinBlueness)
                    continue;
                float shade = Mathf.Clamp01(p.b);
                pixels[i] = new Color(Yellow.r * shade, Yellow.g * shade, Yellow.b * shade, p.a);
            }
            texture.SetPixels(pixels);
            texture.Apply();
            var sprite = Sprite.Create(texture, source.rect, new Vector2(0.5f, 0.5f), source.pixelsPerUnit, 0, SpriteMeshType.FullRect, source.border);
            sprite.hideFlags = HideFlags.DontUnloadUnusedAsset;
            return sprite;
        }
    }
}
