using System.Linq;
using HeyHeyThere.EscapePuzzleKitFree;
using UnityEngine;

namespace LastShift.Props
{
    /// <summary>
    /// Fires a story beat once every one of its flags is set, whatever order they come in:
    /// Room C's exit waits for the clothes and the cap. It can also show objects, such as the dawn
    /// light; those come back on after a load too, since the flags are saved.
    /// </summary>
    public class FlagGate : MonoBehaviour
    {
        public string[] flags = new string[0];
        [Tooltip("Empty: the StoryBeat on this object")]
        public StoryBeat beat;
        [Tooltip("Switched on once the flags are all set")]
        public GameObject[] show = new GameObject[0];

        void Awake()
        {
            if (beat == null)
                beat = GetComponent<StoryBeat>();
        }

        void OnEnable() => Run.FlagSet += OnFlagSet;

        void OnDisable() => Run.FlagSet -= OnFlagSet;

        void Start() => Check();  // flags already set by a loaded save

        void OnFlagSet(string flag) => Check();

        void Check()
        {
            if (flags.Length == 0 || !flags.All(Run.HasFlag))
                return;
            foreach (var go in show)
                if (go != null)
                    go.SetActive(true);
            if (beat != null)
                beat.Fire();  // fires once; a loaded save remembers it already did
        }
    }
}
