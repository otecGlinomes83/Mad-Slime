# Rewarder

**Слой:** Gameplay · **Код:** `Assets/MadSlime/Scripts/Game/Rewarder.cs` · сцена Fill

Начисление монет за итог заливки. Единственный потребитель [[RewardConfig]].

## Механика

- `RewardWin(percentage)` — требует `percentage >= 1`; награда **плоская** = `BaseReward` (50): процент заливки на сумму победы не влияет (параметр только валидируется)
- `RewardLose(percentage)` — `0`, если заливка < 25% (`LoseMultiplierThreshold`); иначе `BaseReward / LoseRewardDivisor` (12); `Wallet.Add` только при reward > 0
- **`RewardGranted(amount, isWin)` стреляет всегда**, даже с нулём — чтобы UI показал FailMenu

## Цепочка

[[FillSessionHandler]] (FillCompleted) → Rewarder → [[Wallet]] (Add ⇒ Save) → `RewardGranted` → [[UI-фабрики | FillUIFabric]] → [[Окна | WinMenu/FailMenu]]; удвоение — rewarded через [[AdScheduler]] поверх WinMenu.

## Слабые места

- Плоская награда за победу: прогрессия уровней не увеличивает доход — экономика к эндгейму (растущая цена скинов) не подпитана ростом дохода, см. [[Техдолг и баги]].
- 25%-порог утешительной выплаты — единственный параметр, связывающий провал с деньгами; окно «почти win» (95%+) платит те же 12 монет.
