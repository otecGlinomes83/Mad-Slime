using System;
using System.IO;
using Items;
using UnityEditor;
using UnityEngine;

namespace EditorTools
{
    public sealed class ItemPropFactory : EditorWindow
    {
        private DefaultAsset _modelsFolder;
        private DefaultAsset _definitionsFolder;
        private DefaultAsset _prefabsFolder;
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
                "Tiers and masses are assigned per level at bake/generation time, so no per-prop tier here.",
                MessageType.Info);

            _modelsFolder = (DefaultAsset)EditorGUILayout.ObjectField("Models Folder", _modelsFolder, typeof(DefaultAsset), false);
            _definitionsFolder = (DefaultAsset)EditorGUILayout.ObjectField("Definitions Folder", _definitionsFolder, typeof(DefaultAsset), false);
            _prefabsFolder = (DefaultAsset)EditorGUILayout.ObjectField("Prefabs Folder", _prefabsFolder, typeof(DefaultAsset), false);
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

            foreach (string modelGuid in modelGuids)
            {
                string modelPath = AssetDatabase.GUIDToAssetPath(modelGuid);
                GameObject model = AssetDatabase.LoadAssetAtPath<GameObject>(modelPath);

                if (model == null)
                {
                    continue;
                }

                ItemDefinition definition = CreateDefinition(model.name, definitionsPath);
                CreateItemPrefab(model, definition, prefabsPath);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"[PropFactory] Generated {modelGuids.Length} props from '{modelsPath}'.");
        }

        private ItemDefinition CreateDefinition(string modelName, string folderPath)
        {
            ItemDefinition definition = CreateInstance<ItemDefinition>();
            SerializedObject serialized = new SerializedObject(definition);
            serialized.FindProperty("_baseMass").intValue = 1;
            serialized.FindProperty("_tier").enumValueIndex = (int)Skills.ItemTier.Small;
            serialized.ApplyModifiedPropertiesWithoutUndo();

            string assetPath = AssetDatabase.GenerateUniqueAssetPath($"{folderPath}/D_{modelName}.asset");
            AssetDatabase.CreateAsset(definition, assetPath);

            return AssetDatabase.LoadAssetAtPath<ItemDefinition>(assetPath);
        }

        private void CreateItemPrefab(GameObject model, ItemDefinition definition, string folderPath)
        {
            int collectableLayer = LayerMask.NameToLayer("Collectable");

            if (collectableLayer < 0)
            {
                throw new InvalidOperationException(
                    "[PropFactory] Layer 'Collectable' does not exist. Add it in Project Settings → Tags and Layers.");
            }

            GameObject root = new GameObject($"Item_{model.name}")
            {
                layer = collectableLayer
            };

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
