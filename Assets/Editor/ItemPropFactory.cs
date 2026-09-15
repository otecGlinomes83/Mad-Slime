using System;
using System.Collections.Generic;
using System.IO;
using Items;
using Scriptables;
using Skills;
using UnityEditor;
using UnityEngine;

namespace EditorTools
{
    public sealed class ItemPropFactory : EditorWindow
    {
        private const string PrefsKey = "MadSlime.ItemPropFactory";
        private const int IconSize = 256;
        private const float IconCameraDistance = 10f;
        private const float IconOrthographicPadding = 1.8f;
        private const float IconLightIntensity = 1.2f;

        private DefaultAsset _modelsFolder;
        private DefaultAsset _prefabsFolder;
        private DefaultAsset _definitionsFolder;
        private DefaultAsset _iconsFolder;
        private PropSet _propSet;
        private TierTable _tierTable;
        private Material _ghostMaterial;
        private GhostFadeConfig _ghostFadeConfig;
        private bool _forceIcons;
        private int _iconLayer;

        [MenuItem("Mad Slime/Prop Factory")]
        private static void Open()
        {
            GetWindow<ItemPropFactory>("Prop Factory");
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
                "Point the fields at folders and press Generate — any open scene is fine, the icon camera is layer-isolated " +
                "and captures only the model. For every model prefab in the Models Folder the tool renders an icon, bakes " +
                "one ItemDefinition per tier from the Tier Table and creates an Item_<Model> prefab (model + BoxCollider " +
                "by the render bounds + Item), then fills the Prop Set. Existing icons are kept — drop your own PNG named " +
                "Item_<Model>.png into the Icons Folder to override. Force Icons re-renders all of them.",
                MessageType.Info);

            _modelsFolder = (DefaultAsset)EditorGUILayout.ObjectField("Models Folder", _modelsFolder, typeof(DefaultAsset), false);
            _prefabsFolder = (DefaultAsset)EditorGUILayout.ObjectField("Prefabs Folder", _prefabsFolder, typeof(DefaultAsset), false);
            _definitionsFolder = (DefaultAsset)EditorGUILayout.ObjectField("Definitions Folder", _definitionsFolder, typeof(DefaultAsset), false);
            _iconsFolder = (DefaultAsset)EditorGUILayout.ObjectField("Icons Folder", _iconsFolder, typeof(DefaultAsset), false);
            _propSet = (PropSet)EditorGUILayout.ObjectField("Prop Set", _propSet, typeof(PropSet), false);
            _tierTable = (TierTable)EditorGUILayout.ObjectField("Tier Table", _tierTable, typeof(TierTable), false);
            _ghostMaterial = (Material)EditorGUILayout.ObjectField("Ghost Material", _ghostMaterial, typeof(Material), false);
            _ghostFadeConfig = (GhostFadeConfig)EditorGUILayout.ObjectField("Ghost Fade Config", _ghostFadeConfig, typeof(GhostFadeConfig), false);
            _forceIcons = EditorGUILayout.Toggle("Force Icons", _forceIcons);

            EditorGUILayout.Space();

            if (GUILayout.Button("Generate"))
            {
                Generate();
            }
        }

