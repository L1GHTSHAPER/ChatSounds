# ChatSounds

![LightShaper](https://raw.githubusercontent.com/L1GHTSHAPER/ChatSounds/main/tools/assets/lightshaper-wordmark.png)

[Source code on GitHub](https://github.com/L1GHTSHAPER/ChatSounds) | [Report an issue](https://github.com/L1GHTSHAPER/ChatSounds/issues) | [Thunderstore](https://thunderstore.io/c/on-together/p/LightShaper/ChatSounds/)

A BepInEx mod for [On Together](https://store.steampowered.com/app/2688490/On_Together/) that plays a sound when a chat message arrives, with separate settings for the **global chat**, the **local chat** and messages that **mention you**.

## Features

- Sounds for other players' messages in the global and local chat, plus a separate sound when a message mentions your name or one of your keywords.
- Each of the three has its own on/off switch, sound, volume, play condition (always / only when you can't see that chat tab / only while the game is in the background) and a cooldown against spam.
- 8 built-in sounds (Ping, Chime, Bubble, Drop, Marimba, Bell, Soft, Alert), the game's own sound effects and pomodoro bells, or your own `.wav` / `.ogg` / `.mp3` files.
- In-game settings window (**F9** or `/chatsound`) that plays each sound as you pick it. The same settings are in the mod manager's config editor, and edits made there apply while the game is running.
- Optional quiet mode for focus sessions: only mentions make a sound while you focus.
- Follows the game's Master volume (can be turned off).
- Only messages that actually appear in your chat make a sound. Your own messages, ignored or muted players and local messages from players too far away stay silent.
- Client-side only: nothing is sent to other players.
- English and Russian interface (follows the game's language).

## Usage

| Action | How |
|---|---|
| Settings window | **F9** or `/chatsound` |
| All sounds on / off | **Left Shift + F9**, `/chatsound on`, `/chatsound off` |
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
| Hotkeys | `SettingsWindow` | `F9` | Opens / closes the settings window. |
| Hotkeys | `ToggleSounds` | `F9 + LeftShift` | Turns all chat sounds on / off. |
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

Мод проигрывает звук, когда в чат приходит сообщение. Отдельные настройки есть для **глобального чата**, **локального чата** и **упоминаний** (ваше имя или заданные слова).

**Возможности**

- Звук на сообщения других игроков в глобальном и локальном чате и отдельный звук, когда сообщение упоминает вас.
- У каждого из трёх свои: вкл/выкл, звук, громкость, условие (всегда / только когда вкладка этого чата не видна / только когда игра в фоне) и пауза между звуками, чтобы не было «пулемёта».
- 8 встроенных звуков, звуки самой игры и колокольчики помодоро, а также свои файлы `.wav` / `.ogg` / `.mp3`.
- Окно настроек в игре (**F9** или `/chatsound`), звук проигрывается сразу при выборе. Те же настройки доступны в редакторе конфигов мод-менеджера, изменения применяются без перезапуска игры.
- Режим «Во время фокуса только упоминания».
- Учитывается общая громкость игры (можно отключить).
- Звучат только сообщения, которые действительно появились у вас в чате: свои сообщения, игроки из списка игнора и мьюта и локальные сообщения издалека звука не дают.
- Всё работает только у вас, другим игрокам ничего не отправляется.
- Интерфейс на русском и английском (по языку игры).

**Управление**

- **F9** или `/chatsound`: окно настроек; **левый Shift + F9**, `/chatsound on|off`: включить или выключить все звуки.
- `/chatsound global|local|mentions on|off`: глобальный чат, локальный чат, упоминания.
- `/chatsound volume 70`: общая громкость; `/chatsound volume local 50`: громкость локального чата.
- `/chatsound sound global Chime`: выбрать звук; `/chatsound sounds`: список звуков; `/chatsound test`: прослушать.
- `/chatsound when global notviewing`: когда играть (`always`: всегда, `notviewing`: если вкладка не видна, `unfocused`: если игра в фоне).
- `/chatsound keyword add марк, маркус`: слова-упоминания (`remove`, `list`, `clear`).
- `/chatsound status`: текущие настройки, `/chatsound help`: все команды. Короткий вариант команды: `/csnd`.

**Свои звуки:** положите файлы в `BepInEx/config/ChatSounds/` (кнопка **Папка звуков** в окне настроек открывает её), нажмите **Обновить** и выберите файл стрелками рядом с названием звука. Файлы лежат в папке конфигов и не пропадают при обновлении мода.

**Настройки:** `BepInEx/config/ontogether.chatsounds.cfg`, таблица выше.
