using System;
using System.Collections.Generic;
using HeyHeyThere.EscapePuzzleKitFree;
using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using UnityEngine.XR.Interaction.Toolkit.Interactors;

// A hook on the clothes rack. Works two ways that end up in the same place:
// - use it while holding a piece (E on desktop, trigger with the VR pointer) to hang it
// - in VR, drop a piece into the socket on the child object and it snaps on
// Using a hook that already has something on it swaps the two.
public class ClothesHook : Interactable
{
    [Tooltip("Where the piece hangs. Give the socket the same attach point so both line up")]
    public Transform hangPoint;

    [Tooltip("The piece that belongs here. Leave empty if this hook should stay empty")]
    public ClothesPiece answer;

    public ClothesPiece Piece { get; private set; }

    public static event Action Changed;

    XRSocketInteractor socket;
    bool locked;

    public override bool CanFocus => base.CanFocus && (ClothesPiece.Held != null || Piece != null);

    public override string Verb =>
        ClothesPiece.Held != null ? "HANG " + ClothesPiece.Held.Name + " HERE"
        : Piece != null ? "TAKE " + Piece.Name
        : "HOOK";

    void Awake()
    {
        if (hangPoint == null)
            hangPoint = transform;

        socket = GetComponentInChildren<XRSocketInteractor>();
        if (socket != null)
        {
            socket.selectEntered.AddListener(OnSocketGrabbed);
            socket.selectExited.AddListener(OnSocketLetGo);
        }
    }

    protected override void OnInteract(Interactor who, Collider part) => Swap();

    // what's on the hook goes in your hand, what was in your hand goes on the hook
    public void Swap()
    {
        var held = ClothesPiece.Held;
        var old = Piece;

        if (old != null)
            old.TakeInHand();
        if (held != null)
            Hang(held);
    }

    public void Hang(ClothesPiece piece)
    {
        if (Piece == piece)
            return;
        if (Piece != null)
            Piece.PutBack();

        if (piece.Hook != null)
            piece.Hook.Clear(piece);

        Piece = piece;
        piece.Hook = this;
        piece.PlaceAt(hangPoint);
        Changed?.Invoke();
    }

    public void Clear(ClothesPiece piece)
    {
        if (Piece != piece)
            return;
        Piece = null;
        piece.Hook = null;
        Changed?.Invoke();
    }

    public void Lock()
    {
        locked = true;
        enabled = false;
        if (socket != null)
            socket.socketActive = false;
        if (Piece != null)
            Piece.Lock();
    }

    void OnSocketGrabbed(SelectEnterEventArgs args)
    {
        var piece = args.interactableObject.transform.GetComponent<ClothesPiece>();
        if (piece != null && !locked)
            Hang(piece);
    }

    void OnSocketLetGo(SelectExitEventArgs args)
    {
        // when the puzzle locks, the socket lets go but the piece should stay counted
        if (locked)
            return;
        var piece = args.interactableObject.transform.GetComponent<ClothesPiece>();
        if (piece != null)
            Clear(piece);
    }

    public override void Save(Dictionary<string, object> state)
    {
        base.Save(state);
        state["piece"] = Piece != null ? Piece.pieceId : "";
    }

    public override void Load(IDictionary<string, object> state)
    {
        base.Load(state);
        var id = Json.Get(state, "piece", "");
        if (id == "")
            return;

        foreach (var piece in FindObjectsByType<ClothesPiece>(FindObjectsInactive.Exclude))
        {
            if (piece.pieceId == id && piece.gameObject.scene == gameObject.scene)
            {
                Hang(piece);
                break;
            }
        }
    }
}
