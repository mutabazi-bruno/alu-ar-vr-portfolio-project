using HeyHeyThere.EscapePuzzleKitFree;
using UnityEngine;
using UnityEngine.Events;

// Watches the rack. When every hook has its answer on it (and the cap is on, if needed)
// it sets the solved flag once. Gama's story beat waits on that flag to open the real exit.
public class ClothesPuzzle : MonoBehaviour
{
    public ClothesHook[] hooks;

    public bool needsCap = true;
    public string capFlag = "cap_worn";
    public string solvedFlag = "clothes_solved";

    public UnityEvent onSolved = new UnityEvent();

    void OnEnable()
    {
        ClothesHook.Changed += Check;
        Run.FlagSet += OnFlagSet;
    }

    void OnDisable()
    {
        ClothesHook.Changed -= Check;
        Run.FlagSet -= OnFlagSet;
    }

    void Start()
    {
        // already solved in a save: the hooks were put back by the save, just lock them
        if (Run.HasFlag(solvedFlag))
            LockHooks();
    }

    void OnFlagSet(string flag)
    {
        if (flag == capFlag)
            Check();
    }

    void Check()
    {
        if (Run.HasFlag(solvedFlag))
            return;

        foreach (var hook in hooks)
            if (hook.Piece != hook.answer)
                return;

        if (needsCap && !Run.HasFlag(capFlag))
            return;

        Run.SetFlag(solvedFlag);
        LockHooks();
        onSolved.Invoke();
    }

    void LockHooks()
    {
        foreach (var hook in hooks)
            hook.Lock();
    }
}
