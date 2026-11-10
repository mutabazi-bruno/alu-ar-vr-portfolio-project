using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

namespace LastShift.EditorTools
{
    // Builds Records Room B out of the kit's set pieces and props.
    // Run it from the menu: Last Shift > Build Room B.
    // It only deletes and rebuilds the "Room" object in the scene, so it is safe to run again
    // after changing a number below.
    public static class RoomLayoutBuilder
    {
        const string KitFolder = "Assets/EscapePuzzleKitFree";
        const string ScenePath = "Assets/_LastShift/Scenes/RecordsRoomB.unity";
        const string RoomPrefabFolder = "Assets/_LastShift/Prefabs/Room";
        const string PropVariantFolder = "Assets/_LastShift/Prefabs/Props";
        const string TextureFolder = "Assets/_LastShift/Art/Textures";
        const string MaterialFolder = "Assets/_LastShift/Art/Materials";
        const string ModelFolder = "Assets/_LastShift/Art/Models";

        // Room size in metres, centred on the origin. The player starts near the front wall facing +Z.
        const float Width = 6f;
        const float Depth = 5f;

        // If a kit prop ends up facing the wall, change its number here (degrees) and rebuild.
        static readonly Dictionary<string, float> YawFix = new Dictionary<string, float>
        {
            { "Workbench", 0 }, { "Chest", 0 }, { "Drawers", 0 }, { "StaffLocker", 0 },
            { "WallSafe", 0 }, { "KeyCabinet", 0 }, { "VentCover", 0 }, { "DoorKeypad", 0 },
            { "Shelving", 0 }, { "SteelDoor", 0 }, { "WoodenDoor", 0 }, { "Crate", 0 }, { "Box", 0 },
        };

        // Set to true if the brick walls are only visible from outside the room.
        static readonly bool FlipWalls = false;

        static List<string> kitPrefabPaths;
        static Transform structureRoot, propsRoot, dressingRoot, lightingRoot, testRoot;

        enum Side { Back, Right, Front, Left }

        // ---------------------------------------------------------------- menu

        [MenuItem("Last Shift/Build Room B")]
        public static void Build()
        {
            if (!AssetDatabase.IsValidFolder(KitFolder))
            {
                EditorUtility.DisplayDialog("Kit missing",
                    "Import Escape Puzzle Kit Free 1.1.0 first (it should land in Assets/EscapePuzzleKitFree).", "OK");
                return;
            }

            EnsureFolder(RoomPrefabFolder);
            EnsureFolder(MaterialFolder);
            EnsureFolder(ModelFolder);
            LoadKitPrefabList();

            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

            var old = GameObject.Find("Room");
            if (old != null) Object.DestroyImmediate(old);

            var room = new GameObject("Room").transform;
            structureRoot = Child(room, "Structure");
            propsRoot = Child(room, "Props");
            dressingRoot = Child(room, "Dressing");
            lightingRoot = Child(room, "Lighting");
            testRoot = Child(room, "TestOnly");

            BuildBadgeModel();
            openings.Clear();
            MeasureWall();

            // Doors go in first so the walls can leave a gap (a doorway piece) for them.
            var steelDoor = PropAsset("SteelDoor", new[] { "doorsteel", "steeldoor" });
            var woodenDoor = PropAsset("WoodenDoor", new[] { "doorwood", "woodendoor", "wooddoor" });
            var steel = steelDoor ? PlaceDoor(steelDoor, "SteelDoor", Side.Front, 0f) : null;
            var wooden = woodenDoor ? PlaceDoor(woodenDoor, "WoodenDoor", Side.Left, -1.5f) : null;

            BuildShell();
            PlaceProps(steel, wooden, wallHeight);
            PlaceLighting(steel, wallHeight);
            AddRoomVolume();
            PlaceTestPlayer();

            var dirLight = GameObject.Find("Directional Light");
            if (dirLight != null) dirLight.SetActive(false);

            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.035f, 0.035f, 0.045f);
            RenderSettings.reflectionIntensity = 0.3f;

            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            Debug.Log("[RoomLayoutBuilder] Records Room B built and saved.");
        }

        [MenuItem("Last Shift/List Kit Prefabs")]
        public static void ListKitPrefabs()
        {
            LoadKitPrefabList();
            Debug.Log("[RoomLayoutBuilder] Kit prefabs:\n" + string.Join("\n", kitPrefabPaths));
        }

        // ---------------------------------------------------------------- finding kit prefabs

        static void LoadKitPrefabList()
        {
            kitPrefabPaths = AssetDatabase.FindAssets("t:Prefab", new[] { KitFolder })
                .Select(AssetDatabase.GUIDToAssetPath).OrderBy(p => p).ToList();
        }

        static string Key(string path) =>
            Path.GetFileNameWithoutExtension(path).ToLowerInvariant().Replace(" ", "").Replace("_", "").Replace("-", "");

