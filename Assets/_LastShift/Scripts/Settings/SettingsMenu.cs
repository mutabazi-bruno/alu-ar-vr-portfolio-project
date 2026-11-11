using HeyHeyThere.EscapePuzzleKitFree;
using UnityEngine;

namespace LastShift.Settings
{
    /// <summary>
    /// A settings panel shown beside the kit's pause menu (Esc), drawn in the Hud's own style:
    /// battery drain, keypad lockout, subtitles, volume, mouse speed and text size.
    /// The kit's Hud is left untouched.
    /// </summary>
    public class SettingsMenu : MonoBehaviour
    {
        GUIStyle label;
        float u;
        Color accent = new Color(0.96f, 0.78f, 0.36f);
        Color panel = new Color(0.06f, 0.07f, 0.08f, 0.92f);

        // The kit stops time only for its pause menu and its end screen.
        static bool PauseMenuOpen =>
            Hud.Current != null && Hud.Current.IsOpen && Time.timeScale == 0f && !Run.HasEscaped;

        void OnGUI()
        {
            if (!PauseMenuOpen)
                return;

            var hud = Hud.Current;
            accent = hud.accent;
            panel = hud.panelColor;
            // The panel keeps one size whatever the text size setting, so it always fits the screen.
            u = Screen.height * hud.textSize / GameSettings.TextSize;
            label ??= new GUIStyle(GUI.skin.label) { wordWrap = true };
            GUI.depth = -20;  // over the Hud, which draws at -10

            // The kit's pause buttons are a column 12 units wide in the middle; this sits to their right.
            float w = Screen.width, h = Screen.height;
            var box = new Rect(Mathf.Min(w / 2f + u * 7f, w - u * 14.5f), h * 0.1f, u * 14f, u * 20.4f);
            Fill(box, panel);
            float x = box.x + u * 0.8f, width = box.width - u * 1.6f, y = box.y + u * 0.6f;

            Text(new Rect(x, y, width, u * 1.4f), "SETTINGS", u * 1.1f, accent, TextAnchor.MiddleLeft);
            y += u * 2f;

            bool drain = GameSettings.BatteryDrain;
            if (Button(new Rect(x, y, width, u * 1.8f), drain ? "BATTERY DRAIN: ON" : "BATTERY DRAIN: OFF"))
                GameSettings.BatteryDrain = !drain;
            y += u * 2f;
            Note(ref y, x, width, drain ? "The flashlight runs down; batteries refill it." : "The flashlight never runs out.");

            bool lockout = GameSettings.KeypadLockout;
            if (Button(new Rect(x, y, width, u * 1.8f), lockout ? "KEYPAD LOCKOUT: ON" : "KEYPAD LOCKOUT: OFF"))
                GameSettings.KeypadLockout = !lockout;
            y += u * 2f;
            Note(ref y, x, width, lockout ? "Three wrong codes lock a keypad for 30 seconds." : "Keypads never lock.");

            bool subtitles = GameSettings.Subtitles;
            if (Button(new Rect(x, y, width, u * 1.8f), subtitles ? "SUBTITLES: ON" : "SUBTITLES: OFF"))
                GameSettings.Subtitles = !subtitles;
            y += u * 2.2f;

            float volume = GameSettings.Volume;
            if (Slider(ref y, x, width, $"VOLUME  {Mathf.RoundToInt(volume * 100f)}%", ref volume, 0f, 1f))
                GameSettings.Volume = volume;
            float mouse = GameSettings.MouseSensitivity;
            if (Slider(ref y, x, width, $"MOUSE SPEED  {mouse:0.0}x", ref mouse, GameSettings.MinMouse, GameSettings.MaxMouse))
                GameSettings.MouseSensitivity = mouse;
            float textSize = GameSettings.TextSize;
            if (Slider(ref y, x, width, $"TEXT SIZE  {Mathf.RoundToInt(textSize * 100f)}%", ref textSize, GameSettings.MinTextSize, GameSettings.MaxTextSize))
                GameSettings.TextSize = textSize;
            y += u * 0.2f;

            if (Button(new Rect(x, y, width, u * 1.4f), "RESET TO DEFAULTS"))
                GameSettings.ResetToDefaults();
        }

        void Note(ref float y, float x, float width, string text)
        {
            Text(new Rect(x, y, width, u * 1.2f), text, u * 0.6f, new Color(1f, 1f, 1f, 0.6f), TextAnchor.UpperLeft, true);
            y += u * 1.4f;
        }

        // Whether the player moved it, so a setting is only saved when it changes, not every frame.
        bool Slider(ref float y, float x, float width, string title, ref float value, float min, float max)
        {
            Text(new Rect(x, y, width, u), title, u * 0.7f, Color.white, TextAnchor.MiddleLeft);
            y += u * 1.1f;
            float moved = GUI.HorizontalSlider(new Rect(x, y, width, u), value, min, max);
            y += u * 1.4f;
            if (Mathf.Approximately(moved, value))
                return false;
            value = moved;
            return true;
        }

        void Fill(Rect r, Color c)
        {
            var was = GUI.color;
            GUI.color = c;
            GUI.DrawTexture(r, Texture2D.whiteTexture);
            GUI.color = was;
        }

        void Text(Rect r, string text, float size, Color color, TextAnchor anchor, bool wrap = false)
        {
            label.fontSize = Mathf.Max(8, (int)size);
            label.alignment = anchor;
            label.wordWrap = wrap;
            label.normal.textColor = color;
            GUI.Label(r, text, label);
        }

        bool Button(Rect r, string text)
        {
            var hover = r.Contains(Event.current.mousePosition);
            Fill(r, hover ? new Color(accent.r, accent.g, accent.b, 0.3f) : new Color(1f, 1f, 1f, 0.08f));
            Text(r, text, u * 0.75f, hover ? accent : Color.white, TextAnchor.MiddleCenter);
            return GUI.Button(r, GUIContent.none, GUIStyle.none);
        }
    }
}
