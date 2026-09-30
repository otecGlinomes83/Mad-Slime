// Точечные правки контента после новых фич:
// 1) QuotaHighlight.mat переезжает с плоской жёлтой заливки на шейдер-аутлайн;
// 2) WinPopup.prefab получает ссылку на фон окна (перекраска в цвет редкости);
// 3) Menu.unity: у DailyRouletteScreen появляются StartMoney/StartAD и
//    провоживаются поля RouletteView (_spinButton/_spinPriceText/_adButton/_adButtonText),
//    без которых ежедневная рулетка в главном меню не инициализируется.
// Запуск только через batchmode:
// batchmode -executeMethod MadSlimeUiFixup.RunAll
public static class MadSlimeUiFixup
{
    private const string HighlightMaterialPath = "Assets/MadSlime/Resources/Materials/QuotaHighlight.mat";
    private const string HighlightShaderName = "MadSlime/ItemOutline";
    private const string UpgradesConfigPath = "Assets/MadSlime/Scriptables/Upgrades/UpgradesConfig.asset";
    private const string WinPopupPrefabPath = "Assets/MadSlime/Resources/Prefabs/UI/WinPopup.prefab";
    private const string MenuScenePath = "Assets/MadSlime/Scenes/Menu.unity";
    private const string FontPath = "Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset";
    private const string RouletteViewPrefabPath = "Assets/MadSlime/Resources/Prefabs/UI/Skins/RouletteView.prefab";
    private const string SecretIconPath = "Assets/Hyper_Casual_UI/Sprites/Icons/lock.png";
    private const string LocalizationTablePath = "Assets/MadSlime/Scriptables/Localization/Localization.asset";
    private const string UiClickClipPath = "Assets/MadSlime/Scriptables/Audio/UI Click.asset";

    private static readonly UnityEngine.Vector2 SpinButtonSize = new UnityEngine.Vector2(302.45f, 130.42f);
    private static readonly UnityEngine.Vector2 SpinButtonPosition = new UnityEngine.Vector2(-212f, -304f);
    private static readonly UnityEngine.Vector2 AdButtonPosition = new UnityEngine.Vector2(151.23f, -304f);
    private static readonly UnityEngine.Color SpinButtonColor = new UnityEngine.Color(0.3105f, 1f, 0f, 1f);
    private static readonly UnityEngine.Color AdButtonColor = new UnityEngine.Color(1f, 0.7088f, 0f, 1f);
    private static readonly UnityEngine.Color CoinsBackgroundColor = new UnityEngine.Color(0.13f, 0.13f, 0.17f, 1f);

    // Вызывается только через batchmode: -executeMethod MadSlimeUiFixup.RunAll
    public static void RunAll()
    {
        UnityEditor.AssetDatabase.Refresh();

        FixHighlightMaterial();
        FixWinPopupBackground();
        FixMenuRoulette();
        FixMenuDailyButton();
        FixRouletteSecretCard();
        EnsureSecretLocalization();

        UnityEditor.AssetDatabase.SaveAssets();
        UnityEngine.Debug.Log("[MadSlimeUiFixup] done");
    }

    // Секретный сектор ежедневной рулетки: в префаб RouletteView ставится
    // иконка-замок (_secretIcon); цвет плашки — дефолт из кода. Плюс гасится
    // вуаль CenterBand (полупрозрачный чёрный прямоугольник ПОВЕРХ карточек):
    // математике ленты нужен только rect объекта, Image не нужен.
    private static void FixRouletteSecretCard()
    {
        UnityEngine.GameObject prefab =
            UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.GameObject>(RouletteViewPrefabPath);

        if (prefab == null)
        {
            throw new System.InvalidOperationException(
                $"RouletteView prefab not found at '{RouletteViewPrefabPath}'.");
        }

        Roulette.RouletteView view = prefab.GetComponent<Roulette.RouletteView>();

        if (view == null)
        {
            throw new System.InvalidOperationException("RouletteView prefab root has no RouletteView.");
        }

        UnityEngine.Sprite secretIcon = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.Sprite>(SecretIconPath);

        if (secretIcon == null)
        {
            throw new System.InvalidOperationException($"Secret icon sprite not found at '{SecretIconPath}'.");
        }

        UnityEditor.SerializedObject serialized = new UnityEditor.SerializedObject(view);
        serialized.FindProperty("_secretIcon").objectReferenceValue = secretIcon;
        serialized.ApplyModifiedPropertiesWithoutUndo();

        UnityEngine.Transform band = prefab.transform.Find("Reel/Viewport/CenterBand");
        UnityEngine.UI.Image bandImage = band != null ? band.GetComponent<UnityEngine.UI.Image>() : null;

        if (bandImage != null && bandImage.enabled == true)
        {
            bandImage.enabled = false;
            UnityEditor.EditorUtility.SetDirty(bandImage);
        }

        UnityEditor.EditorUtility.SetDirty(prefab);
    }

