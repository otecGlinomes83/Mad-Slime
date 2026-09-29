# SavesYG

**Слой:** Saves (Assembly-CSharp) · **Код:** `Assets/MadSlime/Saves/SavesYG.cs` (partial; вторая часть — в плагине YG2, `SavesYG2.cs`, только `idSave`)

Схема сейва игрока. Сериализация YG2 — JsonUtility: **имена полей = JSON-ключи живых сейвов**. Переименование = потеря прогресса у всех игроков. Доступ из игры — только через `Yg2SavesAccess` ([[YG2-адаптеры]]) → фасад [[PlayerProgress]].

## Поля (JSON-ключи)

| Ключ | Тип | Дефолт | Что хранит |
|---|---|---|---|
| `CurrentLevel` / `MaxLevel` | int | 1 / 1 | текущий и максимальный уровень |
| `Language` | string | "" | выбранный язык |
| `musicVolume` / `sfxVolume` | float | 0.5 / 0.35 | громкость (**ключи с маленькой буквы — легаси**) |
| `Balance` | int | 250 | монеты |
| `SelectedSkinType` | PlayerSkins | Slime | надетый скин |
| `_openSkins` | List<PlayerSkins> | {Slime} | открытые скины (**ключ с подчёркиванием — легаси**) |
| `SpeedLevel`…`MetabolismLevel` | int ×4 | 0 | уровни апгрейдов |
| `PurchasedPerks` | List<PerkType> | {} | купленные перки |
| `LastFreeSpinUnixTime` | long | 0 | фри-спин рулетки |
| `RouletteAdSpinTimes` | List<long> | {} | таймстемпы рекламных спинов |
| `SkinSpinCount` | int | 0 | счётчик круток (растущая цена) |
| `PreviousScene` | string | — | **не используется** (оставлен ради JSON-совместимости) |

## Связи

- Читается: [[YG2-адаптеры | Yg2SavesAccess]] → [[PlayerProgress]]
- Сброс в редакторе: PlayerDataResetTool (правит `SavesEditorYG2.json`)

## Слабые места

- Стилевые исключения (`_openSkins`, `musicVolume`) — намеренно заморожены, трогать нельзя.
- `Save()` no-op до инициализации SDK — на свежей сессии в редакторе прогресс не пишется без предупреждения (см. [[YG2-адаптеры]]).
- Единый blob без версионирования: новая платформа/структура = ручная миграция.
