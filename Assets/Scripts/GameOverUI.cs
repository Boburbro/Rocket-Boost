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
        var scaler = canvasGO.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1280, 720);
        scaler.matchWidthOrHeight = 0.5f;

        // Qoraytirilgan fon (bosishlarni ham to'sadi)
        var dim = NewImage(canvasGO.transform, "Dim", new Color(0, 0, 0, 0.6f));
        Stretch(dim.rectTransform);

        float panelH = 230 + buttons.Length * 120;
        var panel = NewImage(canvasGO.transform, "Panel", new Color(0.12f, 0.14f, 0.2f, 0.97f));
        Center(panel.rectTransform, new Vector2(0, 0), new Vector2(900, panelH));

        var titleText = NewText(panel.transform, "Title", title, 64, FontStyle.Bold, new Color(1f, 0.4f, 0.3f));
        Place(titleText.rectTransform, panelH / 2 - 60, 90);

        var msgText = NewText(panel.transform, "Message", message, 34, FontStyle.Normal, Color.white);
        Place(msgText.rectTransform, panelH / 2 - 140, 70);

        for (int i = 0; i < buttons.Length; i++)
        {
            var action = buttons[i].action;
            var btnImg = NewImage(panel.transform, "Button" + i, new Color(0.15f, 0.6f, 0.9f, 1f));
            Place(btnImg.rectTransform, panelH / 2 - 230 - i * 120, 100, 760);
            var btn = btnImg.gameObject.AddComponent<Button>();
            btn.targetGraphic = btnImg;
            btn.onClick.AddListener(() => action?.Invoke());
            var label = NewText(btnImg.transform, "Label", buttons[i].label, 36, FontStyle.Bold, Color.white);
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
