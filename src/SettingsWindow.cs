using System;
using System.Collections.Generic;
using BepInEx.Configuration;
using UnityEngine;
using UnityEngine.UI;

namespace ChatSounds
{
    /// <summary>
    /// In-game settings window (IMGUI). The component is enabled only while the window is open, so it costs
    /// nothing otherwise. Every control writes straight to the config entries.
    /// </summary>
    internal sealed class SettingsWindow : MonoBehaviour
    {
        const int WindowId = 0x43534E44;
        const float Width = 600f;
        float LabelWidth => Mathf.Clamp((_rect.width - 40f) * .32f, 90f, 170f);
        const float ArrowWidth = 26f;
        const float PlayWidth = 32f;
        const float ValueWidth = 54f;
        const float MaxCooldown = 60f;
        // A volume change is previewed once the slider has rested this long.
        const float PreviewDelay = 0.3f;

        static readonly GUILayoutOption[] Shrinkable = { GUILayout.MinWidth(40f), GUILayout.ExpandWidth(true) };

        UiSkin _skin;
        GUISkin _guiSkin;
        WindowInputShield _shield;
        int _tab, _pendingTab = -1;
        readonly Vector2[] _tabScrolls = new Vector2[4];
        bool _advanced, _pendingAdvanced;
        bool _changeAdvanced;
        string _keywords;
        bool _keywordDirty;
        GUI.WindowFunction _drawWindow;
        Rect _rect;
        bool _placed;
        float _screenHeight;
        Vector2 _scroll;
        SoundCategory _pendingPreview;
        float _previewAt;
        // The IMGUI control of this window that holds the mouse (slider or window drag), released on close.
        int _hotControl;

        public void Toggle()
        {
            SetOpen(!enabled);
        }

        public void SetOpen(bool open)
        {
            if (open == enabled)
                return;
            enabled = open;
            if (_shield == null) _shield = new WindowInputShield("ChatSounds");
            _shield.Set(open);
            if (open)
            {
                Lang.Invalidate();
                Plugin.Instance.Sounds.Rescan();
                _keywords = Plugin.Instance.Keywords.Value;
                _keywordDirty = false;
            }
            else
            {
                _pendingPreview = null;
                // Closed with the hotkey while a slider is held: nothing would release the mouse otherwise, and
                // other IMGUI windows would ignore clicks.
                if (_hotControl != 0 && GUIUtility.hotControl == _hotControl)
                    GUIUtility.hotControl = 0;
                _hotControl = 0;
            }
        }

        void OnDisable()
        {
            _shield?.Set(false);
        }

        void Update()
        {
            if (Input.GetKeyDown(KeyCode.Escape)) SetOpen(false);
            if (_pendingPreview != null && Time.unscaledTime >= _previewAt)
            {
                Plugin.Instance.Preview(_pendingPreview);
                _pendingPreview = null;
            }
        }

        void OnGUI()
        {
            if (UiEnvironment.PreviewRendering || Plugin.Instance == null)
                return;
            if (_skin == null)
            { _skin = new UiSkin(); _guiSkin = _skin.CreateGuiSkin(GUI.skin); }
            if (_drawWindow == null)
                _drawWindow = DrawWindow;

            // Keep the window the same physical size on high resolutions (laid out for 1080p).
            float scale = UiEnvironment.Scale;
            Matrix4x4 previousMatrix = GUI.matrix;
            GUISkin previousSkin = GUI.skin;
            try
            {
            GUI.skin = _guiSkin;
            GUI.matrix = Matrix4x4.Scale(new Vector3(scale, scale, 1f));
            float screenWidth = Screen.width / scale;
            _screenHeight = Screen.height / scale;
            if (!_placed)
            {
                _rect = new Rect(Mathf.Round((screenWidth - Width) * 0.5f), Mathf.Round(_screenHeight * 0.08f), Width, 0f);
                _placed = true;
            }

            float width = Mathf.Min(Width, screenWidth - 24f);
            _rect.width = width;
            _rect = GUILayout.Window(WindowId, _rect, _drawWindow, GUIContent.none, _skin.Window, GUILayout.Width(width));
            _rect.x = Mathf.Clamp(_rect.x, 0f, Mathf.Max(0f, screenWidth - _rect.width));
            _rect.y = Mathf.Clamp(_rect.y, 0f, Mathf.Max(0f, _screenHeight - _rect.height));
            }
            finally { GUI.matrix = previousMatrix; GUI.skin = previousSkin; }

            if (Event.current.type == EventType.Repaint)
                _shield?.Place(_rect, scale);
        }

