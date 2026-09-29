# PlayerProgress

**Слой:** Gameplay · **Код:** `Assets/MadSlime/Scripts/Game/PlayerProgress.cs` (MonoBehaviour, компонент префаба [[ProjectLifetimeScope]])

Фасад геймплея над `ISavesAccess` ([[Интерфейсы Core (шов YG2)]]): сам ничего не хранит, всё пробрасывает в адаптер. Единственная точка доступа к сейву для Gameplay/UI.

## Что хранится

`CurrentLevel` / `MaxLevel` · `Balance` · `Language` · `MusicVolume` / `SfxVolume` · `SelectedSkin` / `OpenSkins` · уровни апгрейдов (`GetUpgradeLevel/SetUpgradeLevel` → Speed/Appetite/Taste/Metabolism) · `PurchasedPerks` · рулетка: `LastFreeSpinUnixTime`, `RouletteAdSpinTimes`, `SkinSpinCount` · `IsReady` + `event Ready` (SDK-данные догрузились)

## Ключевое правило

**PlayerProgress никогда не сохраняет сам** — `Save()` вызывают только потребители в явных местах:

| Место | Когда |
|---|---|
| [[Wallet]].Add/Spend | каждая монетная операция |
| [[PlayerUpgrades]].PurchaseStepped/Perk | покупка |
| [[FillSessionHandler]] | ап уровня, выходы |
| [[LocalizationService]] | **только ручная** смена языка |
| AudioMixerController ([[Аудио-подсистема]]) | дебаунс 500 мс + флеш в OnDestroy |
| [[RouletteService]] | фри/ад/платный спин, выдача скина |
| [[ShopPanel]] | выбор скина |

## Слабые места

- Контракт «кто вызывает Save» держится на дисциплине: новый потребитель, забывший Save, даст тихую потерю прогресса.
- `_saves` не валидируется в Construct (`PlayerProgress.cs:143-147`) — единственный крупный класс без fail-fast на инжекте.
- До `Ready` все поля — дефолты [[SavesYG]]; UI, которому нужен прогресс, обязаны ждать `Ready` ([[ShopPanel]], [[SkinApplier]], LeaderboardMenu — делают).
