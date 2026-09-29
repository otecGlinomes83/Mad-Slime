using Audio;
using DI;
using Game;
using Player;
using Scriptables;
using Roulette;
using Skins;
using System;
using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;
using Upgrades;
using UI;

public static class MadSlimeContentSetup
{
    private const string UpgradesConfigPath = "Assets/MadSlime/Scriptables/Upgrades/UpgradesConfig.asset";
    private const string RouletteConfigPath = "Assets/MadSlime/Scriptables/Roulette/RouletteConfig.asset";
    private const string HighlightMaterialPath = "Assets/MadSlime/Resources/Materials/QuotaHighlight.mat";
    private const string BurstPrefabPath = "Assets/MadSlime/Resources/Prefabs/FX/CollectBurst.prefab";
    private const string UpgradeCardPrefabPath = "Assets/MadSlime/Resources/Prefabs/UI/Skins/UpgradeItem.prefab";
    private const string UpgradePlatePrefabPath = "Assets/MadSlime/Resources/Prefabs/UI/UpgradePlate.prefab";
    private const string PerkSeparatorUserPrefabPath = "Assets/MadSlime/Resources/Prefabs/UI/PerkSeporator.prefab";
    private const string SectorCardPrefabPath = "Assets/MadSlime/Resources/Prefabs/UI/Skins/RouletteSectorCard.prefab";
    private const string RouletteViewPrefabPath = "Assets/MadSlime/Resources/Prefabs/UI/Skins/RouletteView.prefab";
    private const string RouletteWindowPrefabPath = "Assets/MadSlime/Resources/Prefabs/UI/Skins/RouletteWindow.prefab";
    private const string UIClickClipPath = "Assets/MadSlime/Scriptables/Audio/UI Click.asset";
    private const string RarityTablePath = "Assets/MadSlime/Scriptables/Skins/SkinRarityTable.asset";
    private const string PerkSeparatorPrefabPath = "Assets/MadSlime/Resources/Prefabs/UI/Skins/PerkSeparator.prefab";
    private const string UpgradeIconsFolder = "Assets/MadSlime/Resources/Images/Upgrades";
    private const string MusicShopClipPath = "Assets/MadSlime/Scriptables/Audio/Music Shop.asset";
    private const string CrownSkinPath = "Assets/MadSlime/Scriptables/Skins/Crown.asset";
    private const string PhantomSkinPath = "Assets/MadSlime/Scriptables/Skins/Phantom.asset";
    private const string FailMenuPrefabPath = "Assets/MadSlime/Resources/Prefabs/UI/ResultMenu/FailMenu.prefab";
    private const string GameScenePath = "Assets/MadSlime/Scenes/Game.unity";
    private const string FontPath = "Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset";


    public static void SetupAll()
    {
        EnsureFolders();

        UpgradesConfig upgradesConfig = CreateUpgradesConfig();
        AssignUpgradeIcons(upgradesConfig);
        CreateExclusiveSkins();
        ApplySkinRarities();
        SkinRarityTable rarityTable = CreateSkinRarityTable();
        CreateRouletteConfig(rarityTable);
        Material highlightMaterial = CreateHighlightMaterial(upgradesConfig);
        GameObject burstPrefab = CreateBurstPrefab();
        GameObject sectorCardPrefab = CreateSectorCardPrefab();
        CreateRouletteViewPrefab(sectorCardPrefab);
        DeleteLegacyRouletteWindowPrefab();
        EnsureRarityLocalization();
        EnsureUpgradeLocalization();
        EnsureWinPopupLocalization();
        FixFontAtlasReadability();

        SetupProjectScope(upgradesConfig);
        SetupItemPrefabs(highlightMaterial);
        SetupGameScene(burstPrefab);
        SetupFillScene();
        SetupSkinCardPrefab();
        SetupShopScene();
        SetupMenuScene();
        SetupFailMenuPrefab();

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log("[MadSlimeContentSetup] done");
    }

    private static void EnsureFolders()
    {
        EnsureFolder("Assets/MadSlime/Scriptables/Upgrades");
        EnsureFolder("Assets/MadSlime/Scriptables/Roulette");
        EnsureFolder("Assets/MadSlime/Resources/Materials");
        EnsureFolder("Assets/MadSlime/Resources/Prefabs/FX");
    }

    private static void FixFontAtlasReadability()
    {
        TMP_FontAsset fontAsset = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);

        if (fontAsset == null)
        {
            throw new InvalidOperationException("LiberationSans SDF font asset not found.");
        }

        Texture2D atlasTexture = fontAsset.atlasTexture;

        if (fontAsset.atlasPopulationMode != AtlasPopulationMode.Dynamic ||
            atlasTexture == null ||
            atlasTexture.isReadable == true)
        {
            return;
        }

        // Dynamic mode rasterizes glyphs into the atlas at runtime, which requires a
        // readable texture. The embedded atlas was baked static, so TMP's own reset
        // re-creates it readable; glyph tables are cleared and re-filled on demand.
        fontAsset.ClearFontAssetData();

