using UnityEngine;
using UnityEngine.InputSystem;

namespace LastShift.XR
{
    /// <summary>
    /// Points a controller's ray the way the player is pointing. On a real headset (OpenXR on Quest,
    /// WebXR in the browser) a controller reports two poses: the grip, how it's held, which tips
    /// down towards the floor, and the aim, where it points. The controller object follows the grip
    /// (so the model sits in the hand); this moves the ray to the aim pose. Where there's no aim pose
    /// (the XR Interaction Simulator) the ray simply stays on the controller, as before.
    /// Put it on the Ray Interactor object, a child of the controller.
    /// </summary>
    [DefaultExecutionOrder(-30000)]
    public class XRAimPose : MonoBehaviour
    {
        public InputActionReference aimPosition;
        public InputActionReference aimRotation;
        [Tooltip("The controller this ray belongs to; its parent is the tracking space the poses are in")]
        public Transform controller;

        void OnEnable() => Application.onBeforeRender += Apply;
        void OnDisable() => Application.onBeforeRender -= Apply;

        // before the interactors cast their rays this frame, and again just before drawing
        void Update() => Apply();

        void Apply()
        {
            if (controller == null) return;
            var pos = aimPosition != null ? aimPosition.action : null;
            var rot = aimRotation != null ? aimRotation.action : null;
            // the device has an aim pose at all (the simulator's controllers don't)
            bool aimed = pos != null && rot != null && pos.enabled && pos.controls.Count > 0 && rot.controls.Count > 0;
            if (!aimed)
            {
                transform.localPosition = Vector3.zero;
                transform.localRotation = Quaternion.identity;
                return;
            }
            var space = controller.parent;
            var p = pos.ReadValue<Vector3>();
            var r = rot.ReadValue<Quaternion>();
            transform.SetPositionAndRotation(
                space != null ? space.TransformPoint(p) : p,
                space != null ? space.rotation * r : r);
        }
    }
}
