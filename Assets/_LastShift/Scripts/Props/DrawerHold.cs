using HeyHeyThere.EscapePuzzleKitFree;
using UnityEngine;

namespace LastShift.Props
{
    /// <summary>
    /// Keeps a drawer where its openable says it is. The cabinet model's own animations can leave
    /// the top drawer pulled out while the game counts it as shut (it showed OPEN on an open
    /// drawer, and using it only flipped the prompt). This sets the drawer's place every frame,
    /// after any animation: shut at rest, pulled out by the offset when open, sliding in between.
    /// </summary>
    public class DrawerHold : MonoBehaviour
    {
        public Openable openable;
        public Transform drawer;
        [Tooltip("The drawer's local position when shut")]
        public Vector3 shut;
        [Tooltip("How far it comes out when open, in its parent's space")]
        public Vector3 pulled = new Vector3(0f, 0f, 0.4f);

        void LateUpdate()
        {
            if (openable == null || drawer == null) return;
            drawer.localPosition = shut + pulled * openable.motion.Position;
        }
    }
}
