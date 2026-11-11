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

        var root = UiKit.CreateCanvas("MobileControls", 100, true, out _);

        // Chap va o'ng — pastki chapda, gaz — pastki o'ngda
        CreateButton(root, "LeftButton", HoldButton.ButtonType.Left,
            new Vector2(0, 0), new Vector2(110, 120), 140);
        CreateButton(root, "RightButton", HoldButton.ButtonType.Right,
            new Vector2(0, 0), new Vector2(270, 120), 140);
        CreateButton(root, "GasButton", HoldButton.ButtonType.Thrust,
            new Vector2(1, 0), new Vector2(-135, 140), 190);
    }

    // Sahnada EventSystem bo'lmasa yaratadi (sahna bilan birga yo'qoladi — takrorlanib qolmasligi uchun)
    public static void EnsureEventSystem()
    {
        if (Object.FindFirstObjectByType<EventSystem>() == null)
            new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
    }

    // Dumaloq, yarim shaffof tugma: ichi to'q, atrofida neon halqa, o'rtasida strelka
    static void CreateButton(Transform parent, string name, HoldButton.ButtonType type,
        Vector2 anchor, Vector2 pos, float size)
    {
        bool thrust = type == HoldButton.ButtonType.Thrust;
        Color accent = thrust ? UiKit.Pink : UiKit.Cyan;
        Color normal = new Color(0.07f, 0.09f, 0.24f, 0.5f);
        Color pressed = new Color(accent.r, accent.g, accent.b, 0.45f);

        var fill = UiKit.Img(parent, name, UiKit.Disc, normal);
        fill.raycastTarget = true;
        UiKit.Place(fill.rectTransform, anchor, new Vector2(0.5f, 0.5f), pos, new Vector2(size, size));

        var ring = UiKit.Img(fill.transform, "Ring", UiKit.Ring, accent);
        UiKit.Stretch(ring.rectTransform);

        var center = new Vector2(0.5f, 0.5f);
        var icon = UiKit.Img(fill.transform, "Icon", UiKit.Chevron, Color.white);
        float iconSize = size * (thrust ? 0.5f : 0.55f);
        UiKit.Place(icon.rectTransform, center, center, new Vector2(0, thrust ? size * 0.08f : 0), new Vector2(iconSize, iconSize));
        if (type == HoldButton.ButtonType.Left) icon.rectTransform.localEulerAngles = new Vector3(0, 0, 90);
        else if (type == HoldButton.ButtonType.Right) icon.rectTransform.localEulerAngles = new Vector3(0, 0, -90);

        if (thrust)
        {
            var label = UiKit.Txt(fill.transform, "Label", "BOOST", 20, FontStyle.Bold, Color.white);
            UiKit.Place(label.rectTransform, center, center, new Vector2(0, -size * 0.27f), new Vector2(size, 28));
        }

        var hold = fill.gameObject.AddComponent<HoldButton>();
        hold.SetType(type);
        hold.SetVisual(fill, normal, pressed);
    }
}
