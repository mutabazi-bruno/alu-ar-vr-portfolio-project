using LastShift.Bedroom;
using UnityEngine;
using UnityEngine.Rendering;

namespace LastShift.Props
{
    /// <summary>
    /// Shows the clothes piece the player is holding until it's hung: low in front of the view, and
    /// worn by the player's figure in Habimana's mirror (the coat or shirt on its chest, the scarf at
    /// its neck, the trousers below). The clothes puzzle hides a piece while it's held, so without
    /// this the player can't see what they carry. Both are look-only copies (meshes and materials,
    /// none of the piece's scripts). The worn copy shows only to the mirror's camera, like the figure.
    /// It creates itself, so no scene needs it placed.
    /// </summary>
    public class HeldClothesView : MonoBehaviour
    {
        [Tooltip("Where the held copy sits in front of the camera, in metres")]
        public Vector3 offset = new Vector3(0.28f, -0.28f, 0.55f);
        [Tooltip("The held copy's height, in metres")]
        public float height = 0.32f;

        ClothesPiece shown;
        GameObject held, worn;
        Renderer[] wornRenderers = new Renderer[0];

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Create()
        {
            var go = new GameObject("HeldClothesView");
            DontDestroyOnLoad(go);
            go.AddComponent<HeldClothesView>();
        }

        void OnEnable()
        {
            RenderPipelineManager.beginCameraRendering += BeginCamera;
            RenderPipelineManager.endCameraRendering += EndCamera;
        }

        void OnDisable()
        {
            RenderPipelineManager.beginCameraRendering -= BeginCamera;
            RenderPipelineManager.endCameraRendering -= EndCamera;
        }

        void LateUpdate()
        {
            if (ClothesPiece.Held != shown)
            {
                if (held != null) Destroy(held);
                if (worn != null) Destroy(worn);
                wornRenderers = new Renderer[0];
                shown = ClothesPiece.Held;
                if (shown != null)
                {
                    held = MakeCopy(shown, out var size);
                    held.transform.localScale = Vector3.one * (height / Mathf.Max(0.001f, size.y));
                    Wear(shown);
                }
            }

            // the player's view: the main camera, else the kit's Interactor, which sits on it
            var view = Camera.main != null ? Camera.main.transform
                : HeyHeyThere.EscapePuzzleKitFree.Interactor.Current != null ? HeyHeyThere.EscapePuzzleKitFree.Interactor.Current.transform : null;
            if (held == null || view == null)
                return;
            held.transform.SetPositionAndRotation(
                view.TransformPoint(offset),
                view.rotation * Quaternion.Euler(0f, 180f, 0f));  // its front towards the player
        }

        // ---------------------------------------------------------------- the mirror's figure

        void Wear(ClothesPiece piece)
        {
            var mirror = FindAnyObjectByType<CapMirror>();
            if (mirror == null || mirror.figure == null)
                return;

            worn = MakeCopy(piece, out var size);
            if (!FitOnBones(mirror.figure, piece.pieceId, size, out var centre, out var scale)
                && !FitOnParts(mirror.figure, piece.pieceId, size, out centre, out scale))
            {
                Destroy(worn);
                return;
            }
            worn.transform.SetParent(mirror.figure, false);
            worn.transform.localRotation = Quaternion.identity;  // facing the way the figure faces
            worn.transform.localScale = Vector3.one * scale / Mathf.Max(0.001f, mirror.figure.lossyScale.y);
            worn.transform.position = centre;
            wornRenderers = worn.GetComponentsInChildren<Renderer>();
            foreach (var r in wornRenderers)
                r.enabled = false;
        }

        // The mirror's character: placed by its skeleton (pelvis, neck, upper arms, feet).
        static bool FitOnBones(Transform figure, string pieceId, Vector3 size, out Vector3 centre, out float scale)
        {
            centre = Vector3.zero;
            scale = 1f;
            var pelvis = Bone(figure, "pelvis", "hips");
            var neck = Bone(figure, "neck_01", "neck");
            if (pelvis == null || neck == null)
                return false;
            var hip = pelvis.position;
            var top = neck.position;
            float torso = Mathf.Max(0.05f, top.y - hip.y);
            var armL = Bone(figure, "upperarm_l");
            var armR = Bone(figure, "upperarm_r");
            float shoulders = armL != null && armR != null ? Vector3.Distance(armL.position, armR.position) : torso * 0.7f;
            var footL = Bone(figure, "foot_l");
            var footR = Bone(figure, "foot_r");
            float legs = footL != null && footR != null ? hip.y - (footL.position.y + footR.position.y) * 0.5f : torso * 1.6f;

            switch (pieceId)
            {
                case "trousers":  // waist to ankles
                    scale = legs * 1.08f / size.y;
                    centre = hip + Vector3.down * (size.y * scale * 0.5f - torso * 0.12f);
                    break;
                case "scarf":  // round the neck, hanging down the chest
                    scale = shoulders * 0.95f / Mathf.Max(size.x, size.z);
                    centre = top + Vector3.down * size.y * scale * 0.35f;
                    break;
                default:  // coat and shirt: collar at the neck, hem below the waist
                    scale = torso * 1.3f / size.y;
                    centre = top + Vector3.down * size.y * scale * 0.42f;
                    break;
            }
            return true;
        }

