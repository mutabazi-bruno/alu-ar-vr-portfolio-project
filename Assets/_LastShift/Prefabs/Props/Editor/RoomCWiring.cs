using System.Collections.Generic;
using System.Linq;
using System.Text;
using HeyHeyThere.EscapePuzzleKitFree;
using LastShift.Props;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace LastShift.EditorTools
{
    // One pass over Room C (taken from Leslie for it): swaps in our props, Bruno's clothes, the
    // hook answers, the story beats and Note 4, then saves. Safe to run again: it skips what's done.
    public static class RoomCWiring
    {
        const string ScenePath = "Assets/_LastShift/Scenes/RoomC.unity";
        const string Props = "Assets/_LastShift/Prefabs/Props";
        const string Story = Props + "/Story";
        const string Clothes = "Assets/_LastShift/Prefabs/Clothes";

        static readonly StringBuilder report = new StringBuilder();
        static Scene scene;

        [MenuItem("Last Shift/Wire Room C")]
        public static void Wire()
        {
            if (EditorApplication.isPlaying || !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;
            scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            report.Clear();

            // 1. Dressing and the doors: our variants in the same place, under the same names.
            Swap("StaffLocker", $"{Props}/RoomCLocker.prefab");
            Swap("KeyCabinet", $"{Props}/RoomCKeyCabinet.prefab");
            Swap("Chest", $"{Props}/RoomCChest.prefab");
            Swap("Drawers", $"{Props}/RoomCDrawers.prefab");
            Swap("WallSafe", $"{Props}/RoomCSafe.prefab");
            Swap("ExitDoor", $"{Props}/ExitDoor.prefab");
            Swap("Wardrobe", $"{Props}/WardrobeLocked.prefab");

            // 2. Bruno's clothes where the stand-ins were; the cap goes in the wardrobe.
            var coat = Clothe("ClothesStandIn_Jacket", "Coat");
            var shirt = Clothe("ClothesStandIn_Shirt", "Shirt");
            var scarf = Clothe("ClothesStandIn_Scarf", "Scarf");
            var trousers = Clothe("ClothesStandIn_Trousers", "Trousers");
            var cap = Clothe("ClothesStandIn_Cap", "Cap");
            var wardrobe = Find("Wardrobe");
            if (cap != null && wardrobe != null)
            {
                var b = Bounds(wardrobe);
                cap.transform.position = new Vector3(b.center.x, b.min.y + b.size.y * 0.55f, b.center.z);
                Record(cap.transform);
                report.AppendLine($"Cap placed inside the wardrobe at {cap.transform.position}");
            }

            // 3. Hook answers from Note 4: last on, first hung.
            Answers(coat, scarf, trousers, shirt);

            // 4. Story beats. The rooms run along z: corridor, Room C's door, the room, the mirror's
            // doorway, then the mirror side with the rack, the wardrobe and the exit.
            var parent = Find("PuzzleSpots") ?? Find("RoomCExtras");
            var roomDoor = Find("RoomCDoor");
            var mirror = Find("Doorway_Mirror");
            var rack = Find("ClothesRack") ?? Find("RackSpot");
            if (roomDoor != null && mirror != null)
            {
                // just inside Room C's door, so H07 plays as the player walks into the room
                var inward = Vector3.forward * Mathf.Sign(mirror.transform.position.z - roomDoor.transform.position.z);
                Beat("RoomCEntranceBeat", parent, roomDoor.transform.position + inward * 1f + Vector3.up * 0.05f, Quaternion.LookRotation(inward), true);
            }
            if (mirror != null && rack != null)
            {
                // just through the glass, on the rack's side
                var through = Vector3.forward * Mathf.Sign(rack.transform.position.z - mirror.transform.position.z);
                Beat("MirrorBeat", parent, mirror.transform.position + through * 0.8f + Vector3.up * 0.05f, Quaternion.LookRotation(through), true);
            }
            Beat("ClothesSolvedBeat", parent, Vector3.zero, Quaternion.identity);
            var exitBeat = Beat("ExitUnlockBeat", parent, Vector3.zero, Quaternion.identity);
            var dawn = Find("DawnEnding");
            if (exitBeat != null && dawn != null)
            {
                var gate = exitBeat.GetComponent<FlagGate>();
                gate.show = new[] { dawn };
                Record(gate);
                report.AppendLine("ExitUnlockBeat shows DawnEnding");
            }
            SignInSheet(Beat("RoomCSignInBeat", parent, Vector3.zero, Quaternion.identity));

            // 5. Note 4, by the rack on the mirror side.
            if (rack != null)
                Note4(rack.transform);

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("[RoomCWiring]\n" + report);
        }

        static IEnumerable<GameObject> All() =>
            scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<Transform>(true)).Select(t => t.gameObject);

        static GameObject Find(string name) => All().FirstOrDefault(g => g.name == name);

        // Saves file objects by their path in the hierarchy, so the swapped prop keeps its old name
        // (replacing a prefab renames the instance after the new prefab).
        static void Swap(string name, string variantPath)
        {
            var variantName = System.IO.Path.GetFileNameWithoutExtension(variantPath);
            var go = All().FirstOrDefault(g => PrefabUtility.IsOutermostPrefabInstanceRoot(g) &&
                (g.name == name || g.name == variantName && PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(g) == variantPath));
            var variant = AssetDatabase.LoadAssetAtPath<GameObject>(variantPath);
            if (go == null || variant == null) { report.AppendLine($"SKIPPED swap {name} ({(go == null ? "not in scene" : "no variant")})"); return; }
            if (PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(go) != variantPath)
                PrefabUtility.ReplacePrefabAssetOfPrefabInstance(go, variant, InteractionMode.AutomatedAction);
            go.name = name;
            Record(go);
            report.AppendLine($"{name} = {variantName} at {go.transform.position}");
        }

        static GameObject Clothe(string standInName, string prefabName)
        {
            var existing = All().FirstOrDefault(g => g.name == prefabName && PrefabUtility.IsOutermostPrefabInstanceRoot(g)
                && PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(g) == $"{Clothes}/{prefabName}.prefab");
            var standIn = All().FirstOrDefault(g => g.name == standInName && g.transform.parent != null && g.transform.parent.name == "ClothesStandIns"
                && g.transform.parent.parent != null && g.transform.parent.parent.name == "Dressing");
            if (existing != null)
            {
                if (standIn != null) Object.DestroyImmediate(standIn);
                report.AppendLine($"{prefabName} already in the room at {existing.transform.position}; stand-in {(standIn != null ? "removed" : "gone")}");
                return existing;
            }
            var go = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>($"{Clothes}/{prefabName}.prefab"), scene);
            if (standIn != null)
            {
                go.transform.SetParent(standIn.transform.parent, false);
                go.transform.SetPositionAndRotation(standIn.transform.position, standIn.transform.rotation);
                Object.DestroyImmediate(standIn);
            }
            report.AppendLine($"{prefabName} placed at {go.transform.position} (stand-in {(standIn != null ? "removed" : "not found")})");
            return go;
        }

        static void Answers(params GameObject[] inOrder)
        {
            var puzzle = All().Select(g => g.GetComponent<ClothesPuzzle>()).FirstOrDefault(p => p != null);
            if (puzzle == null) { report.AppendLine("SKIPPED answers: no ClothesPuzzle"); return; }
            for (int i = 0; i < puzzle.hooks.Length; i++)
            {
                var hook = puzzle.hooks[i];
                var piece = i < inOrder.Length && inOrder[i] != null ? inOrder[i].GetComponent<ClothesPiece>() : null;
                hook.answer = piece;
                EditorUtility.SetDirty(hook);
                PrefabUtility.RecordPrefabInstancePropertyModifications(hook);
                report.AppendLine($"hook {i + 1} ({hook.name}, label '{hook.label}', x {hook.transform.position.x:F2}) answer = {(piece != null ? piece.pieceId : "none")}");
            }
            report.AppendLine($"needsCap = {puzzle.needsCap}");
        }

        static GameObject Beat(string prefabName, GameObject parent, Vector3 at, Quaternion facing, bool place = false)
        {
            var existing = All().FirstOrDefault(g => g.name == prefabName);
            if (existing != null)
            {
                if (place) { existing.transform.SetPositionAndRotation(at, facing); Record(existing.transform); }
                report.AppendLine($"{prefabName} already in the room{(place ? $", moved to {at}" : "")}");
                return existing;
            }
            var go = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>($"{Story}/{prefabName}.prefab"), scene);
            if (parent != null) go.transform.SetParent(parent.transform, true);
            go.transform.SetPositionAndRotation(at, facing);
            report.AppendLine($"{prefabName} placed at {at}");
            return go;
        }

        static void SignInSheet(GameObject beat)
        {
            var sheet = All().FirstOrDefault(g => g.name == "SignInSheet" && g.GetComponent<Renderer>() != null);
            if (sheet == null || beat == null) { report.AppendLine("SKIPPED sign-in sheet"); return; }
            var look = sheet.GetComponent<Inspectable>();
            if (look == null)
                look = sheet.AddComponent<Inspectable>();
            look.label = "Sign-in sheet";
            look.text = "The last row is new: 14 Nov, no name, in at 02:14.";
            look.beat = beat.GetComponent<StoryBeat>();
            if (sheet.GetComponent<Collider>() == null)
            {
                var box = sheet.AddComponent<BoxCollider>();
                box.size = new Vector3(1f, 1f, 0.02f);
            }
            report.AppendLine($"SignInSheet at {sheet.transform.position} examines and fires H08");
        }

        static void Note4(Transform rack)
        {
            var go = All().FirstOrDefault(g => g.name == "Note4");
            if (go == null)
            {
                var item = AssetDatabase.LoadAssetAtPath<Item>("Assets/_LastShift/Items/Note4.asset");
                go = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/EscapePuzzleKitFree/Prefabs/note.prefab"), scene);
                go.name = "Note4";
                foreach (var look in go.GetComponentsInChildren<Inspectable>(true))
                    Object.DestroyImmediate(look);
                var pickup = go.AddComponent<Pickup>();
                pickup.item = item;
                pickup.examine = true;
                pickup.label = "Note";
                go.transform.SetParent(rack.parent, true);
            }
            // on the floor in front of the rack, on the room side of it
            var b = Bounds(rack.gameObject);
            go.transform.position = new Vector3(b.max.x + 0.4f, 0.01f, b.center.z);
            Record(go.transform);
            report.AppendLine($"Note4 pickup at {go.transform.position} (rack bounds {b.min} - {b.max}); adjust by eye");
        }

        // Keeps a change made to a prefab instance in the scene as an override, so a reload doesn't undo it.
        static void Record(Object o)
        {
            if (PrefabUtility.IsPartOfPrefabInstance(o))
                PrefabUtility.RecordPrefabInstancePropertyModifications(o);
            EditorUtility.SetDirty(o);
        }

        static Bounds Bounds(GameObject go)
        {
            var rs = go.GetComponentsInChildren<Renderer>(true);
            var b = rs.Length > 0 ? rs[0].bounds : new Bounds(go.transform.position, Vector3.one);
            foreach (var r in rs) b.Encapsulate(r.bounds);
            return b;
        }
    }
}
