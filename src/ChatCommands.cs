using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using BepInEx.Bootstrap;
using HarmonyLib;
using TMPro;

namespace ChatSounds
{
    /// <summary>
    /// Client-side chat command /chatsound (aliases /chatsounds, /csnd). Handled commands are never sent to other players.
    /// </summary>
    internal static class ChatCommands
    {
        public const string CommandApiGuid = "com.on-together-mods.commandapi";

        static readonly string[] Names = { "chatsound", "chatsounds", "csnd" };

        /// <summary>Returns true when the text was one of our commands (and has been executed).</summary>
        public static bool TryExecute(string text)
        {
            if (string.IsNullOrEmpty(text) || text[0] != '/')
                return false;
            string[] parts = text.Substring(1).Split((char[])null, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 0 || Array.IndexOf(Names, parts[0].ToLowerInvariant()) < 0)
                return false;

            Execute(parts.Skip(1).ToArray());
            return true;
        }

        public static void Execute(string[] args)
        {
            Plugin plugin = Plugin.Instance;
            if (plugin == null)
                return;
            Lang lang = Lang.Current;
            if (args.Length == 0)
            {
                plugin.ToggleWindow();
                return;
            }

            string action = args[0].ToLowerInvariant();
            switch (action)
            {
                case "on":
                    plugin.SetEnabled(true, true);
                    return;
                case "off":
                    plugin.SetEnabled(false, true);
                    return;
                case "toggle":
                    plugin.SetEnabled(!plugin.Enabled.Value, true);
                    return;
                case "ui":
                case "window":
                case "settings":
                    plugin.ToggleWindow();
                    return;
                case "volume":
                case "vol":
                    Volume(plugin, lang, args);
                    return;
                case "sound":
                    SetSound(plugin, lang, args);
                    return;
                case "sounds":
                case "list":
                    plugin.Sounds.Rescan();
                    GameAccess.Notify(string.Format(lang.SoundsList, string.Join(", ", plugin.Sounds.Choices())));
                    return;
                case "test":
                case "play":
                    Test(plugin, lang, args);
                    return;
                case "when":
                    When(plugin, lang, args);
                    return;
                case "keyword":
                case "keywords":
                case "kw":
                    Keyword(plugin, lang, args);
                    return;
                case "reload":
                case "rescan":
                    GameAccess.Notify(string.Format(lang.Rescanned, plugin.Sounds.Rescan()));
                    return;
                case "status":
                    GameAccess.Notify(plugin.DescribeStatus(lang));
                    return;
            }

            SoundCategory category = ParseCategory(plugin, action);
            if (category != null)
                Switch(lang, category, args);
            else
                GameAccess.Notify(string.Format(lang.Help, plugin.WindowKey.Value, plugin.ToggleKey.Value));
        }

        // /chatsound global | local | mentions [on | off]
        static void Switch(Lang lang, SoundCategory category, string[] args)
        {
            string value = args.Length > 1 ? args[1].ToLowerInvariant() : "toggle";
            bool enabled;
            switch (value)
            {
                case "toggle":
                    enabled = !category.Enabled.Value;
                    break;
                case "on":
                    enabled = true;
                    break;
                case "off":
                    enabled = false;
                    break;
                default:
                    GameAccess.Notify(lang.UsageSwitch);
                    return;
            }
            category.Enabled.Value = enabled;
            GameAccess.Notify(string.Format(enabled ? lang.CategoryOn : lang.CategoryOff, category.Title(lang)));
        }

        // /chatsound volume 70 | /chatsound volume global 70
        static void Volume(Plugin plugin, Lang lang, string[] args)
        {
            SoundCategory category = args.Length > 2 ? ParseCategory(plugin, args[1]) : null;
            string number = args.Length > 2 ? args[2] : args.Length > 1 ? args[1] : null;
            if ((args.Length > 2 && category == null) || !TryParsePercent(number, out int percent))
            {
                GameAccess.Notify(lang.UsageVolume);
                return;
            }
            if (category == null)
            {
                plugin.MasterVolume.Value = percent;
                GameAccess.Notify(string.Format(lang.MasterVolumeSet, percent));
                return;
            }
            category.Volume.Value = percent;
            GameAccess.Notify(string.Format(lang.VolumeSet, category.Title(lang), percent));
            plugin.Preview(category);
        }

