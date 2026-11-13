using HeyHeyThere.EscapePuzzleKitFree;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace LastShift.XR
{
    /// <summary>
    /// In a headset the padlock on the locker, the chest or the key cabinet is a small thing to hit
    /// with a ray, and pointing at the rest of the prop shows nothing. While a padlock is still
    /// locked, this makes the whole prop count as the padlock: an invisible box around it that
    /// belongs to the padlock, so the prompt shows wherever the ray rests on the prop. Once it's
    /// unlocked the box goes, so the door and what's inside work as before.
    /// The XR pointer sets it up in each room it wakes in.
    /// </summary>
    public class PadlockReach : MonoBehaviour
    {
        Padlock padlock;
        BoxCollider box;
        static Scene doneFor;

        public static void SetUp()
        {
            var scene = SceneManager.GetActiveScene();
            if (scene == doneFor) return;
            doneFor = scene;
            foreach (var p in FindObjectsByType<Padlock>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
                if (!p.IsOn) Add(p);
        }

        static void Add(Padlock p)
        {
            // the prop the padlock is on: the nearest thing above it that can be used (its door or lid)
            var owner = p.transform.parent != null ? p.transform.parent.GetComponentInParent<Interactable>() : null;
            var prop = owner != null ? owner.transform : p.transform.parent;
            if (prop == null) return;
            var renderers = prop.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0) return;
            var bounds = renderers[0].bounds;
            foreach (var r in renderers) bounds.Encapsulate(r.bounds);
            // only a piece of furniture, never a whole room
            if (bounds.size.x > 3f || bounds.size.y > 3f || bounds.size.z > 3f) return;

            var go = new GameObject("Padlock Reach");
            go.layer = p.gameObject.layer;
            go.transform.SetParent(p.transform, false);
            var reach = go.AddComponent<PadlockReach>();
            reach.padlock = p;
            reach.box = go.AddComponent<BoxCollider>();

            // the prop's bounds, a little larger, in the padlock's own space
            var min = Vector3.one * float.MaxValue;
            var max = Vector3.one * float.MinValue;
            var e = bounds.extents + Vector3.one * 0.01f;
            for (var i = 0; i < 8; i++)
            {
                var corner = bounds.center + Vector3.Scale(e, new Vector3((i & 1) == 0 ? -1 : 1, (i & 2) == 0 ? -1 : 1, (i & 4) == 0 ? -1 : 1));
                var local = go.transform.InverseTransformPoint(corner);
                min = Vector3.Min(min, local);
                max = Vector3.Max(max, local);
            }
            reach.box.center = (min + max) * 0.5f;
            reach.box.size = max - min;
        }

        void Update()
        {
            if (padlock == null || padlock.IsOn)
                Destroy(gameObject);
        }
    }
}