        // Finds a kit prefab whose name contains one of the phrases, or all of the words in mustHaveAll.
        static GameObject FindKit(string[] phrases, string[] mustHaveAll = null, string[] exclude = null)
        {
            IEnumerable<string> hits = kitPrefabPaths.Where(p =>
            {
                string k = Key(p);
                if (exclude != null && exclude.Any(k.Contains)) return false;
                if (phrases.Any(k.Contains)) return true;
                return mustHaveAll != null && mustHaveAll.All(k.Contains);
            });
            // Prefer the shortest name, which is usually the plain prop rather than a demo set-up.
            string best = hits.OrderBy(p => Key(p).Length).FirstOrDefault();
            if (best == null)
            {
                Debug.LogWarning("[RoomLayoutBuilder] No kit prefab matched: " + string.Join(", ", phrases));
                return null;
            }
            return AssetDatabase.LoadAssetAtPath<GameObject>(best);
        }

        // Gama's prop variant if it exists yet, otherwise the kit prefab as a stand-in.
        static GameObject PropAsset(string variantName, string[] phrases, string[] mustHaveAll = null, string[] exclude = null)
        {
            var variant = AssetDatabase.LoadAssetAtPath<GameObject>($"{PropVariantFolder}/{variantName}.prefab");
            return variant != null ? variant : FindKit(phrases, mustHaveAll, exclude);
        }

        // Room pieces (walls, shelving, boxes...) are saved as our own prefab variants.
        static GameObject RoomVariant(string name, GameObject kitPrefab)
        {
            if (kitPrefab == null) return null;
            string path = $"{RoomPrefabFolder}/{name}.prefab";
            var existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (existing != null) return existing;

            var temp = (GameObject)PrefabUtility.InstantiatePrefab(kitPrefab);
            var variant = PrefabUtility.SaveAsPrefabAsset(temp, path);
            Object.DestroyImmediate(temp);
            return variant;
        }

        // ---------------------------------------------------------------- geometry helpers

        static Transform Child(Transform parent, string name)
        {
            var t = new GameObject(name).transform;
            t.SetParent(parent, false);
            return t;
        }

        static GameObject Spawn(GameObject asset, Transform parent, string name)
        {
            var go = (GameObject)PrefabUtility.InstantiatePrefab(asset);
            go.name = name;
            go.transform.SetParent(parent, true);
            return go;
        }

        static Bounds GetBounds(GameObject go)
        {
            var renderers = go.GetComponentsInChildren<Renderer>().Where(r => r.enabled && !(r is ParticleSystemRenderer)).ToArray();
            if (renderers.Length > 0)
            {
                var b = renderers[0].bounds;
                foreach (var r in renderers) b.Encapsulate(r.bounds);
                return b;
            }
            var colliders = go.GetComponentsInChildren<Collider>();
            if (colliders.Length > 0)
            {
                var b = colliders[0].bounds;
                foreach (var c in colliders) b.Encapsulate(c.bounds);
                return b;
            }
            return new Bounds(go.transform.position, Vector3.zero);
        }

        // Inward normal of each wall, and a point on its inner face.
        static Vector3 Inward(Side s) => s switch
        {
            Side.Back => Vector3.back,
            Side.Front => Vector3.forward,
            Side.Left => Vector3.right,
            _ => Vector3.left,
        };

        // 'along' is the world X (back/front walls) or world Z (left/right walls) of the spot on that wall.
        static Vector3 WallPoint(Side s, float along) => s switch
        {
            Side.Back => new Vector3(along, 0, Depth / 2),
            Side.Front => new Vector3(along, 0, -Depth / 2),
            Side.Left => new Vector3(-Width / 2, 0, along),
            _ => new Vector3(Width / 2, 0, along),
        };

        static float HalfExtentAlong(Bounds b, Vector3 dir) =>
            Mathf.Abs(b.extents.x * dir.x) + Mathf.Abs(b.extents.y * dir.y) + Mathf.Abs(b.extents.z * dir.z);

        static Quaternion FacingRoom(Side s, string key)
        {
            float fix = YawFix.TryGetValue(key, out var f) ? f : 0f;
            return Quaternion.LookRotation(Inward(s)) * Quaternion.Euler(0, -fix, 0);
        }

        // Puts a prop with its back against a wall. centreHeight = null stands it on the floor.
        static GameObject PlaceAgainst(GameObject asset, Transform parent, string name, Side side, float along, float? centreHeight)
        {
            if (asset == null) return null;
            var go = Spawn(asset, parent, name);
            go.transform.rotation = FacingRoom(side, name);
            go.transform.position = Vector3.zero;

            var n = Inward(side);
            var b = GetBounds(go);
            var p = WallPoint(side, along);
            var target = p + n * (HalfExtentAlong(b, n) + 0.01f);
            target.y = centreHeight ?? b.extents.y;
            go.transform.position += target - b.center;
            return go;
        }

        static bool IsWallMounted(GameObject asset)
        {
            var temp = (GameObject)PrefabUtility.InstantiatePrefab(asset);
            float h = GetBounds(temp).size.y;
            Object.DestroyImmediate(temp);
            return h < 1.0f;
        }

        // ---------------------------------------------------------------- walls, floor, ceiling