        private void Generate()
        {
            if (Validate() == false)
            {
                return;
            }

            string modelsPath = GetFolderPath(_modelsFolder);
            string prefabsPath = GetFolderPath(_prefabsFolder);
            string definitionsPath = GetFolderPath(_definitionsFolder);
            string iconsPath = GetFolderPath(_iconsFolder);

            List<ModelJob> jobs = CollectModelJobs(modelsPath, prefabsPath, iconsPath);

            if (jobs == null)
            {
                return;
            }

            int collectableLayer = LayerMask.NameToLayer("Collectable");

            if (collectableLayer < 0)
            {
                Debug.LogError("[PropFactory] Layer 'Collectable' does not exist. Add it in Project Settings → Tags and Layers.");
                return;
            }

            _iconLayer = ResolveFreeLayer();

            if (_iconLayer < 0)
            {
                Debug.LogError("[PropFactory] No unnamed layer left in 9-31 for icon rendering. Free one in Project Settings → Tags and Layers.");
                return;
            }

            EnsureFolder(prefabsPath);
            EnsureFolder(definitionsPath);
            EnsureFolder(iconsPath);

            int renderedIcons;

            try
            {
                renderedIcons = RenderIcons(jobs);

                if (renderedIcons > 0)
                {
                    AssetDatabase.Refresh();
                }

                for (int i = 0; i < jobs.Count; i++)
                {
                    EnsureSpriteImport(jobs[i].IconPath);
                }

                BakeDefinitions(jobs, definitionsPath);
                CreateItemPrefabs(jobs, collectableLayer);
            }
            finally
            {
                EditorUtility.ClearProgressBar();
            }

            WritePropSet(jobs);

            AssetDatabase.SaveAssetIfDirty(_propSet);

            int variantsCount = jobs.Count * _tierTable.Entries.Count;

            Debug.Log(
                $"[PropFactory] Done: {jobs.Count} props, {renderedIcons} icons rendered ({jobs.Count - renderedIcons} kept), " +
                $"{variantsCount} definitions, {variantsCount} variants into PropSet '{_propSet.name}'.");
        }

        private bool Validate()
        {
            List<string> missing = new List<string>();

            if (_modelsFolder == null)
            {
                missing.Add("Models Folder");
            }

            if (_prefabsFolder == null)
            {
                missing.Add("Prefabs Folder");
            }

            if (_definitionsFolder == null)
            {
                missing.Add("Definitions Folder");
            }

            if (_iconsFolder == null)
            {
                missing.Add("Icons Folder");
            }

            if (_propSet == null)
            {
                missing.Add("Prop Set");
            }

            if (_tierTable == null)
            {
                missing.Add("Tier Table");
            }

            if (_ghostMaterial == null)
            {
                missing.Add("Ghost Material");
            }

            if (_ghostFadeConfig == null)
            {
                missing.Add("Ghost Fade Config");
            }

            if (missing.Count > 0)
            {
                Debug.LogError("[PropFactory] Not assigned: " + string.Join(", ", missing));
                return false;
            }

            if (_tierTable.Entries.Count == 0)
            {
                Debug.LogError($"[PropFactory] Tier Table '{_tierTable.name}' has no entries.");
                return false;
            }

            return true;
        }

        private List<ModelJob> CollectModelJobs(string modelsPath, string prefabsPath, string iconsPath)
        {
            string[] modelGuids = AssetDatabase.FindAssets("t:Prefab", new[] { modelsPath });

            if (modelGuids.Length == 0)
            {
                Debug.LogError($"[PropFactory] No model prefabs found in '{modelsPath}'.");
                return null;
            }

            List<ModelJob> jobs = new List<ModelJob>(modelGuids.Length);
            Dictionary<string, string> nameToPath = new Dictionary<string, string>();

            for (int i = 0; i < modelGuids.Length; i++)
            {
                string modelPath = AssetDatabase.GUIDToAssetPath(modelGuids[i]);
                GameObject model = AssetDatabase.LoadAssetAtPath<GameObject>(modelPath);

                if (model == null)
                {
                    continue;
                }

                if (model.TryGetComponent<Item>(out Item existingItem) == true)
                {
                    continue;
                }

                if (nameToPath.ContainsKey(model.name) == true)
                {
                    Debug.LogError(
                        $"[PropFactory] Duplicate model name '{model.name}': '{nameToPath[model.name]}' and '{modelPath}'. " +
                        "Model names must be unique — they define prefab, definition and icon names.");
                    return null;
                }

                if (model.GetComponentsInChildren<Renderer>().Length == 0)
                {
                    Debug.LogError(
                        $"[PropFactory] Model '{modelPath}' has no Renderers. Item.Awake would fail at runtime for such a prefab.");
                    return null;
                }

                nameToPath.Add(model.name, modelPath);

                ModelJob job = new ModelJob();
                job.Model = model;
                job.PrefabName = $"Item_{model.name}";
                job.PrefabPath = $"{prefabsPath}/{job.PrefabName}.prefab";
                job.IconPath = $"{iconsPath}/{job.PrefabName}.png";
                jobs.Add(job);
            }

            if (jobs.Count == 0)
            {
                Debug.LogError($"[PropFactory] No model prefabs found in '{modelsPath}'.");
                return null;
            }

            jobs.Sort(CompareJobNames);

            return jobs;
        }

