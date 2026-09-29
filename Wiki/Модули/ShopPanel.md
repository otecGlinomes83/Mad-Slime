# ShopPanel

**Слой:** UI · **Код:** `Assets/MadSlime/Scripts/UI/Shop/ShopPanel.cs` (464 строки) + элементы грида

Магазин: три вкладки-страницы на одном канвасе. Стартовая вкладка — рулетка.

## Вкладки

1. **Улучшения** (`PopulateUpgrades`): грид `UpgradeEntry` (апгрейды) → разделитель перков (`shop_one_time`) → грид перков. Покупка: `Wallet.Spend` + `PlayerUpgrades.PurchaseStepped/PurchasePerk`, затем Refresh всех карточек
2. **Рулетка** (стартовая): страница с встроенным [[RouletteView]] Mode.Skins — скины только за монеты, цена растёт с каждой круткой
3. **Все скины** (`PopulateSkins`): карточки всех скинов ShopContent; открытые — Unlock/Select, эксклюзивные (не покупаются, только выигрыш) — бейдж; клик по открытому: превью в ModelPlacer + `PlayerProgress.SelectedSkin` + `Save()`

## Переключение вкладок

`CanSwitchTab()` = рулетка не крутится (используется и кнопкой закрытия магазина через `IsRouletteSpinning`, см. ниже).

## Элементы

- **ShopItemView**: фон в цвете редкости, замок, бейдж «эксклюзив» (ставит [[MadSlimeContentSetup]]), `Click` — event. `Lock()`/`Unlock()` оба прячут цену — цена на карточке скина мёртвый код
- **UpgradeItemView**: иконка, «ШАГ N/M», цена (формат `1.5k`/`1.2m`), maxed → `shop_max` + неинтерактивна; вся текстовка через ключи локализации (`upgrade_*`, `perk_*`)
- **ModelPlacer**: превью скина — инстанс модели, в `Update` вращение (`unscaledDeltaTime`), подгон ортокамеры по Bounds через 8 углов MeshFilter, `PlayWalk()` — триггер аниматора
- **ShopMusic**: включает трек магазина. **ShopCloseButton**: выход по `GameDirector.PreviousSceneId` (если prev = Shop → Menu), guard на крутящуюся рулетку и `IsTransitioning`

## Связи

- [[Wallet]], [[PlayerUpgrades]], [[PlayerProgress]], [[RouletteService]], [[RouletteView]], [[Конфиги скинов и рулетки | ShopContent]], [[SkinApplier]] (применит выбранный скин в Game)
- Инициализация отложена до `PlayerProgress.Ready` (SDK-данные пришли)

## Слабые места

- **Баг повторной инициализации:** после первого `OnDisable` таб-кнопки отписаны, а `InitializeShop()` при повторном `OnEnable` выходит по `_isInitialized` (`ShopPanel.cs:69-84`) — подписки вкладок не восстанавливаются, вкладки перестают переключаться; баланс при этом живёт (его подписка снята только в OnDestroy).
- `ModelPlacer.Update` крутит превью постоянно, даже когда вкладка со скинами не видна.
- Разброс ответственности: ShopPanel знает и про гринды, и про рулетку, и про превью, и про persist выбора — самый толстый UI-класс.
