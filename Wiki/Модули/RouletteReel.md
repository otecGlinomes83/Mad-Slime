# RouletteReel

**Слой:** UI · **Код:** `Assets/MadSlime/Scripts/UI/Roulette/RouletteReel.cs` (339 строк)

Вертикальная лента-слот из 5 переиспользуемых карточек (видимых 3, карточек 3+2 за границами). Анимация целиком на DOTween + UniTask idle-петля.

## Устройство

- Позиция `_position` — float в «карточках»; карточки визуально едут вниз, `Reposition()` маппит `FloorToInt(_position)-1 … +3` на индексы энтрий через `Mod` и реинициализирует карточку при смене индекса (пул без инстансиаций в спине)
- Idle: бесконечная UniTask-петля (`Delay(IdleStepInterval, DelayType.Realtime, destroyToken)`) — раз в 1.1 с лента «перещёлкивает» карточку `DOTween.To(..., Ease.OutBack)`; шаг только если built, не крутится и active
- **Spin (откат → разгон → торможение):**
  1. `DOTween.Kill(this)` гасит idle; звук старта
  2. `turns = Random 3..5`; `windBack = pos + 0.6`; `delta = Mod(windBack − target, entryTotal)`; `target = windBack − (turns × entryTotal + delta)` — финал гарантированно ≡ targetIndex (mod entryTotal)
  3. Sequence с `SetTarget(this)` + `SetLink(KillOnDisable)`: твин отката (InOutQuad, 0.3 с) → твин разгона (кастомный ease `1−(1−t)^5`, 3.2 с)
- Тик звука на каждой новой целой позиции, но не чаще 0.06 с (`MinTickIntervalSeconds`)
- `OnSpinCompleted`: нормализация позиции к целевому индексу, `_isSpinning = false`, вызов колбэка награды

## Связи

- [[RouletteView]] (хозяин: `Setup(config, sfxPlayer)` → `Build(views)` → `Spin(target, count, onComplete)`), [[Конфиги скинов и рулетки | RouletteConfig]] (все motion-параметры и звуки), карточки — [[RouletteView | RouletteSectorCard]]

## Слабые места

- **Гонка disable-во-время-спина:** `OnDisable` (`RouletteReel.cs:310-313`) гасит твин и сбрасывает `_isSpinning`, но `_spinCompleted` не вызывается и не обнуляется. Игрок уже заплатил/потратил фри-спин — награда не выпадет; при следующем `Spin` старый колбэк молча затирается (`:116`).
- `_cardHeight` считается один раз в `EnsureCards` (`:147`) — изменение размера вьюпорта после первого Build не подхватывается.
- `LoopNormalizationTurns = 64`, `MinTickIntervalSeconds = 0.06` — магические константы.
