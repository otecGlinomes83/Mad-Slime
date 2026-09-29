# ShapeFillOrchestrator

**Слой:** Gameplay · **Код:** `Assets/MadSlime/Scripts/ShapeFill/ShapeFillOrchestrator.cs`, `FillCounter.cs`

Фасад стадии заливки: собирает [[Заливка фигуры | GridBuilder/ShapeFiller]] и переводит результат раунда в число кубов.

## StartFill()

1. `ShapeFiller.Initialize()` + `BuildShape()` (растр текстуры → сетка + призрак + бордюр)
2. `FillCounter.CalculateQuotaFill(maxCubes)` = round(квотный% × maxCubes) — квотный% из [[LevelProgress]] **прошедшего** раунда (`CollectedQuotaCount / TotalQuotaTarget`)
3. `CalculateBonusFill` = round(общий FillPercent × maxCubes) − квотные — «посторонние» предметы конвертируются в бонусные кубы с тинтом
4. `ShapeFiller.Fill(quota, bonus)`

`Rescue()` (спасение за рекламу) — прокси в ShapeFiller; `CanRescue` — для [[FillSessionHandler]]/FailMenu. `FillCompleted(percent)` транслируется наверх.

## Связи

- Вверх: [[FillSessionHandler]] (награда/исходы), [[HUD | FillProgressUI]], FillFinale ([[Заливка фигуры]])
- Вниз: [[Заливка фигуры]], [[LevelProgress]] (мост Game→Fill)

## Слабые места

- Зависимость от «прошлого» состояния project-скоуп [[LevelProgress]] — если когда-нибудь появится вход в Fill мимо Game, счётчики будут нулевыми/мусорными (сейчас защищено только флоу).
