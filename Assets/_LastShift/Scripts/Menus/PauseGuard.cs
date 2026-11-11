using HeyHeyThere.EscapePuzzleKitFree;
using UnityEngine;
using UnityEngine.InputSystem;

namespace LastShift.Menus
{
    /// <summary>
    /// The kit opens its pause menu on Esc and on the right mouse button. Only Esc should: this
    /// closes the menu again in the same frame when right-click opened it. Right-click still works
    /// as "back" inside the inventory and the examine view.
    /// </summary>
    [DefaultExecutionOrder(-500)]
    public class PauseGuard : MonoBehaviour
    {
        bool wasOpen;

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
    }
}
