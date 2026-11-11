using System.Collections.Generic;
using HeyHeyThere.EscapePuzzleKitFree;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace LastShift.Settings
{
    /// <summary>
    /// Applies <see cref="GameSettings"/> to every scene: the flashlights' drain (the desktop one and
    /// the VR torch both use the kit's Flashlight), the master volume and the walker's mouse speed.
    /// It creates itself when the game starts, so no scene needs it placed.
    /// </summary>
    public class SettingsApplier : MonoBehaviour
    {
        // Each component's value as built, so a setting scales it rather than replacing it.
        readonly Dictionary<Flashlight, float> builtDrain = new Dictionary<Flashlight, float>();
        readonly Dictionary<FirstPersonWalker, float> builtMouse = new Dictionary<FirstPersonWalker, float>();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Create()
        {
            var go = new GameObject("LastShiftSettings");
            DontDestroyOnLoad(go);
            go.AddComponent<SettingsApplier>();
            go.AddComponent<SettingsMenu>();
        }

        void OnEnable()
        {
            GameSettings.Changed += Apply;
            SceneManager.sceneLoaded += OnSceneLoaded;
            Apply();
        }

        void OnDisable()
        {
            GameSettings.Changed -= Apply;
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }

        void OnSceneLoaded(Scene scene, LoadSceneMode mode) => Apply();

        // A loaded save brings back its battery level, so keep topping it up while drain is off.
        void Update()
        {
            if (!GameSettings.BatteryDrain && Run.Battery < 1f)
                Run.Battery = 1f;
        }

        void Apply()
        {
            ForgetDestroyed(builtDrain);
            ForgetDestroyed(builtMouse);

            bool drain = GameSettings.BatteryDrain;
            foreach (var torch in FindObjectsByType<Flashlight>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (!builtDrain.ContainsKey(torch))
                    builtDrain[torch] = torch.drain;
                torch.drain = drain ? builtDrain[torch] : 0f;
            }
            // Turning drain off also brings a flat torch back, so nobody is left in the dark.
            if (!drain)
                Run.Battery = 1f;

            AudioListener.volume = GameSettings.Volume;

            foreach (var walker in FindObjectsByType<FirstPersonWalker>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                if (!builtMouse.ContainsKey(walker))
                    builtMouse[walker] = walker.mouse;
                walker.mouse = builtMouse[walker] * GameSettings.MouseSensitivity;
            }
        }

        // Components destroyed with their scene.
        static void ForgetDestroyed<T>(Dictionary<T, float> built) where T : Object
        {
            var gone = new List<T>();
            foreach (var key in built.Keys)
                if (key == null)
                    gone.Add(key);
            foreach (var key in gone)
                built.Remove(key);
        }
    }
}
