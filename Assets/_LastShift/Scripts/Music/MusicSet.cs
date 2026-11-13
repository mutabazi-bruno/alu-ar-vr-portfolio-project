using UnityEngine;

namespace LastShift.Music
{
    // which tracks play where and how loud. it sits in a Resources folder so the music player
    // can load it without being placed in any scene
    [CreateAssetMenu(menuName = "Last Shift/Music Set")]
    public class MusicSet : ScriptableObject
    {
        public const string ResourceName = "MusicSet";

        [Header("Tracks")]
        public AudioClip menu;
        [Tooltip("Under both rooms. It has no tune, so it never fights Habimana's voice")]
        public AudioClip drone;
        [Tooltip("The menu tune made to sound like an old record. Plays from the record player in Room C")]
        public AudioClip record;
        [Tooltip("From the escape to the end card")]
        public AudioClip dawn;

        [Header("Levels")]
        [Range(0f, 1f)] public float menuVolume = 0.6f;
        [Range(0f, 1f)] public float droneVolume = 0.35f;
        [Range(0f, 1f)] public float recordVolume = 0.8f;
        [Tooltip("High, because the birds are quiet between calls")]
        [Range(0f, 1f)] public float dawnVolume = 0.9f;
        [Tooltip("How loud the music stays while Habimana is talking")]
        [Range(0f, 1f)] public float duckTo = 0.35f;

        [Header("Room C")]
        [Tooltip("The drone a little lower in Room C, so the room feels off")]
        [Range(0.5f, 1f)] public float roomCPitch = 0.9f;
        public string recordPlayerName = "Speaker_RecordPlayer";
        [Tooltip("Full volume within this distance of the record player")] public float recordNear = 1f;
        [Tooltip("Can't be heard past this distance")] public float recordFar = 8f;

        [Header("Loops")]
        [Tooltip("Where the drone starts and loops back to, past its fade-in")]
        public float droneLoopFrom = 3f;
        [Tooltip("Seconds the drone's end overlaps its start. Longer than its fade-out, so the loop doesn't dip")]
        public float droneCrossfade = 7f;
        [Tooltip("Where the birds start and loop back to: just before the first call, so the dawn isn't silent")]
        public float dawnLoopFrom = 16f;

        [Tooltip("Seconds to fade from one track to the next, and to loop the menu tune")]
        public float fade = 2.5f;
    }
}
