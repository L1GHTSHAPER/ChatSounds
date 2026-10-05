using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;

namespace ChatSounds
{
    /// <summary>When a kind of chat sound is allowed to play.</summary>
    public enum PlayWhen
    {
        Always,
        NotViewing,
        Unfocused
    }

    public enum UiLanguage
    {
        Auto,
        English,
        Russian
    }

    [BepInPlugin(PluginGuid, PluginName, PluginVersion)]
    [BepInProcess("OnTogether.exe")]
    [BepInDependency(ChatCommands.CommandApiGuid, BepInDependency.DependencyFlags.SoftDependency)]
    public sealed class Plugin : BaseUnityPlugin
    {
        public const string PluginGuid = "ontogether.chatsounds";
        public const string PluginName = "ChatSounds";
        public const string PluginVersion = "1.0.1";

        // The config file is checked this often for edits made in the mod manager while the game is running.
        const float ConfigPollInterval = 1f;
        // A changed file is reloaded once it has stayed unchanged this long (the editor may still be writing it).
        const float ConfigSettleTime = 0.5f;
        // Changes made in game are written this long after the last one, so dragging a slider writes the file once.
        const float SaveDelay = 0.75f;
        // A config file that could not be read or written (e.g. locked by another program) is tried again after this.
        const float RetryDelay = 2f;

        internal static Plugin Instance { get; private set; }
        internal static ManualLogSource Log { get; private set; }

        internal ConfigEntry<bool> Enabled;
        internal ConfigEntry<int> MasterVolume;
        internal ConfigEntry<bool> FollowGameVolume;
        internal ConfigEntry<bool> QuietDuringFocus;
        internal ConfigEntry<UiLanguage> Language;
        internal ConfigEntry<KeyboardShortcut> WindowKey;
        internal ConfigEntry<KeyboardShortcut> ToggleKey;
        internal ConfigEntry<bool> MatchMyName;
        internal ConfigEntry<string> Keywords;

        internal SoundCategory Global;
        internal SoundCategory Local;
        internal SoundCategory Mentions;
        internal SoundCategory[] Categories;

        internal SoundLibrary Sounds { get; private set; }
        internal IList<string> KeywordList => _keywords;

        GameObject _host;
        SettingsWindow _window;
        Harmony _harmony;
        List<string> _keywords = new List<string>();
        // Settings changed in game and not saved yet, with their new values. When the file is edited in the mod
        // manager at the same time, these win and every other setting takes the value from the file.
        readonly Dictionary<ConfigEntryBase, object> _unsaved = new Dictionary<ConfigEntryBase, object>();
        bool _reloading;
        float _saveAt = -1f;
        float _nextConfigPoll;
        float _reloadAt = -1f;
        DateTime _configStamp;
        DateTime _pendingStamp;
        bool _fileErrorLogged;
        string _lastError;

        void Awake()
        {
            Instance = this;
            Log = Logger;
            BindConfig();
            // Bind() has written any missing keys; from now on saves are batched (see SaveDelay).
            Config.SaveOnConfigSet = false;
            _keywords = MentionMatcher.ParseKeywords(Keywords.Value);
            _configStamp = ConfigStamp();

            _host = new GameObject("ChatSounds") { hideFlags = HideFlags.HideAndDontSave };
            DontDestroyOnLoad(_host);
            Sounds = _host.AddComponent<SoundLibrary>();
            Sounds.Init(Path.Combine(Paths.ConfigPath, "ChatSounds"));
            _window = _host.AddComponent<SettingsWindow>();
            _window.enabled = false;

            _harmony = new Harmony(PluginGuid);
            Patch(typeof(MessagePatch), "no sounds will play");
            Patch(typeof(ChatCommandPatch), "the /chatsound command will not work");
            ChatCommands.RegisterWithCommandApi();

            Config.SettingChanged += OnSettingChanged;
            foreach (SoundCategory category in Categories)
                Sounds.Preload(category.Sound.Value);
            Log.LogInfo($"{PluginName} {PluginVersion} loaded. Settings window: {WindowKey.Value} or /chatsound");
        }

