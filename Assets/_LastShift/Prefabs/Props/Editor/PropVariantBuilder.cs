using System;
using System.Linq;
using HeyHeyThere.EscapePuzzleKitFree;
using LastShift.Props;
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

        // Where Room B's steel door leads.
        const string NextRoom = "RoomC";
        const string NextRoomEntrance = "FromRoomB";

        // Item ids of the keys.
        const string LockerKey = "key_brass";
        const string KeyCabinetKey = "small_steel_key";
        const string ChestKey = "chest_key_hook6";

        // Habimana's lines and the objective after each, from the story and script v2 running order.
        // The subtitle text is exactly the recorded line; the intercom adds the voice later.
        const string Speaker = "HABIMANA";
        const string H01 = "Ah. Someone stayed late. ... I'm sorry about the door. Actually, no, I'm not, I locked it. " +
            "My name is Joseph Habimana. I've kept this archive for forty-seven years, and tonight you're going to keep it with me. " +
            "Everything you need is in this room, if you look properly. There's a torch on the workbench. You'll want it.";
        const string H02 ="There you go. Now you see what I see. Mind the battery. I never do.";
        const string H03 ="My locker. Forty-seven years and that's all I kept. Read the note. Slowly.";
        const string H04 = "Good. You read the boxes, not just the numbers. Most people don't.";
        const string H06 = "Hook six. I never could remember it either. One more and you're out. Or so you'd think.";

        // Flags set when each lock opens. Doors and story beats wait on these.
        // Don't rename after Day 3: saves store them.
        const string ExitCodeFlag = "exit_code_entered";
        const string SafeFlag = "wall_safe_opened";
        const string LockerFlag = "staff_locker_unlocked";
        const string KeyCabinetFlag = "key_cabinet_unlocked";
        const string ChestFlag = "chest_unlocked";

        // Set once each container has been searched (its contents given).
        const string DrawerSearched = "top_drawer_searched";
        const string VentSearched = "vent_searched";
        const string LockerSearched = "staff_locker_searched";
        const string SafeSearched = "wall_safe_searched";
        const string KeyCabinetSearched = "key_cabinet_searched";
        const string ChestSearched = "chest_searched";

        [MenuItem("Last Shift/Build Prop Variants")]
        public static void Build()
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
                var stash = go.GetComponentInChildren<Stash>(true);
                KeyLock(go, "Staff locker", LockerKey, LockerFlag);
                SwingOutward(stash, "locker_staff_door", 105f, 0.8f);
                Fill(stash, LockerSearched, new[] { "staff_badge", "note_2" },
                    Beat(go, "Solve the caretaker's riddle. Check the archive boxes.", H03));
            });
            Variant("KeyCabinet", "key_cabinet", go =>
            {
                var stash = go.GetComponentInChildren<Stash>(true);
                KeyLock(go, "Key cabinet", KeyCabinetKey, KeyCabinetFlag);
                SwingOutward(stash, "key_cabinet_door", 110f, 0.7f);
                Fill(stash, KeyCabinetSearched, new[] { "hook_key_2", "chest_key_hook6", "hook_key_7" },
                    Beat(go, "Which hook did he mean? Check his badge."));
            });
            Variant("Chest", "chest_wood", go =>
            {
                KeyLock(go, "Chest", ChestKey, ChestFlag);
                Fill(go.GetComponentInChildren<Stash>(true), ChestSearched, new[] { "note_3" },
                    Beat(go, "Find the time he signed in.", H06));
            });
            Variant("WallSafe", "safe_wall", WallSafe);
            Variant("DoorKeypad", "keypad_prison", DoorKeypad);
            Variant("SteelDoor", "door_steel", SteelDoor);
            Variant("WoodenDoor", "door_wood", WoodenDoor);
            Variant("VentCover", "vent_cover", go =>
            {
                var vent = go.GetComponentInChildren<Door>(true);
                vent.label = "Vent cover";
                Fill(vent, VentSearched, new[] { "battery" }, Beat(go, "Open the caretaker's locker."));
            });
            Variant("Drawers", "cabinet_drawers", go =>
                Fill(TopDrawer(go), DrawerSearched, new[] { "note_1", "key_brass" }, Beat(go, "Find where the air comes in.")));
            Variant("Flashlight", "flashlight", go =>
            {
                // Picking up the torch: H02, the next objective, and the hint for its key.
                // The VR HUD shows the hint's key as X or A.
                var beat = Beat(go, "Search the room.", H02);
                beat.hintKey = "F";
                beat.hint = "TORCH";
                go.GetComponentInChildren<Pickup>(true).beat = beat;
            });

            StoryBeats();

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
            Fill(stash, SafeSearched, new[] { "small_steel_key" }, Beat(go, "Open the key cabinet.", H04));
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

            // No line here: the script wants silence, so the player thinks it's over.
            var beat = Beat(go, "Get out.");
            RemoveListenersTo(keypad.onSwitchedOn, beat);
            UnityEventTools.AddVoidPersistentListener(keypad.onSwitchedOn, beat.Fire);
        }

        // Room B's way out: unlocks once the door keypad takes its code, and leads on to Room C.
        // The game ends at Room C's exit door, not here.
        static void SteelDoor(GameObject go)
        {
            var door = go.GetComponentInChildren<Door>(true);
            door.label = "Steel door";
            door.lockState = Door.Lock.Locked;
            door.unlockFlag = ExitCodeFlag;
            door.lockedText = "Locked. The keypad next to it wants a code.";
            door.endsGame = false;
            door.leadsTo = NextRoom;
            door.entrance = NextRoomEntrance;
        }

        // The storage door is a dead end and never opens; its line says why.
        static void WoodenDoor(GameObject go)
        {
            var door = go.GetComponentInChildren<Door>(true);
            door.label = "Storage door";
            door.lockState = Door.Lock.Jammed;
            door.jammedText = "STORAGE. Habimana's key only.";
        }

        // ---------------------------------------------------------------- contents and beats

        // What a container gives the first time it opens. Its Stash keeps no item of its own, so all
        // of them come from Contents together, with one "Found: ..." line.
        static void Fill(Openable container, string searchedFlag, string[] itemIds, StoryBeat beat)
        {
            if (container is Stash stash)
                stash.item = null;
            var contents = container.GetComponent<Contents>();
            if (contents == null)
                contents = container.gameObject.AddComponent<Contents>();
            contents.container = container;
            contents.searchedFlag = searchedFlag;
            contents.items = itemIds.Select(id => FindItem(id, container.name)).Where(i => i != null).ToArray();
            contents.beat = beat;
        }

        // The prop's story beat, on a child named StoryBeat: fired by its container, once.
        static StoryBeat Beat(GameObject go, string objective, string line = "")
        {
            var child = go.transform.Find("StoryBeat");
            if (child == null)
            {
                child = new GameObject("StoryBeat").transform;
                child.SetParent(go.transform, false);
            }
            var beat = child.GetComponent<StoryBeat>();
            if (beat == null)
                beat = child.gameObject.AddComponent<StoryBeat>();
            beat.when = StoryBeat.When.Fired;
            beat.once = true;
            beat.objective = objective;
            beat.subtitle = line;
            beat.speaker = line != "" ? Speaker : "";
            return beat;
        }

        // ---------------------------------------------------------------- story beats placed in the rooms

        const string StoryFolder = VariantFolder + "/Story";

        // Beats that belong to a room rather than a prop. Each is a prefab for Leslie to drop into the
        // scene, so the beat's text stays here and nobody has to edit it inside the scene.
        static void StoryBeats()
        {
            if (!AssetDatabase.IsValidFolder(StoryFolder))
                AssetDatabase.CreateFolder(VariantFolder, "Story");

            // Records Room B starts: Habimana's opening line, then the first objective.
            RoomBeat("OpeningBeat", StoryBeat.When.OnStart, "Find a light.", H01);
        }

        static void RoomBeat(string name, StoryBeat.When when, string objective, string line, string setsFlag = "")
        {
            string path = $"{StoryFolder}/{name}.prefab";
            bool exists = AssetDatabase.LoadAssetAtPath<GameObject>(path) != null;
            var root = exists ? PrefabUtility.LoadPrefabContents(path) : new GameObject(name);
            try
            {
                var beat = root.GetComponent<StoryBeat>();
                if (beat == null)
                    beat = root.AddComponent<StoryBeat>();
                beat.when = when;
                beat.once = true;
                beat.objective = objective;
                beat.subtitle = line;
                beat.speaker = line != "" ? Speaker : "";
                beat.setsFlag = setsFlag;
                PrefabUtility.SaveAsPrefabAsset(root, path);
            }
            finally
            {
                if (exists)
                    PrefabUtility.UnloadPrefabContents(root);
                else
                    UnityEngine.Object.DestroyImmediate(root);
            }
        }

        // The drawers have no names on their parts, so the top one is the highest.
        static Stash TopDrawer(GameObject go) =>
            go.GetComponentsInChildren<Stash>(true)
                .OrderByDescending(s => s.GetComponentsInChildren<Renderer>(true).Select(r => r.bounds.center.y).DefaultIfEmpty(s.transform.position.y).Max())
                .First();

        static void RemoveListenersTo(UnityEventBase unityEvent, UnityEngine.Object target)
        {
            for (int i = unityEvent.GetPersistentEventCount() - 1; i >= 0; i--)
                if (unityEvent.GetPersistentTarget(i) == target)
                    UnityEventTools.RemovePersistentListener(unityEvent, i);
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
