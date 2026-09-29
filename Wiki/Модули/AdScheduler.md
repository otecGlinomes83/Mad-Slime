# AdScheduler

**Слой:** Gameplay · **Код:** `Assets/MadSlime/Scripts/Game/AdScheduler.cs`

Мост к `IAdsService` ([[Интерфейсы Core (шов YG2)]]): превращает сырые события YG2 в надёжный rewarded-флоу. Живёт в сценах Menu/Shop/Fill (см. [[Скоупы сцен]]); id-шники — [[Мелкие конфиги Core | YandexConfig]].

## Rewarded-механика

`ShowRewarded(rewardId, onGranted, onRejected)`:
- реклама уже идёт (`IsAdShowing`) → **сразу onRejected**, молча
- `granted` только если `RewardReceived` пришёл **до** `RewardedClosed`; ошибка показа → onRejected
- `ShowDoubleReward(onGranted)` — обёртка с id `DoubleReward`; `RouletteRewardId` — для [[RouletteView]]

**TryShowInterstitial()** — прямой `ShowInterstitial()` без счётчика, кулдауна и капа. Имя «Scheduler» не соответствует: он ничего не планирует.

## Кто вызывает

- [[FillSessionHandler]]: интерстишл перед каждым уходом из Fill (next/restart/menu/win-menu)
- [[UI-фабрики | FillUIFabric]]: ×2 награды, спасение заливки (`FillRescueRewardId`)
- [[RouletteView]]: рекламные спины (`RouletteRewardId`)

## Слабые места

- **Нет частотного капа интерстишла** (`AdScheduler.cs:55-58`): показ на каждом выходе из Fill, включая частые рестарты — зависит только от внутренних таймаутов YG2-плагина; риск политик Яндекса.
- Окно рассинхрона: pending-колбэки перезаписываются, если UI вызвал второй показ до `RewardedOpened` (`:85-88`) — молчаливая потеря.
- onRejected при занятой рекламе — без лога; диагностика «почему не дали награду» только по коду.
