using System;
using System.Linq;
using HeyHeyThere.EscapePuzzleKitFree;
using LastShift.Narration;
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
        const string LockerKey = "brass_key";  // ours: its tag carries the safe hint
        const string KeyCabinetKey = "small_steel_key";
        const string ChestKey = "chest_key_hook6";

        // Habimana's lines, from the story and script v2 running order: Brian's VoiceLine assets, which
        // hold the recording and its timed subtitle. Each beat plays its line through an IntercomCue.
        const string VoiceFolder = "Assets/_LastShift/Audio/Voice/Lines";
        const string H01 = "H01_Opening", H02 = "H02_Torch", H03 = "H03_Locker", H04 = "H04_Safe", H06 = "H06_Chest";
        const string H07 = "H07_RoomC", H08 = "H08_SignIn", H09 = "H09_Mirror", H11 = "H11_Solved", H12 = "H12_Cap", H13 = "H13_Ending";

        // Room C's flags: Bruno's clothes puzzle sets the first two, the exit beat sets exit_unlocked.
        const string RoomCEnteredFlag = "roomc_entered";
        const string ClothesSolvedFlag = "clothes_solved";
        const string CapWornFlag = "cap_worn";
        const string ExitUnlockedFlag = "exit_unlocked";

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
            {
                // The brass key and Note 1 lie in the top drawer, to be taken one at a time.
                var top = TopDrawer(go);
                foreach (var drawer in go.GetComponentsInChildren<Stash>(true))
                    FrontOnlyCollider(drawer);  // so a look inside one drawer can't reach the one below
                // The kit finds a drawer's clip on the first Animation inside it, and the models lying in it
                // bring their own, so the top drawer slides by itself instead of playing the clip.
                SlideOut(top);
                LyingPickup(top, "BrassKey", "key_brass", "brass_key", -0.08f);
                LyingPickup(top, "Note1", "note", "note_1", 0.08f);
                Fill(top, DrawerSearched, new string[0], Beat(go, "Find where the air comes in."));
            });
            Variant("ExitDoor", "door_steel", ExitDoor);

            // Habimana's clothes: Bruno's coat, shirt and trousers (his scripts untouched) wearing the
            // After Dark garments instead of their placeholder boxes. The scarf and cap have no match there.
            Variant("HabimanaCoat", $"{ClothesFolder}/Coat.prefab", go => Dress(go, "shirt-0002"));
            Variant("HabimanaShirt", $"{ClothesFolder}/Shirt.prefab", go => Dress(go, "shirt-0003"));
            Variant("HabimanaTrousers", $"{ClothesFolder}/Trousers.prefab", go => Dress(go, "pants-0002"));

            // Room B: the one archive box without a proper label, with his hint for the safe.
            Variant("OddBox", "cardboard_box", go =>
            {
                var look = go.GetComponent<Inspectable>();
                if (look == null)
                    look = go.AddComponent<Inspectable>();
                look.label = "Unlabelled box";
                look.hold = false;
                look.text = "No label on this one, only his handwriting: \"The safe remembers my years. " +
                    "Read the years on the boxes and take the last digit of each.\"";
            });

            // Room C: the wardrobe holds the cap and opens once the clothes are hung right.
            Variant("WardrobeLocked", "Assets/_LastShift/Prefabs/Room/Wardrobe.prefab", go =>
            {
                var stash = go.GetComponentInChildren<Stash>(true);
                stash.label = "Wardrobe";
                stash.item = null;
                ShutUntil(stash, ClothesSolvedFlag, "Wardrobe", "Locked. Not until his clothes are where they belong.");
                // The kit locker's body is one solid box, so the look lands on it and never on the cap
                // inside. Kept to the back panel; the door has a collider of its own.
                foreach (var body in go.GetComponentsInChildren<BoxCollider>(true).Where(b => b.size.z > 0.3f))
                {
                    const float back = 0.06f;
                    var min = body.center.z - body.size.z / 2f;
                    body.size = new Vector3(body.size.x, body.size.y, back);
                    body.center = new Vector3(body.center.x, body.center.y, min + back / 2f);
                }
            });

            // Room C's dressing: the same props as Room B, kept shut so the wardrobe and the rack stand out.
            Variant("RoomCLocker", "locker_staff", go => ShutForGood(go, "Staff locker"));
            Variant("RoomCKeyCabinet", "key_cabinet", go => ShutForGood(go, "Key cabinet"));
            Variant("RoomCChest", "chest_wood", go => ShutForGood(go, "Chest"));
            Variant("RoomCDrawers", "cabinet_drawers", go => ShutForGood(go, "Drawer"));
            Variant("RoomCSafe", "safe_wall", go => ShutForGood(go, "Wall safe"));
            Variant("Flashlight", "flashlight", go =>
            {
                // Taking the torch switches it off, beat and all, so its beat lives in the opening beat
                // instead (see StoryBeats) and waits for the torch to be carried.
                var pickup = go.GetComponentInChildren<Pickup>(true);
                pickup.beat = null;
                pickup.examine = true;  // held up as it's taken, like everything else the player finds
                var old = go.transform.Find("StoryBeat");
                if (old != null)
                    UnityEngine.Object.DestroyImmediate(old.gameObject);
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
                // A kit prefab's name, or the full path of one of ours (Leslie's wardrobe).
                var source = kitName.Contains("/") ? kitName : $"{KitFolder}/Prefabs/{kitName}.prefab";
                var kit = AssetDatabase.LoadAssetAtPath<GameObject>(source);
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
            keypad.wrongText = "Wrong code. The years on the boxes, maybe?";
            // The shift card shows his last sign-in, 22:07: the door code, so the card is the way out.
            Fill(stash, SafeSearched, new[] { "shift_card", "small_steel_key" },
                Beat(go, "The card is your way out. Study it well.", H04),
                "Inside: Habimana's shift card and a small steel key. The card is your way out. Study it well.");
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

        // Room C's way out, the real end of the game: it unlocks once the exit beat sets exit_unlocked
        // (clothes hung and cap on), and opening it plays Habimana's last line before the end screen.
        static void ExitDoor(GameObject go)
        {
            var door = go.GetComponentInChildren<Door>(true);
            door.label = "Exit";
            door.lockState = Door.Lock.Locked;
            door.unlockFlag = ExitUnlockedFlag;
            door.lockedText = "Locked. Something here still isn't right.";
            door.endsGame = true;
            door.leadsTo = "";

            var beat = Beat(go, "", H13);
            RemoveListenersTo(door.onOpened, beat);
            UnityEventTools.AddVoidPersistentListener(door.onOpened, beat.Fire);
        }

        // The storage door is a dead end and never opens; its line says why.
        static void WoodenDoor(GameObject go)
        {
            var door = go.GetComponentInChildren<Door>(true);
            door.label = "Storage door";
            door.lockState = Door.Lock.Jammed;
            door.jammedText = "STORAGE. Habimana's key only.";
        }

        // A container held shut by a FlagLock until the flag is set.
        static void ShutUntil(Behaviour container, string flag, string label, string lockedText)
        {
            container.enabled = false;
            var flagLock = container.GetComponent<FlagLock>();
            if (flagLock == null)
                flagLock = container.gameObject.AddComponent<FlagLock>();
            flagLock.flag = flag;
            flagLock.container = container;
            flagLock.label = label;
            flagLock.lockedText = lockedText;
        }

        // Every container and keypad on the prop stays shut and says so.
        static void ShutForGood(GameObject go, string label)
        {
            foreach (var keypad in go.GetComponentsInChildren<Keypad>(true))
                keypad.enabled = false;
            foreach (var stash in go.GetComponentsInChildren<Stash>(true))
            {
                stash.item = null;
                ShutUntil(stash, "", label, "It won't open.");
            }
        }

        // ---------------------------------------------------------------- contents and beats

        // What a container gives the first time it opens. Its Stash keeps no item of its own, so all
        // of them come from Contents together, with one "Found: ..." line.
        static void Fill(Openable container, string searchedFlag, string[] itemIds, StoryBeat beat, string message = "")
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
            contents.message = message;
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
            Voice(beat, line);
            return beat;
        }

        // The beat plays its voice line through the intercom, which shows the subtitle in time with the
        // recording, so the beat keeps no subtitle or hint of its own (they'd show twice or too early).
        // A hint goes on the cue instead, shown once the line has been said.
        static void Voice(StoryBeat beat, string voiceLine, string hintKey = "", string hint = "")
        {
            beat.subtitle = "";
            beat.speaker = "";
            beat.hintKey = "";
            beat.hint = "";
            if (voiceLine == "")
                return;
            var line = AssetDatabase.LoadAssetAtPath<VoiceLine>($"{VoiceFolder}/{voiceLine}.asset");
            if (line == null)
            {
                Debug.LogWarning($"[PropVariantBuilder] No voice line {voiceLine} in {VoiceFolder}.");
                return;
            }
            var cue = beat.GetComponent<IntercomCue>();
            if (cue == null)
                cue = beat.gameObject.AddComponent<IntercomCue>();
            cue.line = line;
            cue.when = IntercomCue.When.WhenCalled;
            cue.once = true;
            cue.hintKey = hintKey;
            cue.hint = hint;
            RemoveListenersTo(beat.onFired, cue);
            UnityEventTools.AddVoidPersistentListener(beat.onFired, cue.Play);
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
            // Once the torch is carried: H02, the next objective and the torch's key. It sits in the opening
            // beat, which is already in Room B, because a beat on the torch is switched off with it.
            TorchBeat();

            // Room C. The two OnEnter beats are trigger boxes: place and size them where the player walks.
            RoomBeat("RoomCEntranceBeat", StoryBeat.When.OnEnter, "Learn how he keeps his room.", H07, RoomCEnteredFlag);
            RoomBeat("MirrorBeat", StoryBeat.When.OnEnter, "Hang his clothes the way he does.", H09);
            // Fired by Room C's sign-in sheet (an Inspectable's beat); the objective stays as it was.
            RoomBeat("RoomCSignInBeat", StoryBeat.When.Fired, "", H08);
            // Clothes hung right, then the cap on (either order): the exit unlocks and the dawn shows.
            RoomBeat("ClothesSolvedBeat", StoryBeat.When.Fired, "Put on his cap.", H11, gate: new[] { ClothesSolvedFlag });
            RoomBeat("ExitUnlockBeat", StoryBeat.When.Fired, "Go home.", H12, ExitUnlockedFlag,
                gate: new[] { ClothesSolvedFlag, CapWornFlag });
        }

        static void TorchBeat()
        {
            string path = $"{StoryFolder}/OpeningBeat.prefab";
            var root = PrefabUtility.LoadPrefabContents(path);
            try
            {
                var child = root.transform.Find("TorchBeat");
                if (child == null)
                {
                    child = new GameObject("TorchBeat").transform;
                    child.SetParent(root.transform, false);
                }
                var beat = child.GetComponent<StoryBeat>();
                if (beat == null)
                    beat = child.gameObject.AddComponent<StoryBeat>();
                beat.when = StoryBeat.When.Fired;
                beat.once = true;
                beat.objective = "Search the room.";
                Voice(beat, H02, "F", "TORCH");  // the VR HUD shows the key as X or A
                var gate = beat.GetComponent<FlagGate>();
                if (gate == null)
                    gate = beat.gameObject.AddComponent<FlagGate>();
                gate.items = new[] { "flashlight" };
                gate.beat = beat;
                PrefabUtility.SaveAsPrefabAsset(root, path);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        static void RoomBeat(string name, StoryBeat.When when, string objective, string line, string setsFlag = "",
            string[] gate = null)
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
                beat.setsFlag = setsFlag;
                Voice(beat, line);

                if (when == StoryBeat.When.OnEnter && root.GetComponent<BoxCollider>() == null)
                {
                    // A doorway-sized box to start with; resize it in the scene.
                    var box = root.AddComponent<BoxCollider>();
                    box.isTrigger = true;
                    box.size = new Vector3(2f, 2.5f, 0.6f);
                    box.center = new Vector3(0f, 1.25f, 0f);
                }

                if (gate != null)
                {
                    var flagGate = root.GetComponent<FlagGate>();
                    if (flagGate == null)
                        flagGate = root.AddComponent<FlagGate>();
                    flagGate.flags = gate;
                    flagGate.beat = beat;
                }
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

        // ---------------------------------------------------------------- Habimana's clothes

        const string ClothesFolder = "Assets/_LastShift/Prefabs/Clothes";
        // Asset Store pack, imported by each of us (like the kit), never committed.
        const string GarmentFolder = "Assets/Genies/AfterDark";

        // Hides the placeholder box and puts the garment where it was, sized to it. The root keeps
        // Bruno's collider, grab and hanging scripts, so the piece behaves exactly as before.
        static void Dress(GameObject go, string garment)
        {
            var model = AssetDatabase.LoadAssetAtPath<GameObject>($"{GarmentFolder}/{garment}-afterDark/{garment}-afterDark_model.prefab");
            if (model == null)
            {
                Debug.LogWarning($"[PropVariantBuilder] {garment} not found: import the After Dark clothing pack first.");
                return;
            }
            var box = go.transform.Find("Model");
            var look = go.transform.Find("Look");
            if (look == null)
            {
                look = ((GameObject)PrefabUtility.InstantiatePrefab(model, go.transform)).transform;
                look.name = "Look";
            }
            // the garment's own colliders would catch the look and the hooks instead of Bruno's box
            foreach (var c in look.GetComponentsInChildren<Collider>(true))
                UnityEngine.Object.DestroyImmediate(c);
            if (box == null)
                return;

            var placeholder = box.GetComponent<Renderer>();
            var target = placeholder.bounds;
            placeholder.enabled = false;
            var fallback = go.GetComponent<LookFallback>();
            if (fallback == null)
                fallback = go.AddComponent<LookFallback>();
            fallback.look = look;
            fallback.placeholder = placeholder;

            look.localPosition = Vector3.zero;
            look.localRotation = Quaternion.identity;
            look.localScale = Vector3.one;
            var size = RendererBounds(look.gameObject).size;
            // as tall as the placeholder, but sleeves held out no wider than a hanger and a half
            float scale = Mathf.Min(target.size.y / size.y, target.size.x * 1.5f / size.x);
            look.localScale = Vector3.one * scale;
            look.position += target.center - RendererBounds(look.gameObject).center;
        }

        static Bounds RendererBounds(GameObject go)
        {
            var rs = go.GetComponentsInChildren<Renderer>(true);
            var b = rs[0].bounds;
            foreach (var r in rs) b.Encapsulate(r.bounds);
            return b;
        }

        // A kit drawer's box fills the whole drawer, so the look would land on the drawer and never on
        // what lies inside. Kept to the front panel, the drawer still opens and closes from the front.
        // Out along its front (local +Z) by most of its depth, and back.
        static void SlideOut(Stash drawer)
        {
            const float depth = 0.63f;  // the kit drawer's depth, from its original box
            drawer.motion.mode = HeyHeyThere.EscapePuzzleKitFree.Motion.Mode.Slide;
            drawer.motion.part = drawer.transform;
            drawer.motion.offset = new Vector3(0f, 0f, depth * 0.7f);
            drawer.motion.seconds = 0.45f;
        }

        static void FrontOnlyCollider(Stash drawer)
        {
            var box = drawer.GetComponent<BoxCollider>();
            if (box == null)
                return;
            const float thickness = 0.04f;
            var front = box.center.z + box.size.z / 2f;  // the drawer's front is its local +Z
            box.size = new Vector3(box.size.x, box.size.y, thickness);
            box.center = new Vector3(box.center.x, box.center.y, front - thickness / 2f);
        }

        // A kit prop lying in a drawer as a pickup of one of our items, moving with the drawer.
        // Taken with E, it's held up to look at (its tag, its text), like any kit pickup.
        static void LyingPickup(Stash drawer, string name, string kitPrefab, string itemId, float sideways)
        {
            var existing = drawer.transform.Find(name);
            var go = existing != null ? existing.gameObject
                : (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>($"{KitFolder}/Prefabs/{kitPrefab}.prefab"), drawer.transform);
            go.name = name;

            // Our Pickup only: the kit note is an Inspectable, the kit key a pickup of the kit's item.
            foreach (var other in go.GetComponents<Interactable>().Where(i => !(i is Pickup)))
                UnityEngine.Object.DestroyImmediate(other);
            var pickup = go.GetComponent<Pickup>();
            if (pickup == null)
                pickup = go.AddComponent<Pickup>();
            pickup.item = FindItem(itemId, name);
            pickup.examine = true;
            pickup.label = pickup.item != null ? pickup.item.title : name;

            // On the drawer's floor, side by side, at its middle front to back.
            var inside = drawer.GetComponent<BoxCollider>();
            var renderers = drawer.GetComponentsInChildren<Renderer>(true).Where(r => !r.transform.IsChildOf(go.transform)).ToArray();
            var b = renderers[0].bounds;
            foreach (var r in renderers) b.Encapsulate(r.bounds);
            var floor = drawer.transform.InverseTransformPoint(new Vector3(b.center.x, b.min.y, b.center.z));
            var middleZ = inside != null ? inside.center.z - 0.25f : floor.z;
            go.transform.localPosition = new Vector3(floor.x + sideways, floor.y + 0.03f, middleZ);
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
