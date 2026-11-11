using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

// Socket for a clothes hook. The hooks are close together and a coat is wider than the gap,
// so a plain socket would pull a coat off the next hook, or grab one dropped on its neighbour.
// Here a hook only takes a free piece whose hanger is right at it. It also keeps the cap off the hooks.
public class ClothesSocket : XRSocketInteractor
{
    [Tooltip("How close the top of the hanger has to be to the hook, in metres. Keep it under half the gap between hooks")]
    public float snapDistance = 0.12f;

    ClothesHook hook;

    protected override void Awake()
    {
        base.Awake();
        hook = GetComponentInParent<ClothesHook>();
    }

    public override bool CanHover(IXRHoverInteractable interactable) => base.CanHover(interactable) && Fits(interactable);

    public override bool CanSelect(IXRSelectInteractable interactable) => base.CanSelect(interactable) && Fits(interactable);

    bool Fits(IXRInteractable interactable)
    {
        var piece = interactable.transform.GetComponent<ClothesPiece>();
        if (piece == null)
            return false;
        if (piece.Hook == hook)
            return true;

        // a piece's root is the top of its hanger
        var point = attachTransform != null ? attachTransform.position : transform.position;
        return piece.Hook == null && Vector3.Distance(piece.transform.position, point) < snapDistance;
    }
}
