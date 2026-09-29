# Startup

**Слой:** Gameplay · **Код:** `Assets/MadSlime/Scripts/Game/Startup.cs` · сцена Menu (GO на execution order −100)

Первая точка входа игры. Живёт только в Menu-сцене, инжектится через [[Скоупы сцен | MenuLifetimeScope]].

## Что делает

1. `[Inject] Construct(GameDirector)` — тянет синглтон из корня ([[ProjectLifetimeScope]])
2. `Awake`: GameDirector не заинжектился → `InvalidOperationException` (fail-fast)
3. `_gameDirector.EnsureInitialized()` — определяет текущий `SceneId` по фактически открытой сцене (Menu/Game/Fill/Shop, иначе исключение)
4. `Time.timeScale = 1` — гарантия размороженного времени на старте

## Зачем

Директор должен существовать и знать, где мы, до того как любой UI нажмётся. Порядок безопасен: VContainer строит контейнеры при `autoRun`, Awake сценических объектов идёт после инжекции.

## Связи

- Инициирует: [[GameDirector]]
- Смена сцен дальше: [[MainMenu]] → `GameDirector.LoadAsync(Game)`

## Слабые места

- Двойная неявная зависимость: должен быть зарегистрирован и в MenuLifetimeScope (инжекция), и получить GameDirector из ProjectLifetimeScope — связь выражена только текстом ошибки.
