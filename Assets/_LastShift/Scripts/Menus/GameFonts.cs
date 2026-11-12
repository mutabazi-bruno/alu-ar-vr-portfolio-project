using HeyHeyThere.EscapePuzzleKitFree;
using UnityEngine;

namespace LastShift.Menus
{
    /// <summary>
    /// Gives the kit's own screens (prompts, subtitles on a monitor, inventory, pause menu, code
    /// pad and the settings panel drawn in the same style) the game's look instead of the kit's:
    /// the body font, and the <see cref="Theme"/>'s colours. They're drawn with Unity's built-in
    /// skin, whose font is set here once, and coloured from the Hud's accent and panel colour.
    /// </summary>
    public class GameFonts : MonoBehaviour
    {
        public Font body;
        static bool fontSet;

        void Start() => Recolour();

        void Recolour()
        {
            foreach (var hud in FindObjectsByType<Hud>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                hud.accent = Theme.Accent;
                hud.panelColor = Theme.Panel;
            }
        }

        void OnGUI()
        {
            if (fontSet || body == null) return;
            GUI.skin.font = body;
            fontSet = true;
        }
    }
}