        void BindConfig()
        {
            Enabled = Config.Bind("General", "Enabled", true,
                "Play a sound when another player's chat message arrives.");
            MasterVolume = Config.Bind("General", "MasterVolume", 80,
                new ConfigDescription("Volume of all chat sounds, in percent.", new AcceptableValueRange<int>(0, 100)));
            FollowGameVolume = Config.Bind("General", "FollowGameVolume", true,
                "Also scale chat sounds by the game's Master volume setting.");
            QuietDuringFocus = Config.Bind("General", "QuietDuringFocus", false,
                "While you are in a focus session, only mentions make a sound.");
            Language = Config.Bind("General", "Language", UiLanguage.Auto,
                "Language of the settings window and of the mod's chat messages. Auto follows the game's language.");

            WindowKey = Config.Bind("Hotkeys", "SettingsWindow", new KeyboardShortcut(KeyCode.F9),
                "Opens or closes the settings window (same as typing /chatsound). Ignored while typing.");
            ToggleKey = Config.Bind("Hotkeys", "ToggleSounds", new KeyboardShortcut(KeyCode.F9, KeyCode.LeftShift),
                "Turns all chat sounds on or off. Ignored while typing.");

            Global = new SoundCategory(Config, "Global",
                "Play a sound for new messages in the global chat.", "Ping", 60, 3f);
            Local = new SoundCategory(Config, "Local",
                "Play a sound for new messages in the local chat (players near you).", "Bubble", 80, 1f);
            Mentions = new SoundCategory(Config, "Mentions",
                "Play a separate sound when a message mentions you (in either chat). For that message it replaces " +
                "the global/local sound.", "Alert", 100, 1f);
            MatchMyName = Config.Bind("Mentions", "MatchMyName", true,
                "A message that contains your in-game name counts as a mention.");
            Keywords = Config.Bind("Mentions", "Keywords", "",
                "More words that count as a mention, separated by commas, e.g. nicknames: mark, markus. " +
                "Case does not matter; only whole words match.");
            Categories = new[] { Global, Local, Mentions };
        }

        void Patch(Type patchClass, string consequence)
        {
            try
            {
                _harmony.CreateClassProcessor(patchClass).Patch();
            }
            catch (Exception e)
            {
                Log.LogError($"Could not patch the game ({consequence}): {e}");
            }
        }

        void Update()
        {
            try
            {
                HandleHotkeys();
                PollConfigFile();
                if (_saveAt >= 0f && Time.unscaledTime >= _saveAt)
                    SaveNow();
            }
            catch (Exception e)
            {
                LogOnce("Update failed: ", e);
            }
        }

        void HandleHotkeys()
        {
            KeyboardShortcut toggleKey = ToggleKey.Value;
            KeyboardShortcut windowKey = WindowKey.Value;
            bool toggle = Pressed(toggleKey);
            bool window = Pressed(windowKey);
            if (!toggle && !window)
                return;
            // Shift+F9 also satisfies a plain F9 shortcut: the shortcut with more modifiers wins.
            if (toggle && window)
            {
                if (toggleKey.Modifiers.Count() >= windowKey.Modifiers.Count())
                    window = false;
                else
                    toggle = false;
            }
            if (GameAccess.IsAnyTextFieldFocused())
                return;
            if (toggle)
                SetEnabled(!Enabled.Value, true);
            else
                ToggleWindow();
        }

        // KeyboardShortcut.IsDown() does not fire while any other key is held (e.g. W while walking), so only the
        // shortcut's own keys are checked.
        static bool Pressed(KeyboardShortcut shortcut)
        {
            KeyCode mainKey = shortcut.MainKey;
            if (mainKey == KeyCode.None || !Input.GetKeyDown(mainKey))
                return false;
            foreach (KeyCode modifier in shortcut.Modifiers)
            {
                if (!Input.GetKey(modifier))
                    return false;
            }
            return true;
        }

