using HeyHeyThere.EscapePuzzleKitFree;
using UnityEngine;

namespace LastShift.Settings
{
    /// <summary>
    /// A settings panel shown beside the kit's pause menu (Esc), drawn in the Hud's own style:
    /// battery drain on or off, volume and mouse speed. The kit's Hud is left untouched.
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
            u = Screen.height * hud.textSize;
            label ??= new GUIStyle(GUI.skin.label) { wordWrap = true };
            GUI.depth = -20;  // over the Hud, which draws at -10

            // The kit's pause buttons are a column 12 units wide in the middle; this sits to their right.
            float w = Screen.width, h = Screen.height;
            var box = new Rect(Mathf.Min(w / 2f + u * 7f, w - u * 14.5f), h * 0.45f - u * 2.4f, u * 14f, u * 13.4f);
            Fill(box, panel);
            float x = box.x + u * 0.8f, width = box.width - u * 1.6f, y = box.y + u * 0.6f;

            Text(new Rect(x, y, width, u * 1.4f), "SETTINGS", u * 1.1f, accent, TextAnchor.MiddleLeft);
            y += u * 2f;

            bool drain = GameSettings.BatteryDrain;
            if (Button(new Rect(x, y, width, u * 1.8f), drain ? "BATTERY DRAIN: ON" : "BATTERY DRAIN: OFF"))
                GameSettings.BatteryDrain = !drain;
            y += u * 2f;
            Text(new Rect(x, y, width, u * 1.2f), drain ? "The flashlight runs down; batteries refill it." : "The flashlight never runs out.",
                u * 0.6f, new Color(1f, 1f, 1f, 0.6f), TextAnchor.UpperLeft, true);
            y += u * 1.6f;

            float volume = GameSettings.Volume;
            Text(new Rect(x, y, width, u), $"VOLUME  {Mathf.RoundToInt(volume * 100f)}%", u * 0.7f, Color.white, TextAnchor.MiddleLeft);
            y += u * 1.1f;
            float newVolume = GUI.HorizontalSlider(new Rect(x, y, width, u), volume, 0f, 1f);
            if (!Mathf.Approximately(newVolume, volume))
                GameSettings.Volume = newVolume;
            y += u * 1.4f;

            float mouse = GameSettings.MouseSensitivity;
            Text(new Rect(x, y, width, u), $"MOUSE SPEED  {mouse:0.0}x", u * 0.7f, Color.white, TextAnchor.MiddleLeft);
            y += u * 1.1f;
            float newMouse = GUI.HorizontalSlider(new Rect(x, y, width, u), mouse, GameSettings.MinMouse, GameSettings.MaxMouse);
            if (!Mathf.Approximately(newMouse, mouse))
                GameSettings.MouseSensitivity = newMouse;
            y += u * 1.6f;

            if (Button(new Rect(x, y, width, u * 1.4f), "RESET TO DEFAULTS"))
                GameSettings.ResetToDefaults();
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
