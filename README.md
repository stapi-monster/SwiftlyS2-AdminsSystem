# 🛡️ AdminsSystem for SwiftlyS2

**AdminsSystem** — мощная и гибкая нативная система администрирования для Counter-Strike 2 (SwiftlyS2 Framework) с полной поддержкой таблиц базы данных и конфигурации формата **`cs2-admin_system`** (Pisex / AdminSystem).

---

## 📋 Оглавление

- [Особенности](#особенности)
- [Структура базы данных](#структура-базы-данных)
- [Конфигурация (config.jsonc)](#конфигурация-configjsonc)
- [Буквенные флаги доступа (flags.jsonc)](#буквенные-флаги-доступа-flagsjsonc)
- [Режимы иммунитета (immunity_type)](#режимы-иммунитета-immunity_type)
- [Команды и права доступа](#команды-и-права-доступа)
- [Консольное управление (sw_admins & sw_groups)](#консольное-управление-sw_admins--sw_groups)
- [Сборка и установка](#сборка-и-установка)

---

## ✨ Особенности

- 🗄️ **Совместимость с веб-панелями**: использует стандартные таблицы `as_admins`, `as_bans`, `as_mutes`.
- 🔀 **Буквенные флаги (`a`-`z`)**: настраиваемое сопоставление буквенных флагов с правами в `flags.jsonc`.
- 🌐 **Мультисерверная сеть**: гибкая привязка к серверам по `admin_server_id` и `punish_server_id` (`-1` — для всей сети).
- 🛡️ **Система иммунитета**: 4 режима защиты администрации (`0`, `1`, `2`, `3`).
- ⏱️ **Задержка перед баном (`ban_delay`)**: отображение причины бана игроку в чат перед киком.
- 🔒 **Разграничение снятия наказаний (`unpunish_type`)**: разрешить снимать только свои наказания или любые.
- 💬 **Настройка анонимности (`message_type` & `notify_type`)**: гибкое оповещение в чат о действиях админов.

---

## 🗄️ Структура базы данных

Плагин использует подключение `admin_system` из `configs/database.jsonc`. Поддерживаются **SQLite**, **MySQL**, **MariaDB** и **PostgreSQL**.

### Таблицы:

1. **`as_admins`**:
   - `id` — Уникальный ID записи.
   - `steamid` — SteamID64 администратора.
   - `name` — Отображаемое имя администратора.
   - `flags` — Строка флагов (например, `"a,b,c"` или `"z"`).
   - `immunity` — Уровень иммунитета (`0`–`100`).
   - `end` — Время окончания прав (`0` — бессрочно).

2. **`as_bans`**:
   - `id` — Уникальный ID бана.
   - `steamid` — SteamID64 нарушителя.
   - `name` — Имя нарушителя.
   - `ip` — IP-адрес нарушителя.
   - `ban_type` — Тип бана (`0` — SteamID, `1` — IP).
   - `duration` — Длительность бана в секундах.
   - `created` — Unix timestamp создания.
   - `ends` — Unix timestamp окончания (`0` — перманет).
   - `reason` — Причина бана.
   - `admin_steamid` — SteamID64 админа.
   - `admin_name` — Имя админа.
   - `server` — ID сервера.
   - `global_ban` — Глобальный бан для всей сети (`0`/`1`).

3. **`as_mutes`**:
   - `id` — Уникальный ID блокировки.
   - `steamid` — SteamID64 нарушителя.
   - `name` — Имя нарушителя.
   - `ip` — IP-адрес нарушителя.
   - `type` — Тип блокировки (`0` — Mute/чат, `1` — Gag/микрофон, `2` — Silence/всё).
   - `duration` — Длительность в секундах.
   - `created` — Unix timestamp создания.
   - `ends` — Unix timestamp окончания (`0` — перманет).
   - `reason` — Причина.
   - `admin_steamid` — SteamID64 админа.
   - `admin_name` — Имя админа.
   - `server` — ID сервера.

---

## ⚙️ Конфигурация (`config.jsonc`)

Параметры конфигурации располагаются в блоке `"Main"` файла `configs/plugins/Admins.Core/config.jsonc`:

```jsonc
{
  "Main": {
    "Prefix": "[RED][Admins][DEFAULT]",
    "UseDatabase": true,
    "TimeZone": "UTC",
    "AdminsDatabaseSyncIntervalSeconds": 60,
    "BansDatabaseSyncIntervalSeconds": 30,
    "SanctionsDatabaseSyncIntervalSeconds": 30,

    // Префикс таблиц в базе данных
    "database_prefix": "as_",

    // Server ID для загрузки администраторов (-1 — брать все сервера)
    "admin_server_id": 1,

    // Server ID для загрузки наказаний (-1 — брать вне зависимости от сервера)
    "punish_server_id": 1,

    // Учитывать IP адреса при наказаниях (0 — нет, 1 — да)
    "punish_ip": 1,

    // Кто получает информацию о наказании (0 — никто, 1 — нарушитель и админ, 2 — весь сервер)
    "notify_type": 1,

    // Режим работы иммунитета (0, 1, 2, 3)
    "immunity_type": 1,

    // Задержка перед киком в секундах, чтобы забаненный увидел причину (0 — мгновенно)
    "ban_delay": 5,

    // Снятие наказаний: 0 — только свои наказания, 1 — любые наказания
    "unpunish_type": 0,

    // Макс. количество игроков в списках оффлайн-наказаний
    "punish_offline_count": 30,
    "unpunish_offline_count": 30,

    // Оповещения в чат: 0 — выкл, 1 — анонимно ("Администратор"), 2 — с ником админа
    "message_type": 1,

    // Разрешить ввод своей причины в чат (0 — нет, 1 — да, требуется право @admin/own_reason)
    "own_reason": 1
  }
}
```

---

## 🔤 Буквенные флаги доступа (`flags.jsonc`)

Файл `configs/plugins/Admins.Core/flags.jsonc` задаёт сопоставление буквенных флагов с правами:

```jsonc
{
  "Flags": {
    "a": {
      "name": "Резервный слот",
      "access": {
        "@admin/reserve": "1"
      }
    },
    "b": {
      "name": "Администратор (Баны и кики)",
      "access": {
        "admins.commands.ban": "1",
        "admins.commands.unban": "1",
        "admins.commands.kick": "1"
      }
    },
    "c": {
      "name": "Кик игроков",
      "access": {
        "admins.commands.kick": "1"
      }
    },
    "d": {
      "name": "Баны игроков",
      "access": {
        "admins.commands.ban": "1",
        "admins.commands.unban": "1"
      }
    },
    "e": {
      "name": "Муты / Гаги",
      "access": {
        "admins.commands.mute": "1",
        "admins.commands.gag": "1",
        "admins.commands.silence": "1"
      }
    },
    "z": {
      "name": "Главный администратор (Полный доступ)",
      "access": {
        "admins.commands.*": "1",
        "admins.menu.bans": "1",
        "admins.menu.comms": "1",
        "@admin/own_reason": "1"
      }
    }
  }
}
```

---

## 🛡️ Режимы иммунитета (`immunity_type`)

- **`1` (ProtectFromLowerAccess)**: Администратор с иммунитетом `50` может наказать администратора с иммунитетом `<= 50`, но не с `51+`.
- **`2` (ProtectFromEqualOrLowerAccess)**: Администратор с иммунитетом `50` НЕ может наказать администратора с `50` (требуется строго больший уровень иммунитета).
- **`3` (ProtectWithNoImmunityBypass)**: Администраторы НЕ могут наказывать друг друга ни при каких условиях. Наказания применимы только к обычным игрокам.
- **`0` (IgnoreImmunity)**: Иммунитет полностью отключен.

---

## 📜 Команды и права доступа

### Основные и Меню
| Команда | Право | Описание |
| :--- | :--- | :--- |
| `!admin` | `admins.commands.admin` | Открыть GUI-меню админа |
| `!admins` | `admins.command.admins` | Управление админами через чат |
| `!groups` | `admins.command.groups` | Управление группами через чат |

### Наказания и Баны (`Admins.Bans`)
| Команда | Право | Описание |
| :--- | :--- | :--- |
| `!ban <player> <time> <reason>` | `admins.commands.ban` | Забанить игрока |
| `!globalban <player> <time> <reason>` | `admins.commands.globalban` | Глобальный бан по всей сети |
| `!banip <player/ip> <time> <reason>` | `admins.commands.ban` | Забанить по IP |
| `!bano <steamid64> <time> <reason>` | `admins.commands.ban` | Оффлайн-бан по SteamID64 |
| `!unban <steamid64>` | `admins.commands.unban` | Разбанить по SteamID64 |
| `!unbanip <ip>` | `admins.commands.unban` | Разбанить IP |

### Блокировки общения (`Admins.Comms`)
| Команда | Право | Описание |
| :--- | :--- | :--- |
| `!gag <player> <time> <reason>` | `admins.commands.gag` | Отключить голосовой чат |
| `!mute <player> <time> <reason>` | `admins.commands.mute` | Отключить текстовый чат |
| `!silence <player> <time> <reason>` | `admins.commands.silence` | Отключить текстовый и голосовой чат |
| `!ungag <player/steamid64>` | `admins.commands.ungag` | Снять гаг |
| `!unmute <player/steamid64>` | `admins.commands.unmute` | Снять мут |
| `!unsilence <player/steamid64>` | `admins.commands.unsilence` | Снять сайленс |
| `!gago <steamid64> <time> <reason>` | `admins.commands.gag` | Оффлайн-гаг |
| `!muteo <steamid64> <time> <reason>` | `admins.commands.mute` | Оффлайн-мут |
| `!silenceo <steamid64> <time> <reason>` | `admins.commands.silence` | Оффлайн-сайленс |

### Супер-команды (`Admins.SuperCommands`)
| Команда | Право | Описание |
| :--- | :--- | :--- |
| `!hp <player> <health> [armor]` | `admins.commands.hp` | Установить здоровье и броню |
| `!slay <player>` | `admins.commands.slay` | Убить игрока |
| `!slap <player> [damage]` | `admins.commands.slap` | Пнуть/нанести урон игроку |
| `!freeze <player>` / `!unfreeze` | `admins.commands.freeze` | Заморозить / разморозить |
| `!noclip [player]` | `admins.commands.noclip` | Включить/выключить сквозь стены |
| `!god <player>` | `admins.commands.god` | Режим бога |
| `!goto <player>` / `!bring <player>` | `admins.commands.goto` | Телепортация к игроку / игрока к себе |
| `!respawn <player>` | `admins.commands.respawn` | Возродить игрока |
| `!swap <player>` / `!team <player> <team>` | `admins.commands.swap` | Сменить команду игроку |

---

## 🖥️ Консольное управление (`sw_admins` & `sw_groups`)

Вы можете управлять администраторами и группами напрямую из серверной консоли:

```bash
# Выдача прав админа (SteamID64, Имя, Иммунитет, Права, Группы, Сервера)
sw_admins give 76561198123456789 "AdminName" 100 "z" "" "1"

# Редактирование админа
sw_admins edit 76561198123456789 immunity 90
sw_admins edit 76561198123456789 permissions "a,b,c"

# Удаление админа
sw_admins remove 76561198123456789

# Список всех админов
sw_admins list
```

---

## 🔨 Сборка и установка

### Требования:
- **.NET 10.0 SDK**
- Игровой сервер CS2 с установленным **SwiftlyS2**

---

### 🪟 Быстрая автоматическая сборка (Windows):

Запустите скрипт `build_all.bat` или `build_all.ps1` из корня папки `AdminsSystem`:

```cmd
build_all.bat
```
или в PowerShell:
```powershell
.\build_all.ps1
```

Скрипт автоматически соберёт все 5 модулей и скомпонует их в единую тестовую папку `./build/test_publish/`.

---

### 🐧 Быстрая автоматическая сборка (Linux):

```bash
chmod +x build_all.sh
./build_all.sh
```

---

### 🛠️ Ручная сборка каждого модуля (Windows `win-x64`):

Выполните команды публикации из корня проекта:

```powershell
# 1. Ядро (Admins.Core)
dotnet publish ./Admins.Core/Admins.Core.csproj -c Release -r win-x64 --self-contained false -o ./build/plugins/Admins.Core

# 2. Баны (Admins.Bans)
dotnet publish ./Admins.Bans/Admins.Bans.csproj -c Release -r win-x64 --self-contained false -o ./build/plugins/Admins.Bans

# 3. Муты / Гаги (Admins.Comms)
dotnet publish ./Admins.Comms/Admins.Comms.csproj -c Release -r win-x64 --self-contained false -o ./build/plugins/Admins.Comms

# 4. Меню (Admins.Menu)
dotnet publish ./Admins.Menu/Admins.Menu.csproj -c Release -r win-x64 --self-contained false -o ./build/plugins/Admins.Menu

# 5. Супер-команды (Admins.SuperCommands)
dotnet publish ./Admins.SuperCommands/Admins.SuperCommands.csproj -c Release -r win-x64 --self-contained false -o ./build/plugins/Admins.SuperCommands
```

---

### 🐧 Сборка для Linux (`linux-x64`):

```bash
# 1. Ядро (Admins.Core)
dotnet publish ./Admins.Core/Admins.Core.csproj -c Release -r linux-x64 --self-contained false -o ./build/plugins/Admins.Core

# 2. Баны (Admins.Bans)
dotnet publish ./Admins.Bans/Admins.Bans.csproj -c Release -r linux-x64 --self-contained false -o ./build/plugins/Admins.Bans

# 3. Муты / Гаги (Admins.Comms)
dotnet publish ./Admins.Comms/Admins.Comms.csproj -c Release -r linux-x64 --self-contained false -o ./build/plugins/Admins.Comms

# 4. Меню (Admins.Menu)
dotnet publish ./Admins.Menu/Admins.Menu.csproj -c Release -r linux-x64 --self-contained false -o ./build/plugins/Admins.Menu

# 5. Супер-команды (Admins.SuperCommands)
dotnet publish ./Admins.SuperCommands/Admins.SuperCommands.csproj -c Release -r linux-x64 --self-contained false -o ./build/plugins/Admins.SuperCommands
```

---

### 📂 Размещение на сервере:

После компиляции скопируйте скомпилированные папки из `./build/plugins/` в директорию игрового сервера `addons/swiftlys2/plugins/`:

- `addons/swiftlys2/plugins/Admins.Core/`
- `addons/swiftlys2/plugins/Admins.Bans/`
- `addons/swiftlys2/plugins/Admins.Comms/`
- `addons/swiftlys2/plugins/Admins.Menu/`
- `addons/swiftlys2/plugins/Admins.SuperCommands/`

