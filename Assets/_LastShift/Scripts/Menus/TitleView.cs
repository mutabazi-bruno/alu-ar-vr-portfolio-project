using UnityEngine;

namespace LastShift.Menus
{
    /// <summary>
    /// The title screen has no player to walk around: a fixed camera on a monitor, or the XR rig
    /// standing in the same spot when a headset (or the simulator) is on, checked the same way as
    /// the rig switcher in the rooms. The menu canvas takes clicks from the mouse and from the
    /// controller rays.
    /// </summary>
    public class TitleView : MonoBehaviour
    {
        public GameObject desktopCamera;
        public GameObject xrRig;
        public Canvas menu;
        const float Interval = 0.25f;
        float next;
        bool? vr;

        void Start() => Apply(LastShift.XR.RigSwitcher.HeadsetActive());

        void Update()
        {
            if (Time.unscaledTime < next) return;
            next = Time.unscaledTime + Interval;
            var now = LastShift.XR.RigSwitcher.HeadsetActive();
            if (vr != now) Apply(now);
        }

        void Apply(bool headset)
        {
            vr = headset;
            if (xrRig != null) xrRig.SetActive(headset);
            if (desktopCamera != null) desktopCamera.SetActive(!headset);
            if (menu != null && desktopCamera != null)
                menu.worldCamera = headset ? null : desktopCamera.GetComponent<Camera>();
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = !headset;
        }
    }
}
