using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;

namespace LastShift.XR
{
    /// <summary>
    /// Makes a world-space canvas draw over the room, so walls and furniture never cut through the
    /// VR HUD or menu text: its graphics use a copy of the UI shader with the depth test off.
    /// </summary>
    public static class UIOnTop
    {
        static Material onTop;

        public static void Apply(Canvas canvas, int sortingOrder = 100)
        {
            if (canvas == null) return;
            if (onTop == null)
            {
                onTop = new Material(Shader.Find("UI/Default")) { name = "UI On Top" };
                onTop.SetInt("unity_GUIZTestMode", (int)CompareFunction.Always);
                onTop.renderQueue = 2999;  // after the room, before the controller rays, so the rays show over menus
            }
            canvas.sortingOrder = sortingOrder;
            foreach (var g in canvas.GetComponentsInChildren<Graphic>(true))
                g.material = onTop;
        }
    }
}
