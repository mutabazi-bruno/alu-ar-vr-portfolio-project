using UnityEngine;

namespace LastShift.Props
{
    /// <summary>
    /// A clothes piece wears a garment from an Asset Store pack that each of us imports ourselves.
    /// With the garment there the placeholder box stays hidden, even after the clothes puzzle shows
    /// every renderer again (hanging a piece back up). Where the pack is missing the garment has
    /// nothing to draw, so the placeholder shows instead of the piece being invisible.
    /// </summary>
    public class LookFallback : MonoBehaviour
    {
        public Transform look;
        public Renderer placeholder;

        public bool HasLook { get; private set; }

        void Awake()
        {
            HasLook = look != null && look.GetComponentInChildren<Renderer>(true) != null;
            if (placeholder != null)
                placeholder.enabled = !HasLook;
        }

        void LateUpdate()
        {
            if (HasLook && placeholder != null && placeholder.enabled)
                placeholder.enabled = false;
        }
    }
}
