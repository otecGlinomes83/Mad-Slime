# UI-фабрики

**Слой:** UI · **Код:** `Assets/MadSlime/Scripts/UI/Spawners/`

Единственная точка, где окна и HUD-префабы инстансятся с DI (`IObjectResolver.Instantiate`).

## GameplayUIFabric (сцена Game)

- Инжект: `resolver`, [[GameplaySessionHandler]]
- `_pauseButton` → спавн PauseMenu с `menuAction: _sessionHandler.ExitToMenu`

## FillUIFabric (сцена Fill) — фабрика исходов

- Инжект: `resolver`, [[FillSessionHandler]], [[Wallet]], [[AdScheduler]]
- `OnEnable` подписки: `_sessionHandler.Win` / `Failed` (`Action<int>` — сумма награды)
- **Победа** → WinMenu(reward, LoadNextLevel, `RequestDoubleReward`, ExitToMenuAfterWin). Двойная награда: `AdScheduler.ShowDoubleReward` → по granted `Wallet.Add(lastReward)`
- **Провал** → FailMenu(RequestFillRescue, `CanRescueFill`, RestartLevel, ExitToMenu). Спасение: rewarded с `YandexConfig.FillRescueRewardId` → granted: `FailMenu.Dismiss()` + `FillSessionHandler.RescueFill()`; rejected: кнопка возвращается
- Плюс pause-кнопка как в Game

## Shop-фабрики

- **ShopItemViewFactory** / **UpgradeItemViewFactory** (`UI/Shop/`): `Get(...)` = `resolver.Instantiate(prefab, parent)` + `Initialize(...)`; у апгрейд-фабрики два перегруза (UpgradeType / PerkType)

## Связи

- [[Окна]], [[GameplaySessionHandler]], [[FillSessionHandler]], [[Wallet]], [[AdScheduler]], [[ShopPanel]]

## Слабые места

- `FillUIFabric._activeFailMenu` не обнуляется при рестарте/выходе — ссылка висит на уничтоженное окно до следующего Fail (`FillUIFabric.cs:107-114`); при спасении после рестарта возможен `Dismiss()` по stale-ссылке (защищено null-проверкой, но не «свежестью»).
- Дублирование «Instantiate + Initialize» между фабриками и MainMenu.
