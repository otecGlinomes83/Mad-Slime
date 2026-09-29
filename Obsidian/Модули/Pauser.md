# Pauser

**Слой:** Gameplay · **Код:** `Assets/MadSlime/Scripts/Game/Pauser.cs`

Счётчиковая пауза на `Time.timeScale`. Регистрируется **в каждой сцене свой** (счётчик обнуляется сменой сцены).

## Как работает

- `RequestPause()` → `_pauseRequestCount++`, `timeScale = 0`
- `RequestResume()` → декремент (не ниже 0); `timeScale = 1` **только если счётчик обнулился И `IAdsService.IsPauseGame == false`**

Вторая половина условия — единственная точка сопряжения геймплея и рекламы по времени: пока Яндекс показывает рекламу (плагин сам ставит `isPauseGame`), игра не разморозится, сколько бы окон ни закрылось.

## Кто ставит/снимает

- [[GameplaySessionHandler]]: пауза на старте раунда (до первого ввода), resume в `Begin`
- [[Окна | BaseWindow]]: **любое окно** — pause на `Initialize`, resume на `OnDisable`
- [[FillSessionHandler]] — через окна

## Слабые места

- Гашение/восстановление timeScale размазано по трём местам (Pauser, GameDirector-гард, Startup) — рассинхрон даёт «вечную паузу» (см. [[GameDirector]]).
- Пауза только через timeScale: анимации на `SetUpdate(true)` и Realtime-задержки продолжают жить под паузой — осознанный приём, но каждый новый tween надо не забыть про unscaled time.
