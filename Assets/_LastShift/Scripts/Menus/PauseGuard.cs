using System.Reflection;
using HeyHeyThere.EscapePuzzleKitFree;
using UnityEngine;
using UnityEngine.InputSystem;

namespace LastShift.Menus
{
    /// <summary>
    /// Two fixes to the kit's pause menu, without changing the kit:
    ///  - it opens on Esc and on the right mouse button; only Esc should, so this closes it again in
    ///    the same frame when right-click opened it (right-click still works as "back" elsewhere);
    ///  - its QUIT calls Application.Quit, which the Editor ignores and which would close a desktop
    ///    build outright. A click on QUIT is caught first: the Editor stops Play mode, a build goes
    ///    back to the title screen (whose own Quit really closes the game).
    /// </summary>
    [DefaultExecutionOrder(-500)]
    public class PauseGuard : MonoBehaviour
    {
        bool wasOpen;
        static readonly FieldInfo PanelField = typeof(Hud).GetField("panel", BindingFlags.Instance | BindingFlags.NonPublic);

        void Update()
        {
            wasOpen = Hud.Current != null && Hud.Current.IsOpen;
        }

        void LateUpdate()
        {
            var hud = Hud.Current;
            if (hud == null || wasOpen || !hud.IsOpen) return;
            var mouse = Mouse.current;
            var keys = Keyboard.current;
            bool right = mouse != null && mouse.rightButton.wasPressedThisFrame;
            bool esc = keys != null && keys.escapeKey.wasPressedThisFrame;
            if (right && !esc) hud.Close();
        }

        static bool PauseShown(Hud hud) =>
            hud != null && hud.isActiveAndEnabled && PanelField != null && PanelField.GetValue(hud)?.ToString() == "Pause";

        // drawn just above the kit's HUD, over the exact spot of its QUIT button
        void OnGUI()
        {
            var hud = Hud.Current;
            if (!PauseShown(hud)) return;
            float w = Screen.width, h = Screen.height, u = h * hud.textSize;
            var quit = new Rect(w / 2f - u * 6f, h * 0.45f + u * 6.6f, u * 12f, u * 1.8f);
            GUI.depth = -20;
            if (GUI.Button(quit, GUIContent.none, GUIStyle.none))
                Quit(hud);
        }

        static void Quit(Hud hud)
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            hud.Close();
            GameFlow.ToTitle();
#endif
        }
    }
}
