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
    private const string SectorCardPrefabPath = "Assets/MadSlime/Resources/Prefabs/UI/Skins/RouletteSectorCard.prefab";
    private const string RouletteViewPrefabPath = "Assets/MadSlime/Resources/Prefabs/UI/Skins/RouletteView.prefab";
    private const string RouletteWindowPrefabPath = "Assets/MadSlime/Resources/Prefabs/UI/Skins/RouletteWindow.prefab";
    private const string UIClickClipPath = "Assets/MadSlime/Scriptables/Audio/UI Click.asset";
    private const string CrownSkinPath = "Assets/MadSlime/Scriptables/Skins/Crown.asset";
    private const string PhantomSkinPath = "Assets/MadSlime/Scriptables/Skins/Phantom.asset";
    private const string ShopPrefabPath = "Assets/MadSlime/Resources/Prefabs/UI/Skins/Shop.prefab";
    private const string FailMenuPrefabPath = "Assets/MadSlime/Resources/Prefabs/UI/ResultMenu/FailMenu.prefab";
    private const string GameScenePath = "Assets/MadSlime/Scenes/Game.unity";
    private const string FontPath = "Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset";

    [MenuItem("Mad Slime/Setup Content")]
    public static void SetupAll()
    {
        EnsureFolders();

        UpgradesConfig upgradesConfig = CreateUpgradesConfig();
        CreateExclusiveSkins();
        RouletteConfig rouletteConfig = CreateRouletteConfig();
        Material highlightMaterial = CreateHighlightMaterial(upgradesConfig);
        GameObject burstPrefab = CreateBurstPrefab();
        GameObject upgradeCardPrefab = CreateUpgradeCardPrefab();
        GameObject sectorCardPrefab = CreateSectorCardPrefab();
        GameObject rouletteViewPrefab = CreateRouletteViewPrefab(sectorCardPrefab);
        DeleteLegacyRouletteWindowPrefab();

        SetupProjectScope(upgradesConfig);
        SetupItemPrefabs(highlightMaterial);
        SetupGameScene(burstPrefab);
        SetupFillScene();
        SetupShopPrefab(upgradeCardPrefab, rouletteViewPrefab);
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

        upgrades.ClearArray();
        InsertUpgrade(upgrades, UpgradeType.Speed, 200, 150, 0.1f, 5);
        InsertUpgrade(upgrades, UpgradeType.Appetite, 250, 200, 0.15f, 5);
        InsertUpgrade(upgrades, UpgradeType.Taste, 400, 300, 0.2f, 5);
        InsertUpgrade(upgrades, UpgradeType.Metabolism, 350, 250, 0.15f, 5);

        SerializedProperty perks = serialized.FindProperty("_perks");

        perks.ClearArray();
        InsertPerk(perks, PerkType.Smell, 3000);
        InsertPerk(perks, PerkType.Adrenaline, 4000);
        InsertPerk(perks, PerkType.Ambitions, 5000);

        serialized.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(config);

        return config;
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

    private static void InsertPerk(SerializedProperty array, PerkType type, int cost)
    {
        int index = array.arraySize;
        array.InsertArrayElementAtIndex(index);
        SerializedProperty element = array.GetArrayElementAtIndex(index);

        element.FindPropertyRelative("_type").enumValueIndex = (int)type;
        element.FindPropertyRelative("_cost").intValue = cost;
    }

    private static RouletteConfig CreateRouletteConfig()
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
        serialized.FindProperty("_idleDegreesPerSecond").floatValue = 24f;
        serialized.FindProperty("_windBackDegrees").floatValue = 45f;
        serialized.FindProperty("_windBackDuration").floatValue = 0.35f;
        serialized.FindProperty("_minTurns").intValue = 3;
        serialized.FindProperty("_maxTurns").intValue = 5;
        serialized.FindProperty("_spinDuration").floatValue = 3f;

        serialized.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(config);

        return config;
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

    private static GameObject CreateUpgradeCardPrefab()
    {
        if (AssetDatabase.LoadAssetAtPath<GameObject>(UpgradeCardPrefabPath) != null)
        {
            return AssetDatabase.LoadAssetAtPath<GameObject>(UpgradeCardPrefabPath);
        }

        TMP_FontAsset font = LoadFont();
        Color cardColor = new Color(0.16f, 0.16f, 0.2f);

        GameObject root = new GameObject("UpgradeItem", typeof(RectTransform), typeof(Image), typeof(Button));
        RectTransform rootRect = (RectTransform)root.transform;
        rootRect.sizeDelta = new Vector2(300f, 110f);
        root.GetComponent<Image>().color = cardColor;

        GameObject title = CreateText("Title", font, 34, TextAnchor.UpperLeft, new Vector2(-140f, 12f), new Vector2(20f, 98f));
        title.transform.SetParent(root.transform, false);

        GameObject state = CreateText("State", font, 30, TextAnchor.UpperLeft, new Vector2(-140f, 12f), new Vector2(20f, 52f));
        state.transform.SetParent(root.transform, false);

        GameObject price = CreateText("Price", font, 34, TextAnchor.UpperRight, new Vector2(20f, 12f), new Vector2(160f, 44f));
        price.transform.SetParent(root.transform, false);
        price.AddComponent<IntValueView>();

        UpgradeItemView view = root.AddComponent<UpgradeItemView>();
        SerializedObject serialized = new SerializedObject(view);
        serialized.FindProperty("_titleText").objectReferenceValue = title.GetComponent<TMP_Text>();
        serialized.FindProperty("_stateText").objectReferenceValue = state.GetComponent<TMP_Text>();
        serialized.FindProperty("_priceView").objectReferenceValue = price.GetComponent<IntValueView>();
        serialized.ApplyModifiedPropertiesWithoutUndo();

        PrefabUtility.SaveAsPrefabAsset(root, UpgradeCardPrefabPath);
        UnityEngine.Object.DestroyImmediate(root);

        return AssetDatabase.LoadAssetAtPath<GameObject>(UpgradeCardPrefabPath);
    }

    private static GameObject CreateSectorCardPrefab()
    {
        if (AssetDatabase.LoadAssetAtPath<GameObject>(SectorCardPrefabPath) != null)
        {
            return AssetDatabase.LoadAssetAtPath<GameObject>(SectorCardPrefabPath);
        }

        TMP_FontAsset font = LoadFont();

        GameObject root = new GameObject("RouletteSectorCard", typeof(RectTransform), typeof(Image));
        RectTransform rootRect = (RectTransform)root.transform;
        rootRect.sizeDelta = new Vector2(150f, 150f);
        root.GetComponent<Image>().color = Color.white;

        GameObject icon = new GameObject("Icon", typeof(RectTransform), typeof(Image));
        icon.transform.SetParent(root.transform, false);
        RectTransform iconRect = (RectTransform)icon.transform;
        iconRect.anchorMin = new Vector2(0.5f, 0.5f);
        iconRect.anchorMax = new Vector2(0.5f, 0.5f);
        iconRect.sizeDelta = new Vector2(90f, 90f);

        GameObject label = CreateText("Label", font, 34, TextAnchor.MiddleCenter, new Vector2(0f, -55f), new Vector2(140f, 40f));
        label.transform.SetParent(root.transform, false);

        RouletteSectorCard card = root.AddComponent<RouletteSectorCard>();
        SerializedObject serialized = new SerializedObject(card);
        serialized.FindProperty("_background").objectReferenceValue = root.GetComponent<Image>();
        serialized.FindProperty("_icon").objectReferenceValue = icon.GetComponent<Image>();
        serialized.FindProperty("_label").objectReferenceValue = label.GetComponent<TMP_Text>();
        serialized.ApplyModifiedPropertiesWithoutUndo();

        PrefabUtility.SaveAsPrefabAsset(root, SectorCardPrefabPath);
        UnityEngine.Object.DestroyImmediate(root);

        return AssetDatabase.LoadAssetAtPath<GameObject>(SectorCardPrefabPath);
    }

    private static GameObject CreateRouletteViewPrefab(GameObject sectorCardPrefab)
    {
        if (AssetDatabase.LoadAssetAtPath<GameObject>(RouletteViewPrefabPath) != null)
        {
            return AssetDatabase.LoadAssetAtPath<GameObject>(RouletteViewPrefabPath);
        }

        TMP_FontAsset font = LoadFont();

        GameObject root = new GameObject("RouletteView", typeof(RectTransform));
        RectTransform rootRect = (RectTransform)root.transform;
        rootRect.sizeDelta = new Vector2(1080f, 1920f);

        GameObject wheelObject = new GameObject("Wheel", typeof(RectTransform));
        wheelObject.transform.SetParent(root.transform, false);
        RectTransform wheelRect = (RectTransform)wheelObject.transform;
        wheelRect.anchorMin = new Vector2(0.5f, 0.5f);
        wheelRect.anchorMax = new Vector2(0.5f, 0.5f);
        wheelRect.sizeDelta = new Vector2(900f, 900f);
        wheelRect.anchoredPosition = new Vector2(0f, 200f);

        GameObject result = CreateText("Result", font, 56, TextAnchor.MiddleCenter, new Vector2(0f, -420f), new Vector2(900f, 120f));
        result.transform.SetParent(root.transform, false);
        result.GetComponent<TMP_Text>().color = Color.white;

        GameObject cost = CreateText("Cost", font, 48, TextAnchor.MiddleCenter, new Vector2(0f, -520f), new Vector2(400f, 70f));
        cost.transform.SetParent(root.transform, false);
        cost.GetComponent<TMP_Text>().color = Color.white;

        GameObject freeTimer = CreateText("FreeTimer", font, 40, TextAnchor.MiddleCenter, new Vector2(-280f, -640f), new Vector2(420f, 60f));
        freeTimer.transform.SetParent(root.transform, false);
        freeTimer.GetComponent<TMP_Text>().color = Color.white;

        GameObject adSpins = CreateText("AdSpins", font, 40, TextAnchor.MiddleCenter, new Vector2(280f, -640f), new Vector2(420f, 60f));
        adSpins.transform.SetParent(root.transform, false);
        adSpins.GetComponent<TMP_Text>().color = Color.white;

        Button freeButton = CreateButton("FreeButton", font, new Vector2(-280f, -760f), new Vector2(420f, 140f), Color.green);
        freeButton.transform.SetParent(root.transform, false);

        Button adButton = CreateButton("AdButton", font, new Vector2(280f, -760f), new Vector2(420f, 140f), new Color(0.9f, 0.6f, 0.1f));
        adButton.transform.SetParent(root.transform, false);

        Button coinsButton = CreateButton("CoinsButton", font, new Vector2(0f, -920f), new Vector2(700f, 140f), new Color(0.2f, 0.5f, 0.9f));
        coinsButton.transform.SetParent(root.transform, false);

        RouletteWheel wheel = root.AddComponent<RouletteWheel>();
        SerializedObject wheelSerialized = new SerializedObject(wheel);
        wheelSerialized.FindProperty("_wheelContainer").objectReferenceValue = wheelRect;
        wheelSerialized.FindProperty("_sectorCardPrefab").objectReferenceValue = sectorCardPrefab.GetComponent<RouletteSectorCard>();
        wheelSerialized.FindProperty("_radius").floatValue = 340f;
        wheelSerialized.ApplyModifiedPropertiesWithoutUndo();

        RouletteView view = root.AddComponent<RouletteView>();
        SerializedObject serialized = new SerializedObject(view);
        serialized.FindProperty("_wheel").objectReferenceValue = wheel;
        serialized.FindProperty("_freeButton").objectReferenceValue = freeButton;
        serialized.FindProperty("_adButton").objectReferenceValue = adButton;
        serialized.FindProperty("_coinsButton").objectReferenceValue = coinsButton;
        serialized.FindProperty("_freeTimerText").objectReferenceValue = freeTimer.GetComponent<TMP_Text>();
        serialized.FindProperty("_adSpinsText").objectReferenceValue = adSpins.GetComponent<TMP_Text>();
        serialized.FindProperty("_costText").objectReferenceValue = cost.GetComponent<TMP_Text>();
        serialized.FindProperty("_resultText").objectReferenceValue = result.GetComponent<TMP_Text>();
        serialized.ApplyModifiedPropertiesWithoutUndo();

        EnsureClickSound(freeButton.gameObject);
        EnsureClickSound(adButton.gameObject);
        EnsureClickSound(coinsButton.gameObject);

        PrefabUtility.SaveAsPrefabAsset(root, RouletteViewPrefabPath);
        UnityEngine.Object.DestroyImmediate(root);

        return AssetDatabase.LoadAssetAtPath<GameObject>(RouletteViewPrefabPath);
    }

    private static void DeleteLegacyRouletteWindowPrefab()
    {
        if (AssetDatabase.LoadAssetAtPath<GameObject>(RouletteWindowPrefabPath) != null)
        {
            AssetDatabase.DeleteAsset(RouletteWindowPrefabPath);
        }
    }

    private static void EnsureClickSound(GameObject buttonObject)
    {
        if (buttonObject.GetComponent<UIButtonSound>() != null)
        {
            return;
        }

        SfxClip clip = AssetDatabase.LoadAssetAtPath<SfxClip>(UIClickClipPath);

        if (clip == null)
        {
            throw new InvalidOperationException("UI Click SfxClip asset not found.");
        }

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
        CollectBurstSpawner burst = player.GetComponentInChildren<CollectBurstSpawner>(true);
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

        if (burst == null)
        {
            burst = player.gameObject.AddComponent<CollectBurstSpawner>();
        }

        SerializedObject burstSerialized = new SerializedObject(burst);
        burstSerialized.FindProperty("_burstPrefab").objectReferenceValue = burstPrefab;
        burstSerialized.ApplyModifiedPropertiesWithoutUndo();

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
        scopeSerialized.FindProperty("_collectBurstSpawner").objectReferenceValue = burst;
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
            throw new InvalidOperationException("Shop scene has no ShopLifetimeScope.");
        }

        UpgradeItemViewFactory upgradeFactory = UnityEngine.Object.FindAnyObjectByType<UpgradeItemViewFactory>(FindObjectsInactive.Include);

        if (upgradeFactory == null)
        {
            GameObject shopRoot = UnityEngine.Object.FindAnyObjectByType<Shop>(FindObjectsInactive.Include).gameObject;
            upgradeFactory = shopRoot.AddComponent<UpgradeItemViewFactory>();
        }

        AdScheduler adScheduler = UnityEngine.Object.FindAnyObjectByType<AdScheduler>(FindObjectsInactive.Include);

        if (adScheduler == null)
        {
            GameObject shopRoot = UnityEngine.Object.FindAnyObjectByType<Shop>(FindObjectsInactive.Include).gameObject;
            adScheduler = shopRoot.AddComponent<AdScheduler>();
        }

        SerializedObject adSerialized = new SerializedObject(adScheduler);
        adSerialized.FindProperty("_config").objectReferenceValue = LoadYandexConfig();
        adSerialized.ApplyModifiedPropertiesWithoutUndo();

        RouletteService rouletteService = UnityEngine.Object.FindAnyObjectByType<RouletteService>(FindObjectsInactive.Include);

        if (rouletteService == null)
        {
            GameObject shopRoot = UnityEngine.Object.FindAnyObjectByType<Shop>(FindObjectsInactive.Include).gameObject;
            rouletteService = shopRoot.AddComponent<RouletteService>();
        }

        SerializedObject rouletteSerialized = new SerializedObject(rouletteService);
        rouletteSerialized.FindProperty("_config").objectReferenceValue = LoadRouletteConfig();
        rouletteSerialized.ApplyModifiedPropertiesWithoutUndo();

        RouletteView rouletteView = UnityEngine.Object.FindAnyObjectByType<RouletteView>(FindObjectsInactive.Include);

        if (rouletteView == null)
        {
            throw new InvalidOperationException("Shop scene has no RouletteView. Run the shop prefab setup first.");
        }

        SerializedObject scopeSerialized = new SerializedObject(scope);
        scopeSerialized.FindProperty("_upgradeItemViewFactory").objectReferenceValue = upgradeFactory;
        scopeSerialized.FindProperty("_rouletteService").objectReferenceValue = rouletteService;
        scopeSerialized.FindProperty("_rouletteView").objectReferenceValue = rouletteView;
        scopeSerialized.FindProperty("_adScheduler").objectReferenceValue = adScheduler;

        Pauser pauser = UnityEngine.Object.FindAnyObjectByType<Pauser>(FindObjectsInactive.Include);

        if (pauser == null)
        {
            GameObject pauserObject = new GameObject("Pauser");
            pauser = pauserObject.AddComponent<Pauser>();
        }

        scopeSerialized.FindProperty("_pauser").objectReferenceValue = pauser;
        scopeSerialized.ApplyModifiedPropertiesWithoutUndo();

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        EditorSceneManager.SaveScene(EditorSceneManager.GetActiveScene());
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

    private static void SetupShopPrefab(GameObject upgradeCardPrefab, GameObject rouletteViewPrefab)
    {
        GameObject root = PrefabUtility.LoadPrefabContents(ShopPrefabPath);

        try
        {
            ShopPanel panel = root.GetComponentInChildren<ShopPanel>(true);
            Shop shop = root.GetComponentInChildren<Shop>(true);
            UpgradeItemViewFactory upgradeFactory = root.GetComponentInChildren<UpgradeItemViewFactory>(true);

            if (panel == null || shop == null)
            {
                throw new InvalidOperationException(
                    $"Shop prefab is missing components. root='{root.name}' " +
                    $"ShopPanel={(panel != null ? "ok" : "NULL")} " +
                    $"Shop={(shop != null ? "ok" : "NULL")}");
            }

            if (upgradeFactory == null)
            {
                upgradeFactory = panel.gameObject.AddComponent<UpgradeItemViewFactory>();
            }

            SerializedObject factorySerialized = new SerializedObject(upgradeFactory);
            factorySerialized.FindProperty("_upgradeItemViewPrefab").objectReferenceValue = upgradeCardPrefab.GetComponent<UpgradeItemView>();
            factorySerialized.ApplyModifiedPropertiesWithoutUndo();

            Transform itemsParent = (Transform)new SerializedObject(panel).FindProperty("_itemsParent").objectReferenceValue;

            if (itemsParent == null)
            {
                throw new InvalidOperationException("ShopPanel._itemsParent is not assigned.");
            }

            RectTransform upgradesParent = FindOrCreateRectChild(itemsParent.parent, "UpgradesGrid", (RectTransform)itemsParent);
            RectTransform sourceRect = (RectTransform)itemsParent;
            upgradesParent.anchorMin = sourceRect.anchorMin;
            upgradesParent.anchorMax = sourceRect.anchorMax;
            upgradesParent.pivot = sourceRect.pivot;
            upgradesParent.anchoredPosition = sourceRect.anchoredPosition;
            upgradesParent.sizeDelta = sourceRect.sizeDelta;
            upgradesParent.offsetMin = sourceRect.offsetMin;
            upgradesParent.offsetMax = sourceRect.offsetMax;
            GridLayoutGroup grid = upgradesParent.GetComponent<GridLayoutGroup>();

            if (grid == null)
            {
                grid = upgradesParent.gameObject.AddComponent<GridLayoutGroup>();
            }

            GridLayoutGroup skinGrid = itemsParent.GetComponent<GridLayoutGroup>();

            if (skinGrid != null)
            {
                grid.cellSize = skinGrid.cellSize;
                grid.spacing = skinGrid.spacing;
                grid.padding = skinGrid.padding;
                grid.constraint = skinGrid.constraint;
                grid.constraintCount = skinGrid.constraintCount;
            }

            upgradesParent.gameObject.SetActive(false);

            TMP_FontAsset font = LoadFont();

            RectTransform tabs = FindOrCreateRectChild(root.transform, "Tabs", null);
            tabs.anchorMin = new Vector2(0.5f, 1f);
            tabs.anchorMax = new Vector2(0.5f, 1f);
            tabs.pivot = new Vector2(0.5f, 1f);
            tabs.anchoredPosition = new Vector2(0f, -40f);
            tabs.sizeDelta = new Vector2(900f, 110f);

            Transform legacySkinsTab = tabs.Find("SkinsTab");

            if (legacySkinsTab != null)
            {
                legacySkinsTab.name = "AllSkinsTab";
            }

            Transform legacyRouletteTab = tabs.Find("RouletteTab");

            if (legacyRouletteTab != null)
            {
                legacyRouletteTab.name = "SkinsTab";
            }

            Button upgradesTab = EnsureTabButton(tabs, "UpgradesTab", "tab_upgrades", font, new Vector2(-300f, 0f));
            Button skinsTab = EnsureTabButton(tabs, "SkinsTab", "tab_skins", font, new Vector2(0f, 0f));
            Button allSkinsTab = EnsureTabButton(tabs, "AllSkinsTab", "tab_all_skins", font, new Vector2(300f, 0f));

            ForceLocalized(upgradesTab.transform, "tab_upgrades");
            ForceLocalized(skinsTab.transform, "tab_skins");
            ForceLocalized(allSkinsTab.transform, "tab_all_skins");

            EnsureClickSound(upgradesTab.gameObject);
            EnsureClickSound(skinsTab.gameObject);
            EnsureClickSound(allSkinsTab.gameObject);

            RemoveChildByName(root.transform, "SkinsRoulette");
            RemoveRootVestigeComponents(root);

            RectTransform roulettePage = FindOrCreateRectChild(itemsParent.parent, "RoulettePage", (RectTransform)itemsParent);
            roulettePage.anchorMin = ((RectTransform)itemsParent).anchorMin;
            roulettePage.anchorMax = ((RectTransform)itemsParent).anchorMax;
            roulettePage.pivot = ((RectTransform)itemsParent).pivot;
            roulettePage.anchoredPosition = ((RectTransform)itemsParent).anchoredPosition;
            roulettePage.sizeDelta = ((RectTransform)itemsParent).sizeDelta;

            RouletteView rouletteView = EnsureRouletteView(roulettePage, rouletteViewPrefab);

            roulettePage.gameObject.SetActive(true);
            ((RectTransform)itemsParent).gameObject.SetActive(false);

            RemoveDuplicateChildren(roulettePage, "RouletteView", rouletteView.transform);

            SerializedObject panelSerialized = new SerializedObject(panel);
            panelSerialized.FindProperty("_upgradesParent").objectReferenceValue = upgradesParent;
            panelSerialized.FindProperty("_rouletteParent").objectReferenceValue = roulettePage;
            panelSerialized.FindProperty("_upgradeFactory").objectReferenceValue = upgradeFactory;
            panelSerialized.FindProperty("_upgradesTabButton").objectReferenceValue = upgradesTab;
            panelSerialized.FindProperty("_skinsTabButton").objectReferenceValue = skinsTab;
            panelSerialized.FindProperty("_allSkinsTabButton").objectReferenceValue = allSkinsTab;
            panelSerialized.FindProperty("_rouletteView").objectReferenceValue = rouletteView;
            panelSerialized.ApplyModifiedPropertiesWithoutUndo();

            SetupSkinCardPrefab();

            PrefabUtility.SaveAsPrefabAsset(root, ShopPrefabPath);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }

    private static RouletteView EnsureRouletteView(RectTransform page, GameObject viewPrefab)
    {
        RouletteView existing = page.GetComponentInChildren<RouletteView>(true);

        if (existing != null)
        {
            return existing;
        }

        GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(viewPrefab);
        instance.transform.SetParent(page, false);

        RectTransform instanceRect = (RectTransform)instance.transform;
        instanceRect.anchorMin = new Vector2(0.5f, 0.5f);
        instanceRect.anchorMax = new Vector2(0.5f, 0.5f);
        instanceRect.anchoredPosition = Vector2.zero;
        instanceRect.localScale = Vector3.one;

        return instance.GetComponent<RouletteView>();
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

    private static void RemoveRootVestigeComponents(GameObject root)
    {
        UIButtonSound vestigialSound = root.GetComponent<UIButtonSound>();

        if (vestigialSound != null)
        {
            UnityEngine.Object.DestroyImmediate(vestigialSound);
        }

        Button vestigialButton = root.GetComponent<Button>();

        if (vestigialButton != null)
        {
            UnityEngine.Object.DestroyImmediate(vestigialButton);
        }

        AudioSource vestigialSource = root.GetComponent<AudioSource>();

        if (vestigialSource != null)
        {
            UnityEngine.Object.DestroyImmediate(vestigialSource);
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
        text.alignment = (TextAlignmentOptions)anchor;
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

    private static RectTransform FindOrCreateRectChild(Transform parent, string name, RectTransform sizeSource)
    {
        Transform existing = parent.Find(name);

        if (existing != null)
        {
            return (RectTransform)existing;
        }

        GameObject child = new GameObject(name, typeof(RectTransform));
        RectTransform rect = (RectTransform)child.transform;
        rect.SetParent(parent, false);
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.localScale = Vector3.one;

        if (sizeSource != null)
        {
            RectTransform sourceRect = (RectTransform)sizeSource;
            rect.anchoredPosition = sourceRect.anchoredPosition + new Vector2(0f, 130f);
            rect.sizeDelta = sourceRect.sizeDelta;
        }

        return rect;
    }

    private static Button EnsureTabButton(Transform parent, string name, string key, TMP_FontAsset font, Vector2 position)
    {
        Transform existing = parent.Find(name);

        if (existing != null)
        {
            return existing.GetComponent<Button>();
        }

        Button button = CreateButton(name, font, position, new Vector2(280f, 100f), new Color(0.25f, 0.35f, 0.55f));
        button.transform.SetParent(parent, false);
        SetLocalized(button.transform, key, font);

        return button;
    }

    private static Button FindChildButton(Transform parent, string name)
    {
        Transform found = FindDeep(parent, name);

        return found != null ? found.GetComponent<Button>() : null;
    }

    private static void RemoveDuplicateChildren(Transform parent, string name, Transform keep)
    {
        for (int i = parent.childCount - 1; i >= 0; i--)
        {
            Transform child = parent.GetChild(i);

            if (child != keep && child.name.StartsWith(name) == true)
            {
                UnityEngine.Object.DestroyImmediate(child.gameObject);
            }
        }
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
