using System.Reflection;
using HeyHeyThere.EscapePuzzleKitFree;
using UnityEngine;

namespace LastShift.Menus
{
    /// <summary>
    /// Gives the kit's own screens (prompts, inventory, close-ups, code pad) the game's look instead
    /// of the kit's: the typed body font and the <see cref="Theme"/>'s colours. They're drawn with
    /// Unity's built-in skin, whose font is set here once, and coloured from the Hud's accent and
    /// panel colour.
    /// Subtitles are drawn here instead of by the kit, in the security monitor look (amber on a
    /// black screen) like the headset's. While an item is held up close they move to the top,
    /// under the item's name, so they never run across the item's own text.
    /// </summary>
    public class GameFonts : MonoBehaviour
    {
        public Font body;
        [Tooltip("The security monitor font, for subtitles")]
        public Font screen;
        static bool fontSet;

        // the kit keeps these to itself; read from outside so its files stay untouched
        static readonly FieldInfo PanelField = typeof(Hud).GetField("panel", BindingFlags.Instance | BindingFlags.NonPublic);
        static readonly FieldInfo LeftField = typeof(Hud).GetField("subtitleLeft", BindingFlags.Instance | BindingFlags.NonPublic);
        static readonly FieldInfo TextField = typeof(Hud).GetField("subtitle", BindingFlags.Instance | BindingFlags.NonPublic);
        static readonly FieldInfo SpeakerField = typeof(Hud).GetField("speaker", BindingFlags.Instance | BindingFlags.NonPublic);

        float lineLeft;
        GUIStyle style;

        void Start() => Recolour();

        void Recolour()
        {
            foreach (var hud in FindObjectsByType<Hud>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                hud.accent = Theme.Accent;
                hud.panelColor = Theme.Panel;
            }
        }

        static string PanelName(Hud hud) => PanelField != null ? PanelField.GetValue(hud).ToString() : "";

        // late in the frame: the intercom says its lines from a coroutine, which runs after
        // Update, and the kit would draw a line said then behind this one for a frame
        void LateUpdate()
        {
            var hud = Hud.Current;
            if (hud == null || LeftField == null) return;
            // take each new line off the kit, which would draw it in its own style
            var kitLeft = (float)LeftField.GetValue(hud);
            if (kitLeft > 0f) { lineLeft = kitLeft; LeftField.SetValue(hud, 0f); }
            else lineLeft -= Time.unscaledDeltaTime;
        }

        void OnGUI()
        {
            if (!fontSet && body != null)
            {
                GUI.skin.font = body;
                fontSet = true;
            }

            var hud = Hud.Current;
            if (lineLeft <= 0f || hud == null || !hud.isActiveAndEnabled || TextField == null || PauseGuard.IsOpen) return;
            var panel = PanelName(hud);
            if (panel == "Pause" || panel == "End") return;

            GUI.depth = -11;  // over the kit's screens, which draw at -10
            float w = Screen.width, h = Screen.height, u = h * hud.textSize;
            style ??= new GUIStyle(GUI.skin.label) { wordWrap = true, richText = true, alignment = TextAnchor.MiddleCenter };
            style.font = screen != null ? screen : GUI.skin.font;
            style.fontSize = Mathf.Max(8, (int)(u * (screen != null ? 1.25f : 1f)));

            var text = (string)TextField.GetValue(hud);
            var who = SpeakerField != null ? (string)SpeakerField.GetValue(hud) : "";
            var a = Mathf.Clamp01(lineLeft * 2f);
            var amber = Theme.Amber;
            var line = (string.IsNullOrEmpty(who) ? "" : $"<color=#{ColorUtility.ToHtmlStringRGB(amber)}>{who.ToUpperInvariant()}:</color> ") + text;

            // a black screen just big enough for the line
            var width = w * 0.62f;
            var height = style.CalcHeight(new GUIContent(line), width - u * 1.6f) + u * 0.9f;
            var y = panel == "Examine" ? h * 0.05f + u * 2.2f : h * 0.86f - height;
            var box = new Rect((w - width) / 2f, y, width, height);
            var was = GUI.color;
            GUI.color = new Color(Theme.Screen.r, Theme.Screen.g, Theme.Screen.b, Theme.Screen.a * a);
            GUI.DrawTexture(box, Texture2D.whiteTexture);
            GUI.color = new Color(amber.r, amber.g, amber.b, 0.35f * a);
            GUI.DrawTexture(new Rect(box.x, box.y, box.width, 2f), Texture2D.whiteTexture);
            GUI.DrawTexture(new Rect(box.x, box.yMax - 2f, box.width, 2f), Texture2D.whiteTexture);
            GUI.color = was;
            style.normal.textColor = new Color(Theme.AmberText.r, Theme.AmberText.g, Theme.AmberText.b, a);
            GUI.Label(new Rect(box.x + u * 0.8f, box.y, box.width - u * 1.6f, box.height), line, style);
        }
    }
}
