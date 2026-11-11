using HeyHeyThere.EscapePuzzleKitFree;
using UnityEngine;
using UnityEngine.InputSystem;

namespace LastShift.XR
{
    /// <summary>
    /// Uses kit props (doors, drawers, keypad keys) from a VR hand. Sits next to the kit's
    /// Interactor on an object without a Camera, so the Interactor aims along this object's
    /// forward (the hand's ray) instead of the screen centre. The use action stands in for E.
    /// </summary>
    [RequireComponent(typeof(Interactor))]
    public class XRKitPointer : MonoBehaviour
    {
        [Tooltip("Press to use what the ray rests on (the trigger)")]
        public InputActionReference useAction;

        Interactor interactor;

        /// <summary>What the ray rests on now, or null.</summary>
        public Interactable Focus => interactor != null ? interactor.Focus : null;

        /// <summary>The collider the ray rests on (a keypad's key, a door), or null.</summary>
        public Collider FocusPart => interactor != null ? interactor.FocusPart : null;

        /// <summary>The ray the kit looks along, and how far it reaches.</summary>
        public Ray Ray => new Ray(transform.position, transform.forward);
        public float Reach => interactor != null ? interactor.reach : 0f;

        void Awake()
        {
            interactor = GetComponent<Interactor>();
            interactor.readKeys = false;
        }

        void Update()
        {
            if (useAction != null && useAction.action != null && useAction.action.WasPressedThisFrame())
                interactor.Interact();
        }
    }
}
