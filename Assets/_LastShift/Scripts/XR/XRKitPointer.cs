using System.Linq;
using HeyHeyThere.EscapePuzzleKitFree;
using UnityEngine;
using UnityEngine.InputSystem;

namespace LastShift.XR
{
    /// <summary>
    /// Uses kit props (doors, drawers, keypad keys) from a VR hand. Sits next to the kit's
    /// Interactor on an object without a Camera, so the Interactor aims along this object's
    /// forward (the hand's ray) instead of the screen centre. The trigger stands in for E.
    /// Either hand works: the pointer rides on the ray of the hand whose trigger was pulled last,
    /// so the prompt and the use always belong to the hand the player is using.
    /// </summary>
    [RequireComponent(typeof(Interactor))]
    [DefaultExecutionOrder(-100)]   // placed on the hand before the Interactor looks this frame
    public class XRKitPointer : MonoBehaviour
    {
        [Tooltip("Press to use what the ray rests on (the trigger of this pointer's own hand)")]
        public InputActionReference useAction;

        Interactor interactor;

        /// <summary>What the ray rests on now, or null.</summary>
        public Interactable Focus => interactor != null ? interactor.Focus : null;

        /// <summary>The collider the ray rests on (a keypad's key, a door), or null.</summary>
        public Collider FocusPart => interactor != null ? interactor.FocusPart : null;

        /// <summary>The ray the kit looks along, and how far it reaches.</summary>
        public Ray Ray => new Ray(transform.position, transform.forward);
        public float Reach => interactor != null ? interactor.reach : 0f;

        class Hand { public Transform ray; public InputAction use; public bool released; }
        Hand[] hands = new Hand[0];
        Hand active;
        float wokeAt;

        void Awake()
        {
            interactor = GetComponent<Interactor>();
            interactor.readKeys = false;
            FindHands();
        }

        // Each hand's ray (the object that follows its aim) and its own trigger action, found by
        // name in the rig: "Left Hand/Activate" goes with the left controller's ray.
        void FindHands()
        {
            var asset = useAction != null && useAction.action != null ? useAction.action.actionMap?.asset : null;
            var list = new System.Collections.Generic.List<Hand>();
            foreach (var aim in transform.root.GetComponentsInChildren<XRAimPose>(true))
            {
                var side = aim.controller != null && aim.controller.name.ToLowerInvariant().Contains("left") ? "Left Hand" : "Right Hand";
                var use = asset != null ? asset.FindAction(side + "/Activate") : null;
                if (use != null) list.Add(new Hand { ray = aim.transform, use = use });
            }
            hands = list.ToArray();
            // start on the hand this pointer was built on
            active = hands.FirstOrDefault(h => transform.IsChildOf(h.ray)) ?? hands.FirstOrDefault();
        }

        // The trigger that pressed PLAY (or Continue) is often still held as the room loads, and
        // counted as a fresh press it would open whatever the ray happens to rest on, like the top
        // drawer. So a hand's press only counts once its trigger has been let go since the pointer woke.
        void OnEnable()
        {
            wokeAt = Time.unscaledTime;
            foreach (var h in hands) h.released = false;
        }

        // padlocks are small targets for a ray: once per room, the whole padlocked prop counts
        void Start() => PadlockReach.SetUp();

        void Update()
        {
            if (hands.Length == 0)
            {
                // no hands found (another rig): this pointer's own action, as before
                if (useAction != null && useAction.action != null && useAction.action.WasPressedThisFrame() && Time.unscaledTime - wokeAt > 0.3f)
                    interactor.Interact();
                return;
            }

            Hand pressed = null;
            foreach (var h in hands)
            {
                if (!h.released)
                {
                    if (!h.use.IsPressed() && Time.unscaledTime - wokeAt > 0.3f) h.released = true;
                    continue;
                }
                if (h.use.WasPressedThisFrame()) pressed = h;
            }
            if (pressed != null) active = pressed;

            // ride on the active hand's ray, then look and use from there
            if (active != null && active.ray != null)
                transform.SetPositionAndRotation(active.ray.position, active.ray.rotation);
            if (pressed != null)
            {
                interactor.Look();
                interactor.Interact();
            }
        }
    }
}
