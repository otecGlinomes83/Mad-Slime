# FillConfig

**Слой:** Core · **Код:** `Assets/MadSlime/Scripts/Core/Scriptables/Fill/FillConfig.cs`

Все параметры сцены заливки (Fill): темп, тап-бусты, бонус-волна, финальные эффекты.

## Группы

- **Fill:** задержка первого куба 0.55 с, спавн каждые 0.04 с, полёт куба 0.5 с
- **Tap Boost** (после первого тапа): спавн 0.015 с, полёт 0.25 с
- **Tap Instant** (после второго тапа): окно залпа 0.25 с, полёт 0.1 с — заливка мгновенным шквалом
- **Bonus Wave:** задержка 0.35 с, тинт-цвет + сила 0.3
- **Border:** каскад рамки 0.5 с
- **Ghost:** прозрачность силуэта формы 0.4
- **Shape Punch:** эластичный пунш формы (сила 0.06, 0.4 с, vibrato 10)
- **Camera Kick:** FOV-кик 9° на 0.6 с
- **Session:** окно победы через 1.3 с после заливки

## Связи

- **Потребители:** [[ShapeFillOrchestrator]], [[Заливка фигуры]] (ShapeFiller, CubeSpawner, FlyingCube, FillTapInput, FillFinale), [[ShapeFillOrchestrator | FillCounter]], [[HUD | FillProgressUI]]
- Регистрация: `RegisterInstance` в [[Скоупы сцен | FillLifetimeScope]]

## Слабые места

- 21 числовой параметр без валидации взаимных зависимостей (например, boosted < обычного интервала) — рассинхрон только по факту на глазах.
