using System;
using System.Collections.Generic;
using HeyHeyThere.EscapePuzzleKitFree;
using Unity.XR.CoreUtils;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.XR;
using UnityEngine.XR;

namespace LastShift.XR
{
    /// <summary>
    /// Swaps between the desktop player and the XR rig. Desktop by default; the rig while a
    /// headset session runs (WebXR in the browser, OpenXR on Quest, or the XR Device Simulator
    /// in the editor). The player keeps their place in the room when it swaps.
    /// </summary>
    [DisallowMultipleComponent]
    public class RigSwitcher : MonoBehaviour
    {
        [Tooltip("Desktop player root (the kit's FirstPersonWalker). Empty: found in the scene")]
        public GameObject desktopPlayer;

        [Tooltip("XR rig root. Empty: the XR Origin under this object")]
        public XROrigin xrRig;

        [Tooltip("Seconds between headset checks")]
        public float checkInterval = 0.25f;

        /// <summary>True while the XR rig is the active player.</summary>
        public bool InXR { get; private set; }

        /// <summary>Raised after every swap: true for XR, false for desktop.</summary>
        public static event Action<bool> ModeChanged;

        static readonly List<XRDisplaySubsystem> displays = new List<XRDisplaySubsystem>();
        float nextCheck;

        void Awake()
        {
            if (xrRig == null)
                xrRig = GetComponentInChildren<XROrigin>(true);
            if (desktopPlayer == null)
            {
                var walker = FindAnyObjectByType<FirstPersonWalker>(FindObjectsInactive.Include);
                if (walker != null)
                    desktopPlayer = walker.gameObject;
            }
            if (xrRig == null)
                Debug.LogWarning("RigSwitcher: no XR Origin found under " + name, this);
        }

        void Start() => Apply(HeadsetActive(), force: true);

        void Update()
        {
            if (Time.unscaledTime < nextCheck)
                return;
            nextCheck = Time.unscaledTime + checkInterval;

            var xr = HeadsetActive();
            if (xr != InXR)
                Apply(xr);
        }

        void Apply(bool xr, bool force = false)
        {
            if (!force && xr == InXR)
                return;
            InXR = xr;

            if (xr)
            {
                var spot = DesktopFeet(out var yaw);
                if (desktopPlayer != null)
                    desktopPlayer.SetActive(false);
                if (xrRig != null)
                {
                    xrRig.gameObject.SetActive(true);
                    if (spot.HasValue)
                        xrRig.GetComponent<IPlayerBody>()?.Place(spot.Value, yaw, 0f);
                }
            }
            else
            {
                Vector3? spot = null;
                var yaw = 0f;
                if (xrRig != null && xrRig.gameObject.activeInHierarchy && xrRig.Camera != null)
                {
                    var head = xrRig.Camera.transform;
                    spot = new Vector3(head.position.x, xrRig.transform.position.y, head.position.z);
                    yaw = head.eulerAngles.y;
                }
                if (xrRig != null)
                    xrRig.gameObject.SetActive(false);
                if (desktopPlayer != null)
                {
                    desktopPlayer.SetActive(true);
                    if (spot.HasValue && !force)
                        desktopPlayer.GetComponent<IPlayerBody>()?.Place(spot.Value, yaw, 0f);
                }
            }

            Debug.Log(xr ? "RigSwitcher: headset on, using the XR rig" : "RigSwitcher: no headset, using the desktop player", this);
            ModeChanged?.Invoke(xr);
        }

        /// <summary>Where the desktop player's feet are, and which way it faces.</summary>
        Vector3? DesktopFeet(out float yaw)
        {
            yaw = 0f;
            if (desktopPlayer == null)
                return null;
            var t = desktopPlayer.transform;
            yaw = t.eulerAngles.y;
            var cc = desktopPlayer.GetComponent<CharacterController>();
            if (cc == null)
                return t.position;
            var centre = t.TransformPoint(cc.center);
            return centre - Vector3.up * (cc.height * 0.5f * t.lossyScale.y);
        }

        /// <summary>Whether a headset session is running right now.</summary>
        public static bool HeadsetActive()
        {
#if UNITY_WEBGL && !UNITY_EDITOR
            return WebXRInVR();
#elif UNITY_ANDROID && !UNITY_EDITOR
            // a Quest build: the app only ever runs inside the headset
            return true;
#else
            SubsystemManager.GetSubsystems(displays);
            foreach (var d in displays)
                if (d.running)
                    return true;
            // The XR Device Simulator adds a simulated headset device.
            foreach (var device in InputSystem.devices)
                if (device is XRHMD && device.enabled)
                    return true;
            return false;
#endif
        }

        // The WebXR package's code can't be named from the game's scripts (its assembly isn't
        // referenced automatically), so its manager is found by name and asked for its state.
        static System.Reflection.PropertyInfo webxrInstance, webxrState;
        static bool webxrLooked;

        static bool WebXRInVR()
        {
            if (!webxrLooked)
            {
                webxrLooked = true;
                var type = Type.GetType("WebXR.WebXRManager, WebXR");
                if (type != null)
                {
                    webxrInstance = type.GetProperty("Instance", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static);
                    webxrState = type.GetProperty("XRState", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
                }
            }
            if (webxrInstance == null || webxrState == null) return false;
            var manager = webxrInstance.GetValue(null);
            if (manager == null) return false;
            var state = webxrState.GetValue(manager);
            return state != null && state.ToString() == "VR";
        }
    }
}