        static GameObject wallAsset, doorwayAsset;
        static float wallHeight = 2.8f, wallThick = 0.2f;
        static readonly List<(Side side, float along, float half, float top, bool doorway)> openings =
            new List<(Side, float, float, float, bool)>();

        static void MeasureWall()
        {
            wallAsset = RoomVariant("RoomWall", FindKit(new[] { "wallbrick", "brickwall" },
                new[] { "wall" }, new[] { "safe", "door", "vent", "post", "stained", "lamp", "sign" }));
            doorwayAsset = RoomVariant("RoomDoorway", FindKit(new[] { "walldoorway", "doorway" }));
            if (wallAsset == null)
            {
                Debug.LogWarning("[RoomLayoutBuilder] No wall piece found. Check Last Shift > List Kit Prefabs.");
                return;
            }
            var probe = Spawn(wallAsset, structureRoot, "probe");
            probe.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
            var size = GetBounds(probe).size;
            wallThick = Mathf.Min(size.x, size.z);
            wallHeight = size.y;
            Object.DestroyImmediate(probe);
        }

        // Lays a wall piece along a wall: its long side along the wall line.
        static Quaternion PieceRotation(Side side, GameObject asset)
        {
            WallLine(side, out _, out var dir);
            var probe = Spawn(asset, structureRoot, "probe");
            probe.transform.SetPositionAndRotation(Vector3.zero, Quaternion.identity);
            var size = GetBounds(probe).size;
            Object.DestroyImmediate(probe);
            var rot = size.x >= size.z ? Quaternion.LookRotation(Vector3.Cross(dir, Vector3.up)) : Quaternion.LookRotation(dir);
            if (FlipWalls) rot = Quaternion.AngleAxis(180, Vector3.up) * rot;
            return rot;
        }

        // A door standing in its own doorway piece, in the middle of the wall's thickness.
        static GameObject PlaceDoor(GameObject doorAsset, string name, Side side, float along)
        {
            WallLine(side, out _, out var dir);
            var outward = -Inward(side);
            var centre = WallPoint(side, along) + outward * (wallThick / 2);

            float half = 0f;
            bool hasDoorway = doorwayAsset != null;
            if (hasDoorway)
            {
                var doorway = Spawn(doorwayAsset, structureRoot, $"Doorway_{name}");
                doorway.transform.rotation = PieceRotation(side, doorwayAsset);
                doorway.transform.position = Vector3.zero;
                var db = GetBounds(doorway);
                var t = centre; t.y = db.extents.y;
                doorway.transform.position += t - db.center;
                half = HalfExtentAlong(GetBounds(doorway), dir);
            }

            var door = Spawn(doorAsset, propsRoot, name);
            door.transform.rotation = FacingRoom(side, name);
            door.transform.position = Vector3.zero;
            var b = GetBounds(door);
            var target = centre; target.y = b.extents.y;
            door.transform.position += target - b.center;
            b = GetBounds(door);
            if (!hasDoorway) half = HalfExtentAlong(b, dir) + 0.01f;

            openings.Add((side, along, half, b.max.y, hasDoorway));
            return door;
        }

        static void BuildShell()
        {
            var floorAsset = RoomVariant("RoomFloor", FindKit(new[] { "floor" }));
            var ceilingAsset = RoomVariant("RoomCeiling", FindKit(new[] { "ceiling" }, null, new[] { "lamp", "light", "fan" }));

            if (wallAsset != null)
                foreach (Side s in new[] { Side.Back, Side.Right, Side.Front, Side.Left })
                    BuildWall(s);

            if (floorAsset != null) TileFlat(floorAsset, "Floor", 0f, false);
            if (ceilingAsset != null) TileFlat(ceilingAsset, "Ceiling", wallHeight, false);
            else if (floorAsset != null) TileFlat(floorAsset, "Ceiling", wallHeight, true);

            foreach (Transform t in structureRoot)
                SetStatic(t.gameObject);
        }

        // Each wall runs clockwise seen from above.
        static void WallLine(Side s, out Vector3 start, out Vector3 dir)
        {
            float w = Width / 2, d = Depth / 2;
            switch (s)
            {
                case Side.Back: start = new Vector3(-w, 0, d); dir = Vector3.right; break;
                case Side.Right: start = new Vector3(w, 0, d); dir = Vector3.back; break;
                case Side.Front: start = new Vector3(w, 0, -d); dir = Vector3.left; break;
                default: start = new Vector3(-w, 0, -d); dir = Vector3.forward; break;
            }
        }