        /// <summary>Called after the game has added another player's message to the chat panel.</summary>
        internal void OnMessageShown(string text, bool isLocal)
        {
            try
            {
                if (!Enabled.Value)
                    return;
                float now = Time.realtimeSinceStartup;
                // A mention plays its own sound instead of the channel's; when it does not play (condition,
                // cooldown, zero volume) the channel's own rules apply.
                if (Mentions.Enabled.Value && IsMention(text) && TryPlay(Mentions, isLocal, now))
                    return;
                if (QuietDuringFocus.Value && GameAccess.IsLocalPlayerFocusing())
                    return;
                SoundCategory channel = isLocal ? Local : Global;
                if (channel.Enabled.Value)
                    TryPlay(channel, isLocal, now);
            }
            catch (Exception e)
            {
                LogOnce("Could not play a chat sound: ", e);
            }
        }

        bool IsMention(string text)
        {
            string name = MatchMyName.Value ? GameAccess.LocalPlayerName : null;
            return MentionMatcher.IsMention(text, name, _keywords);
        }

        bool TryPlay(SoundCategory category, bool isLocal, float now)
        {
            if (!IsAllowed(category.When.Value, isLocal))
                return false;
            float volume = EffectiveVolume(category);
            if (volume <= 0f || !category.TryStartCooldown(now))
                return false;
            Sounds.Play(category.Sound.Value, volume);
            return true;
        }

        static bool IsAllowed(PlayWhen when, bool isLocal)
        {
            switch (when)
            {
                case PlayWhen.NotViewing:
                    return !GameAccess.IsChannelVisible(isLocal);
                case PlayWhen.Unfocused:
                    return !Application.isFocused;
                default:
                    return true;
            }
        }

        internal float EffectiveVolume(SoundCategory category)
        {
            float volume = MasterVolume.Value / 100f * (category.Volume.Value / 100f);
            if (FollowGameVolume.Value)
                volume *= GameAccess.GameMasterVolume;
            return Mathf.Clamp01(volume);
        }

        /// <summary>Plays a category's sound as it would sound for a message, ignoring its conditions.</summary>
        internal void Preview(SoundCategory category)
        {
            Sounds.Play(category.Sound.Value, EffectiveVolume(category));
        }

        internal void PreviewAll()
        {
            StartCoroutine(PreviewSequence());
        }

        IEnumerator PreviewSequence()
        {
            foreach (SoundCategory category in Categories)
            {
                Preview(category);
                yield return new WaitForSecondsRealtime(1f);
            }
        }

        internal void SetEnabled(bool enabled, bool notify)
        {
            Enabled.Value = enabled;
            if (notify)
                GameAccess.Notify(enabled ? Lang.Current.SoundsOn : Lang.Current.SoundsOff);
        }

        internal void ToggleWindow()
        {
            _window.Toggle();
        }

        internal string DescribeStatus(Lang lang)
        {
            var text = new StringBuilder();
            text.AppendFormat(lang.StatusFormat, Enabled.Value ? lang.On : lang.Off, MasterVolume.Value);
            foreach (SoundCategory category in Categories)
            {
                text.Append('\n').AppendFormat(lang.CategoryStatus, category.Title(lang),
                    category.Enabled.Value ? lang.On : lang.Off, Sounds.DisplayName(category.Sound.Value, lang),
                    category.Volume.Value, lang.Describe(category.When.Value));
            }
            string name = MentionMatcher.StripTags(GameAccess.LocalPlayerName);
            text.Append('\n').AppendFormat(lang.MentionStatus,
                MatchMyName.Value && name.Length > 0 ? name : lang.None,
                _keywords.Count > 0 ? MentionMatcher.JoinKeywords(_keywords) : lang.None);
            return text.ToString();
        }

