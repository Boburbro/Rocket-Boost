using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Har bir levelda osmon o'rniga kosmik fon chizadi: to'q tumanlik, yulduzlar va sayyora.
// Hammasi kod orqali yasaladi (rasm fayllari kerak emas) va kameraning eng orqasida turadi,
// shuning uchun sahnadagi obyektlar uning oldida ko'rinadi.
public class SpaceBackdrop : MonoBehaviour
{
    const string ObjectName = "SpaceBackdrop";

    static Texture2D nebulaTex, starTex;
    static Sprite planetSprite;

    Camera cam;
    RawImage stars;
    RectTransform planet;
    Vector2 planetHome;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Init()
    {
        SceneManager.sceneLoaded += (scene, mode) => Create();
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void InitFirstScene()
    {
        Create();
    }

    static void Create()
    {
        if (FindFirstObjectByType<Movement>() == null) return;
        if (GameObject.Find(ObjectName) != null) return;
        var camera = Camera.main;
        if (camera == null) return;

        var canvasGO = new GameObject(ObjectName, typeof(Canvas), typeof(CanvasScaler));
        var canvas = canvasGO.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceCamera;
        canvas.worldCamera = camera;
        canvas.planeDistance = camera.farClipPlane * 0.9f; // hamma narsaning orqasida
        canvas.sortingOrder = -100;
        UiKit.SetupScaler(canvasGO.GetComponent<CanvasScaler>());

        var backdrop = canvasGO.AddComponent<SpaceBackdrop>();
        backdrop.cam = camera;

        var nebula = NewRaw(canvasGO.transform, "Nebula", GetNebula());
        UiKit.Stretch(nebula.rectTransform);

        backdrop.stars = NewRaw(canvasGO.transform, "Stars", GetStars());
        UiKit.Stretch(backdrop.stars.rectTransform);

        var planetImg = UiKit.Img(canvasGO.transform, "Planet", GetPlanet(), Color.white);
        var corner = new Vector2(1, 1);
        backdrop.planetHome = new Vector2(-190, -170);
        UiKit.Place(planetImg.rectTransform, corner, new Vector2(0.5f, 0.5f), backdrop.planetHome, new Vector2(230, 230));
        backdrop.planet = planetImg.rectTransform;
    }

    static RawImage NewRaw(Transform parent, string name, Texture texture)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(RawImage));
        go.transform.SetParent(parent, false);
        var raw = go.GetComponent<RawImage>();
        raw.texture = texture;
        raw.raycastTarget = false;
        return raw;
    }

    // Kamera harakatlanganda yulduzlar va sayyora sekin siljiydi (chuqurlik hissi)
    void LateUpdate()
    {
        if (cam == null) return;
        Vector3 p = cam.transform.position;
        if (stars != null) stars.uvRect = new Rect(p.x * 0.004f, p.y * 0.004f, 1f, 1f);
        if (planet != null)
            planet.anchoredPosition = planetHome - Vector2.ClampMagnitude(new Vector2(p.x, p.y) * 1.2f, 70f);
    }

    static float Fbm(float x, float y)
    {
        float sum = 0f, amp = 0.5f;
        for (int i = 0; i < 4; i++)
        {
            sum += Mathf.PerlinNoise(x, y) * amp;
            x *= 2f; y *= 2f; amp *= 0.5f;
        }
        return sum / 0.9375f;
    }

    // To'q ko'k-binafsha osmon, ustida binafsha va moviy tumanlik bulutlari
    static Texture2D GetNebula()
    {
        if (nebulaTex != null) return nebulaTex;
        const int w = 256, h = 144;
        var tex = new Texture2D(w, h, TextureFormat.RGB24, false);
        tex.wrapMode = TextureWrapMode.Clamp;
        tex.filterMode = FilterMode.Bilinear;
        var px = new Color[w * h];
        var top = new Color(0.015f, 0.02f, 0.07f);
        var bottom = new Color(0.1f, 0.05f, 0.22f);
        var violet = new Color(0.4f, 0.16f, 0.62f);
        var teal = new Color(0.04f, 0.34f, 0.46f);
        for (int y = 0; y < h; y++)
        {
            float v = y / (float)(h - 1);
            Color baseColor = Color.Lerp(bottom, top, Mathf.Sqrt(v));
            for (int x = 0; x < w; x++)
            {
                float a = Mathf.SmoothStep(0.45f, 0.8f, Fbm(x * 0.014f + 11.3f, y * 0.014f + 4.7f));
                float b = Mathf.SmoothStep(0.5f, 0.85f, Fbm(x * 0.011f + 57.1f, y * 0.011f + 31.9f));
                px[y * w + x] = baseColor + violet * (a * 0.55f) + teal * (b * 0.45f);
            }
        }
        tex.SetPixels(px);
        tex.Apply(false, true);
        return nebulaTex = tex;
    }

    // Shaffof fonda har xil o'lcham va rangdagi yulduzlar (takrorlanadigan tekstura)
    static Texture2D GetStars()
    {
        if (starTex != null) return starTex;
        const int w = 1024, h = 576;
        var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Repeat;
        tex.filterMode = FilterMode.Bilinear;
        var px = new Color[w * h]; // boshida hammasi shaffof
        var rng = new System.Random(2024);
        Color[] tints =
        {
            Color.white, Color.white, new Color(0.75f, 0.88f, 1f), new Color(1f, 0.85f, 0.95f), new Color(0.8f, 0.75f, 1f)
        };
        for (int i = 0; i < 520; i++)
        {
            int sx = rng.Next(w), sy = rng.Next(h);
            Color tint = tints[rng.Next(tints.Length)];
            double roll = rng.NextDouble();
            float radius = roll > 0.97 ? 3.2f : roll > 0.85 ? 1.8f : 0.9f;
            float brightness = 0.35f + (float)rng.NextDouble() * 0.65f;
            int reach = Mathf.CeilToInt(radius);
            for (int dy = -reach; dy <= reach; dy++)
                for (int dx = -reach; dx <= reach; dx++)
                {
                    float d = Mathf.Sqrt(dx * dx + dy * dy);
                    float alpha = Mathf.Clamp01(1f - d / (radius + 0.5f));
                    alpha = alpha * alpha * brightness;
                    if (alpha <= 0f) continue;
                    int idx = ((sy + dy + h) % h) * w + (sx + dx + w) % w;
                    if (alpha > px[idx].a) px[idx] = new Color(tint.r, tint.g, tint.b, alpha);
                }
        }
        tex.SetPixels(px);
        tex.Apply(false, true);
        return starTex = tex;
    }

    // Yuqori chapdan yoritilgan, yo'l-yo'l sayyora
    static Sprite GetPlanet()
    {
        if (planetSprite != null) return planetSprite;
        const int size = 256;
        var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.wrapMode = TextureWrapMode.Clamp;
        tex.filterMode = FilterMode.Bilinear;
        var px = new Color[size * size];
        float c = (size - 1) / 2f, r = size / 2f - 2f;
        Vector3 light = new Vector3(-0.55f, 0.6f, 0.58f).normalized;
        var c1 = new Color(0.5f, 0.32f, 0.9f);
        var c2 = new Color(0.25f, 0.6f, 0.95f);
        for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                float nx = (x - c) / r, ny = (y - c) / r;
                float d2 = nx * nx + ny * ny;
                if (d2 > 1.05f) continue;
                float nz = Mathf.Sqrt(Mathf.Max(0f, 1f - d2));
                float lit = Mathf.Clamp01(Vector3.Dot(new Vector3(nx, ny, nz), light));
                float band = 0.5f + 0.5f * Mathf.Sin(ny * 9f + nx * 2f + Mathf.Sin(nx * 5f) * 0.6f);
                Color col = Color.Lerp(c1, c2, band) * (0.07f + 0.93f * lit);
                col.a = Mathf.Clamp01((1f - Mathf.Sqrt(d2)) * r);
                px[y * size + x] = col;
            }
        tex.SetPixels(px);
        tex.Apply(false, true);
        return planetSprite = Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 100f);
    }
}
