using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace LastShift.EditorTools
{
    // Checks whether the kit props' open/close clips also move the prop itself (its root),
    // which would snap a turned or scaled prop back to the clip's own pose when it opens.
    // Writes Logs/RoomShots/prop_anim_check.txt. Touches no asset and no scene.
    public static class PropAnimationCheck
    {
        [MenuItem("Last Shift/Check Prop Animations")]
        public static void Run()
        {
            var sb = new StringBuilder();
            var paths = AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/EscapePuzzleKitFree/Prefabs" })
                .Select(AssetDatabase.GUIDToAssetPath).OrderBy(p => p);

            foreach (var path in paths)
            {
                var asset = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                var anims = asset.GetComponentsInChildren<Animation>(true);
                if (anims.Length == 0) continue;

                // A copy turned to face a side wall, scaled a little, like a prop placed in a room.
                var copy = (GameObject)Object.Instantiate(asset, new Vector3(50, 0, 50), Quaternion.Euler(0, 90, 0));
                copy.hideFlags = HideFlags.HideAndDontSave;
                copy.transform.localScale = Vector3.one * 1.1f;

                sb.AppendLine($"== {Path.GetFileNameWithoutExtension(path)}");
                foreach (var anim in copy.GetComponentsInChildren<Animation>(true))
                {
                    string animPath = AnimationUtility.CalculateTransformPath(anim.transform, copy.transform);
                    foreach (AnimationState state in anim)
                    {
                        if (state.clip.name.Contains("_key_")) continue;
                        Inner(state);
                    }
                    void Inner(AnimationState state)
                    {
                        var clip = state.clip;
                        var rootCurves = AnimationUtility.GetCurveBindings(clip)
                            .Where(b => b.path == "" && b.type == typeof(Transform))
                            .Select(b => b.propertyName).Distinct().ToArray();

                        var t = anim.transform;
                        var pos = t.position; var rot = t.rotation; var scale = t.lossyScale;
                        var rootPos = copy.transform.position; var rootRot = copy.transform.rotation; var rootScale = copy.transform.localScale;

                        // The rest pose of each animated value, read before sampling.
                        var rest = AnimationUtility.GetCurveBindings(clip).ToDictionary(
                            b => b.path + "/" + b.propertyName,
                            b => AnimationUtility.GetFloatValue(anim.gameObject, b, out var v) ? v.ToString("F3") : "?");

                        clip.SampleAnimation(anim.gameObject, clip.length);
                        bool animMoved = (t.position - pos).sqrMagnitude > 1e-6f || Quaternion.Angle(t.rotation, rot) > 0.5f || (t.lossyScale - scale).sqrMagnitude > 1e-6f;
                        bool rootMoved = (copy.transform.position - rootPos).sqrMagnitude > 1e-6f || Quaternion.Angle(copy.transform.rotation, rootRot) > 0.5f || (copy.transform.localScale - rootScale).sqrMagnitude > 1e-6f;

                        sb.AppendLine($"  clip {clip.name,-24} on '{(animPath == "" ? "(root)" : animPath)}'  " +
                                      $"curves on its own transform: [{string.Join(", ", rootCurves)}]  " +
                                      $"moves the Animation object: {animMoved}  moves the prop root: {rootMoved}");

                        // Every curve with its value at the start and at the end of the clip.
                        foreach (var b in AnimationUtility.GetCurveBindings(clip))
                        {
                            var c = AnimationUtility.GetEditorCurve(clip, b);
                            sb.AppendLine($"      {b.path,-34} {b.propertyName,-26} rest {rest[b.path + "/" + b.propertyName],8}  clip {c.Evaluate(0f),8:F3} -> {c.Evaluate(clip.length),8:F3}");
                        }

                        // Put it back before the next clip.
                        t.SetPositionAndRotation(pos, rot);
                        copy.transform.SetPositionAndRotation(rootPos, rootRot);
                        copy.transform.localScale = rootScale;
                    }
                }
                Object.DestroyImmediate(copy);
            }

            Directory.CreateDirectory("Logs/RoomShots");
            File.WriteAllText("Logs/RoomShots/prop_anim_check.txt", sb.ToString());
            Debug.Log("[PropAnimationCheck] Written to Logs/RoomShots/prop_anim_check.txt");
        }
    }
}
