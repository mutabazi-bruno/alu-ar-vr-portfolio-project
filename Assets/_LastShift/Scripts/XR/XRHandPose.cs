using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace LastShift.XR
{
    /// <summary>
    /// Curls a hand model's fingers from the controller: the grip closes the middle, ring and little
    /// fingers (and the thumb a little), the trigger closes the index finger. Each finger joint turns
    /// from its rest pose by an angle around one local axis, smoothed so the motion looks natural.
    /// The joints are found by name (Index, Middle, Ring, Little, Thumb), so the XR Hands models work
    /// as they are. Put it on the hand model's root under a controller.
    /// </summary>
    public class XRHandPose : MonoBehaviour
    {
        public InputActionReference grip;
        public InputActionReference trigger;
        [Tooltip("How far each joint bends when fully closed, in degrees")]
        public float curl = 70f;
        public float thumbCurl = 35f;
        [Tooltip("The local axis the fingers bend around; flip the sign if they bend backwards")]
        public Vector3 axis = new Vector3(1f, 0f, 0f);
        public float smoothing = 14f;

        class Joint { public Transform t; public Quaternion rest; public bool index, thumb; }
        readonly List<Joint> joints = new List<Joint>();
        float gripNow, triggerNow;

        void Awake()
        {
            foreach (var t in GetComponentsInChildren<Transform>(true))
            {
                var n = t.name.ToLowerInvariant();
                bool finger = n.Contains("index") || n.Contains("middle") || n.Contains("ring") || n.Contains("little") || n.Contains("thumb");
                bool segment = n.Contains("proximal") || n.Contains("intermediate") || n.Contains("distal");
                if (!finger || !segment) continue;
                joints.Add(new Joint { t = t, rest = t.localRotation, index = n.Contains("index"), thumb = n.Contains("thumb") });
            }
        }

        [Header("Holding the controller")]
        [Tooltip("At start, turn and place the hand so it holds the controller: fingers forward along " +
                 "the handle, index finger on top, knuckles just ahead of the grip. Worked out from the " +
                 "model's own finger joints, so it doesn't matter which way the model was made")]
        public bool holdController = true;
        [Tooltip("Where the middle knuckle sits, in the pointing pose's space (metres)")]
        public Vector3 knuckles = new Vector3(0f, -0.025f, -0.03f);

        void Start()
        {
            // A real Quest controller's grip pose runs down the handle, tilted against the way you
            // point, while the simulator's points straight ahead, so a hand on the grip looks right
            // in the editor and twisted in the headset. The pointing (aim) pose is the same in both:
            // the hand rides on the ray's object, which follows it.
            var aim = transform.parent != null ? transform.parent.GetComponentInChildren<XRAimPose>(true) : null;
            if (aim != null) transform.SetParent(aim.transform, false);
            if (holdController) Hold();
        }

        void Hold()
        {
            var parent = transform.parent;
            if (parent == null) return;
            Transform wrist = null, middle = null, index = null, little = null;
            foreach (var t in GetComponentsInChildren<Transform>(true))
            {
                var n = t.name.ToLowerInvariant();
                if (n.Contains("wrist") && wrist == null) wrist = t;
                if (!n.Contains("proximal") || n.Contains("thumb")) continue;
                if (n.Contains("middle")) middle = t;
                else if (n.Contains("index")) index = t;
                else if (n.Contains("little")) little = t;
            }
            if (middle == null || index == null || little == null) return;
            var from = wrist != null ? wrist.position : transform.position;

            // the way the fingers point, and the way across the knuckles to the index finger
            var fingers = parent.InverseTransformDirection(middle.position - from);
            var across = parent.InverseTransformDirection(index.position - little.position);
            if (fingers.sqrMagnitude < 1e-6f || across.sqrMagnitude < 1e-6f) return;
            var now = Quaternion.LookRotation(fingers, across);
            transform.localRotation = Quaternion.Inverse(now) * transform.localRotation;
            transform.localRotation = Quaternion.LookRotation(Vector3.forward, Vector3.up) * transform.localRotation;

            var knuckle = parent.InverseTransformPoint(middle.position);
            transform.localPosition += knuckles - knuckle;
        }

        static float Read(InputActionReference r) =>
            r != null && r.action != null && r.action.enabled ? Mathf.Clamp01(r.action.ReadValue<float>()) : 0f;

        void Update()
        {
            var k = 1f - Mathf.Exp(-smoothing * Time.unscaledDeltaTime);
            gripNow = Mathf.Lerp(gripNow, Read(grip), k);
            triggerNow = Mathf.Lerp(triggerNow, Read(trigger), k);
            foreach (var j in joints)
            {
                if (j.t == null) continue;
                float amount = j.index ? triggerNow : gripNow;
                float angle = j.thumb ? thumbCurl * gripNow : curl * amount;
                j.t.localRotation = j.rest * Quaternion.AngleAxis(angle, axis);
            }
        }
    }
}
