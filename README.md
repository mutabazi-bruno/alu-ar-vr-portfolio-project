# The Last Shift

A short VR escape room made in Unity for our AR/VR portfolio project at ALU.

You stayed late to return a box of old records and the door locked behind you. Now you're stuck overnight in Records Room B of an old campus archive, and the only way out is to follow the notes left by the night caretaker, Joseph Habimana. Getting out of Room B is only half of it: the door opens onto Room C, which looks the same but isn't quite right. Two rooms, and no timer.

It runs in the browser on a desktop, and in a VR headset through WebXR.

## Links

- Play in the browser: coming soon (itch.io)
- DevLog: coming soon
- Video walkthrough: coming soon

## Team

| Member | Role |
|---|---|
| Mutabazi Ishimwe Bruno | Repo setup, WebGL build and itch.io page, README, end card scene |
| Leslie (group leader) | Room layout, lighting, labels and textures, screenshots, video walkthrough |
| Gama | Items, locks, notes and objectives, battery and settings scripts, DevLog |
| Brian | XR rig and rig switcher, gaze interaction, world-space HUD, subtitles and accessibility |

## How to play

On desktop:

| Key | Action |
|---|---|
| WASD + mouse | Move and look around |
| E | Use, pick up, open |
| F | Flashlight |
| Tab | Inventory (you can turn items over to examine them) |
| Esc | Pause menu |

VR controls will be added here once the XR rig is in.

## Accessibility

- Puzzle clues never rely on colour alone. Labels carry a shape and a year, and keys and hooks carry numbers.
- Every important sound has a subtitle.
- No timer. The flashlight battery can be switched off in the pause menu.
- The objective line always tells you what to try next, so it doubles as a hint.
- Notes are short and shown in large, high-contrast text.
- In VR everything can be reached seated, with teleport and snap turn.

## Running the project

1. Install **Unity 6000.4.7f1** with WebGL Build Support. Add Android Build Support too if you want to build for Quest.
2. Clone this repo.
3. Download **Escape Puzzle Kit Free 1.1.0** by heyheythere from itch.io (https://heyheythere.itch.io). It isn't in this repo because its licence doesn't allow sharing its files.
4. Open the project in Unity Hub (Add > Add project from disk). If Unity offers to enter Safe Mode because the kit is missing, choose Ignore.
5. Import the kit: Assets > Import Package > Custom Package, pick `escape-puzzle-kit-free-1.1.0.unitypackage` and import everything. It goes into `Assets/EscapePuzzleKitFree/`.
6. Open `Assets/_LastShift/Scenes/Title.unity` and press Play. To skip the menu while testing, open `RecordsRoomB.unity` instead.

## Web build

1. File > Build Profiles, pick Web and click Build. Choose an empty folder outside the repo.
2. Zip what's inside that folder (`index.html` has to be at the top of the zip, not inside another folder).
3. On itch.io, upload the zip and tick "This file will be played in the browser". Set the embed size to 960 x 600 and turn on the fullscreen button.

The project is set to Gzip compression with decompression fallback, which is what lets the build load on itch.io. If you change it back to Brotli the page stays stuck on a blank screen.

Saves live in the browser, so they survive a page reload but not a new upload to itch.io (each upload gets a new address).

## Project layout

```
Assets/
  _LastShift/            everything we made
    Scenes/  Prefabs/  Scripts/  Items/  Art/  Audio/
  EscapePuzzleKitFree/   the kit, imported locally, not tracked by git
  Settings/              URP settings
```

## Working on the project

- Don't edit anything inside `EscapePuzzleKitFree/`. If you need to change a kit prefab, make a prefab variant and save it in `_LastShift/Prefabs/`.
- Everyone uses the same kit version (1.1.0). Our prefabs point at the kit's files by their IDs, so a different version can break them.
- Work on your own branch and open a pull request into `main`. Keep `main` in a state that opens and plays.
- Only one person edits a scene at a time. Unity scene files don't merge well, so say in the group chat before you change `RecordsRoomB.unity` or `RoomC.unity`.

## End card

When the player opens the way out, the game moves to `EndCard.unity`, which shows their time, the credits, and Play again and Quit buttons. For this to happen, the exit door's Door component needs `endsGame` ticked. Nothing else has to go in the room scene, because `EndCardLoader` picks up the kit's escape event by itself.

Play again starts a fresh run (no items, flags or saved state). On the web build, Quit shows a "you can close this tab" line, since a web page can't close its own tab.

## Clothes puzzle (Room C)

The player re-hangs the caretaker's clothes on the rack in the right order and puts on his cap. It works the same on desktop and in VR:

- Desktop, or the VR pointer: use a piece to pick it up, then use a hook to hang it. Using a hook that already has something on it swaps the two. Use the cap to put it on.
- VR hands: grab a piece with grip and let go with the top of the hanger at a hook, and it snaps on. Lift the cap to your head to wear it.

The prefabs are in `Prefabs/Clothes/`: `ClothesRack` (four hooks and the puzzle), `Coat`, `Shirt`, `Scarf` and `Cap`. The clothes are placeholder shapes for now. To set it up in a scene:

1. Drop in `ClothesRack`, the clothes and the cap. Keep the clothes away from the hooks at the start.
2. On each hook, set `answer` to the piece that belongs there. Leave it empty for a hook that should stay empty.
3. Every piece needs its own `pieceId`, because saves use it.

When every piece is on the right hook, the puzzle sets the flag `clothes_solved` and locks the rack. Wearing the cap is a separate step that sets `cap_worn`, and the exit in Room C waits for both. The cap can't be taken until `clothes_solved` is set (its `lockedUntil` field), so nobody can grab it through the side of the locked wardrobe. If you want the rack itself to wait for the cap, tick `needsCap` on the rack and clear `lockedUntil` on the cap. `Scenes/Sandbox/Bruno.unity` has a working setup to try it in.

## What came from the kit and what we built

**From Escape Puzzle Kit Free:** the interaction, inventory, doors and locks systems, story beats and saves, the HUD, the prop and set piece models, and the sounds.

**Built by us:** the room layout, the story, notes and codes, the items and their textures, the labels and signs, the battery toggle and settings menu, the end card, the clothes puzzle, the rig switcher, VR interaction and the world-space HUD.

## Credits

- Escape Puzzle Kit Free and PSX Horror SFX by heyheythere (https://heyheythere.itch.io), used under the kit's licence.