        private int RenderIcons(List<ModelJob> jobs)
        {
            int rendered = 0;

            for (int i = 0; i < jobs.Count; i++)
            {
                EditorUtility.DisplayProgressBar("Prop Factory", $"Icon {jobs[i].PrefabName} ({i + 1}/{jobs.Count})", (float)i / jobs.Count);

                if (RenderIcon(jobs[i]) == true)
                {
                    rendered++;
                }
            }

            return rendered;
        }

        private bool RenderIcon(ModelJob job)
        {
            if (File.Exists(job.IconPath) == true && _forceIcons == false)
            {
                return false;
            }

            Texture2D texture = RenderModelToTexture(job.Model, _iconLayer);

            File.WriteAllBytes(job.IconPath, texture.EncodeToPNG());
            DestroyImmediate(texture);

            return true;
        }

        private static void EnsureSpriteImport(string iconPath)
        {
            TextureImporter importer = AssetImporter.GetAtPath(iconPath) as TextureImporter;

            if (importer == null)
            {
                Debug.LogWarning($"[PropFactory] No TextureImporter for '{iconPath}'.");
                return;
            }

            if (importer.textureType == TextureImporterType.Sprite)
            {
                return;
            }

            importer.textureType = TextureImporterType.Sprite;
            importer.SaveAndReimport();
        }

        private void BakeDefinitions(List<ModelJob> jobs, string definitionsPath)
        {
            IReadOnlyList<TierEntry> entries = _tierTable.Entries;

            for (int i = 0; i < jobs.Count; i++)
            {
                EditorUtility.DisplayProgressBar("Prop Factory", $"Definitions {jobs[i].PrefabName} ({i + 1}/{jobs.Count})", (float)i / jobs.Count);
                BakeJobDefinitions(jobs[i], entries, definitionsPath);
            }
        }

        private void BakeJobDefinitions(ModelJob job, IReadOnlyList<TierEntry> entries, string definitionsPath)
        {
            for (int i = 0; i < entries.Count; i++)
            {
                TierEntry entry = entries[i];
                string assetPath = $"{definitionsPath}/D_{job.PrefabName}_{entry.Tier}.asset";
                ItemDefinition definition = AssetDatabase.LoadAssetAtPath<ItemDefinition>(assetPath);

                if (definition == null)
                {
                    definition = CreateInstance<ItemDefinition>();
                    AssetDatabase.CreateAsset(definition, assetPath);
                }

                SerializedObject serialized = new SerializedObject(definition);
                serialized.FindProperty("_tier").enumValueIndex = (int)entry.Tier;
                serialized.FindProperty("_baseMass").intValue = entry.Mass;
                serialized.FindProperty("_icon").objectReferenceValue = LoadIcon(job.IconPath, job.PrefabName);
                serialized.ApplyModifiedPropertiesWithoutUndo();

                job.Definitions.Add(definition);

                if (entry.Tier == ItemTier.Small)
                {
                    job.FallbackDefinition = definition;
                }
            }

            if (job.FallbackDefinition == null)
            {
                job.FallbackDefinition = job.Definitions[0];
            }
        }

        private static Sprite LoadIcon(string iconPath, string prefabName)
        {
            Sprite icon = AssetDatabase.LoadAssetAtPath<Sprite>(iconPath);

            if (icon == null)
            {
                Debug.LogWarning($"[PropFactory] No Sprite at '{iconPath}' for '{prefabName}'. Definition gets no icon.");
            }

            return icon;
        }

