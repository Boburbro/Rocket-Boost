using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Scene oynasida (Play bosmasdan oldin) ham kublarni o'yindagi shaklda ko'rsatadi.
// Bu faqat ko'rinish: sahna fayliga hech narsa yozilmaydi. Saqlashdan va Play'dan oldin
// kublar asl holiga (oddiy Cube) qaytariladi, keyin ko'rinish yana qo'yiladi.
[InitializeOnLoad]
static class LevelShapesPreview
{
    // Har bir kub uchun oxirgi qo'yilgan shaklning "imzosi" (o'lcham + shakl turi)
    static readonly Dictionary<MeshFilter, Vector4> applied = new Dictionary<MeshFilter, Vector4>();
    static double nextCheck;

    static LevelShapesPreview()
    {
        EditorApplication.update += Tick;
        EditorSceneManager.sceneSaving += (scene, path) => Restore();
        EditorApplication.playModeStateChanged += state =>
        {
            if (state == PlayModeStateChange.ExitingEditMode) Restore();
        };
    }

    // Sekundiga ikki marta tekshiradi: yangi kub qo'shilsa yoki o'lchami o'zgarsa, shaklni yangilaydi
    static void Tick()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling) return;
        if (EditorApplication.timeSinceStartup < nextCheck) return;
        nextCheck = EditorApplication.timeSinceStartup + 0.5;

        if (Object.FindFirstObjectByType<Movement>() == null) return;

        var cubes = LevelShapes.FindCubes();
        var platforms = LevelShapes.PlatformBounds(cubes);
        bool changed = false;

        foreach (var mf in cubes)
        {
            Vector3 scale = mf.transform.lossyScale;
            int kind = LevelShapes.KindOf(mf, platforms);
            var signature = new Vector4(scale.x, scale.y, scale.z, kind);
            bool styled = mf.sharedMesh != null && mf.sharedMesh.name == LevelShapes.StyledName;
            if (styled && applied.TryGetValue(mf, out Vector4 old) && old == signature) continue;

            Mesh mesh = LevelShapes.CreateMesh(scale, kind);
            if (mesh == null) continue;
            mesh.hideFlags = HideFlags.HideAndDontSave;
            if (styled) Object.DestroyImmediate(mf.sharedMesh);
            mf.sharedMesh = mesh;
            applied[mf] = signature;
            changed = true;
        }

        if (changed) SceneView.RepaintAll();
    }

    // Kublarni asl holiga qaytaradi (sahna fayliga doim oddiy Cube yoziladi)
    static void Restore()
    {
        Mesh cube = Resources.GetBuiltinResource<Mesh>("Cube.fbx");
        if (cube == null) return;
        foreach (var mf in LevelShapes.FindCubes())
        {
            Mesh m = mf.sharedMesh;
            if (m != null && m.name != LevelShapes.StyledName) continue;
            if (m != null) Object.DestroyImmediate(m);
            mf.sharedMesh = cube;
        }
        applied.Clear();
        nextCheck = EditorApplication.timeSinceStartup + 0.5;
    }
}
