using System;
using UnityEngine;

namespace LastShift.Settings
{
    /// <summary>
    /// The player's settings, kept in PlayerPrefs so they last between sessions (in the browser too).
    /// They are not part of a save: loading a save keeps the settings as they are.
    /// Desktop and VR menus both read and write these, and listen to <see cref="Changed"/>.
    /// </summary>
    public static class GameSettings
    {
        const string BatteryDrainKey = "LastShift.BatteryDrain";
        const string VolumeKey = "LastShift.Volume";
        const string MouseKey = "LastShift.MouseSensitivity";
        const string LockoutKey = "LastShift.KeypadLockout";
        const string SubtitlesKey = "LastShift.Subtitles";
        const string TextSizeKey = "LastShift.TextSize";

        public const float MinMouse = 0.25f, MaxMouse = 3f;
        public const float MinTextSize = 0.75f, MaxTextSize = 1.5f;

        public static event Action Changed;

        /// <summary>On, the flashlight runs down and batteries refill it. Off by default: no timer, and
        /// the battery bar is hidden in our rooms.</summary>
        public static bool BatteryDrain
        {
            get => GetBool(BatteryDrainKey, false);
            set => SetBool(BatteryDrainKey, value);
        }

        /// <summary>Three wrong codes lock a keypad for a while. Read by the keypad screens.</summary>
        public static bool KeypadLockout
        {
            get => GetBool(LockoutKey, true);
            set => SetBool(LockoutKey, value);
        }

        /// <summary>Show subtitles for voice lines and sounds. Read by the intercom and subtitle display.</summary>
        public static bool Subtitles
        {
            get => GetBool(SubtitlesKey, true);
            set => SetBool(SubtitlesKey, value);
        }

        /// <summary>Text size, as a multiple of each display's own: the HUD, notes and subtitles.</summary>
        public static float TextSize
        {
            get => PlayerPrefs.GetFloat(TextSizeKey, 1f);
            set { PlayerPrefs.SetFloat(TextSizeKey, Mathf.Clamp(value, MinTextSize, MaxTextSize)); Save(); }
        }

        /// <summary>Master volume, 0 to 1.</summary>
        public static float Volume
        {
            get => PlayerPrefs.GetFloat(VolumeKey, 1f);
            set { PlayerPrefs.SetFloat(VolumeKey, Mathf.Clamp01(value)); Save(); }
        }

        /// <summary>Mouse look speed, as a multiple of the walker's own.</summary>
        public static float MouseSensitivity
        {
            get => PlayerPrefs.GetFloat(MouseKey, 1f);
            set { PlayerPrefs.SetFloat(MouseKey, Mathf.Clamp(value, MinMouse, MaxMouse)); Save(); }
        }

        public static void ResetToDefaults()
        {
            foreach (var key in new[] { BatteryDrainKey, VolumeKey, MouseKey, LockoutKey, SubtitlesKey, TextSizeKey })
                PlayerPrefs.DeleteKey(key);
            Save();
        }

        static bool GetBool(string key, bool fallback) => PlayerPrefs.GetInt(key, fallback ? 1 : 0) == 1;

        static void SetBool(string key, bool value)
        {
            PlayerPrefs.SetInt(key, value ? 1 : 0);
            Save();
        }

        static void Save()
        {
            PlayerPrefs.Save();  // WebGL only writes PlayerPrefs to the browser's storage on Save
            Changed?.Invoke();
        }
    }
}
