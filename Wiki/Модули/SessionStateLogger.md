# SessionStateLogger

**Слой:** Gameplay · **Код:** `Assets/MadSlime/Scripts/Game/SessionStateLogger.cs`

Единственный VContainer-EntryPoint проекта (`IStartable`, Singleton, регистрируется в [[ProjectLifetimeScope]]).

## Что делает

- При старте логирует `[Session] session start | Level=… Balance=… | Quota N/M Fill=…` (конструктор-инжекция [[PlayerProgress]] + [[LevelProgress]])
- Подписывается на `SceneManager.sceneLoaded` и логирует каждую загрузку сцены с текущим `timeScale`

## Зачем

Диагностика сессий по логам WebGL-билда: сразу видно, на каком уровне, с каким балансом и не завис ли timeScale после перехода.

## Связи

- Наблюдатель: [[PlayerProgress]], [[LevelProgress]], [[GameDirector]]

## Слабые места

- Подписка на `sceneLoaded` без отписки (`SessionStateLogger.cs:24`) — толерантно (живёт всю сессию), но хрупко при реструктуризации.
