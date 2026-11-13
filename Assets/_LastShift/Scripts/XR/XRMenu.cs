using System.Collections.Generic;
using System.Linq;
using HeyHeyThere.EscapePuzzleKitFree;
using LastShift.Settings;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using UnityEngine.XR.Interaction.Toolkit.UI;

namespace LastShift.XR
{
    /// <summary>
    /// The VR player's menu: the kit's inventory, note reader and pause menu are drawn flat on the
    /// screen, which a headset can't show, so this puts them on a panel in front of the player.
    /// Y / B (or the left menu button) opens and closes it. Items lists what's carried; a note's
    /// text is shown in large print, page by page, and an item with a model turns slowly beside it
    /// (so the back of the badge can be read). Settings are the same as the title screen's.
    /// It also opens by itself the first time a note is found, and when a document in the room is
    /// read (the kit's examine view), showing that text.
    /// The panel and its parts are ordinary objects on the XR rig, editable in the Inspector.
    /// </summary>
    public class XRMenu : MonoBehaviour
    {
        [Header("Input and placement")]
        public InputActionReference menuAction;
        public Transform head;
        [Tooltip("Turned off while the menu is open, so the sticks don't walk the player")]
        public GameObject locomotion;
        public float distance = 0.85f;
        public float drop = 0.12f;

        [Header("Panel")]
        public Canvas canvas;
        public GameObject itemsPage, settingsPage;
        public Button itemsTab, settingsTab;

        [Header("Items")]
        [Tooltip("A disabled button that's copied for each item")]
        public Button itemTemplate;
        public Transform itemList;
        public Text emptyText, detailTitle, detailBody, pageLabel;
        public Button prevPage, nextPage;
        public Transform preview;

        [Header("Settings")]
        public Toggle subtitles, batteryDrain, keypadLockout;
        public Slider textSize, volume;
        public Text textSizeValue, volumeValue;

        public bool IsOpen => canvas != null && canvas.gameObject.activeSelf;

        readonly List<Button> itemButtons = new List<Button>();
        readonly HashSet<string> known = new HashSet<string>();
        string[] pages = new string[0];
        int page;
        GameObject previewModel;

        void Awake()
        {
            if (canvas != null) canvas.gameObject.SetActive(false);
            // over the room and over the HUD, so walls never cut through the menu
            UIOnTop.Apply(canvas, 200);
            if (itemTemplate != null) itemTemplate.gameObject.SetActive(false);
            // a long note shrinks a little to fit the page instead of losing its last lines
            if (detailBody != null)
            {
                detailBody.resizeTextMaxSize = Mathf.Max(detailBody.resizeTextMaxSize, detailBody.fontSize);
                detailBody.resizeTextMinSize = Mathf.Max(10, Mathf.RoundToInt(detailBody.fontSize * 0.6f));
                detailBody.resizeTextForBestFit = true;
                detailBody.verticalOverflow = VerticalWrapMode.Truncate;
            }
            foreach (var kv in Run.Items) known.Add(kv.Key);
        }

        void OnEnable()
        {
            Run.ItemsChanged += OnItemsChanged;
            Panels.Examine += OnExamine;
        }

        void OnDisable()
        {
            Run.ItemsChanged -= OnItemsChanged;
            Panels.Examine -= OnExamine;
            if (IsOpen) Close();
        }

        void Update()
        {
            if (menuAction != null && menuAction.action != null && menuAction.action.WasPressedThisFrame() && !Run.HasEscaped)
            {
                if (IsOpen) Close(); else Open();
            }
            if (IsOpen && previewModel != null)
                previewModel.transform.Rotate(0f, 35f * Time.unscaledDeltaTime, 0f, Space.World);
        }

        // ---- opening -----------------------------------------------------------------------------

        public void Open() => Open(null, null, null);

        void Open(string itemId, string title, string text)
        {
            if (canvas == null) return;
            EnsureEventSystem();
            if (!IsOpen)
            {
                Place();
                canvas.gameObject.SetActive(true);
                Interactor.Hold(this, true);
                if (locomotion != null) locomotion.SetActive(false);
            }
            ShowItems();
            ReadSettings();
            if (text != null) ShowText(title, text, null);
            else if (itemId != null) Select(itemId);
        }

        public void Close()
        {
            if (canvas != null) canvas.gameObject.SetActive(false);
            Interactor.Hold(this, false);
            if (locomotion != null) locomotion.SetActive(true);
            ClearPreview();
        }

        void Place()
        {
            var cam = head != null ? head : (Camera.main != null ? Camera.main.transform : null);
            if (cam == null) return;
            var fwd = Vector3.ProjectOnPlane(cam.forward, Vector3.up);
            if (fwd.sqrMagnitude < 0.001f) fwd = Vector3.ProjectOnPlane(cam.up, Vector3.up);
            fwd.Normalize();
            canvas.transform.position = cam.position + fwd * distance - Vector3.up * drop;
            canvas.transform.rotation = Quaternion.LookRotation(fwd, Vector3.up);
        }

