# HUD

**Слой:** UI · **Код:** `Assets/MadSlime/Scripts/UI/HUD/` (+ `Common/ValueView.cs`, `IntValueView.cs`)

Набор мелких Mono-view: каждое подписано на один источник данных и только рисует.

## Состав

| Вью | Источник | Что показывает |
|---|---|---|
| **TimerUI** | `Timer.Ticked` (`Action<float>`) | остаток секунд `«37.5»` |
| **QuotaUI** + **QuotaPlateUI** | `LevelProgress.QuotaChanged` (`Action<int, QuotaEntry>`) | тарелки квоты: иконка + «осталось»; intro-каскад, pop при тике, сдвиг/удаление плашек; все твины `SetUpdate(true)` — живут под паузой |
| **GrowthBarView** | `PlayerTier.MassChanged`, `TierChanged` | прогресс массы до следующего тира (`TierResolver.GetTierProgress`) + локализованное имя тира `tier_{tier}` |
| **LevelLabelUI** | `PlayerProgress.Ready`, `Localization.LanguageChanged` | «Уровень N» (`level_label`) |
| **FillProgressUI** | `ShapeFiller.CubeArrived`, `FillCompleted` | % заливки; DOPunchScale на вехах 25/50/75/100 |
| **ValueView<T>/IntValueView** | вручную | универсальный «текст + Show/Hide» для цен |

Все — method injection (`Construct`), подписки строго в `OnEnable`/`OnDisable`, твины убиваются в `OnDisable`/`OnDestroy`.

## Связи

- Источники: [[Timer]], [[LevelProgress]], [[PlayerTier]], [[LocalizationService | Localization]], [[ShapeFillOrchestrator | ShapeFiller]]
- Спавн: сцены собирают HUD через префабы; Game-сцена инжектится через [[Скоупы сцен | GameLifetimeScope]]

## Слабые места

- QuotaUI перестраивает позиции плашек вручную (Y-офсеты по `_verticalSpacing`) — хрупко к смене лейаута.
- `GrowthBarView` твинает `fillAmount` DOTween'ом на каждый `MassChanged` — при шквале пикапов твин перезапускается (твин убивается только в OnDisable).
