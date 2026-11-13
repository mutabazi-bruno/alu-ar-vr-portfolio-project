using HeyHeyThere.EscapePuzzleKitFree;
using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

// The caretaker's cap. Desktop or VR pointer: "PUT ON". VR hands: grab it and lift it to your head.
// Wearing it only sets a flag, the cap itself just disappears.
public class ClothesCap : Interactable
{
    public string wornFlag = "cap_worn";
    [TextArea(1, 3)] public string wornText = "You put on the caretaker's cap. It fits.";

    [Tooltip("How close to the headset the cap has to come in VR, in metres")]
    public float headReach = 0.3f;

    [Tooltip("Can't be taken until this flag is set, so it can't be reached through the side of the locked wardrobe. Empty: any time. Leave it empty if the rack has needsCap ticked")]
    public string lockedUntil = "clothes_solved";

    bool Free => lockedUntil == "" || Run.HasFlag(lockedUntil);

    public override bool CanFocus => base.CanFocus && Free;

    public override string Verb => "PUT ON";

    XRGrabInteractable grab;
    XROrigin rig;

    void Awake()
    {
        grab = GetComponent<XRGrabInteractable>();

        // loading a save where it's already on. Has to be Awake: the save turns this
        // component off, and Start never runs on a disabled component
        if (Run.HasFlag(wornFlag))
            Hide();
    }

    protected override void OnInteract(Interactor who, Collider part) => PutOn();

    void Update()
    {
        // vr hands too, not just the look
        if (grab != null)
            grab.enabled = Free;

        if (grab == null || !grab.isSelected || grab.firstInteractorSelecting is XRSocketInteractor)
            return;

        if (rig == null)
            rig = FindAnyObjectByType<XROrigin>();
        if (rig == null || rig.Camera == null)
            return;

        if (Vector3.Distance(transform.position, rig.Camera.transform.position) < headReach)
            PutOn();
    }

    void PutOn()
    {
        if (Run.HasFlag(wornFlag))
            return;
        Run.SetFlag(wornFlag);
        Say(wornText);
        Hide();
    }

    void Hide()
    {
        enabled = false;
        if (grab != null)
            grab.enabled = false;
        foreach (var r in GetComponentsInChildren<Renderer>())
            r.enabled = false;
        foreach (var c in GetComponentsInChildren<Collider>())
            c.enabled = false;
    }
}
