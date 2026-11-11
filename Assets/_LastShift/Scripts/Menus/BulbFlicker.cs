using UnityEngine;

namespace LastShift.Menus
{
    /// <summary>
    /// A tired bulb: a slow wobble in brightness and, now and then, a short dip with a crackle.
    /// It hums while it's on. The hum and crackle are made in code, so no sound files are needed.
    /// </summary>
    [RequireComponent(typeof(Light))]
    public class BulbFlicker : MonoBehaviour
    {
        [Range(0f, 0.5f)] public float wobble = 0.12f;
        [Tooltip("Average seconds between dips")] public float dipEvery = 6f;
        [Range(0f, 1f)] public float humVolume = 0.12f;
        [Range(0f, 1f)] public float crackleVolume = 0.35f;
        [Tooltip("Within this distance the hum is at full volume")] public float nearDistance = 1.5f;
        public float farDistance = 9f;

        Light bulb;
        AudioSource hum, crackle;
        AudioClip[] crackles;
        float baseIntensity, dipUntil, nextDip;

        void Awake()
        {
            bulb = GetComponent<Light>();
            baseIntensity = bulb.intensity;
            nextDip = Time.time + Random.Range(2f, dipEvery);

            hum = Source(true);
            hum.clip = MakeHum();
            hum.volume = humVolume;
            hum.Play();
            crackle = Source(false);
            crackles = new[] { MakeCrackle(11), MakeCrackle(29), MakeCrackle(53) };
        }

        AudioSource Source(bool loop)
        {
            var s = gameObject.AddComponent<AudioSource>();
            s.loop = loop;
            s.playOnAwake = false;
            s.spatialBlend = 1f;
            s.rolloffMode = AudioRolloffMode.Linear;
            s.minDistance = nearDistance;
            s.maxDistance = farDistance;
            s.dopplerLevel = 0f;
            return s;
        }

        void Update()
        {
            var t = Time.time;
            if (t > nextDip)
            {
                dipUntil = t + Random.Range(0.06f, 0.22f);
                nextDip = t + Random.Range(dipEvery * 0.5f, dipEvery * 1.5f);
                crackle.pitch = Random.Range(0.85f, 1.15f);
                crackle.PlayOneShot(crackles[Random.Range(0, crackles.Length)], crackleVolume);
            }
            var dipping = t < dipUntil;
            var noise = (Mathf.PerlinNoise(t * 1.7f, 0.3f) - 0.5f) * 2f * wobble;
            bulb.intensity = baseIntensity * (dipping ? 0.25f : 1f + noise);
            // the hum drops out with the light and swells a little with it
            hum.volume = dipping ? humVolume * 0.15f : humVolume * (1f + noise);
        }

        // mains buzz: 100 Hz with a few harmonics and a little grit; one second loops seamlessly
        static AudioClip MakeHum()
        {
            const int rate = 22050;
            var data = new float[rate];
            var rng = new System.Random(7);
            for (int i = 0; i < rate; i++)
            {
                float t = (float)i / rate;
                float s = Mathf.Sin(2f * Mathf.PI * 100f * t) * 0.5f
                        + Mathf.Sin(2f * Mathf.PI * 200f * t) * 0.25f
                        + Mathf.Sin(2f * Mathf.PI * 300f * t) * 0.12f
                        + Mathf.Sin(2f * Mathf.PI * 1200f * t) * 0.03f
                        + (float)(rng.NextDouble() * 2 - 1) * 0.04f;
                data[i] = s * 0.6f;
            }
            var clip = AudioClip.Create("Bulb Hum", rate, 1, rate, false);
            clip.SetData(data, 0);
            return clip;
        }

        // a short electrical crackle: a few sharp pops in fading noise
        static AudioClip MakeCrackle(int seed)
        {
            const int rate = 22050;
            int n = (int)(rate * 0.25f);
            var data = new float[n];
            var rng = new System.Random(seed);
            float smooth = 0f;
            for (int i = 0; i < n; i++)
            {
                float env = Mathf.Exp(-6f * i / n);
                float noise = (float)(rng.NextDouble() * 2 - 1);
                smooth = Mathf.Lerp(smooth, noise, 0.6f);
                float pop = rng.NextDouble() < 0.012 ? (float)(rng.NextDouble() * 2 - 1) * 1.5f : 0f;
                data[i] = Mathf.Clamp((smooth * 0.4f + pop) * env, -1f, 1f);
            }
            var clip = AudioClip.Create("Bulb Crackle", n, 1, rate, false);
            clip.SetData(data, 0);
            return clip;
        }
    }
}
