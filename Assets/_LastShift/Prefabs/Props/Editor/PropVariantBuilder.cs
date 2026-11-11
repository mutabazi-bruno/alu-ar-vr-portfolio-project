using System;
using System.Linq;
using HeyHeyThere.EscapePuzzleKitFree;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Events;

namespace LastShift.EditorTools
{
    // LastShift.Keypad is a namespace too, so name the kit's component outright.
    using Keypad = HeyHeyThere.EscapePuzzleKitFree.Keypad;

    // Makes our variants of the kit's props in Prefabs/Props, under the names the room builder
    // looks for, and sets their locks, codes and keys from the values below.
    // Run it from the menu: Last Shift > Build Prop Variants.
    // Running it again only resets the fields set here, so sounds or subtitles added to a
    // variant afterwards are kept.
    public static class PropVariantBuilder
    {
        const string KitFolder = "Assets/EscapePuzzleKitFree";
        const string VariantFolder = "Assets/_LastShift/Prefabs/Props";
        static readonly string[] ItemFolders = { "Assets/_LastShift/Items", "Assets/EscapePuzzleKitFree/Items" };

        // Codes from the script.
        const string SafeCode = "9437";
        const string ExitCode = "2207";  // Habimana's last sign-in time, 13 Nov

        // Item ids of the keys. Leave "" until the item exists, then run the menu again.
        const string LockerKey = "";
        const string KeyCabinetKey = "";
        const string ChestKey = "chest_key_hook6";

        // Flags set when each lock opens. Doors and story beats wait on these.
        // Don't rename after Day 3: saves store them.
        const string ExitCodeFlag = "exit_code_entered";
        const string SafeFlag = "wall_safe_opened";
        const string LockerFlag = "staff_locker_unlocked";
        const string KeyCabinetFlag = "key_cabinet_unlocked";
        const string ChestFlag = "chest_unlocked";

        [MenuItem("Last Shift/Build Prop Variants")]
        static void Build()
        {
            if (!AssetDatabase.IsValidFolder(KitFolder))
            {
                EditorUtility.DisplayDialog("Kit missing",
                    "Import Escape Puzzle Kit Free 1.1.0 first (it should land in Assets/EscapePuzzleKitFree).", "OK");
                return;
            }
            if (!AssetDatabase.IsValidFolder(VariantFolder))
                AssetDatabase.CreateFolder("Assets/_LastShift/Prefabs", "Props");

            Variant("StaffLocker", "locker_staff", go =>
            {
                KeyLock(go, "Staff locker", LockerKey, LockerFlag);
                SwingOutward(go.GetComponentInChildren<Stash>(true), "locker_staff_door", 105f, 0.8f);
            });
            Variant("KeyCabinet", "key_cabinet", go =>
            {
                KeyLock(go, "Key cabinet", KeyCabinetKey, KeyCabinetFlag);
                SwingOutward(go.GetComponentInChildren<Stash>(true), "key_cabinet_door", 110f, 0.7f);
            });
            Variant("Chest", "chest_wood", go => KeyLock(go, "Chest", ChestKey, ChestFlag));
            Variant("WallSafe", "safe_wall", WallSafe);
            Variant("DoorKeypad", "keypad_prison", DoorKeypad);
            Variant("SteelDoor", "door_steel", SteelDoor);
            Variant("WoodenDoor", "door_wood", WoodenDoor);
            Variant("VentCover", "vent_cover", go => go.GetComponentInChildren<Door>(true).label = "Vent cover");
            Variant("Drawers", "cabinet_drawers", _ => { });
            Variant("Flashlight", "flashlight", _ => { });

            AssetDatabase.SaveAssets();
            Debug.Log($"[PropVariantBuilder] Prop variants built in {VariantFolder}.");
        }

        // Our variant for each kit prop, by the kit prefab's name.
        static readonly (string variant, string kit)[] Pairs =
        {
            ("StaffLocker", "locker_staff"), ("KeyCabinet", "key_cabinet"), ("Chest", "chest_wood"),
            ("WallSafe", "safe_wall"), ("DoorKeypad", "keypad_prison"), ("SteelDoor", "door_steel"),
            ("WoodenDoor", "door_wood"), ("VentCover", "vent_cover"), ("Drawers", "cabinet_drawers"),
            ("Flashlight", "flashlight"),
        };

