using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// O'yin paytida ekranning tepasida "LEVEL N" yozuvini ko'rsatadi.
// Har bir sahnada o'zi yaratiladi (sahnaga qo'yish shart emas).
public static class LevelHud
{
    const string ObjectName = "LevelHud";

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Init()
    {
        SceneManager.sceneLoaded += (scene, mode) => Create();
    }

    // Birinchi sahna uchun (Editor'da sceneLoaded birinchi sahnada chaqirilmasligi mumkin)
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void InitFirstScene()
    {
        Create();
    }

    static void Create()
    {
        // Faqat o'yin levellarida (Movement bor sahnada) ko'rsatamiz
        if (Object.FindFirstObjectByType<Movement>() == null) return;
        if (GameObject.Find(ObjectName) != null) return;

        int level = SceneManager.GetActiveScene().buildIndex + 1;

        var canvasGO = new GameObject(ObjectName, typeof(Canvas), typeof(CanvasScaler));
        var canvas = canvasGO.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 50;
        var scaler = canvasGO.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1280, 720);
        scaler.matchWidthOrHeight = 0.5f;

        var textGO = new GameObject("Text", typeof(RectTransform), typeof(Text), typeof(Outline));
        textGO.transform.SetParent(canvasGO.transform, false);

        var rt = textGO.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 1f);
        rt.pivot = new Vector2(0.5f, 1f);
        rt.anchoredPosition = new Vector2(0, -20);
        rt.sizeDelta = new Vector2(500, 80);

        var text = textGO.GetComponent<Text>();
        text.text = "LEVEL " + level;
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.fontSize = 56;
        text.fontStyle = FontStyle.Bold;
        text.alignment = TextAnchor.MiddleCenter;
        text.color = Color.white;
        text.raycastTarget = false;

        var outline = textGO.GetComponent<Outline>();
        outline.effectColor = new Color(0, 0, 0, 0.8f);
        outline.effectDistance = new Vector2(3, -3);
    }
}
