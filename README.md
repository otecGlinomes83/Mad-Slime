# Mad Slime

Проект для **Unity 2022.3.62f2**. Модели, текстуры, звуки и шрифты хранятся
через **Git LFS**. В Git лежат небольшие текстовые указатели, а содержимое
скачивается отдельно. Установить только Git недостаточно — нужен Git LFS
на каждом компьютере.

## Первый запуск на другом компьютере

1. Установи Git и [Git LFS](https://git-lfs.com/).
2. В терминале выполни:

   ```sh
   git lfs install
   git clone https://github.com/otecGlinomes83/Mad-Slime.git
   cd Mad-Slime
   ```

3. Запусти `setup-assets.cmd` из терминала Windows или
   `bash setup-assets.sh` на Linux/macOS/в Git Bash.
   Скрипт скачает ресурсы текущей ветки и проверит их целостность.
4. После успешной проверки добавь папку в Unity Hub и открой её
   в Unity 2022.3.62f2. Unity сама восстановит `Library` и скачает пакеты.

Используй `git clone`, а не **Download ZIP**: скрипту нужна папка `.git`.

## Если репозиторий уже скачан, но вместо ресурсов текстовые указатели

Закрой Unity. В терминале внутри папки проекта выполни:

```sh
git pull --ff-only
git lfs install --local
git -c lfs.fetchinclude= -c lfs.fetchexclude= -c lfs.skipdownloaderrors=false lfs pull
```

Затем запусти `setup-assets.cmd` или `bash setup-assets.sh`.
Пересоздавать репозиторий или проект не нужно. Если есть локальные правки,
сохрани их отдельным коммитом перед обновлением; не удаляй папку `Assets`.

Если LFS сообщает об ошибке авторизации, войди в GitHub с аккаунтом,
имеющим доступ к репозиторию. Ошибки отсутствующих объектов или квоты
нельзя исправить пересборкой Unity: сначала нужно устранить ошибку LFS.

## Отправка изменений

После первого запуска скрипта обычные `git add`, `git commit` и `git push`
отправляют и код, и LFS-ресурсы. Не пропускай Git hooks (`--no-verify`):
хук `pre-push` загружает ресурсы на сервер до отправки коммита.

Если меняешь правила LFS в `.gitattributes`, существующие подходящие файлы
нужно также перевести под эти правила и закоммитить. Одно изменение
`.gitattributes` не преобразует уже сохранённые файлы.

Подробнее: [Git LFS](https://git-lfs.com/) и
[восстановление загрузки объектов на GitHub](https://docs.github.com/en/repositories/working-with-files/managing-large-files/resolving-git-large-file-storage-upload-failures).
