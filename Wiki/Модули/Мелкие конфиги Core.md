# Мелкие конфиги Core

**Слой:** Core · **Код:** `Assets/MadSlime/Scripts/ScriptableObjects/` (кроме уровней и крупных, у которых свои доки)

## Состав

| Конфиг | Что настраивает |
|---|---|
| **SfxClip** (Audio) | звуковой эффект: клип + громкость + случайный питч ([[Аудио-подсистема]]) |
| **Playlist** (Audio) | список треков для MusicChannel + `RandomStart` (плейлисты Menu/Game/Fill у оркестраторов) |
| **CameraConfig** (Camera) | камера-хаптика: подтяжка/пуш на тир-апе (MaxPull/MaxPush/TierPushStrength), шейк от тяжёлых предметов, FOV-кик ([[Камера]]) |
| **AttractConfig** (Collectables) | магнит: сила притяжения, разгон у точки захвата, орбитальность ([[Детекция и сбор | ItemAttractor]]) |
| **GhostFadeConfig** (Items) | переход обычный вид ↔ призрак ([[Предметы | GhostAnimator]]) |
| **ItemDefinition** (Items) | иконка + тир предмета ([[Предметы]]) |
| **YandexConfig** (Platform) | id rewarded-блоков (`DoubleReward`, спасение заливки, `Roulette`), имя лидерборда `CollectedItems` ([[AdScheduler]], [[Лидерборды]]) |
| **LocalizationTable** (Localization) | ключ → ru/en/tr; фолбэки tr→en→ru→сам ключ ([[Локализация UI | Локализация]]) |

Свои доки: [[PlayerConfig]], [[TierTable]], [[UpgradesConfig]], [[RewardConfig]], [[FillConfig]], [[Конфиги скинов и рулетки]] (RouletteConfig, SkinRarityTable, ShopContent, SkinItem), [[Конфиги уровней]].

## Связи

- Потребители: [[Аудио-подсистема]], [[Камера]], [[Детекция и сбор]], [[AdScheduler]], [[Лидерборды]], [[LocalizationService]]