        void DrawWindow(int id)
        {
            Plugin plugin = Plugin.Instance;
            Lang lang = Lang.Current;
            int hotControlBefore = GUIUtility.hotControl;
            if (Event.current.type == EventType.Layout)
            {
                if (_pendingTab >= 0) { _tabScrolls[_tab] = _scroll; _tab = _pendingTab; _pendingTab = -1; _scroll = _tabScrolls[_tab]; GUI.FocusControl(null); }
                if (_changeAdvanced) { _advanced = _pendingAdvanced; _changeAdvanced = false; }
            }

            GUILayout.BeginHorizontal();
            GUILayout.Label("CS", _skin.Logo);
            GUILayout.BeginVertical();
            GUILayout.Label("ChatSounds", _skin.Title);
            GUILayout.Label(UiEnvironment.L("Chat notifications", "Уведомления чата"), _skin.Subtitle);
            GUILayout.EndVertical();
            GUILayout.FlexibleSpace();
            if (GUILayout.Button("×", _skin.CloseButton))
                SetOpen(false);
            GUILayout.EndHorizontal();

            GUILayout.Space(8);
            string[] tabs = { UiEnvironment.L("General", "Общее"), lang.GlobalChat, lang.LocalChat, lang.Mentions };
            int columns = _rect.width < 480 ? 2 : 4;
            for (int first = 0; first < 4; first += columns)
            {
                GUILayout.BeginHorizontal();
                for (int i = first; i < first + columns; i++) if (GUILayout.Button(tabs[i], i == _tab ? _skin.SelectedTab : _skin.Tab)) _pendingTab = i;
                GUILayout.EndHorizontal();
            }
            GUILayout.Space(8);

            // The settings scroll when the screen is too low for all of them. The few spare pixels cover the first
            // row's margin and rounding, so no scrollbar appears when everything fits.
            float viewHeight = Mathf.Min(430f, Mathf.Max(80f, _screenHeight - (_rect.width < 480 ? 290f : 250f)));
            _scroll = GUILayout.BeginScrollView(_scroll, false, false, GUI.skin.horizontalScrollbar,
                GUI.skin.verticalScrollbar, GUIStyle.none, GUILayout.Height(viewHeight));
            GUILayout.BeginVertical();
            DrawSettings(plugin, lang);
            GUILayout.EndVertical();
            GUILayout.EndScrollView();

            GUILayout.Space(10f);
            GUILayout.BeginHorizontal();
            if (GUILayout.Button(lang.OpenFolder, _skin.Button))
                plugin.Sounds.OpenFolder();
            if (GUILayout.Button(lang.Rescan, _skin.Button))
                plugin.Sounds.Rescan();
            GUILayout.FlexibleSpace();
            if (GUILayout.Button(lang.Close, _skin.Button))
                SetOpen(false);
            GUILayout.EndHorizontal();
            GUILayout.Space(6f);
            GUILayout.Label(string.Format(lang.FilesHint, plugin.Sounds.FileCount), _skin.Hint);
            GUILayout.Label(string.Format(lang.WindowHint, plugin.WindowKey.Value, plugin.ToggleKey.Value), _skin.Hint);

            GUI.DragWindow(new Rect(0f, 0f, _rect.width - 48f, 62f));

            // A control in this window took or released the mouse during this event.
            if (GUIUtility.hotControl != hotControlBefore)
                _hotControl = GUIUtility.hotControl;
        }

