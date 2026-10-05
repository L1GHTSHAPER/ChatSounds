using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using HarmonyLib;
using UnityEngine;
using UnityEngine.Networking;

namespace ChatSounds
{
    /// <summary>
    /// Turns Sound settings into audio clips and plays them on a 2D audio source: built-in synthesized sounds, the
    /// game's own sound effects and the player's audio files from BepInEx/config/ChatSounds.
    /// </summary>
    internal sealed class SoundLibrary : MonoBehaviour
    {
        public const string DefaultSound = "Ping";

        internal const string ConfigHelp =
            "Sound to play.\n" +
            "Built-in: Ping, Chime, Bubble, Drop, Marimba, Bell, Soft, Alert.\n" +
            "Game sounds: game:CompleteTask, game:EarnTicket, game:PetUnlock, game:FocusEnd, game:Bell1 and more " +
            "(the settings window lists all of them).\n" +
            "Your own file: put a .wav, .ogg or .mp3 file into BepInEx/config/ChatSounds and write file:<name>, " +
            "e.g. file:ding.ogg (a full path works too).";

        const string ReadmeText =
            "Put your own notification sounds here: .wav, .ogg or .mp3 files. Short sounds (under 2 seconds) work best.\r\n" +
            "Then pick them in the settings window (F9 or /chatsound in the chat; press \"Rescan\" after adding files),\r\n" +
            "or write file:<name> into a Sound setting in BepInEx/config/ontogether.chatsounds.cfg, e.g. file:ding.ogg\r\n" +
            "\r\n" +
            "Положите сюда свои звуки уведомлений: файлы .wav, .ogg или .mp3. Лучше всего подходят короткие звуки (до 2 секунд).\r\n" +
            "Затем выберите их в окне настроек (F9 или /chatsound в чате; после добавления файлов нажмите «Обновить»)\r\n" +
            "или впишите file:<имя> в параметр Sound в BepInEx/config/ontogether.chatsounds.cfg, например file:ding.ogg\r\n";

        // Single-clip fields of the game's SFXSettings that work as notification sounds.
        static readonly string[] GameClips =
        {
            "CompleteTask", "EarnTicket", "BuyItem", "PetUnlock", "FocusStart", "FocusEnd", "FishAlertSound",
            "FishSuccessSound", "FishCatchSound", "BalloonPop", "LemonadeSound", "PetAppearSound", "StoreOpen",
            "InventoryOpen", "UIClick", "UIChange", "UIError"
        };

        // Pomodoro bells (SoundManager.BellSounds) are offered as game:Bell1, game:Bell2...
        const string BellPrefix = "Bell";
        const long MaxFileBytes = 20L * 1024 * 1024;
        // WAV files up to this size are decoded at once on the main thread; larger ones by Unity in the background.
        const long MaxDirectWavBytes = 4L * 1024 * 1024;
        // A file sound that is still loading when its message arrives plays if it gets ready within this time.
        const float MaxPlayDelay = 3f;
        const float SfxLookupInterval = 2f;

        static readonly AccessTools.FieldRef<SFXManager, SFXSettings> SfxSettingsRef = TrySfxSettingsRef();

        readonly Dictionary<string, AudioClip> _builtins = new Dictionary<string, AudioClip>(StringComparer.OrdinalIgnoreCase);
        readonly Dictionary<string, FileSound> _files = new Dictionary<string, FileSound>(StringComparer.OrdinalIgnoreCase);
        readonly Dictionary<string, FieldInfo> _gameFields = new Dictionary<string, FieldInfo>(StringComparer.OrdinalIgnoreCase);
        readonly HashSet<string> _warned = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        List<string> _fileNames = new List<string>();
        AudioSource _source;
        SFXSettings _sfxSettings;
        float _nextSfxLookup;

        sealed class FileSound
        {
            public AudioClip Clip;
            public DateTime Stamp;
            public bool Loading;
            public float PendingVolume = -1f;
            public float PendingSince;
        }

        public string Folder { get; private set; }

        public int FileCount => _fileNames.Count;

        public void Init(string folder)
        {
            Folder = folder;
            _source = gameObject.AddComponent<AudioSource>();
            _source.playOnAwake = false;
            _source.loop = false;
            _source.spatialBlend = 0f;
            _source.priority = 0;
            _source.volume = 1f;
            _source.bypassEffects = true;
            _source.bypassListenerEffects = true;
            _source.bypassReverbZones = true;
            _source.ignoreListenerPause = true;
            EnsureFolder();
            Rescan();
        }