        static void BuildWall(Side side)
        {
            WallLine(side, out var start, out var dir);
            float length = (side == Side.Back || side == Side.Front) ? Width : Depth;
            var outward = -Inward(side);
            var rot = PieceRotation(side, wallAsset);

            var probe = Spawn(wallAsset, structureRoot, "probe");
            probe.transform.SetPositionAndRotation(Vector3.zero, rot);
            float pieceLen = HalfExtentAlong(GetBounds(probe), dir) * 2;
            Object.DestroyImmediate(probe);

            var gaps = openings.Where(o => o.side == side)
                .Select(o =>
                {
                    float t = Vector3.Dot(WallPoint(side, o.along) - start, dir);
                    return (from: t - o.half, to: t + o.half, o.top, o.doorway);
                })
                .OrderBy(g => g.from).ToList();

            float cursor = 0;
            int index = 0;
            foreach (var g in gaps)
            {
                FillSegment(side, start, dir, outward, rot, cursor, g.from, 0, wallHeight, pieceLen, ref index);
                if (!g.doorway && g.top < wallHeight - 0.02f)
                    FillSegment(side, start, dir, outward, rot, g.from, g.to, g.top, wallHeight, pieceLen, ref index);
                cursor = g.to;
            }
            FillSegment(side, start, dir, outward, rot, cursor, length, 0, wallHeight, pieceLen, ref index);
        }

        static void FillSegment(Side side, Vector3 start, Vector3 dir, Vector3 outward, Quaternion rot,
            float from, float to, float bottom, float top, float pieceLen, ref int index)
        {
            float t = Mathf.Max(0, from);
            while (to - t > 0.02f)
            {
                float len = Mathf.Min(pieceLen, to - t);
                var go = Spawn(wallAsset, structureRoot, $"Wall_{side}_{index++}");
                go.transform.SetPositionAndRotation(Vector3.zero, rot);

                // Stretch the last piece (or the strip over a door) to fit exactly.
                var s = go.transform.localScale;
                var size = GetBounds(go).size;
                float sx = len / (HalfExtentAlong(GetBounds(go), dir) * 2);
                float sy = (top - bottom) / size.y;
                var localAlong = Quaternion.Inverse(rot) * dir;
                if (Mathf.Abs(localAlong.x) > Mathf.Abs(localAlong.z)) s.x *= sx; else s.z *= sx;
                s.y *= sy;
                go.transform.localScale = s;

                var b = GetBounds(go);
                var target = start + dir * (t + len / 2) + outward * (wallThick / 2);
                target.y = (bottom + top) / 2;
                go.transform.position += target - b.center;
                t += len;
            }
        }

        static void TileFlat(GameObject asset, string name, float y, bool flip)
        {
            var probe = Spawn(asset, structureRoot, "probe");
            if (flip) probe.transform.rotation = Quaternion.Euler(180, 0, 0);
            var size = GetBounds(probe).size;
            Object.DestroyImmediate(probe);
            if (size.x < 0.05f || size.z < 0.05f) return;

            int index = 0;
            for (float x = -Width / 2; x < Width / 2 - 0.02f; x += size.x)
            for (float z = -Depth / 2; z < Depth / 2 - 0.02f; z += size.z)
            {
                float lx = Mathf.Min(size.x, Width / 2 - x), lz = Mathf.Min(size.z, Depth / 2 - z);
                var go = Spawn(asset, structureRoot, $"{name}_{index++}");
                go.transform.rotation = flip ? Quaternion.Euler(180, 0, 0) : Quaternion.identity;
                var s = go.transform.localScale;
                s.x *= lx / size.x; s.z *= lz / size.z;
                go.transform.localScale = s;
                go.transform.position = Vector3.zero;
                var b = GetBounds(go);
                var target = new Vector3(x + lx / 2, 0, z + lz / 2);
                // The floor's top sits at 0; the ceiling's underside sits at the wall height.
                target.y = name == "Floor" ? -b.extents.y : y + b.extents.y;
                go.transform.position += target - b.center;
            }
        }

        static void SetStatic(GameObject go)
        {
            foreach (var t in go.GetComponentsInChildren<Transform>())
                GameObjectUtility.SetStaticEditorFlags(t.gameObject,
                    StaticEditorFlags.ContributeGI | StaticEditorFlags.BatchingStatic |
                    StaticEditorFlags.OccluderStatic | StaticEditorFlags.OccludeeStatic);
        }

        // ---------------------------------------------------------------- props and dressing