        // A scene built before the variants existed still holds the kit's props, with the kit's codes.
        // This swaps each one for our variant in the open scene, keeping where it stands.
        // Not for RecordsRoomB: Leslie's Build Room B picks the variants up there.
        [MenuItem("Last Shift/Swap In Prop Variants (open scene)")]
        static void SwapInOpenScene()
        {
            if (EditorApplication.isPlaying)
            {
                EditorUtility.DisplayDialog("Stop Play mode first", "Scenes can't be changed while the game is running.", "OK");
                return;
            }
            var scene = EditorSceneManager.GetActiveScene();
            if (scene.path.EndsWith("RecordsRoomB.unity"))
            {
                EditorUtility.DisplayDialog("Leslie's scene",
                    "RecordsRoomB belongs to Leslie. Open your sandbox scene instead, or ask Leslie to run Last Shift > Build Room B.", "OK");
                return;
            }
            Swap(scene);
        }

        // Opens Gama's sandbox and swaps the props there. Also runs from the command line (-executeMethod).
        [MenuItem("Last Shift/Swap In Prop Variants (Gama sandbox)")]
        public static void SwapInSandbox()
        {
            if (EditorApplication.isPlaying || !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                return;
            Swap(EditorSceneManager.OpenScene(SandboxScene, OpenSceneMode.Single));
        }

        const string SandboxScene = "Assets/_LastShift/Scenes/Sandbox/Gama.unity";

        static void Swap(UnityEngine.SceneManagement.Scene scene)
        {
            int swapped = 0;
            foreach (var (variantName, kitName) in Pairs)
            {
                var variant = AssetDatabase.LoadAssetAtPath<GameObject>($"{VariantFolder}/{variantName}.prefab");
                var kitPath = $"{KitFolder}/Prefabs/{kitName}.prefab";
                if (variant == null)
                    continue;
                foreach (var go in scene.GetRootGameObjects().SelectMany(r => r.GetComponentsInChildren<Transform>(true)).Select(t => t.gameObject).ToList())
                {
                    if (!PrefabUtility.IsOutermostPrefabInstanceRoot(go) || PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(go) != kitPath)
                        continue;
                    PrefabUtility.ReplacePrefabAssetOfPrefabInstance(go, variant, InteractionMode.UserAction);
                    swapped++;
                }
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log($"[PropVariantBuilder] Swapped {swapped} kit props for our variants in {scene.name}.");
        }

        // Creates the variant the first time, then applies the settings to it.
        static void Variant(string name, string kitName, Action<GameObject> setup)
        {
            string path = $"{VariantFolder}/{name}.prefab";
            if (AssetDatabase.LoadAssetAtPath<GameObject>(path) == null)
            {
                var kit = AssetDatabase.LoadAssetAtPath<GameObject>($"{KitFolder}/Prefabs/{kitName}.prefab");
                if (kit == null)
                {
                    Debug.LogWarning($"[PropVariantBuilder] Kit prefab {kitName} not found, skipped {name}.");
                    return;
                }
                var temp = (GameObject)PrefabUtility.InstantiatePrefab(kit);
                PrefabUtility.SaveAsPrefabAsset(temp, path);
                UnityEngine.Object.DestroyImmediate(temp);
            }

            var root = PrefabUtility.LoadPrefabContents(path);
            try
            {
                setup(root);
                PrefabUtility.SaveAsPrefabAsset(root, path);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        // ---------------------------------------------------------------- the props

        // The kit's locker, cabinet and chest have no lock of their own. A padlock on the same object
        // takes the key, and only then is the stash enabled, so it can be searched. The kit's safe
        // works the same way with its keypad.
        static void KeyLock(GameObject go, string label, string keyId, string flag)
        {
            var stash = go.GetComponentInChildren<Stash>(true);
            stash.label = label;
            stash.enabled = false;

            var padlock = stash.GetComponent<Padlock>();
            if (padlock == null)
                padlock = stash.gameObject.AddComponent<Padlock>();
            padlock.label = label;
            padlock.flag = flag;
            padlock.missingText = "It's locked.";
            padlock.item = FindItem(keyId, label);

            var kitPadlock = AssetDatabase.LoadAssetAtPath<GameObject>($"{KitFolder}/Prefabs/padlock.prefab");
            if (kitPadlock != null)
            {
                var sounds = kitPadlock.GetComponentInChildren<Padlock>(true);
                padlock.onSound = sounds.onSound;
                padlock.refusedSound = sounds.refusedSound;
            }

            ClearListeners(padlock.onSwitchedOn);
            UnityEventTools.AddBoolPersistentListener(padlock.onSwitchedOn, EnabledSetter(stash), true);
        }

        static void WallSafe(GameObject go)
        {
            // The kit's safe already enables its stash once the keypad takes the code.
            var keypad = go.GetComponentInChildren<Keypad>(true);
            keypad.label = "Wall safe";
            keypad.usePad = false;  // its own keys are pressed, in the world and in VR
            // The keypad's open clip holds the safe door shut and would undo the door's swing below.
            keypad.motion.mode = HeyHeyThere.EscapePuzzleKitFree.Motion.Mode.None;
            keypad.code = SafeCode;
            keypad.flag = SafeFlag;
            var stash = go.GetComponentInChildren<Stash>(true);
            stash.label = "Wall safe";
            SwingOutward(stash, "safe_wall_door", 100f, 1f);
        }

        // The kit's open clips turn the key cabinet and locker doors the wrong way (into the prop), and
        // the safe's clip doesn't move its door at all. A plain turn about the hinge replaces them.
        // A door's pivot is its hinge; swinging out means towards the prop's front, its local +Z.
        static void SwingOutward(Openable openable, string doorName, float degrees, float seconds)
        {
            var door = openable.GetComponentsInChildren<Transform>(true).FirstOrDefault(t => t.name == doorName);
            var leaf = door != null ? door.GetComponentInChildren<Renderer>(true) : null;
            if (leaf == null)
            {
                Debug.LogWarning($"[PropVariantBuilder] No {doorName} found on {openable.name}, its door keeps the kit's motion.");
                return;
            }
            // Leaf to the left of the hinge (-X) swings out with a positive turn about Y, to the right with a negative one.
            bool leafLeftOfHinge = door.InverseTransformPoint(leaf.bounds.center).x < 0f;
            openable.motion.mode = HeyHeyThere.EscapePuzzleKitFree.Motion.Mode.Turn;
            openable.motion.part = door;
            openable.motion.axis = Vector3.up;
            openable.motion.angle = leafLeftOfHinge ? degrees : -degrees;
            openable.motion.seconds = seconds;
        }

        static void DoorKeypad(GameObject go)
        {
            var keypad = go.GetComponentInChildren<Keypad>(true);
            keypad.label = "Door keypad";
            keypad.usePad = false;
            keypad.code = ExitCode;
            keypad.flag = ExitCodeFlag;
        }

        // The way out: unlocks once the door keypad takes its code, and opening it ends the game.
        static void SteelDoor(GameObject go)
        {
            var door = go.GetComponentInChildren<Door>(true);
            door.label = "Steel door";
            door.lockState = Door.Lock.Locked;
            door.unlockFlag = ExitCodeFlag;
            door.lockedText = "Locked. The keypad next to it wants a code.";
            door.endsGame = true;
        }

        // The storage door is a dead end and never opens.
        static void WoodenDoor(GameObject go)
        {
            var door = go.GetComponentInChildren<Door>(true);
            door.label = "Storage door";
            door.lockState = Door.Lock.Jammed;
            door.jammedText = "It won't budge.";
        }

        // ---------------------------------------------------------------- helpers

        static Item FindItem(string id, string lockName)
        {
            if (string.IsNullOrEmpty(id))
            {
                Debug.LogWarning($"[PropVariantBuilder] {lockName} has no key set yet, so it opens without one.");
                return null;
            }
            var item = AssetDatabase.FindAssets("t:Item", ItemFolders)
                .Select(guid => AssetDatabase.LoadAssetAtPath<Item>(AssetDatabase.GUIDToAssetPath(guid)))
                .FirstOrDefault(i => i != null && i.Id == id);
            if (item == null)
                Debug.LogWarning($"[PropVariantBuilder] No item with id {id} yet, so {lockName} opens without a key.");
            return item;
        }

        static UnityAction<bool> EnabledSetter(Behaviour target) =>
            (UnityAction<bool>)Delegate.CreateDelegate(typeof(UnityAction<bool>), target,
                typeof(Behaviour).GetProperty(nameof(Behaviour.enabled)).GetSetMethod());

        static void ClearListeners(UnityEventBase unityEvent)
        {
            for (int i = unityEvent.GetPersistentEventCount() - 1; i >= 0; i--)
                UnityEventTools.RemovePersistentListener(unityEvent, i);
        }
    }
}
