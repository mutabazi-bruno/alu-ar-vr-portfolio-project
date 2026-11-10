using System;
using System.Linq;
using HeyHeyThere.EscapePuzzleKitFree;
using UnityEditor;
using UnityEditor.Events;
using UnityEngine;
using UnityEngine.Events;

namespace LastShift.EditorTools
{
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

            Variant("StaffLocker", "locker_staff", go => KeyLock(go, "Staff locker", LockerKey, LockerFlag));
            Variant("KeyCabinet", "key_cabinet", go => KeyLock(go, "Key cabinet", KeyCabinetKey, KeyCabinetFlag));
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
            keypad.code = SafeCode;
            keypad.flag = SafeFlag;
            go.GetComponentInChildren<Stash>(true).label = "Wall safe";
        }

        static void DoorKeypad(GameObject go)
        {
            var keypad = go.GetComponentInChildren<Keypad>(true);
            keypad.label = "Door keypad";
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
