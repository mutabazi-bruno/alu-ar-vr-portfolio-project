using HeyHeyThere.EscapePuzzleKitFree;
using UnityEngine;
using UnityEngine.InputSystem;

namespace LastShift.XR
{
    /// <summary>
    /// The torch in VR: a light on the left controller run by the kit's own Flashlight, so it
    /// keeps the same rules as on desktop (works once the flashlight is picked up, drains the
    /// same battery, flickers when it's nearly flat). The button stands in for F.
    /// </summary>
    [RequireComponent(typeof(Flashlight))]
    public class XRTorch : MonoBehaviour
    {
        [Tooltip("Press to switch the torch on or off (left X button)")]
        public InputActionReference toggleAction;

        Flashlight torch;

        void Awake()
        {
            torch = GetComponent<Flashlight>();
            torch.readKeys = false;
        }

        void Update()
        {
            if (toggleAction == null || toggleAction.action == null || Interactor.Paused)
                return;
            if (toggleAction.action.WasPressedThisFrame())
            {
                if (torch.item != null && !Run.Has(torch.item.Id))
                    Run.Say("You don't have a torch yet.");
                else
                    torch.Toggle();
            }
        }
    }
}
