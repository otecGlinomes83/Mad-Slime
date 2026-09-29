# Timer

**Слой:** Gameplay · **Код:** `Assets/MadSlime/Scripts/Game/Timer.cs`

Универсальный таймер раунда на UniTask.

## API и механика

- `Setup(duration)` — только > 0, иначе исключение; `StartCount()`, `Stop()` (отмена через CTS), `Continue()` (сейчас вне использования)
- `RunTimerAsync`: `UniTask.Yield(Update)` в цикле; **тик только при `Time.timeScale > 0`** — под паузой время не течёт и `Ticked` не стреляет
- События: `Ticked(float remaining)` — каждый кадр; `Finished` — один раз при 0
- CTS линкуется с `GetCancellationTokenOnDestroy()` — смерть объекта останавливает счёт

## Связи

- Хозяин: [[GameplaySessionHandler]] (Setup/StartCount/Stop/Finished)
- Слушатели `Ticked`: [[HUD | TimerUI]], TimerTickSound ([[Аудио-подсистема]]), [[AdrenalineBoost]]; `Finished`: handler + звук + AdrenalineBoost

## Слабые места

- `Ticked` стреляет каждый кадр — три подписчика на каждый кадр раунда; для WebGL приемлемо, но это осознанный cost.
- `Continue()` — мёртвый API (YAGNI-нарушение в обратную сторону: код без потребителя).
