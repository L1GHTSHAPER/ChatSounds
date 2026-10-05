using System;
using System.Globalization;
using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.Localization;
using UnityEngine.Localization.Settings;

namespace ChatSounds
{
    /// <summary>Texts of the settings window and of the mod's chat messages (English and Russian).</summary>
    internal sealed class Lang
    {
        // Settings window
        public string Title;
        public string SoundsEnabled;
        public string MasterVolume;
        public string FollowGameVolume;
        public string QuietDuringFocus;
        public string GlobalChat;
        public string LocalChat;
        public string Mentions;
        public string Sound;
        public string Volume;
        public string When;
        public string Cooldown;
        public string NoCooldown;
        public string SecondsFormat;
        public bool DecimalComma;
        public string WhenAlways;
        public string WhenNotViewing;
        public string WhenUnfocused;
        public string MatchMyName;
        public string MatchMyNameFormat;
        public string Keywords;
        public string None;
        public string KeywordsHint;
        public string OpenFolder;
        public string Rescan;
        public string Close;
        public string WindowHint;
        public string FilesHint;
        public string GamePrefix;
        public string FilePrefix;
        // Parallel to Synth.Names.
        public string[] SynthNames;

        // Chat messages
        public string On;
        public string Off;
        public string SoundsOn;
        public string SoundsOff;
        public string CategoryOn;
        public string CategoryOff;
        public string MasterVolumeSet;
        public string VolumeSet;
        public string SoundSet;
        public string WhenSet;
        public string UnknownSound;
        public string SoundsList;
        public string KeywordAdded;
        public string KeywordRemoved;
        public string KeywordMissing;
        public string KeywordsList;
        public string KeywordsCleared;
        public string Rescanned;
        public string StatusFormat;
        public string CategoryStatus;
        public string MentionStatus;
        public string UsageSwitch;
        public string UsageVolume;
        public string UsageSound;
        public string UsageWhen;
        public string UsageKeyword;
        public string Help;

        public static readonly Lang English = new Lang
        {
            Title = "Chat Sounds",
            SoundsEnabled = "Sounds for new chat messages",
            MasterVolume = "Master volume",
            FollowGameVolume = "Follow the game's master volume",
            QuietDuringFocus = "During focus sessions, only mentions",
            GlobalChat = "Global chat",
            LocalChat = "Local chat",
            Mentions = "Mentions",
            Sound = "Sound",
            Volume = "Volume",
            When = "Play",
            Cooldown = "Cooldown",
            NoCooldown = "off",
            SecondsFormat = "{0} s",
            WhenAlways = "Always",
            WhenNotViewing = "When the tab isn't visible",
            WhenUnfocused = "When the game is in background",
            MatchMyName = "Your name",
            MatchMyNameFormat = "Your name ({0})",
            Keywords = "Keywords",
            None = "none",
            KeywordsHint = "Change with /chatsound keyword add | remove <word>",
            OpenFolder = "Sounds folder",
            Rescan = "Rescan",
            Close = "Close",
            WindowHint = "{0}: this window · {1}: all sounds on/off · /chatsound help: commands",
            FilesHint = "Your sound files: {0} (.wav, .ogg, .mp3 in BepInEx/config/ChatSounds)",
            GamePrefix = "Game: ",
            FilePrefix = "File: ",
            SynthNames = new[] { "Ping", "Chime", "Bubble", "Drop", "Marimba", "Bell", "Soft", "Alert" },

            On = "on",
            Off = "off",
            SoundsOn = "Chat sounds: on",
            SoundsOff = "Chat sounds: off",
            CategoryOn = "{0}: sound on",
            CategoryOff = "{0}: sound off",
            MasterVolumeSet = "Master volume: {0}%",
            VolumeSet = "{0}: volume {1}%",
            SoundSet = "{0}: sound {1}",
            WhenSet = "{0}: {1}",
            UnknownSound = "Unknown sound \"{0}\". /chatsound sounds lists them all",
            SoundsList = "Sounds: {0}",
            KeywordAdded = "Keyword added: {0}",
            KeywordRemoved = "Keyword removed: {0}",
            KeywordMissing = "No such keyword: {0}",
            KeywordsList = "Keywords: {0}",
            KeywordsCleared = "Keywords cleared",
            Rescanned = "Sound files found: {0}",
            StatusFormat = "Chat sounds: {0}, master volume {1}%",
            CategoryStatus = "{0}: {1}, {2}, {3}%, {4}",
            MentionStatus = "Mentions match: name {0}; keywords {1}",
            UsageSwitch = "Usage: /chatsound global | local | mentions [on | off]",
            UsageVolume = "Usage: /chatsound volume [global | local | mentions] <0-100>",
            UsageSound = "Usage: /chatsound sound <global | local | mentions> <sound>",
            UsageWhen = "Usage: /chatsound when <global | local | mentions> always | notviewing | unfocused",
            UsageKeyword = "Usage: /chatsound keyword add | remove <word>, /chatsound keyword list | clear",
            Help =
                "Chat sounds, commands:\n" +
                "/chatsound - settings window ({0})\n" +
                "/chatsound on | off - all sounds ({1})\n" +
                "/chatsound global | local | mentions [on | off]\n" +
                "/chatsound volume [global | local | mentions] <0-100>\n" +
                "/chatsound sound <global | local | mentions> <sound>\n" +
                "/chatsound sounds - list of sounds\n" +
                "/chatsound test [global | local | mentions | <sound>]\n" +
                "/chatsound when <global | local | mentions> always | notviewing | unfocused\n" +
                "/chatsound keyword add | remove <word>, keyword list | clear\n" +
                "/chatsound reload - rescan your sound files\n" +
                "/chatsound status"
        };

