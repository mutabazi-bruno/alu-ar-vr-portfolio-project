using System.Collections.Generic;
using UnityEngine;

namespace LastShift.Narration
{
    /// <summary>
    /// A speaker in the wall. Every speaker in the scene plays the intercom's line at the same
    /// moment, as a 3D sound, so the voice comes from around the player (direction in VR, panning
    /// on desktop). The filters make a clean recording sound like an old building intercom.
    /// </summary>
    [RequireComponent(typeof(AudioSource))]
    public class IntercomSpeaker : MonoBehaviour
    {
        public static readonly List<IntercomSpeaker> All = new List<IntercomSpeaker>();

        [Tooltip("Old intercom sound: cuts the lows and highs and adds a little grit")]
        public bool radioFilter = true;
        [Range(200f, 1500f)] public float lowCut = 450f;
        [Range(2000f, 8000f)] public float highCut = 3400f;
        [Range(0f, 0.6f)] public float grit = 0.25f;
        [Tooltip("Within this distance it's at full volume")] public float nearDistance = 1.5f;
        [Tooltip("Beyond this distance it can't be heard")] public float farDistance = 14f;

        public AudioSource Source { get; private set; }

        void Awake()
        {
            Source = GetComponent<AudioSource>();
            Source.playOnAwake = false;
            Source.loop = false;
            Source.spatialBlend = 1f;
            Source.dopplerLevel = 0f;
            Source.rolloffMode = AudioRolloffMode.Linear;
            Source.minDistance = nearDistance;
            Source.maxDistance = farDistance;

            if (!radioFilter) return;
            Get<AudioHighPassFilter>().cutoffFrequency = lowCut;
            Get<AudioLowPassFilter>().cutoffFrequency = highCut;
            if (grit > 0f) Get<AudioDistortionFilter>().distortionLevel = grit;
        }

        T Get<T>() where T : Component
        {
            var c = GetComponent<T>();
            return c != null ? c : gameObject.AddComponent<T>();
        }

        void OnEnable() => All.Add(this);
        void OnDisable() => All.Remove(this);

        public void PlayAt(AudioClip clip, double dspTime, float volume)
        {
            if (clip == null || Source == null) return;
            Source.clip = clip;
            Source.volume = volume;
            Source.PlayScheduled(dspTime);
        }

        public void Stop()
        {
            if (Source != null) Source.Stop();
        }

        void OnDrawGizmos()
        {
            Gizmos.color = new Color(1f, 0.75f, 0.3f, 0.6f);
            Gizmos.DrawWireSphere(transform.position, nearDistance);
        }
    }
}
