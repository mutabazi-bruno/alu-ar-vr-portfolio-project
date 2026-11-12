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
            Debug.Log(headset ? "Title: headset on, using the XR rig" : "Title: no headset, using the desktop camera");
            if (headset && xrRig != null) StartCoroutine(Recentre());
        }

        // the headset's floor origin can be anywhere in the play area: once tracking has settled,
        // stand the player where the desktop camera stands, facing the menu
        System.Collections.IEnumerator Recentre()
        {
            for (int i = 0; i < 10; i++) yield return null;
            var body = xrRig.GetComponent<LastShift.XR.XRRigBody>();
            if (body == null) yield break;
            var spot = desktopCamera != null ? desktopCamera.transform.position : Vector3.zero;
            spot.y = 0f;
            var yaw = desktopCamera != null ? desktopCamera.transform.eulerAngles.y : 0f;
            body.Place(spot, yaw, 0f);
        }
    }
}