    private static void EnsureSecretLocalization()
    {
        Scriptables.LocalizationTable table = UnityEditor.AssetDatabase.LoadAssetAtPath<
            Scriptables.LocalizationTable>(LocalizationTablePath);

        if (table == null)
        {
            throw new System.InvalidOperationException(
                $"Localization table not found at '{LocalizationTablePath}'.");
        }

        UnityEditor.SerializedObject serialized = new UnityEditor.SerializedObject(table);
        UnityEditor.SerializedProperty entries = serialized.FindProperty("_entries");

        for (int i = 0; i < entries.arraySize; i++)
        {
            if (entries.GetArrayElementAtIndex(i).FindPropertyRelative("_key").stringValue == "roulette_secret")
            {
                return;
            }
        }

        int last = entries.arraySize;
        entries.InsertArrayElementAtIndex(last);

        UnityEditor.SerializedProperty entry = entries.GetArrayElementAtIndex(last);
        entry.FindPropertyRelative("_key").stringValue = "roulette_secret";
        entry.FindPropertyRelative("_ru").stringValue = "СЕКРЕТНО";
        entry.FindPropertyRelative("_en").stringValue = "SECRET";
        entry.FindPropertyRelative("_tr").stringValue = "GİZLİ";

        serialized.ApplyModifiedPropertiesWithoutUndo();
        UnityEditor.EditorUtility.SetDirty(table);
    }