        public void Play(string value, float volume)
        {
            if (volume <= 0f)
                return;
            try
            {
                SoundRef sound = SoundRef.Parse(value);
                switch (sound.Kind)
                {
                    case SoundKind.Game:
                        AudioClip gameClip = GameClip(sound.Name);
                        PlayClip(gameClip != null ? gameClip : Fallback(value), volume);
                        break;
                    case SoundKind.File:
                        PlayFile(ResolvePath(sound.Name), value, volume);
                        break;
                    default:
                        AudioClip builtin = Builtin(sound.Name);
                        PlayClip(builtin != null ? builtin : Fallback(value), volume);
                        break;
                }
            }
            catch (Exception e)
            {
                WarnOnce("play:" + value, $"Could not play sound \"{value}\": {e}");
            }
        }

        /// <summary>Starts loading a file sound ahead of time, so its first message is not delayed.</summary>
        public void Preload(string value)
        {
            SoundRef sound = SoundRef.Parse(value);
            if (sound.Kind == SoundKind.File)
                FileEntry(ResolvePath(sound.Name));
        }

        /// <summary>Every sound that can be picked right now, as Sound setting values.</summary>
        public List<string> Choices()
        {
            var choices = new List<string>(Synth.Names);
            foreach (string name in GameClips)
            {
                if (GameClip(name) != null)
                    choices.Add(SoundRef.GamePrefix + name);
            }
            List<AudioClip> bells = Bells();
            if (bells != null)
            {
                for (int i = 0; i < bells.Count; i++)
                {
                    if (bells[i] != null)
                        choices.Add(SoundRef.GamePrefix + BellPrefix + (i + 1));
                }
            }
            foreach (string file in _fileNames)
                choices.Add(SoundRef.FilePrefix + file);
            return choices;
        }

        public string DisplayName(string value, Lang lang)
        {
            SoundRef sound = SoundRef.Parse(value);
            switch (sound.Kind)
            {
                case SoundKind.Game:
                    return lang.GamePrefix + SoundRef.Friendly(sound.Name);
                case SoundKind.File:
                    return lang.FilePrefix + SafeFileName(sound.Name);
                default:
                    string canonical = Synth.Canonical(sound.Name);
                    return canonical != null ? lang.SoundName(canonical) : sound.Name;
            }
        }

        /// <summary>
        /// Turns typed input into a Sound setting value, or returns null when there is no such sound. Besides the
        /// setting syntax it accepts a file name without extension ("ding") and a game sound without prefix.
        /// </summary>
        public string Canonicalize(string input)
        {
            SoundRef sound = SoundRef.Parse(input);
            switch (sound.Kind)
            {
                case SoundKind.Game:
                    return CanonicalGameSound(sound.Name);
                case SoundKind.File:
                    string path = ResolvePath(sound.Name);
                    return path != null && File.Exists(path) ? SoundRef.FilePrefix + sound.Name : null;
                default:
                    string canonical = Synth.Canonical(sound.Name);
                    if (canonical != null)
                        return canonical;
                    foreach (string file in _fileNames)
                    {
                        if (string.Equals(Path.GetFileNameWithoutExtension(file), sound.Name, StringComparison.OrdinalIgnoreCase))
                            return SoundRef.FilePrefix + file;
                    }
                    return CanonicalGameSound(sound.Name);
            }
        }

        string CanonicalGameSound(string name)
        {
            if (string.IsNullOrEmpty(name))
                return null;
            if (TryParseBell(name, out int number))
                return SoundRef.GamePrefix + BellPrefix + number;
            FieldInfo field = GameField(name);
            return field != null ? SoundRef.GamePrefix + field.Name : null;
        }

        public int Rescan()
        {
            var names = new List<string>();
            try
            {
                if (Directory.Exists(Folder))
                {
                    foreach (string path in Directory.GetFiles(Folder))
                    {
                        if (SoundRef.IsAudioFileName(path))
                            names.Add(Path.GetFileName(path));
                    }
                }
            }
            catch (Exception e)
            {
                Plugin.Log.LogWarning("Could not list the sounds folder: " + e.Message);
            }
            names.Sort(StringComparer.OrdinalIgnoreCase);
            _fileNames = names;
            return names.Count;
        }

        public void OpenFolder()
        {
            EnsureFolder();
            try
            {
                System.Diagnostics.Process.Start("explorer.exe", "\"" + Folder + "\"")?.Dispose();
            }
            catch (Exception)
            {
                try
                {
                    Application.OpenURL(new Uri(Folder).AbsoluteUri);
                }
                catch (Exception e)
                {
                    Plugin.Log.LogWarning("Could not open the sounds folder: " + e.Message);
                }
            }
        }

        void EnsureFolder()
        {
            try
            {
                Directory.CreateDirectory(Folder);
                string readme = Path.Combine(Folder, "README.txt");
                if (!File.Exists(readme))
                    File.WriteAllText(readme, ReadmeText);
            }
            catch (Exception e)
            {
                Plugin.Log.LogWarning("Could not create the sounds folder " + Folder + ": " + e.Message);
            }
        }

