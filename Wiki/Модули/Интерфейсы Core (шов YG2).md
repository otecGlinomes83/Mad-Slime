# Интерфейсы Core (шов YG2)

**Слой:** Core · **Код:** `Assets/MadSlime/Scripts/Core/Interfaces/`

Единственное место, где игра касается Яндекс-платформы абстрактно. Пять интерфейсов + два DTO. Код `Gameplay`/`UI` YG2 не видит вообще — прямые `using YG` вне `Adapters/` и `Saves/` считаются ошибкой ревью.

## Зачем

Изоляция платформы: asmdef'ы не могут ссылаться на Assembly-CSharp, где живёт YG2-плагин. Игра говорит с платформой только через эти контракты, реализации подменяются в одном месте — [[ProjectLifetimeScope]].

## Состав

| Интерфейс | Отвечает за | Ключевые члены |
|---|---|---|
| `ISavesAccess` | сейв | `Balance`, `CurrentLevel`/`MaxLevel`, `Language`, `MusicVolume`/`SfxVolume`, `SelectedSkinType`, `OpenSkins`, `SpeedLevel`/`AppetiteLevel`/`TasteLevel`/`MetabolismLevel`, `PurchasedPerks`, `LastFreeSpinUnixTime`, `RouletteAdSpinTimes`, `SkinSpinCount`, `Save()`, `IsReady` + `event Ready` |
| `IAdsService` | реклама | `ShowRewarded(id)`, `ShowInterstitial()`, `IsAdShowing`, `IsPauseGame`, события `RewardedOpened/RewardReceived/RewardedClosed/RewardedError` |
| `ILeaderboardService` | лидерборд + авторизация | `SetScore`, `RequestEntries`, `OpenAuthDialog`, `IsAuthorized`, `event EntriesReceived` |
| `ILanguageProvider` | язык платформы | `Language`, `event LanguageSwitched` |
| `IGameplayReporter` | аналитика | `ReportStart()`, `ReportStop()` |

DTO: `LeaderboardEntryData` (struct: Id, Name, Rank, Score), `LeaderboardSnapshot` (TechnoName, Players[], CurrentPlayer).

## Связи

- **Реализации:** [[YG2-адаптеры]] (`Yg2SavesAccess`, `Yg2AdsService`, `Yg2LeaderboardService`, `Yg2LanguageProvider`, `Yg2GameplayReporter`), регистрируются в [[ProjectLifetimeScope]]
- **Потребители:** [[PlayerProgress]] (ISavesAccess), [[AdScheduler]] и [[Pauser]] (IAdsService), [[LeaderboardReporter]] и [[Окна]] (ILeaderboardService), [[LocalizationService]] (ILanguageProvider), [[GameplaySessionHandler]] (IGameplayReporter)

## Слабые места

- `ISavesAccess` — «божественный» контракт: баланс, уровни, скины, апгрейды, рулетка, звук, язык в одном интерфейсе. Любой потребитель видит всё лишнее; рост числа сейв-полей раздувает его дальше.
- Реализации жёстко создаются через `new Yg2...()` в `ProjectLifetimeScope.cs:95-99` — смена платформы требует правки кода, а не конфигурации.
- `ILeaderboardService` совмещает два Verantwortlichkeit: данные лидерборда и диалог авторизации.
