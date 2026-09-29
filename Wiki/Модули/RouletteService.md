# RouletteService

**Слой:** UI · **Код:** `Assets/MadSlime/Scripts/UI/Roulette/RouletteService.cs` (386 строк)

Безголовая логика обеих рулеток: таймеры, цены, роллы, выдача. Ничего не знает про UI — [[RouletteView]] только дёргает его API.

## API по группам

- **Цены:** `MainSpinCost` (100); `GetSkinSpinCost() = base(300) + step(150) × SkinSpinCount` — растёт монотонно, никогда не сбрасывается; `PayMainSpin` / `PaySkinSpin` (Spend + `SkinSpinCount++` + Save)
- **Фри-спин:** `CanSpinFree(now)` — прошло ≥ 900 с с `LastFreeSpinUnixTime`; `RegisterFreeSpin` пишет время + Save
- **Рекламные спины:** скользящее окно 1800 с, лимит 3 (`PruneAdSpins` выкидывает устаревшие timestamp'ы, `CanSpinForAd`/`GetAdSpinsLeft`/`RegisterAdSpin` — окно полное → throw)
- **Роллы:** `PickMainSectorIndex` — веса секторов конфига; `PickSkinIndex(pool)` — **двухстадийный**: тир по весам `SkinRarityTable` (RollRarity) → равномерный скин тира
- **Выдача:** `GrantCoins` → [[Wallet]]; `GrantSkin` — если не открыт: `OpenSkins.Add` + Save; `CollectAvailableSkinPool` — только неоткрытые; `GetRarityColor`, `SortByRarityAscending` (стабильная, O(4n))

## Валидация

`Awake` — config, инжекты, RarityTable != null, `DropWeight > 0` у всех четырёх редкостей, у секторов: Skin-сектор без скина / Coins-сектор с `Coins <= 0` → throw. Живые экземпляры: Menu (Systems) и Shop — по одному на сцену.

## Связи

- [[Конфиги скинов и рулетки | RouletteConfig + SkinRarityTable]], [[PlayerProgress]] (таймеры/счётчик в сейве), [[Wallet]], [[RouletteView]], [[AdScheduler]]

## Слабые места

- `PickSkinIndex` на пустом пуле бросает `ArgumentException` (`RouletteService.cs:246-252`); в связке с [[RouletteView]] (кнопка спина не гасится при всех собранных скинах) даёт **платный сломанный спин** — критичный баг эндгейма, см. [[Техдолг и баги]].
- Все таймеры (`CanSpinFree`, окна рекламы) считаются по `DateTimeOffset.UtcNow` — **часам устройства**; перевод часов даёт фри-спины (`RouletteView.cs:446-449`). Серверное время YG2 не используется.
- `GetFreeSpinRemainSeconds` кастит `(int)elapsed` (`RouletteService.cs:105`) — unix-секунды переполнят int после 2038 года (низкий риск, но формально есть).
- `RegisterAdSpin` бросает исключение как ветку логики — колбэк рекламы может прийти, когда окно уже заполнилось (см. [[RouletteView]]).
- Вес 0 у сектора в конфиге не запрещён `Awake` (только отрицательный запрещён атрибутом) — сектор-вес-ноль просто никогда не выпадает, молча.
