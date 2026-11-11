using System;
using UnityEngine;

namespace LastShift.Settings
{
    /// <summary>
    /// The player's settings, kept in PlayerPrefs so they last between sessions (in the browser too).
    /// They are not part of a save: loading a save keeps the settings as they are.
    /// </summary>
    public static class GameSettings
    {
        const string BatteryDrainKey = "LastShift.BatteryDrain";
        const string VolumeKey = "LastShift.Volume";
        const string MouseKey = "LastShift.MouseSensitivity";

        public const float MinMouse = 0.25f, MaxMouse = 3f;

        public static event Action Changed;

        /// <summary>Off, the flashlight never runs down: the no-timer, no-pressure option.</summary>
        public static bool BatteryDrain
        {
            get => PlayerPrefs.GetInt(BatteryDrainKey, 1) == 1;
            set { PlayerPrefs.SetInt(BatteryDrainKey, value ? 1 : 0); Save(); }
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
            PlayerPrefs.DeleteKey(BatteryDrainKey);
            PlayerPrefs.DeleteKey(VolumeKey);
            PlayerPrefs.DeleteKey(MouseKey);
            Save();
        }

        static void Save()
        {
            PlayerPrefs.Save();  // WebGL only writes PlayerPrefs to the browser's storage on Save
            Changed?.Invoke();
        }
    }
}