        static void PlaceProps(GameObject steelDoor, GameObject woodenDoor, float wallHeight)
        {
            // Back wall: chest, workbench with the flashlight, vent cover low down, a crate in the corner.
            PlaceAgainst(PropAsset("Chest", new[] { "chest" }), propsRoot, "Chest", Side.Back, -2.0f, null);
            var bench = PlaceAgainst(RoomVariant("RoomWorkbench", FindKit(new[] { "workbench", "bench" })), dressingRoot, "Workbench", Side.Back, 0.2f, null);
            PlaceAgainst(PropAsset("VentCover", new[] { "ventcover", "vent" }), propsRoot, "VentCover", Side.Back, 1.6f, 0.35f);
            var crate = PlaceAgainst(RoomVariant("RoomCrate", FindKit(new[] { "crate" })), dressingRoot, "Crate", Side.Back, 2.4f, null);
            if (crate) SetStatic(crate);
            if (bench) { SetStatic(bench); PutFlashlightOn(bench); }

            // Left wall: drawers, the staff locker, and the decoy wooden door further along.
            PlaceAgainst(PropAsset("Drawers", new[] { "cabinetdrawers", "drawer" }), propsRoot, "Drawers", Side.Left, 1.3f, null);
            PlaceAgainst(PropAsset("StaffLocker", new[] { "locker" }), propsRoot, "StaffLocker", Side.Left, 0.1f, null);

            // Right wall: shelving with the six archive boxes, the wall safe and the key cabinet.
            var shelving = PlaceAgainst(RoomVariant("RoomShelving", FindKit(new[] { "shelving", "shelves", "shelf" })),
                dressingRoot, "Shelving", Side.Right, 1.2f, null);
            if (shelving) { SetStatic(shelving); FillShelving(shelving); }

            var safeAsset = PropAsset("WallSafe", new[] { "safewall", "wallsafe", "safe" });
            if (safeAsset) PlaceAgainst(safeAsset, propsRoot, "WallSafe", Side.Right, -0.3f, IsWallMounted(safeAsset) ? 1.25f : (float?)null);

            var cabinetAsset = PropAsset("KeyCabinet", new[] { "keycabinet" });
            GameObject cabinet = null;
            if (cabinetAsset) cabinet = PlaceAgainst(cabinetAsset, propsRoot, "KeyCabinet", Side.Right, -1.4f, IsWallMounted(cabinetAsset) ? 1.35f : (float?)null);
            if (cabinet)
            {
                var cb = GetBounds(cabinet);
                float w = HalfExtentAlong(cb, Vector3.forward) * 2 * 0.9f;
                Decal("HookNumbers", "HookNumbers", Side.Right, -1.4f, cb.max.y + 0.07f, w, w / 8f, cabinet.transform);
            }

            // Front wall: keypad next to the steel door, sign-in sheet and calendar on the other side.
            PlaceAgainst(PropAsset("DoorKeypad", new[] { "keypad" }), propsRoot, "DoorKeypad", Side.Front, 0.85f, 1.3f);
            Decal("SignInSheet", "SignInSheet", Side.Front, -0.9f, 1.45f, 0.30f, 0.42f, dressingRoot);
            Decal("CalendarNovember", "CalendarNovember", Side.Front, -1.5f, 1.5f, 0.32f, 0.32f, dressingRoot);

            float doorTop = steelDoor ? GetBounds(steelDoor).max.y : 2.1f;
            Decal("DoorSignRecordsRoomB", "DoorSignRecordsRoomB", Side.Front, 0f, Mathf.Min(doorTop + 0.2f, wallHeight - 0.2f), 0.6f, 0.3f, dressingRoot);

            if (woodenDoor)
            {
                var db = GetBounds(woodenDoor);
                var n = Inward(Side.Left);
                var quad = MakeQuad("DoorSignStorage", "DoorSignStorage", 0.55f, 0.275f, woodenDoor.transform);
                quad.transform.position = new Vector3(db.center.x, 1.55f, db.center.z) + n * (HalfExtentAlong(db, n) + 0.004f);
                quad.transform.rotation = Quaternion.LookRotation(-n);
            }
        }

        static void PutFlashlightOn(GameObject bench)
        {
            var asset = PropAsset("Flashlight", new[] { "flashlight", "torch" });
            if (asset == null) return;
            float top = SurfaceBelow(bench, GetBounds(bench).center + Vector3.left * 0.3f) ?? GetBounds(bench).max.y;
            var go = Spawn(asset, propsRoot, "Flashlight");
            go.transform.rotation = Quaternion.Euler(0, 75, 0);
            go.transform.position = Vector3.zero;
            var b = GetBounds(go);
            var bc = GetBounds(bench).center;
            go.transform.position += new Vector3(bc.x - 0.3f, top + b.extents.y + 0.005f, bc.z) - b.center;
        }

        // Highest collider surface of 'owner' under the given point, if any.
        static float? SurfaceBelow(GameObject owner, Vector3 point)
        {
            Physics.SyncTransforms();
            var b = GetBounds(owner);
            var hits = Physics.RaycastAll(new Vector3(point.x, b.max.y + 0.5f, point.z), Vector3.down, b.size.y + 1f);
            var mine = hits.Where(h => h.collider.transform.IsChildOf(owner.transform)).OrderByDescending(h => h.point.y).ToList();
            return mine.Count > 0 ? mine[0].point.y : (float?)null;
        }

        static readonly (string symbol, int year)[,] ShelfGrid =
        {
            { ("Cross", 1968), ("Triangle", 1987) },
            { ("Wave", 2011), ("Circle", 1994) },
            { ("Star", 1979), ("Square", 2003) },
        };