        void PlayClip(AudioClip clip, float volume)
        {
            if (clip == null || _source == null)
                return;
            _source.PlayOneShot(clip, Mathf.Clamp01(volume));
        }

        AudioClip Fallback(string value)
        {
            WarnOnce(value, $"Sound \"{value}\" is not available; playing {DefaultSound} instead.");
            return Builtin(DefaultSound);
        }

        AudioClip Builtin(string name)
        {
            string canonical = Synth.Canonical(name);
            if (canonical == null)
                return null;
            if (_builtins.TryGetValue(canonical, out AudioClip clip) && clip != null)
                return clip;

            int sampleRate = AudioSettings.outputSampleRate > 0 ? AudioSettings.outputSampleRate : 48000;
            float[] samples = Synth.Render(canonical, sampleRate);
            clip = AudioClip.Create("ChatSounds." + canonical, samples.Length, 1, sampleRate, false);
            clip.SetData(samples, 0);
            clip.hideFlags = HideFlags.DontUnloadUnusedAsset;
            _builtins[canonical] = clip;
            return clip;
        }

        AudioClip GameClip(string name)
        {
            if (string.IsNullOrEmpty(name))
                return null;
            if (TryParseBell(name, out int number))
            {
                List<AudioClip> bells = Bells();
                return bells != null && number <= bells.Count ? bells[number - 1] : null;
            }
            SFXSettings settings = SfxSettings();
            FieldInfo field = settings != null ? GameField(name) : null;
            return field != null ? field.GetValue(settings) as AudioClip : null;
        }

        static bool TryParseBell(string name, out int number)
        {
            number = 0;
            return name.Length > BellPrefix.Length &&
                   name.StartsWith(BellPrefix, StringComparison.OrdinalIgnoreCase) &&
                   int.TryParse(name.Substring(BellPrefix.Length), out number) && number >= 1;
        }

