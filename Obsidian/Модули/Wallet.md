# Wallet

**Слой:** Gameplay · **Код:** `Assets/MadSlime/Scripts/Game/Wallet.cs`

Кошелёк — обёртка над `PlayerProgress.Balance` с событием.

## Механика

- `Add(amount)` / `Spend(amount)`: валидация (amount > 0; при Spend — достаточность баланса, иначе исключение) → изменение `_progress.Balance` → **немедленный `_progress.Save()`** → `BalanceChanged(previous, current)`
- Баланс читают напрямую: [[RouletteService]] (CanSpinForCoins), [[ShopPanel]] (показ)

## Кто двигает монеты

- **Add:** [[Rewarder]] (итог заливки), FillUIFabric (rewarded ×2), [[RouletteService]] (выигрыши рулетки, компенсации)
- **Spend:** [[RouletteService]] (обе крутки), [[ShopPanel]] (апгрейды/перки)

## Слабые места

- Save на каждую операцию: крутка скина даёт **две записи подряд** (Spend сохранил, SkinSpinCount++ сохранил ещё раз — `RouletteService.cs:165-178`) — двойная запись localStorage.
- Исключение при нехватке монет как ветка логики — вызывающие обязаны проверять `CanSpin*`/стоимость заранее.