        // the room scenes have no event system; the controller rays need one to press the buttons
        static void EnsureEventSystem()
        {
            if (EventSystem.current != null) return;
            var es = new GameObject("EventSystem", typeof(EventSystem), typeof(XRUIInputModule));
            Object.DontDestroyOnLoad(es);
        }

        // a note found for the first time opens straight on it
        void OnItemsChanged()
        {
            string newDoc = null;
            foreach (var kv in Run.Items)
            {
                if (known.Add(kv.Key))
                {
                    var item = Item.Find(kv.Key);
                    if (item != null && item.kind == Item.Kind.Document && !string.IsNullOrEmpty(item.text))
                        newDoc = kv.Key;
                }
            }
            if (newDoc != null && isActiveAndEnabled) Open(newDoc, null, null);
        }

        // a document read in the room (the kit's examine view, which the headset can't show)
        void OnExamine(Item item, GameObject shown, string text)
        {
            if (!isActiveAndEnabled) return;
            var body = !string.IsNullOrEmpty(text) ? text : item != null ? item.text : "";
            var title = item != null ? Item.Title(item.Id) : shown != null ? shown.name : "";
            if (string.IsNullOrEmpty(body) && item != null) { Open(item.Id, null, null); return; }
            Open(null, title, body);
            if (item != null) ShowPreview(item);
        }

        // ---- tabs ---------------------------------------------------------------------------------

        public void ShowItems()
        {
            if (itemsPage != null) itemsPage.SetActive(true);
            if (settingsPage != null) settingsPage.SetActive(false);
            RebuildList();
        }

        public void ShowSettings()
        {
            if (itemsPage != null) itemsPage.SetActive(false);
            if (settingsPage != null) settingsPage.SetActive(true);
            ClearPreview();
            ReadSettings();
        }

        public void ToTitle()
        {
            Close();
            LastShift.Menus.GameFlow.ToTitle();
        }

        // ---- items --------------------------------------------------------------------------------

        void RebuildList()
        {
            foreach (var b in itemButtons) if (b != null) Destroy(b.gameObject);
            itemButtons.Clear();
            var items = Run.Items.ToList();
            if (emptyText != null) emptyText.gameObject.SetActive(items.Count == 0);
            if (itemTemplate == null || itemList == null) return;
            foreach (var kv in items)
            {
                var id = kv.Key;
                var b = Instantiate(itemTemplate, itemList);
                b.gameObject.SetActive(true);
                b.name = "Item " + id;
                var label = b.GetComponentInChildren<Text>();
                if (label != null) label.text = Item.Title(id) + (kv.Value > 1 ? "  x" + kv.Value : "");
                b.onClick.AddListener(() => Select(id));
                itemButtons.Add(b);
            }
            FitList();
            if (items.Count > 0 && (detailTitle == null || string.IsNullOrEmpty(detailTitle.text)))
                Select(items[0].Key);
            else if (items.Count == 0)
                ShowText("", "", null);
        }

        // Later in the game the list grows past the bottom of the panel: the buttons get shorter,
        // and their labels smaller, so every item still fits on the page.
        void FitList()
        {
            if (itemButtons.Count == 0 || itemsPage == null) return;
            var page = (RectTransform)itemsPage.transform;
            var list = (RectTransform)itemList;
            var corners = new Vector3[4];
            list.GetWorldCorners(corners);
            var top = page.InverseTransformPoint(corners[1]).y;
            var room = top - page.rect.yMin - 16f;
            var layout = itemList.GetComponent<HorizontalOrVerticalLayoutGroup>();
            var gap = layout != null ? layout.spacing : 4f;
            var full = ((RectTransform)itemTemplate.transform).rect.height;
            var n = itemButtons.Count;
            var each = Mathf.Min(full, (room - gap * (n - 1)) / n);
            if (each >= full - 0.5f) return;
            for (var i = 0; i < n; i++)
            {
                var rt = (RectTransform)itemButtons[i].transform;
                var le = rt.GetComponent<LayoutElement>();
                if (le == null) le = rt.gameObject.AddComponent<LayoutElement>();
                le.minHeight = le.preferredHeight = each;
                rt.sizeDelta = new Vector2(rt.sizeDelta.x, each);
                if (layout == null) rt.anchoredPosition = new Vector2(rt.anchoredPosition.x, -i * (each + gap));
                foreach (var t in rt.GetComponentsInChildren<Text>(true))
                {
                    t.resizeTextForBestFit = true;
                    t.resizeTextMinSize = 6;
                    t.resizeTextMaxSize = Mathf.Max(t.resizeTextMaxSize, t.fontSize);
                }
            }
        }

