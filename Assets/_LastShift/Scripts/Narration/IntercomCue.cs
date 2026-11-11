using System.Collections;
using HeyHeyThere.EscapePuzzleKitFree;
using UnityEngine;

namespace LastShift.Narration
{
    /// <summary>
    /// Plays a voice line through the intercom: as the level starts, when a story flag is set,
    /// when the player gets an item (the torch, say), or when called (wire a StoryBeat's On Fired,
    /// a pickup's On Taken, a trigger or any UnityEvent to Play).
    /// Remembers it has played with a flag of its own, so a loaded save doesn't repeat it.
    /// Leave the StoryBeat's own subtitle empty: the voice line carries the subtitle.
    /// </summary>
    public class IntercomCue : MonoBehaviour
    {
        public enum When { OnStart, OnFlag, OnItem, WhenCalled }

        public VoiceLine line;
        public When when = When.OnFlag;
        [Tooltip("For On Flag: plays once this story flag is set")] public string flag = "";
        [Tooltip("For On Item: plays once the player has this item")] public Item item;
        [Tooltip("Seconds to wait before playing")] public float delay = 0f;
        public bool once = true;
        [Tooltip("A control hint shown once the line has been said: the desktop key (F) and what it does " +
                 "(TORCH). The VR HUD shows the controller button instead of the key")]
        public string hintKey = "", hint = "";

        bool hintPending;

        string PlayedFlag => "vo_" + (line != null ? line.name : name);

        bool Ready => (when == When.OnFlag && Run.HasFlag(flag)) ||
                      (when == When.OnItem && item != null && Run.Has(item.Id));

        void OnEnable()
        {
            Run.FlagSet += OnFlagSet;
            Run.ItemsChanged += OnItemsChanged;
            Intercom.Finished += OnLineFinished;
        }

        void OnDisable()
        {
            Run.FlagSet -= OnFlagSet;
            Run.ItemsChanged -= OnItemsChanged;
            Intercom.Finished -= OnLineFinished;
        }

        void OnLineFinished(VoiceLine finished)
        {
            if (hintPending && finished == line) ShowHint();
        }

        void ShowHint()
        {
            hintPending = false;
            if (hint != "") Run.Hint(hintKey, hint);
        }

        IEnumerator Start()
        {
            // one frame so saves and the intercom are in place first
            yield return null;
            if (when == When.OnStart || Ready)
                Play();
        }

        void OnFlagSet(string set)
        {
            if (when == When.OnFlag && set == flag)
                Play();
        }

        void OnItemsChanged()
        {
            if (when == When.OnItem && Ready)
                Play();
        }

        public void Play()
        {
            if (line == null || (once && Run.HasFlag(PlayedFlag)) || !isActiveAndEnabled) return;
            if (once) Run.SetFlag(PlayedFlag);
            if (delay > 0f) StartCoroutine(Later());
            else Say();
        }

        IEnumerator Later()
        {
            yield return new WaitForSecondsRealtime(delay);
            Say();
        }

        void Say()
        {
            hintPending = hint != "";
            Intercom.Play(line);
            // with no intercom in the scene the line is only subtitles, said at once
            if (hintPending && Intercom.Current == null) ShowHint();
        }
    }
}
