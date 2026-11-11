using HeyHeyThere.EscapePuzzleKitFree;
using UnityEngine;
using UnityEngine.UI;

namespace LastShift.Keypads
{
    /// <summary>
    /// A small screen and status light for a kit Keypad whose keys are pressed in the world
    /// (set the Keypad's "Use Pad" off). Shows the digits as they're typed, flashes red on a
    /// wrong code and turns green when it opens. The kit already moves each key and plays its
    /// click; this adds what the player sees and hears on top.
    /// Right-click the component > Create Screen to make the screen objects, then move them.
    /// </summary>
    public class KeypadScreen : MonoBehaviour
    {
        [Tooltip("Empty: the Keypad on this object or a parent")]
        public HeyHeyThere.EscapePuzzleKitFree.Keypad keypad;

        [Header("Screen")]
        public Text digits;
        public Image statusLight;

        [Header("Colours")]
        public Color waiting = new Color(1f, 0.7f, 0.2f);
        public Color wrong = new Color(0.95f, 0.2f, 0.15f);
        public Color right = new Color(0.25f, 0.9f, 0.35f);

        [Header("Wrong code")]
        [Tooltip("How long the screen shows ERROR and the light stays red")]
        public float wrongSeconds = 1.2f;
        public AudioClip wrongSound;

        [Header("Right code")]
        public AudioClip rightSound;
        [Tooltip("Subtitle line when it opens; empty for none")]
        public string openLine = "";

        [Header("Key press")]
        [Tooltip("How far a key sinks when pressed, in metres. The kit's model only animates some keys, so every key gets this")]
        public float pressDepth = 0.004f;
        public float pressSeconds = 0.12f;

        [Header("Lockout")]
        [Tooltip("Wrong codes in a row before the keypad locks")]
        public int wrongTries = 3;
        [Tooltip("How long it stays locked, in seconds")]
        public float lockSeconds = 30f;
        public AudioClip lockSound;
        [Tooltip("Said the first time any keypad locks (Habimana's H05)")]
        public LastShift.Narration.VoiceLine lockLine;
        [Tooltip("Silence between the buzz ending and the line starting, in seconds")]
        public float lockLinePause = 0.6f;
        public string lockText = "[alarm buzz] The keypad locks.";

        /// <summary>Whether keypads lock after too many wrong codes. The settings menu can turn it off.</summary>
        public static bool LockoutOn = true;
        const string TauntedFlag = "keypad_lockout_taunted";

        float wrongLeft, lockedLeft;
        int wrongInARow;
        bool startEnabled;
        readonly System.Collections.Generic.Dictionary<string, Transform> keys = new System.Collections.Generic.Dictionary<string, Transform>();
        readonly System.Collections.Generic.Dictionary<Transform, Vector3> rest = new System.Collections.Generic.Dictionary<Transform, Vector3>();
        readonly System.Collections.Generic.Dictionary<Transform, float> pressedAt = new System.Collections.Generic.Dictionary<Transform, float>();
        Vector3 inward;

        void Awake()
        {
            if (keypad == null)
                keypad = GetComponentInParent<HeyHeyThere.EscapePuzzleKitFree.Keypad>();
            if (keypad != null)
                startEnabled = keypad.enabled;
            FindKeys();
            if (wrongSound == null) wrongSound = Tone("Keypad Wrong", new[] { (220f, 0.12f), (0f, 0.06f), (180f, 0.18f) }, 0.35f);
            if (rightSound == null) rightSound = Tone("Keypad Right", new[] { (660f, 0.08f), (880f, 0.08f), (1320f, 0.16f) }, 0.3f);
            if (lockSound == null) lockSound = Tone("Keypad Locked", new[] { (110f, 0.9f) }, 0.4f);
        }

        public bool Locked => lockedLeft > 0f;

        void OnEnable()
        {
            if (keypad == null)
                return;
            keypad.onSwitchedOn.AddListener(OnOpened);
            keypad.onRefused.AddListener(OnRefused);
            keypad.onKeyed.AddListener(OnKeyed);
        }

        void OnDisable()
        {
            if (keypad == null)
                return;
            keypad.onSwitchedOn.RemoveListener(OnOpened);
            keypad.onRefused.RemoveListener(OnRefused);
            keypad.onKeyed.RemoveListener(OnKeyed);
        }

