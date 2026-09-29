# ProjectLifetimeScope

**Слой:** DI · **Код:** `Assets/MadSlime/DI/ProjectLifetimeScope.cs` · префаб `Assets/MadSlime/Resources/Prefabs/DI/ProjectScope.prefab`

Корневой скоуп VContainer. В сценах его нет: на префаб указывает `RootLifetimeScope` в `VContainerSettings.asset`, поэтому он автоспавнится из Resources в каждой сцене и живёт в DontDestroyOnLoad. Родитель всех [[Скоупы сцен | сценических скоупов]].

## Зачем

Единый составной корень: кросс-сценовые синглтоны (прогресс, директор, пул), конфиги-каталоги и платформенные адаптеры.

## Что регистрирует

- **Компоненты** (Scoped, ссылки на префабе, 9 fail-fast проверок): [[PlayerProgress]], PlayerUpgrades, [[LocalizationService]], [[Аудио-подсистема | SfxPlayer / MusicPlayer / AudioMixerController]]
- **SO-ассеты** (RegisterInstance): LevelsCatalog (см. [[Конфиги уровней]]), [[TierTable]], LayoutsLibrary
- **Синглтоны-классы**: [[LevelProgress]], [[LevelConfigResolver]], [[ItemPool]], [[GameDirector]]
- **Шов платформы** (`ProjectLifetimeScope.cs:95-99`): `ISavesAccess → new Yg2SavesAccess()`, `IAdsService → new Yg2AdsService()`, `ILeaderboardService → new Yg2LeaderboardService()`, `ILanguageProvider → new Yg2LanguageProvider()`, `IGameplayReporter → new Yg2GameplayReporter()` — см. [[Интерфейсы Core (шов YG2)]] и [[YG2-адаптеры]]
- **EntryPoint:** [[SessionStateLogger]]

## Слабые места

- `new Yg2...()` зашиты в коде конфигуратора — смена платформы = правка DI-кода.
- Сценические скоупы цепляются к нему неявно (parentReference в сценах пуст) — связь существует только на уровне VContainer-конвенций.
- Скоуп держит 9 сериализованных ссылок на всё сразу — правка префаба ProjectScope бьёт по всем сценам.
