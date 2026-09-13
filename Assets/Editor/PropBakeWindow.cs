using System;
using System.Collections.Generic;
using System.IO;
using Items;
using Scriptables;
using UnityEditor;
using UnityEngine;

namespace EditorTools
{
    public sealed class PropBakeWindow : EditorWindow
    {
        private PropSet _propSet;
        private TierTable _tierTable;
        private int _missingIcons;

        [MenuItem("Mad Slime/Prop Bake")]
        private static void Open()
        {
            GetWindow<PropBakeWindow>("Prop Bake");
        }

        private void OnGUI()
        {
            EditorGUILayout.HelpBox(
                "Bakes one item definition per (prop × tier): mass comes from the Tier Table, icon — from the photo " +
                "in Assets/Resources/Icons/<prop name>.png made by Tools → Generate Icons From Scene. " +
                "Re-baking updates existing definitions by path, nothing breaks.",
                MessageType.Info);

            _propSet = (PropSet)EditorGUILayout.ObjectField("Prop Set", _propSet, typeof(PropSet), false);
            _tierTable = (TierTable)EditorGUILayout.ObjectField("Tier Table", _tierTable, typeof(TierTable), false);

            EditorGUILayout.Space();

            if (GUILayout.Button("Bake"))
            {
                Bake();
            }
        }

        private void Bake()
        {
            if (_propSet == null)
            {
                Debug.LogWarning("[PropBake] Prop Set is not assigned.");
                return;
            }

            if (_tierTable == null)
            {
                Debug.LogWarning("[PropBake] Tier Table is not assigned.");
                return;
            }

            string folder = $"Assets/Scriptables/Items/Baked/{_propSet.name}";
            EnsureFolder(folder);

            List<PropVariant> variants = new List<PropVariant>();
            _missingIcons = 0;

            for (int p = 0; p < _propSet.Props.Count; p++)
            {
                Item prefab = _propSet.Props[p];

                if (prefab == null)
                {
                    Debug.LogWarning($"[PropBake] Prop at index {p} is null, skipped.");
                    continue;
                }

                for (int t = 0; t < _tierTable.Entries.Count; t++)
                {
                    TierEntry entry = _tierTable.Entries[t];
                    ItemDefinition definition = GetOrCreateDefinition(prefab, entry, folder);

                    if (definition == null)
                    {
                        continue;
                    }

                    variants.Add(new PropVariant(prefab, definition));
                }
            }

            WriteVariants(variants);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log($"[PropBake] Baked {variants.Count} variants into '{folder}'. Missing icons: {_missingIcons}.");
        }

        private ItemDefinition GetOrCreateDefinition(Item prefab, TierEntry entry, string folder)
        {
            string assetPath = $"{folder}/D_{prefab.name}_{entry.Tier}.asset";
            ItemDefinition definition = AssetDatabase.LoadAssetAtPath<ItemDefinition>(assetPath);

            if (definition == null)
            {
                definition = CreateInstance<ItemDefinition>();
                AssetDatabase.CreateAsset(definition, assetPath);
            }

            SerializedObject serialized = new SerializedObject(definition);
            serialized.FindProperty("_tier").enumValueIndex = (int)entry.Tier;
            serialized.FindProperty("_baseMass").intValue = entry.Mass;
            serialized.FindProperty("_icon").objectReferenceValue = FindIcon(prefab.name);
            serialized.ApplyModifiedPropertiesWithoutUndo();

            return definition;
        }

        private Sprite FindIcon(string prefabName)
        {
            Sprite icon = AssetDatabase.LoadAssetAtPath<Sprite>($"Assets/Resources/Icons/{prefabName}.png");

            if (icon == null)
            {
                _missingIcons++;
                Debug.LogWarning($"[PropBake] No icon for '{prefabName}' at Assets/Resources/Icons/{prefabName}.png. " +
                    "Generate icons with Tools → Generate Icons From Scene and re-bake.");
            }

            return icon;
        }

        private void WriteVariants(List<PropVariant> variants)
        {
            SerializedObject serialized = new SerializedObject(_propSet);
            SerializedProperty list = serialized.FindProperty("_variants");

            if (list == null || list.isArray == false)
            {
                Debug.LogWarning("[PropBake] PropSet has no _variants list.");
                return;
            }

            list.ClearArray();

            for (int i = 0; i < variants.Count; i++)
            {
                list.InsertArrayElementAtIndex(list.arraySize);

                SerializedProperty element = list.GetArrayElementAtIndex(list.arraySize - 1);
                element.FindPropertyRelative("_prefab").objectReferenceValue = variants[i].Prefab;
                element.FindPropertyRelative("_definition").objectReferenceValue = variants[i].Definition;
            }

            serialized.ApplyModifiedPropertiesWithoutUndo();
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
    }
}