        private void CreateItemPrefabs(List<ModelJob> jobs, int collectableLayer)
        {
            for (int i = 0; i < jobs.Count; i++)
            {
                EditorUtility.DisplayProgressBar("Prop Factory", $"Prefab {jobs[i].PrefabName} ({i + 1}/{jobs.Count})", (float)i / jobs.Count);

                if (AssetDatabase.LoadAssetAtPath<GameObject>(jobs[i].PrefabPath) != null)
                {
                    UpdateItemPrefab(jobs[i], collectableLayer);
                }
                else
                {
                    CreateItemPrefab(jobs[i], collectableLayer);
                }

                jobs[i].Prefab = AssetDatabase.LoadAssetAtPath<Item>(jobs[i].PrefabPath);
            }
        }

        private void CreateItemPrefab(ModelJob job, int collectableLayer)
        {
            GameObject root = new GameObject(job.PrefabName)
            {
                layer = collectableLayer
            };

            GameObject modelInstance = (GameObject)PrefabUtility.InstantiatePrefab(job.Model, root.transform);
            modelInstance.transform.localPosition = Vector3.zero;

            BoxCollider collider = root.AddComponent<BoxCollider>();
            ApplyCollider(collider, ComputeBounds(root));

            Item item = root.AddComponent<Item>();
            WriteItemFields(item, collider, job.FallbackDefinition);

            PrefabUtility.SaveAsPrefabAsset(root, job.PrefabPath);
            DestroyImmediate(root);
        }

