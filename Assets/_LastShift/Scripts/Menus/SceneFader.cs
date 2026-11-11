using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace LastShift.Menus
{
    /// <summary>
    /// Changes scene behind a short fade to black, the same on a monitor and in a headset (the fade
    /// sits just in front of whichever camera is showing). Made on first use and kept across scenes.
    /// </summary>
    public class SceneFader : MonoBehaviour
    {
        const float FadeTime = 0.4f;
        static SceneFader instance;
        Canvas canvas;
        CanvasGroup group;

        public static bool Busy { get; private set; }

        /// <summary>Fades out, runs the load, fades back in.</summary>
        public static void Load(System.Action load)
        {
            if (Busy) return;
            Get().StartCoroutine(instance.Run(load));
        }

        public static void Load(string scene) => Load(() => SceneManager.LoadScene(scene));

        static SceneFader Get()
        {
            if (instance != null) return instance;
            var go = new GameObject("Scene Fader", typeof(RectTransform));
            DontDestroyOnLoad(go);
            instance = go.AddComponent<SceneFader>();
            instance.canvas = go.AddComponent<Canvas>();
            instance.canvas.sortingOrder = 32000;
            instance.group = go.AddComponent<CanvasGroup>();
            instance.group.alpha = 0f;
            instance.group.blocksRaycasts = false;
            var black = new GameObject("Black", typeof(RectTransform)).AddComponent<Image>();
            black.transform.SetParent(go.transform, false);
            black.color = Color.black;
            black.raycastTarget = false;
            var rt = (RectTransform)black.transform;
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
            return instance;
        }

        // in front of the camera that's on now: works for the desktop camera and the headset
        void Aim()
        {
            var cam = Camera.main;
            if (cam != null)
            {
                canvas.renderMode = RenderMode.ScreenSpaceCamera;
                canvas.worldCamera = cam;
                canvas.planeDistance = Mathf.Max(cam.nearClipPlane + 0.02f, 0.1f);
            }
            else canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        }

        IEnumerator Run(System.Action load)
        {
            Busy = true;
            Aim();
            yield return Fade(0f, 1f);
            Time.timeScale = 1f;
            load();
            yield return null;
            yield return null;
            Aim();
            yield return Fade(1f, 0f);
            Busy = false;
        }

        IEnumerator Fade(float from, float to)
        {
            for (float t = 0f; t < FadeTime; t += Time.unscaledDeltaTime)
            {
                group.alpha = Mathf.Lerp(from, to, t / FadeTime);
                yield return null;
            }
            group.alpha = to;
        }
    }
}
