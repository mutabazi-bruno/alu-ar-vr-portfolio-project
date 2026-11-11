using System.Collections;
using HeyHeyThere.EscapePuzzleKitFree;
using UnityEngine;

namespace LastShift.Narration
{
    /// <summary>
    /// Plays a voice line through the intercom: as the level starts, when a story flag is set,
    /// or when called (wire a StoryBeat's On Fired, a trigger or any UnityEvent to Play).
    /// Remembers it has played with a flag of its own, so a loaded save doesn't repeat it.
    /// Leave the StoryBeat's own subtitle empty: the voice line carries the subtitle.
    /// </summary>
    public class IntercomCue : MonoBehaviour
    {
        public enum When { OnStart, OnFlag, WhenCalled }

        public VoiceLine line;
        public When when = When.OnFlag;
        [Tooltip("For On Flag: plays once this story flag is set")] public string flag = "";
        [Tooltip("Seconds to wait before playing")] public float delay = 0f;
        public bool once = true;

        string PlayedFlag => "vo_" + (line != null ? line.name : name);

        void OnEnable() => Run.FlagSet += OnFlagSet;
        void OnDisable() => Run.FlagSet -= OnFlagSet;

        IEnumerator Start()
        {
            // one frame so saves and the intercom are in place first
            yield return null;
            if (when == When.OnStart || (when == When.OnFlag && Run.HasFlag(flag)))
                Play();
        }

        void OnFlagSet(string set)
        {
            if (when == When.OnFlag && set == flag)
                Play();
        }

        public void Play()
        {
            if (line == null || (once && Run.HasFlag(PlayedFlag)) || !isActiveAndEnabled) return;
            if (once) Run.SetFlag(PlayedFlag);
            if (delay > 0f) StartCoroutine(Later());
            else Intercom.Play(line);
        }

        IEnumerator Later()
        {
            yield return new WaitForSecondsRealtime(delay);
            Intercom.Play(line);
        }
    }
}
