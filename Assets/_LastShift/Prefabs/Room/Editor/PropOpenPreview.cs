using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace LastShift.EditorTools
{
    // Renders the room's openable props closed and fully open, using copies so the scene itself is
    // never changed. Saves Logs/RoomShots/open_<name>.png and closed_<name>.png.
    public static class PropOpenPreview
    {
        static readonly System.Collections.Generic.Dictionary<string, (string part, float angle)> TurnFix =
            new System.Collections.Generic.Dictionary<string, (string, float)>
            {
                { "StaffLocker", ("locker_staff_door", 105f) },
                { "KeyCabinet", ("key_cabinet_door", 110f) },
                { "WallSafe", ("safe_wall_door", 100f) },
            };

        static readonly string[] Props = { "StaffLocker", "KeyCabinet", "WallSafe", "Chest", "Drawers", "VentCover", "SteelDoor", "WoodenDoor" };

        [MenuItem("Last Shift/Preview Props Open")]
        public static void Run()
        {
            var props = GameObject.Find("Room/Props");
            if (props == null) { Debug.LogWarning("[PropOpenPreview] No Room/Props in the open scene."); return; }
            Directory.CreateDirectory("Logs/RoomShots");

            var oldAmbient = RenderSettings.ambientLight;
            RenderSettings.ambientLight = new Color(0.6f, 0.6f, 0.6f);
            var camGo = new GameObject("PreviewCamera") { hideFlags = HideFlags.HideAndDontSave };
            var cam = camGo.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.15f, 0.15f, 0.18f);
            cam.nearClipPlane = 0.05f;
            cam.fieldOfView = 55;

            try
            {
                foreach (var name in Props)
                {
                    var original = props.transform.Find(name);
                    if (original == null) continue;

                    var copy = Object.Instantiate(original.gameObject, original.position, original.rotation);
                    copy.hideFlags = HideFlags.HideAndDontSave;
                    copy.transform.localScale = original.lossyScale;
                    original.gameObject.SetActive(false);

                    var b = Bounds(copy);
                    var front = original.forward;
                    var side = Vector3.Cross(Vector3.up, front);
                    var from = b.center + front * (b.extents.magnitude * 2.2f + 0.6f) + side * 0.6f + Vector3.up * 0.25f;
                    cam.transform.SetPositionAndRotation(from, Quaternion.LookRotation(b.center - from));

                    Shoot(cam, $"closed_{name}");
                    foreach (var anim in copy.GetComponentsInChildren<Animation>(true))
                    {
                        var clip = anim.GetClip("open");
                        if (clip != null) clip.SampleAnimation(anim.gameObject, clip.length);
                        else foreach (AnimationState s in anim) if (s.clip.name.StartsWith("drawer")) s.clip.SampleAnimation(anim.gameObject, s.clip.length);
                    }
                    Shoot(cam, $"open_{name}");
                    Object.DestroyImmediate(copy);

                    // The same prop with its door turned the other way: what a Turn motion would do.
                    if (TurnFix.TryGetValue(name, out var fix))
                    {
                        copy = Object.Instantiate(original.gameObject, original.position, original.rotation);
                        copy.hideFlags = HideFlags.HideAndDontSave;
                        copy.SetActive(true); // the original is hidden while we shoot, so the copy starts hidden too
                        var part = copy.GetComponentsInChildren<Transform>(true).FirstOrDefault(t => t.name.EndsWith(fix.part));
                        if (part != null)
                        {
                            part.localRotation = part.localRotation * Quaternion.AngleAxis(fix.angle, Vector3.up);
                            Shoot(cam, $"turned_{name}");
                        }
                        Object.DestroyImmediate(copy);
                    }

                    original.gameObject.SetActive(true);
                }
            }
            finally
            {
                RenderSettings.ambientLight = oldAmbient;
                Object.DestroyImmediate(camGo);
            }
            Debug.Log("[PropOpenPreview] Saved to Logs/RoomShots");
        }

        static Bounds Bounds(GameObject go)
        {
            var rs = go.GetComponentsInChildren<Renderer>();
            var b = rs.Length > 0 ? rs[0].bounds : new Bounds(go.transform.position, Vector3.one);
            foreach (var r in rs) b.Encapsulate(r.bounds);
            return b;
        }

        static void Shoot(Camera cam, string name)
        {
            var rt = new RenderTexture(640, 480, 24);
            cam.targetTexture = rt;
            cam.Render();
            RenderTexture.active = rt;
            var tex = new Texture2D(640, 480, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, 640, 480), 0, 0);
            tex.Apply();
            RenderTexture.active = null;
            cam.targetTexture = null;
            File.WriteAllBytes($"Logs/RoomShots/{name}.png", tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
            rt.Release();
            Object.DestroyImmediate(rt);
        }
    }
}
