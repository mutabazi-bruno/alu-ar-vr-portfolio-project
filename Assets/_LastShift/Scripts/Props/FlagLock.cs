using HeyHeyThere.EscapePuzzleKitFree;
using UnityEngine;

namespace LastShift.Props
{
    /// <summary>
    /// Keeps a container shut until a story flag is set: the kit locks a container with an item
    /// (a padlock) or a code (a keypad), but Room C's wardrobe opens once the clothes are hung.
    /// Until then it says its locked text; then it enables the container and steps aside.
    /// With no flag it stays shut for good: Room C's dressing props.
    /// </summary>
    public class FlagLock : Interactable
    {
        [Tooltip("Opens once this flag is set; empty: never")]
        public string flag = "";
        [Tooltip("The container it holds shut, enabled once the flag is set")]
        public Behaviour container;
        [TextArea(1, 3)] public string lockedText = "It's locked.";

        public override string Verb => "OPEN";
        public override bool CanFocus => base.CanFocus && !Open;

        bool Open => flag != "" && Run.HasFlag(flag);

        protected override void OnInteract(Interactor who, Collider part) => Say(lockedText);

        // Checked every frame rather than on the flag event, so a loaded save opens it too.
        void Update()
        {
            if (Open && container != null && !container.enabled)
                container.enabled = true;
        }
    }
}
