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
        private DefaultAsset _modelsFolder;
        private DefaultAsset _definitionsFolder;
        private DefaultAsset _prefabsFolder;
        private LevelTheme _theme;
        private ItemTier _tier = ItemTier.Small;
        private int _baseMass = 1;
        private int _massStep = 1;
        private float _colliderPadding = 1.15f;

        [MenuItem("Mad Slime/Prop Factory")]
        private static void Open()
        {
            GetWindow<ItemPropFactory>("Prop Factory");
        }

        private void OnGUI()
        {
            EditorGUILayout.HelpBox(
                "Point the fields at folders and press Generate. For every model prefab in the Models Folder " +
                "an ItemDefinition asset and an Item prefab (model + BoxCollider + Item) are created. " +
                "If a LevelTheme is assigned, the new item is appended to its item pool.",
                MessageType.Info);

            _modelsFolder = (DefaultAsset)EditorGUILayout.ObjectField("Models Folder", _modelsFolder, typeof(DefaultAsset), false);
            _definitionsFolder = (DefaultAsset)EditorGUILayout.ObjectField("Definitions Folder", _definitionsFolder, typeof(DefaultAsset), false);
            _prefabsFolder = (DefaultAsset)EditorGUILayout.ObjectField("Prefabs Folder", _prefabsFolder, typeof(DefaultAsset), false);
            _theme = (LevelTheme)EditorGUILayout.ObjectField("Level Theme (optional)", _theme, typeof(LevelTheme), false);

            EditorGUILayout.Space();
            _tier = (ItemTier)EditorGUILayout.EnumPopup("Tier", _tier);
            _baseMass = EditorGUILayout.IntField("Base Mass", _baseMass);
            _massStep = EditorGUILayout.IntField("Mass Step Per Item", _massStep);
            _colliderPadding = EditorGUILayout.Slider("Collider Padding", _colliderPadding, 1f, 2f);

            EditorGUILayout.Space();

            if (GUILayout.Button("Generate"))
            {
                Generate();
            }
        }

        private void Generate()
        {
            string modelsPath = GetFolderPath(_modelsFolder, null);

            if (modelsPath == null)
            {
                Debug.LogWarning("[PropFactory] Models Folder is not assigned.");
                return;
            }

            string definitionsPath = GetFolderPath(_definitionsFolder, "Assets/Scriptables/Items/Generated");
            string prefabsPath = GetFolderPath(_prefabsFolder, "Assets/Resources/Prefabs/Items/Generated");

            string[] modelGuids = AssetDatabase.FindAssets("t:Prefab", new[] { modelsPath });

            if (modelGuids.Length == 0)
            {
                Debug.LogWarning($"[PropFactory] No model prefabs found in '{modelsPath}'.");
                return;
            }

            int mass = _baseMass;

            foreach (string modelGuid in modelGuids)
            {
                string modelPath = AssetDatabase.GUIDToAssetPath(modelGuid);
                GameObject model = AssetDatabase.LoadAssetAtPath<GameObject>(modelPath);

                if (model == null)
                {
                    continue;
                }

                ItemDefinition definition = CreateDefinition(model.name, mass, definitionsPath);
                Item item = CreateItemPrefab(model, definition, prefabsPath);
                AppendToTheme(item);

                mass += _massStep;
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"[PropFactory] Generated {modelGuids.Length} props from '{modelsPath}'.");
        }

        private ItemDefinition CreateDefinition(string modelName, int mass, string folderPath)
        {
            ItemDefinition definition = CreateInstance<ItemDefinition>();
            SerializedObject serialized = new SerializedObject(definition);
            serialized.FindProperty("_baseMass").intValue = mass;
            serialized.FindProperty("_tier").enumValueIndex = (int)_tier;
            serialized.ApplyModifiedPropertiesWithoutUndo();

            string assetPath = AssetDatabase.GenerateUniqueAssetPath($"{folderPath}/D_{modelName}.asset");
            AssetDatabase.CreateAsset(definition, assetPath);

            return AssetDatabase.LoadAssetAtPath<ItemDefinition>(assetPath);
        }

        private Item CreateItemPrefab(GameObject model, ItemDefinition definition, string folderPath)
        {
            GameObject root = new GameObject($"Item_{model.name}");

            GameObject modelInstance = (GameObject)PrefabUtility.InstantiatePrefab(model, root.transform);
            modelInstance.transform.localPosition = Vector3.zero;

            BoxCollider collider = root.AddComponent<BoxCollider>();
            Bounds bounds = ComputeBounds(modelInstance);

            if (bounds.size == Vector3.zero)
            {
                Debug.LogWarning($"[PropFactory] '{model.name}' has no renderers, collider left at default size.");
                collider.size = Vector3.one;
            }
            else
            {
                collider.size = bounds.size * _colliderPadding;
                collider.center = bounds.center;
            }

            Item item = root.AddComponent<Item>();
            SerializedObject serialized = new SerializedObject(item);
            serialized.FindProperty("_definition").objectReferenceValue = definition;
            serialized.FindProperty("_collider").objectReferenceValue = collider;
            serialized.ApplyModifiedPropertiesWithoutUndo();

            string prefabPath = AssetDatabase.GenerateUniqueAssetPath($"{folderPath}/Item_{model.name}.prefab");
            PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
            DestroyImmediate(root);

            return AssetDatabase.LoadAssetAtPath<Item>(prefabPath);
        }

        private void AppendToTheme(Item item)
        {
            if (_theme == null || item == null)
            {
                return;
            }

            SerializedObject serialized = new SerializedObject(_theme);
            SerializedProperty pool = serialized.FindProperty("_itemPool");

            if (pool == null || pool.isArray == false)
            {
                Debug.LogWarning("[PropFactory] LevelTheme has no _itemPool list.");
                return;
            }

            pool.InsertArrayElementAtIndex(pool.arraySize);
            pool.GetArrayElementAtIndex(pool.arraySize - 1).objectReferenceValue = item;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static Bounds ComputeBounds(GameObject instance)
        {
            Renderer[] renderers = instance.GetComponentsInChildren<Renderer>();
            Bounds world = new Bounds(instance.transform.position, Vector3.zero);

            for (int i = 0; i < renderers.Length; i++)
            {
                world.Encapsulate(renderers[i].bounds);
            }

            return new Bounds
            (
                instance.transform.InverseTransformPoint(world.center),
                instance.transform.InverseTransformVector(world.size)
            );
        }

        private static string GetFolderPath(DefaultAsset folder, string fallback)
        {
            if (folder == null)
            {
                if (fallback == null)
                {
                    return null;
                }

                Directory.CreateDirectory(fallback);
                return fallback;
            }

            string path = AssetDatabase.GetAssetPath(folder);

            if (File.Exists(path))
            {
                path = Path.GetDirectoryName(path);
            }

            return path;
        }
    }
}
