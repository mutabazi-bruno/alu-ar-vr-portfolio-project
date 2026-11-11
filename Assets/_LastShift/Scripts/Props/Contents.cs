using System.Linq;
using HeyHeyThere.EscapePuzzleKitFree;
using UnityEngine;

namespace LastShift.Props
{
    /// <summary>
    /// What a drawer, locker, cabinet, chest or vent holds, given the first time it opens.
    /// The kit's Stash holds a single item; our containers hold up to three (a note and a key,
    /// three keys on hooks), so this gives them all at once and fires the container's story beat.
    /// Whether it was searched is kept as a story flag, so saves remember it without extra code.
    /// </summary>
    public class Contents : MonoBehaviour
    {
        [Tooltip("Empty: the Openable (Stash, Door) on this object")]
        public Openable container;
        public Item[] items = new Item[0];
        [Tooltip("Set once searched; snake_case, never renamed once used")]
        public string searchedFlag = "";
        [Tooltip("Fired once searched: an objective, a line of Habimana's")]
        public StoryBeat beat;

        void Awake()
        {
            if (container == null)
                container = GetComponent<Openable>();
        }

        void OnEnable()
        {
            if (container != null)
                container.onOpened.AddListener(Search);
        }

        void OnDisable()
        {
            if (container != null)
                container.onOpened.RemoveListener(Search);
        }

        void Search()
        {
            if (searchedFlag == "" || Run.HasFlag(searchedFlag))
                return;
            Run.SetFlag(searchedFlag);

            var found = items.Where(i => i != null).ToList();
            foreach (var item in found)
                Run.Add(item.Id);
            if (found.Count > 0)
                Run.Say("Found: " + string.Join(", ", found.Select(i => Item.Title(i.Id))) + ".");

            if (beat != null)
                beat.Fire();
        }
    }
}
