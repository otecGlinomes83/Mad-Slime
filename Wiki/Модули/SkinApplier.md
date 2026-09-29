# SkinApplier

**Слой:** Gameplay · **Код:** `Assets/MadSlime/Scripts/Player/SkinApplier.cs`

Ставит выбранную скин-модель на игрока при загрузке сцены с ним (Game).

## Механика

- `OnEnable`: подписка `PlayerProgress.Ready`; `Start`: сразу применить (для уже загруженных сейвов)
- `ApplySelectedSkin`: `SelectedSkin` → линейный поиск по `ShopContent.SkinItems` по SkinType → `Destroy(_currentModel)` → `Instantiate(item.Model, _skinsContainer)`
- `OnDestroy` убирает модель. Пишут выбор: [[ShopPanel]] и [[RouletteService]] (сразу в сейв)

## Связи

- [[PlayerProgress]], [[Конфиги скинов и рулетки | ShopContent/SkinItem]], [[Player]]

## Слабые места

- Двойной молчаливый early-return (`SkinApplier.cs:68-71, 77-80`): прогресс null или скин не найден → **модели не будет вообще** и ни одного warning. Рассинхрон сейва и ShopContent (удалили скин) даст невидимого игрока.
- Линейный поиск — ок для 5 скинов, но растёт с каталогом.
