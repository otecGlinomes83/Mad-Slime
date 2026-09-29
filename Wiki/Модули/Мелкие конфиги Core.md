# Мелкие конфиги Core

**Слой:** Core · **Код:** `Assets/MadSlime/Scripts/Core/Scriptables/{Audio,Camera,Items,Skills,Ads,Localization}/`

## Состав

| Конфиг | Что настраивает |
|---|---|
| **SfxClip** (`Mad Slime/Sfx Clip`) | звуковой эффект: клип + громкость + случайный питч (0.1–3). Дизайнер вешает на `UIButtonSound._sfxClip` и т.п. |
| **GhostFadeConfig** | переход обычный вид ↔ призрак: 0.25 с, ease InOutSine. Читается [[Item и IAttractable \| Item]] |
| **CameraImpulseConfig** | камера-хаптика: кривая «масса съеденного → подтяжка камеры» (0.4→2.5, max 6), отъезд при тир-апе (4, max 8), шейк от тяжёлых пикапов (порог массы 1000, накапливается, max 0.35), FOV-кик на тир-апе 7° |
| **AttractConfig** (`Mad Slime/Attract Config`) | магнит: сила притяжения 6, разгон ×3 у точки захвата (крутизна 2), орбитальность 0.5 (спираль вокруг игрока) |
| **YandexConfig** | id rewarded-блоков: `DoubleReward`, `NextLevel` (спасение заливки), `Roulette`; имя лидерборда `max_level` |
| **LocalizationTable** | ключ → ru/en/tr; фолбэки tr→en→ru→сам ключ; `Get()` — линейный поиск |

## Связи

- **Потребители:** [[Аудио-подсистема]] (SfxClip), [[Камера]] (CameraImpulseConfig), [[Детекция и сбор]] / [[Item и IAttractable \| Item]] (GhostFade, Attract), [[AdScheduler]] / [[Rewarder]] / [[RouletteService]] (YandexConfig), [[LocalizationService]] (LocalizationTable)

## Слабые места

- `YandexConfig._fillRescueRewardId` по умолчанию `"NextLevel"` — имя не соответствует смыслу «спасение заливки» (`YandexConfig.cs:13`).
- Все `Get()` — линейные поиски; `LocalizationTable.Get` дёргается на каждый текст, исключение-фолбэк тяжёлый.
