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

        // A Quest controller's grip pose is tilted up from where the player points, so a torch on
        // the grip shone above what the ray was on. It rides on its hand's ray instead (the object
        // that follows the aim pose), a few centimetres forward, so the beam goes where the ray goes.
        void Start()
        {
            var aim = transform.parent != null ? transform.parent.GetComponentInChildren<XRAimPose>(true) : null;
            if (aim == null) return;
            transform.SetParent(aim.transform, false);
            transform.localPosition = new Vector3(0f, 0f, 0.06f);
            transform.localRotation = Quaternion.identity;
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
