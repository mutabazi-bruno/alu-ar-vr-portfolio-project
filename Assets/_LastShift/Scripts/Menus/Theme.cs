using UnityEngine;

namespace LastShift.Menus
{
    /// <summary>
    /// The game's look in one place, so every screen matches: the title menu, the kit's HUD and
    /// pause menu, the VR HUD and the end screen. "Night archive": cold dark panels, parchment
    /// text, a pale fluorescent teal for highlights and a signal red for warnings.
    /// </summary>
    public static class Theme
    {
        public static readonly Color Accent = new Color32(0x6F, 0xC7, 0xBA, 0xFF);      // fluorescent teal
        public static readonly Color AccentDeep = new Color32(0x1E, 0x4A, 0x46, 0xF2);  // teal for filled buttons
        public static readonly Color Ink = new Color32(0xEC, 0xE4, 0xD2, 0xFF);         // parchment
        public static readonly Color Dim = new Color32(0x9A, 0x96, 0x8C, 0xFF);         // faded ink
        public static readonly Color Panel = new Color32(0x0D, 0x11, 0x16, 0xEB);       // cold blue-black
        public static readonly Color Button = new Color32(0x16, 0x1C, 0x22, 0xF2);
        public static readonly Color Alert = new Color32(0xD9, 0x48, 0x3B, 0xFF);       // signal red
    }
}
