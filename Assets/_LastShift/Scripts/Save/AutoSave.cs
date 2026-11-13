using System.Linq;
using HeyHeyThere.EscapePuzzleKitFree;
using LastShift.Menus;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace LastShift.Save
{
    /// <summary>
    /// Saves the player's progress by itself, so nobody has to start over: shortly after something is
    /// picked up or a story flag is set (a lock opened, a line said, the clothes hung), and on arriving
    /// in a room. It uses the kit's first save slot, so the pause menu's LOAD offers it, and the title
    /// screen can offer Continue with <see cref="HasSave"/> and <see cref="Continue"/>.
    /// Once the player has escaped the save is cleared, so Continue never starts a finished game.
    /// It creates itself, so no scene needs it placed.
    /// </summary>
    public class AutoSave : MonoBehaviour
    {
        public const int Slot = 0;

        // Only the rooms of the game are saved: never the title, the end card or a sandbox.
        static readonly string[] Rooms = { "RecordsRoomB", "RoomC" };

        const float Delay = 0.75f;      // after a change, so a burst of changes makes one save
        const float MinGap = 1.5f;      // between two saves

        float saveAt = -1f, lastSave = -10f;

        /// <summary>Whether there's progress to continue from.</summary>
        public static bool HasSave => SaveGame.Exists(Slot);

        /// <summary>The title's Continue: back to where the player was, with what they had.</summary>
        public static void Continue()
        {
            if (HasSave)
                SceneFader.Load(() => SaveGame.Load(Slot));
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Create()
        {
            var go = new GameObject("AutoSave");
            DontDestroyOnLoad(go);
            go.AddComponent<AutoSave>();
        }

        void OnEnable()
        {
            Run.FlagSet += OnFlagSet;
            Run.ItemsChanged += Soon;
            Run.Escaped += OnEscaped;
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        void OnDisable()
        {
            Run.FlagSet -= OnFlagSet;
            Run.ItemsChanged -= Soon;
            Run.Escaped -= OnEscaped;
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }

        void OnFlagSet(string flag) => Soon();

        void OnSceneLoaded(Scene scene, LoadSceneMode mode) => Soon();

        void Soon()
        {
            if (!SaveGame.Loading)
                saveAt = Time.realtimeSinceStartup + Delay;
        }

        void OnEscaped()
        {
            saveAt = -1f;
            SaveGame.Delete(Slot);
        }

        void Update()
        {
            if (saveAt < 0f || Time.realtimeSinceStartup < saveAt || Time.realtimeSinceStartup - lastSave < MinGap)
                return;
            saveAt = -1f;
            if (!InARoom || Run.HasEscaped || SaveGame.Loading)
                return;
            SaveGame.Save(Slot);
            lastSave = Time.realtimeSinceStartup;
        }

        static bool InARoom => Rooms.Contains(SceneManager.GetActiveScene().name);
    }
}
