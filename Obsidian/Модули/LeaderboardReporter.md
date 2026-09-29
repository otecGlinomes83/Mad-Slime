# LeaderboardReporter

**Слой:** Gameplay · **Код:** `Assets/MadSlime/Scripts/Game/LeaderboardReporter.cs` · сцена Fill

Запись очков в Яндекс-лидерборд.

## Механика

- `Report(score)`: не авторизован → только Debug.Log, игроку ничего; авторизован → `ILeaderboardService.SetScore(YandexConfig.LeaderboardName = "max_level", score)`
- Вызывается **только** из [[FillSessionHandler]]: `LoadNextLevel` и `ExitToMenuAfterWin`, со значением `_progress.MaxLevel`

Таблица топ-10 рендерит [[Окна | LeaderboardMenu]] (через `RequestEntries` + `EntriesReceived`).

## Связи

- [[Интерфейсы Core (шов YG2) | ILeaderboardService]] → [[YG2-адаптеры | Yg2LeaderboardService]], [[PlayerProgress]], [[Мелкие конфиги Core | YandexConfig]]

## Слабые места

- Неавторизованный игрок молча не попадает в таблицу — UI не предлагает авторизацию в момент первого результата (диалог есть только в LeaderboardMenu).
- Репорт только на победных выходах: победил → сразу вышел из приложения до интерстишла? — точек записи две, обе после Save, приемлемо.
