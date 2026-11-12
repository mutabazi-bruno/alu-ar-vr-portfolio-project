using HeyHeyThere.EscapePuzzleKitFree;
using UnityEngine;
using UnityEngine.UI;

namespace LastShift.XR
{
    /// <summary>
    /// The kit's screen HUD, for VR. The kit draws its HUD flat on the screen, which a headset
    /// never shows, so this puts the same information in the world:
    ///  - a prompt beside whatever the ray rests on ("Trigger  OPEN  Office door")
    ///  - a panel that drifts in front of the view (LazyFollow) with the objective, the lines
    ///    said (with speaker) and control hints, on the same timings as the kit's own HUD.
    /// Lives on the XR rig, so it only runs while the rig is the active player.
    /// </summary>
    public class XRWorldHud : MonoBehaviour
    {
        [Header("Sources")]
        public XRKitPointer pointer;
        public Transform head;

        [Header("Prompt beside the object")]
        public RectTransform prompt;
        public Text promptText;
        [Tooltip("Prompt size at 1 m; it grows with distance so it stays readable")]
        public float promptScale = 0.0008f;

        [Header("Panel in front of the view")]
        public CanvasGroup objectiveGroup;
        public Text objectiveText;
        public CanvasGroup lineGroup;
        public Text lineText;
        public CanvasGroup hintGroup;
        public Text hintText;

        [Header("Look")]
        public Color accent = new Color(0.96f, 0.78f, 0.36f);
        [Tooltip("Accessibility: scales every HUD text")]
        [Range(0.75f, 2f)] public float textScale = 1f;
        [Tooltip("What the VR control for 'use' is called in prompts")]
        public string useControl = "Trigger";
        [Tooltip("What the VR control for the torch is called in hints")]
        public string torchControl = "X / A";

        float objectiveLeft, lineLeft, hintLeft;
        Text[] texts;
        int[] baseSizes;
        float appliedScale = -1f;

        void Awake()
        {
            accent = LastShift.Menus.Theme.Accent;
            // the panels behind the text take the game's panel colour
            foreach (var img in GetComponentsInChildren<Image>(true))
                if (img.color.r < 0.2f && img.color.g < 0.2f && img.color.b < 0.2f)
                    img.color = new Color(LastShift.Menus.Theme.Panel.r, LastShift.Menus.Theme.Panel.g, LastShift.Menus.Theme.Panel.b, img.color.a);
            texts = new[] { promptText, objectiveText, lineText, hintText };
            baseSizes = new int[texts.Length];
            for (var i = 0; i < texts.Length; i++)
                baseSizes[i] = texts[i] != null ? texts[i].fontSize : 0;
        }

        void OnEnable()
        {
            Run.Said += OnSaid;
            Run.ObjectiveChanged += OnObjective;
            Run.Hinted += OnHint;
            SetAlpha(objectiveGroup, 0f);
            SetAlpha(lineGroup, 0f);
            SetAlpha(hintGroup, 0f);
            if (prompt != null)
                prompt.gameObject.SetActive(false);
        }

        void OnDisable()
        {
            Run.Said -= OnSaid;
            Run.ObjectiveChanged -= OnObjective;
            Run.Hinted -= OnHint;
        }

        void OnSaid(string text, string speaker)
        {
            if (lineText == null)
                return;
            var hex = ColorUtility.ToHtmlStringRGB(accent);
            lineText.text = (string.IsNullOrEmpty(speaker) ? "" : $"<color=#{hex}>{speaker}:</color> ") + text;
            lineLeft = 2f + text.Length * 0.06f;  // the kit's own subtitle timing
        }

        void OnObjective(string text)
        {
            if (objectiveText == null)
                return;
            objectiveText.text = text;
            objectiveLeft = string.IsNullOrEmpty(text) ? 0f : 6f;
        }

        void OnHint(string key, string verb)
        {
            if (hintText == null)
                return;
            var control = VrControl(key);
            var hex = ColorUtility.ToHtmlStringRGB(accent);
            hintText.text = control != "" ? $"<color=#{hex}>{control}</color>  {verb}" : verb;
            hintLeft = 5f;
        }

        /// <summary>The kit names desktop keys; say the VR control instead, or nothing.</summary>
        string VrControl(string key)
        {
            switch ((key ?? "").ToUpperInvariant())
            {
                case "E": return useControl;
                case "F": return torchControl;
                default: return "";
            }
        }

        void Update()
        {
            // The kit's flat code pad and inventory can't be seen in a headset, and while open
            // they freeze the pointer. In VR the keypad's own keys are used instead.
            if (Hud.Current != null && Hud.Current.IsOpen && !Run.HasEscaped)
                Hud.Current.Close();

            ApplyTextScale();
            UpdatePrompt();

            Tick(ref objectiveLeft, objectiveGroup);
            Tick(ref lineLeft, lineGroup);
            Tick(ref hintLeft, hintGroup);
        }

        void UpdatePrompt()
        {
            if (prompt == null || promptText == null)
                return;

            var focus = pointer != null ? pointer.Focus : null;
            var part = pointer != null ? pointer.FocusPart : null;
            if (focus == null || part == null || head == null)
            {
                prompt.gameObject.SetActive(false);
                return;
            }

            // Sit where the ray meets the object, nudged towards the player and up a little.
            var ray = pointer.Ray;
            var spot = part.Raycast(ray, out var hit, pointer.Reach + 0.5f) ? hit.point : part.bounds.center;
            var toHead = (head.position - spot).normalized;
            var pos = spot + toHead * 0.06f + Vector3.up * 0.07f;

            prompt.position = pos;
            prompt.rotation = Quaternion.LookRotation(pos - head.position, Vector3.up);
            var distance = Mathf.Clamp(Vector3.Distance(head.position, pos), 0.4f, 4f);
            prompt.localScale = Vector3.one * promptScale * distance;

            var hex = ColorUtility.ToHtmlStringRGB(accent);
            promptText.text = $"<color=#{hex}>{useControl}</color>  {focus.Verb}" +
                              (string.IsNullOrEmpty(focus.label) ? "" : "  " + focus.label);
            prompt.gameObject.SetActive(true);
        }

        void ApplyTextScale()
        {
            // the player's text size setting, shared with the desktop HUD
            textScale = Mathf.Clamp(LastShift.Settings.GameSettings.TextSize, 0.75f, 2f);
            if (Mathf.Approximately(appliedScale, textScale))
                return;
            appliedScale = textScale;
            for (var i = 0; i < texts.Length; i++)
                if (texts[i] != null)
                    texts[i].fontSize = Mathf.RoundToInt(baseSizes[i] * textScale);
        }

        static void Tick(ref float left, CanvasGroup group)
        {
            if (left > 0f)
                left -= Time.unscaledDeltaTime;
            SetAlpha(group, Mathf.Clamp01(left * 2f));  // fades over the last half second
        }

        static void SetAlpha(CanvasGroup group, float a)
        {
            if (group != null)
                group.alpha = a;
        }
    }
}