        FieldInfo GameField(string name)
        {
            if (_gameFields.TryGetValue(name, out FieldInfo field))
                return field;
            foreach (FieldInfo candidate in typeof(SFXSettings).GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
            {
                if (candidate.FieldType == typeof(AudioClip) && string.Equals(candidate.Name, name, StringComparison.OrdinalIgnoreCase))
                {
                    field = candidate;
                    break;
                }
            }
            _gameFields[name] = field;
            return field;
        }

        static List<AudioClip> Bells()
        {
            SoundManager manager = GameAccess.Music;
            return manager != null ? manager.BellSounds : null;
        }

        SFXSettings SfxSettings()
        {
            if (_sfxSettings != null)
                return _sfxSettings;
            if (Time.unscaledTime < _nextSfxLookup)
                return null;
            _nextSfxLookup = Time.unscaledTime + SfxLookupInterval;

            SFXManager manager = GameAccess.Sfx;
            if (manager != null && SfxSettingsRef != null)
                _sfxSettings = SfxSettingsRef(manager);
            if (_sfxSettings == null)
            {
                SFXSettings[] loaded = Resources.FindObjectsOfTypeAll<SFXSettings>();
                if (loaded.Length > 0)
                    _sfxSettings = loaded[0];
            }
            return _sfxSettings;
        }

        string ResolvePath(string name)
        {
            if (string.IsNullOrEmpty(name))
                return null;
            try
            {
                return Path.IsPathRooted(name) ? name : Path.Combine(Folder, name);
            }
            catch (ArgumentException)
            {
                return null;
            }
        }

        void PlayFile(string path, string value, float volume)
        {
            FileSound sound = FileEntry(path);
            if (sound == null)
            {
                PlayClip(Fallback(value), volume);
                return;
            }
            if (sound.Loading)
            {
                sound.PendingVolume = volume;
                sound.PendingSince = Time.realtimeSinceStartup;
                return;
            }
            PlayClip(sound.Clip != null ? sound.Clip : Fallback(value), volume);
        }

        /// <summary>The cache entry of an existing file; (re)loads it when it is new or has changed on disk.</summary>
        FileSound FileEntry(string path)
        {
            DateTime stamp;
            try
            {
                if (string.IsNullOrEmpty(path) || !File.Exists(path))
                    return null;
                stamp = File.GetLastWriteTimeUtc(path);
            }
            catch (Exception)
            {
                return null;
            }

            if (!_files.TryGetValue(path, out FileSound sound))
            {
                sound = new FileSound();
                _files[path] = sound;
            }
            if (!sound.Loading && sound.Stamp != stamp)
            {
                sound.Stamp = stamp;
                StartCoroutine(Load(path, sound));
            }
            return sound;
        }

        // Every step is guarded: an exception escaping the coroutine would leave the sound marked as loading forever.
        IEnumerator Load(string path, FileSound sound)
        {
            sound.Loading = true;
            AudioClip clip = null;
            string error = null;
            bool tooLarge = false;
            try
            {
                long length = new FileInfo(path).Length;
                tooLarge = length > MaxFileBytes;
                if (tooLarge)
                    error = "the file is larger than 20 MB";
                else if (length <= MaxDirectWavBytes && path.EndsWith(".wav", StringComparison.OrdinalIgnoreCase))
                    clip = LoadWav(path, out error);
            }
            catch (Exception e)
            {
                error = e.Message;
            }

            // Unity decodes OGG and MP3 (and the WAV encodings the reader above does not handle).
            UnityWebRequest request = null;
            UnityWebRequestAsyncOperation operation = null;
            string earlierError = error;
            if (clip == null && !tooLarge)
            {
                try
                {
                    request = UnityWebRequestMultimedia.GetAudioClip(new Uri(path).AbsoluteUri, AudioTypeOf(path));
                    if (request.downloadHandler is DownloadHandlerAudioClip handler)
                        handler.streamAudio = false;
                    operation = request.SendWebRequest();
                }
                catch (Exception e)
                {
                    error = e.Message;
                }
            }
            if (operation != null)
                yield return operation;
            if (request != null)
            {
                try
                {
                    if (request.result == UnityWebRequest.Result.Success)
                    {
                        clip = DownloadHandlerAudioClip.GetContent(request);
                        if (clip != null && clip.samples <= 0)
                            clip = null;
                        error = clip != null ? null : "no audio data";
                    }
                    else
                    {
                        error = request.error;
                    }
                }
                catch (Exception e)
                {
                    error = e.Message;
                }
                finally
                {
                    request.Dispose();
                }
                if (clip == null && earlierError != null)
                    error = earlierError + "; " + error;
            }

            try
            {
                Finish(path, sound, clip, error);
            }
            catch (Exception e)
            {
                Plugin.Log.LogWarning($"Could not use sound file {path}: {e}");
            }
            finally
            {
                sound.Loading = false;
            }
        }

        void Finish(string path, FileSound sound, AudioClip clip, string error)
        {
            string fileName = SafeFileName(path);
            if (clip != null)
            {
                clip.name = "ChatSounds." + fileName;
                clip.hideFlags = HideFlags.DontUnloadUnusedAsset;
                if (sound.Clip != null && sound.Clip != clip)
                    Destroy(sound.Clip);
                sound.Clip = clip;
                Plugin.Log.LogInfo($"Loaded sound file {fileName} ({clip.length:0.##} s)");
            }
            else
            {
                // A file that failed to reload keeps playing its previous version, if any.
                Plugin.Log.LogWarning($"Could not load sound file {path}: {error}");
            }
            sound.Loading = false;

            if (sound.PendingVolume >= 0f)
            {
                float volume = sound.PendingVolume;
                sound.PendingVolume = -1f;
                if (Time.realtimeSinceStartup - sound.PendingSince <= MaxPlayDelay)
                    PlayClip(sound.Clip != null ? sound.Clip : Builtin(DefaultSound), volume);
            }
        }

        static AudioClip LoadWav(string path, out string error)
        {
            byte[] bytes = File.ReadAllBytes(path);
            if (!WavReader.TryRead(bytes, out float[] samples, out int channels, out int sampleRate, out error))
                return null;
            AudioClip clip = AudioClip.Create("ChatSounds.wav", samples.Length / channels, channels, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        static AudioType AudioTypeOf(string path)
        {
            switch (Path.GetExtension(path).ToLowerInvariant())
            {
                case ".ogg":
                    return AudioType.OGGVORBIS;
                case ".mp3":
                    return AudioType.MPEG;
                case ".wav":
                    return AudioType.WAV;
                default:
                    return AudioType.UNKNOWN;
            }
        }

        static string SafeFileName(string path)
        {
            try
            {
                return Path.GetFileName(path);
            }
            catch (ArgumentException)
            {
                return path;
            }
        }

        void WarnOnce(string key, string message)
        {
            if (_warned.Add(key ?? string.Empty))
                Plugin.Log.LogWarning(message);
        }

        static AccessTools.FieldRef<SFXManager, SFXSettings> TrySfxSettingsRef()
        {
            try
            {
                return AccessTools.FieldRefAccess<SFXManager, SFXSettings>("_sfxSettings");
            }
            catch (Exception)
            {
                return null;
            }
        }

        void OnDestroy()
        {
            foreach (AudioClip clip in _builtins.Values)
            {
                if (clip != null)
                    Destroy(clip);
            }
            foreach (FileSound sound in _files.Values)
            {
                if (sound.Clip != null)
                    Destroy(sound.Clip);
            }
        }
    }
}
