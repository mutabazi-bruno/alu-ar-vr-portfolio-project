using System.Collections;
using System.Collections.Generic;
using System.Linq;
using HeyHeyThere.EscapePuzzleKitFree;
using UnityEngine;

namespace LastShift.Props
{
    /// <summary>
    /// What a drawer, locker, cabinet, chest or vent holds, given the first time it opens.
    /// The kit's Stash holds a single item; our containers hold up to three (a note and a key,
    /// three keys on hooks), so this gives them all at once and fires the container's story beat.
    /// Each item is then held up in the examine view in turn, the way a kit pickup shows what you take.
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
        [Tooltip("Said once the items have been looked at; empty for nothing more")]
        [TextArea(1, 3)] public string message = "";

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

            // Carried at once, so a save made while they're being shown already has them.
            var found = items.Where(i => i != null).ToList();
            foreach (var item in found)
                Run.Add(item.Id);

            if (beat != null)
                beat.Fire();
            StartCoroutine(Show(found));
        }

        IEnumerator Show(List<Item> found)
        {
            // Let the door or drawer swing open first, so the player sees where they came from.
            yield return new WaitForSecondsRealtime(Mathf.Max(0.3f, container.motion.Length));

            var shownAny = false;
            foreach (var item in found)
            {
                yield return WaitForPanelsClosed();
                var text = item.kind == Item.Kind.Document ? item.text : item.description;
                if (item.model == null || !Panels.OpenExamine(item, item.model, text))
                    continue;
                // In VR the world HUD closes flat panels straight away; only count it if it stayed open.
                yield return null;
                yield return null;
                shownAny |= Hud.Current != null && Hud.Current.IsOpen;
            }

            // No examine view (in VR the flat panels stay closed): name what was found instead.
            if (!shownAny && found.Count > 0)
                Run.Say("Found: " + string.Join(", ", found.Select(i => Item.Title(i.Id))) + ".");

            if (message != "")
            {
                yield return WaitForPanelsClosed();
                Run.Say(message);
            }
        }

        static IEnumerator WaitForPanelsClosed()
        {
            while (Hud.Current != null && Hud.Current.IsOpen)
                yield return null;
            yield return new WaitForSecondsRealtime(0.15f);
        }
    }
}
