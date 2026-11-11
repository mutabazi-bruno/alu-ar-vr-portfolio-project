using HeyHeyThere.EscapePuzzleKitFree;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

// One of the caretaker's clothes for the rack in Room C.
// Desktop (and the VR pointer): use it to pick it up, then use a hook to hang it.
// VR hands: grab it with grip and let go near a hook, the hook's socket snaps it on.
// On desktop you only ever hold one piece, so this doesn't go through the kit inventory.
public class ClothesPiece : Interactable
{
    [Tooltip("Unique in the scene. Saves use it to put the piece back on the right hook")]
    public string pieceId = "coat";

    public static ClothesPiece Held { get; private set; }

    // the hook it's hanging on, set by the hook
    public ClothesHook Hook { get; set; }

    public string Name => label != "" ? label.ToUpperInvariant() : pieceId.ToUpperInvariant();

    // looking at a hanging piece while holding another works like using its hook
    bool SwapsOnHook => Hook != null && Held != null && Held != this;

    public override string Verb => SwapsOnHook ? Hook.Verb : "TAKE";

    Vector3 homePosition;
    Quaternion homeRotation;
    XRGrabInteractable grab;
    Renderer[] renderers;
    Collider[] colliders;

    void Awake()
    {
        homePosition = transform.position;
        homeRotation = transform.rotation;
        grab = GetComponent<XRGrabInteractable>();
        renderers = GetComponentsInChildren<Renderer>(true);
        colliders = GetComponentsInChildren<Collider>(true);
    }

    void OnDestroy()
    {
        if (Held == this)
            Held = null;
    }

    protected override void OnInteract(Interactor who, Collider part)
    {
        if (SwapsOnHook)
            Hook.Swap();
        else
            TakeInHand();
    }

    public void TakeInHand()
    {
        // swapping: whatever we had goes back where it came from
        if (Held != null && Held != this)
            Held.PutBack();

        if (Hook != null)
            Hook.Clear(this);

        SetVisible(false);
        Held = this;
    }

    public void PutBack()
    {
        if (Held == this)
            Held = null;
        transform.SetPositionAndRotation(homePosition, homeRotation);
        SetVisible(true);
    }

    public void PlaceAt(Transform point)
    {
        if (Held == this)
            Held = null;
        transform.SetPositionAndRotation(point.position, point.rotation);
        SetVisible(true);
    }

    // once the puzzle is solved the clothes stay where they are
    public void Lock()
    {
        enabled = false;
        if (grab != null)
            grab.enabled = false;
    }

    void SetVisible(bool on)
    {
        foreach (var r in renderers)
            r.enabled = on;
        foreach (var c in colliders)
            c.enabled = on;
        // turning the grab off also lets go of any socket holding it
        if (grab != null)
            grab.enabled = on && enabled;
    }
}
