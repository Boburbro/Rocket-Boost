using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// O'yin paytidagi HUD: chap tepada zona nomi va level progressi, o'rtada vaqt.
// Har bir sahnada o'zi yaratiladi (sahnaga qo'yish shart emas).
public static class LevelHud
{
    const string ObjectName = "LevelHud";

    // Har bir level uchun zona nomi (yangi level qo'shsangiz shu yerga nom qo'shing)
    static readonly string[] ZoneNames =
    {
        "LUNAR BASE", "NEBULA RUN", "ASTEROID BELT", "ORBITAL STATION", "ION STORM", "DARK SIDE"
    };

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

        int index = Mathf.Max(0, SceneManager.GetActiveScene().buildIndex);
        int level = index + 1;
        int total = Mathf.Max(level, SceneManager.sceneCountInBuildSettings);
        string zone = index < ZoneNames.Length ? ZoneNames[index] : "DEEP SPACE";

        var root = UiKit.CreateCanvas(ObjectName, 50, false, out _);

        BuildZoneBlock(root, zone, level, total);
        BuildTimer(root);
        if (!Application.isMobilePlatform) BuildKeyboardHint(root);
    }

    // Chap tepa: zona nomi, "SECTOR N" belgisi, "LEVEL N/M" va progress chiziqchalari
    static void BuildZoneBlock(RectTransform root, string zone, int level, int total)
    {
        var topLeft = new Vector2(0, 1);

        var title = UiKit.Txt(root, "Zone", zone, 36, FontStyle.Bold, Color.white, TextAnchor.UpperLeft);
        UiKit.Place(title.rectTransform, topLeft, topLeft, new Vector2(28, -16), new Vector2(600, 44));
        var shadow = title.gameObject.AddComponent<Shadow>();
        shadow.effectColor = new Color(0.2f, 0.1f, 0.5f, 0.7f);
        shadow.effectDistance = new Vector2(0, -3);

        var badge = UiKit.Img(root, "SectorBadge", UiKit.Pill, UiKit.Cyan, 1f);
        UiKit.Place(badge.rectTransform, topLeft, topLeft, new Vector2(28, -66), new Vector2(104, 28));
        var badgeText = UiKit.Txt(badge.transform, "Text", "SECTOR " + level, 15, FontStyle.Bold, UiKit.Ink);
        UiKit.Stretch(badgeText.rectTransform);

        var levelText = UiKit.Txt(root, "Level", "LEVEL " + level + "/" + total, 18, FontStyle.Bold,
            new Color(1f, 1f, 1f, 0.92f), TextAnchor.MiddleLeft);
        UiKit.Place(levelText.rectTransform, topLeft, topLeft, new Vector2(146, -66), new Vector2(120, 28));

        int dashes = Mathf.Min(total, 10);
        for (int i = 0; i < dashes; i++)
        {
            Color color = i < level ? UiKit.Cyan : new Color(1f, 1f, 1f, 0.25f);
            var dash = UiKit.Img(root, "Dash" + i, UiKit.Pill, color, 1f);
            UiKit.Place(dash.rectTransform, topLeft, topLeft, new Vector2(262 + i * 28, -77), new Vector2(22, 6));
        }
    }

    // O'rta tepa: vaqt hisoblagichi
    static void BuildTimer(RectTransform root)
    {
        var top = new Vector2(0.5f, 1f);

        var panel = UiKit.Img(root, "TimerPanel", UiKit.Pill, UiKit.Panel, 1f);
        UiKit.Place(panel.rectTransform, top, top, new Vector2(0, -14), new Vector2(210, 42));

        var outline = UiKit.Img(panel.transform, "Outline", UiKit.PillOutline, new Color(0.36f, 0.88f, 1f, 0.45f), 1f);
        UiKit.Stretch(outline.rectTransform);

        var center = new Vector2(0.5f, 0.5f);
        var label = UiKit.Txt(panel.transform, "Label", "TIME", 15, FontStyle.Bold, UiKit.Muted, TextAnchor.MiddleRight);
        UiKit.Place(label.rectTransform, center, new Vector2(1f, 0.5f), new Vector2(-14, 0), new Vector2(70, 42));

        var value = UiKit.Txt(panel.transform, "Value", "0:00.0", 22, FontStyle.Bold, Color.white, TextAnchor.MiddleLeft);
        UiKit.Place(value.rectTransform, center, new Vector2(0f, 0.5f), new Vector2(-2, 0), new Vector2(100, 42));

        panel.gameObject.AddComponent<HudTimer>().Init(value);
    }

    // Kompyuterda: pastda klaviatura tugmalari eslatmasi
    static void BuildKeyboardHint(RectTransform root)
    {
        var bottom = new Vector2(0.5f, 0f);
        var hint = UiKit.Txt(root, "KeyboardHint", "SPACE  boost     ·     ←  →  tilt", 16, FontStyle.Normal,
            new Color(1f, 1f, 1f, 0.55f));
        UiKit.Place(hint.rectTransform, bottom, bottom, new Vector2(0, 12), new Vector2(600, 24));
    }
}
