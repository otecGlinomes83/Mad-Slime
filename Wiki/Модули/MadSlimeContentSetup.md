# MadSlimeContentSetup

**Слой:** Editor · **Код:** `Assets/MadSlime/Editor/MadSlimeContentSetup.cs` (2221 строка, static, без namespace)

Генератор и починщик контента: конфиги, префабы, сцены, локализация, связки DI. Запуск — **только** `-executeMethod MadSlimeContentSetup.SetupAll` в batchmode (атрибута `[MenuItem]` в файле нет — упоминание меню «Mad Slime/Setup Content» в CLAUDE.md устарело).

## Что делает SetupAll (порядок)

1. Папки под конфиги/материалы/FX
2. **UpgradesConfig**: 4 апгрейда (Speed 200/150, Appetite 250/200, Taste 400/300, Metabolism 350/250 — base/step, 5 ступеней) + перки (Smell 3000, Adrenaline 4000, Ambitions 5000) + иконки из `Resources/Images/Upgrades`
3. **Эксклюзивные скины** Crown/Phantom (копия модели Slime, Price 0), редкости: Slime/Pacman=Common, TripleT=Rare, Crown=Epic, Phantom=Legendary
4. **SkinRarityTable** — пересоздаёт: Common 100, Rare 45, Epic 15, Legendary 4 (веса) + цвета плашек
5. **RouletteConfig** — пересоздаёт секторы: монеты ×50(w20) ×150(w12) ×300(w8) ×600(w4) ×1500(w1.5), Crown(w0.35), Phantom(w0.15); таймеры 1800/3/900; motion ленты
6. Материал подсветки квоты, префаб CollectBurst, префабы RouletteSectorCard/RouletteView (скелет ленты, CenterBand), чинка шрифта TMP
7. **Локализация**: дозапись ключей `rarity_*`, `upgrade_*`, `perk_*`
8. **ProjectScope.prefab**: PlayerUpgrades + линк в ProjectLifetimeScope
9. **Префабы предметов**: `_highlightMaterial`
10. **Сцены**: Game (DeformContainer/AdrenalineBoost на Player, линки в скоуп), Fill (FillDebug → FillProgressUI), Shop (компоненты, RouletteView под RoulettePage, лейаут вкладок, ShopCloseButton, 11 ссылок скоупа), Menu (AdScheduler/RouletteService/Wallet на Systems, DailyButton, DailyRouletteScreen + прокидка в MainMenu, 7 ссылок скоупа)

## Идемпотентность

- «Insert-if-missing» — апгрейды, перки, иконки, локали, скины, компоненты в сценах: повторный запуск безопасен
- **Не идемпотентно к ручным правкам:** секторы рулетки и таблица редкостей стираются `ClearArray()` при каждом прогоне (`MadSlimeContentSetup.cs:282, 325`) — ручные веса живут только до следующего SetupAll
- Префабы карточки/вью рулетки перезаписываются на диск каждый запуск

## Слабые места

- Поиск объектов сцены по именам-строкам (`FindDeep("UpgradesPage")` и т.п.) — переименование объекта ломает сетап молча или throw'ом посреди прогона.
- `AssetDatabase.SaveAssets()` один раз в конце — сбой в середине теряет всё созданное до него.
- Ручные правки весов рулетки/редкостей беззащитны перед повторным прогоном (см. выше) — это надо знать и держать веса в коде сетапа.