        static void FillShelving(GameObject shelving)
        {
            var boxAsset = RoomVariant("RoomArchiveBox", FindKit(new[] { "cardboardbox", "box" }, null, new[] { "fuse", "key" }));
            if (boxAsset == null) return;

            var n = Inward(Side.Right);
            var viewerRight = Vector3.Cross(Vector3.up, -n);
            var sb = GetBounds(shelving);
            float shelfWidth = HalfExtentAlong(sb, viewerRight) * 2;
            float shelfDepth = HalfExtentAlong(sb, n) * 2;

            // Find the shelf boards: the heights where the shelving mesh has large upward-facing surfaces.
            var levels = ShelfLevels(shelving);
            var usable = levels.Where(y => y > 0.3f && y < 1.75f).Take(3).ToList();
            if (usable.Count < 3)
            {
                Debug.LogWarning("[RoomLayoutBuilder] Couldn't find 3 shelf boards, spacing boxes evenly instead.");
                usable = new List<float> { sb.min.y + sb.size.y * 0.72f, sb.min.y + sb.size.y * 0.42f, sb.min.y + sb.size.y * 0.12f };
            }

            for (int row = 0; row < 3; row++)
            {
                float gap = row == 0 ? 0.4f : Mathf.Max(0.2f, usable[row - 1] - usable[row]);
                for (int col = 0; col < 2; col++)
                {
                    var (symbol, year) = ShelfGrid[row, col];
                    var box = Spawn(boxAsset, dressingRoot, $"ArchiveBox{symbol}{year}");
                    box.transform.rotation = FacingRoom(Side.Right, "Box");
                    box.transform.position = Vector3.zero;
                    var bb = GetBounds(box);
                    float scale = Mathf.Min(1f,
                        gap * 0.8f / bb.size.y,
                        shelfWidth * 0.42f / (HalfExtentAlong(bb, viewerRight) * 2),
                        shelfDepth * 0.9f / (HalfExtentAlong(bb, n) * 2));
                    box.transform.localScale *= scale;
                    bb = GetBounds(box);

                    float sideSign = col == 0 ? -1f : 1f; // column 0 is the viewer's left
                    var target = sb.center + viewerRight * (shelfWidth * 0.25f * sideSign);
                    target += n * (shelfDepth / 2f + 0.02f - HalfExtentAlong(bb, n));
                    target.y = usable[row] + bb.extents.y + 0.002f;
                    box.transform.position += target - bb.center;
                    bb = GetBounds(box);
                    SetStatic(box);

                    float faceW = HalfExtentAlong(bb, viewerRight) * 2;
                    float w = Mathf.Min(faceW * 0.85f, 0.3f);
                    float hgt = Mathf.Min(w / 2f, bb.size.y * 0.8f);
                    var label = MakeQuad($"BoxLabel{symbol}{year}", $"BoxLabel{symbol}{year}", hgt * 2f, hgt, box.transform);
                    label.transform.position = bb.center + n * (HalfExtentAlong(bb, n) + 0.003f);
                    label.transform.rotation = Quaternion.LookRotation(-n);
                }
            }
        }

        static List<float> ShelfLevels(GameObject shelving)
        {
            var area = new Dictionary<int, float>();
            foreach (var mf in shelving.GetComponentsInChildren<MeshFilter>())
            {
                var mesh = mf.sharedMesh;
                if (mesh == null) continue;
                var m = mf.transform.localToWorldMatrix;
                var v = mesh.vertices;
                var tris = mesh.triangles;
                for (int i = 0; i < tris.Length; i += 3)
                {
                    Vector3 a = m.MultiplyPoint3x4(v[tris[i]]), b = m.MultiplyPoint3x4(v[tris[i + 1]]), c = m.MultiplyPoint3x4(v[tris[i + 2]]);
                    var n = Vector3.Cross(b - a, c - a);
                    float triArea = n.magnitude / 2f;
                    if (triArea < 1e-6f || n.normalized.y < 0.95f) continue;
                    int key = Mathf.RoundToInt((a.y + b.y + c.y) / 3f / 0.02f);
                    area[key] = (area.TryGetValue(key, out var x) ? x : 0f) + triArea;
                }
            }
            // Merge heights closer than 15 cm, then keep only the big flat surfaces: the boards themselves,
            // not the tops of the clutter modelled onto the shelving.
            var merged = new List<(float y, float area)>();
            foreach (var kv in area.OrderByDescending(kv => kv.Key))
            {
                float y = kv.Key * 0.02f;
                if (merged.Count > 0 && merged[merged.Count - 1].y - y <= 0.15f)
                {
                    var last = merged[merged.Count - 1];
                    merged[merged.Count - 1] = (last.area >= kv.Value ? last.y : y, last.area + kv.Value);
                }
                else merged.Add((y, kv.Value));
            }
            float biggest = merged.Count > 0 ? merged.Max(m => m.area) : 0f;
            var levels = merged.Where(m => m.area >= biggest * 0.6f).Select(m => m.y).ToList();
            Debug.Log("[RoomLayoutBuilder] Shelf surfaces (height:area): " + string.Join(", ", merged.Select(m => $"{m.y:F2}:{m.area:F2}"))
                + " -> boards " + string.Join(", ", levels.Select(l => l.ToString("F2"))));
            return levels;
        }

        // ---------------------------------------------------------------- flat textured quads