        // /chatsound sound local Bell | /chatsound sound mentions file:ding.ogg
        static void SetSound(Plugin plugin, Lang lang, string[] args)
        {
            SoundCategory category = args.Length > 2 ? ParseCategory(plugin, args[1]) : null;
            if (category == null)
            {
                GameAccess.Notify(lang.UsageSound);
                return;
            }
            plugin.Sounds.Rescan();
            string input = string.Join(" ", args, 2, args.Length - 2);
            string sound = plugin.Sounds.Canonicalize(input);
            if (sound == null)
            {
                GameAccess.Notify(string.Format(lang.UnknownSound, input));
                return;
            }
            category.Sound.Value = sound;
            GameAccess.Notify(string.Format(lang.SoundSet, category.Title(lang), plugin.Sounds.DisplayName(sound, lang)));
            plugin.Preview(category);
        }

        // /chatsound test | /chatsound test local | /chatsound test Bell
        static void Test(Plugin plugin, Lang lang, string[] args)
        {
            if (args.Length < 2)
            {
                plugin.PreviewAll();
                return;
            }
            SoundCategory category = ParseCategory(plugin, args[1]);
            if (category != null)
            {
                plugin.Preview(category);
                return;
            }
            plugin.Sounds.Rescan();
            string input = string.Join(" ", args, 1, args.Length - 1);
            string sound = plugin.Sounds.Canonicalize(input);
            if (sound == null)
                GameAccess.Notify(string.Format(lang.UnknownSound, input));
            else
                plugin.Sounds.Play(sound, plugin.EffectiveVolume(plugin.Local));
        }

        // /chatsound when global notviewing
        static void When(Plugin plugin, Lang lang, string[] args)
        {
            SoundCategory category = args.Length > 2 ? ParseCategory(plugin, args[1]) : null;
            if (category == null || !TryParseWhen(args[2], out PlayWhen when))
            {
                GameAccess.Notify(lang.UsageWhen);
                return;
            }
            category.When.Value = when;
            GameAccess.Notify(string.Format(lang.WhenSet, category.Title(lang), lang.Describe(when)));
        }

        // /chatsound keyword add mark, markus | remove mark | list | clear
        static void Keyword(Plugin plugin, Lang lang, string[] args)
        {
            string action = args.Length > 1 ? args[1].ToLowerInvariant() : "list";
            List<string> given = MentionMatcher.ParseKeywords(args.Length > 2 ? string.Join(" ", args, 2, args.Length - 2) : null);
            List<string> words = MentionMatcher.ParseKeywords(plugin.Keywords.Value);
            switch (action)
            {
                case "add":
                    if (given.Count == 0)
                        break;
                    foreach (string word in given)
                    {
                        if (!words.Exists(w => string.Equals(w, word, StringComparison.OrdinalIgnoreCase)))
                            words.Add(word);
                    }
                    plugin.Keywords.Value = MentionMatcher.JoinKeywords(words);
                    GameAccess.Notify(string.Format(lang.KeywordAdded, MentionMatcher.JoinKeywords(given)));
                    return;
                case "remove":
                case "delete":
                    if (given.Count == 0)
                        break;
                    int removed = 0;
                    foreach (string word in given)
                        removed += words.RemoveAll(w => string.Equals(w, word, StringComparison.OrdinalIgnoreCase));
                    if (removed == 0)
                    {
                        GameAccess.Notify(string.Format(lang.KeywordMissing, MentionMatcher.JoinKeywords(given)));
                        return;
                    }
                    plugin.Keywords.Value = MentionMatcher.JoinKeywords(words);
                    GameAccess.Notify(string.Format(lang.KeywordRemoved, MentionMatcher.JoinKeywords(given)));
                    return;
                case "clear":
                    plugin.Keywords.Value = string.Empty;
                    GameAccess.Notify(lang.KeywordsCleared);
                    return;
                case "list":
                    GameAccess.Notify(string.Format(lang.KeywordsList,
                        words.Count > 0 ? MentionMatcher.JoinKeywords(words) : lang.None));
                    return;
            }
            GameAccess.Notify(lang.UsageKeyword);
        }

