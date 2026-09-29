# UiAnimations

**Слой:** UI · **Код:** `Assets/MadSlime/Scripts/UI/Animations/`

Мини-фреймворк появления/исчезновения UI на DOTween.

## Состав

- **UiAnimations** (static): `ScaleIn` (0.6× → база, OutBack, 0.25 с, unscaled time) и `ScaleOut` (в ноль, InBack, 0.2 с + callback) — база всех окон ([[Окна]]) и экрана рулетки
- **UiEnableAnimation**: авто-анимация появления при `OnEnable` — scale-in или слайд из края (`UiAppearMode`: None/Scale/FromLeft/…), 0.3 с, офсет 140; `PlayOutro()` — обратная и `SetActive(false)`; запоминает «включённое» состояние в Awake
- **UiEnableScheduler**: режиссёр каскадов — два массива `UiEnableTarget {target, delay}`: включение при старте сцены и при первом движении (`PlayerInputReader.MovementKeyPressed` — до старта сессии HUD скрыт), выключение/Outro при `Timer.Finished` или `QuotaCompleted`
- **UiEnableTarget** — сериализуемая пара

## Связи

- [[HUD]], [[Окна]], [[RouletteView]], [[Timer]], [[LevelProgress]], [[PlayerInputReader]]
- Сцена Game настраивается тулзой «Run Mad Slime → Setup Game FX» (упоминание в тексте исключений `UiEnableScheduler.cs:156`)

## Слабые места

- Отписки UiEnableScheduler только в `OnDestroy` (`UiEnableScheduler.cs:62-79`): при `OnDisable` подписки живы — FinishTargets может дёрнуться на выключенном компоненте.
- Тексты исключений ссылаются на имя тулзы, которого нет в меню SetupAll (запускается отдельно) — лёгкий рассинхрон доков.
