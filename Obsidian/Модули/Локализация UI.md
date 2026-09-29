# Локализация UI

**Слой:** UI · **Код:** `Assets/MadSlime/Scripts/UI/LocalizedText.cs`, `LanguageSwitcher.cs`; данные — [[Мелкие конфиги Core | LocalizationTable]], движок — [[LocalizationService]]

## Как работает

- **LocalizedText** (`[RequireComponent(TMP_Text)]`): ключ в поле, `OnEnable` применяет перевод и подписывается на `Localization.LanguageChanged`; пустой ключ → throw. Весь статичный текст UI живёт через него
- **LanguageSwitcher**: кнопка циклической смены (`Localization.CycleLanguage()`) + метка текущего языка
- Ключи раскладываются по таблице `LocalizationTable` (key → ru/en/tr, фолбэки tr→en→ru→ключ). Новые ключи дописывает [[MadSlimeContentSetup]]

## Где ключи собираются в коде

- Динамические строки: `string.Format(Localization.Get("level_label"), N)` — [[HUD]]; `tier_{tier}` — GrowthBarView; `upgrade_*`/`perk_*` — switch-маппинги UpgradeItemView; `rarity_*` — RouletteView; `roulette_won_*`, `shop_*`, `menu_daily`, `to_menu`, `lang_self_*`

## Связи

- [[LocalizationService]] (источник языка и событий), [[Интерфейсы Core (шов YG2) | ILanguageProvider]] (язык платформы), все UI-заметки

## Слабые места

- Ключи-строки рассыпаны по коду switch'ами (`UpgradeItemView.cs:174-262`, `RouletteView.GetRarityKey`) — опечатка в ключе молча даст фолбэк.
- Дефолтные таблицы — ru-first: пустой en → русский текст у англоязычных, пока ключ не заполнен.
