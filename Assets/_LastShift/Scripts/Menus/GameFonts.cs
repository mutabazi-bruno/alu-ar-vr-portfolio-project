using UnityEngine;

namespace LastShift.Menus
{
    /// <summary>
    /// Gives the kit's own screens (pause menu, inventory, subtitles on a monitor, the settings
    /// panel) the game's body font. They're drawn with Unity's built-in skin, whose font is set
    /// here once; everything that copies the skin's styles then uses it.
    /// </summary>
    public class GameFonts : MonoBehaviour
    {
        public Font body;
        static bool set;

        void OnGUI()
        {
            if (set || body == null) return;
            GUI.skin.font = body;
            set = true;
        }
    }
}
