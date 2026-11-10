using HeyHeyThere.EscapePuzzleKitFree;
using Unity.XR.CoreUtils;
using UnityEngine;

namespace LastShift.XR
{
    /// <summary>
    /// Lets the kit place the XR rig (save points, entrances, the rig switcher). The head is
    /// turned to face the yaw and stood over the position; pitch is ignored, since the
    /// player's real head decides where they look.
    /// </summary>
    [RequireComponent(typeof(XROrigin))]
    public class XRRigBody : MonoBehaviour, IPlayerBody
    {
        public void Place(Vector3 position, float yaw, float pitch)
        {
            var origin = GetComponent<XROrigin>();
            var cc = GetComponent<CharacterController>();
            if (cc != null)
                cc.enabled = false;

            var head = origin.Camera != null ? origin.Camera.transform : transform;
            transform.RotateAround(head.position, Vector3.up, Mathf.DeltaAngle(head.eulerAngles.y, yaw));

            var h = head.position;
            transform.position += new Vector3(position.x - h.x, position.y - transform.position.y, position.z - h.z);

            if (cc != null)
                cc.enabled = true;
            Physics.SyncTransforms();
        }
    }
}
