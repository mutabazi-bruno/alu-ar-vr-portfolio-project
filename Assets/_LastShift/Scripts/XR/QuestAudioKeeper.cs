using UnityEngine;

namespace LastShift.XR
{
    /// <summary>
    /// On a Quest build the Android window can report "lost focus" while the VR session is still
    /// running in front of the player, and Unity then stops all sound. This restarts the audio
    /// output whenever that happens, so the game keeps its voice and sounds. Quest builds only.
    /// </summary>
    class QuestAudioKeeper : MonoBehaviour
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Create()
        {
            var go = new GameObject("Quest Audio Keeper");
            DontDestroyOnLoad(go);
            go.AddComponent<QuestAudioKeeper>();
        }

        float checkAt;

        void OnApplicationFocus(bool focused)
        {
            if (!focused) checkAt = Time.realtimeSinceStartup + 0.5f;
        }

        void Update()
        {
            if (checkAt <= 0f || Time.realtimeSinceStartup < checkAt) return;
            checkAt = 0f;
            // still rendering for the headset, so the player is still in the game: bring sound back
            if (UnityEngine.XR.XRSettings.isDeviceActive)
            {
                AudioListener.pause = false;
                AudioSettings.Reset(AudioSettings.GetConfiguration());
                // a reset stops every sound; start the looping ones (hums, ambience) again
                foreach (var s in FindObjectsByType<AudioSource>(FindObjectsSortMode.None))
                    if (s.loop && s.clip != null && s.isActiveAndEnabled) s.Play();
                Debug.Log("QuestAudioKeeper: focus lost while in VR, audio restarted");
            }
        }
#endif
    }
}