        public void Select(string id)
        {
            if (itemsPage != null && !itemsPage.activeSelf) ShowItems();
            var item = Item.Find(id);
            var body = item == null ? "" : item.kind == Item.Kind.Document && !string.IsNullOrEmpty(item.text) ? item.text : item.description;
            ShowText(Item.Title(id), body, item);
        }

        void ShowText(string title, string body, Item item)
        {
            if (detailTitle != null) detailTitle.text = title ?? "";
            pages = (body ?? "").Replace("\r", "").Split(new[] { "\n---\n", "\n---", "---\n" }, System.StringSplitOptions.None)
                .Select(p => p.Trim()).Where(p => p.Length > 0).DefaultIfEmpty("").ToArray();
            page = 0;
            ShowPage();
            ClearPreview();
            if (item != null) ShowPreview(item);
        }

        void ShowPage()
        {
            if (detailBody != null) detailBody.text = pages.Length > 0 ? pages[page] : "";
            var many = pages.Length > 1;
            if (pageLabel != null) pageLabel.text = many ? $"{page + 1} / {pages.Length}" : "";
            if (prevPage != null) prevPage.gameObject.SetActive(many);
            if (nextPage != null) nextPage.gameObject.SetActive(many);
        }

        public void NextPage() { if (pages.Length > 1) { page = (page + 1) % pages.Length; ShowPage(); } }
        public void PrevPage() { if (pages.Length > 1) { page = (page + pages.Length - 1) % pages.Length; ShowPage(); } }

        // a still copy of the item's model, turning slowly, about 18 cm across
        void ShowPreview(Item item)
        {
            ClearPreview();
            if (preview == null || item == null || item.model == null) return;
            // copied under a switched-off holder so the prop's own scripts never wake up, then
            // stripped down to what it looks like
            var holder = new GameObject("Preview");
            holder.SetActive(false);
            holder.transform.SetParent(preview, false);
            var copy = Instantiate(item.model, holder.transform);
            foreach (var c in copy.GetComponentsInChildren<Component>(true).Reverse())
                if (!(c is Transform) && !(c is Renderer) && !(c is MeshFilter))
                    DestroyImmediate(c);
            previewModel = holder;
            holder.SetActive(true);
            previewModel.transform.localPosition = Vector3.zero;
            previewModel.transform.localRotation = Quaternion.identity;
            var rs = previewModel.GetComponentsInChildren<Renderer>();
            if (rs.Length == 0) return;
            var b = rs[0].bounds;
            foreach (var r in rs) b.Encapsulate(r.bounds);
            var size = Mathf.Max(b.size.x, b.size.y, b.size.z);
            if (size > 0.0001f)
                previewModel.transform.localScale *= 0.18f / size / Mathf.Max(0.0001f, preview.lossyScale.x);
            rs = previewModel.GetComponentsInChildren<Renderer>();
            b = rs[0].bounds;
            foreach (var r in rs) b.Encapsulate(r.bounds);
            previewModel.transform.position += preview.position - b.center;
        }

        void ClearPreview()
        {
            if (previewModel != null) Destroy(previewModel);
            previewModel = null;
        }

        // ---- settings, the same ones as the title screen and the pause menu ------------------------

        void ReadSettings()
        {
            if (subtitles != null) subtitles.SetIsOnWithoutNotify(GameSettings.Subtitles);
            if (batteryDrain != null) batteryDrain.SetIsOnWithoutNotify(GameSettings.BatteryDrain);
            if (keypadLockout != null) keypadLockout.SetIsOnWithoutNotify(GameSettings.KeypadLockout);
            if (textSize != null)
            {
                textSize.minValue = GameSettings.MinTextSize;
                textSize.maxValue = GameSettings.MaxTextSize;
                textSize.SetValueWithoutNotify(GameSettings.TextSize);
            }
            if (volume != null) volume.SetValueWithoutNotify(GameSettings.Volume);
            Labels();
        }

        void Labels()
        {
            if (textSizeValue != null) textSizeValue.text = Mathf.RoundToInt(GameSettings.TextSize * 100f) + "%";
            if (volumeValue != null) volumeValue.text = Mathf.RoundToInt(GameSettings.Volume * 100f) + "%";
        }

        public void SetSubtitles(bool on) => GameSettings.Subtitles = on;
        public void SetBatteryDrain(bool on) => GameSettings.BatteryDrain = on;
        public void SetKeypadLockout(bool on) => GameSettings.KeypadLockout = on;
        public void SetTextSize(float v) { GameSettings.TextSize = v; Labels(); }
        public void SetVolume(float v) { GameSettings.Volume = v; Labels(); }
    }
}
