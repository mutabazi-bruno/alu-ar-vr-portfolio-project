using UnityEngine;

namespace LastShift.Menus
{
    /// <summary>
    /// The game's look in one place, so every screen matches. Two looks:
    ///  - "Case file" for the menus, notes and end card: sheets from the archive, aged manila
    ///    paper, typed text and a red rubber stamp.
    ///  - "Security monitor" for anything Habimana watches or says: his subtitles, the HUD in the
    ///    headset, the keypad screens. An old amber screen on black.
    /// </summary>
    public static class Theme
    {
        // case file
        public static readonly Color Paper = new Color32(0xC9, 0xB8, 0x8F, 0xFA);       // manila, dimmed so it doesn't glare in a headset
        public static readonly Color PaperDeep = new Color32(0xB5, 0xA2, 0x77, 0xFF);   // buttons, tracks and boxes on the paper
        public static readonly Color PaperInk = new Color32(0x2B, 0x23, 0x18, 0xFF);    // typed text
        public static readonly Color PaperDim = new Color32(0x4A, 0x3D, 0x2A, 0xFF);    // faded typing, help lines
        public static readonly Color Stamp = new Color32(0xA3, 0x26, 0x1B, 0xFF);       // red rubber stamp
        public static readonly Color Backdrop = new Color32(0x0B, 0x0D, 0x10, 0xFF);    // behind the paper

        // security monitor
        public static readonly Color Amber = new Color32(0xFF, 0xB3, 0x47, 0xFF);       // bright amber, names and keys
        public static readonly Color AmberText = new Color32(0xF2, 0xC2, 0x7A, 0xFF);   // softer amber for reading
        public static readonly Color Screen = new Color32(0x06, 0x08, 0x07, 0xEB);      // the monitor's black

        // used across the game's screens: highlights are the monitor's amber, panels its black
        public static readonly Color Accent = Amber;
        public static readonly Color Ink = AmberText;
        public static readonly Color Dim = new Color32(0x9A, 0x86, 0x66, 0xFF);
        public static readonly Color Panel = Screen;
        public static readonly Color Button = new Color32(0x14, 0x12, 0x0E, 0xF2);
        public static readonly Color Alert = new Color32(0xD9, 0x48, 0x3B, 0xFF);       // signal red
    }
}
