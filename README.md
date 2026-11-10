# The Last Shift

A short VR escape room made in Unity for our AR/VR portfolio project at ALU.

You stayed late to return a box of old records and the door locked behind you. Now you're stuck overnight in Records Room B of an old campus archive, and the only way out is to follow the notes left by the night caretaker, Joseph Habimana. One room, eight puzzles, about 10 minutes to play, and no timer.

It runs in the browser on a desktop, and in a VR headset through WebXR.

## Links

- Play in the browser: coming soon (itch.io)
- DevLog: coming soon
- Video walkthrough: coming soon

## Team

| Member | Role |
|---|---|
| Mutabazi Ishimwe Bruno | Repo setup, WebGL build and itch.io page, README, end card scene |
| to add | Room layout, lighting, labels and textures, screenshots, video walkthrough |
| to add | Items, locks, notes and objectives, battery and settings scripts, DevLog |
| to add | XR rig and rig switcher, gaze interaction, world-space HUD, subtitles and accessibility |

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
6. Open `Assets/_LastShift/Scenes/RecordsRoomB.unity` and press Play.

To make a web build: File > Build Profiles > Web > Build.

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
- Only one person edits a scene at a time. Unity scene files don't merge well, so say in the group chat before you change `RecordsRoomB.unity`.

## What came from the kit and what we built

**From Escape Puzzle Kit Free:** the interaction, inventory, doors and locks systems, story beats and saves, the HUD, the prop and set piece models, and the sounds.

**Built by us:** the room layout, the story, notes and codes, the items and their textures, the labels and signs, the battery toggle and settings menu, the end card, the rig switcher, VR interaction and the world-space HUD.

## Credits

- Escape Puzzle Kit Free and PSX Horror SFX by heyheythere (https://heyheythere.itch.io), used under the kit's licence.