        private void UpdateItemPrefab(ModelJob job, int collectableLayer)
        {
            GameObject contents = PrefabUtility.LoadPrefabContents(job.PrefabPath);

            try
            {
                contents.layer = collectableLayer;

                BoxCollider collider = contents.GetComponent<BoxCollider>();

                if (collider == null)
                {
                    collider = contents.AddComponent<BoxCollider>();
                }

                ApplyCollider(collider, ComputeBounds(contents));

                Item item = contents.GetComponent<Item>();

                if (item == null)
                {
                    item = contents.AddComponent<Item>();
                }

                WriteItemFields(item, collider, job.FallbackDefinition);

                PrefabUtility.SaveAsPrefabAsset(contents, job.PrefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(contents);
            }
        }

        private void WriteItemFields(Item item, BoxCollider collider, ItemDefinition fallbackDefinition)
        {
            SerializedObject serialized = new SerializedObject(item);
            serialized.FindProperty("_definition").objectReferenceValue = fallbackDefinition;
            serialized.FindProperty("_collider").objectReferenceValue = collider;
            serialized.FindProperty("_ghostMaterial").objectReferenceValue = _ghostMaterial;
            serialized.FindProperty("_ghostFadeConfig").objectReferenceValue = _ghostFadeConfig;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private void WritePropSet(List<ModelJob> jobs)
        {
            SerializedObject serialized = new SerializedObject(_propSet);

            SerializedProperty props = serialized.FindProperty("_props");
            SerializedProperty variants = serialized.FindProperty("_variants");

            if (props == null || props.isArray == false || variants == null || variants.isArray == false)
            {
                Debug.LogError($"[PropFactory] PropSet '{_propSet.name}' has no _props or _variants list.");
                return;
            }

            props.ClearArray();

            for (int i = 0; i < jobs.Count; i++)
            {
                props.InsertArrayElementAtIndex(props.arraySize);
                props.GetArrayElementAtIndex(props.arraySize - 1).objectReferenceValue = jobs[i].Prefab;
            }

            variants.ClearArray();

            for (int i = 0; i < jobs.Count; i++)
            {
                for (int j = 0; j < jobs[i].Definitions.Count; j++)
                {
                    variants.InsertArrayElementAtIndex(variants.arraySize);

                    SerializedProperty element = variants.GetArrayElementAtIndex(variants.arraySize - 1);
                    element.FindPropertyRelative("_prefab").objectReferenceValue = jobs[i].Prefab;
                    element.FindPropertyRelative("_definition").objectReferenceValue = jobs[i].Definitions[j];
                }
            }

            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void ApplyCollider(BoxCollider collider, Bounds bounds)
        {
            collider.size = bounds.size;
            collider.center = bounds.center;
        }

        private static Bounds ComputeBounds(GameObject instance)
        {
            Renderer[] renderers = instance.GetComponentsInChildren<Renderer>();
            Bounds world = new Bounds(instance.transform.position, Vector3.zero);

            for (int i = 0; i < renderers.Length; i++)
            {
                world.Encapsulate(renderers[i].bounds);
            }

            Matrix4x4 worldToLocal = instance.transform.worldToLocalMatrix;

            Vector3 localSize = new Vector3
            (
                Mathf.Abs(worldToLocal.m00) * world.size.x + Mathf.Abs(worldToLocal.m01) * world.size.y + Mathf.Abs(worldToLocal.m02) * world.size.z,
                Mathf.Abs(worldToLocal.m10) * world.size.x + Mathf.Abs(worldToLocal.m11) * world.size.y + Mathf.Abs(worldToLocal.m12) * world.size.z,
                Mathf.Abs(worldToLocal.m20) * world.size.x + Mathf.Abs(worldToLocal.m21) * world.size.y + Mathf.Abs(worldToLocal.m22) * world.size.z
            );

            if (localSize.x <= 0f || localSize.y <= 0f || localSize.z <= 0f)
            {
                throw new InvalidOperationException(
                    $"[PropFactory] '{instance.name}': computed a degenerate collider size {localSize} " +
                    "from world size " + world.size + ". Check the model's scale in its hierarchy.");
            }

            return new Bounds(instance.transform.InverseTransformPoint(world.center), localSize);
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

            UnityEngine.Camera cam = cameraGO.AddComponent<UnityEngine.Camera>();
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

        private static void FitCameraToBounds(UnityEngine.Camera cam, GameObject instance)
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
            if (AssetDatabase.IsValidFolder(folder))
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
            if (EditorPrefs.HasKey(PrefsKey) == false)
            {
                return;
            }

            WindowState state = JsonUtility.FromJson<WindowState>(EditorPrefs.GetString(PrefsKey));

            if (state == null)
            {
                return;
            }

            _modelsFolder = LoadStateAsset<DefaultAsset>(state.ModelsPath);
            _prefabsFolder = LoadStateAsset<DefaultAsset>(state.PrefabsPath);
            _definitionsFolder = LoadStateAsset<DefaultAsset>(state.DefinitionsPath);
            _iconsFolder = LoadStateAsset<DefaultAsset>(state.IconsPath);
            _propSet = LoadStateAsset<PropSet>(state.PropSetPath);
            _tierTable = LoadStateAsset<TierTable>(state.TierTablePath);
            _ghostMaterial = LoadStateAsset<Material>(state.GhostMaterialPath);
            _ghostFadeConfig = LoadStateAsset<GhostFadeConfig>(state.GhostFadeConfigPath);
            _forceIcons = state.ForceIcons;
        }

        private void SaveState()
        {
            WindowState state = new WindowState();
            state.ModelsPath = GetAssetPathOrNull(_modelsFolder);
            state.PrefabsPath = GetAssetPathOrNull(_prefabsFolder);
            state.DefinitionsPath = GetAssetPathOrNull(_definitionsFolder);
            state.IconsPath = GetAssetPathOrNull(_iconsFolder);
            state.PropSetPath = GetAssetPathOrNull(_propSet);
            state.TierTablePath = GetAssetPathOrNull(_tierTable);
            state.GhostMaterialPath = GetAssetPathOrNull(_ghostMaterial);
            state.GhostFadeConfigPath = GetAssetPathOrNull(_ghostFadeConfig);
            state.ForceIcons = _forceIcons;

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

        private static int CompareJobNames(ModelJob left, ModelJob right)
        {
            return string.CompareOrdinal(left.PrefabName, right.PrefabName);
        }

        private sealed class ModelJob
        {
            public GameObject Model;
            public string PrefabName;
            public string PrefabPath;
            public string IconPath;
            public Item Prefab;
            public ItemDefinition FallbackDefinition;
            public readonly List<ItemDefinition> Definitions = new List<ItemDefinition>();
        }

        [Serializable]
        private sealed class WindowState
        {
            public string ModelsPath;
            public string PrefabsPath;
            public string DefinitionsPath;
            public string IconsPath;
            public string PropSetPath;
            public string TierTablePath;
            public string GhostMaterialPath;
            public string GhostFadeConfigPath;
            public bool ForceIcons;
        }
    }
}
