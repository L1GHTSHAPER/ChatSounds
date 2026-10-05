using System;
using System.Collections.Generic;
using HarmonyLib;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace ChatSounds
{
    /// <summary>
    /// Cached, exception-safe access to the game objects the mod reads.
    /// The game's singleton getters fall back to FindAnyObjectByType when the instance is missing
    /// (e.g. in the main menu), so lookups of missing instances are throttled.
    /// </summary>
    internal static class GameAccess
    {
        const float LookupInterval = 1f;

        static TextChannelManager _chat;
        static UIManager _ui;
        static DataManager _data;
        static SFXManager _sfx;
        static SoundManager _music;
        static float _nextChatLookup;
        static float _nextUiLookup;
        static float _nextDataLookup;
        static float _nextSfxLookup;
        static float _nextMusicLookup;

        static readonly AccessTools.FieldRef<TextChannelManager, TMP_Text> TextPrefabRef =
            TryFieldRef<TMP_Text>("_textPrefab");
        static readonly AccessTools.FieldRef<TextChannelManager, List<GameObject>> LocalMessagesRef =
            TryFieldRef<List<GameObject>>("_messageObjectsLocal");

        public static TextChannelManager Chat => Lookup(ref _chat, ref _nextChatLookup, () => NetworkSingleton<TextChannelManager>.I);
        public static UIManager UI => Lookup(ref _ui, ref _nextUiLookup, () => MonoSingleton<UIManager>.I);
        public static DataManager Data => Lookup(ref _data, ref _nextDataLookup, () => MonoSingleton<DataManager>.I);
        public static SFXManager Sfx => Lookup(ref _sfx, ref _nextSfxLookup, () => MonoSingleton<SFXManager>.I);
        public static SoundManager Music => Lookup(ref _music, ref _nextMusicLookup, () => MonoSingleton<SoundManager>.I);

        static T Lookup<T>(ref T cached, ref float nextLookup, Func<T> find) where T : UnityEngine.Object
        {
            if (cached == null && Time.unscaledTime >= nextLookup)
            {
                nextLookup = Time.unscaledTime + LookupInterval;
                cached = find();
            }
            return cached;
        }

        /// <summary>The game's Master volume (0..1); 1 when it cannot be read.</summary>
        public static float GameMasterVolume
        {
            get
            {
                DataManager data = Data;
                SettingsData settings = data != null ? data.SettingsData : null;
                return settings != null ? Mathf.Clamp01(settings.MasterVolume) : 1f;
            }
        }

        /// <summary>The name your messages are sent with (may contain rich text tags).</summary>
        public static string LocalPlayerName
        {
            get
            {
                TextChannelManager chat = Chat;
                return chat != null ? chat.UserName : null;
            }
        }

        public static bool IsLocalPlayerFocusing()
        {
            TextChannelManager chat = Chat;
            PlayerFocusController focus = chat != null ? chat.MainFocusController : null;
            return focus != null && focus.IsFocus;
        }

        /// <summary>
        /// True while the player can see the given chat tab: the game window has focus, the chat panel is shown and
        /// that tab is selected.
        /// </summary>
        public static bool IsChannelVisible(bool isLocal)
        {
            if (!Application.isFocused)
                return false;
            UIManager ui = UI;
            TextChannelManager chat = Chat;
            return ui != null && !ui.IsMessagePanelHidden && chat != null && chat.Islocal == isLocal;
        }

        /// <summary>True while any text field has keyboard focus (chat, journal, to-do list...).</summary>
        public static bool IsAnyTextFieldFocused()
        {
            EventSystem eventSystem = EventSystem.current;
            if (eventSystem == null)
                return false;
            GameObject selected = eventSystem.currentSelectedGameObject;
            if (selected == null)
                return false;
            TMP_InputField tmpInput = selected.GetComponent<TMP_InputField>();
            if (tmpInput != null && tmpInput.isFocused)
                return true;
            InputField legacyInput = selected.GetComponent<InputField>();
            return legacyInput != null && legacyInput.isFocused;
        }

        /// <summary>
        /// Shows a client-side line in the chat tab the player is currently looking at.
        /// Nothing is sent to other players.
        /// </summary>
        public static void Notify(string message)
        {
            TextChannelManager chat = Chat;
            if (chat == null)
                return;

            // noparse: command syntax such as <sound> must not be read as rich text tags.
            string text = "<color=#9FD1FF>[ChatSounds]</color> <noparse>" + message + "</noparse>";
            try
            {
                if (chat.Islocal && TryAddLocalLine(chat, text))
                    return;
                chat.AddNotification(text);
            }
            catch (Exception e)
            {
                Plugin.Log.LogWarning("Could not show chat notification: " + e.Message);
            }
        }

        /// <summary>Mirrors TextChannelManager.AddNotification, but for the Local tab.</summary>
        static bool TryAddLocalLine(TextChannelManager chat, string text)
        {
            if (TextPrefabRef == null)
                return false;
            UIManager ui = UI;
            Transform parent = ui != null ? ui.TextContentLocalTransform : null;
            TMP_Text prefab = TextPrefabRef(chat);
            if (parent == null || prefab == null)
                return false;

            TMP_Text line = UnityEngine.Object.Instantiate(prefab, parent);
            Button button = line.GetComponent<Button>();
            if (button != null)
                button.interactable = false;
            line.text = text;

            // Register the line with the game so its normal history limit trims it later.
            List<GameObject> messages = LocalMessagesRef != null ? LocalMessagesRef(chat) : null;
            if (messages != null)
            {
                messages.Add(line.gameObject);
                int limit = ScriptableSingleton<GameSettings>.I.LocalMessageLimitCount;
                while (messages.Count > limit && messages.Count > 0)
                {
                    GameObject oldest = messages[0];
                    messages.RemoveAt(0);
                    if (oldest != null)
                        UnityEngine.Object.Destroy(oldest);
                }
            }
            return true;
        }

        static AccessTools.FieldRef<TextChannelManager, T> TryFieldRef<T>(string fieldName)
        {
            try
            {
                return AccessTools.FieldRefAccess<TextChannelManager, T>(fieldName);
            }
            catch (Exception)
            {
                return null;
            }
        }
    }
}
