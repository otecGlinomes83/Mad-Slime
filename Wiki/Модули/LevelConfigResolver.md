# LevelConfigResolver

**Слой:** Gameplay · **Код:** `Assets/MadSlime/Scripts/Game/Level/LevelConfigResolver.cs` (plain-класс, синглтон [[ProjectLifetimeScope]])

Резолвит `LevelConfig` для `PlayerProgress.CurrentLevel` через [[Конфиги уровней | LevelsCatalog]] (список диапазонов «от уровня X до Y → конфиг»).

## Зачем

Три потребителя на трёх сценах получают один и тот же конфиг: [[GameplaySessionHandler]] (длительность таймера), [[LevelGenerator]] (контент), [[FillSessionHandler]] (тема формы). Смена уровня — только через сейв, конфиг всегда согласован.

## Слабые места

- Уровень за пределами всех диапазонов каталога → исключение (fail-fast; новый контент = правка ассета LevelsCatalog, иначе игра падает на переходе).
