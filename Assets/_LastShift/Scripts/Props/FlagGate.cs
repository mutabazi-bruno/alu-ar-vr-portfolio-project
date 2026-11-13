using System.Linq;
using HeyHeyThere.EscapePuzzleKitFree;
using UnityEngine;

namespace LastShift.Props
{
    /// <summary>
    /// Fires a story beat once every one of its flags is set and every one of its items is carried,
    /// whatever order they come in: Room C's exit waits for the clothes and the cap, and the torch's
    /// beat waits for the torch. It can also show objects, such as the dawn light; those come back on
    /// after a load too, since flags and items are saved.
    /// </summary>
    public class FlagGate : MonoBehaviour
    {
        public string[] flags = new string[0];
        [Tooltip("Item ids that must be carried")]
        public string[] items = new string[0];
        [Tooltip("Empty: the StoryBeat on this object")]
        public StoryBeat beat;
        [Tooltip("Switched on once the flags are all set")]
        public GameObject[] show = new GameObject[0];

        void Awake()
        {
            if (beat == null)
                beat = GetComponent<StoryBeat>();
        }

        void OnEnable()
        {
            Run.FlagSet += OnFlagSet;
            Run.ItemsChanged += Check;
        }

        void OnDisable()
        {
            Run.FlagSet -= OnFlagSet;
            Run.ItemsChanged -= Check;
        }

        void Start() => Check();  // flags or items already there from a loaded save

        void OnFlagSet(string flag) => Check();

        void Check()
        {
            if (flags.Length + items.Length == 0 || !flags.All(Run.HasFlag) || !items.All(id => Run.Has(id)))
                return;
            foreach (var go in show)
                if (go != null)
                    go.SetActive(true);
            if (beat != null)
                beat.Fire();  // fires once; a loaded save remembers it already did
        }
    }
}
