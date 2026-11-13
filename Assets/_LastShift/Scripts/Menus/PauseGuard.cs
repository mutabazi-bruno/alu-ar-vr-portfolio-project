using HeyHeyThere.EscapePuzzleKitFree;
using LastShift.Settings;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.UI;
using UnityEngine.UI;

namespace LastShift.Menus
{
    /// <summary>
    /// The desktop pause menu, in the game's case-file look: a sheet from the archive with Resume,
    /// Settings, Restart and Quit. It takes the place of the kit's own pause menu (switched off
    /// here, the kit's files are untouched), so only Esc opens it, never the right mouse button.
    /// Quit stops Play mode in the Editor; in a build it goes back to the title screen, whose own
    /// Quit closes the game. The settings are the same ones as the title screen's.
    /// In a headset the VR menu (Y / B) does this job.
    /// </summary>
    [DefaultExecutionOrder(-500)]
    public class PauseGuard : MonoBehaviour
    {
        public Font typed, typedBold, title;

        public static bool IsOpen { get; private set; }

        bool wasOpen;
        GameObject root, mainPage, settingsPage;
        Text objective;
        Text drainLabel, lockoutLabel, subtitlesLabel, volumeLabel, mouseLabel, sizeLabel;
        Slider volume, mouse, size;

        void Update()
        {
            var hud = Hud.Current;
            if (hud != null) hud.pauseMenu = false;
            // the kit closes a close-up or the inventory on Esc in the same frame: that Esc isn't ours
            wasOpen = hud != null && hud.IsOpen;
            var keys = Keyboard.current;
            if (keys == null || !keys.escapeKey.wasPressedThisFrame) return;
            if (IsOpen)
            {
                if (settingsPage != null && settingsPage.activeSelf) ShowMain();
                else Close();
            }
            else if (!wasOpen && !Interactor.Paused && !Run.HasEscaped && Time.timeScale > 0f)
                Open();
        }

        void OnDisable()
        {
            if (IsOpen) Close();
            if (Hud.Current != null) Hud.Current.pauseMenu = true;
        }

        void OnDestroy()
        {
            if (root != null) Destroy(root);
        }

        // ---- open and close --------------------------------------------------------------------------

        public void Open()
        {
            if (root == null) Build();
            EnsureEventSystem();
            IsOpen = true;
            root.SetActive(true);
            ShowMain();
            if (objective != null) objective.text = Run.Objective;
            Interactor.Hold(this, true);   // frees the cursor and stops the walker
            Time.timeScale = 0f;
        }

        public void Close()
        {
            IsOpen = false;
            if (root != null) root.SetActive(false);
            Interactor.Hold(this, false);
            Time.timeScale = 1f;
        }

        void ShowMain()
        {
            mainPage.SetActive(true);
            settingsPage.SetActive(false);
        }

        void ShowSettings()
        {
            mainPage.SetActive(false);
            settingsPage.SetActive(true);
            ReadSettings();
        }

        void Restart()
        {
            Close();
            SaveGame.Restart();
        }

        void Quit()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Close();
            GameFlow.ToTitle();
#endif
        }

        static void EnsureEventSystem()
        {
            if (EventSystem.current != null) return;
            new GameObject("EventSystem", typeof(EventSystem), typeof(InputSystemUIInputModule));
        }

        // ---- settings ------------------------------------------------------------------------------

        void ReadSettings()
        {
            volume.SetValueWithoutNotify(GameSettings.Volume);
            mouse.minValue = GameSettings.MinMouse; mouse.maxValue = GameSettings.MaxMouse;
            mouse.SetValueWithoutNotify(GameSettings.MouseSensitivity);
            size.minValue = GameSettings.MinTextSize; size.maxValue = GameSettings.MaxTextSize;
            size.SetValueWithoutNotify(GameSettings.TextSize);
            Labels();
        }

        void Labels()
        {
            drainLabel.text = ">  BATTERY DRAIN: " + (GameSettings.BatteryDrain ? "ON" : "OFF");
            lockoutLabel.text = ">  KEYPAD LOCKOUT: " + (GameSettings.KeypadLockout ? "ON" : "OFF");
            subtitlesLabel.text = ">  SUBTITLES: " + (GameSettings.Subtitles ? "ON" : "OFF");
            volumeLabel.text = "Volume  " + Mathf.RoundToInt(GameSettings.Volume * 100f) + "%";
            mouseLabel.text = "Mouse speed  " + GameSettings.MouseSensitivity.ToString("0.0") + "x";
            sizeLabel.text = "Text size  " + Mathf.RoundToInt(GameSettings.TextSize * 100f) + "%";
        }