        Debug.Log("[MadSlimeContentSetup] font atlas made readable for dynamic glyphs");
    }

    private static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path) == true)
        {
            return;
        }

        string parent = Path.GetDirectoryName(path).Replace('\\', '/');
        string leaf = Path.GetFileName(path);

        AssetDatabase.CreateFolder(parent, leaf);
    }

    private static UpgradesConfig CreateUpgradesConfig()
    {
        UpgradesConfig config = AssetDatabase.LoadAssetAtPath<UpgradesConfig>(UpgradesConfigPath);

        if (config == null)
        {
            config = ScriptableObject.CreateInstance<UpgradesConfig>();
            AssetDatabase.CreateAsset(config, UpgradesConfigPath);
        }

        SerializedObject serialized = new SerializedObject(config);
        SerializedProperty upgrades = serialized.FindProperty("_upgrades");

        InsertUpgradeIfMissing(upgrades, UpgradeType.Speed, 200, 150, 0.1f, 5);
        InsertUpgradeIfMissing(upgrades, UpgradeType.Appetite, 250, 200, 0.15f, 5);
        InsertUpgradeIfMissing(upgrades, UpgradeType.Taste, 400, 300, 0.2f, 5);
        InsertUpgradeIfMissing(upgrades, UpgradeType.Metabolism, 350, 250, 0.15f, 5);

        SerializedProperty perks = serialized.FindProperty("_perks");

        InsertPerkIfMissing(perks, PerkType.Smell, 3000);
        InsertPerkIfMissing(perks, PerkType.Adrenaline, 4000);
        InsertPerkIfMissing(perks, PerkType.Ambitions, 5000);

        serialized.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(config);

        return config;
    }

    private static void InsertUpgradeIfMissing(SerializedProperty array, UpgradeType type, int baseCost,
        int costStep, float valuePerStep, int maxSteps)
    {
        for (int i = 0; i < array.arraySize; i++)
        {
            SerializedProperty element = array.GetArrayElementAtIndex(i);

            if (element.FindPropertyRelative("_type").enumValueIndex == (int)type)
            {
                return;
            }
        }

        InsertUpgrade(array, type, baseCost, costStep, valuePerStep, maxSteps);
    }

    private static void InsertPerkIfMissing(SerializedProperty array, PerkType type, int cost)
    {
        for (int i = 0; i < array.arraySize; i++)
        {
            SerializedProperty element = array.GetArrayElementAtIndex(i);

            if (element.FindPropertyRelative("_type").enumValueIndex == (int)type)
            {
                return;
            }
        }

        InsertPerk(array, type, cost);
    }

    private static void InsertUpgrade(SerializedProperty array, UpgradeType type, int baseCost, int costStep,
        float valuePerStep, int maxSteps)
    {
        int index = array.arraySize;
        array.InsertArrayElementAtIndex(index);
        SerializedProperty element = array.GetArrayElementAtIndex(index);

        element.FindPropertyRelative("_type").enumValueIndex = (int)type;
        element.FindPropertyRelative("_baseCost").intValue = baseCost;
        element.FindPropertyRelative("_costStep").intValue = costStep;
        element.FindPropertyRelative("_valuePerStep").floatValue = valuePerStep;
        element.FindPropertyRelative("_maxSteps").intValue = maxSteps;
    }

    private static void AssignUpgradeIcons(UpgradesConfig config)
    {
        SerializedObject serialized = new SerializedObject(config);

        SerializedProperty upgrades = serialized.FindProperty("_upgrades");
        AssignIcon(upgrades, (int)UpgradeType.Speed, "SpeedPerk");
        AssignIcon(upgrades, (int)UpgradeType.Appetite, "AppetitePerk");
        AssignIcon(upgrades, (int)UpgradeType.Taste, "TastePerk");
        AssignIcon(upgrades, (int)UpgradeType.Metabolism, "Metabolism");

        SerializedProperty perks = serialized.FindProperty("_perks");
        AssignIcon(perks, (int)PerkType.Smell, "SmellPerk");
        AssignIcon(perks, (int)PerkType.Adrenaline, "AdrenalinePerk");
        AssignIcon(perks, (int)PerkType.Ambitions, "AmbitionsPerk");

        serialized.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(config);
    }

    private static void AssignIcon(SerializedProperty array, int typeIndex, string iconName)
    {
        string path = $"{UpgradeIconsFolder}/{iconName}.png";

        TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;

        if (importer != null && importer.textureType != TextureImporterType.Sprite)
        {
            importer.textureType = TextureImporterType.Sprite;
            importer.mipmapEnabled = false;
            importer.SaveAndReimport();
        }

        Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);

        if (sprite == null)
        {
            Debug.LogWarning($"[MadSlimeContentSetup] upgrade icon not found: {path}");
            return;
        }

        for (int i = 0; i < array.arraySize; i++)
        {
            SerializedProperty element = array.GetArrayElementAtIndex(i);

            if (element.FindPropertyRelative("_type").enumValueIndex != typeIndex)
            {
                continue;
            }

            SerializedProperty icon = element.FindPropertyRelative("_icon");

            if (icon.objectReferenceValue == null)
            {
                icon.objectReferenceValue = sprite;
            }

            return;
        }
    }

    private static void InsertPerk(SerializedProperty array, PerkType type, int cost)
    {
        int index = array.arraySize;
        array.InsertArrayElementAtIndex(index);
        SerializedProperty element = array.GetArrayElementAtIndex(index);

        element.FindPropertyRelative("_type").enumValueIndex = (int)type;
        element.FindPropertyRelative("_cost").intValue = cost;
    }

    private static RouletteConfig CreateRouletteConfig(SkinRarityTable rarityTable)
    {
        RouletteConfig config = AssetDatabase.LoadAssetAtPath<RouletteConfig>(RouletteConfigPath);

        if (config == null)
        {
            config = ScriptableObject.CreateInstance<RouletteConfig>();
            AssetDatabase.CreateAsset(config, RouletteConfigPath);
        }

        SerializedObject serialized = new SerializedObject(config);
        SerializedProperty sectors = serialized.FindProperty("_sectors");

        sectors.ClearArray();
        InsertCoinSector(sectors, 50, 20f);
        InsertCoinSector(sectors, 150, 12f);
        InsertCoinSector(sectors, 300, 8f);
        InsertCoinSector(sectors, 600, 4f);
        InsertCoinSector(sectors, 1500, 1.5f);
        InsertSkinSector(sectors, PlayerSkins.Crown, 0.35f);
        InsertSkinSector(sectors, PlayerSkins.Phantom, 0.15f);

        serialized.FindProperty("_adSpinWindowSeconds").intValue = 1800;
        serialized.FindProperty("_adSpinsPerWindow").intValue = 3;
        serialized.FindProperty("_freeSpinCooldownSeconds").intValue = 900;
        serialized.FindProperty("_rarityTable").objectReferenceValue = rarityTable;
        serialized.FindProperty("_idleStepInterval").floatValue = 1.1f;
        serialized.FindProperty("_idleStepDuration").floatValue = 0.18f;
        serialized.FindProperty("_windBackCards").floatValue = 0.6f;
        serialized.FindProperty("_windBackDuration").floatValue = 0.3f;
        serialized.FindProperty("_minTurns").intValue = 3;
        serialized.FindProperty("_maxTurns").intValue = 5;
        serialized.FindProperty("_spinDuration").floatValue = 3.2f;
        serialized.FindProperty("_stepClip").objectReferenceValue = LoadUiClickClip();

        serialized.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(config);

        return config;
    }

    private static SkinRarityTable CreateSkinRarityTable()
    {
        EnsureFolder("Assets/MadSlime/Scriptables/Skins");

        SkinRarityTable table = AssetDatabase.LoadAssetAtPath<SkinRarityTable>(RarityTablePath);

        if (table == null)
        {
            table = ScriptableObject.CreateInstance<SkinRarityTable>();
            AssetDatabase.CreateAsset(table, RarityTablePath);
        }

        SerializedObject serialized = new SerializedObject(table);
        SerializedProperty settings = serialized.FindProperty("_settings");

        settings.ClearArray();
        InsertRaritySettings(settings, SkinRarity.Common, 100f, new Color(0.55f, 0.58f, 0.62f));
        InsertRaritySettings(settings, SkinRarity.Rare, 45f, new Color(0.25f, 0.55f, 0.95f));
        InsertRaritySettings(settings, SkinRarity.Epic, 15f, new Color(0.65f, 0.35f, 0.95f));
        InsertRaritySettings(settings, SkinRarity.Legendary, 4f, new Color(1f, 0.72f, 0.2f));

        serialized.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(table);

        return table;
    }

    private static void InsertRaritySettings(SerializedProperty array, SkinRarity rarity, float dropWeight,
        Color plateColor)
    {
        int index = array.arraySize;
        array.InsertArrayElementAtIndex(index);
        SerializedProperty element = array.GetArrayElementAtIndex(index);

        element.FindPropertyRelative("_rarity").enumValueIndex = (int)rarity;
        element.FindPropertyRelative("_dropWeight").floatValue = dropWeight;
        element.FindPropertyRelative("_plateColor").colorValue = plateColor;
    }

    private static void ApplySkinRarities()
    {
        ApplySkinRarity(PlayerSkins.Slime, SkinRarity.Common);
        ApplySkinRarity(PlayerSkins.Pacman, SkinRarity.Common);
        ApplySkinRarity(PlayerSkins.TripleT, SkinRarity.Rare);
        ApplySkinRarity(PlayerSkins.Crown, SkinRarity.Epic);
        ApplySkinRarity(PlayerSkins.Phantom, SkinRarity.Legendary);
    }

    private static void ApplySkinRarity(PlayerSkins skinType, SkinRarity rarity)
    {
        SkinItem skin = FindSkinByType(skinType);

        if (skin == null)
        {
            Debug.LogWarning($"[MadSlimeContentSetup] no SkinItem for {skinType}, rarity skipped");
            return;
        }

        SerializedObject serialized = new SerializedObject(skin);
        serialized.FindProperty("<Rarity>k__BackingField").enumValueIndex = (int)rarity;
        serialized.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(skin);
    }

    private static void EnsureRarityLocalization()
    {
        const string path = "Assets/MadSlime/Scriptables/Localization/Localization.asset";

        LocalizationTable table = AssetDatabase.LoadAssetAtPath<LocalizationTable>(path);

        if (table == null)
        {
            throw new InvalidOperationException("Localization asset not found.");
        }

        SerializedObject serialized = new SerializedObject(table);
        SerializedProperty entries = serialized.FindProperty("_entries");

        InsertLocaleEntryIfMissing(entries, "rarity_common", "ОБЫЧНЫЙ", "COMMON", "NORMAL");
        InsertLocaleEntryIfMissing(entries, "rarity_rare", "РЕДКИЙ", "RARE", "NADİR");
        InsertLocaleEntryIfMissing(entries, "rarity_epic", "ЭПИЧЕСКИЙ", "EPIC", "EPİK");
        InsertLocaleEntryIfMissing(entries, "rarity_legendary", "ЛЕГЕНДАРНЫЙ", "LEGENDARY", "EFSANEVİ");

        serialized.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(table);
    }

    private static void EnsureUpgradeLocalization()
    {
        const string path = "Assets/MadSlime/Scriptables/Localization/Localization.asset";

        LocalizationTable table = AssetDatabase.LoadAssetAtPath<LocalizationTable>(path);

        if (table == null)
        {
            throw new InvalidOperationException("Localization asset not found.");
        }

        SerializedObject serialized = new SerializedObject(table);
        SerializedProperty entries = serialized.FindProperty("_entries");

        InsertLocaleEntryIfMissing(entries, "upgrade_step", "ШАГ {0}/{1}", "STEP {0}/{1}", "ADIM {0}/{1}");
        InsertLocaleEntryIfMissing(entries, "upgrade_speed_desc", "Скорость +{0}%", "Speed +{0}%", "Hız +{0}%");
        InsertLocaleEntryIfMissing(entries, "upgrade_appetite_desc", "Объём +{0}%", "Size +{0}%", "Büyüklük +{0}%");
        InsertLocaleEntryIfMissing(entries, "upgrade_taste_desc", "Сытность +{0}%", "Fullness +{0}%", "Doyuruculuk +{0}%");
        InsertLocaleEntryIfMissing(entries, "upgrade_metabolism_desc", "Усвоение +{0}%", "Digestion +{0}%", "Sindirim +{0}%");
        InsertLocaleEntryIfMissing(entries, "perk_smell_desc", "Подсвечивает нужные предметы", "Highlights the required items", "Gerekli nesneleri vurgular");
        InsertLocaleEntryIfMissing(entries, "perk_adrenaline_desc", "Ускорение в конце раунда", "Speed boost at the end of the round", "Rauntun sonunda hızlanma");
        InsertLocaleEntryIfMissing(entries, "perk_ambitions_desc", "Старт на тир выше", "Start one tier higher", "Bir kademeden yüksek başla");
        InsertLocaleEntryIfMissing(entries, "shop_one_time", "ОДНОРАЗОВЫЕ ПОКУПКИ", "ONE-TIME BUYS", "TEK SEFERLİK SATIN ALIMLAR");

        serialized.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(table);
    }

    private static void EnsureWinPopupLocalization()
    {
        const string path = "Assets/MadSlime/Scriptables/Localization/Localization.asset";

        LocalizationTable table = AssetDatabase.LoadAssetAtPath<LocalizationTable>(path);

        if (table == null)
        {
            throw new InvalidOperationException("Localization asset not found.");
        }

        SerializedObject serialized = new SerializedObject(table);
        SerializedProperty entries = serialized.FindProperty("_entries");

        InsertLocaleEntryIfMissing(entries, "roulette_take", "ЗАБРАТЬ", "TAKE", "AL");

        serialized.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(table);
    }

    private static void InsertLocaleEntryIfMissing(SerializedProperty entries, string key, string ru, string en,
        string tr)
    {
        for (int i = 0; i < entries.arraySize; i++)
        {
            SerializedProperty element = entries.GetArrayElementAtIndex(i);

            if (element.FindPropertyRelative("_key").stringValue == key)
            {
                return;
            }
        }

        int index = entries.arraySize;
        entries.InsertArrayElementAtIndex(index);
        SerializedProperty entry = entries.GetArrayElementAtIndex(index);

        entry.FindPropertyRelative("_key").stringValue = key;
        entry.FindPropertyRelative("_ru").stringValue = ru;
        entry.FindPropertyRelative("_en").stringValue = en;
        entry.FindPropertyRelative("_tr").stringValue = tr;
    }

    private static void InsertCoinSector(SerializedProperty array, int coins, float weight)
    {
        int index = array.arraySize;
        array.InsertArrayElementAtIndex(index);
        SerializedProperty element = array.GetArrayElementAtIndex(index);

        element.FindPropertyRelative("_rewardKind").enumValueIndex = 0;
        element.FindPropertyRelative("_coins").intValue = coins;
        element.FindPropertyRelative("_skin").objectReferenceValue = null;
        element.FindPropertyRelative("_weight").floatValue = weight;
    }

    private static void InsertSkinSector(SerializedProperty array, PlayerSkins skinType, float weight)
    {
        SkinItem skin = FindSkinByType(skinType);

        if (skin == null)
        {
            Debug.LogWarning($"[MadSlimeContentSetup] no SkinItem for {skinType}, sector skipped");
            return;
        }

        int index = array.arraySize;
        array.InsertArrayElementAtIndex(index);
        SerializedProperty element = array.GetArrayElementAtIndex(index);

        element.FindPropertyRelative("_rewardKind").enumValueIndex = 1;
        element.FindPropertyRelative("_coins").intValue = 0;
        element.FindPropertyRelative("_skin").objectReferenceValue = skin;
        element.FindPropertyRelative("_weight").floatValue = weight;
    }

    private static void CreateExclusiveSkins()
    {
        SkinItem template = FindSkinByType(PlayerSkins.Slime);

        CreateExclusiveSkin(CrownSkinPath, PlayerSkins.Crown, template);
        CreateExclusiveSkin(PhantomSkinPath, PlayerSkins.Phantom, template);

        const string shopContentPath = "Assets/MadSlime/Scriptables/Shop/ShopContent.asset";
        ShopContent shopContent = AssetDatabase.LoadAssetAtPath<ShopContent>(shopContentPath);

        if (shopContent == null)
        {
            throw new InvalidOperationException("ShopContent asset not found.");
        }

        SerializedObject serialized = new SerializedObject(shopContent);
        SerializedProperty items = serialized.FindProperty("_skinItems");

        AppendSkinIfMissing(items, AssetDatabase.LoadAssetAtPath<SkinItem>(CrownSkinPath));
        AppendSkinIfMissing(items, AssetDatabase.LoadAssetAtPath<SkinItem>(PhantomSkinPath));

        serialized.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(shopContent);
    }

    private static void AppendSkinIfMissing(SerializedProperty items, SkinItem skin)
    {
        if (skin == null)
        {
            return;
        }

        for (int i = 0; i < items.arraySize; i++)
        {
            if (items.GetArrayElementAtIndex(i).objectReferenceValue == skin)
            {
                return;
            }
        }

        items.InsertArrayElementAtIndex(items.arraySize);
        items.GetArrayElementAtIndex(items.arraySize - 1).objectReferenceValue = skin;
    }

    private static void CreateExclusiveSkin(string path, PlayerSkins skinType, SkinItem template)
    {
        SkinItem skin = AssetDatabase.LoadAssetAtPath<SkinItem>(path);

        if (skin != null)
        {
            return;
        }

        skin = ScriptableObject.CreateInstance<SkinItem>();
        AssetDatabase.CreateAsset(skin, path);

        SerializedObject serialized = new SerializedObject(skin);

        if (template != null)
        {
            serialized.FindProperty("<Model>k__BackingField").objectReferenceValue = GetTemplateModel(template);
            serialized.FindProperty("<Icon>k__BackingField").objectReferenceValue = GetTemplateIcon(template);
        }

        serialized.FindProperty("<Price>k__BackingField").intValue = 0;
        serialized.FindProperty("<SkinType>k__BackingField").enumValueIndex = (int)skinType;

        serialized.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(skin);
    }

    private static UnityEngine.Object GetTemplateModel(SkinItem template)
    {
        return new SerializedObject(template).FindProperty("<Model>k__BackingField").objectReferenceValue;
    }

    private static UnityEngine.Object GetTemplateIcon(SkinItem template)
    {
        return new SerializedObject(template).FindProperty("<Icon>k__BackingField").objectReferenceValue;
    }

    private static SkinItem FindSkinByType(PlayerSkins skinType)
    {
        string[] guids = AssetDatabase.FindAssets("t:SkinItem");

        for (int i = 0; i < guids.Length; i++)
        {
            string path = AssetDatabase.GUIDToAssetPath(guids[i]);
            SkinItem skin = AssetDatabase.LoadAssetAtPath<SkinItem>(path);

            if (skin != null && skin.SkinType == skinType)
            {
                return skin;
            }
        }

        return null;
    }

    private static Material CreateHighlightMaterial(UpgradesConfig config)
    {
        Material material = AssetDatabase.LoadAssetAtPath<Material>(HighlightMaterialPath);

        if (material != null)
        {
            return material;
        }

        Shader shader = Shader.Find("Unlit/Color");

        if (shader == null)
        {
            throw new InvalidOperationException("Unlit/Color shader not found.");
        }

        material = new Material(shader);
        material.color = config != null ? config.HighlightColor : Color.yellow;
        AssetDatabase.CreateAsset(material, HighlightMaterialPath);

        return material;
    }

    private static GameObject CreateBurstPrefab()
    {
        if (AssetDatabase.LoadAssetAtPath<GameObject>(BurstPrefabPath) != null)
        {
            return AssetDatabase.LoadAssetAtPath<GameObject>(BurstPrefabPath);
        }

        GameObject root = new GameObject("CollectBurst");
        ParticleSystem particles = root.AddComponent<ParticleSystem>();

        ParticleSystem.MainModule main = particles.main;
        main.duration = 0.01f;
        main.loop = false;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.3f, 0.5f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(2f, 4f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.12f);
        main.gravityModifier = 3f;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.playOnAwake = false;

        ParticleSystem.EmissionModule emission = particles.emission;
        emission.rateOverTime = new ParticleSystem.MinMaxCurve(0f);
        emission.SetBursts(new[]
        {
            new ParticleSystem.Burst(0f, 12)
        });

        ParticleSystem.ShapeModule shape = particles.shape;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = 0.15f;

        ParticleSystemRenderer renderer = root.GetComponent<ParticleSystemRenderer>();
        renderer.renderMode = ParticleSystemRenderMode.Mesh;
        renderer.mesh = FindBuiltInCubeMesh();

        Material cubeMaterial = new Material(Shader.Find("Standard"));
        cubeMaterial.name = "CollectBurstCube";
        AssetDatabase.CreateAsset(cubeMaterial, "Assets/MadSlime/Resources/Materials/CollectBurstCube.mat");
        renderer.material = cubeMaterial;

        PrefabUtility.SaveAsPrefabAsset(root, BurstPrefabPath);
        UnityEngine.Object.DestroyImmediate(root);

        return AssetDatabase.LoadAssetAtPath<GameObject>(BurstPrefabPath);
    }

    private static Mesh FindBuiltInCubeMesh()
    {
        GameObject temp = GameObject.CreatePrimitive(PrimitiveType.Cube);
        Mesh mesh = temp.GetComponent<MeshFilter>().sharedMesh;
        UnityEngine.Object.DestroyImmediate(temp);

        return mesh;
    }

    private static GameObject EnsureUpgradeCardPrefab()
    {
        GameObject existing = AssetDatabase.LoadAssetAtPath<GameObject>(UpgradeCardPrefabPath);

        if (existing == null)
        {
            GameObject created = new GameObject(
                "UpgradeItem",
                typeof(RectTransform),
                typeof(Image),
                typeof(Button),
                typeof(UpgradeItemView));

            BuildUpgradeCardLayout(created);
            PrefabUtility.SaveAsPrefabAsset(created, UpgradeCardPrefabPath);
            UnityEngine.Object.DestroyImmediate(created);

            return AssetDatabase.LoadAssetAtPath<GameObject>(UpgradeCardPrefabPath);
        }

        GameObject root = PrefabUtility.LoadPrefabContents(UpgradeCardPrefabPath);

        try
        {
            UpgradeItemView view = root.GetComponent<UpgradeItemView>();

            if (view == null)
            {
                view = root.AddComponent<UpgradeItemView>();
            }

            SerializedObject serialized = new SerializedObject(view);

            if (serialized.FindProperty("_icon").objectReferenceValue != null)
            {
                return AssetDatabase.LoadAssetAtPath<GameObject>(UpgradeCardPrefabPath);
            }

            BuildUpgradeCardLayout(root);
            PrefabUtility.SaveAsPrefabAsset(root, UpgradeCardPrefabPath);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }

        return AssetDatabase.LoadAssetAtPath<GameObject>(UpgradeCardPrefabPath);
    }

    // The hand-styled UpgradePlate is the canonical upgrade card. Only missing
    // references are filled here — the user's layout is never rebuilt.
    private static GameObject EnsureUpgradePlateWired()
    {
        GameObject plate = AssetDatabase.LoadAssetAtPath<GameObject>(UpgradePlatePrefabPath);

        if (plate == null || plate.GetComponent<UpgradeItemView>() == null)
        {
            return null;
        }

        GameObject root = PrefabUtility.LoadPrefabContents(UpgradePlatePrefabPath);

        try
        {
            UpgradeItemView view = root.GetComponent<UpgradeItemView>();
            SerializedObject serialized = new SerializedObject(view);
            bool changed = false;

            changed |= AssignCardReference(serialized, "_icon", FindCardIcon(root.transform));
            changed |= AssignCardReference(serialized, "_stepText", FindCardText(root.transform, "StepText", "Step"));
            changed |= AssignCardReference(serialized, "_titleText", FindCardText(root.transform, "TitleText", "Title"));
            changed |= AssignCardReference(serialized, "_effectText", FindCardText(root.transform, "EffectText", "Effect"));
            changed |= AssignCardReference(serialized, "_priceText", FindCardPriceText(root.transform));

            if (changed == true)
            {
                serialized.ApplyModifiedPropertiesWithoutUndo();
                PrefabUtility.SaveAsPrefabAsset(root, UpgradePlatePrefabPath);
                Debug.Log("[MadSlimeContentSetup] UpgradePlate missing references wired");
            }
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }

        return plate;
    }

    private static bool AssignCardReference(SerializedObject serialized, string property, UnityEngine.Object target)
    {
        if (target == null)
        {
            return false;
        }

        SerializedProperty propertyField = serialized.FindProperty(property);

        if (propertyField == null || propertyField.objectReferenceValue != null)
        {
            return false;
        }

        propertyField.objectReferenceValue = target;

        return true;
    }

    private static Image FindCardIcon(Transform root)
    {
        foreach (Image image in root.GetComponentsInChildren<Image>(true))
        {
            if (image.gameObject.name != "Icon")
            {
                continue;
            }

            Transform parent = image.transform.parent;

            if (parent != null && parent.name == "Money")
            {
                continue;
            }

            return image;
        }

        return null;
    }

    private static TMP_Text FindCardText(Transform root, string primaryName, string fallbackName)
    {
        foreach (TMP_Text text in root.GetComponentsInChildren<TMP_Text>(true))
        {
            if (text.gameObject.name == primaryName || text.gameObject.name == fallbackName)
            {
                return text;
            }
        }

        return null;
    }

    private static TMP_Text FindCardPriceText(Transform root)
    {
        foreach (TMP_Text text in root.GetComponentsInChildren<TMP_Text>(true))
        {
            Transform parent = text.transform.parent;

            if (parent != null && parent.name == "Money")
            {
                return text;
            }
        }

        return FindCardText(root, "Price", "PriceText");
    }

    private static void BuildUpgradeCardLayout(GameObject root)
    {
        TMP_FontAsset font = LoadFont();

        root.GetComponent<Image>().color = Color.white;

        RectTransform rootRect = (RectTransform)root.transform;
        rootRect.sizeDelta = new Vector2(980f, 220f);

        for (int i = root.transform.childCount - 1; i >= 0; i--)
        {
            UnityEngine.Object.DestroyImmediate(root.transform.GetChild(i).gameObject);
        }

        GameObject icon = new GameObject("Icon", typeof(RectTransform), typeof(Image));
        icon.transform.SetParent(root.transform, false);

        RectTransform iconRect = (RectTransform)icon.transform;
        iconRect.anchorMin = new Vector2(0.5f, 0.5f);
        iconRect.anchorMax = new Vector2(0.5f, 0.5f);
        iconRect.anchoredPosition = new Vector2(-375f, 0f);
        iconRect.sizeDelta = new Vector2(180f, 180f);

        GameObject step = CreateText("Step", font, 32, TextAnchor.MiddleCenter, new Vector2(-375f, -80f), new Vector2(220f, 44f));
        step.transform.SetParent(root.transform, false);

        GameObject title = CreateText("Title", font, 44, TextAnchor.MiddleLeft, new Vector2(30f, 55f), new Vector2(600f, 60f));
        title.transform.SetParent(root.transform, false);

        GameObject effect = CreateText("Effect", font, 34, TextAnchor.MiddleLeft, new Vector2(30f, -10f), new Vector2(620f, 55f));
        effect.transform.SetParent(root.transform, false);

        GameObject price = CreateText("Price", font, 44, TextAnchor.MiddleCenter, new Vector2(340f, 0f), new Vector2(280f, 90f));
        price.transform.SetParent(root.transform, false);

        UpgradeItemView view = root.GetComponent<UpgradeItemView>();
        SerializedObject viewSerialized = new SerializedObject(view);
        viewSerialized.FindProperty("_icon").objectReferenceValue = icon.GetComponent<Image>();
        viewSerialized.FindProperty("_stepText").objectReferenceValue = step.GetComponent<TMP_Text>();
        viewSerialized.FindProperty("_titleText").objectReferenceValue = title.GetComponent<TMP_Text>();
        viewSerialized.FindProperty("_effectText").objectReferenceValue = effect.GetComponent<TMP_Text>();
        viewSerialized.FindProperty("_priceText").objectReferenceValue = price.GetComponent<TMP_Text>();
        viewSerialized.ApplyModifiedPropertiesWithoutUndo();
    }

    internal static GameObject CreateSectorCardPrefab()
    {
        if (AssetDatabase.LoadAssetAtPath<GameObject>(SectorCardPrefabPath) == null)
        {
            GameObject created = new GameObject("RouletteSectorCard", typeof(RectTransform), typeof(Image));
            created.GetComponent<Image>().color = Color.white;

            GameObject icon = new GameObject("Icon", typeof(RectTransform), typeof(Image));
            icon.transform.SetParent(created.transform, false);

            GameObject label = CreateText("Label", LoadFont(), 22, TextAnchor.MiddleCenter, Vector2.zero, Vector2.zero);
            label.transform.SetParent(created.transform, false);

            RouletteSectorCard card = created.AddComponent<RouletteSectorCard>();
            SerializedObject serialized = new SerializedObject(card);
            serialized.FindProperty("_background").objectReferenceValue = created.GetComponent<Image>();
            serialized.FindProperty("_icon").objectReferenceValue = icon.GetComponent<Image>();
            serialized.FindProperty("_label").objectReferenceValue = label.GetComponent<TMP_Text>();
            serialized.ApplyModifiedPropertiesWithoutUndo();

            PrefabUtility.SaveAsPrefabAsset(created, SectorCardPrefabPath);
            UnityEngine.Object.DestroyImmediate(created);
        }

        ApplySectorCardLayout();

        return AssetDatabase.LoadAssetAtPath<GameObject>(SectorCardPrefabPath);
    }

    // Sector card layout by owner request (2026-09-29): the skin icon is primary
    // and always visible; the rarity text is a narrow strip along the bottom edge
    // and may never overlap the icon.
    private static void ApplySectorCardLayout()
    {
        GameObject root = PrefabUtility.LoadPrefabContents(SectorCardPrefabPath);

        try
        {
            Transform icon = root.transform.Find("Icon");
            Transform label = root.transform.Find("Label");

            if (icon == null || label == null)
            {
                throw new InvalidOperationException("RouletteSectorCard prefab has no Icon or Label child.");
            }

            RectTransform iconRect = (RectTransform)icon;
            iconRect.anchorMin = new Vector2(0.5f, 0.5f);
            iconRect.anchorMax = new Vector2(0.5f, 0.5f);
            iconRect.pivot = new Vector2(0.5f, 0.5f);
            iconRect.anchoredPosition = new Vector2(0f, 9f);
            iconRect.sizeDelta = new Vector2(130f, 130f);

            RectTransform labelRect = (RectTransform)label;
            labelRect.anchorMin = new Vector2(0f, 0f);
            labelRect.anchorMax = new Vector2(1f, 0f);
            labelRect.pivot = new Vector2(0.5f, 0f);
            labelRect.anchoredPosition = new Vector2(0f, 6f);
            labelRect.sizeDelta = new Vector2(0f, 30f);

            TextMeshProUGUI labelText = label.GetComponent<TextMeshProUGUI>();

            if (labelText == null)
            {
                throw new InvalidOperationException("RouletteSectorCard Label has no TextMeshProUGUI.");
            }

            labelText.fontSize = 22;
            labelText.alignment = ConvertAnchor(TextAnchor.MiddleCenter);

            PrefabUtility.SaveAsPrefabAsset(root, SectorCardPrefabPath);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }

    internal static GameObject CreateRouletteViewPrefab(GameObject sectorCardPrefab)
    {
        GameObject root = PrefabUtility.LoadPrefabContents(RouletteViewPrefabPath);

        try
        {
            RemoveMissingScriptsRecursive(root.transform);
            RemoveChildByName(root.transform, "Wheel");

            RouletteView view = root.GetComponent<RouletteView>();

            if (view == null)
            {
                throw new InvalidOperationException("RouletteView prefab has no RouletteView component.");
            }

            Transform reel = root.transform.Find("Reel");

            if (reel == null)
            {
                GameObject created = CreateReelSkeleton(sectorCardPrefab);
                created.transform.SetParent(root.transform, false);
                created.transform.SetAsFirstSibling();
                reel = created.transform;
            }

            MoveCenterMarkerIntoViewport(reel);

            // The window size and the whole look belong to the owner — the setup
            // only fills in what is physically missing, never resizes.
            SerializedObject serialized = new SerializedObject(view);
            serialized.FindProperty("_reel").objectReferenceValue = reel.GetComponent<RouletteReel>();
            serialized.ApplyModifiedPropertiesWithoutUndo();

            WireReelCenterZone(reel);
            EnsureWinPopup(root, view);

            PrefabUtility.SaveAsPrefabAsset(root, RouletteViewPrefabPath);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }

        return AssetDatabase.LoadAssetAtPath<GameObject>(RouletteViewPrefabPath);
    }

    private static void MoveCenterMarkerIntoViewport(Transform reel)
    {
        Transform viewport = reel.Find("Viewport");

        if (viewport == null)
        {
            return;
        }

        MoveChildIfMissing(reel, viewport, "CenterBand");
        MoveChildIfMissing(reel, viewport, "CenterLine");
        MoveChildIfMissing(reel, viewport, "CenterLine");
    }

    private static void MoveChildIfMissing(Transform parent, Transform newParent, string childName)
    {
        Transform child = parent.Find(childName);

        if (child != null)
        {
            child.SetParent(newParent, false);
        }
    }

    private static void WireReelCenterZone(Transform reel)
    {
        RouletteReel reelComponent = reel.GetComponent<RouletteReel>();

        if (reelComponent == null)
        {
            throw new InvalidOperationException("The reel has no RouletteReel component.");
        }

        SerializedObject serialized = new SerializedObject(reelComponent);
        SerializedProperty centerZone = serialized.FindProperty("_centerZone");

        if (centerZone.objectReferenceValue == null)
        {
            Transform centerBand = reel.Find("Viewport/CenterBand");

            if (centerBand != null)
            {
                centerZone.objectReferenceValue = (RectTransform)centerBand;
            }
        }

        // Legacy even-grid default: the count now means extra cells beside the
        // center one and must be even.
        SerializedProperty rowCount = serialized.FindProperty("_visibleRowCount");

        if (rowCount.intValue == 3)
        {
            rowCount.intValue = 2;
        }

        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    private static GameObject CreateReelSkeleton(GameObject sectorCardPrefab)
    {
        GameObject reel = new GameObject("Reel", typeof(RectTransform));
        RectTransform reelRect = (RectTransform)reel.transform;
        reelRect.anchorMin = new Vector2(0.5f, 0.5f);
        reelRect.anchorMax = new Vector2(0.5f, 0.5f);
        reelRect.pivot = new Vector2(0.5f, 0.5f);
        reelRect.anchoredPosition = new Vector2(0f, 330f);
        reelRect.sizeDelta = new Vector2(780f, 780f);
        reelRect.localScale = Vector3.one;

        GameObject viewport = new GameObject("Viewport", typeof(RectTransform), typeof(RectMask2D));
        viewport.transform.SetParent(reel.transform, false);

        RectTransform viewportRect = (RectTransform)viewport.transform;
        viewportRect.anchorMin = Vector2.zero;
        viewportRect.anchorMax = Vector2.one;
        viewportRect.pivot = new Vector2(0.5f, 0.5f);
        viewportRect.sizeDelta = Vector2.zero;

        GameObject content = new GameObject("Content", typeof(RectTransform));
        content.transform.SetParent(viewport.transform, false);

        RectTransform contentRect = (RectTransform)content.transform;
        contentRect.anchorMin = Vector2.zero;
        contentRect.anchorMax = Vector2.one;
        contentRect.pivot = new Vector2(0.5f, 0.5f);
        contentRect.sizeDelta = Vector2.zero;

        RouletteReel reelComponent = reel.AddComponent<RouletteReel>();
        SerializedObject serialized = new SerializedObject(reelComponent);
        serialized.FindProperty("_viewport").objectReferenceValue = viewportRect;
        serialized.FindProperty("_content").objectReferenceValue = contentRect;
        serialized.FindProperty("_cardPrefab").objectReferenceValue = sectorCardPrefab.GetComponent<RouletteSectorCard>();
        serialized.ApplyModifiedPropertiesWithoutUndo();

        CreateCenterMarker(reelRect);

        return reel;
    }

    private static void CreateCenterMarker(RectTransform reelRect)
    {
        GameObject band = new GameObject("CenterBand", typeof(RectTransform), typeof(Image));
        band.transform.SetParent(reelRect, false);

        Image bandImage = band.GetComponent<Image>();
        bandImage.color = new Color(0f, 0f, 0f, 0.16f);
        bandImage.raycastTarget = false;

        RectTransform bandRect = (RectTransform)band.transform;
        bandRect.anchorMin = new Vector2(0.5f, 0.5f);
        bandRect.anchorMax = new Vector2(0.5f, 0.5f);
        bandRect.anchoredPosition = Vector2.zero;
        bandRect.sizeDelta = new Vector2(780f, 260f);

        CreateReelLine(reelRect, 130f);
        CreateReelLine(reelRect, -130f);
    }

    private static void CreateReelLine(RectTransform reelRect, float offsetY)
    {
        GameObject line = new GameObject("CenterLine", typeof(RectTransform), typeof(Image));
        line.transform.SetParent(reelRect, false);

        Image lineImage = line.GetComponent<Image>();
        lineImage.color = new Color(1f, 1f, 1f, 0.4f);
        lineImage.raycastTarget = false;

        RectTransform lineRect = (RectTransform)line.transform;
        lineRect.anchorMin = new Vector2(0.5f, 0.5f);
        lineRect.anchorMax = new Vector2(0.5f, 0.5f);
        lineRect.anchoredPosition = new Vector2(0f, offsetY);
        lineRect.sizeDelta = new Vector2(780f, 6f);
    }

    private static void RemoveMissingScriptsRecursive(Transform parent)
    {
        GameObjectUtility.RemoveMonoBehavioursWithMissingScript(parent.gameObject);

        for (int i = 0; i < parent.childCount; i++)
        {
            RemoveMissingScriptsRecursive(parent.GetChild(i));
        }
    }

    private static void DeleteLegacyRouletteWindowPrefab()
    {
        if (AssetDatabase.LoadAssetAtPath<GameObject>(RouletteWindowPrefabPath) != null)
        {
            AssetDatabase.DeleteAsset(RouletteWindowPrefabPath);
        }
    }

    private static void EnsureWinPopup(GameObject root, RouletteView view)
    {
        Transform popup = root.transform.Find("WinPopup");

        // Migration: the stage camera and light must live under ModelSlot so the
        // whole stage moves together and the light lands in the camera's culling mask.
        if (popup != null && popup.Find("ModelSlot/StageCamera") == null)
        {
            UnityEngine.Object.DestroyImmediate(popup.gameObject);
            popup = null;
        }

        if (popup == null)
        {
            popup = BuildWinPopup(root.transform).transform;
        }

        SerializedObject serialized = new SerializedObject(view);
        serialized.FindProperty("_winPopup").objectReferenceValue = popup.GetComponent<RouletteWinPopup>();
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    private static GameObject BuildWinPopup(Transform parent)
    {
        TMP_FontAsset font = LoadFont();

        GameObject popupRoot = new GameObject("WinPopup", typeof(RectTransform), typeof(RouletteWinPopup));
        popupRoot.transform.SetParent(parent, false);

        RectTransform popupRect = (RectTransform)popupRoot.transform;
        popupRect.anchorMin = Vector2.zero;
        popupRect.anchorMax = Vector2.one;
        popupRect.pivot = new Vector2(0.5f, 0.5f);
        popupRect.anchoredPosition = Vector2.zero;
        popupRect.sizeDelta = Vector2.zero;
        popupRect.localScale = Vector3.one;

        GameObject fade = new GameObject("Fade", typeof(RectTransform), typeof(Image));
        fade.transform.SetParent(popupRoot.transform, false);

        RectTransform fadeRect = (RectTransform)fade.transform;
        fadeRect.anchorMin = Vector2.zero;
        fadeRect.anchorMax = Vector2.one;
        fadeRect.pivot = new Vector2(0.5f, 0.5f);
        fadeRect.anchoredPosition = Vector2.zero;
        fadeRect.sizeDelta = Vector2.zero;

        Image fadeImage = fade.GetComponent<Image>();
        fadeImage.color = new Color(0f, 0f, 0f, 0.85f);
        fadeImage.raycastTarget = true;

        GameObject window = new GameObject("Window", typeof(RectTransform), typeof(Image));
        window.transform.SetParent(popupRoot.transform, false);

        RectTransform windowRect = (RectTransform)window.transform;
        windowRect.anchorMin = new Vector2(0.5f, 0.5f);
        windowRect.anchorMax = new Vector2(0.5f, 0.5f);
        windowRect.pivot = new Vector2(0.5f, 0.5f);
        windowRect.anchoredPosition = Vector2.zero;
        windowRect.sizeDelta = new Vector2(680f, 880f);

        window.GetComponent<Image>().color = new Color(0.13f, 0.13f, 0.17f, 1f);

        GameObject prizeArea = new GameObject("PrizeArea", typeof(RectTransform), typeof(RawImage));
        prizeArea.transform.SetParent(window.transform, false);

        RectTransform prizeRect = (RectTransform)prizeArea.transform;
        prizeRect.anchorMin = new Vector2(0.5f, 0.5f);
        prizeRect.anchorMax = new Vector2(0.5f, 0.5f);
        prizeRect.pivot = new Vector2(0.5f, 0.5f);
        prizeRect.anchoredPosition = new Vector2(0f, 105f);
        prizeRect.sizeDelta = new Vector2(560f, 470f);

        RawImage prizeRawImage = prizeArea.GetComponent<RawImage>();
        prizeRawImage.raycastTarget = false;
        prizeRawImage.color = Color.white;

        GameObject coinsLabel = CreateText("CoinsLabel", font, 96, TextAnchor.MiddleCenter, new Vector2(0f, 105f), new Vector2(560f, 160f));
        coinsLabel.transform.SetParent(window.transform, false);
        coinsLabel.GetComponent<TMP_Text>().color = new Color(1f, 0.8f, 0.3f, 1f);

        GameObject rarityPlate = new GameObject("RarityPlate", typeof(RectTransform), typeof(Image));
        rarityPlate.transform.SetParent(window.transform, false);

        RectTransform rarityRect = (RectTransform)rarityPlate.transform;
        rarityRect.anchorMin = new Vector2(0.5f, 0.5f);
        rarityRect.anchorMax = new Vector2(0.5f, 0.5f);
        rarityRect.pivot = new Vector2(0.5f, 0.5f);
        rarityRect.anchoredPosition = new Vector2(0f, -245f);
        rarityRect.sizeDelta = new Vector2(440f, 96f);

        rarityPlate.GetComponent<Image>().raycastTarget = false;

        GameObject rarityLabel = CreateText("Label", font, 40, TextAnchor.MiddleCenter, Vector2.zero, new Vector2(400f, 72f));
        rarityLabel.transform.SetParent(rarityPlate.transform, false);

        Button takeButton = CreateButton("TakeButton", font, new Vector2(0f, -375f), new Vector2(360f, 116f), new Color(0.75f, 0.45f, 0.1f));
        takeButton.transform.SetParent(window.transform, false);
        SetLocalized(takeButton.transform, "roulette_take", font);
        EnsureClickSound(takeButton.gameObject);

        GameObject stage = new GameObject("Stage");
        stage.transform.SetParent(popupRoot.transform, false);

        GameObject modelSlot = new GameObject("ModelSlot");
        modelSlot.transform.SetParent(stage.transform, false);

        GameObject cameraObject = new GameObject("StageCamera", typeof(Camera));
        cameraObject.transform.SetParent(modelSlot.transform, false);
        cameraObject.transform.localPosition = new Vector3(0f, 0f, -12f);

        Camera stageCamera = cameraObject.GetComponent<Camera>();
        stageCamera.orthographic = true;
        stageCamera.orthographicSize = 5f;
        stageCamera.clearFlags = CameraClearFlags.SolidColor;
        stageCamera.backgroundColor = new Color(0f, 0f, 0f, 0f);
        stageCamera.nearClipPlane = 0.3f;
        stageCamera.farClipPlane = 100f;
        stageCamera.cullingMask = 0;

        GameObject lightObject = new GameObject("StageLight", typeof(Light));
        lightObject.transform.SetParent(modelSlot.transform, false);
        lightObject.transform.localPosition = new Vector3(0f, 8f, -8f);
        lightObject.transform.localRotation = Quaternion.Euler(45f, 0f, 0f);

        Light stageLight = lightObject.GetComponent<Light>();
        stageLight.type = LightType.Spot;
        stageLight.color = new Color(1f, 0.96f, 0.9f, 1f);
        stageLight.intensity = 1.1f;
        stageLight.range = 40f;
        stageLight.spotAngle = 50f;
        stageLight.shadows = LightShadows.None;

        RouletteWinPopup popup = popupRoot.GetComponent<RouletteWinPopup>();
        SerializedObject popupSerialized = new SerializedObject(popup);
        popupSerialized.FindProperty("_window").objectReferenceValue = windowRect;
        popupSerialized.FindProperty("_fade").objectReferenceValue = fadeImage;
        popupSerialized.FindProperty("_prizeArea").objectReferenceValue = prizeRawImage;
        popupSerialized.FindProperty("_coinsLabel").objectReferenceValue = coinsLabel.GetComponent<TMP_Text>();
        popupSerialized.FindProperty("_rarityPlate").objectReferenceValue = rarityPlate;
        popupSerialized.FindProperty("_rarityLabel").objectReferenceValue = rarityLabel.GetComponent<TMP_Text>();
        popupSerialized.FindProperty("_takeButton").objectReferenceValue = takeButton;
        popupSerialized.FindProperty("_stageCamera").objectReferenceValue = stageCamera;
        popupSerialized.FindProperty("_modelSlot").objectReferenceValue = modelSlot.transform;
        popupSerialized.ApplyModifiedPropertiesWithoutUndo();

        popupRoot.SetActive(false);

        return popupRoot;
    }

    private static SfxClip LoadUiClickClip()
    {
        SfxClip clip = AssetDatabase.LoadAssetAtPath<SfxClip>(UIClickClipPath);

        if (clip == null)
        {
            throw new InvalidOperationException("UI Click SfxClip asset not found.");
        }

        return clip;
    }

    private static void EnsureClickSound(GameObject buttonObject)
    {
        if (buttonObject.GetComponent<UIButtonSound>() != null)
        {
            return;
        }

        SfxClip clip = LoadUiClickClip();

        UIButtonSound sound = buttonObject.AddComponent<UIButtonSound>();
        SerializedObject serialized = new SerializedObject(sound);
        serialized.FindProperty("_sfxClip").objectReferenceValue = clip;
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void SetupProjectScope(UpgradesConfig config)
    {
        const string path = "Assets/MadSlime/Resources/Prefabs/DI/ProjectScope.prefab";
        GameObject root = PrefabUtility.LoadPrefabContents(path);

        try
        {
            PlayerUpgrades upgrades = root.GetComponentInChildren<PlayerUpgrades>(true);

            if (upgrades == null)
            {
                upgrades = root.AddComponent<PlayerUpgrades>();
            }

            SerializedObject serialized = new SerializedObject(upgrades);
            serialized.FindProperty("_config").objectReferenceValue = config;
            serialized.ApplyModifiedPropertiesWithoutUndo();

            ProjectLifetimeScope scope = root.GetComponentInChildren<ProjectLifetimeScope>(true);

            if (scope == null)
            {
                throw new InvalidOperationException("ProjectScope prefab has no ProjectLifetimeScope.");
            }

            SerializedObject scopeSerialized = new SerializedObject(scope);
            scopeSerialized.FindProperty("_playerUpgrades").objectReferenceValue = upgrades;
            scopeSerialized.ApplyModifiedPropertiesWithoutUndo();

            PrefabUtility.SaveAsPrefabAsset(root, path);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }

    private static void SetupItemPrefabs(Material highlightMaterial)
    {
        string[] guids = AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/MadSlime/Resources/Prefabs/Items" });

        for (int i = 0; i < guids.Length; i++)
        {
            string path = AssetDatabase.GUIDToAssetPath(guids[i]);
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);

            if (prefab == null || prefab.GetComponent<Items.Item>() == null)
            {
                continue;
            }

            SerializedObject serialized = new SerializedObject(prefab.GetComponent<Items.Item>());
            SerializedProperty property = serialized.FindProperty("_highlightMaterial");

            if (property.objectReferenceValue == highlightMaterial)
            {
                continue;
            }

            property.objectReferenceValue = highlightMaterial;
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(prefab);
        }
    }

    private static void SetupGameScene(GameObject burstPrefab)
    {
        EditorSceneManager.OpenScene(GameScenePath, OpenSceneMode.Single);

        Player.Player player = UnityEngine.Object.FindAnyObjectByType<Player.Player>();

        if (player == null)
        {
            throw new InvalidOperationException("Game scene has no Player.");
        }

        MovementDeformer deformer = player.GetComponentInChildren<MovementDeformer>(true);
        AdrenalineBoost adrenaline = player.GetComponentInChildren<AdrenalineBoost>(true);

        SkinApplier skinApplier = player.GetComponentInChildren<SkinApplier>(true);

        if (skinApplier == null)
        {
            throw new InvalidOperationException("Game scene has no SkinApplier under Player.");
        }

        SerializedObject applierSerialized = new SerializedObject(skinApplier);
        Transform skinContainer = (Transform)applierSerialized.FindProperty("_skinsContainer").objectReferenceValue;

        if (skinContainer == null)
        {
            throw new InvalidOperationException("SkinApplier._skinsContainer is not assigned.");
        }

        if (deformer == null)
        {
            GameObject deformContainer = new GameObject("DeformContainer");
            deformContainer.transform.SetParent(skinContainer, false);
            deformer = deformContainer.AddComponent<MovementDeformer>();
        }

        SerializedObject deformerSerialized = new SerializedObject(deformer);
        deformerSerialized.FindProperty("_config").objectReferenceValue = LoadPlayerConfig();
        deformerSerialized.ApplyModifiedPropertiesWithoutUndo();

        applierSerialized.FindProperty("_skinsContainer").objectReferenceValue = deformer.transform;
        applierSerialized.ApplyModifiedPropertiesWithoutUndo();

        if (adrenaline == null)
        {
            adrenaline = player.gameObject.AddComponent<AdrenalineBoost>();
        }

        GameLifetimeScope scope = UnityEngine.Object.FindAnyObjectByType<GameLifetimeScope>();

        if (scope == null)
        {
            throw new InvalidOperationException("Game scene has no GameLifetimeScope.");
        }

        SerializedObject scopeSerialized = new SerializedObject(scope);
        scopeSerialized.FindProperty("_movementDeformer").objectReferenceValue = deformer;
        scopeSerialized.FindProperty("_adrenalineBoost").objectReferenceValue = adrenaline;
        scopeSerialized.ApplyModifiedPropertiesWithoutUndo();

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
    }

    private static void SetupFillScene()
    {
        const string path = "Assets/MadSlime/Scenes/Fill.unity";

        EditorSceneManager.OpenScene(path, OpenSceneMode.Single);

        ShapeFill.FillDebug fillDebug = UnityEngine.Object.FindAnyObjectByType<ShapeFill.FillDebug>();

        if (fillDebug == null)
        {
            throw new InvalidOperationException("Fill scene has no FillDebug.");
        }

        UI.FillProgressUI progress = UnityEngine.Object.FindAnyObjectByType<UI.FillProgressUI>();

        if (progress == null)
        {
            throw new InvalidOperationException("Fill scene has no FillProgressUI.");
        }

        SerializedObject serialized = new SerializedObject(fillDebug);
        serialized.FindProperty("_fillProgressLabel").objectReferenceValue = progress.gameObject;
        serialized.ApplyModifiedPropertiesWithoutUndo();

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
    }

    private static void SetupShopScene()
    {
        const string path = "Assets/MadSlime/Scenes/Shop.unity";

        EditorSceneManager.OpenScene(path, OpenSceneMode.Single);

        ShopLifetimeScope scope = UnityEngine.Object.FindAnyObjectByType<ShopLifetimeScope>();

        if (scope == null)
        {
            throw new InvalidOperationException("NewShop scene has no ShopLifetimeScope.");
        }

        GameObject shopCanvas = FindSceneRoot("ShopCanvas");

        if (shopCanvas == null)
        {
            throw new InvalidOperationException("NewShop scene has no ShopCanvas.");
        }

        GameObject systemsRoot = scope.gameObject;
        UpgradeItemViewFactory upgradeFactory = UnityEngine.Object.FindAnyObjectByType<UpgradeItemViewFactory>(FindObjectsInactive.Include);

        if (upgradeFactory == null)
        {
            upgradeFactory = systemsRoot.AddComponent<UpgradeItemViewFactory>();
        }

        GameObject cardPrefab = EnsureUpgradePlateWired() ?? EnsureUpgradeCardPrefab();

        SerializedObject factorySerialized = new SerializedObject(upgradeFactory);
        SerializedProperty cardPrefabProperty = factorySerialized.FindProperty("_upgradeItemViewPrefab");
        UpgradeItemView currentCard = cardPrefabProperty.objectReferenceValue as UpgradeItemView;
        bool pointsAtScaffold = currentCard != null &&
            AssetDatabase.GetAssetPath(currentCard) == UpgradeCardPrefabPath;

        if (currentCard == null || pointsAtScaffold == true)
        {
            cardPrefabProperty.objectReferenceValue = cardPrefab.GetComponent<UpgradeItemView>();
            factorySerialized.ApplyModifiedPropertiesWithoutUndo();
        }

        AdScheduler adScheduler = UnityEngine.Object.FindAnyObjectByType<AdScheduler>(FindObjectsInactive.Include);

        if (adScheduler == null)
        {
            adScheduler = systemsRoot.AddComponent<AdScheduler>();
        }

        SerializedObject adSerialized = new SerializedObject(adScheduler);
        adSerialized.FindProperty("_config").objectReferenceValue = LoadYandexConfig();
        adSerialized.ApplyModifiedPropertiesWithoutUndo();

        RouletteService rouletteService = UnityEngine.Object.FindAnyObjectByType<RouletteService>(FindObjectsInactive.Include);

        if (rouletteService == null)
        {
            rouletteService = systemsRoot.AddComponent<RouletteService>();
        }

        SerializedObject rouletteSerialized = new SerializedObject(rouletteService);
        rouletteSerialized.FindProperty("_config").objectReferenceValue = LoadRouletteConfig();
        rouletteSerialized.ApplyModifiedPropertiesWithoutUndo();

        Wallet wallet = UnityEngine.Object.FindAnyObjectByType<Wallet>(FindObjectsInactive.Include);

        if (wallet == null)
        {
            wallet = systemsRoot.AddComponent<Wallet>();
        }

        ModelPlacer modelPlacer = UnityEngine.Object.FindAnyObjectByType<ModelPlacer>(FindObjectsInactive.Include);

        if (modelPlacer == null)
        {
            modelPlacer = systemsRoot.AddComponent<ModelPlacer>();
        }

        RouletteView rouletteView = UnityEngine.Object.FindAnyObjectByType<RouletteView>(FindObjectsInactive.Include);

        if (rouletteView == null)
        {
            rouletteView = CreateShopRouletteInstance(shopCanvas.transform);
        }

        MoveRouletteViewToPage(rouletteView, shopCanvas.transform);
        RemoveObsoleteRouletteResult(shopCanvas.transform);
        WireModelPlacer(modelPlacer, shopCanvas.transform);

        Pauser pauser = UnityEngine.Object.FindAnyObjectByType<Pauser>(FindObjectsInactive.Include);

        if (pauser == null)
        {
            GameObject pauserObject = new GameObject("Pauser");
            pauser = pauserObject.AddComponent<Pauser>();
        }

        ShopPanel shopPanel = UnityEngine.Object.FindAnyObjectByType<ShopPanel>(FindObjectsInactive.Include);

        if (shopPanel == null)
        {
            throw new InvalidOperationException("NewShop scene has no ShopPanel.");
        }

        ShopContent shopContent = LoadShopContent();
        CleanShopPages(shopCanvas.transform);
        GameObject separatorPrefab = EnsurePerkSeparatorPrefab();
        GameObject userSeparator = EnsureUserPerkSeparatorWired();

        if (userSeparator != null)
        {
            separatorPrefab = userSeparator;
        }

        Button closeButton = FindChildButton(shopCanvas.transform, "CloseButton");
        ShopCloseButton shopCloseButton = null;

        if (closeButton != null)
        {
            shopCloseButton = closeButton.GetComponent<ShopCloseButton>();

            if (shopCloseButton == null)
            {
                shopCloseButton = closeButton.gameObject.AddComponent<ShopCloseButton>();
            }

            SerializedObject closeButtonSerialized = new SerializedObject(shopCloseButton);
            closeButtonSerialized.FindProperty("_shopPanel").objectReferenceValue = shopPanel;
            closeButtonSerialized.ApplyModifiedPropertiesWithoutUndo();
        }
        else
        {
            Debug.LogWarning("[MadSlimeContentSetup] NewShop has no CloseButton — close wiring skipped.");
        }

        SerializedObject panelSerialized = new SerializedObject(shopPanel);
        panelSerialized.FindProperty("_factory").objectReferenceValue = UnityEngine.Object.FindAnyObjectByType<ShopItemViewFactory>(FindObjectsInactive.Include);
        panelSerialized.FindProperty("_upgradeFactory").objectReferenceValue = upgradeFactory;
        panelSerialized.FindProperty("_rouletteView").objectReferenceValue = rouletteView;
        panelSerialized.FindProperty("_shopContent").objectReferenceValue = shopContent;
        panelSerialized.FindProperty("_perkSeparatorPrefab").objectReferenceValue = separatorPrefab;
        panelSerialized.FindProperty("_upgradesPage").objectReferenceValue = FindDeep(shopCanvas.transform, "UpgradesPage").gameObject;
        panelSerialized.FindProperty("_allSkinsPage").objectReferenceValue = FindDeep(shopCanvas.transform, "SkinsPage").gameObject;
        panelSerialized.FindProperty("_roulettePage").objectReferenceValue = FindDeep(shopCanvas.transform, "RoulettePage").gameObject;

        SerializedProperty rouletteTabProperty = panelSerialized.FindProperty("_rouletteTabButton");

        if (rouletteTabProperty.objectReferenceValue == null)
        {
            Button rouletteButton = FindChildButton(shopCanvas.transform, "RouletteButton");

            if (rouletteButton != null)
            {
                rouletteTabProperty.objectReferenceValue = rouletteButton;
            }
        }

        panelSerialized.ApplyModifiedPropertiesWithoutUndo();

        SerializedObject scopeSerialized = new SerializedObject(scope);
        scopeSerialized.FindProperty("_shopPanel").objectReferenceValue = shopPanel;
        scopeSerialized.FindProperty("_shopItemViewFactory").objectReferenceValue = UnityEngine.Object.FindAnyObjectByType<ShopItemViewFactory>(FindObjectsInactive.Include);
        scopeSerialized.FindProperty("_upgradeItemViewFactory").objectReferenceValue = upgradeFactory;
        scopeSerialized.FindProperty("_modelPlacer").objectReferenceValue = modelPlacer;
        scopeSerialized.FindProperty("_rouletteService").objectReferenceValue = rouletteService;
        scopeSerialized.FindProperty("_rouletteView").objectReferenceValue = rouletteView;
        scopeSerialized.FindProperty("_adScheduler").objectReferenceValue = adScheduler;
        scopeSerialized.FindProperty("_wallet").objectReferenceValue = wallet;
        scopeSerialized.FindProperty("_pauser").objectReferenceValue = pauser;
        scopeSerialized.FindProperty("_shopCloseButton").objectReferenceValue = shopCloseButton;

        ShopMusic shopMusic = UnityEngine.Object.FindAnyObjectByType<ShopMusic>(FindObjectsInactive.Include);

        if (shopMusic == null)
        {
            shopMusic = systemsRoot.AddComponent<ShopMusic>();
        }

        SfxClip musicShopClip = AssetDatabase.LoadAssetAtPath<SfxClip>(MusicShopClipPath);

        if (musicShopClip == null)
        {
            throw new InvalidOperationException("Music Shop SfxClip asset not found.");
        }

        SerializedObject musicSerialized = new SerializedObject(shopMusic);
        musicSerialized.FindProperty("_musicTrack").objectReferenceValue = musicShopClip;
        musicSerialized.ApplyModifiedPropertiesWithoutUndo();
        scopeSerialized.FindProperty("_shopMusic").objectReferenceValue = shopMusic;
        scopeSerialized.ApplyModifiedPropertiesWithoutUndo();

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
    }

    // The result text is gone from the roulette flow — the win popup replaced it.
    // Scene-side leftovers (owner-made "Result" object under RoulettePage) are removed here.
    private static void RemoveObsoleteRouletteResult(Transform shopCanvas)
    {
        Transform page = FindDeep(shopCanvas, "RoulettePage");

        if (page == null)
        {
            return;
        }

        Transform result = FindDeep(page, "Result");

        if (result != null && result.GetComponent<TextMeshProUGUI>() != null)
        {
            UnityEngine.Object.DestroyImmediate(result.gameObject);
        }
    }

    private static void MoveRouletteViewToPage(RouletteView rouletteView, Transform shopCanvas)
    {
        Transform page = FindDeep(shopCanvas, "RoulettePage");

        if (page == null)
        {
            throw new InvalidOperationException("NewShop scene has no RoulettePage under ShopCanvas.");
        }

        if (rouletteView.transform.parent == page)
        {
            return;
        }

        rouletteView.transform.SetParent(page, false);

        RectTransform viewRect = (RectTransform)rouletteView.transform;
        viewRect.anchorMin = new Vector2(0.5f, 0.5f);
        viewRect.anchorMax = new Vector2(0.5f, 0.5f);
        viewRect.pivot = new Vector2(0.5f, 0.5f);
        viewRect.anchoredPosition = Vector2.zero;
        viewRect.localScale = Vector3.one;
    }

    private static void WireModelPlacer(ModelPlacer modelPlacer, Transform shopCanvas)
    {
        SerializedObject serialized = new SerializedObject(modelPlacer);

        if (serialized.FindProperty("_modelsParent").objectReferenceValue != null
            && serialized.FindProperty("_camera").objectReferenceValue != null)
        {
            return;
        }

        Transform skinRoot = FindDeep(shopCanvas, "SkinRoot");
        Transform skinCamera = FindDeep(shopCanvas, "SkinCamera");

        if (skinRoot == null || skinCamera == null)
        {
            Debug.LogWarning("[MadSlimeContentSetup] NewShop has no SkinRoot/SkinCamera — ModelPlacer wiring skipped.");
            return;
        }

        serialized.FindProperty("_modelsParent").objectReferenceValue = skinRoot;
        serialized.FindProperty("_camera").objectReferenceValue = skinCamera.GetComponent<Camera>();
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void CleanShopPages(Transform shopCanvas)
    {
        Transform upgradesPage = FindDeep(shopCanvas, "UpgradesPage");

        if (upgradesPage != null)
        {
            CleanPageViewports(upgradesPage);
        }

        Transform skinsPage = FindDeep(shopCanvas, "SkinsPage");

        if (skinsPage != null)
        {
            CleanPageViewports(skinsPage);
        }
    }

    private static void CleanPageViewports(Transform page)
    {
        Transform viewport = FindDeep(page, "Viewport");

        if (viewport == null)
        {
            return;
        }

        RemoveChildByName(viewport, "UpgradesGrid");

        Transform content = FindDeep(viewport, "Content");

        if (content != null && page.name == "UpgradesPage")
        {
            ApplyUpgradesListLayout(content);
        }
    }

    private static void ApplyUpgradesListLayout(Transform content)
    {
        VerticalLayoutGroup vertical = content.GetComponent<VerticalLayoutGroup>();

        if (vertical == null)
        {
            GridLayoutGroup grid = content.GetComponent<GridLayoutGroup>();

            if (grid != null)
            {
                UnityEngine.Object.DestroyImmediate(grid);
            }

            vertical = content.gameObject.AddComponent<VerticalLayoutGroup>();

            SerializedObject fresh = new SerializedObject(vertical);
            fresh.FindProperty("m_Spacing").vector2Value = new Vector2(0f, 24f);
            fresh.FindProperty("m_ChildAlignment").intValue = (int)TextAnchor.UpperCenter;
            fresh.FindProperty("m_ChildForceExpandHeight").boolValue = false;
            fresh.ApplyModifiedPropertiesWithoutUndo();
        }

        // One card per row: the list owns the width regardless of the card's own
        // anchors and preferred size, the card keeps its authored height.
        SerializedObject serialized = new SerializedObject(vertical);
        serialized.FindProperty("m_ChildControlWidth").boolValue = true;
        serialized.FindProperty("m_ChildControlHeight").boolValue = false;
        serialized.FindProperty("m_ChildForceExpandWidth").boolValue = true;
        serialized.ApplyModifiedPropertiesWithoutUndo();

        // Top-stretch anchors: the viewport owns the width, the fitter owns the height.
        RectTransform contentRect = (RectTransform)content;
        contentRect.anchorMin = new Vector2(0f, 1f);
        contentRect.anchorMax = new Vector2(1f, 1f);
        contentRect.anchoredPosition = new Vector2(0f, 0f);
        contentRect.sizeDelta = new Vector2(0f, 0f);

        ContentSizeFitter fitter = content.GetComponent<ContentSizeFitter>();

        if (fitter == null)
        {
            fitter = content.gameObject.AddComponent<ContentSizeFitter>();
        }

        SerializedObject fitterSerialized = new SerializedObject(fitter);
        fitterSerialized.FindProperty("m_HorizontalFit").intValue = 0;
        fitterSerialized.FindProperty("m_VerticalFit").intValue = 2;
        fitterSerialized.ApplyModifiedPropertiesWithoutUndo();
    }

    private static GameObject EnsurePerkSeparatorPrefab()
    {
        GameObject existing = AssetDatabase.LoadAssetAtPath<GameObject>(PerkSeparatorPrefabPath);

        if (existing != null)
        {
            return existing;
        }

        TMP_FontAsset font = LoadFont();

        GameObject root = new GameObject("PerkSeparator", typeof(RectTransform), typeof(Image));
        RectTransform rootRect = (RectTransform)root.transform;
        rootRect.sizeDelta = new Vector2(980f, 90f);
        root.GetComponent<Image>().color = new Color(1f, 1f, 1f, 0.35f);

        GameObject label = CreateText("Text", font, 36, TextAnchor.MiddleCenter, Vector2.zero, new Vector2(900f, 70f));
        label.transform.SetParent(root.transform, false);
        label.GetComponent<TMP_Text>().color = new Color(0.2f, 0.2f, 0.2f);

        LocalizedText localized = label.AddComponent<LocalizedText>();
        SerializedObject localizedSerialized = new SerializedObject(localized);
        localizedSerialized.FindProperty("_key").stringValue = "shop_one_time";
        localizedSerialized.ApplyModifiedPropertiesWithoutUndo();

        PrefabUtility.SaveAsPrefabAsset(root, PerkSeparatorPrefabPath);
        UnityEngine.Object.DestroyImmediate(root);

        return AssetDatabase.LoadAssetAtPath<GameObject>(PerkSeparatorPrefabPath);
    }

    // The hand-styled separator takes priority over the scaffold; only the
    // localization component is ensured here, the layout is never touched.
    private static GameObject EnsureUserPerkSeparatorWired()
    {
        GameObject separator = AssetDatabase.LoadAssetAtPath<GameObject>(PerkSeparatorUserPrefabPath);

        if (separator == null)
        {
            return null;
        }

        GameObject root = PrefabUtility.LoadPrefabContents(PerkSeparatorUserPrefabPath);

        try
        {
            TMP_Text text = root.GetComponentInChildren<TMP_Text>(true);

            if (text == null)
            {
                return null;
            }

            LocalizedText localized = text.GetComponent<LocalizedText>();
            bool changed = false;

            if (localized == null)
            {
                localized = text.gameObject.AddComponent<LocalizedText>();
                changed = true;
            }

            SerializedObject localizedSerialized = new SerializedObject(localized);

            if (localizedSerialized.FindProperty("_key").stringValue != "shop_one_time")
            {
                localizedSerialized.FindProperty("_key").stringValue = "shop_one_time";
                localizedSerialized.ApplyModifiedPropertiesWithoutUndo();
                changed = true;
            }

            if (changed == true)
            {
                PrefabUtility.SaveAsPrefabAsset(root, PerkSeparatorUserPrefabPath);
                Debug.Log("[MadSlimeContentSetup] PerkSeporator localization wired");
            }
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }

        return separator;
    }

    private static RouletteView CreateShopRouletteInstance(Transform shopCanvas)
    {
        GameObject viewPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(RouletteViewPrefabPath);

        if (viewPrefab == null)
        {
            throw new InvalidOperationException("RouletteView prefab not found. Run the prefab setup first.");
        }

        GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(viewPrefab, shopCanvas);
        RectTransform instanceRect = (RectTransform)instance.transform;

        instanceRect.anchorMin = new Vector2(0.5f, 0.5f);
        instanceRect.anchorMax = new Vector2(0.5f, 0.5f);
        instanceRect.pivot = new Vector2(0.5f, 0.5f);
        instanceRect.anchoredPosition = Vector2.zero;
        instanceRect.localScale = Vector3.one;

        return instance.GetComponent<RouletteView>();
    }

    private static ShopContent LoadShopContent()
    {
        const string path = "Assets/MadSlime/Scriptables/Shop/ShopContent.asset";

        ShopContent content = AssetDatabase.LoadAssetAtPath<ShopContent>(path);

        if (content == null)
        {
            throw new InvalidOperationException("ShopContent asset not found.");
        }

        return content;
    }

    private static void SetupMenuScene()
    {
        const string path = "Assets/MadSlime/Scenes/Menu.unity";

        EditorSceneManager.OpenScene(path, OpenSceneMode.Single);

        MenuLifetimeScope scope = UnityEngine.Object.FindAnyObjectByType<MenuLifetimeScope>();

        if (scope == null)
        {
            throw new InvalidOperationException("Menu scene has no MenuLifetimeScope.");
        }

        MainMenu mainMenu = UnityEngine.Object.FindAnyObjectByType<MainMenu>(FindObjectsInactive.Include);

        if (mainMenu == null)
        {
            throw new InvalidOperationException("Menu scene has no MainMenu.");
        }

        Startup startup = UnityEngine.Object.FindAnyObjectByType<Startup>(FindObjectsInactive.Include);

        if (startup == null)
        {
            throw new InvalidOperationException("Menu scene has no Startup component.");
        }

        Pauser pauser = UnityEngine.Object.FindAnyObjectByType<Pauser>(FindObjectsInactive.Include);

        if (pauser == null)
        {
            throw new InvalidOperationException("Menu scene has no Pauser on the Systems object.");
        }

        GameObject systems = pauser.gameObject;

        AdScheduler adScheduler = systems.GetComponent<AdScheduler>();

        if (adScheduler == null)
        {
            adScheduler = systems.AddComponent<AdScheduler>();
        }

        SerializedObject adSerialized = new SerializedObject(adScheduler);
        adSerialized.FindProperty("_config").objectReferenceValue = LoadYandexConfig();
        adSerialized.ApplyModifiedPropertiesWithoutUndo();

        RouletteService rouletteService = systems.GetComponent<RouletteService>();

        if (rouletteService == null)
        {
            rouletteService = systems.AddComponent<RouletteService>();
        }

        SerializedObject rouletteSerialized = new SerializedObject(rouletteService);
        rouletteSerialized.FindProperty("_config").objectReferenceValue = LoadRouletteConfig();
        rouletteSerialized.ApplyModifiedPropertiesWithoutUndo();

        Wallet wallet = systems.GetComponent<Wallet>();

        if (wallet == null)
        {
            wallet = systems.AddComponent<Wallet>();
        }

        GameObject rouletteViewPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(RouletteViewPrefabPath);

        if (rouletteViewPrefab == null)
        {
            throw new InvalidOperationException("RouletteView prefab not found. Run the prefab setup first.");
        }

        TMP_FontAsset font = LoadFont();

        Button dailyButton = FindChildButton(mainMenu.transform, "DailyButton");

        if (dailyButton == null)
        {
            Button created = CreateButton("DailyButton", font, new Vector2(0f, -1600f), new Vector2(600f, 130f), new Color(0.75f, 0.45f, 0.1f));
            created.transform.SetParent(mainMenu.transform, false);
            dailyButton = created;
        }

        ForceLocalized(dailyButton.transform, "menu_daily");
        EnsureClickSound(dailyButton.gameObject);

        GameObject screen = FindSceneRoot("DailyRouletteScreen");

        if (screen == null)
        {
            screen = CreateDailyRouletteScreen(rouletteViewPrefab, font);
        }

        RouletteView rouletteView = screen.GetComponentInChildren<RouletteView>(true);
        Button closeButton = FindChildButton(screen.transform, "CloseButton");

        if (closeButton == null)
        {
            throw new InvalidOperationException("DailyRouletteScreen has no CloseButton.");
        }

        SerializedObject viewSerialized = new SerializedObject(rouletteView);
        viewSerialized.FindProperty("_screenRoot").objectReferenceValue = screen;
        viewSerialized.FindProperty("_closeButton").objectReferenceValue = closeButton;
        viewSerialized.ApplyModifiedPropertiesWithoutUndo();

        screen.SetActive(false);

        SerializedObject menuSerialized = new SerializedObject(mainMenu);
        menuSerialized.FindProperty("_dailyButton").objectReferenceValue = dailyButton;
        menuSerialized.FindProperty("_dailyRoulette").objectReferenceValue = rouletteView;
        menuSerialized.ApplyModifiedPropertiesWithoutUndo();

        SerializedObject scopeSerialized = new SerializedObject(scope);
        scopeSerialized.FindProperty("_startup").objectReferenceValue = startup;
        scopeSerialized.FindProperty("_rouletteService").objectReferenceValue = rouletteService;
        scopeSerialized.FindProperty("_rouletteView").objectReferenceValue = rouletteView;
        scopeSerialized.FindProperty("_adScheduler").objectReferenceValue = adScheduler;
        scopeSerialized.FindProperty("_wallet").objectReferenceValue = wallet;
        scopeSerialized.FindProperty("_pauser").objectReferenceValue = pauser;
        scopeSerialized.ApplyModifiedPropertiesWithoutUndo();

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
    }

    private static GameObject FindSceneRoot(string name)
    {
        GameObject[] sceneRoots = UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects();

        for (int i = 0; i < sceneRoots.Length; i++)
        {
            if (sceneRoots[i].name == name)
            {
                return sceneRoots[i];
            }
        }

        return null;
    }

    private static GameObject CreateDailyRouletteScreen(GameObject rouletteViewPrefab, TMP_FontAsset font)
    {
        GameObject screen = new GameObject(
            "DailyRouletteScreen",
            typeof(Canvas),
            typeof(CanvasScaler),
            typeof(GraphicRaycaster));

        Canvas canvas = screen.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 20;

        CanvasScaler scaler = screen.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1080f, 1920f);
        scaler.matchWidthOrHeight = 0.5f;

        RectTransform screenRect = (RectTransform)screen.transform;
        screenRect.sizeDelta = new Vector2(1080f, 1920f);

        GameObject fade = new GameObject("Fade", typeof(RectTransform), typeof(Image));
        fade.transform.SetParent(screen.transform, false);
        RectTransform fadeRect = (RectTransform)fade.transform;
        fadeRect.anchorMin = Vector2.zero;
        fadeRect.anchorMax = Vector2.one;
        fadeRect.sizeDelta = Vector2.zero;
        fade.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.85f);

        GameObject viewInstance = (GameObject)PrefabUtility.InstantiatePrefab(rouletteViewPrefab);
        viewInstance.transform.SetParent(screen.transform, false);

        RectTransform viewRect = (RectTransform)viewInstance.transform;
        viewRect.anchorMin = new Vector2(0.5f, 0.5f);
        viewRect.anchorMax = new Vector2(0.5f, 0.5f);
        viewRect.anchoredPosition = Vector2.zero;
        viewRect.localScale = Vector3.one;

        Button closeButton = CreateButton("CloseButton", font, new Vector2(0f, -820f), new Vector2(400f, 110f), new Color(0.6f, 0.2f, 0.2f));
        closeButton.transform.SetParent(screen.transform, false);
        SetLocalized(closeButton.transform, "to_menu", font);
        EnsureClickSound(closeButton.gameObject);

        return screen;
    }

    private static RouletteConfig LoadRouletteConfig()
    {
        RouletteConfig config = AssetDatabase.LoadAssetAtPath<RouletteConfig>(RouletteConfigPath);

        if (config == null)
        {
            throw new InvalidOperationException("RouletteConfig asset not found.");
        }

        return config;
    }

    private static void ForceLocalized(Transform buttonTransform, string key)
    {
        TMP_Text text = buttonTransform.GetComponentInChildren<TMP_Text>(true);

        if (text == null)
        {
            return;
        }

        LocalizedText localized = text.GetComponent<LocalizedText>();

        if (localized == null)
        {
            localized = text.gameObject.AddComponent<LocalizedText>();
        }

        SerializedObject serialized = new SerializedObject(localized);
        serialized.FindProperty("_key").stringValue = key;
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void RemoveChildByName(Transform parent, string name)
    {
        Transform child = parent.Find(name);

        if (child != null)
        {
            UnityEngine.Object.DestroyImmediate(child.gameObject);
        }
    }

    private static void SetupSkinCardPrefab()
    {
        const string path = "Assets/MadSlime/Resources/Prefabs/UI/Skins/SkinItem.prefab";
        GameObject root = PrefabUtility.LoadPrefabContents(path);

        try
        {
            ShopItemView view = root.GetComponent<ShopItemView>();

            if (view == null)
            {
                throw new InvalidOperationException("SkinItem prefab has no ShopItemView.");
            }

            if (new SerializedObject(view).FindProperty("_rouletteBadge").objectReferenceValue != null)
            {
                return;
            }

            TMP_FontAsset font = LoadFont();

            GameObject badge = new GameObject("RouletteBadge", typeof(RectTransform), typeof(Image));
            badge.transform.SetParent(root.transform, false);

            RectTransform badgeRect = (RectTransform)badge.transform;
            badgeRect.anchorMin = new Vector2(0.5f, 1f);
            badgeRect.anchorMax = new Vector2(0.5f, 1f);
            badgeRect.pivot = new Vector2(0.5f, 0.5f);
            badgeRect.anchoredPosition = new Vector2(0f, 24f);
            badgeRect.sizeDelta = new Vector2(160f, 44f);
            badge.GetComponent<Image>().color = new Color(0.7f, 0.3f, 0.9f);

            GameObject label = CreateText("Label", font, 26, TextAnchor.MiddleCenter, Vector2.zero, new Vector2(150f, 40f));
            label.transform.SetParent(badge.transform, false);
            label.GetComponent<TMP_Text>().color = Color.white;

            LocalizedText localized = label.AddComponent<LocalizedText>();
            SerializedObject localizedSerialized = new SerializedObject(localized);
            localizedSerialized.FindProperty("_key").stringValue = "shop_exclusive";
            localizedSerialized.ApplyModifiedPropertiesWithoutUndo();

            SerializedObject serialized = new SerializedObject(view);
            serialized.FindProperty("_rouletteBadge").objectReferenceValue = badge;
            serialized.ApplyModifiedPropertiesWithoutUndo();

            PrefabUtility.SaveAsPrefabAsset(root, path);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }

    private static void SetupFailMenuPrefab()
    {
        GameObject root = PrefabUtility.LoadPrefabContents(FailMenuPrefabPath);

        try
        {
            FailMenu menu = root.GetComponentInChildren<FailMenu>(true);

            if (menu == null)
            {
                throw new InvalidOperationException("FailMenu prefab has no FailMenu component.");
            }

            Transform rescueButtonTransform = FindDeep(root.transform, "NextLevel");

            if (rescueButtonTransform == null)
            {
                throw new InvalidOperationException("FailMenu prefab has no NextLevel button.");
            }

            Button rescueButton = rescueButtonTransform.GetComponent<Button>();

            TMP_Text text = rescueButtonTransform.GetComponentInChildren<TMP_Text>(true);

            if (text == null)
            {
                GameObject label = CreateText("Text", LoadFont(), 34, TextAnchor.MiddleCenter, Vector2.zero, new Vector2(420f, 100f));
                label.transform.SetParent(rescueButtonTransform, false);
                text = label.GetComponent<TMP_Text>();
            }

            if (text.GetComponent<LocalizedText>() == null)
            {
                LocalizedText localized = text.gameObject.AddComponent<LocalizedText>();
                SerializedObject localizedSerialized = new SerializedObject(localized);
                localizedSerialized.FindProperty("_key").stringValue = "fill_rescue";
                localizedSerialized.ApplyModifiedPropertiesWithoutUndo();
            }

            SerializedObject serialized = new SerializedObject(menu);
            serialized.FindProperty("_rescueButton").objectReferenceValue = rescueButton;
            serialized.ApplyModifiedPropertiesWithoutUndo();

            PrefabUtility.SaveAsPrefabAsset(root, FailMenuPrefabPath);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }

    private static PlayerConfig LoadPlayerConfig()
    {
        PlayerConfig config = AssetDatabase.LoadAssetAtPath<PlayerConfig>("Assets/MadSlime/Scriptables/Player/PlayerConfig.asset");

        if (config == null)
        {
            throw new InvalidOperationException("PlayerConfig asset not found.");
        }

        return config;
    }

    private static YandexConfig LoadYandexConfig()
    {
        YandexConfig config = AssetDatabase.LoadAssetAtPath<YandexConfig>("Assets/MadSlime/Scriptables/AD/YandexConfig.asset");

        if (config == null)
        {
            throw new InvalidOperationException("YandexConfig asset not found.");
        }

        return config;
    }

    private static TMP_FontAsset LoadFont()
    {
        TMP_FontAsset font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(FontPath);

        if (font == null)
        {
            throw new InvalidOperationException("LiberationSans SDF font asset not found.");
        }

        return font;
    }

    private static GameObject CreateText(string name, TMP_FontAsset font, int fontSize, TextAnchor anchor,
        Vector2 position, Vector2 size)
    {
        GameObject textObject = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
        TextMeshProUGUI text = textObject.GetComponent<TextMeshProUGUI>();
        text.font = font;
        text.fontSize = fontSize;
        text.alignment = ConvertAnchor(anchor);
        text.text = name;
        text.color = Color.white;
        text.raycastTarget = false;

        RectTransform rect = (RectTransform)textObject.transform;
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;

        return textObject;
    }

    // A direct (TextAlignmentOptions)anchor cast is wrong: the numeric values of
    // TextAnchor and TextAlignmentOptions do not match (MiddleCenter lands on TopRight).
    private static TextAlignmentOptions ConvertAnchor(TextAnchor anchor)
    {
        switch (anchor)
        {
            case TextAnchor.UpperLeft:
                return TextAlignmentOptions.TopLeft;

            case TextAnchor.UpperCenter:
                return TextAlignmentOptions.Top;

            case TextAnchor.UpperRight:
                return TextAlignmentOptions.TopRight;

            case TextAnchor.MiddleLeft:
                return TextAlignmentOptions.Left;

            case TextAnchor.MiddleCenter:
                return TextAlignmentOptions.Center;

            case TextAnchor.MiddleRight:
                return TextAlignmentOptions.Right;

            case TextAnchor.LowerLeft:
                return TextAlignmentOptions.BottomLeft;

            case TextAnchor.LowerCenter:
                return TextAlignmentOptions.Bottom;

            case TextAnchor.LowerRight:
                return TextAlignmentOptions.BottomRight;

            default:
                throw new ArgumentOutOfRangeException(
                    nameof(anchor),
                    anchor,
                    "CreateText received an unknown text anchor.");
        }
    }

    private static Button CreateButton(string name, TMP_FontAsset font, Vector2 position, Vector2 size, Color color)
    {
        GameObject buttonObject = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
        buttonObject.GetComponent<Image>().color = color;

        RectTransform rect = (RectTransform)buttonObject.transform;
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;

        GameObject label = CreateText("Text", font, 36, TextAnchor.MiddleCenter, Vector2.zero, size);
        label.transform.SetParent(buttonObject.transform, false);

        return buttonObject.GetComponent<Button>();
    }

    private static void SetLocalized(Transform buttonTransform, string key, TMP_FontAsset font)
    {
        TMP_Text text = buttonTransform.GetComponentInChildren<TMP_Text>(true);

        if (text == null)
        {
            return;
        }

        if (text.GetComponent<LocalizedText>() != null)
        {
            return;
        }

        LocalizedText localized = text.gameObject.AddComponent<LocalizedText>();
        SerializedObject serialized = new SerializedObject(localized);
        serialized.FindProperty("_key").stringValue = key;
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    private static Button FindChildButton(Transform parent, string name)
    {
        Transform found = FindDeep(parent, name);

        return found != null ? found.GetComponent<Button>() : null;
    }

    private static Transform FindDeep(Transform parent, string name)
    {
        if (parent.name == name)
        {
            return parent;
        }

        for (int i = 0; i < parent.childCount; i++)
        {
            Transform found = FindDeep(parent.GetChild(i), name);

            if (found != null)
            {
                return found;
            }
        }

        return null;
    }
}
