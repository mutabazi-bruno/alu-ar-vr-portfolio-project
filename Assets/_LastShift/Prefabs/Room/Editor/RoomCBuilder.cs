using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace LastShift.EditorTools
{
    // Room C: the fake escape. Same layout as Room B with the wrong details, a corridor in front
    // (where the player arrives from Room B), and a "mirror" in the back wall that is really a
    // doorway into a mirrored copy of the room, where the clothes puzzle and the real exit are.
    // Run it from the menu: Last Shift > Build Room C. Rebuilds only "Room" and "RoomCExtras".
    public static partial class RoomLayoutBuilder
    {
        const string RoomBScenePath = "Assets/_LastShift/Scenes/RecordsRoomB.unity";
        const string RoomCScenePath = "Assets/_LastShift/Scenes/RoomC.unity";

        // Where the mirror sits along the back wall (world X), and the corridor's size in metres.
        const float MirrorAlong = -0.6f;
        const float CorridorWidth = 1.8f;
        const float CorridorLength = 6f;
        const float CorridorHeight = 3f;

        static Vector2 mirrorGlassSize = new Vector2(1.1f, 2.2f);

        [MenuItem("Last Shift/Build Room C")]
        public static void BuildRoomC()
        {
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(RoomCScenePath) == null)
                AssetDatabase.CopyAsset(RoomBScenePath, RoomCScenePath); // starts with Room B's player, volume and settings
            BuildRoom(true);
        }

        // Inner face of the front wall is at -Depth/2; the corridor starts at its outer face.
        static float CorridorStartZ => -Depth / 2 - wallThick;
        static float CorridorEndZ => CorridorStartZ - CorridorLength;
        static Vector3 ArrivalPoint => new Vector3(0, 0.05f, CorridorEndZ + 0.9f);

        // The mirror plane runs through the middle of the back wall.
        static float MirrorPlaneZ => Depth / 2 + wallThick / 2;

        // ---------------------------------------------------------------- the mirror doorway

        static void PlaceMirrorOpening()
        {
            if (doorwayAsset == null) return;
            var side = Side.Back;
            WallLine(side, out _, out var dir);
            var outward = -Inward(side);

            // The glass fills the same opening a door would, so size it from the kit's steel door.
            var doorAsset = FindKit(new[] { "doorsteel" });
            if (doorAsset != null)
            {
                var temp = Spawn(doorAsset, structureRoot, "probe");
                temp.transform.SetPositionAndRotation(Vector3.zero, FacingRoom(side, "SteelDoor"));
                var db = GetBounds(temp);
                mirrorGlassSize = new Vector2(HalfExtentAlong(db, dir) * 2 * 0.9f, db.size.y * 0.96f);
                Object.DestroyImmediate(temp);
            }

            var doorway = Spawn(doorwayAsset, structureRoot, "Doorway_Mirror");
            doorway.transform.SetPositionAndRotation(Vector3.zero, PieceRotation(side, doorwayAsset));
            var b = GetBounds(doorway);
            var target = WallPoint(side, MirrorAlong) + outward * (wallThick / 2);
            target.y = b.extents.y;
            doorway.transform.position += target - b.center;
            openings.Add((side, MirrorAlong, HalfExtentAlong(GetBounds(doorway), dir), 0f, true));
        }

        // ---------------------------------------------------------------- Room C's wrong details

        // Folded-clothes stand-ins where Room B has the archive boxes. Gama's clothes items replace them.
        static void FillShelvingClothes(GameObject shelving)
        {
            var n = Inward(Side.Right);
            var viewerRight = Vector3.Cross(Vector3.up, -n);
            var sb = GetBounds(shelving);
            float shelfWidth = HalfExtentAlong(sb, viewerRight) * 2;
            float shelfDepth = HalfExtentAlong(sb, n) * 2;
            var boards = ShelfLevels(shelving).Where(y => y > 0.3f && y < 1.75f).Take(3).ToList();
            if (boards.Count < 3) boards = new List<float> { sb.min.y + sb.size.y * 0.7f, sb.min.y + sb.size.y * 0.47f, sb.min.y + sb.size.y * 0.25f };

            var holder = Child(dressingRoot, "ClothesStandIns");
            var pieces = new (string name, Color colour, int row, float side, Vector3 size)[]
            {
                ("Jacket",   new Color(0.12f, 0.16f, 0.28f), 0, -1, new Vector3(0.40f, 0.14f, 0.32f)),
                ("Shirt",    new Color(0.62f, 0.64f, 0.66f), 0,  1, new Vector3(0.36f, 0.10f, 0.28f)),
                ("Trousers", new Color(0.22f, 0.20f, 0.18f), 1, -1, new Vector3(0.38f, 0.12f, 0.30f)),
                ("Scarf",    new Color(0.45f, 0.10f, 0.12f), 1,  1, new Vector3(0.34f, 0.08f, 0.24f)),
                ("Cap",      new Color(0.10f, 0.12f, 0.20f), 2, -1, new Vector3(0.22f, 0.10f, 0.22f)),
            };
            foreach (var p in pieces)
            {
                var go = GameObject.CreatePrimitive(p.name == "Cap" ? PrimitiveType.Cylinder : PrimitiveType.Cube);
                go.name = $"ClothesStandIn_{p.name}";
                go.transform.SetParent(holder, false);
                go.transform.rotation = Quaternion.LookRotation(-n);
                go.transform.localScale = p.name == "Cap" ? new Vector3(p.size.x, p.size.y / 2, p.size.z) : p.size;
                go.GetComponent<Renderer>().sharedMaterial = GetMaterial($"Cloth{p.name}", false, p.colour);
                // At the front edge, slightly proud, so the clutter modelled onto the shelving sits behind it.
                var target = sb.center + viewerRight * (shelfWidth * 0.25f * p.side) + n * (shelfDepth / 2f + 0.02f - p.size.z / 2f);
                target.y = boards[p.row] + p.size.y / 2f + 0.002f;
                go.transform.position = target;
            }
        }

        // A wall clock stopped at a time. The hands are their own objects ("HourHand", "MinuteHand")
        // so a script can turn them later if the optional shift clock is switched on.
        static void PlaceClock(Side side, float along, float height, int hour, int minute)
        {
            var n = Inward(side);
            var root = new GameObject("WallClock").transform;
            root.SetParent(dressingRoot, false);
            root.position = WallPoint(side, along) + n * 0.03f + Vector3.up * height;
            root.rotation = Quaternion.LookRotation(-n);

            var face = MakeQuad("Face", "ClockFace", 0.36f, 0.36f, root);
            face.transform.localPosition = Vector3.zero;
            face.transform.localRotation = Quaternion.identity;
            var mat = face.GetComponent<Renderer>().sharedMaterial;
            mat.SetFloat("_AlphaClip", 1f);
            mat.SetFloat("_Cutoff", 0.5f);
            mat.EnableKeyword("_ALPHATEST_ON");
            mat.renderQueue = (int)RenderQueue.AlphaTest;
            EditorUtility.SetDirty(mat);

            Hand(root, "HourHand", 0.085f, 0.012f, -0.004f, (hour % 12) * 30f + minute * 0.5f);
            Hand(root, "MinuteHand", 0.13f, 0.008f, -0.007f, minute * 6f);
        }

        static void Hand(Transform clock, string name, float length, float width, float z, float angle)
        {
            var pivot = new GameObject(name).transform;
            pivot.SetParent(clock, false);
            pivot.localPosition = new Vector3(0, 0, z);
            pivot.localRotation = Quaternion.Euler(0, 0, -angle); // clockwise as seen from the room
            var blade = GameObject.CreatePrimitive(PrimitiveType.Quad);
            blade.name = "Blade";
            Object.DestroyImmediate(blade.GetComponent<Collider>());
            blade.transform.SetParent(pivot, false);
            blade.transform.localPosition = new Vector3(0, length / 2 - 0.015f, 0);
            blade.transform.localScale = new Vector3(width, length, 1);
            blade.GetComponent<Renderer>().sharedMaterial = GetMaterial("ClockHand", true, new Color(0.08f, 0.08f, 0.08f));
            blade.GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.Off;
        }

        // ---------------------------------------------------------------- corridor, mirror side, exit

        static void BuildRoomCExtras()
        {
            var old = GameObject.Find("RoomCExtras");
            if (old != null) Object.DestroyImmediate(old);
            var extras = new GameObject("RoomCExtras").transform;

            BuildCorridor(Child(extras, "Corridor"));
            BuildMirrorGlass(Child(extras, "MirrorGlass"));
            BuildMirrorSide(Child(extras, "MirrorSide"));
            PlacePuzzleSpots(Child(extras, "PuzzleSpots"));

            // Whoever comes through Room B's steel door arrives at the far end of the corridor.
            var arrival = new GameObject("FromRoomB");
            arrival.transform.SetParent(extras, false);
            arrival.transform.SetPositionAndRotation(ArrivalPoint, Quaternion.LookRotation(Vector3.forward));
            var entranceType = KitType("Entrance");
            if (entranceType != null)
            {
                var entrance = arrival.AddComponent(entranceType);
                var so = new SerializedObject(entrance);
                var id = so.FindProperty("id");
                if (id != null) { id.stringValue = "FromRoomB"; so.ApplyModifiedPropertiesWithoutUndo(); }
            }

            var player = GameObject.Find("LastShiftPlayer");
            if (player != null) player.transform.SetPositionAndRotation(new Vector3(ArrivalPoint.x, 0, ArrivalPoint.z), Quaternion.LookRotation(Vector3.forward));
        }

        static void BuildCorridor(Transform root)
        {
            var keep = structureRoot;
            structureRoot = root; // the wall helpers spawn into structureRoot
            try
            {
                float w = CorridorWidth / 2;
                float z0 = CorridorStartZ, z1 = CorridorEndZ;

                // Side walls and the end wall, the end one with a doorway for the door you came through.
                CorridorWall(Side.Left, new Vector3(-w, 0, z1), Vector3.forward, Vector3.left, CorridorLength, null);
                CorridorWall(Side.Right, new Vector3(w, 0, z0), Vector3.back, Vector3.right, CorridorLength, null);

                float half = 0.6f;
                GameObject backDoor = null;
                var doorAsset = FindKit(new[] { "doorsteel" });
                if (doorAsset != null && doorwayAsset != null)
                {
                    var centre = new Vector3(0, 0, z1) + Vector3.back * (wallThick / 2);
                    var doorway = Spawn(doorwayAsset, root, "Doorway_BackDoor");
                    doorway.transform.SetPositionAndRotation(Vector3.zero, PieceRotation(Side.Front, doorwayAsset));
                    var db = GetBounds(doorway);
                    doorway.transform.position += new Vector3(centre.x, db.extents.y, centre.z) - db.center;
                    half = HalfExtentAlong(GetBounds(doorway), Vector3.right);

                    // The door the player just came through: locked from this side, so it only says so.
                    backDoor = Spawn(doorAsset, root, "BackDoor");
                    backDoor.transform.SetPositionAndRotation(Vector3.zero, Quaternion.LookRotation(Vector3.forward));
                    var bb = GetBounds(backDoor);
                    backDoor.transform.position += new Vector3(centre.x, bb.extents.y, centre.z) - bb.center;
                    SetDoorLocked(backDoor, "It clicked shut behind you.");
                }
                CorridorWall(Side.Front, new Vector3(w, 0, z1), Vector3.left, Vector3.back, CorridorWidth,
                    new List<(float, float)> { (w - half, w + half) });

                // The corridor's open end butts against Room C's front wall, so it needs no wall there.
                var floor = AssetDatabase.LoadAssetAtPath<GameObject>($"{RoomPrefabFolder}/RoomFloor.prefab");
                var ceiling = AssetDatabase.LoadAssetAtPath<GameObject>($"{RoomPrefabFolder}/RoomCeiling.prefab");
                // The floor runs on under the doorway too, so there is no gap at the threshold.
                if (floor) TileRect(floor, "CorridorFloor", -w, w, z1, -Depth / 2, 0f, false, root);
                if (ceiling) TileRect(ceiling, "CorridorCeiling", -w, w, z1, z0, CorridorHeight, true, root);
                foreach (Transform t in root) SetStatic(t.gameObject);
            }
            finally { structureRoot = keep; }

            // Signs on the corridor side of Room C's door: an EXIT that leads into another records room.
            float doorTop = 2.3f;
            var cDoor = GameObject.Find("Room/Props/RoomCDoor");
            if (cDoor != null) doorTop = GetBounds(cDoor).max.y;
            SignFacing("ExitSign", "ExitSign", new Vector3(0, doorTop + 0.15f, CorridorStartZ - 0.006f), Vector3.back, 0.5f, 0.19f, root);
            SignFacing("DoorSignRoomC_Corridor", "DoorSignRecordsRoomC", new Vector3(0, Mathf.Min(doorTop + 0.55f, CorridorHeight - 0.2f), CorridorStartZ - 0.006f), Vector3.back, 0.6f, 0.3f, root);

            // Two dim lights along the corridor.
            foreach (var (name, z) in new[] { ("CorridorLightNear", CorridorStartZ - 1.5f), ("CorridorLightFar", CorridorEndZ + 1.5f) })
            {
                var go = new GameObject(name);
                go.transform.SetParent(root, false);
                go.transform.position = new Vector3(0, CorridorHeight - 0.3f, z);
                var l = go.AddComponent<Light>();
                l.type = LightType.Point; l.range = 4f; l.intensity = 0.7f;
                l.color = new Color(0.8f, 0.85f, 0.75f);
                l.shadows = LightShadows.None;
                l.lightmapBakeType = LightmapBakeType.Realtime;
            }
        }

        static void CorridorWall(Side like, Vector3 start, Vector3 dir, Vector3 outward, float length, List<(float from, float to)> gaps)
        {
            if (wallAsset == null) return;
            var rot = PieceRotation(like, wallAsset);
            var probe = Spawn(wallAsset, structureRoot, "probe");
            probe.transform.SetPositionAndRotation(Vector3.zero, rot);
            float pieceLen = HalfExtentAlong(GetBounds(probe), dir) * 2;
            Object.DestroyImmediate(probe);

            int index = 0;
            float cursor = 0;
            foreach (var g in (gaps ?? new List<(float, float)>()).OrderBy(g => g.from))
            {
                FillSegment(like, start, dir, outward, rot, cursor, g.from, 0, CorridorHeight, pieceLen, ref index);
                cursor = g.to;
            }
            FillSegment(like, start, dir, outward, rot, cursor, length, 0, CorridorHeight, pieceLen, ref index);
        }

        static void TileRect(GameObject asset, string name, float x0, float x1, float z0, float z1, float y, bool ceiling, Transform parent)
        {
            var probe = Spawn(asset, parent, "probe");
            var size = GetBounds(probe).size;
            Object.DestroyImmediate(probe);
            if (size.x < 0.05f || size.z < 0.05f) return;
            int index = 0;
            for (float x = x0; x < x1 - 0.02f; x += size.x)
            for (float z = z0; z < z1 - 0.02f; z += size.z)
            {
                float lx = Mathf.Min(size.x, x1 - x), lz = Mathf.Min(size.z, z1 - z);
                var go = Spawn(asset, parent, $"{name}_{index++}");
                var s = go.transform.localScale;
                s.x *= lx / size.x; s.z *= lz / size.z;
                go.transform.localScale = s;
                go.transform.position = Vector3.zero;
                var b = GetBounds(go);
                var target = new Vector3(x + lx / 2, ceiling ? y + b.extents.y : -b.extents.y, z + lz / 2);
                go.transform.position += target - b.center;
            }
        }

        static void SignFacing(string name, string texture, Vector3 position, Vector3 towardViewer, float width, float height, Transform parent)
        {
            var quad = MakeQuad(name, texture, width, height, parent);
            quad.transform.position = position;
            quad.transform.rotation = Quaternion.LookRotation(-towardViewer);
        }

        static void SetDoorLocked(GameObject door, string text)
        {
            var doorType = KitType("Door");
            if (doorType == null) return;
            var c = door.GetComponentInChildren(doorType, true);
            if (c == null) return;
            var so = new SerializedObject(c);
            var state = so.FindProperty("lockState");
            if (state != null) state.enumValueIndex = 1; // Locked
            var locked = so.FindProperty("lockedText");
            if (locked != null) locked.stringValue = text;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        // A sheet of glass you can walk through. No collider, faintly tinted, seen from both sides.
        static void BuildMirrorGlass(Transform root)
        {
            var mat = TransparentMaterial("MirrorGlass", new Color(0.55f, 0.62f, 0.62f, 0.08f), 0.85f);
            var centre = new Vector3(MirrorAlong, mirrorGlassSize.y / 2, MirrorPlaneZ);
            foreach (var (name, face) in new[] { ("GlassRoomSide", Vector3.back), ("GlassMirrorSide", Vector3.forward) })
            {
                var q = GameObject.CreatePrimitive(PrimitiveType.Quad);
                q.name = name;
                Object.DestroyImmediate(q.GetComponent<Collider>());
                q.transform.SetParent(root, false);
                q.transform.position = centre;
                q.transform.rotation = Quaternion.LookRotation(-face);
                q.transform.localScale = new Vector3(mirrorGlassSize.x, mirrorGlassSize.y, 1);
                var r = q.GetComponent<Renderer>();
                r.sharedMaterial = mat;
                r.shadowCastingMode = ShadowCastingMode.Off;
            }

            // Floor under the mirror, across the wall's thickness, so walking through has no gap.
            var floorAsset = AssetDatabase.LoadAssetAtPath<GameObject>($"{RoomPrefabFolder}/RoomFloor.prefab");
            if (floorAsset != null)
                TileRect(floorAsset, "MirrorThreshold", MirrorAlong - mirrorGlassSize.x / 2 - 0.1f, MirrorAlong + mirrorGlassSize.x / 2 + 0.1f,
                    Depth / 2 - 0.02f, Depth / 2 + wallThick + 0.02f, 0f, false, root);

            // A thin dark frame so it reads as a mirror, not an open doorway.
            var frameMat = GetMaterial("MirrorFrame", false, new Color(0.18f, 0.13f, 0.08f));
            float t = 0.05f, w = mirrorGlassSize.x, h = mirrorGlassSize.y;
            foreach (var (name, pos, size) in new[]
            {
                ("FrameTop", new Vector3(0, h / 2 - t / 2, 0), new Vector3(w, t, 0.04f)),
                ("FrameLeft", new Vector3(-w / 2 + t / 2, 0, 0), new Vector3(t, h, 0.04f)),
                ("FrameRight", new Vector3(w / 2 - t / 2, 0, 0), new Vector3(t, h, 0.04f)),
            })
            {
                var c = GameObject.CreatePrimitive(PrimitiveType.Cube);
                c.name = name;
                Object.DestroyImmediate(c.GetComponent<Collider>());
                c.transform.SetParent(root, false);
                c.transform.position = centre + pos;
                c.transform.localScale = size;
                c.GetComponent<Renderer>().sharedMaterial = frameMat;
            }
        }

        static Material TransparentMaterial(string name, Color colour, float smoothness)
        {
            string path = $"{MaterialFolder}/{name}.mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat == null)
            {
                mat = new Material(Shader.Find("Universal Render Pipeline/Lit"));
                AssetDatabase.CreateAsset(mat, path);
            }
            mat.SetColor("_BaseColor", colour);
            mat.SetFloat("_Smoothness", smoothness);
            mat.SetFloat("_Surface", 1f);
            mat.SetFloat("_Blend", 0f);
            mat.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
            mat.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
            mat.SetFloat("_ZWrite", 0f);
            mat.SetFloat("_Cull", 0f);
            mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            mat.SetOverrideTag("RenderType", "Transparent");
            mat.renderQueue = (int)RenderQueue.Transparent;
            EditorUtility.SetDirty(mat);
            return mat;
        }

        // Everything in Room C copied and flipped through the mirror plane, as plain scenery: the kit's
        // scripts are stripped so nothing behind the glass can be used. A few things are left out on
        // purpose, so the "reflection" is wrong: the safe and key cabinet are missing (the wardrobe stands
        // there) and the door you came in by becomes the real exit.
        static readonly HashSet<string> NotMirrored = new HashSet<string> { "RoomCDoor", "WallSafe", "KeyCabinet", "DoorKeypad" };

        static void BuildMirrorSide(Transform root)
        {
            colliders = Child(root.parent, "MirrorSideColliders");
            root.position = new Vector3(0, 0, 2 * MirrorPlaneZ);
            root.localScale = new Vector3(1, 1, -1);

            foreach (var groupName in new[] { "Structure", "Dressing", "Props", "Lighting" })
            {
                var group = GameObject.Find($"Room/{groupName}");
                if (group == null) continue;
                var copyGroup = Child(root, groupName);
                foreach (Transform child in group.transform)
                {
                    var n = child.name;
                    if (n.StartsWith("Wall_Back") || n == "Doorway_Mirror" || NotMirrored.Contains(n)) continue;
                    if (groupName == "Lighting" && n == "DawnEnding") continue;
                    var copy = Object.Instantiate(child.gameObject, copyGroup, false);
                    copy.name = n;
                    StripScripts(copy);
                    if (groupName != "Lighting") { SetStatic(copy); ReplaceColliders(copy, colliders); }
                }
            }
        }

        // Colliders don't work under a flipped (negative) scale, so each mirrored piece loses its own
        // and gets plain boxes, unflipped, in "MirrorSideColliders". Doorways keep their opening.
        static Transform colliders;

        static void ReplaceColliders(GameObject copy, Transform root)
        {
            var cols = copy.GetComponentsInChildren<Collider>(true);
            if (cols.Length == 0) return;
            var b = GetBounds(copy);
            foreach (var c in cols) Object.DestroyImmediate(c);

            if (!copy.name.StartsWith("Doorway_")) { Box(root, copy.name, b.min, b.max); return; }

            // Leave a door-sized hole: boxes either side of it and one above.
            float doorWidth = mirrorGlassSize.x / 0.9f, doorHeight = mirrorGlassSize.y / 0.96f;
            bool alongX = b.size.x >= b.size.z;
            float c0 = alongX ? b.center.x : b.center.z, lo = alongX ? b.min.x : b.min.z, hi = alongX ? b.max.x : b.max.z;
            Vector3 Pt(float along, float y, bool max) => alongX
                ? new Vector3(along, y, max ? b.max.z : b.min.z)
                : new Vector3(max ? b.max.x : b.min.x, y, along);
            Box(root, copy.name + "_A", Pt(lo, b.min.y, false), Pt(c0 - doorWidth / 2, b.max.y, true));
            Box(root, copy.name + "_B", Pt(c0 + doorWidth / 2, b.min.y, false), Pt(hi, b.max.y, true));
            Box(root, copy.name + "_Top", Pt(c0 - doorWidth / 2, doorHeight, false), Pt(c0 + doorWidth / 2, b.max.y, true));
        }

        static void Box(Transform root, string name, Vector3 min, Vector3 max)
        {
            var go = new GameObject(name);
            go.transform.SetParent(root, false);
            var lo = Vector3.Min(min, max); var hi = Vector3.Max(min, max);
            go.transform.position = (lo + hi) / 2;
            var box = go.AddComponent<BoxCollider>();
            box.size = Vector3.Max(hi - lo, Vector3.one * 0.01f);
            GameObjectUtility.SetStaticEditorFlags(go, StaticEditorFlags.BatchingStatic);
        }

        static void StripScripts(GameObject go)
        {
            for (int pass = 0; pass < 3; pass++)
                foreach (var mb in go.GetComponentsInChildren<MonoBehaviour>(true))
                    if (mb != null) Object.DestroyImmediate(mb);
        }

        // Spots for the puzzle on the mirror side. Bruno's wardrobe and rack replace the placeholders;
        // the exit door is the kit's steel door until Gama's exit variant exists.
        static void PlacePuzzleSpots(Transform root)
        {
            float zm2 = 2 * MirrorPlaneZ;               // mirror image of z is zm2 - z
            float midZ = zm2;                           // the mirror side's centre
            var placeholder = TransparentMaterial("PlaceholderBlue", new Color(0.3f, 0.55f, 0.95f, 0.35f), 0.2f);

            Spot(root, "WardrobeSpot", new Vector3(Width / 2 - 0.32f, 0, zm2 + 0.85f), Vector3.left, new Vector3(1.1f, 2.0f, 0.6f), placeholder);
            Spot(root, "RackSpot", new Vector3(-1.5f, 0, midZ + 0.3f), Vector3.right, new Vector3(1.4f, 1.7f, 0.5f), placeholder);

            // The real exit: a steel door in the doorway the mirror makes of Room C's entrance.
            var exitAsset = PropAsset("ExitDoor", new[] { "doorsteel" });
            GameObject exit = null;
            if (exitAsset != null)
            {
                exit = Spawn(exitAsset, root, "ExitDoor");
                var inward = Vector3.back;
                exit.transform.SetPositionAndRotation(Vector3.zero, Quaternion.LookRotation(inward));
                var b = GetBounds(exit);
                var centre = new Vector3(0, b.extents.y, zm2 + Depth / 2 + wallThick / 2);
                exit.transform.position += centre - b.center;
                SetDoorLocked(exit, "Locked. Something here still isn't right.");
            }

            // Dawn behind the exit, switched off until the exit opens (Gama's story beat turns it on).
            var dawn = Child(root, "DawnEnding");
            var doorCentre = exit ? GetBounds(exit).center : new Vector3(0, 1f, zm2 + Depth / 2);
            var outside = Vector3.forward;
            var sunGo = new GameObject("DawnLight");
            sunGo.transform.SetParent(dawn, false);
            var sun = sunGo.AddComponent<Light>();
            sun.type = LightType.Spot; sun.spotAngle = 80; sun.range = 9f; sun.intensity = 6f;
            sun.color = new Color(1f, 0.78f, 0.55f);
            sun.shadows = LightShadows.Soft;
            sun.lightmapBakeType = LightmapBakeType.Realtime;
            sunGo.transform.position = new Vector3(doorCentre.x, 2.0f, doorCentre.z) + outside * 2.5f;
            sunGo.transform.rotation = Quaternion.LookRotation((-outside + Vector3.down * 0.25f).normalized);

            var glow = GameObject.CreatePrimitive(PrimitiveType.Quad);
            glow.name = "DawnSky";
            Object.DestroyImmediate(glow.GetComponent<Collider>());
            glow.GetComponent<MeshRenderer>().sharedMaterial = GetMaterial("DawnSky", true, new Color(1f, 0.82f, 0.6f));
            glow.GetComponent<MeshRenderer>().shadowCastingMode = ShadowCastingMode.Off;
            glow.transform.SetParent(dawn, false);
            glow.transform.localScale = new Vector3(3f, 3f, 1f);
            glow.transform.position = new Vector3(doorCentre.x, 1.4f, doorCentre.z) + outside * 1.2f;
            glow.transform.rotation = Quaternion.LookRotation(outside);
            dawn.gameObject.SetActive(false);
        }

        static void Spot(Transform root, string name, Vector3 floorPoint, Vector3 facing, Vector3 size, Material mat)
        {
            var spot = new GameObject(name).transform;
            spot.SetParent(root, false);
            spot.SetPositionAndRotation(floorPoint, Quaternion.LookRotation(facing));
            var box = GameObject.CreatePrimitive(PrimitiveType.Cube);
            box.name = "Placeholder (delete when the real one is in)";
            Object.DestroyImmediate(box.GetComponent<Collider>());
            box.transform.SetParent(spot, false);
            box.transform.localPosition = new Vector3(0, size.y / 2, 0);
            box.transform.localScale = new Vector3(size.x, size.y, size.z);
            box.GetComponent<Renderer>().sharedMaterial = mat;
            box.GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.Off;
        }
    }
}
