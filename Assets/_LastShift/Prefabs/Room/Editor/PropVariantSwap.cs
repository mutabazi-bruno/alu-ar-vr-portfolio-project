using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace LastShift.EditorTools
{
    // Swaps the kit stand-ins in Records Room B for Gama's prop variants (Prefabs/Props), keeping each
    // prop's name, place and anything we attached to it (hook numbers, the STORAGE sign).
    // Only the props listed below; WallSafe and DoorKeypad wait for Brian's keypad screens.
    // Safe to run again: props already using Gama's variant are left alone.
    public static class PropVariantSwap
    {
        const string ScenePath = "Assets/_LastShift/Scenes/RecordsRoomB.unity";
        const string VariantFolder = "Assets/_LastShift/Prefabs/Props";

        static readonly string[] Ready =
        {
            "StaffLocker", "KeyCabinet", "Chest", "Drawers", "VentCover", "Flashlight", "SteelDoor", "WoodenDoor",
        };

        [MenuItem("Last Shift/Swap In Prop Variants (Room B)")]
        public static void SwapRoomB()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            var props = GameObject.Find("Room/Props");
            if (props == null) { Debug.LogWarning("[PropVariantSwap] No Room/Props in Room B."); return; }

            var report = new List<string>();
            foreach (var name in Ready)
            {
                var old = props.transform.Find(name);
                var variant = AssetDatabase.LoadAssetAtPath<GameObject>($"{VariantFolder}/{name}.prefab");
                if (old == null || variant == null) { report.Add($"{name}: skipped (missing {(old == null ? "in scene" : "variant")})"); continue; }
                if (PrefabUtility.GetCorrespondingObjectFromSource(old.gameObject) == variant) { report.Add($"{name}: already Gama's"); continue; }

                var fresh = (GameObject)PrefabUtility.InstantiatePrefab(variant, old.parent);
                fresh.name = name;
                fresh.transform.SetSiblingIndex(old.GetSiblingIndex());
                fresh.transform.localPosition = old.localPosition;
                fresh.transform.localRotation = old.localRotation;
                fresh.transform.localScale = old.localScale;

                // Keep what we added on top of the kit prop (signs, decals), at the same spot in the world.
                var added = old.Cast<Transform>().Where(c => PrefabUtility.IsAddedGameObjectOverride(c.gameObject)).ToList();
                foreach (var child in added) child.SetParent(fresh.transform, true);

                Object.DestroyImmediate(old.gameObject);
                report.Add($"{name}: swapped{(added.Count > 0 ? $" (kept {string.Join(", ", added.Select(a => a.name))})" : "")}");
            }

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("[PropVariantSwap] Room B:\n" + string.Join("\n", report));
        }
    }
}