        void DrawSettings(Plugin plugin, Lang lang)
        {
            if (_tab == 0)
            {
                GUILayout.BeginVertical(_skin.Panel);
                GUILayout.Label(UiEnvironment.L("Notifications", "Уведомления"), _skin.SectionTitle);
                ToggleRow(plugin.Enabled, lang.SoundsEnabled);
                PercentRow(plugin.MasterVolume, lang.MasterVolume, plugin.Global);
                GUILayout.EndVertical();
                GUILayout.BeginVertical(_skin.Panel);
                if (GUILayout.Button((_advanced ? "▼ " : "► ") + UiEnvironment.L("More options", "Дополнительно"), _skin.SectionTitle))
                { _pendingAdvanced = !_advanced; _changeAdvanced = true; }
                if (_advanced) { ToggleRow(plugin.FollowGameVolume, lang.FollowGameVolume); ToggleRow(plugin.QuietDuringFocus, lang.QuietDuringFocus); }
                GUILayout.EndVertical();
            }
            else CategoryPanel(plugin, lang, _tab == 1 ? plugin.Global : _tab == 2 ? plugin.Local : plugin.Mentions);
        }

        void CategoryPanel(Plugin plugin, Lang lang, SoundCategory category)
        {
            GUILayout.BeginVertical(_skin.Panel);
            GUILayout.BeginHorizontal(_skin.Row);
            bool enabled = GUILayout.Toggle(category.Enabled.Value, GUIContent.none, _skin.Check);
            if (GUILayout.Button(category.Title(lang), _skin.SectionTitle))
                enabled = !category.Enabled.Value;
            GUILayout.EndHorizontal();
            if (enabled != category.Enabled.Value)
                category.Enabled.Value = enabled;

            bool wasEnabled = GUI.enabled;
            GUI.enabled = wasEnabled && category.Enabled.Value;
            if (category == plugin.Mentions)
                MentionRows(plugin, lang);
            SoundRow(plugin, lang, category);
            PercentRow(category.Volume, lang.Volume, category);
            WhenRow(lang, category);
            CooldownRow(lang, category);
            GUI.enabled = wasEnabled;
            GUILayout.EndVertical();
        }

        void MentionRows(Plugin plugin, Lang lang)
        {
            string name = MentionMatcher.StripTags(GameAccess.LocalPlayerName).Trim();
            ToggleRow(plugin.MatchMyName, name.Length > 0 ? string.Format(lang.MatchMyNameFormat, name) : lang.MatchMyName);

            GUILayout.BeginHorizontal(_skin.Row);
            GUILayout.Label(lang.Keywords, _skin.Label, GUILayout.Width(LabelWidth));
            string nextKeywords = GUILayout.TextField(_keywords ?? plugin.Keywords.Value, _skin.EditorHex, Shrinkable);
            if (nextKeywords != _keywords) { _keywords = nextKeywords; _keywordDirty = true; }
            GUILayout.EndHorizontal();
            bool previousEnabled = GUI.enabled;
            GUI.enabled = previousEnabled && _keywordDirty;
            if (GUILayout.Button(UiEnvironment.L("Apply keywords", "Сохранить слова"), _skin.Button)) { plugin.Keywords.Value = _keywords; _keywordDirty = false; }
            GUI.enabled = previousEnabled;
            GUILayout.Label(lang.KeywordsHint, _skin.Hint);
        }

        void ToggleRow(ConfigEntry<bool> entry, string label)
        {
            GUILayout.BeginHorizontal(_skin.Row);
            bool value = GUILayout.Toggle(entry.Value, GUIContent.none, _skin.Check);
            if (GUILayout.Button(label, _skin.ToggleLabel, Shrinkable))
                value = !entry.Value;
            GUILayout.EndHorizontal();
            if (value != entry.Value)
                entry.Value = value;
        }

