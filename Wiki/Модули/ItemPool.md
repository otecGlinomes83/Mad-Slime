# ItemPool

**Слой:** Gameplay · **Код:** `Assets/MadSlime/Scripts/Game/Level/ItemPool.cs` (plain-класс, синглтон [[ProjectLifetimeScope]])

Пул предметов: `Dictionary<prefab → стек инстансов>` + обратный маппинг `instance → prefab`. Root-объект «PooledItems» с DontDestroyOnLoad.

## Как работает

- `Get(prefab)`: из стека или `Instantiate` под root
- `Release(item)`: `item.Shutdown()` (деактивация) → под root в стек; неизвестный инстанс → `Destroy`
- Живые предметы при спавне пере-родительятся в `_itemsRoot` сцены ([[LevelGenerator]])

## Связи

- [[LevelGenerator]] (Get/Release), [[Item и IAttractable | Item]] (Shutdown)

## Слабые места

- **Утечка словаря:** несобранные предметы умирают вместе со сценой, мёртвые записи `_prefabByInstance` чистятся никогда (`ItemPool.cs:10, 38`) — словарь растёт от уровня к уровню (медленно, но монотонно; WebGL-сессия долгая).
- Несобранные предметы не возвращаются в пул при выгрузке сцены — «пул» работает только для собранных.
