# YG2-адаптеры

**Слой:** Adapters (Assembly-CSharp) · **Код:** `Assets/MadSlime/Adapters/`

Пять тонких обёрток над статическим `YG2`. Единственные классы (вместе с [[SavesYG]]), которым разрешён `using YG`. Регистрируются в [[ProjectLifetimeScope]] как реализации [[Интерфейсы Core (шов YG2) | интерфейсов Core]].

| Адаптер | Интерфейс | Что делает |
|---|---|---|
| **Yg2SavesAccess** | ISavesAccess | прямые прокси на `YG2.saves.*` + `Save()` → `YG2.SaveProgress()`; `Ready` — транслатор `YG2.onGetSDKData`; `IsReady => YG2.isSDKEnabled` |
| **Yg2AdsService** | IAdsService | ретрансляция `YG2.onOpenRewardedAdv / onRewardAdv / onCloseRewardedAdv / onErrorRewardedAdv` → свои события; `ShowRewarded(id)`, `ShowInterstitial()`; `IsPauseGame => YG2.isPauseGame` (паузу ставит сам плагин, `autoPauseGame`) |
| **Yg2LeaderboardService** | ILeaderboardService | `SetLeaderboard` / `GetLeaderboard` / `OpenAuthDialog`; маппинг `LBData` → `LeaderboardSnapshot`, анонимизация имён через `LBMethods.AnonymousName` |
| **Yg2LanguageProvider** | ILanguageProvider | `YG2.lang` + транслатор `YG2.onSwitchLang` |
| **Yg2GameplayReporter** | IGameplayReporter | `YG2.GameplayStart()` / `GameplayStop()` |

## Зачем

asmdef'ы не ссылаются на Assembly-CSharp, где живёт плагин: игра видит только интерфейсы Core, адаптеры подменяются в одной точке ([[ProjectLifetimeScope]]). Смена платформы = переписать 5 маленьких классов.

## Слабые места

- **Подписки без отписки:** Yg2AdsService (`:21-27`) и Yg2LeaderboardService (`:19-22`) подписываются на статические события YG2 в конструкторе и не имеют Dispose — при пересоздании контейнера обработчики задвоятся.
- `Yg2SavesAccess.Save()` **молча игнорируется**, пока `isSDKEnabled == false` (`:188-196`) — в редакторе прогресс тихо не сохраняется.
- Поля сейва с «кривыми» JSON-ключами торчат наружу: `_openSkins` (с подчёркиванием), `musicVolume`/`sfxVolume` (с маленькой буквы) — см. [[SavesYG]].
