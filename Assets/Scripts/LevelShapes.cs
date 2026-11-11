using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

// Sahnadagi oddiy kublarga kosmik shakl beradi (sahnani o'zgartirish shart emas):
//  - platformalar (yer, start va finish maydonchalari): qirralari kesilgan plita, to'qnashuvi o'zgarmaydi;
//  - turg'un to'siqlar: uchi torayib boradigan sakkiz qirrali ustun (osilib turgani teskari);
//  - harakatlanuvchi to'siqlar (Oscillator): olmos shaklidagi "mina".
// To'siqlarning to'qnashuvi ham yangi shaklga moslanadi, ya'ni ko'ringan joyigagina urilasiz.
public static class LevelShapes
{
    const float Bevel = 0.12f; // qirra kesimi, dunyo birligida

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
        if (Object.FindFirstObjectByType<Movement>() == null) return;

        var cubes = FindCubes();
        var platforms = PlatformBounds(cubes);

        foreach (var mf in cubes)
        {
            // Allaqachon shakl berilgan bo'lsa (metod bir sahnada ikki marta chaqirilishi mumkin), tegmaymiz
            if (mf.sharedMesh != null && mf.sharedMesh.name == StyledName) continue;
            int kind = KindOf(mf, platforms);
            Mesh mesh = CreateMesh(mf.transform.lossyScale, kind);
            if (mesh == null) continue;
            mf.sharedMesh = mesh;
            if (kind == KindSlab) continue; // BoxCollider qoladi: qo'nish maydoni o'zgarmasin

            var box = mf.GetComponent<BoxCollider>();
            box.enabled = false;
            Object.Destroy(box);
            var collider = mf.gameObject.AddComponent<MeshCollider>();
            collider.convex = true;
            collider.sharedMesh = mesh;
        }
    }

    // Quyidagi metodlarni Editor'dagi ko'rinish skripti (LevelShapesPreview) ham ishlatadi,
    // shunda Scene oynasida ham Play paytidagi shakllar ko'rinadi.
    public const string StyledName = "StyledShape";
    public const int KindSlab = 0, KindSpire = 1, KindSpireFlipped = 2, KindMine = 3;

    // Shakl beriladigan kublar: BoxCollider'li, raketaga tegishli bo'lmagan oddiy kublar
    public static List<MeshFilter> FindCubes()
    {
        var cubes = new List<MeshFilter>();
        foreach (var mf in Object.FindObjectsByType<MeshFilter>(FindObjectsSortMode.None))
        {
            Mesh m = mf.sharedMesh;
            if (m != null && m.name != "Cube" && m.name != StyledName) continue;
            if (mf.GetComponent<BoxCollider>() == null || mf.GetComponent<Renderer>() == null) continue;
            if (mf.GetComponentInParent<Movement>() != null) continue; // raketa qismlari emas
            cubes.Add(mf);
        }
        return cubes;
    }

    // To'siq osilib turganini bilish uchun platformalarning joylashuvi kerak
    public static List<Bounds> PlatformBounds(List<MeshFilter> cubes)
    {
        var platforms = new List<Bounds>();
        foreach (var mf in cubes)
            if (IsPlatform(mf)) platforms.Add(mf.GetComponent<Renderer>().bounds);
        return platforms;
    }

    public static int KindOf(MeshFilter mf, List<Bounds> platforms)
    {
        if (IsPlatform(mf)) return KindSlab;
        if (mf.GetComponent<Oscillator>() != null) return KindMine;
        return IsHanging(mf, platforms) ? KindSpireFlipped : KindSpire;
    }

    public static Mesh CreateMesh(Vector3 scale, int kind)
    {
        scale = new Vector3(Mathf.Abs(scale.x), Mathf.Abs(scale.y), Mathf.Abs(scale.z));
        if (scale.x < 0.01f || scale.y < 0.01f || scale.z < 0.01f) return null;
        switch (kind)
        {
            case KindSlab: return Slab(scale);
            case KindMine: return Mine(scale);
            default: return Spire(scale, kind == KindSpireFlipped);
        }
    }

    // Start/finish maydonchalari va balandligidan ancha keng bo'lgan kublar — platforma
    static bool IsPlatform(MeshFilter mf)
    {
        if (mf.CompareTag("Friendly") || mf.CompareTag("Finish")) return true;
        Vector3 s = mf.transform.lossyScale;
        return Mathf.Abs(s.x) >= Mathf.Abs(s.y) * 2f;
    }

    // Tagida (yaqinida) platforma bo'lmasa, to'siq yuqoridan osilib turgan hisoblanadi
    static bool IsHanging(MeshFilter mf, List<Bounds> platforms)
    {
        if (mf.name.Contains("Hang")) return true;
        Bounds b = mf.GetComponent<Renderer>().bounds;
        bool found = false;
        float top = float.MinValue;
        foreach (var p in platforms)
        {
            if (p.max.x < b.min.x || p.min.x > b.max.x) continue; // tagida emas
            if (p.max.y > b.center.y) continue;                    // pastda emas
            found = true;
            top = Mathf.Max(top, p.max.y);
        }
        return found && b.min.y - top > 1.5f;
    }

    // Qirralari kesilgan plita
    static Mesh Slab(Vector3 scale)
    {
        float by = Mathf.Min(Bevel / scale.y, 0.25f);
        float fx = 1f - 2f * Mathf.Min(Bevel / scale.x, 0.2f);
        float fz = 1f - 2f * Mathf.Min(Bevel / scale.z, 0.2f);
        return Build(scale,
            new Vector3(-0.5f, fx, fz),
            new Vector3(-0.5f + by, 1f, 1f),
            new Vector3(0.5f - by, 1f, 1f),
            new Vector3(0.5f, fx, fz));
    }

    // Asosi keng, uchi toraygan ustun. flipped = true bo'lsa, uchi pastga qaraydi.
    static Mesh Spire(Vector3 scale, bool flipped)
    {
        float tip = Mathf.Min(0.7f / scale.y, 0.3f);
        float foot = Mathf.Min(Bevel / scale.y, 0.2f);
        float d = flipped ? -1f : 1f;
        var rings = new[]
        {
            new Vector3(-0.5f * d, 0.94f, 0.94f),
            new Vector3((-0.5f + foot) * d, 1f, 1f),
            new Vector3((0.5f - tip) * d, 0.8f, 0.8f),
            new Vector3(0.5f * d, 0.45f, 0.45f)
        };
        if (flipped) System.Array.Reverse(rings);
        return Build(scale, rings);
    }

    // Ikki uchi toraygan olmos shakli
    static Mesh Mine(Vector3 scale)
    {
        return Build(scale,
            new Vector3(-0.5f, 0.4f, 0.4f),
            new Vector3(-0.16f, 1f, 1f),
            new Vector3(0.16f, 1f, 1f),
            new Vector3(0.5f, 0.4f, 0.4f));
    }

    // Shaklni pastdan yuqoriga "halqalar" bo'yicha yasaydi. Har bir halqa: (y, x bo'yicha eni, z bo'yicha eni),
    // hammasi kubning o'z o'lchamiga nisbatan (-0.5..0.5). Kesim — burchaklari kesilgan to'rtburchak (8 nuqta).
    static Mesh Build(Vector3 scale, params Vector3[] rings)
    {
        float cx = Mathf.Min(Bevel * 2f / scale.x, 0.25f);
        float cz = Mathf.Min(Bevel * 2f / scale.z, 0.25f);
        Vector2[] outline =
        {
            new Vector2(0.5f - cx, -0.5f), new Vector2(0.5f, -0.5f + cz),
            new Vector2(0.5f, 0.5f - cz), new Vector2(0.5f - cx, 0.5f),
            new Vector2(-0.5f + cx, 0.5f), new Vector2(-0.5f, 0.5f - cz),
            new Vector2(-0.5f, -0.5f + cz), new Vector2(-0.5f + cx, -0.5f)
        };
        int n = outline.Length;

        var points = new Vector3[rings.Length][];
        for (int r = 0; r < rings.Length; r++)
        {
            points[r] = new Vector3[n];
            for (int i = 0; i < n; i++)
                points[r][i] = new Vector3(outline[i].x * rings[r].y, rings[r].x, outline[i].y * rings[r].z);
        }

        var verts = new List<Vector3>();
        // Yon tomonlar
        for (int r = 0; r < rings.Length - 1; r++)
            for (int i = 0; i < n; i++)
            {
                int j = (i + 1) % n;
                AddTri(verts, points[r][i], points[r][j], points[r + 1][j]);
                AddTri(verts, points[r][i], points[r + 1][j], points[r + 1][i]);
            }
        // Past va ustki qopqoqlar
        foreach (var cap in new[] { points[0], points[rings.Length - 1] })
            for (int i = 1; i < n - 1; i++)
                AddTri(verts, cap[0], cap[i], cap[i + 1]);

        var tris = new int[verts.Count];
        var uvs = new Vector2[verts.Count];
        for (int i = 0; i < tris.Length; i++)
        {
            tris[i] = i;
            uvs[i] = new Vector2(verts[i].x + 0.5f, verts[i].y + 0.5f);
        }

        var mesh = new Mesh { name = StyledName };
        mesh.SetVertices(verts);
        mesh.uv = uvs;
        mesh.triangles = tris;
        mesh.RecalculateNormals(); // uchlar umumiy emas, shuning uchun qirralar aniq ko'rinadi
        mesh.RecalculateBounds();
        return mesh;
    }

    // Uchburchakni tashqariga qaratib qo'shadi (shakl qavariq va markazi (0,0,0) da)
    static void AddTri(List<Vector3> verts, Vector3 a, Vector3 b, Vector3 c)
    {
        Vector3 normal = Vector3.Cross(b - a, c - a);
        if (normal.sqrMagnitude < 1e-10f) return;
        if (Vector3.Dot(normal, a + b + c) < 0f)
        {
            Vector3 t = b; b = c; c = t;
        }
        verts.Add(a);
        verts.Add(b);
        verts.Add(c);
    }
}
