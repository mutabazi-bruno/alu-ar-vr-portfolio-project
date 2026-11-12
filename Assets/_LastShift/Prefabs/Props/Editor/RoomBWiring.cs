using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace LastShift.EditorTools
{
    // One pass over Records Room B (taken from Leslie for it): the unlabelled box beside the record
    // shelves. Safe to run again: it moves the box that's there instead of adding another.
    public static class RoomBWiring
    {
        const string ScenePath = "Assets/_LastShift/Scenes/RecordsRoomB.unity";
        const string Props = "Assets/_LastShift/Prefabs/Props";

        [MenuItem("Last Shift/Wire Room B")]
        public static void Wire()
        {
            if (EditorApplication.isPlaying || !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            var report = new StringBuilder();
            var all = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<Transform>(true)).Select(t => t.gameObject).ToList();
            GameObject Find(string name) => all.FirstOrDefault(g => g.name == name);

            // On the floor at the end of the record shelves, so it reads as one of the archive boxes.
            var shelving = Find("Shelving");
            var box = Find("OddBox") ?? Place(scene, $"{Props}/OddBox.prefab", shelving != null ? shelving.transform.parent.gameObject : null);
            if (shelving != null)
            {
                var b = Bounds(shelving);
                box.transform.SetPositionAndRotation(new Vector3(b.center.x, 0f, b.min.z - 0.35f), shelving.transform.rotation);
                report.AppendLine($"OddBox at {box.transform.position} (shelving {b.min} - {b.max}); adjust by eye");
            }
            else
                report.AppendLine("OddBox placed, but no Shelving found to stand it by");
            EditorUtility.SetDirty(box.transform);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("[RoomBWiring]\n" + report);
        }

        static GameObject Place(Scene scene, string path, GameObject parent)
        {
            var go = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(path), scene);
            if (parent != null)
                go.transform.SetParent(parent.transform, false);
            return go;
        }

        static Bounds Bounds(GameObject go)
        {
            var rs = go.GetComponentsInChildren<Renderer>(true);
            var b = rs[0].bounds;
            foreach (var r in rs) b.Encapsulate(r.bounds);
            return b;
        }
    }
}