        // ---- the sheet -------------------------------------------------------------------------------

        void Build()
        {
            if (typed == null) typed = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            if (typedBold == null) typedBold = typed;
            if (title == null) title = typedBold;

            root = new GameObject("Pause Menu", typeof(RectTransform));
            DontDestroyOnLoad(root);
            var canvas = root.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 500;
            var scaler = root.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 1f;
            root.AddComponent<GraphicRaycaster>();

            var dim = Box("Backdrop", root.transform, new Color(Theme.Backdrop.r, Theme.Backdrop.g, Theme.Backdrop.b, 0.78f));
            Stretch(dim.rectTransform);

            var sheet = Box("Sheet", root.transform, Theme.Paper);
            sheet.rectTransform.sizeDelta = new Vector2(660f, 860f);
            sheet.rectTransform.localRotation = Quaternion.Euler(0f, 0f, -1f);
            var tab = Box("Tab", sheet.transform, Theme.PaperDeep);
            tab.rectTransform.anchorMin = tab.rectTransform.anchorMax = new Vector2(0f, 1f);
            tab.rectTransform.pivot = new Vector2(0f, 0f);
            tab.rectTransform.sizeDelta = new Vector2(200f, 26f);
            tab.rectTransform.anchoredPosition = new Vector2(36f, 0f);

            var head = Label("PAUSED", sheet.transform, title, 72, Theme.PaperInk, TextAnchor.MiddleLeft);
            Place(head.rectTransform, 56f, 40f, 90f);
            var stamp = Label("NIGHT SHIFT  ·  02:14", sheet.transform, typedBold, 24, Theme.Stamp, TextAnchor.MiddleLeft);
            Place(stamp.rectTransform, 60f, 130f, 34f);
            stamp.rectTransform.localRotation = Quaternion.Euler(0f, 0f, 2f);

            mainPage = Page("Main", sheet.transform);
            objective = Label("", mainPage.transform, typed, 26, Theme.PaperDim, TextAnchor.UpperLeft);
            Size(objective.gameObject, 70f);
            Button("RESUME", mainPage.transform, Close);
            Button("SETTINGS", mainPage.transform, ShowSettings);
            Button("RESTART SHIFT", mainPage.transform, Restart);
            Button("QUIT TO TITLE", mainPage.transform, Quit);

            settingsPage = Page("Settings", sheet.transform);
            drainLabel = Button("", settingsPage.transform, () => { GameSettings.BatteryDrain = !GameSettings.BatteryDrain; Labels(); });
            lockoutLabel = Button("", settingsPage.transform, () => { GameSettings.KeypadLockout = !GameSettings.KeypadLockout; Labels(); });
            subtitlesLabel = Button("", settingsPage.transform, () => { GameSettings.Subtitles = !GameSettings.Subtitles; Labels(); });
            volume = SliderRow(settingsPage.transform, out volumeLabel, v => { GameSettings.Volume = v; AudioListener.volume = v; Labels(); });
            mouse = SliderRow(settingsPage.transform, out mouseLabel, v => { GameSettings.MouseSensitivity = v; Labels(); });
            size = SliderRow(settingsPage.transform, out sizeLabel, v => { GameSettings.TextSize = v; Labels(); });
            Button("RESET TO DEFAULTS", settingsPage.transform, () => { GameSettings.ResetToDefaults(); ReadSettings(); });
            Button("BACK", settingsPage.transform, ShowMain);

            root.SetActive(false);
        }

