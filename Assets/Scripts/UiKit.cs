using System;
using UnityEngine;
using UnityEngine.UI;

// Kosmos uslubidagi UI uchun umumiy ranglar, shakllar (kod orqali chiziladi) va yordamchi metodlar.
// Rasm fayllari kerak emas: doira, halqa, yumaloq burchakli panel va strelka shu yerda yasaladi.
public static class UiKit
{
    // Ranglar palitrasi
    public static readonly Color Cyan = new Color(0.36f, 0.88f, 1f);
    public static readonly Color Violet = new Color(0.62f, 0.5f, 1f);
    public static readonly Color Pink = new Color(1f, 0.45f, 0.8f);
    public static readonly Color Ink = new Color(0.04f, 0.05f, 0.14f);
    public static readonly Color Panel = new Color(0.06f, 0.08f, 0.2f, 0.72f);
    public static readonly Color Muted = new Color(0.62f, 0.7f, 0.92f);

    static Sprite disc, ring, pill, pillOutline, chevron;

    public static Sprite Disc => disc != null ? disc : disc = Make(256, Vector4.zero, (x, y, r) =>
        Mathf.Clamp01(r - Dist(x, y)));

    public static Sprite Ring => ring != null ? ring : ring = Make(256, Vector4.zero, (x, y, r) =>
    {
        float d = Dist(x, y);
        return Mathf.Clamp01(r - d) * Mathf.Clamp01(d - (r - 7f));
    });

    // 9-slice: istalgan o'lchamga cho'ziladigan yumaloq burchakli to'ldirilgan shakl
    public static Sprite Pill => pill != null ? pill : pill = Make(64, Vector4.one * 31f, (x, y, r) =>
        Mathf.Clamp01(r - Dist(x, y)));

    // 9-slice: yumaloq burchakli kontur (ichi bo'sh)
    public static Sprite PillOutline => pillOutline != null ? pillOutline : pillOutline = Make(64, Vector4.one * 31f, (x, y, r) =>
    {
        float d = Dist(x, y);
        return Mathf.Clamp01(r - d) * Mathf.Clamp01(d - (r - 3f));
    });

    // Tepaga qaragan strelka (^). Chap/o'ng uchun Image'ni aylantiramiz.
    public static Sprite Chevron => chevron != null ? chevron : chevron = Make(256, Vector4.zero, (x, y, r) =>
    {
        var p = new Vector2(x, y) / r; // -1..1
        float d = Mathf.Min(
            SegDist(p, new Vector2(-0.5f, -0.25f), new Vector2(0f, 0.3f)),
            SegDist(p, new Vector2(0.5f, -0.25f), new Vector2(0f, 0.3f)));
        return Mathf.Clamp01((0.1f - d) * r + 0.5f);
    });

    static float Dist(float x, float y) => Mathf.Sqrt(x * x + y * y);

    static float SegDist(Vector2 p, Vector2 a, Vector2 b)
    {
        Vector2 ab = b - a;
        float t = Mathf.Clamp01(Vector2.Dot(p - a, ab) / ab.sqrMagnitude);
        return (p - (a + ab * t)).magnitude;
    }

    // alpha(x, y, radius): markazdan piksel koordinatasi bo'yicha shaffoflikni qaytaradi
    static Sprite Make(int size, Vector4 border, Func<float, float, float, float> alpha)
    {
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Clamp;
        tex.filterMode = FilterMode.Bilinear;
        var px = new Color32[size * size];
        float c = (size - 1) / 2f;
        float r = size / 2f - 1f;
        for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
                px[y * size + x] = new Color32(255, 255, 255, (byte)(Mathf.Clamp01(alpha(x - c, y - c, r)) * 255f));
        tex.SetPixels32(px);
        tex.Apply(false, true);
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f, 0,
            SpriteMeshType.FullRect, border);
    }

    // Canvas yaratadi va ichida telefon "notch"idan qochadigan xavfsiz zona qaytaradi
    public static RectTransform CreateCanvas(string name, int sortingOrder, bool raycasts, out GameObject canvasGO)
    {
        canvasGO = raycasts
            ? new GameObject(name, typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster))
            : new GameObject(name, typeof(Canvas), typeof(CanvasScaler));
        var canvas = canvasGO.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = sortingOrder;
        SetupScaler(canvasGO.GetComponent<CanvasScaler>());

        var rootGO = new GameObject("SafeArea", typeof(RectTransform));
        rootGO.transform.SetParent(canvasGO.transform, false);
        var root = rootGO.GetComponent<RectTransform>();
        Rect safe = Screen.safeArea;
        if (Screen.width > 0 && Screen.height > 0)
        {
            root.anchorMin = new Vector2(safe.xMin / Screen.width, safe.yMin / Screen.height);
            root.anchorMax = new Vector2(safe.xMax / Screen.width, safe.yMax / Screen.height);
        }
        else
        {
            root.anchorMin = Vector2.zero;
            root.anchorMax = Vector2.one;
        }
        root.offsetMin = root.offsetMax = Vector2.zero;
        return root;
    }

    public static void SetupScaler(CanvasScaler scaler)
    {
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1280, 720);
        scaler.matchWidthOrHeight = 0.5f;
    }

    public static Image Img(Transform parent, string name, Sprite sprite, Color color, float sliceScale = 0f)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        var img = go.GetComponent<Image>();
        img.sprite = sprite;
        img.color = color;
        img.raycastTarget = false;
        if (sliceScale > 0f)
        {
            img.type = Image.Type.Sliced;
            img.pixelsPerUnitMultiplier = sliceScale;
        }
        return img;
    }

    public static Text Txt(Transform parent, string name, string content, int size, FontStyle style, Color color,
        TextAnchor align = TextAnchor.MiddleCenter)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Text));
        go.transform.SetParent(parent, false);
        var t = go.GetComponent<Text>();
        t.text = content;
        t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        t.fontSize = size;
        t.fontStyle = style;
        t.alignment = align;
        t.color = color;
        t.raycastTarget = false;
        t.horizontalOverflow = HorizontalWrapMode.Overflow;
        t.verticalOverflow = VerticalWrapMode.Overflow;
        return t;
    }

    public static void Place(RectTransform rt, Vector2 anchor, Vector2 pivot, Vector2 pos, Vector2 size)
    {
        rt.anchorMin = rt.anchorMax = anchor;
        rt.pivot = pivot;
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;
    }

    public static void Stretch(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
    }
}