        void OnSettingChanged(object sender, SettingChangedEventArgs e)
        {
            ConfigEntryBase changed = e.ChangedSetting;
            if (!_reloading)
            {
                _unsaved[changed] = changed.BoxedValue;
                _saveAt = Time.unscaledTime + SaveDelay;
            }

            if (changed == Keywords)
            {
                _keywords = MentionMatcher.ParseKeywords(Keywords.Value);
                return;
            }
            if (changed == Language)
            {
                Lang.Invalidate();
                return;
            }
            foreach (SoundCategory category in Categories)
            {
                if (changed == category.Sound)
                    Sounds.Preload(category.Sound.Value);
            }
        }

        /// <summary>Applies edits made to the config file while the game is running (e.g. in the mod manager).</summary>
        void PollConfigFile()
        {
            float now = Time.unscaledTime;
            if (_reloadAt >= 0f)
            {
                if (now < _reloadAt)
                    return;
                DateTime stamp = ConfigStamp();
                if (stamp == _configStamp)
                {
                    // Already read, or replaced by our own save in the meantime.
                    _reloadAt = -1f;
                    return;
                }
                if (stamp != _pendingStamp)
                {
                    _pendingStamp = stamp;
                    _reloadAt = now + ConfigSettleTime;
                    return;
                }
                _reloadAt = TryReloadConfig() ? -1f : now + RetryDelay;
                return;
            }

            if (now < _nextConfigPoll)
                return;
            _nextConfigPoll = now + ConfigPollInterval;
            DateTime current = ConfigStamp();
            if (current == _configStamp)
                return;
            _pendingStamp = current;
            _reloadAt = now + ConfigSettleTime;
        }

        /// <summary>
        /// Reads the config file again, keeping the settings changed in game that are not saved yet.
        /// Returns false when the file could not be read.
        /// </summary>
        bool TryReloadConfig()
        {
            if (!File.Exists(Config.ConfigFilePath))
            {
                SaveNow();
                return true;
            }
            // Taken before reading, so a write that happens during the read is noticed next time.
            DateTime stamp = ConfigStamp();
            _reloading = true;
            try
            {
                Config.Reload();
                foreach (KeyValuePair<ConfigEntryBase, object> change in _unsaved)
                    change.Key.BoxedValue = change.Value;
            }
            catch (Exception e)
            {
                LogFileErrorOnce("Could not read the config file, will try again: " + e.Message);
                return false;
            }
            finally
            {
                _reloading = false;
            }
            _configStamp = stamp;
            _fileErrorLogged = false;
            Log.LogInfo("Settings reloaded from the config file.");
            return true;
        }

        void SaveNow()
        {
            _saveAt = -1f;
            // An edit made in the mod manager since our last save is read first, so that saving does not undo it.
            if (File.Exists(Config.ConfigFilePath) && ConfigStamp() != _configStamp && !TryReloadConfig())
            {
                _saveAt = Time.unscaledTime + RetryDelay;
                return;
            }
            try
            {
                Config.Save();
                _unsaved.Clear();
                _configStamp = ConfigStamp();
                _fileErrorLogged = false;
            }
            catch (Exception e)
            {
                LogFileErrorOnce("Could not save the settings, will try again: " + e.Message);
                _saveAt = Time.unscaledTime + RetryDelay;
            }
        }

        void LogFileErrorOnce(string message)
        {
            if (_fileErrorLogged)
                return;
            _fileErrorLogged = true;
            Log.LogWarning(message);
        }

        DateTime ConfigStamp()
        {
            try
            {
                return File.GetLastWriteTimeUtc(Config.ConfigFilePath);
            }
            catch (Exception)
            {
                return default;
            }
        }

        void LogOnce(string what, Exception e)
        {
            // Log each distinct error once instead of every frame.
            string message = what + e.GetType().Name + ": " + e.Message;
            if (message == _lastError)
                return;
            _lastError = message;
            Log.LogError(what + e);
        }

        void OnApplicationQuit()
        {
            if (_saveAt >= 0f)
                SaveNow();
        }

        void OnDestroy()
        {
            Config.SettingChanged -= OnSettingChanged;
            if (_saveAt >= 0f)
                SaveNow();
            if (_harmony != null)
                _harmony.UnpatchSelf();
            if (_host != null)
                Destroy(_host);
        }
    }
}
