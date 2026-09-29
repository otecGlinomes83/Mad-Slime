# GameDirector

**Слой:** Gameplay · **Код:** `Assets/MadSlime/Scripts/Game/GameDirector.cs` (plain-класс, синглтон [[ProjectLifetimeScope]])

Единственная точка смены сцен. Никто не имеет права дергать `SceneManager.LoadScene` — только `LoadAsync(SceneId)`.

## API

- `EnsureInitialized()` — лениво резолвит `SceneId` по открытым сценам (Menu/Game/Fill/Shop; «NewShop» тоже маппится в Shop). Вызывает [[Startup]] и читается свойствами `CurrentSceneId`/`PreviousSceneId`
- `LoadAsync(SceneId)` — guard'ы: переход уже идёт → throw; target == current → throw
- `IsTransitioning` — его проверяют все вызывающие перед нажатием (MainMenu, ShopCloseButton, оба SessionHandler)

## TransitionAsync — что происходит

1. Гашение уходящей сцены: у всех корней рекурсивно выключаются `AudioListener`, `EventSystem`, `Canvas` — нет двойного звука/ввода и мигания UI
2. timeScale-гард: `Time.timeScale = 1` **только если** `IAdsService.IsPauseGame == false` (во время рекламы время не трогаем)
3. Аддитивная загрузка с `allowSceneActivation = false`, ожидание прогресса до 0.9, активация
4. `SetActiveScene` → выгрузка исходящей → восстановление EventSystem, если он остался только в умершей сцене
5. Обновление `PreviousSceneId` (им пользуется [[ShopPanel | ShopCloseButton]] для возврата)

## Связи

- Вызывают: [[MainMenu]], [[GameplaySessionHandler]], [[FillSessionHandler]], [[ShopPanel | ShopCloseButton]]
- Зависимость: `IAdsService` ([[Интерфейсы Core (шов YG2)]]) — только для timeScale-гарда

## Слабые места

- **Нет отмены:** цикл ожидания активации и `ToUniTask()` без CancellationToken (`GameDirector.cs:121-127`) — разрушение инициатора переход не останавливает.
- **Риск «вечной паузы»:** переход во время рекламной паузы оставляет `timeScale = 0`, а восстанавливает его только `Startup` в Menu (`GameDirector.cs:105-108`) — в Game/Fill размораживать некому.
- Повторный `LoadAsync` во время перехода бросает, вызывающие страхуются молчаливым early-return — тихая потеря нажатия.
- Асимметрия маппинга: «NewShop» распознаётся, но `GetSceneName(Shop)` = «Shop» — на сцену с другим именем через директора не перейти (`:198-220` vs `:252-277`).
- До первого перехода `PreviousSceneId == CurrentSceneId` (`:61`) — компенсируется в ShopCloseButton (Shop → Menu).
