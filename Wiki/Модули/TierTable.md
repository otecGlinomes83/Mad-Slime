# TierTable

**Слой:** Core · **Код:** `Assets/MadSlime/Scripts/Core/Scriptables/Tiers/TierTable.cs`

Таблица `ItemTier → (scale, mass)`: визуальный масштаб предмета при спавне и масса, которую игрок получает за поглощение.

## Зачем

Единый источник «веса» тиров для [[LevelGenerator]] (масштаб), [[PlayerTier]] (набор массы) и баланса порогов в [[PlayerConfig]].

## Особенности

- `Get(ItemTier)` — линейный поиск; записи нет → `InvalidOperationException` (fail-fast по канону проекта)
- Геттеры клампят: `scale >= 0.01`, `mass >= 1`

## Связи

- Потребители: [[LevelGenerator]], [[PlayerTier]], [[Player]], [[Камера]] (шейк по массе пикапа)
- Регистрация: `RegisterInstance` в [[ProjectLifetimeScope]]

## Слабые места

- Линейный поиск + throw на каждом обращении — таблица крошечная, но вызовы частые (каждый пикап); исключение как «ветка отсутствия записи» — тяжёлый фолбэк.
