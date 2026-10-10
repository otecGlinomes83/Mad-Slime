using System;
using System.IO;
using Skins;
using Shop;
using UnityEditor;
using UnityEngine;

namespace EditorTools
{
    public class SkinItemFactory : EditorWindow
    {
        private const string PrefsKey = "MadSlime.SkinItemFactory";
        private const int IconSize = 256;
        private const float IconCameraDistance = 10f;
        private const float IconOrthographicPadding = 1.8f;
        private const float IconLightIntensity = 1.2f;
        private const string DefaultSkinsPath = "Assets/MadSlime/Scriptables/Skins";
        private const string DefaultPrefabsPath = "Assets/MadSlime/Resources/Models/Player";
        private const string DefaultIconsPath = "Assets/MadSlime/Resources/Icons/Skins";
        private const string DefaultShopContentPath = "Assets/MadSlime/Scriptables/Shop/ShopContent.asset";

        private GameObject _sourceModel;
        private ShopContent _shopContent;
        private DefaultAsset _skinsFolder;
        private DefaultAsset _prefabsFolder;
        private DefaultAsset _iconsFolder;
        private string _skinId = "";
        private SkinRarity _rarity = SkinRarity.Rare;
        private bool _forceIcon;

        [MenuItem("Mad Slime/Skin Factory")]
        private static void Open()
        {
            GetWindow<SkinItemFactory>("Skin Factory");
        }

        private void OnEnable()
        {
            LoadState();
        }

        private void OnDisable()
        {
            SaveState();
        }

        private void OnGUI()
        {
            EditorGUILayout.HelpBox(
                "Drop a model or a prefab into Source Model. A prefab is used as is; a raw model is saved as " +
                "'<Name>Skin.prefab' into the Prefabs Folder (an existing file is reused, never duplicated). The tool " +
                "renders the icon into the Icons Folder as '<Id>.png' (kept unless Force Icon is on), creates the " +
                "SkinItem asset in the Skins Folder and appends it to Shop Content. The id must be unique — it is the " +
                "save key of the skin and must never change after release.",
                MessageType.Info);

            _sourceModel = (GameObject)EditorGUILayout.ObjectField("Source Model", _sourceModel, typeof(GameObject), false);
            _shopContent = (ShopContent)EditorGUILayout.ObjectField("Shop Content", _shopContent, typeof(ShopContent), false);
            _skinsFolder = (DefaultAsset)EditorGUILayout.ObjectField("Skins Folder", _skinsFolder, typeof(DefaultAsset), false);
            _prefabsFolder = (DefaultAsset)EditorGUILayout.ObjectField("Prefabs Folder", _prefabsFolder, typeof(DefaultAsset), false);
            _iconsFolder = (DefaultAsset)EditorGUILayout.ObjectField("Icons Folder", _iconsFolder, typeof(DefaultAsset), false);

            EditorGUILayout.Space();

            _skinId = EditorGUILayout.TextField("Skin Id", _skinId);
            _rarity = (SkinRarity)EditorGUILayout.EnumPopup("Rarity", _rarity);
            _forceIcon = EditorGUILayout.Toggle("Force Icon", _forceIcon);

            EditorGUILayout.Space();

            if (GUILayout.Button("Create Skin"))
            {
                CreateSkin();
            }
        }

        private void CreateSkin()
        {
            if (Validate() == false)
            {
                return;
            }

            string skinsPath = GetFolderPath(_skinsFolder);
            string prefabsPath = GetFolderPath(_prefabsFolder);
            string iconsPath = GetFolderPath(_iconsFolder);
            string assetPath = $"{skinsPath}/{_skinId}.asset";

            if (IsIdUsed(_skinId) == true)
            {
                Debug.LogError($"[SkinFactory] Skin id '{_skinId}' is already used in ShopContent '{_shopContent.name}'.");
                return;
            }

            if (File.Exists(assetPath) == true)
            {
                Debug.LogError($"[SkinFactory] SkinItem asset already exists: {assetPath}");
                return;
            }

            GameObject prefab = ResolveSkinPrefab(prefabsPath);

            if (prefab == null)
            {
                return;
            }

            EnsureFolder(skinsPath);

            string iconPath = $"{iconsPath}/{_skinId}.png";

            RenderIconIfNeeded(prefab, iconPath);

            SkinItem skin = ScriptableObject.CreateInstance<SkinItem>();

            SerializedObject serialized = new SerializedObject(skin);
            serialized.FindProperty("_model").objectReferenceValue = prefab;
            serialized.FindProperty("_icon").objectReferenceValue = AssetDatabase.LoadAssetAtPath<Sprite>(iconPath);
            serialized.FindProperty("_rarity").enumValueIndex = (int)_rarity;
            serialized.FindProperty("_id").stringValue = _skinId;
            serialized.ApplyModifiedPropertiesWithoutUndo();

            AssetDatabase.CreateAsset(skin, assetPath);
            AppendToShopContent(skin);

            AssetDatabase.SaveAssetIfDirty(_shopContent);
            AssetDatabase.Refresh();

            Selection.activeObject = skin;
            EditorGUIUtility.PingObject(skin);

            Debug.Log($"[SkinFactory] Skin '{_skinId}' created at {assetPath} and appended to '{_shopContent.name}'.");
        }

