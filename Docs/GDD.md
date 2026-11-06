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

Title → Lobby → Shop Selection → Move → Infiltrate → Loot → Escape → Result → Retry / Title

Lobby is a hub before shop select (D-012). It does not change run rules. Unimplemented hub menus show “준비 중” only.

Shop selection happens at run start. Each `ShopTable` row changes:

- `StartTurn`, `StartAlert`
- `SuccessRateBonusPp` (percentage points added to action success)
- `CandyMultiplier` (applied once to run candy on escape success; see [Candy](#candy))
- `EventRateBonusPp` and `AllowedEventTags`

Phase order after shop select follows `PhaseTable.PhaseOrder`: MOVE → INFILTRATE → LOOT → ESCAPE → RESULT. In a phase with several `ActionType=REQUIRED` rows, the player picks **one**. One success with `CompleteCondition=REQUIRED_ACTION_SUCCESS` advances via `SuccessNextPhase` / `NextPhaseID`.

LOOT stays in LOOT after a successful steal (`SuccessNextPhase=LOOT`) so the player can steal again (Push Your Luck). `ACT_013` (`OPTIONAL`, `ShowCondition=CANDY_AT_LEAST_1`) moves to ESCAPE after its own success roll. Meeting candy / phase conditions does not auto-succeed. An ESCAPE-phase action must then succeed to win (D-014).

## Win / fail

Win (all required):

- run candy ≥ `GameConfig.MIN_CANDY_TO_ESCAPE`
- an **escape-phase** action succeeds its chance roll (same formula as other actions; D-014)

Fail (first match in the end-check order below):

- alert ≥ `GameConfig.MAX_ALERT`
- remaining turns reach 0, except when this same action already produced a win

Owner-approved last-turn and overlap rules (D-011):

- `TurnCost` > remaining turns: the action cannot be selected.
- `TurnCost` == remaining turns: the action may be executed.
- After that action, remaining turns may be 0. If it was a successful escape and run candy ≥ `MIN_CANDY_TO_ESCAPE`, the run **wins**.
- Last-turn escape **fail** is not a win. Apply the same end-check order (alert first, else remaining turns 0 fail).
- Otherwise remaining turns 0 is a fail.
- If this action makes alert ≥ `MAX_ALERT` and also satisfies escape success, **alert fail wins**.
- Escape success with run candy 0 is not a win.
- Candy / ESCAPE phase / last-turn eligibility does not skip the success roll or auto-win (D-014).

End-condition checks beat event spawning. If a resolved action or option already meets win/fail, do not roll or present an event.

Resolve order (do not reorder in code):

1. Reject selection if `TurnCost` > remaining turns
2. Subtract action/option `TurnCost`
3. Apply success or fail candy / alert / turn deltas (integers; no `CandyMultiplier`)
4. End check, in this order:
   1. `Alert >= MAX_ALERT` → fail
   2. Else escape-action success and candy ≥ `MIN_CANDY_TO_ESCAPE` → win (allowed at 0 turns)
   3. Else remaining turns ≤ 0 → fail
   4. Else the run stays live
5. If the run is still live, event check

`finalReward` is not computed in M1-3. See [Candy](#candy).

## Actions

Actions are data rows in `ActionTable`, referenced by ID.

| Rule | Spec |
|---|---|
| Types | `ActionType`: `REQUIRED` or `OPTIONAL` |
| Success | `clamp(SuccessRate + Shop.SuccessRateBonusPp, MIN_SUCCESS_RATE, MAX_SUCCESS_RATE)` — values are **percent 0–100**, not 0–1. Includes ESCAPE rows and `ACT_013` (D-014) |
| Turn cost | `TurnCost` subtracted on attempt, then `SuccessTurnDelta` / `FailTurnDelta`. Cannot select if `TurnCost` > remaining turns (D-011) |
| On failure | `FailAlert`, `FailCandy`, `FailTurnDelta` from the action row. Candy deltas are integers added to run candy. Fail does not consume the action |
| On success | Add integer `SuccessCandy` to run candy (no per-action multiplier), `SuccessAlert`, `SuccessTurnDelta` |
| Required failure | Same-phase retry while the run is live, including `Repeatable=0` rows |
| Repeatable | Post-success reappearance this run, not an attempt count (D-013). `1`: choosable after success or fail. `0`: choosable after fail; not after success. Eligibility ≠ spawn chance. Current ActionTable rows are `1` |
| Optional | Current table: only `ACT_013` (leave LOOT → ESCAPE when candy ≥ `MIN_CANDY_TO_ESCAPE`). Still uses the success-chance formula; success is a phase change, not a win. Not a skip of MOVE/INFILTRATE |
| Escape roll | Stage clear requires a successful ESCAPE-phase roll. Players choose among current rows (`ACT_010`–`ACT_012`) for chance vs turn cost. Do not add extra safer escape rows until rates and unlocks are approved |
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
| Repeatable | Same post-success meaning as actions (D-013). Failed events may remain candidates. Eligibility ≠ `Weight` / trigger chance |
| Priority | Run-end check > event |

M1 uses every `EventTable` row with `Enabled=1` (currently EVT_001–EVT_005), not a hardcoded count of 3.

M1-7 event pick must apply D-013 to `EventTable.Repeatable`. Do not treat Repeatable as a roll chance.

## Candy

Owner-approved. D-009. Do not use floor / truncate.

| When | Rule |
|---|---|
| During the run | Candy is an integer. Add `SuccessCandy` / `FailCandy` and event-option candy deltas as integers. `CANDY_AT_LEAST_1` and win’s “candy ≥ 1” use this run total |
| Escape success | `finalReward = CeilToInt(runCandy × Shop.CandyMultiplier)`. Apply the multiplier **once**, not per action |
| Escape failure, or run fail (turns / alert) | `finalReward = 0` |

`RunState` in M1-3 stores run candy only. `finalReward` is computed in later escape/result work.

## Score

```
score = candy_acquired × risk_bonus
```

`candy_acquired` is `finalReward` (D-009), not the pre-multiplier run total.

`risk_bonus` formula is `TBD` (balance in M3).  
TEMP for M1–M2: use `GameConfig.BASE_SCORE_MULTIPLIER` (1). Do not ship this as final.

Gold-candy-on-clear from v0.1 is superseded. v0.2 score uses final rewarded candy and risk bonus only.

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

**ActionTable** — `ActionID` PK. Columns: `PhaseID`, `ActionType`, `ActionName`, `Description`, `SuccessRate`, `TurnCost`, `FailAlert`, `SuccessCandy`, `SuccessAlert`, `FailCandy`, `SuccessTurnDelta`, `FailTurnDelta`, `Repeatable` (D-013: after success, not attempt count), `ShowCondition`, `SuccessNextPhase`, `EventEligible`, `Enabled`, `Notes`.

Current enabled actions: MOVE `ACT_001`–`ACT_003`; INFILTRATE `ACT_004`–`ACT_006`; LOOT required `ACT_007`–`ACT_009`; LOOT optional `ACT_013`; ESCAPE `ACT_010`–`ACT_012`.

**EventTable** — `EventID` PK. Columns: `EventName`, `EventDescription`, `PhaseFilter`, `EventTags`, `Weight`, `MinAlert`, `MaxAlert`, `MinRemainingTurn`, `MaxRemainingTurn`, `Repeatable` (D-013; confirm at M1-7), `ImageKey` → `AssetTable.ImageKey`, `Enabled`, `Notes`. Enabled: `EVT_001`–`EVT_005`.

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

Title / Lobby / Shop Select exist as M1-UI-1 hub screens (D-012). Result and the in-run choice UI remain later. Title / result / shop-select were originally M2; Title and Shop Select stubs are now pulled forward.

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
4. Turns reaching 0 fail unless that action was a successful escape with candy ≥ `MIN_CANDY_TO_ESCAPE`. Last-turn escape fail follows D-011. Escape uses the same success roll as other actions (D-014). Alert ≥ `MAX_ALERT` always fails, including when it coincides with escape success.
5. After an `EventEligible` action, an enabled event may appear; resolving an option does not chain another event.
6. If the run already ended, no event is shown.
7. Portrait placeholder UI at 1080×1920 shows turns, alert, candy, and choices.
8. Editor can complete the M1 loop with mouse clicks standing in for touch.

## Open decisions (ask before coding if blocking)

1. Candy × `CandyMultiplier` rounding — **closed (D-009):** integer run candy; `CeilToInt` once on escape success; escape fail = 0.
2. Whether an action whose `TurnCost` exceeds remaining turns can be selected — **closed (D-011):** no. Equal cost is allowed. Last-turn escape can win. Last-turn escape fail follows D-011. Alert ≥ max beats escape.
3. RNG seed / run replay (M2).
4. Score `risk_bonus` formula (M3). Until then `BASE_SCORE_MULTIPLIER`.
5. Cosmetic currency display name (M4).
6. M1 default shop if shop-select is deferred — **closed (D-010):** test/M1 auto-select `SHOP_001` only. Player shop-select is M2.
7. Save format (PlayerPrefs vs JSON). `TEMP`: PlayerPrefs for M4.
8. When to generate `AssetTable` PNGs (not needed for M1 placeholders).
9. Whether escape skips the success roll — **closed (D-014):** same formula as other actions; meeting conditions does not auto-succeed. Extra safer escape rows (rates / unlocks) stay **open**; do not add them.

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
