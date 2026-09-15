# AI_PLAN — согласованные планы работ (не начаты)

> Для AI-агента. Здесь лежат планы, согласованные с владельцем, но ещё не реализованные. Исполненный план удаляется/переносится в `AI_NOTES.md` как свершившийся факт. Правила стиля — `AI_RULES.md`, состояние кода — `AI_CONTEXT.md`.

---

## План 1: слить Prop Factory + Icon Generator + Prop Bake в один пайплайн (2026-09-15)

**Статус:** согласован владельцем (выбран вариант «одна кнопка», ссылки на иконки в рантайме остаются). **НЕ начат.** Владелец просил не трогать ничего, пока он сам не закончит свои правки фабрики — перед стартом сверить этот план с фактическим состоянием кода.

### Целевая картина

Закинул модели в папку → одна кнопка → префабы + дефинишны (предмет × тир) + иконки + заполненный PropSet. Рукодельная иконка: PNG с правильным именем в `Resources/Icons/` → следующий прогон подхватит сам. Танцы с `IconGenerationSource` в сцене умирают (ссылок в сценах уже нет — проверено 2026-09-15, остался только .cs).

### Состояние на момент плана (проверено 2026-09-15)

- Три редакторских инструмента дублируют друг друга:
  - `Assets/Editor/ItemPropFactory.cs` — префабы + дефинишны (`D_<Model>.asset`, mass=1, tier=Small, иконку НЕ ставит), папка `Items/Generated`.
  - `Assets/Editor/ItemIconGenerator/ItemIconGenerator.cs` — рендер иконок, но через `IconGenerationSource` в сцене (модели кидаются руками).
  - `Assets/Editor/PropBakeWindow.cs` — дефинишны (предмет × тир) в `Items/Baked/<PropSet>/` + авто-иконка по имени + запись `_variants` в PropSet. Похоже, ни разу не гонялся: папки `Baked/` нет.
- В `Assets/Scriptables/Items/` лежат 14 старых `D_*.asset` в корне — артефакт старого фабричного прогона (`_icon: 0`, mass=1, tier=Small).
- Живой ключ имени: Item-префаб `Item_<Model>` ↔ иконка `Item_<Model>.png` (существующие PNG уже так названы).
- `PropSet.Props` читает `LayoutPreviewDrawer` (превью раскладки) — список Props продолжать поддерживать.

### Шаги

1. **Новый файл `Assets/Editor/ItemPipelineWindow.cs`** (namespace `EditorTools`, sealed EditorWindow, меню сохранить `Mad Slime/Prop Factory`). Поля: Models Folder, Prefabs Folder (дефолт `Assets/Resources/Prefabs/Items/Generated`), PropSet, TierTable, Collider Inset (0.25), тумблер Force Icons.
2. **Generate — один прогон, стадии:**
   - **Префабы** (логика `ItemPropFactory.CreateItemPrefab`): get-or-create `Item_<Model>.prefab` — НЕ пересоздавать (GUID'ы и ссылки в PropSet'ах/сценах должны жить), обновлять коллайдер с инсетом, слой Collectable, `_collider`. `_definition` на корне → Small-дефинишн как fallback.
   - **Иконки**: если `Resources/Icons/<имя префаба>.png` нет → рендер кодом из `ItemIconGenerator` (256px, орто, padding 1.8, свет 45/-30) прямо из сгенерённого префаба, без сцены. PNG есть — не трогать, рукодельный приоритет. Force Icons — перерендер всех.
   - **Дефинишны** (логика `PropBakeWindow.GetOrCreateDefinition`): get-or-create `D_<префаб>_<Tier>.asset` в `Assets/Scriptables/Items/Baked/<PropSet.name>/` путь-стабильно; писать `_tier`/`_baseMass` из TierTable + `_icon` по имени.
   - **PropSet**: `Props` = префабы из папки (алфавитно), `Variants` = пары (prefab, definition); запись SerializedObject'ом как сейчас.
   - **Отчёт в Console**: сколько пропсов, недостающие иконки (LogWarning на каждую), дубли имён моделей → LogError и abort до старта (имя = ключ всего).
3. **Reapply Collider Inset** — переносится как вторая кнопка.
4. **Удаление мёртвого**: `PropBakeWindow.cs`, `ItemIconGenerator/ItemIconGenerator.cs`, `IconGenerationSource.cs`, старые пункты меню.
5. **Чистка легаси-данных — только после ОК владельца и первого успешного прогона**: 14 старых `D_*.asset` в корне `Items/`. Перед удалением проверить их GUID'ы по LevelConfig/PropSet/сценам; ссылки в `_variants` перепишутся новым бейком.
6. **Верификация**: batchmode-компиляция; кнопку Generate жмёт владелец в редакторе (рендер иконок в `-nographics` невозможен), смотрит отчёт. Апдейт `AI_NOTES.md`.

### Дефолты (владелец не оспорил)

- Ключ предмета = имя Item-префаба `Item_<Model>` — везде: иконки, дефинишны, отчёты.
- Дефинишны живут в `Baked/<PropSet>/` — эту конвенцию уже подсказывают логи `LevelGenerator`'а.
- Осиротевшие ассеты (переименовал модель) тулза только репортит, молча не удаляет.

### Риски

- Рендер иконок требует графику — автопрогон в batchmode не сделать, верификация за владельцем.
- Владелец правит фабрику параллельно — перед стартом перечитать состояние (папки, PropSet'ы, TierTable, что стало со старыми `D_*`).
