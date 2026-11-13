using HeyHeyThere.EscapePuzzleKitFree;
using UnityEngine;

namespace LastShift.Props
{
    /// <summary>
    /// Once every hook on Habimana's rack has something on it but the rack still isn't solved, the
    /// player is told the order is wrong; otherwise a full rack in the wrong order looks like the
    /// puzzle isn't working. It creates itself, so no scene needs it placed.
    /// </summary>
    public class ClothesRackHint : MonoBehaviour
    {
        const string SolvedFlag = "clothes_solved";
        const string WrongText = "That's not how he hangs them. Read his note again: hook one is on the left.";

        bool changed;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Create()
        {
            var go = new GameObject("ClothesRackHint");
            DontDestroyOnLoad(go);
            go.AddComponent<ClothesRackHint>();
        }

        void OnEnable() => ClothesHook.Changed += OnChanged;

        void OnDisable() => ClothesHook.Changed -= OnChanged;

        // checked a frame later, so the rack has had its turn to mark itself solved
        void OnChanged() => changed = true;

        void Update()
        {
            if (!changed)
                return;
            changed = false;
            if (Run.HasFlag(SolvedFlag))
                return;
            var puzzle = FindAnyObjectByType<ClothesPuzzle>();
            if (puzzle == null || puzzle.hooks == null || puzzle.hooks.Length == 0)
                return;
            foreach (var hook in puzzle.hooks)
                if (hook == null || hook.Piece == null)
                    return;
            Run.Say(WrongText);
        }
    }
}
