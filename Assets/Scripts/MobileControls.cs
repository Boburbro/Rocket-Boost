using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

// Har bir sahnada (Level_1, Level_2, ...) telefon uchun 3 ta tugmani avtomatik yaratadi:
// chapga, o'ngga va gaz. Sahnada HoldButton allaqachon bo'lsa, hech narsa qilmaydi.
public static class MobileControls
{
    // Kompyuterda (Editor) ham ko'rish/sinash uchun true qiling
    const bool showInEditor = false;

    // Bu metod ilova ochilganda BIR marta ishlaydi, shuning uchun har bir sahna
    // yuklanganda (qayta boshlash, keyingi level) tugmalarni qayta yaratish uchun ulanamiz.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Init()
    {
        UnityEngine.SceneManagement.SceneManager.sceneLoaded += (scene, mode) => Setup();
    }

    // Birinchi sahna uchun (Editor'da sceneLoaded birinchi sahnada chaqirilmasligi mumkin)
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void InitFirstScene()
    {
        Setup();
    }

    static void Setup()
    {
        bool mobile = Application.isMobilePlatform || (showInEditor && Application.isEditor);
        if (!mobile) return;
        if (Object.FindFirstObjectByType<Movement>() == null) return;
        if (GameObject.Find("MobileControls") != null) return;

        // Sahnadagi eski (matnsiz) tugmalarni olib tashlaymiz, hamma levelda bir xil tugmalar chiqsin
        foreach (var old in Object.FindObjectsByType<HoldButton>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            Object.Destroy(old.gameObject);

        EnsureEventSystem();

        var canvasGO = new GameObject("MobileControls", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        var canvas = canvasGO.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;
        var scaler = canvasGO.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1280, 720);
        scaler.matchWidthOrHeight = 0.5f;

        // Chap va o'ng — pastki chapda, gaz — pastki o'ngda
        CreateButton(canvasGO.transform, "LeftButton", "<", HoldButton.ButtonType.Left,
            new Vector2(0, 0), new Vector2(110, 190), new Vector2(160, 160));
        CreateButton(canvasGO.transform, "RightButton", ">", HoldButton.ButtonType.Right,
            new Vector2(0, 0), new Vector2(290, 190), new Vector2(160, 160));
        CreateButton(canvasGO.transform, "GasButton", "GAS", HoldButton.ButtonType.Thrust,
            new Vector2(1, 0), new Vector2(-140, 210), new Vector2(200, 200));
    }

    // Sahnada EventSystem bo'lmasa yaratadi (sahna bilan birga yo'qoladi — takrorlanib qolmasligi uchun)
    public static void EnsureEventSystem()
    {
        if (Object.FindFirstObjectByType<EventSystem>() == null)
            new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
    }

    static void CreateButton(Transform parent, string name, string label, HoldButton.ButtonType type,
        Vector2 anchor, Vector2 pos, Vector2 size)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(HoldButton));
        go.transform.SetParent(parent, false);

        var rt = go.GetComponent<RectTransform>();
        rt.anchorMin = rt.anchorMax = anchor;
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;

        var img = go.GetComponent<Image>();
        img.color = type == HoldButton.ButtonType.Thrust
            ? new Color(1f, 0.45f, 0.1f, 0.55f)
            : new Color(1f, 1f, 1f, 0.4f);

        go.GetComponent<HoldButton>().SetType(type);

        var textGO = new GameObject("Label", typeof(RectTransform), typeof(Text));
        textGO.transform.SetParent(go.transform, false);
        var trt = textGO.GetComponent<RectTransform>();
        trt.anchorMin = Vector2.zero;
        trt.anchorMax = Vector2.one;
        trt.offsetMin = trt.offsetMax = Vector2.zero;
        var text = textGO.GetComponent<Text>();
        text.text = label;
        text.alignment = TextAnchor.MiddleCenter;
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.fontSize = 60;
        text.fontStyle = FontStyle.Bold;
        text.color = Color.white;
        text.raycastTarget = false;
    }
}
