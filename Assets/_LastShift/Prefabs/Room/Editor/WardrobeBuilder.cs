using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace LastShift.EditorTools
{
    // Makes Prefabs/Room/Wardrobe.prefab: a variant of the kit's staff locker with a wood finish,
    // and its door set to swing out into the room. Works in a preview scene, so no open scene is
    // touched. Also renders it closed and open to Logs/RoomShots/wardrobe_*.png.
    public static class WardrobeBuilder
    {
        const string KitLocker = "Assets/EscapePuzzleKitFree/Prefabs/locker_staff.prefab";
        const string PrefabPath = "Assets/_LastShift/Prefabs/Room/Wardrobe.prefab";
        const string MaterialPath = "Assets/_LastShift/Art/Materials/WardrobeWood.mat";
        const float DoorAngle = 105f; // the kit's own clip swings it -105, into the locker

        [MenuItem("Last Shift/Make Wardrobe Prefab")]
        public static void Make()
        {
            var kit = AssetDatabase.LoadAssetAtPath<GameObject>(KitLocker);
            if (kit == null) { Debug.LogWarning("[WardrobeBuilder] Kit staff locker not found."); return; }

            var scene = EditorSceneManager.NewPreviewScene();
            try
            {
                var wardrobe = (GameObject)PrefabUtility.InstantiatePrefab(kit, scene);
                wardrobe.name = "Wardrobe";

                // A copy of the kit's material, tinted dark wood. The kit material itself is untouched.
                var kitMat = wardrobe.GetComponentInChildren<Renderer>().sharedMaterial;
                var wood = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
                if (wood == null)
                {
                    wood = new Material(kitMat);
                    AssetDatabase.CreateAsset(wood, MaterialPath);
                }
                wood.SetColor("_Color", new Color(0.62f, 0.42f, 0.26f));
                EditorUtility.SetDirty(wood);
                foreach (var r in wardrobe.GetComponentsInChildren<Renderer>(true))
                    r.sharedMaterials = r.sharedMaterials.Select(m => m == kitMat ? wood : m).ToArray();

                // Door swings outward: a Turn on the door leaf instead of the kit's clip.
                var door = wardrobe.GetComponentsInChildren<Transform>(true).FirstOrDefault(t => t.name.EndsWith("locker_staff_door"));
                var stash = wardrobe.GetComponents<MonoBehaviour>().FirstOrDefault(m => m != null && m.GetType().Name == "Stash");
                if (stash != null && door != null)
                {
                    var so = new SerializedObject(stash);
                    so.FindProperty("motion.mode").enumValueIndex = 2; // Turn
                    so.FindProperty("motion.part").objectReferenceValue = door;
                    so.FindProperty("motion.axis").vector3Value = Vector3.up;
                    so.FindProperty("motion.angle").floatValue = DoorAngle;
                    so.FindProperty("motion.seconds").floatValue = 1f;
                    so.ApplyModifiedPropertiesWithoutUndo();
                }

                PrefabUtility.SaveAsPrefabAsset(wardrobe, PrefabPath);
                Render(scene, wardrobe, door);
            }
            finally
            {
                EditorSceneManager.ClosePreviewScene(scene);
            }
            AssetDatabase.SaveAssets();
            Debug.Log($"[WardrobeBuilder] Saved {PrefabPath}");
        }

        // Puts the wardrobe on Room C's WardrobeSpot: back to the wall, on the floor, placeholder removed.
        // Touches nothing else in the scene.
        [MenuItem("Last Shift/Place Wardrobe In Room C")]
        public static void PlaceInRoomC()
        {
            var active = SceneManager.GetActiveScene();
            if (!active.path.EndsWith("RoomC.unity"))
            {
                EditorUtility.DisplayDialog("Open Room C", "Open Scenes/RoomC.unity first, then run this again.", "OK");
                return;
            }
            var spot = GameObject.Find("RoomCExtras/PuzzleSpots/WardrobeSpot");
            var asset = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
            if (spot == null || asset == null) { Debug.LogWarning("[WardrobeBuilder] WardrobeSpot or the wardrobe prefab is missing."); return; }

            foreach (Transform child in spot.transform.Cast<Transform>().ToList())
                Object.DestroyImmediate(child.gameObject); // the blue placeholder (or an older wardrobe)

            var wardrobe = (GameObject)PrefabUtility.InstantiatePrefab(asset, spot.transform);
            wardrobe.name = "Wardrobe";
            wardrobe.transform.localPosition = Vector3.zero;
            wardrobe.transform.localRotation = Quaternion.identity;

            // Back face 1 cm off the wall behind the spot, bottom on the floor.
            var rs = wardrobe.GetComponentsInChildren<Renderer>();
            var b = rs[0].bounds; foreach (var r in rs) b.Encapsulate(r.bounds);
            var back = -spot.transform.forward;
            float wallDistance = 0f;
            if (Physics.Raycast(spot.transform.position + Vector3.up, back, out var hit, 2f)) wallDistance = hit.distance;
            var depthHalf = Mathf.Abs(b.extents.x * back.x) + Mathf.Abs(b.extents.z * back.z);
            var target = spot.transform.position + back * (wallDistance - depthHalf - 0.01f);
            target.y = b.extents.y;
            var centreOnFloor = new Vector3(b.center.x, b.center.y, b.center.z);
            wardrobe.transform.position += target - centreOnFloor;

            EditorSceneManager.MarkSceneDirty(active);
            EditorSceneManager.SaveScene(active);
            Debug.Log($"[WardrobeBuilder] Wardrobe placed on WardrobeSpot (wall {wallDistance:F2} m behind the spot) and Room C saved.");
        }

        static void Render(Scene scene, GameObject wardrobe, Transform door)
        {
            var lightGo = new GameObject("Light");
            SceneManager.MoveGameObjectToScene(lightGo, scene);
            var light = lightGo.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.3f;
            lightGo.transform.rotation = Quaternion.Euler(35, 160, 0);

            var camGo = new GameObject("Camera");
            SceneManager.MoveGameObjectToScene(camGo, scene);
            var cam = camGo.AddComponent<Camera>();
            cam.scene = scene;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.2f, 0.2f, 0.22f);
            cam.fieldOfView = 40;

            var rs = wardrobe.GetComponentsInChildren<Renderer>();
            var b = rs[0].bounds; foreach (var r in rs) b.Encapsulate(r.bounds);
            var from = b.center + new Vector3(1.2f, 0.3f, 3.6f);
            camGo.transform.SetPositionAndRotation(from, Quaternion.LookRotation(b.center - from));

            Directory.CreateDirectory("Logs/RoomShots");
            Shoot(cam, "wardrobe_closed");
            if (door != null)
            {
                var rest = door.localRotation;
                door.localRotation = rest * Quaternion.AngleAxis(DoorAngle, Vector3.up);
                Shoot(cam, "wardrobe_open");
                door.localRotation = rest;
            }
        }

        static void Shoot(Camera cam, string name)
        {
            var rt = new RenderTexture(640, 640, 24);
            cam.targetTexture = rt;
            cam.Render();
            RenderTexture.active = rt;
            var tex = new Texture2D(640, 640, TextureFormat.RGB24, false);
            tex.ReadPixels(new Rect(0, 0, 640, 640), 0, 0);
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
