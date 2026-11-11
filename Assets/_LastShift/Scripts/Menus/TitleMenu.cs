using HeyHeyThere.EscapePuzzleKitFree;
using LastShift.Settings;
using UnityEngine;
using UnityEngine.UI;

namespace LastShift.Menus
{
    /// <summary>
    /// The title screen's buttons. The panels and buttons are ordinary objects in the scene, so text,
    /// size and colours can be edited in the Inspector; each button's On Click calls one of these.
    /// Point with the mouse on a monitor, or with a controller ray and the trigger in a headset.
    /// </summary>
    public class TitleMenu : MonoBehaviour
    {
        [Tooltip("The first room of a new shift")]
        public string firstScene = "RecordsRoomB";
        public GameObject mainPanel;
        public GameObject settingsPanel;
        public GameObject controlsPanel;
        [Tooltip("Hidden in the browser build: a web page can't close its own tab")]
        public GameObject quitButton;

        [Header("Settings")]
        public Toggle batteryDrain;
        public Toggle keypadLockout;
        public Slider volume;
        public Slider mouseSpeed;
        public Text volumeValue, mouseValue;

        public AudioClip click;

        void Start()
        {
            Time.timeScale = 1f;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
#if UNITY_WEBGL && !UNITY_EDITOR
            if (quitButton != null) quitButton.SetActive(false);
#endif
            Show(mainPanel);
            ReadSettings();
        }

        void Show(GameObject panel)
        {
            if (mainPanel != null) mainPanel.SetActive(panel == mainPanel);
            if (settingsPanel != null) settingsPanel.SetActive(panel == settingsPanel);
            if (controlsPanel != null) controlsPanel.SetActive(panel == controlsPanel);
        }

        // ---- buttons ----------------------------------------------------------------------------

        public void Play()
        {
            Click();
            // a fresh shift: items, flags and the timer cleared, as the end card's Play again does
            SceneFader.Load(() => SaveGame.Restart(firstScene));
        }

        public void OpenSettings() { Click(); Show(settingsPanel); }
        public void OpenControls() { Click(); Show(controlsPanel); }
        public void Back() { Click(); Show(mainPanel); }

        public void Quit()
        {
            Click();
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            GameFlow.QuitForReal();
#endif
        }

        // ---- settings, shared with the pause menu's ---------------------------------------------

        void ReadSettings()
        {
            if (batteryDrain != null) batteryDrain.SetIsOnWithoutNotify(GameSettings.BatteryDrain);
            if (keypadLockout != null) keypadLockout.SetIsOnWithoutNotify(Keypads.KeypadScreen.LockoutOn);
            if (volume != null) volume.SetValueWithoutNotify(GameSettings.Volume);
            if (mouseSpeed != null)
            {
                mouseSpeed.minValue = GameSettings.MinMouse;
                mouseSpeed.maxValue = GameSettings.MaxMouse;
                mouseSpeed.SetValueWithoutNotify(GameSettings.MouseSensitivity);
            }
            Labels();
        }

        void Labels()
        {
            if (volumeValue != null) volumeValue.text = Mathf.RoundToInt(GameSettings.Volume * 100f) + "%";
            if (mouseValue != null) mouseValue.text = GameSettings.MouseSensitivity.ToString("0.0") + "x";
        }

        public void SetBatteryDrain(bool on) { GameSettings.BatteryDrain = on; Click(); }
        public void SetKeypadLockout(bool on) { Keypads.KeypadScreen.LockoutOn = on; Click(); }
        public void SetVolume(float v) { GameSettings.Volume = v; AudioListener.volume = v; Labels(); }
        public void SetMouseSpeed(float v) { GameSettings.MouseSensitivity = v; Labels(); }

        public void ResetSettings()
        {
            Click();
            GameSettings.ResetToDefaults();
            Keypads.KeypadScreen.LockoutOn = true;
            ReadSettings();
        }

        void Click()
        {
            if (click != null) AudioSource.PlayClipAtPoint(click, Camera.main != null ? Camera.main.transform.position : Vector3.zero, 0.6f);
        }
    }
}