        static Material GetMaterial(string textureName, bool unlit = false, Color? colour = null)
        {
            string path = $"{MaterialFolder}/{textureName}.mat";
            var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (mat != null) return mat;

            var shader = Shader.Find(unlit ? "Universal Render Pipeline/Unlit" : "Universal Render Pipeline/Lit");
            mat = new Material(shader);
            var tex = AssetDatabase.LoadAssetAtPath<Texture2D>($"{TextureFolder}/{textureName}.png");
            if (tex != null)
            {
                mat.SetTexture("_BaseMap", tex);
                var importer = (TextureImporter)AssetImporter.GetAtPath($"{TextureFolder}/{textureName}.png");
                if (importer != null && importer.anisoLevel < 4) { importer.anisoLevel = 4; importer.SaveAndReimport(); }
            }
            mat.SetColor("_BaseColor", colour ?? Color.white);
            if (!unlit) mat.SetFloat("_Smoothness", 0.12f);
            AssetDatabase.CreateAsset(mat, path);
            return mat;
        }

        static GameObject MakeQuad(string name, string textureName, float width, float height, Transform parent)
        {
            var quad = GameObject.CreatePrimitive(PrimitiveType.Quad);
            quad.name = name;
            Object.DestroyImmediate(quad.GetComponent<Collider>());
            quad.GetComponent<MeshRenderer>().sharedMaterial = GetMaterial(textureName);
            quad.GetComponent<MeshRenderer>().shadowCastingMode = ShadowCastingMode.Off;
            quad.transform.SetParent(parent, false);
            quad.transform.localScale = Vector3.one;
            var ls = parent.lossyScale;
            quad.transform.localScale = new Vector3(width / ls.x, height / ls.y, 1f / ls.z);
            return quad;
        }

        // A flat sign on a wall's inner face.
        static void Decal(string name, string textureName, Side side, float along, float centreHeight, float width, float height, Transform parent)
        {
            var quad = MakeQuad(name, textureName, width, height, parent);
            var n = Inward(side);
            var p = WallPoint(side, along);
            p.y = centreHeight;
            quad.transform.position = p + n * 0.006f;
            quad.transform.rotation = Quaternion.LookRotation(-n);
        }

        // ---------------------------------------------------------------- the ID badge model

        // Two quads back to back, so the front and the "HOOK 6" back each show only on their own side.
        static void BuildBadgeModel()
        {
            string path = $"{ModelFolder}/StaffIDBadge.prefab";
            var root = new GameObject("StaffIDBadge");
            var col = root.AddComponent<BoxCollider>();
            col.size = new Vector3(0.086f, 0.054f, 0.003f);

            // The front faces +Z, Unity's forward, which is the side the kit's examine view shows first.
            // "HOOK 6" only appears once the player turns the badge over.
            var front = MakeQuad("Front", "BadgeFront", 0.086f, 0.054f, root.transform);
            front.transform.localPosition = new Vector3(0, 0, 0.0006f);
            front.transform.localRotation = Quaternion.Euler(0, 180, 0);

            var back = MakeQuad("Back", "BadgeBack", 0.086f, 0.054f, root.transform);
            back.transform.localPosition = new Vector3(0, 0, -0.0006f);
            back.transform.localRotation = Quaternion.identity;

            PrefabUtility.SaveAsPrefabAsset(root, path);
            Object.DestroyImmediate(root);
        }

        // ---------------------------------------------------------------- lighting

