using System;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEngine;

namespace EditorTools
{
    public sealed class DigitAtlasBake : EditorWindow
    {
        private const string Digits = "0123456789+";
        private const string AtlasPath = "Assets/Fx/DigitAtlas.png";

        private TMP_FontAsset _font;
        private int _cellSize = 128;
        private float _glyphFill = 0.8f;

        [MenuItem("Mad Slime/Bake Digit Atlas")]
        private static void Open()
        {
            GetWindow<DigitAtlasBake>("Digit Atlas Bake");
        }

        private void OnGUI()
        {
            EditorGUILayout.HelpBox(
                "Renders 0-9 and '+' from the font into a single-row atlas (Assets/Fx/DigitAtlas.png) for the " +
                "mass popup. Setup: create a material with the MadSlime/DigitParticle shader, assign this atlas, " +
                "use it in your particle system's Renderer and add Custom1.x to its Custom Vertex Streams. " +
                "The code picks the digit frame per particle.",
                MessageType.Info);

            _font = (TMP_FontAsset)EditorGUILayout.ObjectField("Font (empty = TMP default)", _font, typeof(TMP_FontAsset), false);
            _cellSize = EditorGUILayout.IntSlider("Cell Size", _cellSize, 64, 256);
            _glyphFill = EditorGUILayout.Slider("Glyph Fill", _glyphFill, 0.5f, 1f);

            EditorGUILayout.Space();

            if (GUILayout.Button("Bake"))
            {
                Bake();
            }
        }

        private void Bake()
        {
            TMP_FontAsset font = _font != null ? _font : TMP_Settings.defaultFontAsset;

            if (font == null)
            {
                Debug.LogWarning("[DigitAtlas] No font assigned and TMP default font is missing.");
                return;
            }

            int count = Digits.Length;
            int width = _cellSize * count;
            int height = _cellSize;

            RenderTexture renderTexture = RenderTexture.GetTemporary(width, height, 0, RenderTextureFormat.ARGB32);

            Camera camera = CreateHiddenGameObject<Camera>("DigitAtlasCamera").GetComponent<Camera>();
            camera.orthographic = true;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0f, 0f, 0f, 0f);
            camera.targetTexture = renderTexture;

            Canvas canvas = CreateHiddenGameObject<Canvas>("DigitAtlasCanvas").GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceCamera;
            canvas.worldCamera = camera;
            canvas.planeDistance = 1f;

            for (int i = 0; i < count; i++)
            {
                TextMeshProUGUI label = CreateHiddenGameObject<TextMeshProUGUI>(Digits[i].ToString()).GetComponent<TextMeshProUGUI>();
                label.transform.SetParent(canvas.transform, false);

                RectTransform rect = label.rectTransform;
                rect.anchorMin = new Vector2((float)i / count, 0f);
                rect.anchorMax = new Vector2((float)(i + 1) / count, 1f);
                rect.offsetMin = Vector2.zero;
                rect.offsetMax = Vector2.zero;

                label.text = Digits[i].ToString();
                label.font = font;
                label.fontSize = _cellSize * _glyphFill;
                label.alignment = TextAlignmentOptions.Center;
                label.raycastTarget = false;
                label.enableWordWrapping = false;
            }

            Canvas.ForceUpdateCanvases();
            camera.Render();

            RenderTexture previous = RenderTexture.active;
            RenderTexture.active = renderTexture;

            Texture2D atlas = new Texture2D(width, height, TextureFormat.RGBA32, false);
            atlas.ReadPixels(new Rect(0f, 0f, width, height), 0, 0);
            atlas.Apply();

            RenderTexture.active = previous;
            RenderTexture.ReleaseTemporary(renderTexture);

            WriteAtlas(atlas);

            DestroyImmediate(camera.gameObject);
            DestroyImmediate(canvas.gameObject);
            DestroyImmediate(atlas);
        }

        private void WriteAtlas(Texture2D atlas)
        {
            string directory = Path.GetDirectoryName(AtlasPath);

            if (Directory.Exists(directory) == false)
            {
                Directory.CreateDirectory(directory);
            }

            File.WriteAllBytes(AtlasPath, atlas.EncodeToPNG());
            AssetDatabase.ImportAsset(AtlasPath);

            TextureImporter importer = (TextureImporter)AssetImporter.GetAtPath(AtlasPath);
            importer.npotScale = TextureImporterNPOTScale.None;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.SaveAndReimport();

            Debug.Log($"[DigitAtlas] Baked {atlas.width}x{atlas.height} atlas to '{AtlasPath}' from {_font?.name ?? "TMP default font"}.");
        }

        private static GameObject CreateHiddenGameObject<T>(string name) where T : Component
        {
            GameObject gameObject = new GameObject(name, typeof(T))
            {
                hideFlags = HideFlags.HideAndDontSave
            };

            return gameObject;
        }
    }
}