        void PercentRow(ConfigEntry<int> entry, string label, SoundCategory preview)
        {
            GUILayout.BeginHorizontal(_skin.Row);
            GUILayout.Label(label, _skin.Label, GUILayout.Width(LabelWidth));
            float raw = GUILayout.HorizontalSlider(entry.Value, 0f, 100f, _skin.Slider, _skin.Thumb);
            int value = Mathf.RoundToInt(raw);
            GUILayout.Label(value + "%", _skin.Value, GUILayout.Width(ValueWidth));
            GUILayout.EndHorizontal();
            if (value != entry.Value)
            {
                entry.Value = value;
                _pendingPreview = preview;
                _previewAt = Time.unscaledTime + PreviewDelay;
            }
        }

        void SoundRow(Plugin plugin, Lang lang, SoundCategory category)
        {
            GUILayout.BeginHorizontal(_skin.Row);
            GUILayout.Label(lang.Sound, _skin.Label, GUILayout.Width(LabelWidth));
            int step = 0;
            if (GUILayout.Button("◄", _skin.SmallButton, GUILayout.Width(ArrowWidth)))
                step = -1;
            GUILayout.Label(plugin.Sounds.DisplayName(category.Sound.Value, lang), _skin.Field, Shrinkable);
            if (GUILayout.Button("►", _skin.SmallButton, GUILayout.Width(ArrowWidth)))
                step = 1;
            if (GUILayout.Button("♪", _skin.SmallButton, GUILayout.Width(PlayWidth)))
                plugin.Preview(category);
            GUILayout.EndHorizontal();

            if (step != 0)
            {
                List<string> choices = plugin.Sounds.Choices();
                int index = choices.FindIndex(c => string.Equals(c, category.Sound.Value, StringComparison.OrdinalIgnoreCase));
                index = index < 0 ? (step > 0 ? 0 : choices.Count - 1) : (index + step + choices.Count) % choices.Count;
                category.Sound.Value = choices[index];
                plugin.Preview(category);
            }
        }

        void WhenRow(Lang lang, SoundCategory category)
        {
            GUILayout.BeginHorizontal(_skin.Row);
            GUILayout.Label(lang.When, _skin.Label, GUILayout.Width(LabelWidth));
            int step = 0;
            if (GUILayout.Button("◄", _skin.SmallButton, GUILayout.Width(ArrowWidth)))
                step = -1;
            GUILayout.Label(lang.Describe(category.When.Value), _skin.Field, Shrinkable);
            if (GUILayout.Button("►", _skin.SmallButton, GUILayout.Width(ArrowWidth)))
                step = 1;
            // Keeps this field as wide as the sound name above it.
            GUILayout.Label(GUIContent.none, _skin.Spacer, GUILayout.Width(PlayWidth));
            GUILayout.EndHorizontal();

            if (step != 0)
            {
                int count = Enum.GetValues(typeof(PlayWhen)).Length;
                category.When.Value = (PlayWhen)(((int)category.When.Value + step + count) % count);
            }
        }

        void CooldownRow(Lang lang, SoundCategory category)
        {
            // Quadratic scale: fine steps for short pauses, yet the whole 60 s range is within reach.
            float position = Mathf.Sqrt(Mathf.Clamp(category.Cooldown.Value, 0f, MaxCooldown) / MaxCooldown);
            GUILayout.BeginHorizontal(_skin.Row);
            GUILayout.Label(lang.Cooldown, _skin.Label, GUILayout.Width(LabelWidth));
            float newPosition = GUILayout.HorizontalSlider(position, 0f, 1f, _skin.Slider, _skin.Thumb);
            float seconds = category.Cooldown.Value;
            GUILayout.Label(seconds <= 0f ? lang.NoCooldown : lang.Seconds(seconds), _skin.Value, GUILayout.Width(ValueWidth));
            GUILayout.EndHorizontal();

            if (newPosition != position)
            {
                float value = MaxCooldown * newPosition * newPosition;
                category.Cooldown.Value = value < 10f ? Mathf.Round(value * 2f) / 2f : Mathf.Round(value);
            }
        }

        void OnDestroy()
        {
            _shield?.Dispose();
            if (_guiSkin != null) Destroy(_guiSkin);
            if (_skin != null)
                _skin.Destroy();
        }
    }
}
