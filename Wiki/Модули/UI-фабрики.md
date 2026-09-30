# UI-фабрики

**Слой:** UI · **Код:** `Assets/MadSlime/Scripts/UI/Spawners/`

Спавн окон идёт через [[UiSpawner]]. Фабрики остались точками привязки кнопок и ad-флоу сцены — инстанцирование сами они больше не делают.

## GameplayUIFabric (сцена Game)

- Инжект: [[UiSpawner]], [[GameplaySessionHandler]]
- `_pauseButton` → `_uiSpawner.Spawn(_pauseMenuPrefab, UiLayer.Popup)` + `Initialize(true, menuAction: _sessionHandler.ExitToMenu)`

## FillUIFabric (сцена Fill) — фабрика исходов

- Инжект: [[UiSpawner]], [[FillSessionHandler]], [[Wallet]], [[AdScheduler]]
- `OnEnable` подписки: `_sessionHandler.Win` / `Failed` (`Action<int>` — сумма награды)
- **Победа** → Spawn WinMenu(reward, LoadNextLevel, `RequestDoubleReward`, ExitToMenuAfterWin). Двойная награда: `AdScheduler.ShowDoubleReward` → по granted `Wallet.Add(lastReward)`
- **Провал** → Spawn FailMenu(..., onClosed: `OnFailMenuClosed` → `_activeFailMenu = null`). Спасение: rewarded с `YandexConfig.FillRescueRewardId` → granted: `FailMenu.Dismiss()` + `FillSessionHandler.RescueFill()`; rejected: кнопка возвращается
- Плюс pause-кнопка как в Game

## Shop-фабрики

- **ShopItemViewFactory** / **UpgradeItemViewFactory** (`UI/Shop/`): карточные фабрики, остались на `IObjectResolver.Instantiate(prefab, parent)` + `Initialize(...)` — это элементы грида под управлением [[ShopPanel]], не слойный UI; у апгрейд-фабрики два перегруза (UpgradeType / PerkType)

## Связи

- [[UiSpawner]], [[Окна]], [[GameplaySessionHandler]], [[FillSessionHandler]], [[Wallet]], [[AdScheduler]], [[ShopPanel]]