        /// <summary>The key parts (named *_key_1, *_key_C) and which way is "into" the keypad.</summary>
        void FindKeys()
        {
            if (keypad == null)
                return;
            var keyBounds = new Bounds();
            var any = false;
            foreach (var r in keypad.GetComponentsInChildren<Renderer>(true))
            {
                var k = r.name.LastIndexOf("_key_", System.StringComparison.Ordinal);
                if (k < 0)
                    continue;
                keys[r.name.Substring(k + 5)] = r.transform;
                if (any) keyBounds.Encapsulate(r.bounds); else { keyBounds = r.bounds; any = true; }
            }
            if (!any)
                return;
            var body = keyBounds;
            foreach (var r in keypad.GetComponentsInChildren<Renderer>(true))
                body.Encapsulate(r.bounds);
            var outward = keyBounds.center - body.center;
            outward.y = 0f;
            inward = outward.sqrMagnitude > 1e-6f ? -outward.normalized : -keypad.transform.forward;
        }

        void OnKeyed(string key)
        {
            if (!keys.TryGetValue(key, out var t) || t == null)
                return;
            if (!pressedAt.ContainsKey(t))
                rest[t] = t.localPosition;
            pressedAt[t] = Time.time;
        }

        /// <summary>Sinks each pressed key and lets it back out.</summary>
        void AnimatePresses()
        {
            if (pressedAt.Count == 0)
                return;
            var done = new System.Collections.Generic.List<Transform>();
            foreach (var kv in pressedAt)
            {
                var t = kv.Key;
                if (t == null) { done.Add(t); continue; }
                var u = (Time.time - kv.Value) / Mathf.Max(0.01f, pressSeconds);
                var depth = u < 0.4f ? u / 0.4f : Mathf.Clamp01(1f - (u - 0.4f) / 0.6f);
                var offset = t.parent != null ? t.parent.InverseTransformVector(inward * pressDepth) : inward * pressDepth;
                t.localPosition = rest[t] + offset * depth;
                if (u >= 1f)
                {
                    t.localPosition = rest[t];
                    done.Add(t);
                }
            }
            foreach (var t in done)
                pressedAt.Remove(t);
        }

        void OnRefused(string why)
        {
            if (why != "code")
                return;
            wrongLeft = wrongSeconds;
            wrongInARow++;
            if (LockoutOn && wrongTries > 0 && wrongInARow >= wrongTries)
                Lock();
            else
                Play(wrongSound);
        }

        /// <summary>Stops the keys working for a while: the kit can't focus or use a disabled keypad.</summary>
        void Lock()
        {
            wrongInARow = 0;
            lockedLeft = lockSeconds;
            keypad.enabled = false;
            Play(lockSound);
            if (!string.IsNullOrEmpty(lockText))
                Run.Say(lockText);
            if (lockLine != null && !Run.HasFlag(TauntedFlag))
            {
                Run.SetFlag(TauntedFlag);
                StartCoroutine(SayLockLine());
            }
        }

        // after the buzz has died away, so the voice doesn't talk over it
        System.Collections.IEnumerator SayLockLine()
        {
            var buzz = lockSound != null ? lockSound.length : 0f;
            yield return new WaitForSecondsRealtime(buzz + lockLinePause);
            LastShift.Narration.Intercom.Play(lockLine);
        }

        void Unlock()
        {
            lockedLeft = 0f;
            if (keypad != null && startEnabled)
                keypad.enabled = true;
        }

        void OnOpened()
        {
            wrongLeft = 0f;
            wrongInARow = 0;
            Play(rightSound);
            if (!string.IsNullOrEmpty(openLine))
                Run.Say(openLine);
        }

        void LateUpdate()
        {
            if (keypad == null)
                return;
            AnimatePresses();
            if (wrongLeft > 0f)
                wrongLeft -= Time.deltaTime;
            if (lockedLeft > 0f)
            {
                lockedLeft -= Time.deltaTime;
                if (lockedLeft <= 0f || !LockoutOn) Unlock();
            }
            else if (startEnabled && !keypad.enabled && !keypad.IsOn)
                keypad.enabled = true;  // a save made mid-lockout loads it switched off

            if (keypad.IsOn)
                Show("OPEN", right);
            else if (!keypad.Powered)
                Show("", Color.black);
            else if (lockedLeft > 0f)
                Show($"LOCKED {Mathf.CeilToInt(lockedLeft) / 60}:{Mathf.CeilToInt(lockedLeft) % 60:00}", wrong);
            else if (wrongLeft > 0f)
                Show("ERROR", wrong);
            else
                Show(Masked(keypad.Entry, keypad.code.Length), waiting);
        }

