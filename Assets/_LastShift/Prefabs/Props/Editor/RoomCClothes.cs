using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace LastShift.EditorTools
{
    // Hangs Habimana's clothes on the hook rail in Leslie's Room C (taken from her for it) and sets
    // the rack's answers from Note 4. Safe to run again: it moves the pieces that are already there.
    public static class RoomCClothes
    {
        const string ScenePath = "Assets/_LastShift/Scenes/RoomC.unity";
        const string Props = "Assets/_LastShift/Prefabs/Props";
        const string Clothes = "Assets/_LastShift/Prefabs/Clothes";

        // Along the rail, mixed up so the rail doesn't give the rack's order away.
        static readonly (string name, string prefab)[] OnTheRail =
        {
            ("Shirt", $"{Props}/HabimanaShirt.prefab"),
            ("Coat", $"{Props}/HabimanaCoat.prefab"),
            ("Scarf", $"{Clothes}/Scarf.prefab"),
            ("Trousers", $"{Props}/HabimanaTrousers.prefab"),
        };

        [MenuItem("Last Shift/Place Habimana's Clothes (Room C)")]
        public static void Place()
        {
            if (EditorApplication.isPlaying || !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            var report = new StringBuilder();
            var all = scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<Transform>(true)).Select(t => t.gameObject).ToList();

            var rail = all.FirstOrDefault(g => g.name == "HookRail");
            if (rail == null)
            {
                Debug.LogWarning("[RoomCClothes] No HookRail in Room C.");
                return;
            }
            var railBounds = Bounds(rail);
            bool alongX = railBounds.size.x >= railBounds.size.z;
            // the rail is on a wall: the pieces face away from it, into the room
            var intoRoom = alongX ? new Vector3(0f, 0f, -Mathf.Sign(railBounds.center.z)) : new Vector3(-Mathf.Sign(railBounds.center.x), 0f, 0f);
            report.AppendLine($"HookRail bounds {railBounds.min} - {railBounds.max}, pieces face {intoRoom}");

            var pieces = new System.Collections.Generic.Dictionary<string, GameObject>();
            for (int i = 0; i < OnTheRail.Length; i++)
            {
                var (name, prefab) = OnTheRail[i];
                var go = all.FirstOrDefault(g => g.name == name && PrefabUtility.IsOutermostPrefabInstanceRoot(g));
                if (go == null)
                {
                    go = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(prefab), scene);
                    go.transform.SetParent(rail.transform.parent, true);
                    go.name = name;
                }
                // spread evenly along the rail, each piece's top just under it
                float t = (i + 0.5f) / OnTheRail.Length;
                var along = alongX ? new Vector3(Mathf.Lerp(railBounds.min.x, railBounds.max.x, t), 0f, railBounds.center.z)
                                   : new Vector3(railBounds.center.x, 0f, Mathf.Lerp(railBounds.min.z, railBounds.max.z, t));
                go.transform.rotation = Quaternion.LookRotation(intoRoom);
                go.transform.position = new Vector3(along.x, railBounds.min.y, along.z) + intoRoom * 0.12f;
                var b = Bounds(go);
                go.transform.position += Vector3.up * (railBounds.min.y - b.max.y);
                if (PrefabUtility.IsPartOfPrefabInstance(go)) PrefabUtility.RecordPrefabInstancePropertyModifications(go.transform);
                pieces[name] = go;
                report.AppendLine($"{name} ({System.IO.Path.GetFileNameWithoutExtension(prefab)}) at {go.transform.position}");
            }

            // Note 4: last on, first hung. Hook 1 coat, 2 scarf, 3 trousers, 4 shirt.
            var puzzle = all.Select(g => g.GetComponent<ClothesPuzzle>()).FirstOrDefault(p => p != null);
            var answers = new[] { "Coat", "Scarf", "Trousers", "Shirt" };
            if (puzzle != null)
            {
                for (int i = 0; i < puzzle.hooks.Length; i++)
                {
                    var hook = puzzle.hooks[i];
                    hook.answer = i < answers.Length ? pieces[answers[i]].GetComponent<ClothesPiece>() : null;
                    if (PrefabUtility.IsPartOfPrefabInstance(hook)) PrefabUtility.RecordPrefabInstancePropertyModifications(hook);
                    EditorUtility.SetDirty(hook);
                    report.AppendLine($"hook {i + 1} answer = {(hook.answer != null ? hook.answer.name : "none")}");
                }
                report.AppendLine($"needsCap = {puzzle.needsCap}");
            }
            else
                report.AppendLine("No ClothesPuzzle found: hook answers not set");

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("[RoomCClothes]\n" + report);
        }

        static Bounds Bounds(GameObject go)
        {
            var rs = go.GetComponentsInChildren<Renderer>(true).Where(r => r.enabled).ToArray();
            if (rs.Length == 0) return new Bounds(go.transform.position, Vector3.zero);
            var b = rs[0].bounds;
            foreach (var r in rs) b.Encapsulate(r.bounds);
            return b;
        }
    }
}
