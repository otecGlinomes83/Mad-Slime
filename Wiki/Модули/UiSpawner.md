# UiSpawner

**Слой:** UI · **Код:** `Assets/MadSlime/Scripts/UI/Spawners/`

Единая точка показа UI — аналог [[Аудио-подсистема | SfxPlayer]]: сервис, инжектится всем, вызывающий держит только префаб или поле-ссылку.

## API

```csharp
T Spawn<T>(T prefab, UiLayer layer, Action onClosed = null) where T : Component, IShowable
// резолвер.Instantiate(prefab) → корень сцены (инвариант GameDirector) → ApplyLayer → Show() → релей onClosed

T Show<T>(T showable, UiLayer layer, Action onClosed = null) where T : Component, IShowable
// то же без инстанцирования — для существующих объектов сцен с канвасом

void Show(IShowable showable)
// показать без переназначения слоя — хореография HUD по статично нормированным канвасам

void Hide(IShowable showable)
```

- `UiLayer` — полосы sortingOrder: `Hud = 0`, `Buttons = 10`, `Popup = 20`. Пишется в корневой Canvas (fail-fast, если канваса нет) с `overrideSorting = true`. Порядок попапов внутри полосы — порядком создания.
- `IShowable` — контракт: `Show()` / `Hide()` / `event Action Closed`. Для транзиентных окон `Hide()` = закрыть и уничтожить (устоявшийся lifecycle).
- `onClosed` — релей `ClosedRelay` (private nested): подписан именованным методом, отписывается при срабатывании. Незакрытые окна собираются GC вместе с инстансом.
- Регистрация: `builder.Register<UiSpawner>(Lifetime.Scoped)` в каждом сценическом скоупе — окнам нужен сценический [[Pauser]], поэтому корневой резолвер не годится.
- Спавнер никогда не трогает паузу — пауза остаётся решением окна ([[Окна]]).

## Показатели статичного HUD

Канвасы сцен нормируются по полосам генератором (`MadSlimeContentSetup.ApplyUiLayers`, запуск идемпотентен): `JoystickCanvas`/`GrothBarCanvas`/`QuotaCanvas`/`GameTimerCanvas`/`LevelLabelUI`/`FillProgress`/`FillTapZone`/`MenuCanvas`/`ShopCanvas` → Hud; `PauseButtonCanvas` → Buttons; `DailyRouletteScreen` → Popup. Фон меню (`Background = -1`) намеренно ниже полос. Рантайм-переназначение слоёв HUD не делает — только [[HUD | UiEnableScheduler]]-хореография через `Show(IShowable)`.

## Кто что показывает

| Показ | Чем |
|---|---|
| PauseMenu (Game/Fill/настройки в Menu), WinMenu, FailMenu, LeaderboardMenu | `Spawn(prefab, Popup)` из [[UI-фабрики]] и [[MainMenu]] |
| Попап приза рулетки | `Spawn(_winPopupPrefab, Popup, OnWinPopupClosed)` из [[RouletteView]] — отдельный префаб `Prefabs/UI/WinPopup.prefab`, destroy-on-close |
| Экран ежедневной рулетки | `Show(_dailyRoulette, Popup)` из [[MainMenu]] |
| HUD Game (рост-бар, квота, таймер, кнопка паузы) | `Show(animation)` из [[HUD | UiEnableScheduler]] |

## Связи

- [[Окна]], [[UI-фабрики]], [[MainMenu]], [[RouletteView]], [[HUD]]
