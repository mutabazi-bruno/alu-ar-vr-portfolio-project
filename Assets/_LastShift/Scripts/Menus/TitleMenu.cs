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
        public Toggle subtitles;
        public Slider textSize;
        public Text volumeValue, mouseValue, textSizeValue;

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
            if (LastShift.Save.AutoSave.HasSave) AddContinue();
        }

        // With progress saved, a CONTINUE button goes on top, made from PLAY, and the buttons are
        // spaced over the same height as before so nothing else on the panel moves.
        void AddContinue()
        {
            if (mainPanel == null) return;
            var play = mainPanel.transform.Find("Button PLAY") as RectTransform;
            if (play == null) return;
            var copy = Instantiate(play.gameObject, play.parent);
            copy.name = "Button CONTINUE";
            copy.transform.SetSiblingIndex(play.GetSiblingIndex());
            var button = copy.GetComponent<Button>();
            button.onClick = new Button.ButtonClickedEvent();   // not PLAY's
            button.onClick.AddListener(Continue);
            var label = copy.GetComponentInChildren<Text>(true);
            if (label != null) label.text = "CONTINUE";

            var buttons = new System.Collections.Generic.List<RectTransform>();
            foreach (Transform t in mainPanel.transform)
                if (t.gameObject.activeSelf && t.GetComponent<Button>() != null && t != copy.transform)
                    buttons.Add((RectTransform)t);
            buttons.Sort((a, b) => b.anchoredPosition.y.CompareTo(a.anchoredPosition.y));
            if (buttons.Count == 0) return;
            float top = buttons[0].anchoredPosition.y, bottom = buttons[buttons.Count - 1].anchoredPosition.y;
            buttons.Insert(0, (RectTransform)copy.transform);
            var step = (top - bottom) / Mathf.Max(1, buttons.Count - 1);
            var height = Mathf.Min(play.sizeDelta.y, step - 10f);
            for (var i = 0; i < buttons.Count; i++)
            {
                buttons[i].anchoredPosition = new Vector2(buttons[i].anchoredPosition.x, top - i * step);
                buttons[i].sizeDelta = new Vector2(buttons[i].sizeDelta.x, height);
            }
        }

        public void Continue()
        {
            Click();
            LastShift.Save.AutoSave.Continue();
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
            if (keypadLockout != null) keypadLockout.SetIsOnWithoutNotify(GameSettings.KeypadLockout);
            if (subtitles != null) subtitles.SetIsOnWithoutNotify(GameSettings.Subtitles);
            if (textSize != null)
            {
                textSize.minValue = GameSettings.MinTextSize;
                textSize.maxValue = GameSettings.MaxTextSize;
                textSize.SetValueWithoutNotify(GameSettings.TextSize);
            }
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
            if (textSizeValue != null) textSizeValue.text = Mathf.RoundToInt(GameSettings.TextSize * 100f) + "%";
        }

        public void SetBatteryDrain(bool on) { GameSettings.BatteryDrain = on; Click(); }
        public void SetKeypadLockout(bool on) { GameSettings.KeypadLockout = on; Click(); }
        public void SetSubtitles(bool on) { GameSettings.Subtitles = on; Click(); }
        public void SetTextSize(float v) { GameSettings.TextSize = v; Labels(); }
        public void SetVolume(float v) { GameSettings.Volume = v; AudioListener.volume = v; Labels(); }
        public void SetMouseSpeed(float v) { GameSettings.MouseSensitivity = v; Labels(); }

        public void ResetSettings()
        {
            Click();
            GameSettings.ResetToDefaults();
            ReadSettings();
        }

        void Click()
        {
            if (click != null) AudioSource.PlayClipAtPoint(click, Camera.main != null ? Camera.main.transform.position : Vector3.zero, 0.6f);
        }
    }
}
