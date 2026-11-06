using UnityEngine;
using UnityEngine.SceneManagement;

// Oddiy kapsula raketani Kenney Space Kit (CC0) modeli bilan almashtiradi.
// Har bir sahna yuklanganda Movement bor obyektga o'zi qo'shiladi, sahnani o'zgartirish shart emas.
// Model: Assets/Resources/RocketSkin/RocketModel.fbx. Model topilmasa, eski ko'rinish qoladi.
public class RocketSkin : MonoBehaviour
{
    // Modelning "burni" Unity'da qaysi tomonga qarasa, shuni yuqoriga (Y) buradi.
    // Burun noto'g'ri tomonga qarasa, shu qiymatni o'zgartiring (masalan (-90,0,0) yoki (90,180,0)).
    public Vector3 modelEuler = new Vector3(90f, 0f, 0f);
    public float modelHeight = 2.2f;       // raketaning ko'rinadigan balandligi
    public float colliderRadius = 0.45f;   // to'qnashuv radiusi (qanotlar kengroq, shuning uchun biroz kichikroq)
    public float colliderHeight = 2f;

    // Keyingi sahnalar (qayta boshlash, keyingi level) uchun
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void Init()
    {
        SceneManager.sceneLoaded += (scene, mode) => Apply();
    }

    // Birinchi sahna uchun (Editor'da sceneLoaded birinchi sahnada chaqirilmasligi mumkin)
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void InitFirstScene()
    {
        Apply();
    }

    static void Apply()
    {
        var movement = Object.FindFirstObjectByType<Movement>();
        if (movement != null && movement.GetComponent<RocketSkin>() == null)
            movement.gameObject.AddComponent<RocketSkin>();
    }

    void Awake()
    {
        var prefab = Resources.Load<GameObject>("RocketSkin/RocketModel");
        if (prefab == null) return; // model yo'q: eski ko'rinish qoladi

        // Eski kapsula ko'rinishini o'chiramiz (collider va fizika qoladi)
        var oldRenderer = GetComponent<MeshRenderer>();
        Material baseMat = oldRenderer != null ? oldRenderer.sharedMaterial : null;
        if (oldRenderer != null) oldRenderer.enabled = false;

        // Ota-obyekt masshtabi (0.5, 1, 0.5) modelni cho'zib yubormasligi uchun tekislaymiz
        transform.localScale = Vector3.one;
        var capsule = GetComponent<CapsuleCollider>();
        if (capsule != null)
        {
            capsule.radius = colliderRadius;
            capsule.height = colliderHeight;
        }

        var model = Instantiate(prefab, transform);
        model.name = "RocketModel";
        model.transform.localPosition = Vector3.zero;
        model.transform.localRotation = Quaternion.Euler(modelEuler);
        model.transform.localScale = Vector3.one;

        // Modelda collider bo'lsa, o'chiramiz (fizikani faqat kapsula boshqaradi)
        foreach (var c in model.GetComponentsInChildren<Collider>()) Destroy(c);

        FitAndCenter(model);
        ApplyMaterials(model, baseMat);
    }

    // Balandligini modelHeight ga moslab, markazini obyekt markaziga qo'yadi
    void FitAndCenter(GameObject model)
    {
        var renderers = model.GetComponentsInChildren<Renderer>();
        if (renderers.Length == 0) return;

        Bounds b = GetBounds(renderers);
        if (b.size.y > 0.0001f)
            model.transform.localScale *= modelHeight / b.size.y;

        b = GetBounds(renderers);
        Vector3 offset = transform.InverseTransformVector(b.center - transform.position);
        model.transform.localPosition -= offset;
    }

    static Bounds GetBounds(Renderer[] renderers)
    {
        Bounds b = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++) b.Encapsulate(renderers[i].bounds);
        return b;
    }

    // Loyihadagi URP materialdan nusxa olib, qism nomiga qarab rang beramiz
    // (FBX ichidagi materiallar URP da pushti bo'lib qolmasligi uchun)
    static void ApplyMaterials(GameObject model, Material baseMat)
    {
        if (baseMat == null) return;

        foreach (var r in model.GetComponentsInChildren<Renderer>())
        {
            var mats = r.sharedMaterials;
            for (int i = 0; i < mats.Length; i++)
            {
                string n = mats[i] != null ? mats[i].name.ToLowerInvariant() : "";
                var m = new Material(baseMat);
                Color c;
                if (n.Contains("metal")) c = new Color(0.89f, 0.40f, 0.27f);       // to'q to'q-sariq/qizil qismlar
                else if (n.Contains("glass")) c = new Color(0.27f, 0.45f, 0.65f);  // oyna
                else if (n.Contains("pipe")) c = new Color(0.62f, 0.68f, 0.73f);   // kulrang
                else c = Color.white;                                              // asosiy korpus
                if (m.HasProperty("_BaseColor")) m.SetColor("_BaseColor", c);
                if (m.HasProperty("_Color")) m.SetColor("_Color", c);
                mats[i] = m;
            }
            r.sharedMaterials = mats;
        }
    }
}
