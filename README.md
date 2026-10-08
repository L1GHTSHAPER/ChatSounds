# ChatSounds

![LightShaper](tools/assets/lightshaper-wordmark.png)

[Source code on GitHub](https://github.com/L1GHTSHAPER/ChatSounds) | [Report an issue](https://github.com/L1GHTSHAPER/ChatSounds/issues) | [Thunderstore](https://thunderstore.io/c/on-together/p/LightShaper/ChatSounds/)

A BepInEx mod for [On Together](https://store.steampowered.com/app/2688490/On_Together/) that plays a sound when a chat message arrives, with separate settings for the **global chat**, the **local chat** and messages that **mention you**.

**♥ Enjoying the mod? Leave a like on [Thunderstore](https://thunderstore.io/c/on-together/p/LightShaper/ChatSounds/) and a ⭐ on [GitHub](https://github.com/L1GHTSHAPER/ChatSounds) — it helps the project grow!**

## Settings menu

**F4**, `/chatsound` or the speaker side button open/close settings. Tabs: **General**, **Global chat**, **Local chat**, **Mentions**. General → More options contains game volume and focus rules. Mentions → Keywords lets you edit and apply words. The ♪ button previews the sound.

Cream panels, warm brown text, coral accents, rounded controls and game fonts. Side buttons form one group, show the mod name and the settings hotkey (where available), and move away from visible UI panels. If both edges are blocked, the buttons wait until space becomes available. Existing hotkeys, commands and configuration keys are preserved. Menus scroll on smaller screens; changes save automatically. Where shown, **Apply** saves a field draft. Invalid values keep the saved setting.

## Features

- Sounds for other players' messages in the global and local chat, plus a separate sound when a message mentions your name or one of your keywords.
- Each of the three has its own on/off switch, sound, volume, play condition (always / only when you can't see that chat tab / only while the game is in the background) and a cooldown against spam.
- 8 built-in sounds (Ping, Chime, Bubble, Drop, Marimba, Bell, Soft, Alert), the game's own sound effects and pomodoro bells, or your own `.wav` / `.ogg` / `.mp3` files.
- In-game settings window (**F4** or `/chatsound`) that plays each sound as you pick it. The same settings are in the mod manager's config editor, and edits made there apply while the game is running.
- Optional quiet mode for focus sessions: only mentions make a sound while you focus.
- Follows the game's Master volume (can be turned off).
- Only messages that actually appear in your chat make a sound. Your own messages, ignored or muted players and local messages from players too far away stay silent.
- Client-side only: nothing is sent to other players.
- English and Russian interface (follows the game's language).

## Usage

| Action | How |
|---|---|
| Settings window | **F4** or `/chatsound` |
| All sounds on / off | **Left Shift + F4**, `/chatsound on`, `/chatsound off` |
| One kind on / off | `/chatsound global off`, `/chatsound local on`, `/chatsound mentions off` |
| Volume | `/chatsound volume 70` (master), `/chatsound volume local 50` |
| Pick a sound | `/chatsound sound global Chime`, `/chatsound sound mentions file:ding.ogg` |
| List all sounds | `/chatsound sounds` |
| Listen | `/chatsound test` (all three), `/chatsound test local`, `/chatsound test Bell` |
| When to play | `/chatsound when global notviewing` (`always`, `notviewing`, `unfocused`) |
| Mention keywords | `/chatsound keyword add mark, markus`, `keyword remove mark`, `keyword list`, `keyword clear` |
| Rescan your sound files | `/chatsound reload` |
| Current settings | `/chatsound status` |
| Command list | `/chatsound help` |

`/csnd` is a short alias for `/chatsound`. Commands are handled on your side and are never posted to the chat. If [CommandAPI](https://thunderstore.io/c/on-together/p/jaide/CommandAPI/) is installed, the command is also listed by its `/help` and suggested by CommandTypeahead.

## When a sound plays

| Setting | Plays |
|---|---|
| `Always` | for every message |
| `NotViewing` | only when you can't see the message's chat tab right now: the other tab is selected, the chat is hidden or the game window is in the background |
| `Unfocused` | only while the game window is in the background (for example, while you work in another window) |

A mention plays the mention sound **instead of** the global/local sound. If the mention sound does not play (because of its condition, cooldown or zero volume), the message gets the normal global/local sound.

The cooldown is the minimum time between two sounds of the same kind: messages that arrive sooner stay silent.

## Your own sounds

Put `.wav`, `.ogg` or `.mp3` files into `BepInEx/config/ChatSounds/`. The folder is created on first launch, and the **Sounds folder** button in the settings window opens it. Press **Rescan** and pick your file with the arrows next to the sound name, or write `file:<name>` into a `Sound` setting. The folder is in the config directory, so your files survive mod updates. Short sounds (under 2 seconds) work best.

## Configuration

`BepInEx/config/ontogether.chatsounds.cfg` (created on first launch; editable from the mod manager's Config editor, also while the game is running).

| Section | Key | Default | Description |
|---|---|---|---|
| General | `Enabled` | `true` | Play sounds for new chat messages. |
| General | `MasterVolume` | `80` | Volume of all chat sounds, in percent. |
| General | `FollowGameVolume` | `true` | Also scale by the game's Master volume. |
| General | `QuietDuringFocus` | `false` | During a focus session only mentions make a sound. |
| General | `Language` | `Auto` | `Auto` (the game's language), `English`, `Russian`. |
| Hotkeys | `SettingsWindow` | `F4` | Opens / closes the settings window. |
| Hotkeys | `ToggleSounds` | `F4 + LeftShift` | Turns all chat sounds on / off. |
| Global | `Enabled`, `Sound`, `Volume`, `PlayWhen`, `Cooldown` | `true`, `Ping`, `60`, `Always`, `3` | Global chat. |
| Local | `Enabled`, `Sound`, `Volume`, `PlayWhen`, `Cooldown` | `true`, `Bubble`, `80`, `Always`, `1` | Local chat (players near you). |
| Mentions | `Enabled`, `Sound`, `Volume`, `PlayWhen`, `Cooldown` | `true`, `Alert`, `100`, `Always`, `1` | Messages that mention you, in either chat. |
| Mentions | `MatchMyName` | `true` | A message containing your in-game name is a mention. |
| Mentions | `Keywords` | *(empty)* | More mention words, separated by commas, e.g. nicknames. Whole words, case does not matter. |

`Sound` values:

- built-in: `Ping`, `Chime`, `Bubble`, `Drop`, `Marimba`, `Bell`, `Soft`, `Alert`;
- game sounds: `game:CompleteTask`, `game:EarnTicket`, `game:BuyItem`, `game:PetUnlock`, `game:FocusStart`, `game:FocusEnd`, `game:FishAlertSound`, `game:BalloonPop`, `game:UIClick` ..., and the pomodoro bells `game:Bell1`, `game:Bell2` ...;
- your file: `file:ding.ogg` (from `BepInEx/config/ChatSounds`) or a full path.

## Installation

**Thunderstore Mod Manager / r2modman:** install from the mod list, or use *Settings -> Import local mod* with the package zip.

**Manual:** install [BepInExPack](https://thunderstore.io/c/on-together/p/BepInEx/BepInExPack/) and copy `ChatSounds.dll` into `BepInEx/plugins/`.

---

## Русский

**♥ Нравится мод? Поставьте лайк на [Thunderstore](https://thunderstore.io/c/on-together/p/LightShaper/ChatSounds/) и ⭐ звезду на [GitHub](https://github.com/L1GHTSHAPER/ChatSounds) — это помогает проекту расти!**

Настройки: **F4**, `/chatsound` или боковая кнопка с динамиком. Вкладки: **Общее**, **Общий чат**, **Локальный чат**, **Упоминания**. «Общее → Дополнительно» содержит игровые уровни громкости и правила фокусировки. В «Упоминаниях» можно ввести и сохранить ключевые слова. Кнопка ♪ проигрывает образец.

Кремовые панели, коричневый текст, коралловые акценты и скруглённые элементы. Боковые кнопки собраны в одну группу; при наведении видны название мода и клавиша настроек, если она есть. Группа избегает видимых игровых панелей; когда места нет, кнопки скрываются до освобождения края. Настройки и прежние клавиши сохранены. Низкие окна прокручиваются, изменения сохраняются автоматически; кнопка «Применить», где она есть, сохраняет введённое значение.

Мод проигрывает звук, когда в чат приходит сообщение. Отдельные настройки есть для **глобального чата**, **локального чата** и **упоминаний** (ваше имя или заданные слова).

**Возможности**

- Звук на сообщения других игроков в глобальном и локальном чате и отдельный звук, когда сообщение упоминает вас.
- У каждого из трёх свои: вкл/выкл, звук, громкость, условие (всегда / только когда вкладка этого чата не видна / только когда игра в фоне) и пауза между звуками, чтобы не было «пулемёта».
- 8 встроенных звуков, звуки самой игры и колокольчики помодоро, а также свои файлы `.wav` / `.ogg` / `.mp3`.
- Окно настроек в игре (**F4** или `/chatsound`), звук проигрывается сразу при выборе. Те же настройки доступны в редакторе конфигов мод-менеджера, изменения применяются без перезапуска игры.
- Режим «Во время фокуса только упоминания».
- Учитывается общая громкость игры (можно отключить).
- Звучат только сообщения, которые действительно появились у вас в чате: свои сообщения, игроки из списка игнора и мьюта и локальные сообщения издалека звука не дают.
- Всё работает только у вас, другим игрокам ничего не отправляется.
- Интерфейс на русском и английском (по языку игры).

**Управление**

- **F4** или `/chatsound`: окно настроек; **левый Shift + F4**, `/chatsound on|off`: включить или выключить все звуки.
- `/chatsound global|local|mentions on|off`: глобальный чат, локальный чат, упоминания.
- `/chatsound volume 70`: общая громкость; `/chatsound volume local 50`: громкость локального чата.
- `/chatsound sound global Chime`: выбрать звук; `/chatsound sounds`: список звуков; `/chatsound test`: прослушать.
- `/chatsound when global notviewing`: когда играть (`always`: всегда, `notviewing`: если вкладка не видна, `unfocused`: если игра в фоне).
- `/chatsound keyword add марк, маркус`: слова-упоминания (`remove`, `list`, `clear`).
- `/chatsound status`: текущие настройки, `/chatsound help`: все команды. Короткий вариант команды: `/csnd`.

**Свои звуки:** положите файлы в `BepInEx/config/ChatSounds/` (кнопка **Папка звуков** в окне настроек открывает её), нажмите **Обновить** и выберите файл стрелками рядом с названием звука. Файлы лежат в папке конфигов и не пропадают при обновлении мода.

**Настройки:** `BepInEx/config/ontogether.chatsounds.cfg`, таблица выше.

## Building from source

Requires Windows, .NET SDK 6.0 or newer, an installed copy of On Together, and BepInEx 5 (for example, a Thunderstore Mod Manager / r2modman profile).

From the repository directory, run:

```powershell
powershell -ExecutionPolicy Bypass -File .\build.ps1 -GameDir "C:\path\to\On-Together" -BepInExCore "C:\path\to\profile\BepInEx\core"
```

`GameDir` must contain `OnTogether.exe` and `OnTogether_Data\Managed`. `BepInExCore` must contain `BepInEx.dll` and `0Harmony.dll`. Game and BepInEx assemblies are referenced locally and are not distributed in this repository.

The build creates the plugin DLL in `src/bin/Release/` and the installable Thunderstore archive in `dist/`. Ready-to-install archives are also available in [GitHub Releases](https://github.com/L1GHTSHAPER/ChatSounds/releases).


Side settings buttons are opaque squares with rounded corners, a dark brown outline and proportionate icons, sized to match the game's right-hand controls. They hide with the native controls in Desktop mode, including tooltips and pointer hit areas.

Боковые кнопки настроек стали непрозрачными и квадратными: скруглённые углы, коричневая обводка и значки без растягивания. Размер соответствует высоте игровых кнопок справа. В Desktop-режиме они скрываются вместе с игровыми; подсказки и области нажатия также отключаются.

Existing default F9 / Left Shift + F9 shortcuts automatically move to F4 / Left Shift + F4 when MovementPlus is installed. Other custom shortcuts are preserved.

При установленном MovementPlus прежние стандартные F9 / левый Shift + F9 автоматически заменяются на F4 / левый Shift + F4. Остальные пользовательские сочетания сохраняются.
