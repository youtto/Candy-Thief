# Tech spec — Candy Thief v0.2

This file constrains implementation. Gameplay numbers live in CSV / `Docs/GDD.md`, not here.

## Project (keep)

| Item | Current |
|---|---|
| Unity | `6000.6.3f1` |
| Project folder | `CandyThief/` |
| Render | URP 2D |
| Input | Input System (`activeInputHandler` 1) |
| UI modules | `com.unity.ugui` 2.6.0 (includes TextMeshPro) |
| MCP | `com.coplaydev.unity-mcp` |
| 2D extras | sprite, tilemap, animation — keep installed, not required by M1 UI |

Do not add packages for M1. Do not create a second Unity project.

## Current prototype (do not delete)

Existing v0.1 / art prototype stays in the repo and is not the v0.2 loop.

| Area | What exists |
|---|---|
| Scene | `CandyThief/Assets/Scenes/SampleScene.unity` — top-down room, player, NPCs, tilemap |
| Movement | `PlayerController` — WASD / Input System, `Rigidbody2D`, camera follow |
| Visuals | `CharacterVisualAnimator`, `YSortRenderer`, Ninja Adventure temp sprites |
| Editor | `NinjaAdventureSetup` |
| Settings | `defaultScreenOrientation` Auto Rotation (4); landscape-sized default game view |

v0.2 adds new scenes/scripts. It does not remove these files.

## Target runtime

| Item | v0.2 |
|---|---|
| Device | Android first (min SDK already 26 in PlayerSettings) |
| Screen | Portrait lock, reference 1080×1920 |
| Input | Touch on device; mouse on editor UI |
| Build | APK in M5, not WebGL |

Portrait lock and Android player settings are M5 (and a small M1 editor Game View setup if needed). They are not part of this documentation-only pass.

## Architecture (planned)

Keep it small. No extra frameworks. Match existing C# style: no namespace, serialized fields, few types.

```
Title / ShopSelect / Result  (uGUI screens)
        ↓
   RunController             (phase + end checks)
        ↓
   ActionResolver  EventResolver
        ↓
   RunState                  (turns, alert, candy)
        ↓
   CsvDatabase               (id → row)
        ↓
   EffectApplier             (effect_type from EffectTable)
```

### CsvDatabase

- Load UTF-8 BOM (`utf-8-sig`) CSV with a header row from `Resources/Table/` (`Resources.Load`).
- Gameplay files: `GameConfig`, `ShopTable`, `PhaseTable`, `ActionTable`, `EventTable`, `EventOptionTable`, `AchievementTable`, `AssetTable`, `EffectTable`. Do not load `README.csv` as a table.
- Index by the CSV PK (`Key`, `ShopID`, `PhaseID`, `ActionID`, `EventID`, `OptionID`, `AchievementID`, `AssetID`/`ImageKey`, `EffectID`).
- Fail loud on duplicate or missing ID. Skip rows with `Enabled=0`. Do not apply `EffectTable` rows with `Implemented=0`.
- Gameplay code asks for IDs, never for `ActionName` / `ShopName` text.

### RunState

Fields: `ShopID`, `PhaseID`, remaining turns, alert, candy, last result.  
MVP mutations use Action/EventOption columns. Do not route through `EffectTable` until `Implemented=1`.

### Resolvers

Order: subtract `TurnCost` → success/fail deltas → win/fail → event if alive.

- Success chance `%` = clamp(`SuccessRate` + `SuccessRateBonusPp`, `MIN_SUCCESS_RATE`, `MAX_SUCCESS_RATE`).
- Event chance `%` = clamp(`EVENT_TRIGGER_RATE` + `EventRateBonusPp`, 0, 100).
- Candy gain: `SuccessCandy` × `CandyMultiplier` (rounding P-012).
- Required fail: keep the action choosable in the same phase.
- Event only if `EventEligible=1` (and fail-path if `EVENT_ON_FAILED_ACTION=1`). Filter tags/phase/alert/turns, then weighted pick. After an option, stop.

RNG: `UnityEngine.Random` unless P-006 requires a seed.

### UI

- New Canvas scene or a dedicated UI object in a new scene (`TEMP` name: `RunUI`). Do not reuse `SampleScene` as the production HUD.
- Canvas Scaler: Scale With Screen Size, 1080×1920, match width or height `TBD` (`TEMP` match width 0.5).
- Safe Area wrapper on the root.
- One `ChoiceList` widget for required actions, optional actions, and event options.
- Top bar: turns, alert, candy. Bind to `RunState`.
- M1: TMP text and default buttons. No final illustration.

## Save (M4)

`TEMP`: `PlayerPrefs` keys for best score, cumulative candy/runs, achievement id list.  
Do not store run power-ups. Cosmetic currency cannot modify `ActionTable` chances.

## Reuse vs new

**Reuse as-is**

- Unity version, URP, Input System package, uGUI/TMP, MCP, Git
- Editor Play Mode as the M1 test harness
- Existing art only as optional `AssetTable` placeholders

**Reuse with adaptation**

- Input System: UI clicks/touches, not `Player/Move` for the v0.2 loop
- uGUI: new portrait canvas, not a world-space stealth HUD

**Do not reuse for the v0.2 loop**

- `PlayerController` WASD locomotion
- Tilemap collision / guard-less NPCs as gameplay
- Detection/FOV (never implemented; not in v0.2)

**Must implement**

- CSV loader and table types
- `RunState` / phase machine
- Action + event resolvers and effect types
- Portrait choice UI + Safe Area
- Title / shop / result (M2)
- Local records (M4)
- Android portrait APK (M5)

## Coding rules for later implementation

- Fewest new files. No unsolicited refactors of `PlayerController` or `SampleScene`.
- No balance constants in C# except enum `effect_type` names.
- Do not claim Play Mode, MCP, or device tests unless actually run.
- Owner approval before new packages or deleting old assets.
