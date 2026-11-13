using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.TextCore.LowLevel;
using UnityEngine.UI;

public static class V02Ui
{
    public const float RefWidth = 1080f;
    public const float RefHeight = 1920f;

    public static readonly Color Mint = new Color(0.72f, 0.93f, 0.88f);
    public static readonly Color Sky = new Color(0.70f, 0.88f, 0.97f);
    public static readonly Color Pink = new Color(0.98f, 0.78f, 0.86f);
    public static readonly Color Yellow = new Color(1f, 0.93f, 0.65f);
    public static readonly Color Purple = new Color(0.78f, 0.72f, 0.93f);
    public static readonly Color Ink = new Color(0.22f, 0.18f, 0.28f);
    public static readonly Color Hideout = new Color(0.86f, 0.78f, 0.70f);
    public static readonly Color White = new Color(1f, 1f, 1f, 0.94f);

    public const string KoreanFontAssetPath = "Assets/font/NotoSansKR-Black SDF.asset";
    public const string KoreanTtfPath = "Assets/font/NotoSansKR-Black.ttf";

    static TMP_FontAsset cachedFont;
    static bool fontErrorLogged;

    public static void UseFont(TMP_FontAsset font)
    {
        if (CanRenderHangul(font))
            cachedFont = font;
    }

    public static TMP_FontAsset Font()
    {
        if (cachedFont != null)
            return cachedFont;

        cachedFont = LoadKoreanFont();
        if (cachedFont != null)
            return cachedFont;

        if (!fontErrorLogged)
        {
            fontErrorLogged = true;
            Debug.LogError("[V02] 한글 TMP 폰트를 초기화하지 못했습니다. "
                + KoreanFontAssetPath
                + " 와 "
                + KoreanTtfPath
                + " 를 확인하세요. TMP 기본 폰트(LiberationSans)는 한글 글리프가 없습니다. "
                + "필요하면 Window > TextMeshPro > Import TMP Essential Resources 를 실행하세요.");
        }

        return null;
    }

    static TMP_FontAsset LoadKoreanFont()
    {
        TMP_FontAsset sdf = LoadAsset<TMP_FontAsset>(KoreanFontAssetPath, "NotoSansKR-Black SDF");
        if (CanRenderHangul(sdf))
            return sdf;

        Font ttf = LoadAsset<Font>(KoreanTtfPath, "NotoSansKR-Black");
        if (ttf != null)
        {
            TMP_FontAsset dynamicFont = TMP_FontAsset.CreateFontAsset(
                ttf, 90, 9, GlyphRenderMode.SDFAA, 2048, 2048, AtlasPopulationMode.Dynamic, true);
            if (dynamicFont != null)
            {
                dynamicFont.name = "NotoSansKR-Black Dynamic";
                return dynamicFont;
            }

            Debug.LogError("[V02] NotoSansKR TTF로 Dynamic Font Asset을 만들지 못했습니다: " + KoreanTtfPath);
        }

        if (sdf != null)
        {
            Debug.LogWarning("[V02] " + KoreanFontAssetPath
                + " 는 Static ASCII만 포함하고 있어 한글이 □로 표시될 수 있습니다. Atlas Population Mode를 Dynamic으로 바꾸고 Source Font에 NotoSansKR TTF를 연결하세요.");
            return sdf;
        }

        TMP_Settings settings = Resources.Load<TMP_Settings>("TMP Settings");
        if (settings != null && TMP_Settings.defaultFontAsset != null)
        {
            Debug.LogWarning("[V02] 한글 Font Asset이 없어 TMP 기본 폰트를 사용합니다. 한글이 □로 표시됩니다.");
            return TMP_Settings.defaultFontAsset;
        }

        return null;
    }

    static bool CanRenderHangul(TMP_FontAsset font)
    {
        if (font == null)
            return false;

        if (font.atlasPopulationMode == AtlasPopulationMode.Dynamic && font.sourceFontFile != null)
            return true;

        return font.HasCharacter('가');
    }

