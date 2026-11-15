# Candy Thief — GDD v0.2

Status: Owner-approved working spec. Unconfirmed values are marked `TEMP` or `TBD`. Owner approval is required before changing rules.

Current gameplay source of truth. Implementation follows this file plus `Docs/DECISIONS.md`.

## Document history

| Version | Status | Summary |
|---|---|---|
| v0.1 | Superseded | 2D top-down shop stealth. WASD, guard FOV, key/gold candy, WebGL. Archive: [Appendix A](#appendix-a-superseded-v01-spec). |
| v0.2 | Current | Mobile portrait probability-choice roguelite. Push Your Luck, CSV-driven, Android APK first. |
| 2026-11-14 | D-021–D-026 | Operation prep, rumors, quests, rerolls. CSV files on disk are unchanged until a later pass. |
| 2026-11-15 | D-028–D-032 | In-run HUD, escape prep, separate operation result, shared action and event timer. PLAY-01 through PLAY-03 and PLAY-05 through PLAY-07 are reviewed Done. The forecast tooltip stays until the M4 center information modal. |

Change reason is recorded in `Docs/DECISIONS.md` (D-001).

---

## Pitch

사탕 가게에 잠입해, 턴과 위험도를 깎아가며 행동을 고르는 단판형 Push Your Luck 게임. 성공 확률을 보고 더 가져갈지, 지금 탈출할지 고른다. 한 판은 3~5분.

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

Lobby is a hub before shop select (D-012). It does not change run rules. Nickname, energy, gold, and candy on the lobby HUD are live saved values (D-020). Unimplemented hub menus still show “준비 중”. Title stays until nickname login succeeds (D-018).

Shop selection happens before a run (D-021). `작전 시작` opens a saved list of 4 shops, not every enabled row. One list has no duplicate shop. An active quest does not change draw weights. The list refreshes only after a settled win, fail, or explicit abandon (D-025). Reopening the screen keeps the list. Each chosen `ShopTable` row still changes:

- `StartTurn`, `StartAlert`
- `SuccessRateBonusPp` (percentage points added to action success)
- `CandyMultiplier` (applied once to run candy on escape success; see [Candy](#candy))
- `EventRateBonusPp` and `AllowedEventTags`

Rumors (D-022) are not `EventTable` rows. They are fixed when the list is created, then folded into one shop snapshot at run start (`SuccessRateBonusPp`, `StartAlert`, `CandyMultiplier`, `EventRateBonusPp`). No rumors means the snapshot equals the shop row. Resolve order is unchanged (D-011). `EffectTable` stays off.

Quest board rules are D-023. Reroll caps and the KST 05:00 operation day are D-024. Column names are D-026. Shop columns and the empty rumor tables are on disk (D-027, OP-DEV-04). `QuestTable` is not on disk yet.

Phase order after shop select follows `PhaseTable.PhaseOrder`: MOVE → INFILTRATE → LOOT → ESCAPE → RESULT. In a phase with several `ActionType=REQUIRED` rows, the player picks **one**. One success with `CompleteCondition=REQUIRED_ACTION_SUCCESS` advances via `SuccessNextPhase` / `NextPhaseID`.

Action select and event select share one limit, `GameConfig.ACTION_SELECT_TIME_SEC` (30) (D-028, extended by D-031). The countdown runs while action cards or event options are shown. It pauses for the abandon confirm, the escape-prep notice, and the empty-hand confirm; cancel resumes the remaining time. Result feedback and an ended run hide it and stop it. A tooltip does not pause it. At 0 seconds, one `CanExecute` choice is picked with equal probability and resolved through the same action or event path, with no extra turn, alert, or candy penalty. `SpawnWeight` is not that probability. One claim per round, so a click and a timeout cannot both apply. After the claim, the current choices take no further input. Zero executable choices log once and do not auto-pick or add a fail (P-018). A new action round, and the moment event options appear, each start the allowance again. Refreshing the same round does not.

LOOT stays in LOOT after a successful steal (`SuccessNextPhase=LOOT`) so the player can steal again (Push Your Luck). The LOOT `탈출 준비하기` button enters ESCAPE with no turn cost and no success roll (D-029). Candy 0 asks for confirmation first. Candy 1 or more enters immediately. `ACT_013` stays in the table and out of the normal 2-card draw. An ESCAPE-phase action must then succeed to win, and that win still requires candy ≥ `MIN_CANDY_TO_ESCAPE` (D-014). Entering ESCAPE with no candy is not itself a win or a fail.

## Win / fail

Win (all required):

- run candy ≥ `GameConfig.MIN_CANDY_TO_ESCAPE`
- an **escape-phase** action succeeds its chance roll (same formula as other actions; D-014)

Fail, after an escape-action success is judged (D-030):

- empty-hand escape: the escape action succeeded and candy is 0
- else alert ≥ `GameConfig.MAX_ALERT` (captured)
- else remaining turns reach 0 (`NoTurns`)

Owner-approved last-turn and overlap rules (D-011, order updated by D-030):

- `TurnCost` > remaining turns: the action cannot be selected.
- `TurnCost` == remaining turns: the action may be executed.
- Escape-action success is judged before alert and before 0 turns. Candy ≥ `MIN_CANDY_TO_ESCAPE` is an operation success even if alert is max or turns are now 0.
- Escape-action success with candy 0 is an empty-hand escape. It is not an operation success and it is not `NoTurns`.
- A failed escape that reaches both alert max and 0 turns is captured.
- Candy / ESCAPE phase / last-turn eligibility does not skip the success roll or auto-win (D-014).

End-condition checks beat event spawning. If a resolved action or option already meets win/fail, do not roll or present an event.

Resolve order (do not reorder in code):

1. Reject selection if `TurnCost` > remaining turns
2. Subtract action/option `TurnCost`
3. Apply success or fail candy / alert / turn deltas (integers; no `CandyMultiplier`)
4. End check, in this order (D-030):
   1. Escape-action success and candy ≥ `MIN_CANDY_TO_ESCAPE` → operation success (wins over alert and 0 turns)
   2. Else escape-action success and candy 0 → empty-hand escape. The escape succeeded and the operation failed. Settled candy is 0
   3. Else `Alert >= MAX_ALERT` → captured
   4. Else remaining turns ≤ 0 → `NoTurns`
   5. Else the run stays live
5. If the run is still live, event check

Settled candy is `Ceil(run candy × shop CandyMultiplier)` only for an operation success. Every other ending, including empty-hand escape, settles 0 (D-009, D-030).

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
| Optional | `ACT_013` remains the only optional row. It is excluded from the normal draw (`SpawnWeight=0`). The player leaves LOOT with the escape-prep button (D-029), not with this card |
| Escape roll | Stage clear requires a successful ESCAPE-phase roll. Players choose among current rows (`ACT_010`–`ACT_012`) for chance vs turn cost. Do not add extra safer escape rows until rates and unlocks are approved |
| Event after action | Only if `EventEligible=1`. `ACT_013` is 0 |
| Display | Show chance, turn cost, expected reward/penalty from data. Do not hardcode |

Do not put probabilities, turn costs, or alert deltas in C#. Enabled rows only (`Enabled=1`).

## Events

| Rule | Spec |
|---|---|
| When | After a general action with `EventEligible=1`, success or fail (`EVENT_ON_FAILED_ACTION=1`), if the run is still live (D-015) |
| Chance | `clamp(EVENT_TRIGGER_RATE + Shop.EventRateBonusPp, 0, 100)` percent. This is trigger chance, not `Weight` |
| Cap | `MAX_EVENT_PER_ACTION` (1). `EVENT_CHAIN_ENABLED=0`. At most one event per action; no draw after an option |
| Candidates | `Enabled=1`; `PhaseFilter` is `ALL` or the **pre-action** phase; `EventTags` ∩ `Shop.AllowedEventTags` (`\|`); inclusive `MinAlert`–`MaxAlert` and `MinRemainingTurn`–`MaxRemainingTurn` using **post-action** alert/turns; Repeatable (D-013). Then `Weight` pick among remaining rows |
| Options | `EventOptionTable` for that `EventID`. Chance `%` = `clamp(SuccessRate + Shop.SuccessRateBonusPp, MIN_SUCCESS_RATE, MAX_SUCCESS_RATE)`. Apply `TurnCost` then candy/alert/turn deltas. `SuccessEffectID`/`FailEffectID` are `NONE` |
| Success | The chosen option’s chance roll succeeds. Appearance or picking an option is not success (D-015) |
| Pending | Drawn `EventID` is pending until a valid option of that event resolves (or the run ends). Actions cannot be selected or applied while pending |
| LastActionID | Stays the triggering action after an option. `OptionID` is not an ActionID |
| Chain | After an option resolves, no second event |
| Repeatable | Same post-success meaning as actions (D-013) on `EventID`. Failed events may remain candidates. Eligibility ≠ `Weight` / trigger chance |
| Priority | Run-end check > event. Options are not escape actions and cannot win |

M1 uses every `EventTable` row with `Enabled=1` (currently EVT_001–EVT_005), not a hardcoded count of 3.

Draw eligibility (`EventCheckDue`), candidate eligibility, trigger chance, and `Weight` pick are separate (D-015).

## Candy

Owner-approved. D-009. Do not use floor / truncate.

| When | Rule |
|---|---|
| During the run | Candy is an integer. Add `SuccessCandy` / `FailCandy` and event-option candy deltas as integers. `CANDY_AT_LEAST_1` and win’s “candy ≥ 1” use this run total |
| Escape success | `finalReward = CeilToInt(runCandy × Shop.CandyMultiplier)`. Apply the multiplier **once**, not per action |
| Escape failure, or run fail (turns / alert) | `finalReward = 0` |

`RunState.SettledCandy` is that `finalReward`. The profile receives it once, and only for an operation success (D-030). Empty-hand escape, capture, `NoTurns`, and abandon grant 0.

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
| `MIN_CANDY_TO_ESCAPE` | 1 | Candy needed for an escape-action success to win. Not required to enter ESCAPE (D-029) |
| `BASE_SCORE_MULTIPLIER` | 1 | TEMP score factor |
| `EVENT_ON_FAILED_ACTION` | 1 | Roll event after fail |
| `EVENT_CHAIN_ENABLED` | 0 | No chained events |
| `MAX_SUCCESS_RATE` | 100 | Chance clamp |
| `MIN_SUCCESS_RATE` | 0 | Chance clamp |
| `ACTION_SELECT_TIME_SEC` | 30 | Required INT ≥ 1. Shared action and event select limit in seconds (D-028, D-031) |

**ShopTable** — `ShopID` PK. On disk today: 20 shops `SHOP_001`–`SHOP_020` (D-027). `SHOP_001` 토마스 사탕점, `SHOP_002` 잭의 과자점, `SHOP_003` 르블랑 백화점 keep their ids and resolve numbers (D-021); `SHOP_004`–`SHOP_020` carry 1st-pass test balance numbers. Columns on disk: `ShopName`, `Difficulty`, `StartTurn`, `StartAlert`, `SuccessRateBonusPp`, `CandyMultiplier`, `EventRateBonusPp`, `AllowedEventTags`, `BackgroundImageKey` → `AssetTable.ImageKey`, `UnlockCondition`, `Enabled`, `SpawnWeight`, `DistanceGrade`, `ScaleGrade`, `SecurityGrade`, `GoodsGrade`, `EnergyCost`, `Notes`. `EnergyCost` is not spent (D-020). `EnergyCost` is `0` on every shop until its values are approved.

**PhaseTable** — `PhaseID` PK: `MOVE`, `INFILTRATE`, `LOOT`, `ESCAPE`, `RESULT`. Columns: `PhaseName`, `PhaseOrder`, `IsRequired`, `EntryCondition`, `CompleteCondition`, `NextPhaseID`, `AllowOptionalActions`, `SituationText`, `Notes`. `SituationText` is the player line under the illustration during action select (D-028). `Notes` stays a developer note and is not shown. A blank `SituationText` fails the load.

**ActionTable** — `ActionID` PK. Columns: `PhaseID`, `ActionType`, `ActionName`, `Description`, `SuccessRate`, `TurnCost`, `FailAlert`, `SuccessCandy`, `SuccessAlert`, `FailCandy`, `SuccessTurnDelta`, `FailTurnDelta`, `Repeatable` (D-013: after success, not attempt count), `ShowCondition`, `SuccessNextPhase`, `EventEligible`, `Enabled`, `Notes`.

Current enabled actions: MOVE `ACT_001`–`ACT_003`; INFILTRATE `ACT_004`–`ACT_006`; LOOT required `ACT_007`–`ACT_009`; LOOT optional `ACT_013`; ESCAPE `ACT_010`–`ACT_012`.

**EventTable** — `EventID` PK. Columns: `EventName`, `EventDescription`, `PhaseFilter`, `EventTags`, `Weight`, `MinAlert`, `MaxAlert`, `MinRemainingTurn`, `MaxRemainingTurn`, `Repeatable` (D-013 / D-015: after option success, not attempt count), `ImageKey` → `AssetTable.ImageKey`, `Enabled`, `Notes`. Enabled: `EVT_001`–`EVT_005`.

**EventOptionTable** — `OptionID` PK, `EventID` FK, `OptionOrder`. Outcome columns on the option row. `SuccessEffectID` / `FailEffectID` are `NONE` for MVP.

**AchievementTable** — `AchievementID` PK. `ACH_003` is `Enabled=0` (no action with SuccessRate ≤ 10). Unlock in M4.

**AssetTable** — `AssetID` PK, `ImageKey` unique. `FilePath` is a planned PNG list; files are not created yet. M1 uses placeholders.

**EffectTable** — future. All current rows `Implemented=0` and `Enabled=0`. Runtime must not apply them until implemented. MVP outcomes come from Action/EventOption columns. Rumors must not turn these rows on (D-022).

**ShopRumorTable / ShopRumorCandidate / QuestTable** — approved in D-026. The two rumor tables are on disk with no rows yet (OP-DEV-04); `QuestTable` is not on disk yet. Rumors modify the shop snapshot only. Quests are not `AchievementTable` rows. In-run events stay `EventTable`.

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

Screen layout (D-028, portrait 1080×1920):

- Top: current `PhaseName`, then remaining turns, 위험도 gauge with the text `Alert/MAX_ALERT`, candy
- Center: shop or event illustration. Directly under it, the situation line. Action select uses `PhaseTable.SituationText`. A pending event uses `EventTable.EventDescription`. Event art still replaces the shop background when that sprite loads
- Above the choices: the select timer (ceiling seconds). It counts down on action cards and on event options (D-031). It is hidden during result feedback and an ended run. Confirm popups pause it without hiding it.
- Bottom: the existing vertical choice list (name, success chance, turn cost, non-zero success/fail effects, description) and the lobby button

M1: placeholder UI only. Final art in later milestones.

Title / Lobby / Shop Select exist as M1-UI-1 hub screens (D-012). Player shop select becomes the D-021 4-card prefab when that task is implemented; the runtime list is not deleted beforehand. Title requires a local nickname login before Lobby (D-018). In-run placeholder HUD is M1-8 (`RunHud` prefab). Choice results use M1-8.5 feedback (D-016): comic success/fail copy, character reaction, actual snapshot deltas, then `[계속]`. That popup does not carry the operation title. The operation result is the PLAY-07 prefab: four endings, layout approved 2026-11-15, placeholder character art.

## Art direction (still valid)

Official style: Korean casual-mobile kitschy 2D cartoon — large round SD characters, thick even dark-brown outlines, clear color blocks, simple geometry, minimal cell shading, exaggerated comic poses. Cute but not overly feminine or decorative. Candy-shop pastel (mint / sky-blue / pink / yellow / purple) is allowed; do not flood UI with pink, hearts, ribbons, or sparkles. Small readable silhouettes.

Art style source of truth: `Docs/Art/ART_STYLE_GUIDE.md` (v0.2). Official visual references: approved `REF-*` files in `Docs/Art/References/`. Character, background, and UI *look* follow those docs; UI function and data stay in this GDD, TECH_SPEC, DECISIONS, and the current build.

Art is replaceable. Prototype with placeholders. Existing Ninja Adventure temp art stays in the project; v0.2 M1 does not require it.

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
4. End check is D-030. Escape-action success with candy ≥ `MIN_CANDY_TO_ESCAPE` wins even if alert is max or turns are 0. Escape-action success with candy 0 is an empty-hand escape and settles 0. Otherwise alert ≥ `MAX_ALERT` is capture, then turns ≤ 0 is `NoTurns`. Escape uses the same success roll as other actions (D-014).
5. After an `EventEligible` action, an enabled event may appear; resolving an option does not chain another event.
6. If the run already ended, no event is shown.
7. Portrait placeholder UI at 1080×1920 shows turns, alert, candy, and choices.
8. Editor can complete the M1 loop with mouse clicks standing in for touch.

## Open decisions (ask before coding if blocking)

1. Candy × `CandyMultiplier` rounding — **closed (D-009):** integer run candy; `CeilToInt` once on escape success; escape fail = 0.
2. Whether an action whose `TurnCost` exceeds remaining turns can be selected — **closed (D-011):** no. Equal cost is allowed. Last-turn escape can win. Overlap order is D-030: escape success is judged before alert and before 0 turns.
3. RNG seed / run replay (M2).
4. Score `risk_bonus` formula (M3). Until then `BASE_SCORE_MULTIPLIER`.
5. Cosmetic currency display name (M4).
6. M1 default shop if shop-select is deferred — **closed (D-010):** test/M1 auto-select `SHOP_001` only. Player shop-select is M2.
7. Save format (PlayerPrefs vs JSON). `TEMP`: PlayerPrefs for M4.
8. When to generate `AssetTable` PNGs (not needed for M1 placeholders).
9. Whether escape skips the success roll — **closed (D-014):** same formula as other actions; meeting conditions does not auto-succeed. Extra safer escape rows (rates / unlocks) stay **open**; do not add them.
10. Event success timing, PhaseFilter snapshot, option shop bonus, pending — **closed (D-015).**
11. Shipping shop count — **closed (D-021, D-027):** 20 shops, show 4. Names and weights are registered. `EnergyCost` and rumor effect rows stay open (P-016).
12. Reroll max counts and paid prices — **open (P-014).** Until a max is set, the cap equals the free count (D-024). Do not charge gold.
13. Which lobby control opens the quest board — **open (P-015).**
14. Quest gold/candy payout — **open (P-017).** Do not grant it under D-020.
15. Departure energy amount — **open.** D-020 still forbids inventing a spend. `EnergyCost` is stored and not subtracted (D-026).
16. In-run phase label, `Alert/MAX_ALERT`, situation line, and the 30-second select limit — **closed (D-028, D-031).** Event options use the same limit. Timeout pick is not a new resolve order. PLAY-03 still needs a Unity review.
17. Zero executable actions while the run is still `InProgress` — **open (P-018).** Timeout logs once and does not pick or add a fail. D-011 still ends on turns only after an action is applied.

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