        static SoundCategory ParseCategory(Plugin plugin, string text)
        {
            switch (text.ToLowerInvariant())
            {
                case "global":
                case "g":
                    return plugin.Global;
                case "local":
                case "l":
                    return plugin.Local;
                case "mentions":
                case "mention":
                case "m":
                    return plugin.Mentions;
                default:
                    return null;
            }
        }

        static bool TryParseWhen(string text, out PlayWhen when)
        {
            switch (text.ToLowerInvariant())
            {
                case "always":
                    when = PlayWhen.Always;
                    return true;
                case "notviewing":
                case "hidden":
                    when = PlayWhen.NotViewing;
                    return true;
                case "unfocused":
                case "background":
                    when = PlayWhen.Unfocused;
                    return true;
                default:
                    when = PlayWhen.Always;
                    return false;
            }
        }

        static bool TryParsePercent(string text, out int percent)
        {
            percent = 0;
            if (string.IsNullOrEmpty(text) || !int.TryParse(text.TrimEnd('%'), out percent))
                return false;
            percent = Math.Max(0, Math.Min(100, percent));
            return true;
        }

        /// <summary>
        /// When CommandAPI is installed, register the command there too so it is listed by its /help and
        /// suggested by CommandTypeahead. Execution still goes through our own prefix, which runs first.
        /// </summary>
        public static void RegisterWithCommandApi()
        {
            if (!Chainloader.PluginInfos.TryGetValue(CommandApiGuid, out BepInEx.PluginInfo info) || info.Instance == null)
                return;
            try
            {
                Assembly assembly = info.Instance.GetType().Assembly;
                Type registry = assembly.GetType("CommandAPI.CommandRegistry");
                Type parameterType = assembly.GetType("CommandAPI.Parameter");
                Type parameterKind = assembly.GetType("CommandAPI.ParameterType");
                if (registry == null || parameterType == null || parameterKind == null)
                    return;
                MethodInfo register = registry.GetMethod("Register", new[]
                {
                    typeof(string), typeof(string), typeof(Action<string[]>), typeof(string), parameterType.MakeArrayType()
                });
                if (register == null)
                    return;

                object stringKind = Enum.Parse(parameterKind, "String");
                Array parameters = Array.CreateInstance(parameterType, 2);
                parameters.SetValue(Activator.CreateInstance(parameterType, "action", stringKind, true, null, null), 0);
                parameters.SetValue(Activator.CreateInstance(parameterType, "value", stringKind, true, null, null), 1);

                var handler = new Action<string[]>(Execute);
                foreach (string name in Names)
                {
                    register.Invoke(null, new object[]
                    {
                        name, Plugin.PluginName, handler,
                        "Chat message sounds: settings window, on/off, volume, sound, test, keywords, status", parameters
                    });
                }
                Plugin.Log.LogInfo("Registered /chatsound with CommandAPI.");
            }
            catch (Exception e)
            {
                Plugin.Log.LogWarning("Could not register with CommandAPI (the command still works): " + e.Message);
            }
        }
    }

    [HarmonyPatch(typeof(TextChannelManager), nameof(TextChannelManager.OnEnterPressed))]
    internal static class ChatCommandPatch
    {
        // Runs before other mods' command handlers. A handled command clears the input field, so the game
        // and the other prefixes see an empty message: nothing is sent, and the game still releases the
        // input lock and deselects the field as it does after every Enter.
        [HarmonyPrefix]
        [HarmonyPriority(Priority.First)]
        static void Prefix()
        {
            try
            {
                UIManager ui = MonoSingleton<UIManager>.I;
                TMP_InputField input = ui != null ? ui.MessageInput : null;
                if (input != null && ChatCommands.TryExecute(input.text))
                    input.text = string.Empty;
            }
            catch (Exception e)
            {
                Plugin.Log.LogError("Chat command failed: " + e);
            }
        }
    }
}
