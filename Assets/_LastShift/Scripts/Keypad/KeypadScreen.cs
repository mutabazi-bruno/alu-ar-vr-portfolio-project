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

        float wrongLeft;
        readonly System.Collections.Generic.Dictionary<string, Transform> keys = new System.Collections.Generic.Dictionary<string, Transform>();
        readonly System.Collections.Generic.Dictionary<Transform, Vector3> rest = new System.Collections.Generic.Dictionary<Transform, Vector3>();
        readonly System.Collections.Generic.Dictionary<Transform, float> pressedAt = new System.Collections.Generic.Dictionary<Transform, float>();
        Vector3 inward;

        void Awake()
        {
            if (keypad == null)
                keypad = GetComponentInParent<HeyHeyThere.EscapePuzzleKitFree.Keypad>();
            FindKeys();
        }

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
            Play(wrongSound);
        }

        void OnOpened()
        {
            wrongLeft = 0f;
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

            if (keypad.IsOn)
                Show("OPEN", right);
            else if (!keypad.Powered)
                Show("", Color.black);
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
            }
            if (statusLight != null)
                statusLight.color = color;
        }

        void Play(AudioClip clip)
        {
            if (clip != null)
                AudioSource.PlayClipAtPoint(clip, transform.position, 0.8f);
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