        private bool Validate()
        {
            if (_sourceModel == null)
            {
                Debug.LogError("[SkinFactory] Source Model is not assigned.");
                return false;
            }

            if (_shopContent == null)
            {
                Debug.LogError("[SkinFactory] Shop Content is not assigned.");
                return false;
            }

            if (_skinsFolder == null || _prefabsFolder == null || _iconsFolder == null)
            {
                Debug.LogError("[SkinFactory] A folder is not assigned. Fill Skins, Prefabs and Icons folders.");
                return false;
            }

            if (string.IsNullOrWhiteSpace(_skinId) == true)
            {
                Debug.LogError("[SkinFactory] Skin Id is empty. It is the save key of the skin.");
                return false;
            }

            if (_sourceModel.GetComponentsInChildren<Renderer>().Length == 0)
            {
                Debug.LogError($"[SkinFactory] '{_sourceModel.name}' has no Renderers — there is nothing to render the icon from.");
                return false;
            }

            return true;
        }

        private bool IsIdUsed(string skinId)
        {
            foreach (SkinItem item in _shopContent.SkinItems)
            {
                if (item != null && item.Id == skinId)
                {
                    return true;
                }
            }

            return false;
        }

        private GameObject ResolveSkinPrefab(string prefabsPath)
        {
            string sourcePath = AssetDatabase.GetAssetPath(_sourceModel);

            if (sourcePath.EndsWith(".prefab", StringComparison.OrdinalIgnoreCase) == true)
            {
                return _sourceModel;
            }

            EnsureFolder(prefabsPath);

            string prefabPath = $"{prefabsPath}/{_sourceModel.name}Skin.prefab";

            GameObject existing = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);

            if (existing != null)
            {
                return existing;
            }

            GameObject instance;

            if (string.IsNullOrEmpty(sourcePath) == true)
            {
                instance = UnityEngine.Object.Instantiate(_sourceModel);
            }
            else
            {
                instance = (GameObject)PrefabUtility.InstantiatePrefab(_sourceModel);
            }

            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(instance, prefabPath);
            DestroyImmediate(instance);

            return prefab;
        }

