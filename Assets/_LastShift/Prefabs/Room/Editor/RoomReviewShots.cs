using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace LastShift.EditorTools
{
    // Renders a few fixed views of Records Room B to Logs/RoomShots (ignored by git),
    // with a text report of where everything ended up. Handy for checking the layout quickly.
    public static class RoomReviewShots
    {
        const string OutFolder = "Logs/RoomShots";

        [MenuItem("Last Shift/Capture Review Shots")]
        public static void Capture() => Capture(true);

        [MenuItem("Last Shift/Capture Review Shots (Game Lighting)")]
        public static void CaptureDark() => Capture(false);

        static void Capture(bool brighten)
        {
            var room = GameObject.Find("Room");
            if (room == null) { Debug.LogWarning("[RoomReviewShots] No Room object in the open scene."); return; }
            Directory.CreateDirectory(OutFolder);

            var oldAmbient = RenderSettings.ambientLight;
            if (brighten) RenderSettings.ambientLight = new Color(0.55f, 0.55f, 0.55f);

            var ceilings = room.GetComponentsInChildren<Renderer>().Where(r => r.name.StartsWith("Ceiling") || r.transform.parent != null && r.transform.parent.name.StartsWith("Ceiling")).ToList();
            var camGo = new GameObject("ReviewCamera") { hideFlags = HideFlags.HideAndDontSave };
            var cam = camGo.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.15f, 0.15f, 0.18f);
            cam.nearClipPlane = 0.05f;

            string suffix = brighten ? "" : "_dark";
            try
            {
                // Top-down plan with the ceiling hidden.
                foreach (var r in ceilings) r.enabled = false;
                cam.orthographic = true;
                cam.orthographicSize = 3.3f;
                camGo.transform.SetPositionAndRotation(new Vector3(0, 10, 0), Quaternion.Euler(90, 0, 0));
                Shoot(cam, $"plan{suffix}", 1600, 1200);
                foreach (var r in ceilings) r.enabled = true;

                cam.orthographic = false;
                cam.fieldOfView = 75;
                Shot(cam, $"start{suffix}", new Vector3(0, 1.6f, -1.6f), new Vector3(0, 1.3f, 2.5f));
                Shot(cam, $"back{suffix}", new Vector3(0, 1.6f, -0.8f), new Vector3(0, 1.1f, 2.5f));
                Shot(cam, $"right{suffix}", new Vector3(-1.0f, 1.5f, 0f), new Vector3(3f, 1.1f, 0f));
                Shot(cam, $"left{suffix}", new Vector3(1.0f, 1.5f, 0f), new Vector3(-3f, 1.1f, 0f));
                Shot(cam, $"front{suffix}", new Vector3(0, 1.6f, 1.2f), new Vector3(0, 1.2f, -2.5f));
                Shot(cam, $"shelving{suffix}", new Vector3(1.4f, 1.2f, 1.2f), new Vector3(3f, 1.0f, 1.2f));
                Shot(cam, $"bench{suffix}", new Vector3(0.2f, 1.6f, 1.0f), new Vector3(0.2f, 0.9f, 2.4f));
            }
            finally
            {
                foreach (var r in ceilings) r.enabled = true;
                Object.DestroyImmediate(camGo);
                RenderSettings.ambientLight = oldAmbient;
            }

            CaptureBadge();
            WriteReport(room);
            Debug.Log($"[RoomReviewShots] Saved to {Path.GetFullPath(OutFolder)}");
        }

        // The ID badge from both sides: +Z should show the front, -Z the scratched "HOOK 6".
        static void CaptureBadge()
        {
            var asset = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_LastShift/Art/Models/StaffIDBadge.prefab");
            if (asset == null) return;
            var badge = (GameObject)Object.Instantiate(asset, new Vector3(0, -500, 0), Quaternion.identity);
            badge.hideFlags = HideFlags.HideAndDontSave;
            var camGo = new GameObject("BadgeCamera") { hideFlags = HideFlags.HideAndDontSave };
            var cam = camGo.AddComponent<Camera>();
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.2f, 0.2f, 0.22f);
            cam.nearClipPlane = 0.01f;
            cam.fieldOfView = 30;
            var lightGo = new GameObject("BadgeLight") { hideFlags = HideFlags.HideAndDontSave };
            var light = lightGo.AddComponent<Light>();
            light.type = LightType.Directional;
            var old = RenderSettings.ambientLight;
            RenderSettings.ambientLight = Color.white;
            try
            {
                var centre = badge.transform.position;
                foreach (var (name, side) in new[] { ("badge_plus_z", 1f), ("badge_minus_z", -1f) })
                {
                    var from = centre + new Vector3(0, 0, 0.2f * side);
                    cam.transform.SetPositionAndRotation(from, Quaternion.LookRotation(centre - from));
                    lightGo.transform.rotation = cam.transform.rotation;
                    Shoot(cam, name, 640, 420);
                }
            }
            finally
            {
                RenderSettings.ambientLight = old;
                Object.DestroyImmediate(badge);
                Object.DestroyImmediate(camGo);
                Object.DestroyImmediate(lightGo);
            }
        }

        static void Shot(Camera cam, string name, Vector3 from, Vector3 lookAt)
        {
            cam.transform.SetPositionAndRotation(from, Quaternion.LookRotation(lookAt - from));
            Shoot(cam, name, 1280, 800);
        }

        static void Shoot(Camera cam, string name, int w, int h)
        {
            var rt = new RenderTexture(w, h, 24, RenderTextureFormat.ARGB32);
            cam.targetTexture = rt;
            cam.Render();
            var prev = RenderTexture.active;
            RenderTexture.active = rt;
            var tex = new Texture2D(w, h, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, w, h), 0, 0);
            tex.Apply();
            RenderTexture.active = prev;
            cam.targetTexture = null;
            File.WriteAllBytes(Path.Combine(OutFolder, name + ".png"), tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
            rt.Release();
            Object.DestroyImmediate(rt);
        }

        static void WriteReport(GameObject room)
        {
            var sb = new StringBuilder();
            foreach (Transform group in room.transform)
            {
                sb.AppendLine($"== {group.name}");
                foreach (Transform t in group)
                {
                    var rs = t.GetComponentsInChildren<Renderer>();
                    string bounds = "no renderer";
                    if (rs.Length > 0)
                    {
                        var b = rs[0].bounds;
                        foreach (var r in rs) b.Encapsulate(r.bounds);
                        bounds = $"centre {b.center:F2} size {b.size:F2} min {b.min:F2} max {b.max:F2}";
                    }
                    sb.AppendLine($"{t.name,-28} active={t.gameObject.activeSelf} yaw={t.eulerAngles.y:F0} scale={t.localScale:F2} {bounds}");
                }
            }
            File.WriteAllText(Path.Combine(OutFolder, "report.txt"), sb.ToString());
        }
    }
}
