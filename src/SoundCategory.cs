using BepInEx.Configuration;

namespace ChatSounds
{
    /// <summary>Settings and cooldown of one kind of chat sound: global chat, local chat or mentions.</summary>
    internal sealed class SoundCategory
    {
        const string PlayWhenHelp =
            "When to play:\n" +
            "Always;\n" +
            "NotViewing - only when you cannot see the message's chat tab right now (the other tab is open, the chat " +
            "is hidden or the game window is in the background);\n" +
            "Unfocused - only while the game window is in the background.";

        public readonly string Section;
        public readonly ConfigEntry<bool> Enabled;
        public readonly ConfigEntry<string> Sound;
        public readonly ConfigEntry<int> Volume;
        public readonly ConfigEntry<PlayWhen> When;
        public readonly ConfigEntry<float> Cooldown;

        float _lastPlayed = -1e6f;

        public SoundCategory(ConfigFile config, string section, string enabledHelp, string sound, int volume, float cooldown)
        {
            Section = section;
            Enabled = config.Bind(section, "Enabled", true, enabledHelp);
            Sound = config.Bind(section, "Sound", sound, SoundLibrary.ConfigHelp);
            Volume = config.Bind(section, "Volume", volume,
                new ConfigDescription("Volume in percent (multiplied by General.MasterVolume).",
                    new AcceptableValueRange<int>(0, 100)));
            When = config.Bind(section, "PlayWhen", PlayWhen.Always, PlayWhenHelp);
            Cooldown = config.Bind(section, "Cooldown", cooldown,
                new ConfigDescription("Minimum seconds between two of these sounds; messages that arrive sooner stay silent.",
                    new AcceptableValueRange<float>(0f, 60f)));
        }

        public string Title(Lang lang)
        {
            switch (Section)
            {
                case "Global":
                    return lang.GlobalChat;
                case "Local":
                    return lang.LocalChat;
                default:
                    return lang.Mentions;
            }
        }

        /// <summary>Starts the cooldown and returns true, unless this kind of sound played too recently.</summary>
        public bool TryStartCooldown(float now)
        {
            if (now - _lastPlayed < Cooldown.Value)
                return false;
            _lastPlayed = now;
            return true;
        }
    }
}
