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

Fields: `ShopID`, `CurrentPhase`, remaining turns, alert, integer run candy, last action result, `RunStatus` (`InProgress` / `Won` / `Failed`), pending `EventID`, `successfulActionIds`, `successfulEventIds`.  
Start from a `ShopID` via `CsvDatabase` (`StartTurn`, `StartAlert`, candy 0, first `PhaseTable` row by `PhaseOrder`). Unknown shop IDs fail loud. M1 test default shop is `SHOP_001` (D-010), not a production auto-select. A new run clears event success history and pending.

Mutations use Action/EventOption column values passed in by later resolvers. `RunState` does not roll success chance. Do not store `OptionID` as `LastActionID`. Do not route through `EffectTable` until `Implemented=1`. After `Won`/`Failed`, reject further mutation. Pending event blocks action select/apply.

Clamps: turns and candy ≥ 0; alert in `[0, GameConfig.MAX_ALERT]`. Limits come from tables/config, not literals in C#.

Candy (D-009):

- During the run, candy is an `int`. Add table candy deltas as integers. Do not apply `CandyMultiplier` here.
- `finalReward` is not a RunState field in M1-3. Later escape/result work computes `CeilToInt(runCandy × Shop.CandyMultiplier)` on escape success, else 0.

End check (D-011), after TurnCost and deltas:

1. `Alert >= MAX_ALERT` → `Failed` (`AlertLimit`), including when this action was a successful escape
2. Else escape-phase action success and candy ≥ `MIN_CANDY_TO_ESCAPE` → `Won` (allowed if remaining turns are 0)
3. Else remaining turns ≤ 0 → `Failed` (`NoTurns`)
4. Else stay `InProgress`; successful non-winning actions may apply `SuccessNextPhase`

Selection: `TurnCost` > remaining turns is rejected with no state change. Equal cost is allowed.

### Resolvers

Order: reject if `TurnCost` > remaining → subtract `TurnCost` → success/fail deltas → end check (above) → event if still `InProgress`.

- Success chance `%` = clamp(`SuccessRate` + `SuccessRateBonusPp`, `MIN_SUCCESS_RATE`, `MAX_SUCCESS_RATE`). `ActionResolver` computes and rolls this (M1-4). Inject `IChanceRoll` in tests. Event options use the same formula on `EventOptionTable.SuccessRate` (D-015).
- Event chance `%` = clamp(`EVENT_TRIGGER_RATE` + `EventRateBonusPp`, 0, 100). `ActionResolver` reports whether an event check is due; `EventResolver` rolls trigger chance, filters candidates, and `Weight`-picks (M1-7). Do not draw inside `ActionResolver.TryResolve`.
- Candy during run: add integer `SuccessCandy` / `FailCandy` (and event-option candy). Do **not** multiply by `CandyMultiplier` per action (D-009).
- Escape success (later): `finalReward = CeilToInt(runCandy × CandyMultiplier)`. Escape fail: `finalReward = 0`.
- Required fail: keep the action choosable in the same phase while `InProgress`.
- Repeatable column: D-013 (post-success reappearance, not attempt count). `EventResolver` applies this to `EventID` via `successfulEventIds`, never mixed with ActionIDs.
- Event only if `EventEligible=1` (and fail-path if `EVENT_ON_FAILED_ACTION=1`). `PhaseFilter` uses the pre-action phase. Alert/turn windows are inclusive and use post-action state. After an option, stop. Pending event blocks further actions. Options are not escape actions.

RNG: `UnityEngine.Random` unless P-006 requires a seed.

### UI

- v0.2 start scene: `Assets/Scenes/V02/Title.unity` (D-012). Do not reuse `SampleScene` as the production HUD. `SampleScene` stays in the project, not first in Build Settings.
- Hub screens in that scene: Title, Lobby, Shop Select (D-012). After shop pick, instantiate `Resources/V02/UI/RunHud` under the same Canvas SafeArea (M1-8).
- Canvas Scaler: Scale With Screen Size, 1080×1920, match width or height 0.5.
- Safe Area wrapper on the hub root. Run HUD stretches inside it.
- Prefabs: `RunHud.prefab` + `ChoiceButton.prefab`. Layout is Inspector-editable. Choice count Instantiates from CSV.
- One `ChoiceList` for required actions, optional actions, and event options. `IsActionPresented` / `IsOptionPresented` separate hide vs `InsufficientTurns` disable. Execute still uses `EvaluateSelect` / `EvaluateSelectOption`.
- Top bar: turns, alert, candy. Center: phase or event copy, placeholder illustration via `ImageKey`. Bind to `RunState` + resolvers.
- Result feedback (M1-8.5): overlay modal with reaction sprite, wrapping body, snapshot deltas, `[계속]`. Presentation IDs live in `ResultFeedbackTable`; sprites are Inspector/`Resources/V02/Feedback` mapped. Rebuild menu asks before overwriting prefabs; use Patch to keep Inspector slots.
- M1: TMP text and default buttons. Event PNG files are not shipped; placeholder remains. Result screen is M1-9.

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
