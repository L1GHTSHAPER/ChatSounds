using System;
using System.Text;

namespace ChatSounds
{
    internal enum SoundKind
    {
        Builtin,
        Game,
        File
    }

    /// <summary>
    /// A parsed Sound setting: a built-in name ("Ping"), a game sound ("game:CompleteTask") or an audio file
    /// ("file:ding.ogg", or just "ding.ogg"). Plain .NET, no Unity types.
    /// </summary>
    internal readonly struct SoundRef
    {
        public const string GamePrefix = "game:";
        public const string FilePrefix = "file:";

        static readonly string[] AudioExtensions = { ".wav", ".ogg", ".mp3" };

        public readonly SoundKind Kind;
        public readonly string Name;

        SoundRef(SoundKind kind, string name)
        {
            Kind = kind;
            Name = name;
        }

        public static SoundRef Parse(string value)
        {
            string text = (value ?? string.Empty).Trim();
            if (text.StartsWith(GamePrefix, StringComparison.OrdinalIgnoreCase))
                return new SoundRef(SoundKind.Game, text.Substring(GamePrefix.Length).Trim());
            if (text.StartsWith(FilePrefix, StringComparison.OrdinalIgnoreCase))
                return new SoundRef(SoundKind.File, Unquote(text.Substring(FilePrefix.Length).Trim()));
            if (IsAudioFileName(text))
                return new SoundRef(SoundKind.File, Unquote(text));
            return new SoundRef(SoundKind.Builtin, text);
        }

        public static bool IsAudioFileName(string path)
        {
            if (string.IsNullOrEmpty(path))
                return false;
            string trimmed = path.Trim().Trim('"');
            foreach (string extension in AudioExtensions)
            {
                if (trimmed.EndsWith(extension, StringComparison.OrdinalIgnoreCase))
                    return true;
            }
            return false;
        }

        /// <summary>"FishAlertSound" -> "Fish Alert", "UIClick" -> "UI Click", "Bell2" -> "Bell 2".</summary>
        public static string Friendly(string identifier)
        {
            if (string.IsNullOrEmpty(identifier))
                return string.Empty;
            string name = identifier.Length > 5 && identifier.EndsWith("Sound", StringComparison.Ordinal)
                ? identifier.Substring(0, identifier.Length - 5)
                : identifier;
            var text = new StringBuilder(name.Length + 4);
            for (int i = 0; i < name.Length; i++)
            {
                char c = name[i];
                if (i > 0)
                {
                    char previous = name[i - 1];
                    bool lowerFollows = i + 1 < name.Length && char.IsLower(name[i + 1]);
                    bool wordStart = char.IsUpper(c) &&
                                     (char.IsLower(previous) || char.IsDigit(previous) || (char.IsUpper(previous) && lowerFollows));
                    bool numberStart = char.IsDigit(c) && !char.IsDigit(previous);
                    if (wordStart || numberStart)
                        text.Append(' ');
                }
                text.Append(c);
            }
            return text.ToString();
        }

        static string Unquote(string text)
        {
            return text.Length >= 2 && text[0] == '"' && text[text.Length - 1] == '"' ? text.Substring(1, text.Length - 2) : text;
        }
    }
}
