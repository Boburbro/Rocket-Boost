using System;
using UnityEngine;
using UnityEngine.UI;

// Game Over dialogini kod orqali yasaydi (sahnaga prefab qo'yish shart emas).
public static class GameOverUI
{
    static GameObject current;

    public static void Show(string title, string message, params (string label, Action action)[] buttons)
    {
        if (current != null) UnityEngine.Object.Destroy(current);
        MobileControls.EnsureEventSystem();

        var canvasGO = new GameObject("GameOverCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        current = canvasGO;
        var canvas = canvasGO.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 500;
        UiKit.SetupScaler(canvasGO.GetComponent<CanvasScaler>());

        // Qoraytirilgan fon (bosishlarni ham to'sadi)
        var dim = NewImage(canvasGO.transform, "Dim", new Color(0.01f, 0.01f, 0.06f, 0.7f));
        Stretch(dim.rectTransform);

        float panelH = 230 + buttons.Length * 120;
        var panel = NewImage(canvasGO.transform, "Panel", new Color(0.06f, 0.08f, 0.2f, 0.97f));
        Round(panel, 1.2f);
        Center(panel.rectTransform, new Vector2(0, 0), new Vector2(900, panelH));

        var border = UiKit.Img(panel.transform, "Border", UiKit.PillOutline, new Color(0.36f, 0.88f, 1f, 0.5f), 1.2f);
        Stretch(border.rectTransform);

        var titleText = NewText(panel.transform, "Title", title, 64, FontStyle.Bold, UiKit.Pink);
        Place(titleText.rectTransform, panelH / 2 - 60, 90);

        var msgText = NewText(panel.transform, "Message", message, 34, FontStyle.Normal, Color.white);
        Place(msgText.rectTransform, panelH / 2 - 140, 70);

        for (int i = 0; i < buttons.Length; i++)
        {
            var action = buttons[i].action;
            // Birinchi tugma — asosiy (moviy), qolganlari — ikkinchi darajali (binafsha)
            bool primary = i == 0;
            var btnImg = NewImage(panel.transform, "Button" + i, primary ? UiKit.Cyan : new Color(0.3f, 0.26f, 0.62f, 1f));
            Round(btnImg, 1f);
            Place(btnImg.rectTransform, panelH / 2 - 230 - i * 120, 100, 760);
            var btn = btnImg.gameObject.AddComponent<Button>();
            btn.targetGraphic = btnImg;
            btn.onClick.AddListener(() => action?.Invoke());
            var label = NewText(btnImg.transform, "Label", buttons[i].label, 36, FontStyle.Bold, primary ? UiKit.Ink : Color.white);
            Stretch(label.rectTransform);
        }
    }

    static Image NewImage(Transform parent, string name, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        var img = go.GetComponent<Image>();
        img.color = color;
        return img;
    }

    // Burchaklarni yumaloqlaydi
    static void Round(Image img, float sliceScale)
    {
        img.sprite = UiKit.Pill;
        img.type = Image.Type.Sliced;
        img.pixelsPerUnitMultiplier = sliceScale;
    }

    static Text NewText(Transform parent, string name, string content, int size, FontStyle style, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Text));
        go.transform.SetParent(parent, false);
        var t = go.GetComponent<Text>();
        t.text = content;
        t.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        t.fontSize = size;
        t.fontStyle = style;
        t.alignment = TextAnchor.MiddleCenter;
        t.color = color;
        t.raycastTarget = false;
        return t;
    }

    static void Stretch(RectTransform rt)
    {
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = rt.offsetMax = Vector2.zero;
    }

    static void Center(RectTransform rt, Vector2 pos, Vector2 size)
    {
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;
    }

    static void Place(RectTransform rt, float y, float height, float width = 840)
    {
        Center(rt, new Vector2(0, y), new Vector2(width, height));
    }
}
