# Candy Thief — GDD v0.2

Status: Owner-approved working spec. Unconfirmed values are marked `TEMP` or `TBD`. Owner approval is required before changing rules.

Current gameplay source of truth. Implementation follows this file plus `Docs/DECISIONS.md`.

## Document history

| Version | Status | Summary |
|---|---|---|
| v0.1 | Superseded | 2D top-down shop stealth. WASD, guard FOV, key/gold candy, WebGL. Archive: [Appendix A](#appendix-a-superseded-v01-spec). |
| v0.2 | Current | Mobile portrait probability-choice roguelite. Push Your Luck, CSV-driven, Android APK first. |

Change reason is recorded in `Docs/DECISIONS.md` (D-001).

---

## Pitch

사탕 가게에 잠입해, 턴과 경계도를 깎아가며 행동을 고르는 단판형 Push Your Luck 게임. 성공 확률을 보고 더 가져갈지, 지금 탈출할지 고른다. 한 판은 3~5분.

## Target

| Item | Spec |
|---|---|
| Project | Candy Thief |
| Version | v0.2 |
| Engine | Unity 6 (`6000.6.3f1`), existing project `CandyThief/` |
| Genre | 확률 선택형 로그라이트, Push Your Luck, 단판 |
| Platform | Android first. Portrait fixed. Reference resolution 1080×1920. Touch. PC editor test supported. |
| Release | 1st: Android APK. Later: Google Play review. |
| Playtime | 1 run 3~5 minutes |
| Persistence | Run resets. Achievements/titles, best score, cumulative records persist. Cosmetic currency only. |

Do not create a new Unity project. Do not delete existing scenes, scripts, or assets.

## Core loop

Title → Shop Selection → Move → Infiltrate → Loot → Escape → Result → Retry / Title

Shop selection happens at run start. Each `ShopTable` row changes:

- `StartTurn`, `StartAlert`
- `SuccessRateBonusPp` (percentage points added to action success)
- `CandyMultiplier`
- `EventRateBonusPp` and `AllowedEventTags`

Phase order after shop select follows `PhaseTable.PhaseOrder`: MOVE → INFILTRATE → LOOT → ESCAPE → RESULT. In a phase with several `ActionType=REQUIRED` rows, the player picks **one**. One success with `CompleteCondition=REQUIRED_ACTION_SUCCESS` advances via `SuccessNextPhase` / `NextPhaseID`.

LOOT stays in LOOT after a successful steal (`SuccessNextPhase=LOOT`) so the player can steal again (Push Your Luck). `ACT_013` (`OPTIONAL`, `ShowCondition=CANDY_AT_LEAST_1`) moves to ESCAPE. ESCAPE success is a win.

## Win / fail

Win (all required):

- candy acquired ≥ 1
- Escape action succeeds

Fail (either):

- remaining turns reach 0
- alert ≥ `GameConfig.MAX_ALERT` (100)

End-condition checks beat event spawning. If a resolved action or option already meets win/fail, do not roll or present an event.

Resolve order (from table README, do not reorder in code):

1. Subtract action/option `TurnCost`
2. Apply success or fail candy / alert / turn deltas
3. Win/fail check
4. If the run is still live, event check

## Actions

Actions are data rows in `ActionTable`, referenced by ID.

| Rule | Spec |
|---|---|
| Types | `ActionType`: `REQUIRED` or `OPTIONAL` |
| Success | `clamp(SuccessRate + Shop.SuccessRateBonusPp, MIN_SUCCESS_RATE, MAX_SUCCESS_RATE)` — values are **percent 0–100**, not 0–1 |
| Turn cost | `TurnCost` subtracted on attempt, then `SuccessTurnDelta` / `FailTurnDelta` |
| On failure | `FailAlert`, `FailCandy`, `FailTurnDelta` from the action row |
| On success | `SuccessCandy` × `Shop.CandyMultiplier` (rounding `TBD`, pick one method and keep it), `SuccessAlert`, `SuccessTurnDelta` |
| Required failure | Same-phase retry while the run is live (`Repeatable=1` on current rows) |
| Optional | Current table: only `ACT_013` (leave LOOT → ESCAPE when candy ≥ `MIN_CANDY_TO_ESCAPE`). Not a skip of MOVE/INFILTRATE |
| Event after action | Only if `EventEligible=1`. `ACT_013` is 0 |
| Display | Show chance, turn cost, expected reward/penalty from data. Do not hardcode |

Do not put probabilities, turn costs, or alert deltas in C#. Enabled rows only (`Enabled=1`).

## Events

| Rule | Spec |
|---|---|
| When | After a general action with `EventEligible=1`, success or fail (`EVENT_ON_FAILED_ACTION=1`), if the run is still live |
| Chance | `clamp(EVENT_TRIGGER_RATE + Shop.EventRateBonusPp, 0, 100)` percent |
| Cap | `MAX_EVENT_PER_ACTION` (1). `EVENT_CHAIN_ENABLED=0` |
| Candidates | `Enabled=1` rows matching `PhaseFilter` (or `ALL`), overlapping `EventTags` ∩ `Shop.AllowedEventTags` (`\|` separator), alert/turns windows, then `Weight` pick |
| Options | `EventOptionTable` for that `EventID`. MVP applies option columns (`SuccessRate`, `TurnCost`, candy/alert/turn deltas). `SuccessEffectID`/`FailEffectID` are `NONE` |
| Chain | After an option resolves, no second event |
| Priority | Run-end check > event |

M1 uses every `EventTable` row with `Enabled=1` (currently EVT_001–EVT_005), not a hardcoded count of 3.

## Score

```
score = candy_acquired × risk_bonus
```

`risk_bonus` formula is `TBD` (balance in M3).  
TEMP for M1–M2: use `GameConfig.BASE_SCORE_MULTIPLIER` (1). Do not ship this as final.

Gold-candy-on-clear from v0.1 is superseded. v0.2 score uses run candy and risk bonus only.

## Meta

| Item | Spec |
|---|---|
| Run | Fully reset each attempt |
| Permanent stats | None |
| Achievements / titles | Permanent |
| Records | Best score and cumulative stats saved locally |
| Currency | Permanent currency is cosmetics-only. Name `TBD`. TEMP id: `cosmetic_point` |

No account, no server, no gacha, no companion growth.

## Data-driven content

Content is CSV. Code looks up rows by ID, never by display name or localized text.

Required tables:

- `GameConfig`
- `ShopTable`
- `PhaseTable`
- `ActionTable`
- `EventTable`
- `EventOptionTable`
- `AchievementTable`
- `AssetTable`
- `EffectTable`

Source files: `CandyThief/Assets/Resources/Table/*.csv` (UTF-8 BOM). Treat `README.csv` as loader notes, not a gameplay table. Numeric cells are pre-balance TEMP values. New effect *types* may need code; numeric tuning must not.

Condition tokens (`NONE`, `ALWAYS`, `CANDY_AT_LEAST_1`, `REQUIRED_ACTION_SUCCESS`, `MOVE_COMPLETED`, `INFILTRATE_COMPLETED`, `GAME_ENDED`, …) are code enums, not free-form scripts.

### Table files (locked to the CSVs)

**GameConfig** — `Key`, `Value`, `ValueType`, `Description`, `Notes`

| Key | Value | Role |
|---|---|---|
| `MAX_ALERT` | 100 | Fail if alert ≥ this |
| `EVENT_TRIGGER_RATE` | 25 | Base event chance % |
| `MAX_EVENT_PER_ACTION` | 1 | Event cap per action |
| `MIN_CANDY_TO_ESCAPE` | 1 | Candy needed to escape / win |
| `BASE_SCORE_MULTIPLIER` | 1 | TEMP score factor |
| `EVENT_ON_FAILED_ACTION` | 1 | Roll event after fail |
| `EVENT_CHAIN_ENABLED` | 0 | No chained events |
| `MAX_SUCCESS_RATE` | 100 | Chance clamp |
| `MIN_SUCCESS_RATE` | 0 | Chance clamp |

**ShopTable** — `ShopID` PK. Current enabled: `SHOP_001` 동네 사탕 가게, `SHOP_002` 대형 캔디숍, `SHOP_003` 비밀 사탕 창고. Columns: `ShopName`, `Difficulty`, `StartTurn`, `StartAlert`, `SuccessRateBonusPp`, `CandyMultiplier`, `EventRateBonusPp`, `AllowedEventTags`, `BackgroundImageKey` → `AssetTable.ImageKey`, `UnlockCondition`, `Enabled`, `Notes`.

**PhaseTable** — `PhaseID` PK: `MOVE`, `INFILTRATE`, `LOOT`, `ESCAPE`, `RESULT`. Columns: `PhaseName`, `PhaseOrder`, `IsRequired`, `EntryCondition`, `CompleteCondition`, `NextPhaseID`, `AllowOptionalActions`, `Notes`.

**ActionTable** — `ActionID` PK. Columns: `PhaseID`, `ActionType`, `ActionName`, `Description`, `SuccessRate`, `TurnCost`, `FailAlert`, `SuccessCandy`, `SuccessAlert`, `FailCandy`, `SuccessTurnDelta`, `FailTurnDelta`, `Repeatable`, `ShowCondition`, `SuccessNextPhase`, `EventEligible`, `Enabled`, `Notes`.

Current enabled actions: MOVE `ACT_001`–`ACT_003`; INFILTRATE `ACT_004`–`ACT_006`; LOOT required `ACT_007`–`ACT_009`; LOOT optional `ACT_013`; ESCAPE `ACT_010`–`ACT_012`.

**EventTable** — `EventID` PK. Columns: `EventName`, `EventDescription`, `PhaseFilter`, `EventTags`, `Weight`, `MinAlert`, `MaxAlert`, `MinRemainingTurn`, `MaxRemainingTurn`, `Repeatable`, `ImageKey` → `AssetTable.ImageKey`, `Enabled`, `Notes`. Enabled: `EVT_001`–`EVT_005`.

**EventOptionTable** — `OptionID` PK, `EventID` FK, `OptionOrder`. Outcome columns on the option row. `SuccessEffectID` / `FailEffectID` are `NONE` for MVP.

**AchievementTable** — `AchievementID` PK. `ACH_003` is `Enabled=0` (no action with SuccessRate ≤ 10). Unlock in M4.

**AssetTable** — `AssetID` PK, `ImageKey` unique. `FilePath` is a planned PNG list; files are not created yet. M1 uses placeholders.

**EffectTable** — future. All current rows `Implemented=0` and `Enabled=0`. Runtime must not apply them until implemented. MVP outcomes come from Action/EventOption columns.

## UI

Stack: Unity uGUI + TextMeshPro (already available via `com.unity.ugui`).

| Item | Spec |
|---|---|
| Orientation | Portrait locked |
| Canvas Scaler | Scale With Screen Size |
| Reference | 1080×1920 |
| Layout | Anchors |
| Safe Area | Required |
| Touch | Choice buttons large enough for fingers |
| Reuse | Required-action choices and event options share one choice-list UI |

Screen layout:

- Top: remaining turns, alert, candy
- Center: situation, event/action illustration, description
- Bottom: choices with success chance, turn cost, expected reward/penalty

M1: placeholder UI only. Final art in later milestones.

Title / result / shop-select screens are M2 unless a stub is needed to enter the M1 run.

## Art direction (still valid)

Cute but kitschy low-detail pixel art. Candy-shop pastel (mint / sky-blue / pink / yellow / purple). Avoid heart-heavy ornamentation. Small readable silhouettes. Art is replaceable. Prototype with placeholders. Existing Ninja Adventure temp art stays in the project; v0.2 M1 does not require it.

## Out of scope

| Item | Notes |
|---|---|
| v0.1 real-time stealth gameplay as the shipping loop | Archived. Existing scene/scripts stay, unused by v0.2 loop until owner says otherwise |
| Night stages, companions, gacha | Still out |
| Permanent combat/stat growth | Out |
| Server, accounts, multiplayer | Out |
| Google Play listing | After APK |
| Final illustration/SFX | After M3 core loop |
| New Unity project / package adds for M1 | Out unless owner approves |

## Milestones (summary)

Detail and done-when: `Docs/PLAN.md`, `Docs/WBS.md`.

| ID | Goal |
|---|---|
| M1 | CSV loader, run state, actions, retry, Enabled events, portrait temp UI |
| M2 | Shop select, Move/Infiltrate/Loot/Escape, phase change, result |
| M3 | More events/actions, score formula, repeat-play balance |
| M4 | Titles, records, event art, UI motion, sound |
| M5 | Android build, device QA, APK |

## M1 acceptance

1. Required CSV tables load by ID with no name-based lookup.
2. A run can attempt a required action, fail, retry, and succeed using table numbers.
3. Failure spends turns and raises alert from data.
4. Turns reaching 0 or alert ≥ 100 ends the run as fail.
5. After an `EventEligible` action, an enabled event may appear; resolving an option does not chain another event.
6. If the run already ended, no event is shown.
7. Portrait placeholder UI at 1080×1920 shows turns, alert, candy, and choices.
8. Editor can complete the M1 loop with mouse clicks standing in for touch.

## Open decisions (ask before coding if blocking)

1. Candy × `CandyMultiplier` rounding (README: pick one method and keep it).
2. Whether an action whose `TurnCost` exceeds remaining turns can be selected.
3. RNG seed / run replay (M2).
4. Score `risk_bonus` formula (M3). Until then `BASE_SCORE_MULTIPLIER`.
5. Cosmetic currency display name (M4).
6. M1 default shop if shop-select is deferred (`TEMP`: `SHOP_001`).
7. Save format (PlayerPrefs vs JSON). `TEMP`: PlayerPrefs for M4.
8. When to generate `AssetTable` PNGs (not needed for M1 placeholders).

---

## Appendix A: Superseded v0.1 spec

Do not implement the following as the v0.2 shipping loop. Kept for history and for existing prototype scenes.

- Pitch: 2D top-down candy-shop stealth. Evade guard and shoppers, collect key candy, escape. Optional gold candy.
- Target: Unity 2D / PC WebGL / keyboard. 6-week study window.
- Loop: Title → Start → Shop → key candy → exit → Result. Detection → fail.
- Rules: WASD; collisions; one daytime shop; one guard; up to five shoppers; directional FOV blocked by walls; detection fails the run; key candy required; gold granted only on clear.
- UI: title, HUD (key, gold, detection), result/retry.
- Acceptance: WASD test room, guard FOV, key/exit, gold tracking, WebGL loop.
- Open at archive time: detection delay vs gauge; shopper detection; gold save; Input System (project already uses Input System).