        public static readonly Lang Russian = new Lang
        {
            Title = "Звуки чата",
            SoundsEnabled = "Звуки новых сообщений",
            MasterVolume = "Общая громкость",
            FollowGameVolume = "Учитывать общую громкость игры",
            QuietDuringFocus = "Во время фокуса только упоминания",
            GlobalChat = "Глобальный чат",
            LocalChat = "Локальный чат",
            Mentions = "Упоминания",
            Sound = "Звук",
            Volume = "Громкость",
            When = "Когда",
            Cooldown = "Пауза",
            NoCooldown = "нет",
            SecondsFormat = "{0} с",
            DecimalComma = true,
            WhenAlways = "Всегда",
            WhenNotViewing = "Если вкладка не видна",
            WhenUnfocused = "Если игра в фоне",
            MatchMyName = "Ваше имя",
            MatchMyNameFormat = "Ваше имя ({0})",
            Keywords = "Слова",
            None = "нет",
            KeywordsHint = "Изменить: /chatsound keyword add | remove <слово>",
            OpenFolder = "Папка звуков",
            Rescan = "Обновить",
            Close = "Закрыть",
            WindowHint = "{0}: это окно · {1}: вкл/выкл все звуки · /chatsound help: команды",
            FilesHint = "Ваших звуковых файлов: {0} (.wav, .ogg, .mp3 в BepInEx/config/ChatSounds)",
            GamePrefix = "Игра: ",
            FilePrefix = "Файл: ",
            SynthNames = new[] { "Пинг", "Перезвон", "Пузырьки", "Капля", "Маримба", "Колокол", "Мягкий", "Сигнал" },

            On = "вкл",
            Off = "выкл",
            SoundsOn = "Звуки чата включены",
            SoundsOff = "Звуки чата выключены",
            CategoryOn = "{0}: звук включён",
            CategoryOff = "{0}: звук выключен",
            MasterVolumeSet = "Общая громкость: {0}%",
            VolumeSet = "{0}: громкость {1}%",
            SoundSet = "{0}: звук {1}",
            WhenSet = "{0}: {1}",
            UnknownSound = "Неизвестный звук «{0}». Список: /chatsound sounds",
            SoundsList = "Звуки: {0}",
            KeywordAdded = "Добавлено: {0}",
            KeywordRemoved = "Удалено: {0}",
            KeywordMissing = "Такого слова нет: {0}",
            KeywordsList = "Слова-упоминания: {0}",
            KeywordsCleared = "Слова-упоминания удалены",
            Rescanned = "Найдено звуковых файлов: {0}",
            StatusFormat = "Звуки чата: {0}, общая громкость {1}%",
            CategoryStatus = "{0}: {1}, {2}, {3}%, {4}",
            MentionStatus = "Упоминания: имя {0}; слова {1}",
            UsageSwitch = "Использование: /chatsound global | local | mentions [on | off]",
            UsageVolume = "Использование: /chatsound volume [global | local | mentions] <0-100>",
            UsageSound = "Использование: /chatsound sound <global | local | mentions> <звук>",
            UsageWhen = "Использование: /chatsound when <global | local | mentions> always | notviewing | unfocused",
            UsageKeyword = "Использование: /chatsound keyword add | remove <слово>, /chatsound keyword list | clear",
            Help =
                "Звуки чата, команды:\n" +
                "/chatsound - окно настроек ({0})\n" +
                "/chatsound on | off - все звуки ({1})\n" +
                "/chatsound global | local | mentions [on | off] - глобальный чат, локальный чат, упоминания\n" +
                "/chatsound volume [global | local | mentions] <0-100> - громкость\n" +
                "/chatsound sound <global | local | mentions> <звук> - выбрать звук\n" +
                "/chatsound sounds - список звуков\n" +
                "/chatsound test [global | local | mentions | <звук>] - прослушать\n" +
                "/chatsound when <global | local | mentions> always | notviewing | unfocused - когда играть\n" +
                "/chatsound keyword add | remove <слово>, keyword list | clear - слова-упоминания\n" +
                "/chatsound reload - перечитать папку со звуками\n" +
                "/chatsound status - текущие настройки"
        };

