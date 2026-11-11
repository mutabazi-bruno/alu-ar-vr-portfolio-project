using System;
using System.Collections;
using System.Collections.Generic;
using HeyHeyThere.EscapePuzzleKitFree;
using UnityEngine;

namespace LastShift.Narration
{
    /// <summary>
    /// Plays voice lines through every intercom speaker in the scene, one line at a time:
    /// a short crackle, then the line, with its subtitle shown in pieces through the kit's
    /// own subtitle line (so it appears on the desktop HUD and the VR HUD alike).
    /// With no speakers in the scene the line plays flat from here, so nothing is ever lost.
    /// </summary>
    [RequireComponent(typeof(AudioSource))]
    public class Intercom : MonoBehaviour
    {
        public static Intercom Current { get; private set; }

        /// <summary>A line starts or ends. The keypad lockout and anything else can listen.</summary>
        public static event Action<VoiceLine> Started, Finished;

        [Tooltip("Played before each line. Left empty, a crackle is made in code")]
        public AudioClip crackle;
        [Range(0f, 1f)] public float crackleVolume = 0.35f;
        [Tooltip("Pause between two queued lines")] public float gap = 0.6f;
        [Tooltip("Show the crackle as a sound caption")] public bool captionCrackle = true;

        readonly Queue<VoiceLine> queue = new Queue<VoiceLine>();
        AudioSource flat;
        bool busy;

        public bool IsPlaying => busy;

        /// <summary>Queues a line. Safe to call with no intercom in the scene: the subtitle still shows.</summary>
        public static void Play(VoiceLine line)
        {
            if (line == null) return;
            if (Current != null) Current.Enqueue(line);
            else foreach (var (_, text) in line.Timed()) Run.Say(text, line.speaker);
        }

        void Awake()
        {
            Current = this;
            flat = GetComponent<AudioSource>();
            flat.playOnAwake = false;
            flat.spatialBlend = 0f;
            if (crackle == null) crackle = MakeCrackle();
        }

        void OnDestroy()
        {
            if (Current == this) Current = null;
        }

        public void Enqueue(VoiceLine line)
        {
            queue.Enqueue(line);
            if (!busy) StartCoroutine(Drain());
        }

        IEnumerator Drain()
        {
            busy = true;
            while (queue.Count > 0)
            {
                var line = queue.Dequeue();
                Started?.Invoke(line);

                if (captionCrackle) Run.Say("[intercom crackles]");
                var start = AudioSettings.dspTime + 0.05;
                PlayEverywhere(crackle, start, crackleVolume);
                yield return WaitRealtime(crackle != null ? crackle.length : 0.3f);

                start = AudioSettings.dspTime + 0.05;
                PlayEverywhere(line.clip, start, line.volume);
                float t = 0f;
                foreach (var (at, text) in line.Timed())
                {
                    if (at > t) yield return WaitRealtime(at - t);
                    t = at;
                    Run.Say(text, line.speaker);
                }
                var end = line.Duration;
                if (end > t) yield return WaitRealtime(end - t);

                Finished?.Invoke(line);
                if (queue.Count > 0) yield return WaitRealtime(gap);
            }
            busy = false;
        }

        // the kit pauses with timeScale, but a voice already playing should keep its subtitles in step
        static IEnumerator WaitRealtime(float seconds)
        {
            var until = Time.realtimeSinceStartup + seconds;
            while (Time.realtimeSinceStartup < until) yield return null;
        }

        void PlayEverywhere(AudioClip clip, double dspTime, float volume)
        {
            if (clip == null) return;
            if (IntercomSpeaker.All.Count == 0)
            {
                flat.clip = clip;
                flat.volume = volume;
                flat.PlayScheduled(dspTime);
                return;
            }
            foreach (var s in IntercomSpeaker.All) s.PlayAt(clip, dspTime, volume);
        }

        /// <summary>Stops the current line and clears the queue (a new game, say).</summary>
        public void StopAll()
        {
            StopAllCoroutines();
            queue.Clear();
            busy = false;
            flat.Stop();
            foreach (var s in IntercomSpeaker.All) s.Stop();
        }

        // a short burst of filtered noise with a click at each end: no sound file or licence needed
        static AudioClip MakeCrackle()
        {
            const int rate = 22050;
            int n = (int)(rate * 0.35f);
            var data = new float[n];
            var rng = new System.Random(2207);
            float smooth = 0f;
            for (int i = 0; i < n; i++)
            {
                float env = Mathf.Sin(Mathf.PI * i / n);
                float noise = (float)(rng.NextDouble() * 2 - 1);
                smooth = Mathf.Lerp(smooth, noise, 0.35f);
                float pop = rng.NextDouble() < 0.004 ? (float)(rng.NextDouble() * 2 - 1) : 0f;
                data[i] = (smooth * 0.5f + pop) * env * 0.6f;
            }
            data[0] = 0.8f; data[n - 1] = -0.6f;
            var clip = AudioClip.Create("Intercom Crackle", n, 1, rate, false);
            clip.SetData(data, 0);
            return clip;
        }
    }
}