    static T LoadAsset<T>(string assetPath, string resourcesName) where T : Object
    {
        T loaded = Resources.Load<T>(resourcesName);
        if (loaded != null)
            return loaded;

#if UNITY_EDITOR
        loaded = UnityEditor.AssetDatabase.LoadAssetAtPath<T>(assetPath);
#endif
        return loaded;
    }

    public static RectTransform CreateStretch(string name, Transform parent)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        RectTransform rect = (RectTransform)go.transform;
        Stretch(rect);
        return rect;
    }

    public static void Stretch(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        rect.localScale = Vector3.one;
    }

    public static RectTransform CreatePanel(string name, Transform parent, Color color)
    {
        RectTransform rect = CreateStretch(name, parent);
        Image image = rect.gameObject.AddComponent<Image>();
        image.color = color;
        image.raycastTarget = true;
        return rect;
    }

    public static TextMeshProUGUI CreateText(
        string name,
        Transform parent,
        string content,
        float size,
        Color color,
        TextAlignmentOptions align)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        TextMeshProUGUI text = go.AddComponent<TextMeshProUGUI>();
        TMP_FontAsset font = Font();
        if (font != null)
            text.font = font;
        text.text = content;
        text.fontSize = size;
        text.color = color;
        text.alignment = align;
        text.raycastTarget = false;
        text.textWrappingMode = TextWrappingModes.Normal;
        text.overflowMode = TextOverflowModes.Overflow;
        return text;
    }

    public static CanvasGroup AddCanvasGroup(RectTransform rect)
    {
        CanvasGroup group = rect.gameObject.GetComponent<CanvasGroup>();
        if (group == null)
            group = rect.gameObject.AddComponent<CanvasGroup>();
        return group;
    }

    public static void ApplySprite(Image image, Sprite sprite, bool preserveAspect, bool raycastTarget)
    {
        if (sprite != null)
        {
            image.sprite = sprite;
            image.color = Color.white;
            image.type = Image.Type.Simple;
            image.preserveAspect = preserveAspect;
        }

        image.raycastTarget = raycastTarget;
    }

    public static Button CreateImageButton(
        string name,
        Transform parent,
        Sprite sprite,
        UnityAction onClick)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        RectTransform rect = (RectTransform)go.transform;
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = sprite != null ? sprite.rect.size : new Vector2(100f, 100f);
        Image image = go.AddComponent<Image>();
        ApplySprite(image, sprite, true, true);
        if (sprite != null && sprite.texture != null && sprite.texture.isReadable)
            image.alphaHitTestMinimumThreshold = 0.1f;

        Button button = rect.gameObject.AddComponent<Button>();
        button.targetGraphic = image;
        button.transition = Selectable.Transition.None;
        if (onClick != null)
            button.onClick.AddListener(onClick);

        rect.gameObject.AddComponent<V02UiPressScale>();
        return button;
    }

    public static Button CreateButton(
        string name,
        Transform parent,
        string label,
        Color color,
        UnityAction onClick)
    {
        RectTransform rect = CreatePanel(name, parent, color);
        Image image = rect.GetComponent<Image>();
        Button button = rect.gameObject.AddComponent<Button>();
        button.targetGraphic = image;
        ColorBlock colors = button.colors;
        colors.highlightedColor = Color.Lerp(color, Color.white, 0.15f);
        colors.pressedColor = Color.Lerp(color, Ink, 0.12f);
        button.colors = colors;
        if (onClick != null)
            button.onClick.AddListener(onClick);

        TextMeshProUGUI text = CreateText("Label", rect, label, 36f, Ink, TextAlignmentOptions.Center);
        Stretch(text.rectTransform);
        text.raycastTarget = false;
        return button;
    }

    public static void SetAnchors(RectTransform rect, Vector2 min, Vector2 max, Vector2 offsetMin, Vector2 offsetMax)
    {
        rect.anchorMin = min;
        rect.anchorMax = max;
        rect.offsetMin = offsetMin;
        rect.offsetMax = offsetMax;
    }
}