        static Lang _auto = English;
        static float _nextAutoCheck;

        public static Lang Current
        {
            get
            {
                Plugin plugin = Plugin.Instance;
                UiLanguage setting = plugin != null ? plugin.Language.Value : UiLanguage.Auto;
                if (setting == UiLanguage.English)
                    return English;
                if (setting == UiLanguage.Russian)
                    return Russian;
                if (Time.unscaledTime >= _nextAutoCheck)
                {
                    _nextAutoCheck = Time.unscaledTime + 10f;
                    _auto = DetectRussian() ? Russian : English;
                }
                return _auto;
            }
        }

        /// <summary>Re-detects the automatic language on next use (e.g. after the game's language changed).</summary>
        public static void Invalidate()
        {
            _nextAutoCheck = 0f;
        }

        public string Describe(PlayWhen when)
        {
            switch (when)
            {
                case PlayWhen.NotViewing:
                    return WhenNotViewing;
                case PlayWhen.Unfocused:
                    return WhenUnfocused;
                default:
                    return WhenAlways;
            }
        }

        /// <summary>"2.5 s" / "2,5 с", independent of the system's number format.</summary>
        public string Seconds(float value)
        {
            string number = value.ToString("0.#", CultureInfo.InvariantCulture);
            return string.Format(SecondsFormat, DecimalComma ? number.Replace('.', ',') : number);
        }

        public string SoundName(string builtin)
        {
            int index = Array.IndexOf(Synth.Names, builtin);
            return index >= 0 && index < SynthNames.Length ? SynthNames[index] : builtin;
        }

        static bool DetectRussian()
        {
            try
            {
                string code = GameLocaleCode();
                if (!string.IsNullOrEmpty(code))
                    return code.StartsWith("ru", StringComparison.OrdinalIgnoreCase);
            }
            catch (Exception)
            {
                // Unity Localization is not available or not ready: use the system language.
            }
            return Application.systemLanguage == SystemLanguage.Russian;
        }

        // Separate method, so a missing Unity.Localization assembly only fails here.
        [MethodImpl(MethodImplOptions.NoInlining)]
        static string GameLocaleCode()
        {
            Locale locale = LocalizationSettings.SelectedLocale;
            return locale != null ? locale.Identifier.Code : null;
        }
    }
}