        static void PlaceLighting(GameObject steelDoor, float wallHeight)
        {
            // Weak fill so the room isn't pitch black without the flashlight.
            var fill = NewLight("FillLight", LightType.Point, new Color(0.72f, 0.78f, 0.9f), 0.6f, 8f);
            fill.transform.position = new Vector3(0, wallHeight - 0.3f, 0);
            fill.shadows = LightShadows.None;

            // A faint pool of light on the workbench, where the opening says something glints.
            var bench = GameObject.Find("Workbench");
            if (bench != null)
            {
                var spot = NewLight("WorkbenchGlint", LightType.Spot, new Color(1f, 0.85f, 0.65f), 1.2f, 3f);
                spot.spotAngle = 50;
                var bb = GetBounds(bench);
                spot.transform.position = new Vector3(bb.center.x, bb.max.y + 1.4f, bb.center.z);
                spot.transform.rotation = Quaternion.LookRotation(Vector3.down);
                spot.shadows = LightShadows.None;
            }

            // Dawn outside the steel door. Starts switched off; the door keypad's success event turns
            // "DawnEnding" on (Gama wires that in his pass).
            var dawn = Child(lightingRoot, "DawnEnding");
            var doorCentre = steelDoor ? GetBounds(steelDoor).center : new Vector3(0, 1f, -Depth / 2);
            var outside = -Inward(Side.Front);

            var sun = NewLight("DawnLight", LightType.Spot, new Color(1f, 0.78f, 0.55f), 6f, 9f);
            sun.transform.SetParent(dawn, true);
            sun.spotAngle = 80;
            sun.shadows = LightShadows.Soft;
            sun.transform.position = new Vector3(doorCentre.x, 2.0f, doorCentre.z) + outside * 2.5f;
            sun.transform.rotation = Quaternion.LookRotation((Inward(Side.Front) + Vector3.down * 0.25f).normalized);

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

        static Light NewLight(string name, LightType type, Color colour, float intensity, float range)
        {
            var go = new GameObject(name);
            go.transform.SetParent(lightingRoot, false);
            var light = go.AddComponent<Light>();
            light.type = type;
            light.color = colour;
            light.intensity = intensity;
            light.range = range;
            light.lightmapBakeType = LightmapBakeType.Realtime;
            return light;
        }

        // ---------------------------------------------------------------- test player

        // The kit's scripts are looked up by name, so this file still compiles before the kit is imported.
        static System.Type KitType(string name) =>
            System.Type.GetType($"HeyHeyThere.EscapePuzzleKitFree.{name}, HeyHeyThere.EscapePuzzleKitFree");

        static void SetRef(Component c, string field, Object value)
        {
            var so = new SerializedObject(c);
            var prop = so.FindProperty(field);
            if (prop == null) return;
            prop.objectReferenceValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        // A walker built from the kit's own components, only so the room can be walked through now.
        // Brian's player replaces it later: delete Room/TestOnly then.
        static void PlaceTestPlayer()
        {
            var walkerType = KitType("FirstPersonWalker");
            var interactorType = KitType("Interactor");
            var flashlightType = KitType("Flashlight");
            var hudType = KitType("Hud");
            if (walkerType == null || interactorType == null)
            {
                Debug.LogWarning("[RoomLayoutBuilder] Kit scripts not found, so no test player was added.");
                return;
            }

            var player = new GameObject("TestPlayer");
            player.transform.SetParent(testRoot, false);
            player.transform.SetPositionAndRotation(new Vector3(0, 0.05f, -1.6f), Quaternion.LookRotation(Vector3.forward));
            var cc = player.AddComponent<CharacterController>();
            cc.height = 1.75f; cc.radius = 0.3f; cc.center = new Vector3(0, 0.875f, 0);

            var view = new GameObject("View");
            view.transform.SetParent(player.transform, false);
            view.transform.localPosition = new Vector3(0, 1.6f, 0);
            var cam = view.AddComponent<Camera>();
            cam.nearClipPlane = 0.03f;
            view.AddComponent<AudioListener>();
            view.tag = "MainCamera";
            view.AddComponent(interactorType);

            var walker = player.AddComponent(walkerType);
            SetRef(walker, "view", view.transform);

            if (flashlightType != null)
            {
                var lamp = new GameObject("Flashlight");
                lamp.transform.SetParent(view.transform, false);
                lamp.transform.localPosition = new Vector3(0.15f, -0.12f, 0.1f);
                var light = lamp.AddComponent<Light>();
                light.type = LightType.Spot;
                light.range = 12f; light.spotAngle = 55f; light.intensity = 4f;
                light.color = new Color(1f, 0.95f, 0.85f);
                light.shadows = LightShadows.Soft;
                light.enabled = false;
                var fl = lamp.AddComponent(flashlightType);
                SetRef(fl, "item", AssetDatabase.LoadAssetAtPath<Object>($"{KitFolder}/Items/flashlight.asset"));
            }

            if (hudType != null)
            {
                var hudGo = new GameObject("TestHud");
                hudGo.transform.SetParent(testRoot, false);
                var hud = hudGo.AddComponent(hudType);
                var items = AssetDatabase.FindAssets("t:ScriptableObject", new[] { $"{KitFolder}/Items", "Assets/_LastShift/Items" })
                    .Select(g => AssetDatabase.LoadAssetAtPath<ScriptableObject>(AssetDatabase.GUIDToAssetPath(g)))
                    .Where(o => o != null && o.GetType().Name == "Item").ToArray();
                var so = new SerializedObject(hud);
                var cat = so.FindProperty("catalogue");
                if (cat != null)
                {
                    cat.arraySize = items.Length;
                    for (int i = 0; i < items.Length; i++) cat.GetArrayElementAtIndex(i).objectReferenceValue = items[i];
                    so.ApplyModifiedPropertiesWithoutUndo();
                }
            }

            var main = GameObject.Find("Main Camera");
            if (main != null) main.SetActive(false);
        }

        // A trigger naming the room for the save slots.
        static void AddRoomVolume()
        {
            var roomType = KitType("Room");
            if (roomType == null) return;
            var go = new GameObject("RoomVolume");
            go.transform.SetParent(dressingRoot.parent, false);
            var box = go.AddComponent<BoxCollider>();
            box.isTrigger = true;
            box.center = new Vector3(0, wallHeight / 2, 0);
            box.size = new Vector3(Width, wallHeight, Depth);
            var room = go.AddComponent(roomType);
            var so = new SerializedObject(room);
            var title = so.FindProperty("title");
            if (title != null) { title.stringValue = "Records Room B"; so.ApplyModifiedPropertiesWithoutUndo(); }
        }

        static void EnsureFolder(string path)
        {
            if (AssetDatabase.IsValidFolder(path)) return;
            string parent = Path.GetDirectoryName(path).Replace('\\', '/');
            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, Path.GetFileName(path));
        }
    }
}