        /// <summary>"40" with a 4-digit code reads "4 0 _ _".</summary>
        static string Masked(string entry, int length)
        {
            var chars = new string[Mathf.Max(length, entry.Length)];
            for (var i = 0; i < chars.Length; i++)
                chars[i] = i < entry.Length ? entry[i].ToString() : "_";
            return string.Join(" ", chars);
        }

        void Show(string text, Color color)
        {
            if (digits != null)
            {
                digits.text = text;
                digits.color = color;
                // "LOCKED 0:30" is wider than the digits; shrink it to fit
                digits.resizeTextForBestFit = true;
                digits.resizeTextMinSize = 16;
                digits.resizeTextMaxSize = Mathf.Max(digits.resizeTextMaxSize, digits.fontSize);
            }
            if (statusLight != null)
                statusLight.color = color;
        }

        void Play(AudioClip clip)
        {
            if (clip != null)
                AudioSource.PlayClipAtPoint(clip, transform.position, 0.8f);
        }

        /// <summary>A beep made in code from (frequency, seconds) steps, 0 Hz for a gap. No sound files needed.</summary>
        static AudioClip Tone(string name, (float hz, float seconds)[] steps, float volume)
        {
            const int rate = 22050;
            var data = new System.Collections.Generic.List<float>();
            foreach (var (hz, seconds) in steps)
            {
                int n = (int)(rate * seconds);
                for (int i = 0; i < n; i++)
                {
                    float env = Mathf.Min(1f, i / (rate * 0.005f)) * Mathf.Min(1f, (n - i) / (rate * 0.01f));
                    float s = hz <= 0f ? 0f : Mathf.Sign(Mathf.Sin(2f * Mathf.PI * hz * i / rate)) * 0.6f
                                                  + Mathf.Sin(2f * Mathf.PI * hz * 2f * i / rate) * 0.2f;
                    data.Add(s * env * volume);
                }
            }
            var clip = AudioClip.Create(name, data.Count, 1, rate, false);
            clip.SetData(data.ToArray(), 0);
            return clip;
        }

        /// <summary>
        /// Makes a small screen above the keypad's keys: a dark panel, the digits and a light.
        /// It lands roughly in place; move and scale it by hand to sit on the model.
        /// </summary>
        [ContextMenu("Create Screen")]
        void CreateScreen()
        {
            if (digits != null)
            {
                Debug.LogWarning("KeypadScreen already has a screen.", this);
                return;
            }

            var keys = new Bounds();
            var found = false;
            foreach (var r in GetComponentsInChildren<Renderer>(true))
            {
                if (!r.name.Contains("_key_"))
                    continue;
                if (found) keys.Encapsulate(r.bounds);
                else { keys = r.bounds; found = true; }
            }
            var spot = found ? keys.center + Vector3.up * (keys.extents.y + 0.04f) : transform.position;

            var screen = new GameObject("Code Screen", typeof(RectTransform));
            screen.transform.SetParent(transform, false);
            screen.transform.position = spot;
            screen.transform.rotation = transform.rotation;
            var canvas = screen.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            screen.AddComponent<CanvasScaler>().dynamicPixelsPerUnit = 3f;
            var rt = (RectTransform)screen.transform;
            rt.sizeDelta = new Vector2(240f, 70f);
            rt.localScale = Vector3.one * 0.0005f / Mathf.Max(0.0001f, transform.lossyScale.x);

            var panel = screen.AddComponent<Image>();
            panel.color = new Color(0.03f, 0.04f, 0.03f, 1f);
            panel.raycastTarget = false;

            digits = NewChild<Text>(screen.transform, "Digits", new Vector2(-15f, 0f), new Vector2(190f, 64f));
            digits.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            digits.fontSize = 44;
            digits.fontStyle = FontStyle.Bold;
            digits.alignment = TextAnchor.MiddleCenter;
            digits.raycastTarget = false;
            digits.text = "_ _ _ _";
            digits.color = waiting;

            statusLight = NewChild<Image>(screen.transform, "Status Light", new Vector2(98f, 0f), new Vector2(22f, 22f));
            statusLight.color = waiting;
            statusLight.raycastTarget = false;
        }

        static T NewChild<T>(Transform parent, string name, Vector2 pos, Vector2 size) where T : Component
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform;
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
            return go.AddComponent<T>();
        }
    }
}
