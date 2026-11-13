# Candy Thief — Agent & development notes

Gameplay source of truth is `Docs/GDD.md`. Approved changes live in `Docs/DECISIONS.md` (D-019 for UI authoring).

## Editor-editable screens (D-019)

Title, lobby, shop, result, and other hub screens must be tunable in Unity Editor **without Play Mode**.

1. Open the screen’s scene or Prefab and see the layout in Edit Mode.
2. Place fixed UI and art in the scene or Prefab. Do not spawn them in `Awake`/`Start` just to draw the screen.
3. Each widget (background, character, HUD piece, button) is a named Hierarchy object that can be selected.
4. Position, size, rotation, anchors, and sprites are Inspector fields. Save the scene/Prefab.
5. Runtime code binds data and clicks. It must not reset RectTransforms on play.
6. Repeating / data-driven widgets (shop rows, choice lists, toasts) may instantiate from Prefabs.
7. Reuse the existing scene/Prefab. Do not add a second copy of the same screen.
8. Edit Mode layout and Play Mode layout must match.
9. When adding a screen, verify Edit Mode visibility before calling the work done.

Lobby edit scene: `CandyThief/Assets/Scenes/V02/Lobby.unity`  
Lobby Prefab: `CandyThief/Assets/Prefabs/V02/UI/Lobby.prefab`  
Title + lobby instance: `CandyThief/Assets/Scenes/V02/Title.unity`