        GameObject Page(string name, Transform sheet)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(sheet, false);
            var rt = (RectTransform)go.transform;
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(56f, 40f); rt.offsetMax = new Vector2(-56f, -190f);
            var v = go.AddComponent<VerticalLayoutGroup>();
            v.spacing = 12f;
            v.childControlWidth = v.childControlHeight = true;
            v.childForceExpandWidth = true; v.childForceExpandHeight = false;
            return go;
        }

        Text Button(string text, Transform parent, UnityEngine.Events.UnityAction onClick)
        {
            var img = Box("Button " + text, parent, Theme.PaperDeep);
            Size(img.gameObject, 62f);
            var b = img.gameObject.AddComponent<Button>();
            b.targetGraphic = img;
            var cb = b.colors;
            cb.normalColor = Color.white;
            cb.highlightedColor = new Color(1f, 0.82f, 0.76f);
            cb.pressedColor = new Color(0.86f, 0.7f, 0.64f);
            cb.selectedColor = Color.white;
            cb.fadeDuration = 0.08f;
            b.colors = cb;
            b.onClick.AddListener(onClick);
            var label = Label(text, img.transform, typedBold, 30, Theme.PaperInk, TextAnchor.MiddleLeft);
            Stretch(label.rectTransform);
            label.rectTransform.offsetMin = new Vector2(24f, 0f);
            label.text = ">  " + text;
            if (text == "") label.text = "";
            return label;
        }

        Slider SliderRow(Transform parent, out Text label, UnityEngine.Events.UnityAction<float> onChange)
        {
            label = Label("", parent, typed, 26, Theme.PaperInk, TextAnchor.LowerLeft);
            Size(label.gameObject, 32f);

            var row = new GameObject("Slider", typeof(RectTransform));
            row.transform.SetParent(parent, false);
            Size(row, 34f);
            var track = Box("Track", row.transform, Theme.PaperDeep);
            Stretch(track.rectTransform);
            track.rectTransform.offsetMin = new Vector2(0f, 11f); track.rectTransform.offsetMax = new Vector2(0f, -11f);

            var fillArea = new GameObject("Fill Area", typeof(RectTransform));
            fillArea.transform.SetParent(row.transform, false);
            Stretch((RectTransform)fillArea.transform);
            ((RectTransform)fillArea.transform).offsetMin = new Vector2(0f, 11f);
            ((RectTransform)fillArea.transform).offsetMax = new Vector2(0f, -11f);
            var fill = Box("Fill", fillArea.transform, Theme.Stamp);
            Stretch(fill.rectTransform);

            var handleArea = new GameObject("Handle Area", typeof(RectTransform));
            handleArea.transform.SetParent(row.transform, false);
            Stretch((RectTransform)handleArea.transform);
            ((RectTransform)handleArea.transform).offsetMin = new Vector2(10f, 0f);
            ((RectTransform)handleArea.transform).offsetMax = new Vector2(-10f, 0f);
            var handle = Box("Handle", handleArea.transform, Theme.PaperInk);
            handle.rectTransform.sizeDelta = new Vector2(20f, 0f);

            var s = row.AddComponent<Slider>();
            s.fillRect = fill.rectTransform;
            s.handleRect = handle.rectTransform;
            s.targetGraphic = handle;
            s.onValueChanged.AddListener(onChange);
            return s;
        }

        Text Label(string text, Transform parent, Font font, int size, Color color, TextAnchor anchor)
        {
            var go = new GameObject("Text", typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var t = go.AddComponent<Text>();
            t.font = font;
            t.fontSize = size;
            t.color = color;
            t.alignment = anchor;
            t.text = text;
            t.horizontalOverflow = HorizontalWrapMode.Wrap;
            t.verticalOverflow = VerticalWrapMode.Truncate;
            t.raycastTarget = false;
            return t;
        }

        static Image Box(string name, Transform parent, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var img = go.AddComponent<Image>();
            img.color = color;
            return img;
        }

        static void Size(GameObject go, float height)
        {
            var le = go.GetComponent<LayoutElement>();
            if (le == null) le = go.AddComponent<LayoutElement>();
            le.minHeight = le.preferredHeight = height;
        }

        static void Stretch(RectTransform rt)
        {
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
        }

        // a strip across the top of the sheet: inset from the left and right, this far down, this tall
        static void Place(RectTransform rt, float inset, float top, float height)
        {
            rt.anchorMin = new Vector2(0f, 1f); rt.anchorMax = new Vector2(1f, 1f);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.offsetMin = new Vector2(inset, -top - height);
            rt.offsetMax = new Vector2(-inset, -top);
        }
    }
}
