using System.IO;
using UnityEditor;
using UnityEngine;

public static class ItemIconGenerator
{
    private const int IconSize = 256;
    private const string OutputFolder = "Assets/Resources/Icons/";
    private const float OrthographicPadding = 1.8f;
    private const float CameraDistance = 10f;
    private const float LightIntensity = 1.2f;

    [MenuItem("Tools/Generate Icons From Scene")]
    public static void Generate()
    {
        IconGenerationSource source = Object.FindObjectOfType<IconGenerationSource>();

        if (source == null)
        {
            Debug.LogError("[ItemIconGenerator] No IconGenerationSource found in the scene.");
            return;
        }

        GameObject[] sources = source.IconSources;

        if (sources == null || sources.Length == 0)
        {
            Debug.LogWarning("[ItemIconGenerator] IconGenerationSource has no icon sources assigned.");
            return;
        }

        EnsureOutputFolder();

        int generatedCount = 0;

        try
        {
            for (int i = 0; i < sources.Length; i++)
            {
                GameObject prefab = sources[i];

                if (prefab == null)
                {
                    Debug.LogWarning($"[ItemIconGenerator] Source at index {i} is null. Skipping.");
                    continue;
                }

                EditorUtility.DisplayProgressBar(
                    "Generating Icons",
                    $"Rendering {prefab.name} ({i + 1}/{sources.Length})",
                    (float)i / sources.Length);

                Texture2D icon = RenderIconToTexture(prefab);

                string iconPath = OutputFolder + prefab.name + ".png";
                File.WriteAllBytes(iconPath, icon.EncodeToPNG());
                AssetDatabase.ImportAsset(iconPath, ImportAssetOptions.ForceUpdate);

                Object.DestroyImmediate(icon);
                generatedCount++;
            }
        }
        finally
        {
            EditorUtility.ClearProgressBar();
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"[ItemIconGenerator] Generated {generatedCount} icons to {OutputFolder}");
    }

    private static void EnsureOutputFolder()
    {
        if (AssetDatabase.IsValidFolder(OutputFolder))
        {
            return;
        }

        if (Directory.Exists(OutputFolder) == false)
        {
            Directory.CreateDirectory(OutputFolder);
        }

        AssetDatabase.Refresh();
    }

    private static Texture2D RenderIconToTexture(GameObject modelPrefab)
    {
        GameObject renderRoot = new GameObject("IconRenderRoot");
        GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(modelPrefab);
        instance.transform.SetParent(renderRoot.transform);

        GameObject cameraGO = new GameObject("IconCamera");
        Camera cam = cameraGO.AddComponent<Camera>();
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0f, 0f, 0f, 0f);
        cam.orthographic = true;

        GameObject lightGO = new GameObject("IconLight");
        Light light = lightGO.AddComponent<Light>();
        light.type = LightType.Directional;
        light.transform.rotation = Quaternion.Euler(45f, -30f, 0f);
        light.intensity = LightIntensity;

        FitCameraToBounds(cam, instance);

        RenderTexture rt = new RenderTexture(IconSize, IconSize, 24, RenderTextureFormat.ARGB32);
        rt.antiAliasing = 4;
        cam.targetTexture = rt;
        RenderTexture.active = rt;
        cam.Render();

        Texture2D tex = new Texture2D(IconSize, IconSize, TextureFormat.RGBA32, false);
        tex.ReadPixels(new Rect(0f, 0f, IconSize, IconSize), 0, 0);
        tex.Apply();

        RenderTexture.active = null;
        cam.targetTexture = null;
        rt.Release();
        Object.DestroyImmediate(rt);

        Object.DestroyImmediate(renderRoot);
        Object.DestroyImmediate(cameraGO);
        Object.DestroyImmediate(lightGO);

        return tex;
    }

    private static void FitCameraToBounds(Camera cam, GameObject instance)
    {
        Renderer[] renderers = instance.GetComponentsInChildren<Renderer>();

        if (renderers.Length == 0)
        {
            cam.transform.position = new Vector3(-2f, 2f, -2f);
            cam.transform.LookAt(instance.transform.position);
            cam.orthographicSize = 1f;
            return;
        }

        Bounds bounds = renderers[0].bounds;

        for (int i = 1; i < renderers.Length; i++)
        {
            bounds.Encapsulate(renderers[i].bounds);
        }

        Vector3 offset = new Vector3(-1f, 1f, -1f).normalized * CameraDistance;
        cam.transform.position = bounds.center + offset;
        cam.transform.LookAt(bounds.center);

        cam.orthographicSize = bounds.extents.magnitude * OrthographicPadding;
    }
}