    // Кнопка открытия ежедневной рулетки в меню. Ничего не спавнится в рантайме:
    // кнопка живёт на сцене. Раньше она стояла на y=-1600 (глубоко за экраном),
    // теперь — дубликат самой нижней кнопки стека, переименованный и сдвинутый
    // ниже на один шаг стека.
    private static void FixMenuDailyButton()
    {
        UnityEditor.SceneManagement.EditorSceneManager.OpenScene(
            MenuScenePath, UnityEditor.SceneManagement.OpenSceneMode.Single);

        UI.MainMenu mainMenu = UnityEngine.Object.FindAnyObjectByType<UI.MainMenu>(
            UnityEngine.FindObjectsInactive.Include);

        if (mainMenu == null)
        {
            throw new System.InvalidOperationException("Menu scene has no MainMenu.");
        }

        UnityEngine.Transform oldDailyTransform = mainMenu.transform.Find("DailyButton");
        UnityEngine.UI.Button oldDailyButton =
            oldDailyTransform != null ? oldDailyTransform.GetComponent<UnityEngine.UI.Button>() : null;

        if (oldDailyButton == null)
        {
            throw new System.InvalidOperationException(
                "Menu scene has no DailyButton under MainMenu.");
        }

        // Самая нижняя кнопка стека (кроме самой DailyButton).
        UnityEngine.UI.Button lowest = null;

        for (int i = 0; i < mainMenu.transform.childCount; i++)
        {
            UnityEngine.UI.Button button = mainMenu.transform.GetChild(i)
                .GetComponent<UnityEngine.UI.Button>();

            if (button == null || button.transform.name == "DailyButton")
            {
                continue;
            }

            if (lowest == null
                || ((UnityEngine.RectTransform)button.transform).anchoredPosition.y
                < ((UnityEngine.RectTransform)lowest.transform).anchoredPosition.y)
            {
                lowest = button;
            }
        }

        if (lowest == null)
        {
            throw new System.InvalidOperationException(
                "Menu scene has no other buttons under MainMenu to clone the style from.");
        }

        // Шаг стека: зазор между двумя самыми нижними кнопками.
        float lowestY = ((UnityEngine.RectTransform)lowest.transform).anchoredPosition.y;
        float step = ((UnityEngine.RectTransform)lowest.transform).rect.height + 20f;

        for (int i = 0; i < mainMenu.transform.childCount; i++)
        {
            UnityEngine.UI.Button button = mainMenu.transform.GetChild(i)
                .GetComponent<UnityEngine.UI.Button>();

            if (button == null || button == lowest || button.transform.name == "DailyButton")
            {
                continue;
            }

            float y = ((UnityEngine.RectTransform)button.transform).anchoredPosition.y;

            if (y < lowestY)
            {
                step = UnityEngine.Mathf.Abs(lowestY - y);
                break;
            }
        }

        UnityEngine.GameObject dailyObject = UnityEngine.Object.Instantiate(
            lowest.gameObject, lowest.transform.parent);
        dailyObject.name = "DailyButton";

        UnityEngine.RectTransform dailyRect = (UnityEngine.RectTransform)dailyObject.transform;
        dailyRect.anchoredPosition = new UnityEngine.Vector2(
            ((UnityEngine.RectTransform)lowest.transform).anchoredPosition.x,
            lowestY - step);

        // Дубликат тащит локализацию донора — переназначаем на ключ ежедневки.
        UI.LocalizedText localized = dailyObject.GetComponentInChildren<UI.LocalizedText>(true);

        if (localized == null)
        {
            TMPro.TMP_Text label = dailyObject.GetComponentInChildren<TMPro.TMP_Text>(true);

            if (label == null)
            {
                throw new System.InvalidOperationException(
                    "Menu scene: the DailyButton clone has neither LocalizedText nor TMP_Text to localize.");
            }

            localized = label.gameObject.AddComponent<UI.LocalizedText>();
        }

        UnityEditor.SerializedObject localizedSerialized = new UnityEditor.SerializedObject(localized);
        localizedSerialized.FindProperty("_key").stringValue = "menu_daily";
        localizedSerialized.ApplyModifiedPropertiesWithoutUndo();

        // Статичный текст дубликата показывает надпись донора до первого Apply
        // локализации — ставим нейтральный, чтобы в редакторе не путал.
        TMPro.TMP_Text dailyLabel = dailyObject.GetComponentInChildren<TMPro.TMP_Text>(true);

        if (dailyLabel != null)
        {
            dailyLabel.text = "DAILY";
            UnityEditor.EditorUtility.SetDirty(dailyLabel);
        }

        EnsureClickSound(dailyObject);

        // Старая кнопка (за экраном) больше не нужна.
        UnityEngine.Object.DestroyImmediate(oldDailyButton.gameObject);

        UnityEditor.SerializedObject menuSerialized = new UnityEditor.SerializedObject(mainMenu);
        menuSerialized.FindProperty("_dailyButton").objectReferenceValue =
            dailyObject.GetComponent<UnityEngine.UI.Button>();
        menuSerialized.ApplyModifiedPropertiesWithoutUndo();

        UnityEditor.EditorUtility.SetDirty(mainMenu);
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
            UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());
        UnityEditor.SceneManagement.EditorSceneManager.SaveScene(
            UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());
    }

    // Вызывается только через batchmode: -executeMethod MadSlimeUiFixup.ValidateRoulettes
    public static void ValidateRoulettes()
    {
        ValidateMenuScene();
        ValidateShopScene();
        ValidatePopupPrefab();
        ValidateHighlightMaterialAndItems();
        ValidateLocalization();

        UnityEngine.Debug.Log("[MadSlimeUiFixup] ValidateRoulettes: OK");
    }

    private static void Require(bool condition, string message)
    {
        if (condition == false)
        {
            throw new System.InvalidOperationException($"[ValidateRoulettes] {message}");
        }
    }

    private static void RequireAssigned(UnityEditor.SerializedObject serialized, string property, string context)
    {
        UnityEditor.SerializedProperty propertyReference = serialized.FindProperty(property);

        Require(propertyReference != null, $"{context}: no serialized property '{property}'.");
        Require(propertyReference.objectReferenceValue != null,
            $"{context}: '{property}' is not assigned.");
    }

    private static void ValidateMenuScene()
    {
        UnityEditor.SceneManagement.EditorSceneManager.OpenScene(
            MenuScenePath, UnityEditor.SceneManagement.OpenSceneMode.Single);

        DI.MenuLifetimeScope scope =
            UnityEngine.Object.FindAnyObjectByType<DI.MenuLifetimeScope>(
                UnityEngine.FindObjectsInactive.Include);

        Require(scope != null, "Menu scene has no MenuLifetimeScope.");

        UnityEditor.SerializedObject scopeSerialized = new UnityEditor.SerializedObject(scope);

        foreach (string field in new[]
                 {
                     "_mainMenu", "_startup", "_rouletteService", "_rouletteView",
                     "_adScheduler", "_wallet", "_pauser"
                 })
        {
            RequireAssigned(scopeSerialized, field, "MenuLifetimeScope");
        }

        UI.MainMenu mainMenu = UnityEngine.Object.FindAnyObjectByType<UI.MainMenu>(
            UnityEngine.FindObjectsInactive.Include);

        Require(mainMenu != null, "Menu scene has no MainMenu.");

        UnityEditor.SerializedObject menuSerialized = new UnityEditor.SerializedObject(mainMenu);
        RequireAssigned(menuSerialized, "_dailyButton", "MainMenu");
        RequireAssigned(menuSerialized, "_dailyRoulette", "MainMenu");

        UnityEngine.GameObject screen = FindSceneRoot("DailyRouletteScreen");

        Require(screen != null, "Menu scene has no DailyRouletteScreen.");

        Roulette.RouletteView rouletteView = screen.GetComponentInChildren<Roulette.RouletteView>(true);

        Require(rouletteView != null, "DailyRouletteScreen has no RouletteView.");

        ValidateRouletteView(rouletteView, requireAdButton: true);

        UnityEditor.SerializedObject viewSerialized = new UnityEditor.SerializedObject(rouletteView);

        Require(viewSerialized.FindProperty("_screenRoot").objectReferenceValue == screen,
            "RouletteView._screenRoot must point at DailyRouletteScreen.");

        Roulette.RouletteService service =
            UnityEngine.Object.FindAnyObjectByType<Roulette.RouletteService>(
                UnityEngine.FindObjectsInactive.Include);

        Require(service != null, "Menu scene has no RouletteService.");

        UnityEditor.SerializedObject serviceSerialized = new UnityEditor.SerializedObject(service);
        RequireAssigned(serviceSerialized, "_config", "RouletteService");

        Roulette.RouletteConfig config = (Roulette.RouletteConfig)serviceSerialized
            .FindProperty("_config").objectReferenceValue;

        ValidateConfig(config);
    }

    private static void ValidateRouletteView(Roulette.RouletteView view, bool requireAdButton)
    {
        UnityEditor.SerializedObject serialized = new UnityEditor.SerializedObject(view);

        RequireAssigned(serialized, "_reel", view.name);
        RequireAssigned(serialized, "_spinButton", view.name);
        RequireAssigned(serialized, "_spinPriceText", view.name);
        RequireAssigned(serialized, "_winPopupPrefab", view.name);
        RequireAssigned(serialized, "_screenRoot", view.name);

        if (requireAdButton == true)
        {
            RequireAssigned(serialized, "_adButton", view.name);
            RequireAssigned(serialized, "_adButtonText", view.name);
        }

        Roulette.RouletteReel reel = (Roulette.RouletteReel)serialized
            .FindProperty("_reel").objectReferenceValue;

        UnityEditor.SerializedObject reelSerialized = new UnityEditor.SerializedObject(reel);

        RequireAssigned(reelSerialized, "_viewport", reel.name);
        RequireAssigned(reelSerialized, "_content", reel.name);
        RequireAssigned(reelSerialized, "_centerZone", reel.name);
        RequireAssigned(reelSerialized, "_cardPrefab", reel.name);

        int visibleRowCount = reelSerialized.FindProperty("_visibleRowCount").intValue;

        Require(visibleRowCount >= 2 && visibleRowCount % 2 == 0,
            $"RouletteReel '{reel.name}': Visible Row Count must be even and >= 2, got {visibleRowCount}.");

        // Вуаль над карточками должна быть выключена.
        UnityEngine.RectTransform centerZone = (UnityEngine.RectTransform)reelSerialized
            .FindProperty("_centerZone").objectReferenceValue;
        UnityEngine.UI.Image bandImage = centerZone.GetComponent<UnityEngine.UI.Image>();

        Require(bandImage == null || bandImage.enabled == false,
            $"Reel '{reel.name}': CenterBand Image is enabled — it draws a veil over the cards.");

        // Иконка секретного сектора назначена в префабе.
        RequireAssigned(serialized, "_secretIcon", view.name);
    }

    private static void ValidateConfig(Roulette.RouletteConfig config)
    {
        Require(config != null, "RouletteConfig is null.");

        Require(config.FreeSpinCooldownSeconds == 1800,
            $"RouletteConfig: free spin cooldown must be 1800s (30 min), got {config.FreeSpinCooldownSeconds}.");
        Require(config.AdSpinsPerWindow == 3,
            $"RouletteConfig: ad spins per window must be 3, got {config.AdSpinsPerWindow}.");
        Require(config.AdSpinWindowSeconds == 1800,
            $"RouletteConfig: ad window must be 1800s, got {config.AdSpinWindowSeconds}.");
        Require(config.RarityTable != null, "RouletteConfig: RarityTable is not assigned.");
        Require(config.Sectors.Count > 0, "RouletteConfig: no sectors.");
        Require(config.ExclusiveSkins.Count > 0, "RouletteConfig: no exclusive skins.");

        int skinSectorCount = 0;

        foreach (Roulette.RouletteSector sector in config.Sectors)
        {
            Require(sector.Weight > 0f, "RouletteConfig: a sector has zero weight.");

            if (sector.RewardType == Roulette.RouletteSector.RewardKind.Skin)
            {
                Require(sector.Skin != null, "RouletteConfig: a skin sector has no skin.");
                skinSectorCount++;
            }
            else
            {
                Require(sector.Coins > 0, "RouletteConfig: a coins sector pays zero coins.");
            }
        }

        foreach (Skins.SkinItem exclusive in config.ExclusiveSkins)
        {
            Require(exclusive != null, "RouletteConfig: ExclusiveSkins contains a null entry.");
            Require(exclusive.Model != null,
                $"Exclusive skin '{exclusive.name}' has no Model prefab.");
            Require(exclusive.Icon != null,
                $"Exclusive skin '{exclusive.name}' has no Icon.");

            bool winnableHere = false;

            foreach (Roulette.RouletteSector sector in config.Sectors)
            {
                if (sector.RewardType == Roulette.RouletteSector.RewardKind.Skin && sector.Skin == exclusive)
                {
                    winnableHere = true;
                    break;
                }
            }

            Require(winnableHere,
                $"Exclusive skin '{exclusive.name}' has no sector in the daily roulette — it would be unobtainable.");
        }

        Require(skinSectorCount == config.ExclusiveSkins.Count,
            $"RouletteConfig: {skinSectorCount} skin sectors vs {config.ExclusiveSkins.Count} exclusives — sectors must reference exclusives one to one.");
    }

    private static void ValidateShopScene()
    {
        UnityEditor.SceneManagement.EditorSceneManager.OpenScene(
            "Assets/MadSlime/Scenes/Shop.unity", UnityEditor.SceneManagement.OpenSceneMode.Single);

        Roulette.RouletteView rouletteView = UnityEngine.Object.FindAnyObjectByType<Roulette.RouletteView>(
            UnityEngine.FindObjectsInactive.Include);

        Require(rouletteView != null, "Shop scene has no RouletteView.");

        ValidateRouletteView(rouletteView, requireAdButton: true);

        Skins.ShopPanel shopPanel = UnityEngine.Object.FindAnyObjectByType<Skins.ShopPanel>(
            UnityEngine.FindObjectsInactive.Include);

        Require(shopPanel != null, "Shop scene has no ShopPanel.");

        UnityEditor.SerializedObject panelSerialized = new UnityEditor.SerializedObject(shopPanel);
        RequireAssigned(panelSerialized, "_rouletteView", "ShopPanel");
        RequireAssigned(panelSerialized, "_shopContent", "ShopPanel");
    }

    private static void ValidatePopupPrefab()
    {
        UnityEngine.GameObject prefab =
            UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.GameObject>(WinPopupPrefabPath);

        Require(prefab != null, "WinPopup prefab not found.");

        Roulette.RouletteWinPopup popup = prefab.GetComponent<Roulette.RouletteWinPopup>();

        Require(popup != null, "WinPopup prefab root has no RouletteWinPopup.");

        UnityEditor.SerializedObject serialized = new UnityEditor.SerializedObject(popup);

        foreach (string field in new[]
                 {
                     "_window", "_background", "_fade", "_prizeArea", "_coinsLabel",
                     "_rarityPlate", "_rarityLabel", "_takeButton", "_stageCamera", "_modelSlot"
                 })
        {
            RequireAssigned(serialized, field, "WinPopup");
        }
    }

    private static void ValidateHighlightMaterialAndItems()
    {
        UnityEngine.Material material =
            UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.Material>(HighlightMaterialPath);

        Require(material != null, "QuotaHighlight material not found.");

        Require(material.shader != null && material.shader.name == HighlightShaderName,
            $"QuotaHighlight material uses shader '{(material.shader != null ? material.shader.name : "null")}', expected '{HighlightShaderName}'.");

        // Имя шейдера существует даже с битой компиляцией (в игре — маджента),
        // поэтому проверяем именно ошибки компиляции. Тексты ошибок Unity
        // печатает в консоль/лог импорта.
        Require(UnityEditor.ShaderUtil.ShaderHasError(material.shader) == false,
            $"Shader '{HighlightShaderName}' has compile errors — see the import log in Console.");

        string[] guids = UnityEditor.AssetDatabase.FindAssets(
            "t:Prefab", new[] { "Assets/MadSlime/Resources/Prefabs/Items" });

        Require(guids.Length > 0, "No item prefabs found.");

        foreach (string guid in guids)
        {
            string path = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
            UnityEngine.GameObject prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.GameObject>(path);
            Items.Item item = prefab != null ? prefab.GetComponent<Items.Item>() : null;

            if (item == null)
            {
                continue;
            }

            UnityEditor.SerializedObject serialized = new UnityEditor.SerializedObject(item);
            RequireAssigned(serialized, "_highlightMaterial", $"Item prefab '{prefab.name}'");
            RequireAssigned(serialized, "_ghostMaterial", $"Item prefab '{prefab.name}'");
        }
    }

    private static void ValidateLocalization()
    {
        Scriptables.LocalizationTable table = UnityEditor.AssetDatabase.LoadAssetAtPath<
            Scriptables.LocalizationTable>("Assets/MadSlime/Scriptables/Localization/Localization.asset");

        Require(table != null, "Localization asset not found.");

        UnityEditor.SerializedObject serialized = new UnityEditor.SerializedObject(table);
        UnityEditor.SerializedProperty entries = serialized.FindProperty("_entries");

        Require(entries != null && entries.isArray, "LocalizationTable has no _entries array.");

        System.Collections.Generic.HashSet<string> keys = new System.Collections.Generic.HashSet<string>();

        for (int i = 0; i < entries.arraySize; i++)
        {
            keys.Add(entries.GetArrayElementAtIndex(i).FindPropertyRelative("_key").stringValue);
        }

        foreach (string key in new[]
                 {
                     "roulette_free_ready", "roulette_ad_spins", "roulette_secret",
                     "rarity_common", "rarity_rare", "rarity_epic", "rarity_legendary"
                 })
        {
            Require(keys.Contains(key), $"Localization has no key '{key}'.");
        }
    }

    private static void FixHighlightMaterial()
    {
        UnityEngine.Material material =
            UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.Material>(HighlightMaterialPath);

        if (material == null)
        {
            throw new System.InvalidOperationException(
                $"QuotaHighlight material not found at '{HighlightMaterialPath}'.");
        }

        UnityEngine.Shader shader = UnityEngine.Shader.Find(HighlightShaderName);

        if (shader == null)
        {
            throw new System.InvalidOperationException(
                $"Shader '{HighlightShaderName}' not found. Let Unity import Assets/MadSlime/Shaders/ItemOutline.shader first.");
        }

        Upgrades.UpgradesConfig config =
            UnityEditor.AssetDatabase.LoadAssetAtPath<Upgrades.UpgradesConfig>(UpgradesConfigPath);

        if (config == null)
        {
            throw new System.InvalidOperationException(
                $"UpgradesConfig not found at '{UpgradesConfigPath}'.");
        }

        if (material.shader != shader)
        {
            material.shader = shader;
        }

        material.SetColor("_OutlineColor", config.HighlightColor);
        material.SetFloat("_Thickness", config.HighlightThickness);

        UnityEditor.EditorUtility.SetDirty(material);
    }

    private static void FixWinPopupBackground()
    {
        UnityEngine.GameObject prefab =
            UnityEditor.AssetDatabase.LoadAssetAtPath<UnityEngine.GameObject>(WinPopupPrefabPath);

        if (prefab == null)
        {
            throw new System.InvalidOperationException(
                $"WinPopup prefab not found at '{WinPopupPrefabPath}'.");
        }

        Roulette.RouletteWinPopup popup = prefab.GetComponent<Roulette.RouletteWinPopup>();

        if (popup == null)
        {
            throw new System.InvalidOperationException(
                "WinPopup prefab root has no RouletteWinPopup component.");
        }

        UnityEngine.UI.Image background = prefab.transform.Find("Window")?
            .GetComponent<UnityEngine.UI.Image>();

        if (background == null)
        {
            throw new System.InvalidOperationException(
                "WinPopup prefab has no Image on its Window object.");
        }

        UnityEditor.SerializedObject serialized = new UnityEditor.SerializedObject(popup);
        serialized.FindProperty("_background").objectReferenceValue = background;
        serialized.FindProperty("_coinsBackgroundColor").colorValue = CoinsBackgroundColor;
        serialized.ApplyModifiedPropertiesWithoutUndo();

        UnityEditor.EditorUtility.SetDirty(prefab);
    }

    private static void FixMenuRoulette()
    {
        UnityEditor.SceneManagement.EditorSceneManager.OpenScene(
            MenuScenePath, UnityEditor.SceneManagement.OpenSceneMode.Single);

        UnityEngine.GameObject screen = FindSceneRoot("DailyRouletteScreen");

        if (screen == null)
        {
            throw new System.InvalidOperationException(
                "Menu scene has no DailyRouletteScreen root object.");
        }

        Roulette.RouletteView rouletteView = screen.GetComponentInChildren<Roulette.RouletteView>(true);

        if (rouletteView == null)
        {
            throw new System.InvalidOperationException(
                "DailyRouletteScreen has no RouletteView inside.");
        }

        TMPro.TMP_FontAsset font = LoadFont();

        UnityEngine.UI.Button spinButton = EnsureShopStyleButton(
            screen.transform, "StartMoney", SpinButtonPosition, SpinButtonColor, font);
        UnityEngine.UI.Button adButton = EnsureShopStyleButton(
            screen.transform, "StartAD", AdButtonPosition, AdButtonColor, font);

        EnsureClickSound(spinButton.gameObject);
        EnsureClickSound(adButton.gameObject);

        TMPro.TMP_Text spinText = spinButton.GetComponentInChildren<TMPro.TMP_Text>(true);
        TMPro.TMP_Text adText = adButton.GetComponentInChildren<TMPro.TMP_Text>(true);

        if (spinText == null || adText == null)
        {
            throw new System.InvalidOperationException(
                "Menu scene: StartMoney/StartAD lost their TMP text children — restore them before wiring.");
        }

        UnityEditor.SerializedObject serialized = new UnityEditor.SerializedObject(rouletteView);
        serialized.FindProperty("_spinButton").objectReferenceValue = spinButton;
        serialized.FindProperty("_spinPriceText").objectReferenceValue = spinText;
        serialized.FindProperty("_adButton").objectReferenceValue = adButton;
        serialized.FindProperty("_adButtonText").objectReferenceValue = adText;
        serialized.ApplyModifiedPropertiesWithoutUndo();

        UnityEditor.EditorUtility.SetDirty(rouletteView);
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
            UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());
        UnityEditor.SceneManagement.EditorSceneManager.SaveScene(
            UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());
    }

    // Кнопка в стиле владельца из RoulettePage магазина: UISprite, цвет, размер,
    // дочерний TMP-текст на весь прямоугольник (текст — отдельный объект,
    // Image и TMP на одном объекте делят один CanvasRenderer). Позиция как в шопе.
    private static UnityEngine.UI.Button EnsureShopStyleButton(
        UnityEngine.Transform parent, string name, UnityEngine.Vector2 position,
        UnityEngine.Color color, TMPro.TMP_FontAsset font)
    {
        UnityEngine.Transform existing = parent.Find(name);

        if (existing != null)
        {
            UnityEngine.UI.Button existingButton = existing.GetComponent<UnityEngine.UI.Button>();

            if (existingButton == null)
            {
                throw new System.InvalidOperationException(
                    $"Menu scene: '{name}' has no Button component.");
            }

            return existingButton;
        }

        UnityEngine.GameObject buttonObject = new UnityEngine.GameObject(
            name, typeof(UnityEngine.RectTransform), typeof(UnityEngine.UI.Image),
            typeof(UnityEngine.UI.Button));
        buttonObject.transform.SetParent(parent, false);

        UnityEngine.UI.Image image = buttonObject.GetComponent<UnityEngine.UI.Image>();
        image.sprite = UnityEditor.AssetDatabase.GetBuiltinExtraResource<UnityEngine.Sprite>(
            "UI/Skin/UISprite.psd");
        image.color = color;

        UnityEngine.RectTransform rect = (UnityEngine.RectTransform)buttonObject.transform;
        rect.anchorMin = new UnityEngine.Vector2(0.5f, 0.5f);
        rect.anchorMax = new UnityEngine.Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = SpinButtonSize;

        UnityEngine.GameObject labelObject = new UnityEngine.GameObject(
            "Text (TMP)", typeof(UnityEngine.RectTransform), typeof(TMPro.TextMeshProUGUI));
        labelObject.transform.SetParent(buttonObject.transform, false);

        TMPro.TextMeshProUGUI text = labelObject.GetComponent<TMPro.TextMeshProUGUI>();
        text.font = font;
        text.fontSize = 72f;
        text.alignment = TMPro.TextAlignmentOptions.Center;
        text.text = name == "StartMoney" ? "100" : "AD";
        text.color = UnityEngine.Color.white;
        text.raycastTarget = false;
        text.rectTransform.anchorMin = UnityEngine.Vector2.zero;
        text.rectTransform.anchorMax = UnityEngine.Vector2.one;
        text.rectTransform.sizeDelta = UnityEngine.Vector2.zero;

        return buttonObject.GetComponent<UnityEngine.UI.Button>();
    }

    private static UnityEngine.GameObject FindSceneRoot(string name)
    {
        UnityEngine.GameObject[] roots =
            UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects();

        for (int i = 0; i < roots.Length; i++)
        {
            if (roots[i].name == name)
            {
                return roots[i];
            }
        }

        return null;
    }

    private static TMPro.TMP_FontAsset LoadFont()
    {
        TMPro.TMP_FontAsset font = UnityEditor.AssetDatabase.LoadAssetAtPath<TMPro.TMP_FontAsset>(FontPath);

        if (font == null)
        {
            throw new System.InvalidOperationException("LiberationSans SDF font asset not found.");
        }

        return font;
    }

    private static void EnsureClickSound(UnityEngine.GameObject buttonObject)
    {
        if (buttonObject.GetComponent<Audio.UIButtonSound>() != null)
        {
            return;
        }

        Scriptables.SfxClip clip = UnityEditor.AssetDatabase.LoadAssetAtPath<Scriptables.SfxClip>(
            UiClickClipPath);

        if (clip == null)
        {
            throw new System.InvalidOperationException("UI Click SfxClip asset not found.");
        }

        Audio.UIButtonSound sound = buttonObject.AddComponent<Audio.UIButtonSound>();
        UnityEditor.SerializedObject serialized = new UnityEditor.SerializedObject(sound);
        serialized.FindProperty("_sfxClip").objectReferenceValue = clip;
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }
}