        private void RenderIconIfNeeded(GameObject prefab, string iconPath)
        {
            if (File.Exists(iconPath) == true && _forceIcon == false)
            {
                return;
            }

            EnsureFolder(Path.GetDirectoryName(iconPath)?.Replace('\\', '/'));

            int iconLayer = ResolveFreeLayer();

            if (iconLayer < 0)
            {
                Debug.LogError("[SkinFactory] No unnamed layer left in 9-31 for icon rendering. Free one in Project Settings → Tags and Layers.");
                return;
            }

            Texture2D texture = RenderModelToTexture(prefab, iconLayer);

            File.WriteAllBytes(iconPath, texture.EncodeToPNG());
            DestroyImmediate(texture);

            AssetDatabase.ImportAsset(iconPath);

            TextureImporter importer = AssetImporter.GetAtPath(iconPath) as TextureImporter;

            if (importer == null)
            {
                Debug.LogWarning($"[SkinFactory] No TextureImporter for '{iconPath}'.");
                return;
            }

            if (importer.textureType != TextureImporterType.Sprite)
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.SaveAndReimport();
            }
        }

        private void AppendToShopContent(SkinItem skin)
        {
            SerializedObject serialized = new SerializedObject(_shopContent);
            SerializedProperty items = serialized.FindProperty("_skinItems");

            if (items == null || items.isArray == false)
            {
                throw new InvalidOperationException(
                    $"[SkinFactory] ShopContent '{_shopContent.name}' has no _skinItems list.");
            }

            items.InsertArrayElementAtIndex(items.arraySize);
            items.GetArrayElementAtIndex(items.arraySize - 1).objectReferenceValue = skin;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static int ResolveFreeLayer()
        {
            for (int i = 9; i <= 31; i++)
            {
                if (LayerMask.LayerToName(i) == "")
                {
                    return i;
                }
            }

            return -1;
        }

        private static void SetLayerRecursively(GameObject root, int layer)
        {
            Transform[] children = root.GetComponentsInChildren<Transform>(true);

            for (int i = 0; i < children.Length; i++)
            {
                children[i].gameObject.layer = layer;
            }
        }

        private static Texture2D RenderModelToTexture(GameObject modelPrefab, int iconLayer)
        {
            GameObject renderRoot = new GameObject("IconRenderRoot");
            renderRoot.layer = iconLayer;

            GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(modelPrefab, renderRoot.transform);
            SetLayerRecursively(instance, iconLayer);

            GameObject cameraGO = new GameObject("IconCamera");
            cameraGO.transform.SetParent(renderRoot.transform);
            cameraGO.layer = iconLayer;

            Camera cam = cameraGO.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0f, 0f, 0f, 0f);
            cam.orthographic = true;
            cam.cullingMask = 1 << iconLayer;

            GameObject lightGO = new GameObject("IconLight");
            lightGO.transform.SetParent(renderRoot.transform);
            lightGO.layer = iconLayer;

            Light light = lightGO.AddComponent<Light>();
            light.type = LightType.Directional;
            light.transform.rotation = Quaternion.Euler(45f, -30f, 0f);
            light.intensity = IconLightIntensity;
            light.cullingMask = 1 << iconLayer;
            light.shadows = LightShadows.None;

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
            DestroyImmediate(rt);
            DestroyImmediate(renderRoot);

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

            Vector3 offset = new Vector3(-1f, 1f, -1f).normalized * IconCameraDistance;
            cam.transform.position = bounds.center + offset;
            cam.transform.LookAt(bounds.center);

            cam.orthographicSize = bounds.extents.magnitude * IconOrthographicPadding;
        }

        private static string GetFolderPath(DefaultAsset folder)
        {
            string path = AssetDatabase.GetAssetPath(folder);

            if (File.Exists(path) == true)
            {
                path = Path.GetDirectoryName(path);
            }

            return path;
        }

        private static void EnsureFolder(string folder)
        {
            if (string.IsNullOrEmpty(folder) == true || AssetDatabase.IsValidFolder(folder) == true)
            {
                return;
            }

            string parent = Path.GetDirectoryName(folder)?.Replace('\\', '/');
            string leaf = Path.GetFileName(folder);

            if (string.IsNullOrEmpty(parent) == false && AssetDatabase.IsValidFolder(parent) == false)
            {
                EnsureFolder(parent);
            }

            AssetDatabase.CreateFolder(parent, leaf);
        }

        private void LoadState()
        {
            if (EditorPrefs.HasKey(PrefsKey) == true)
            {
                WindowState state = JsonUtility.FromJson<WindowState>(EditorPrefs.GetString(PrefsKey));

                if (state != null)
                {
                    _shopContent = LoadStateAsset<ShopContent>(state.ShopContentPath);
                    _skinsFolder = LoadStateAsset<DefaultAsset>(state.SkinsPath);
                    _prefabsFolder = LoadStateAsset<DefaultAsset>(state.PrefabsPath);
                    _iconsFolder = LoadStateAsset<DefaultAsset>(state.IconsPath);
                    _rarity = state.Rarity;
                    _forceIcon = state.ForceIcon;

                    return;
                }
            }

            _shopContent = AssetDatabase.LoadAssetAtPath<ShopContent>(DefaultShopContentPath);
            _skinsFolder = AssetDatabase.LoadAssetAtPath<DefaultAsset>(DefaultSkinsPath);
            _prefabsFolder = AssetDatabase.LoadAssetAtPath<DefaultAsset>(DefaultPrefabsPath);
            _iconsFolder = AssetDatabase.LoadAssetAtPath<DefaultAsset>(DefaultIconsPath);
        }

        private void SaveState()
        {
            WindowState state = new WindowState();
            state.ShopContentPath = GetAssetPathOrNull(_shopContent);
            state.SkinsPath = GetAssetPathOrNull(_skinsFolder);
            state.PrefabsPath = GetAssetPathOrNull(_prefabsFolder);
            state.IconsPath = GetAssetPathOrNull(_iconsFolder);
            state.Rarity = _rarity;
            state.ForceIcon = _forceIcon;

            EditorPrefs.SetString(PrefsKey, JsonUtility.ToJson(state));
        }

        private static T LoadStateAsset<T>(string path) where T : UnityEngine.Object
        {
            if (string.IsNullOrEmpty(path) == true)
            {
                return null;
            }

            return AssetDatabase.LoadAssetAtPath<T>(path);
        }

        private static string GetAssetPathOrNull(UnityEngine.Object asset)
        {
            if (asset == null)
            {
                return string.Empty;
            }

            return AssetDatabase.GetAssetPath(asset);
        }

        [Serializable]
        private class WindowState
        {
            public string ShopContentPath;
            public string SkinsPath;
            public string PrefabsPath;
            public string IconsPath;
            public SkinRarity Rarity;
            public bool ForceIcon;
        }
    }
}
