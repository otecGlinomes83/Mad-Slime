# Мелкие UI-компоненты

**Слой:** UI · **Код:** корень `Assets/MadSlime/Scripts/UI/`

- **LookAtCamera** (`UI/LookAtCamera.cs`, 24 строки): в `LateUpdate` копирует поворот камеры — билборды (тарелки квоты над предметами)
- **MenuBackground** (`UI/MenuBackground.cs`, 43 строки): шейдерный анимированный фон меню; создаёт инстанс материала (`new Material`) в коде и каждый кадр пишет `_UnscaledTime`; инстанс уничтожает в OnDestroy

## Связи

- LookAtCamera: [[HUD]]/мировые плашки + [[Камера]]
- MenuBackground: [[MainMenu]]

## Слабые места

- `MenuBackground` — аллокация материала и запись в шейдер-свойство каждый кадр; для меню WebGL приемлемо, но это постоянный cost.
