using HeyHeyThere.EscapePuzzleKitFree;
using LastShift.Narration;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace LastShift.Music
{
    /// <summary>
    /// The game's music. The title plays the music box tune, both rooms have the drone (lower in
    /// Room C, where the tune also comes from the record player), and the escape fades into
    /// birdsong that carries on through the end card. Everything dips while Habimana is talking.
    /// It creates itself and lives across scenes, so no scene needs it placed. Scenes that aren't
    /// part of the game (sandboxes, kit demos) stay silent.
    /// </summary>
    public class MusicPlayer : MonoBehaviour
    {
        const string Title = "Title", RoomB = "RecordsRoomB", RoomC = "RoomC", End = "EndCard";

        MusicSet set;
        Track menu, drone, dawn, record;
        float duck = 1f;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        static void Create()
        {
            var set = Resources.Load<MusicSet>(MusicSet.ResourceName);
            if (set == null)
                return;
            var go = new GameObject("Music");
            DontDestroyOnLoad(go);
            go.AddComponent<MusicPlayer>().set = set;
        }

        void Start()
        {
            menu = new Track(gameObject, set.menu) { volume = set.menuVolume, crossfade = set.fade };
            drone = new Track(gameObject, set.drone) { volume = set.droneVolume, loopFrom = set.droneLoopFrom, crossfade = set.droneCrossfade };
            dawn = new Track(gameObject, set.dawn) { volume = set.dawnVolume, loopFrom = set.dawnLoopFrom, crossfade = set.fade };
            Apply(SceneManager.GetActiveScene());
        }

        void OnEnable()
        {
            SceneManager.sceneLoaded += OnSceneLoaded;
            Run.Escaped += OnEscaped;
        }

        void OnDisable()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            Run.Escaped -= OnEscaped;
        }

        void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (menu != null && mode == LoadSceneMode.Single)
                Apply(scene);
        }

        void Apply(Scene scene)
        {
            string name = scene.name;
            bool inRoom = name == RoomB || name == RoomC;

            menu.on = name == Title;
            drone.on = inRoom && !Run.HasEscaped;
            drone.pitch = name == RoomC ? set.roomCPitch : 1f;
            dawn.on = name == End || (inRoom && Run.HasEscaped);

            // the record player is part of the room, so its sound goes away with the scene
            record = null;
            if (name == RoomC && set.record != null)
            {
                var player = GameObject.Find(set.recordPlayerName);
                if (player != null)
                {
                    record = new Track(player, set.record, set.recordNear, set.recordFar) { volume = set.recordVolume, crossfade = set.fade, on = true };
                    record.level = 1f;  // already playing when you walk in
                }
            }
        }

        void OnEscaped()
        {
            // the door is open: the night is over
            drone.on = false;
            if (record != null)
                record.on = false;
            dawn.on = true;
        }

        void Update()
        {
            if (menu == null)
                return;
            // unscaled, because the kit freezes time for its menus and at the escape
            float dt = Time.unscaledDeltaTime;
            bool talking = Intercom.Current != null && Intercom.Current.IsPlaying;
            duck = Mathf.MoveTowards(duck, talking ? set.duckTo : 1f, dt / 0.4f);

            menu.Tick(dt, set.fade, duck);
            drone.Tick(dt, set.fade, duck);
            dawn.Tick(dt, set.fade, duck);
            if (record != null && !record.Gone)
                record.Tick(dt, set.fade, duck);
        }

        // One looping track. Near the end of the clip it fades into a second copy started from the
        // top, so the loop has no gap or jump even though these tracks weren't made to loop.
        class Track
        {
            readonly AudioSource[] sources = new AudioSource[2];
            readonly float[] weights = new float[2];
            readonly AudioClip clip;
            int active;
            float startedAt = -10f;

            public bool on;
            public float volume = 1f, pitch = 1f;
            public float level;  // 0 to 1, how far it has faded in
            // where it starts and loops back to, and how long the end overlaps that start
            public float loopFrom, crossfade = 2.5f;

            public bool Gone => sources[0] == null;

            public Track(GameObject host, AudioClip clip, float near = 0f, float far = 0f)
            {
                this.clip = clip;
                for (int i = 0; i < 2; i++)
                {
                    var s = host.AddComponent<AudioSource>();
                    s.playOnAwake = false;
                    s.loop = false;
                    s.clip = clip;
                    s.volume = 0f;
                    s.dopplerLevel = 0f;
                    if (far > 0f)
                    {
                        s.spatialBlend = 1f;
                        s.rolloffMode = AudioRolloffMode.Linear;
                        s.minDistance = near;
                        s.maxDistance = far;
                    }
                    sources[i] = s;
                }
            }

            public void Tick(float dt, float fade, float duck)
            {
                level = Mathf.MoveTowards(level, on ? 1f : 0f, dt / fade);
                if (clip == null)
                    return;

                if (level <= 0f && !on)
                {
                    for (int i = 0; i < 2; i++)
                    {
                        weights[i] = 0f;
                        if (sources[i].isPlaying)
                            sources[i].Stop();
                    }
                    return;
                }

                if (clip.loadState != AudioDataLoadState.Loaded)
                {
                    clip.LoadAudioData();
                    return;
                }

                var current = sources[active];
                // stopped while it should be playing: the first time, or the Quest dropping all sound.
                // the wait stops it being restarted every frame while the browser is still starting it
                if (!current.isPlaying && Time.realtimeSinceStartup - startedAt > 1f)
                {
                    Play(current);
                    weights[active] = 1f;
                }
                else if (clip.length - loopFrom > crossfade * 3f && current.time > clip.length - crossfade)
                {
                    active = 1 - active;
                    Play(sources[active]);
                }

                for (int i = 0; i < 2; i++)
                {
                    weights[i] = Mathf.MoveTowards(weights[i], i == active ? 1f : 0f, dt / crossfade);
                    sources[i].volume = weights[i] * level * volume * duck;
                    sources[i].pitch = pitch;
                    if (i != active && weights[i] <= 0f && sources[i].isPlaying)
                        sources[i].Stop();
                }
            }

            void Play(AudioSource s)
            {
                s.time = Mathf.Min(loopFrom, clip.length - 1f);
                s.Play();
                startedAt = Time.realtimeSinceStartup;
            }
        }
    }
}