        static Transform Bone(Transform figure, params string[] names)
        {
            foreach (var t in figure.GetComponentsInChildren<Transform>(true))
                foreach (var n in names)
                    if (string.Equals(t.name, n, System.StringComparison.OrdinalIgnoreCase))
                        return t;
            return null;
        }

        // The mirror's first figure, built from simple parts named Chest and Neck.
        static bool FitOnParts(Transform figure, string pieceId, Vector3 size, out Vector3 centre, out float scale)
        {
            centre = Vector3.zero;
            scale = 1f;
            var chest = PartBounds(figure, "Chest");
            if (chest == null)
                return false;
            var c = chest.Value;
            switch (pieceId)
            {
                case "trousers":  // below the chest, from the waist down
                    scale = c.size.y * 2.2f / size.y;
                    centre = c.center + Vector3.down * (c.extents.y + size.y * scale * 0.5f);
                    break;
                case "scarf":  // round the neck
                    var n = PartBounds(figure, "Neck") ?? c;
                    scale = Mathf.Max(n.size.x, n.size.z) * 1.6f / Mathf.Max(size.x, size.z);
                    centre = n.center + Vector3.down * n.extents.y;
                    break;
                default:  // coat and shirt: over the chest and shoulders
                    scale = c.size.y * 1.35f / size.y;
                    centre = c.center + Vector3.up * c.extents.y * 0.2f;
                    break;
            }
            return true;
        }

        // A part of the figure in world space; its renderer is off between mirror draws, so its mesh is used.
        static Bounds? PartBounds(Transform figure, string name)
        {
            foreach (var t in figure.GetComponentsInChildren<Transform>(true))
            {
                if (t.name != name) continue;
                var filter = t.GetComponent<MeshFilter>();
                if (filter == null || filter.sharedMesh == null) continue;
                var local = filter.sharedMesh.bounds;
                return new Bounds(t.TransformPoint(local.center), Vector3.Scale(local.size, t.lossyScale));
            }
            return null;
        }

        // Like the figure itself, the worn copy is drawn by the mirror's camera only.
        static bool IsMirrorCamera(Camera cam) => cam.targetTexture != null && cam.GetComponentInParent<CapMirror>() != null;

        void BeginCamera(ScriptableRenderContext context, Camera cam) => ShowWorn(IsMirrorCamera(cam));

        void EndCamera(ScriptableRenderContext context, Camera cam) => ShowWorn(false);

        void ShowWorn(bool on)
        {
            foreach (var r in wornRenderers)
                if (r != null)
                    r.enabled = on;
        }

        // ---------------------------------------------------------------- copies

        // The piece's visible meshes, centred on the copy's origin, at the piece's own size.
        static GameObject MakeCopy(ClothesPiece piece, out Vector3 size)
        {
            var root = new GameObject("Copy of " + piece.name);
            var fallback = piece.GetComponent<LookFallback>();
            var pivot = piece.transform;

            foreach (var r in piece.GetComponentsInChildren<Renderer>(true))
            {
                // the placeholder box stays out when the garment is there
                if (fallback != null && r == fallback.placeholder && fallback.HasLook)
                    continue;
                Mesh mesh;
                if (r is SkinnedMeshRenderer skinned)
                {
                    mesh = new Mesh();
                    skinned.BakeMesh(mesh, true);
                }
                else
                {
                    var filter = r.GetComponent<MeshFilter>();
                    if (filter == null || filter.sharedMesh == null)
                        continue;
                    mesh = filter.sharedMesh;
                }
                var part = new GameObject(r.name, typeof(MeshFilter), typeof(MeshRenderer));
                part.transform.SetParent(root.transform, false);
                // where the part sits in the piece, so the copy keeps its shape
                part.transform.localPosition = pivot.InverseTransformPoint(r.transform.position);
                part.transform.localRotation = Quaternion.Inverse(pivot.rotation) * r.transform.rotation;
                part.transform.localScale = r is SkinnedMeshRenderer ? Vector3.one : r.transform.lossyScale;
                part.GetComponent<MeshFilter>().sharedMesh = mesh;
                var renderer = part.GetComponent<MeshRenderer>();
                renderer.sharedMaterials = r.sharedMaterials;
                renderer.shadowCastingMode = ShadowCastingMode.Off;
            }

            size = Vector3.one;
            var parts = root.GetComponentsInChildren<Renderer>();
            if (parts.Length == 0)
                return root;
            // the root is still at the origin, unscaled, so world bounds are its own space
            var b = parts[0].bounds;
            foreach (var p in parts) b.Encapsulate(p.bounds);
            foreach (Transform child in root.transform)
                child.localPosition -= b.center;
            size = b.size;
            return root;
        }
    }
}
