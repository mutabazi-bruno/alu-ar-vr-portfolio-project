using HeyHeyThere.EscapePuzzleKitFree;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace LastShift.Bedroom
{
    /// <summary>
    /// Habimana's wall mirror, for checking how the cap fits. A camera mirrored through the glass draws the
    /// room into a texture, and a head-and-shoulders figure follows the player's head. Only the mirror's camera
    /// draws the figure, so the player never sees it directly. The cap on the figure shows once the cap is worn.
    /// Works on desktop and in VR: it follows whichever camera is looking.
    /// </summary>
    public class CapMirror : MonoBehaviour
    {
        [Tooltip("A quad whose front faces the room (local -Z, like Unity's Quad), scaled to the glass size in metres")]
        public MeshRenderer glass;
        [Tooltip("The figure that stands where the player's head is; only the mirror draws it")]
        public Transform figure;
        [Tooltip("Turns up and down with the player's view")]
        public Transform head;
        [Tooltip("On the figure's head, shown once the flag is set")]
        public GameObject cap;
        public string capFlag = "cap_worn";
        [Tooltip("The mirror only draws while the player is this close")]
        public float range = 4.5f;
        public int pixelsPerMetre = 900;

        Camera mirrorCam;
        RenderTexture texture;
        Material glassMaterial;
        Renderer[] figureRenderers = new Renderer[0];
        Camera viewer;
        float nextViewerCheck;
        bool figureOn;
        Quaternion headRest = Quaternion.identity;

        static readonly int BaseMap = Shader.PropertyToID("_BaseMap");
        static readonly int BaseColor = Shader.PropertyToID("_BaseColor");

        void Awake()
        {
            if (glass == null)
            {
                enabled = false;
                return;
            }
            var size = glass.transform.lossyScale;
            texture = new RenderTexture(Mathf.Max(64, Mathf.RoundToInt(size.x * pixelsPerMetre)),
                Mathf.Max(64, Mathf.RoundToInt(size.y * pixelsPerMetre)), 24) { name = "CapMirror" };
            glassMaterial = glass.material;
            glassMaterial.SetTexture(BaseMap, texture);
            glassMaterial.SetColor(BaseColor, Color.white);

            var go = new GameObject("Mirror Camera") { hideFlags = HideFlags.DontSave };
            go.transform.SetParent(transform, false);
            mirrorCam = go.AddComponent<Camera>();
            mirrorCam.targetTexture = texture;
            mirrorCam.depth = -10;
            mirrorCam.clearFlags = CameraClearFlags.SolidColor;
            mirrorCam.backgroundColor = Color.black;
            mirrorCam.allowMSAA = false;
            mirrorCam.allowHDR = false;
            mirrorCam.enabled = false;
            var data = mirrorCam.GetUniversalAdditionalCameraData();
            data.renderPostProcessing = false;
            data.renderShadows = false;
            data.requiresColorTexture = false;
            data.requiresDepthTexture = false;

            if (head != null)
                headRest = head.localRotation;
            if (figure != null)
                figureRenderers = figure.GetComponentsInChildren<Renderer>(true);
            foreach (var r in figureRenderers)
            {
                r.shadowCastingMode = ShadowCastingMode.Off;
                r.enabled = false;
            }
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
            if (mirrorCam != null)
                mirrorCam.enabled = false;
        }

        void OnDestroy()
        {
            if (texture != null)
                texture.Release();
            if (glassMaterial != null)
                Destroy(glassMaterial);
        }

        void LateUpdate()
        {
            if (viewer == null || !viewer.isActiveAndEnabled || Time.unscaledTime > nextViewerCheck)
            {
                viewer = FindViewer();
                nextViewerCheck = Time.unscaledTime + 0.5f;
            }
            if (viewer == null)
            {
                mirrorCam.enabled = false;
                return;
            }

            var eye = viewer.transform.position;
            var centre = glass.transform.position;
            var normal = -glass.transform.forward;  // out of the glass, into the room
            float distance = Vector3.Dot(eye - centre, normal);
            bool on = distance > 0.05f && (eye - centre).sqrMagnitude < range * range;
            mirrorCam.enabled = on;
            if (!on)
                return;

            // The camera sits at the player's eye reflected through the glass and looks out into the room.
            // Its frustum is cut to the glass, and its near plane lies on the glass, so the wall behind isn't drawn.
            var mirrored = eye - 2f * distance * normal;
            var facing = Quaternion.LookRotation(normal, Vector3.up);
            mirrorCam.transform.SetPositionAndRotation(mirrored, facing);
            var right = facing * Vector3.right;
            var up = facing * Vector3.up;
            var toGlass = centre - mirrored;
            float near = Vector3.Dot(toGlass, normal);
            float x = Vector3.Dot(toGlass, right), y = Vector3.Dot(toGlass, up);
            var size = glass.transform.lossyScale;
            float hw = size.x * 0.5f, hh = size.y * 0.5f;
            mirrorCam.nearClipPlane = near;
            mirrorCam.farClipPlane = near + 30f;
            mirrorCam.projectionMatrix = Matrix4x4.Frustum(x - hw, x + hw, y - hh, y + hh, near, near + 30f);

            // The figure stands where the player is, turned the way the player looks.
            if (figure != null)
            {
                var look = viewer.transform.eulerAngles;
                figure.SetPositionAndRotation(eye, Quaternion.Euler(0f, look.y, 0f));
                if (head != null)
                {
                    // nod on top of the head's own resting angle (a character's head bone isn't level),
                    // around the figure's left-right axis
                    head.localRotation = headRest;
                    float pitch = Mathf.Clamp(Mathf.DeltaAngle(0f, look.x), -35f, 35f);
                    head.rotation = Quaternion.AngleAxis(pitch, figure.right) * head.rotation;
                }
            }
            if (cap != null)
            {
                bool worn = Run.HasFlag(capFlag);
                if (cap.activeSelf != worn)
                    cap.SetActive(worn);
            }
        }

        // The camera the player sees through: the main camera, or else the nearest one drawing to the screen.
        Camera FindViewer()
        {
            var main = Camera.main;
            if (main != null && main.isActiveAndEnabled && main.targetTexture == null)
                return main;
            Camera best = null;
            float bestDistance = float.MaxValue;
            foreach (var c in Camera.allCameras)
            {
                if (c == mirrorCam || c.targetTexture != null)
                    continue;
                float d = (c.transform.position - transform.position).sqrMagnitude;
                if (d < bestDistance)
                {
                    bestDistance = d;
                    best = c;
                }
            }
            return best;
        }

        void BeginCamera(ScriptableRenderContext context, Camera cam) => ShowFigure(cam == mirrorCam);

        void EndCamera(ScriptableRenderContext context, Camera cam) => ShowFigure(false);

        void ShowFigure(bool show)
        {
            if (figureOn == show)
                return;
            figureOn = show;
            foreach (var r in figureRenderers)
                if (r != null)
                    r.enabled = show;
        }
    }
}
