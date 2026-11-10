# Decisions

Design authority: owner approval. Implementation authority: `Docs/GDD.md` + this file.

Do not treat archived v0.1 rows as current rules.

## Approved — v0.2

### D-001 — Genre and platform pivot (2026-11-05)

**Decision:** Candy Thief shipping gameplay is a mobile portrait probability-choice roguelite (Push Your Luck, single-run), not a real-time 2D top-down stealth action game.

**Why:**

- Owner approved v0.2. Target session is 3–5 minutes on a phone, not a WASD stealth stage in WebGL.
- Balance must be CSV-driven so shops, chances, turns, and alert can change without code.
- v0.1 implementation never left the movement/art prototype (`SampleScene`, `PlayerController`). The old scene is kept, not deleted, and is not the v0.2 loop.

**Follow-through:** Android first, portrait 1080×1920, touch, editor test. Keep Unity 6, packages, Cursor, Unity MCP, GitHub. No new Unity project.

### D-002 — Win / fail

Win: candy ≥ 1 and Escape action success.  
Fail: turns ≤ 0 or alert ≥ 100.  
End checks beat event spawn.  
Last-turn escape, selection gate, and alert-vs-escape overlap are in D-011.  
Escape uses the same success-chance formula as other actions; meeting conditions does not auto-succeed (D-014).

### D-003 — Actions and events

Actions split required / optional. Each has success chance and fixed turn cost from tables. Failure spends turns and raises alert. Required actions may be retried while the run continues. `Repeatable` meaning is D-013. Events may roll after any completed action using `GameConfig` / `ShopTable`, then `EventTable` / `EventOptionTable`. No chained event after an option. Lookup is by ID, never by display text.

### D-004 — Meta

Each run resets. No permanent stat growth. Achievements/titles persist. Best score and cumulative records persist. Permanent currency is cosmetics-only.

### D-005 — Content pipeline

Nine CSV tables are required: `GameConfig`, `ShopTable`, `PhaseTable`, `ActionTable`, `EventTable`, `EventOptionTable`, `AchievementTable`, `AssetTable`, `EffectTable`. New effect types need code; numeric tuning does not.

### D-006 — UI stack

uGUI + TextMeshPro. Portrait, Canvas Scaler, 1080×1920, anchors, Safe Area. Action choices and event options share one choice UI. M1 is placeholder UI.

### D-007 — Existing project freeze for this pivot

Do not delete existing scenes, scripts, or assets. Do not change packages for the document pivot. v0.2 systems are added, not swapped in by removing v0.1 files.

### D-008 — CSV files are the column source of truth (2026-11-05)

**Decision:** Load tables from `CandyThief/Assets/Resources/Table/` (UTF-8 BOM). Column names and IDs in those CSVs replace the earlier GDD TEMP schema. `README.csv` is loader/rules notes, not a runtime table.

**Closed by the files:** P-001 columns; P-002 events EVT_001–EVT_005; P-003 actions ACT_001–ACT_013; P-004 optional is `ACT_013` only (LOOT → ESCAPE); P-005/P-009 three shops SHOP_001–SHOP_003.

### D-009 — Candy reward calculation (2026-11-05)

**Decision:** P-012 is closed. Candy payout uses integer run totals and applies the shop multiplier once at a successful escape.

1. Candy gained during a run accumulates as integers (`SuccessCandy`, `FailCandy`, and event-option candy deltas). Do not apply `ShopTable.CandyMultiplier` per action.
2. On **escape success**, apply `CandyMultiplier` once to that integer total: `finalReward = CeilToInt(runCandy × CandyMultiplier)`.
3. Non-integer products round **up** (`Mathf.CeilToInt`). Truncate / floor (`소수점 이하 버림`) is discarded.
4. On **escape failure** (or any run that does not win by escape success), `finalReward` is 0.

**Why:** Owner confirmed the payout timing and rounding. Per-action multiply would distort Push Your Luck and made rounding ambiguous.

**Follow-through:** M1-3 `RunState` stores integer run candy only. Multiplier application and `finalReward` wait for later escape/result work. Score still uses `candy_acquired × risk_bonus`; `candy_acquired` for score is `finalReward`.

Owner example: two integer gains of 5 → run candy 10; `CandyMultiplier` 1.5 on escape success → `CeilToInt(10 × 1.5) = 15`.

### D-010 — M1 default shop (2026-11-05)

**Decision:** P-005 is closed. M1 / editor test runs start on `SHOP_001`. Shipping play uses player shop-select (M2). Auto-select is test-only; do not treat it as a gameplay unlock.

### D-011 — Turn cost and end-check order (2026-11-05)

**Decision:** P-013 is closed. Refines D-002.

- An action with `TurnCost` > remaining turns cannot be selected. `TurnCost` equal to remaining turns is allowed.
- Resolve order: (1) subtract `TurnCost` (2) apply success/fail candy / alert / turn deltas (3) end check (4) event only if still in progress. Do not reorder.
- End check order: (1) `Alert >= GameConfig.MAX_ALERT` → fail, even if this action was a successful escape (2) else escape-action success and run candy ≥ `MIN_CANDY_TO_ESCAPE` → win, even if remaining turns are now 0 (3) else remaining turns ≤ 0 → fail (4) else continue.
- Last-turn escape fail is not a win. After TurnCost and deltas, use this same end-check order.
- Escape with candy 0 is not a win.
- Escape chance roll is D-014. Do not skip it because candy / phase / last-turn conditions are already met.

### D-012 — M1-UI-1 hub screens (2026-11-05)

**Decision:** v0.2 starts on a new Title scene, not `SampleScene`. Flow for this pass: Title → Lobby → Shop Select. Shop Select initializes `RunState` from the chosen `ShopID` and shows a temporary debug panel. Gameplay run UI is not in this pass.

Lobby widgets (energy, gold, gold candy, gacha, companions, settings, profile) are **placeholders**. Taps show “준비 중”. They are not gameplay systems and do not grant run power.

Title has no start button: show at least 1 second, then auto-enter Lobby when CSV load succeeds. Background Images are swappable later.

**Why:** Owner requested the hub UI before M1-4 ActionResolver. GDD core loop remains Title → Shop Select for the heist; Lobby is an extra hub before shop select.

### D-013 — Repeatable is post-success reappearance (2026-11-06)

**Decision:** `Repeatable` on `ActionTable` / `EventTable` is not an attempt counter. It is whether a row may reappear after a **success** in the same run.

- `Repeatable=1`: may be chosen again after success or fail, if other selection rules still pass (phase, `ShowCondition`, `TurnCost`, run still live).
- `Repeatable=0`: may be chosen again after **fail**. After **success**, it cannot be chosen again in that run.
- Eligibility to reappear is separate from spawn / roll chance. Do not treat `Repeatable` as a probability.

**Why:** Owner corrected M1-5. GDD had tied required fail-retry to “`Repeatable=1` on current rows”, which read as fail-retry only when Repeatable=1. Fail never consumes the row.

**Follow-through:** M1-5 action select records successful ActionIDs only. Phase change can still hide a row (`WrongPhase`) even when Repeatable=1. **M1-7 event pick must apply this same meaning to `EventTable.Repeatable`:** a failed event may stay a candidate; a Repeatable=0 event that resolved successfully must not reappear this run. Weight / trigger chance stay M1-7. Do not implement event roll in M1-5.

### D-014 — Escape uses the same success roll (2026-11-06)

**Decision:** M1-6 QA conflict closed. Escape actions use the same success-chance check as other actions. Stage clear requires a successful escape-phase roll. Meeting escape conditions does not auto-succeed.

- Formula unchanged: `clamp(SuccessRate + Shop.SuccessRateBonusPp, MIN_SUCCESS_RATE, MAX_SUCCESS_RATE)`.
- `ACT_010`–`ACT_012` (ESCAPE) and `ACT_013` (LOOT → ESCAPE) all use this roll. `ACT_013` success is a phase change, not a win.
- Last-turn escape success still wins at 0 remaining turns (D-011).
- Last-turn escape fail follows D-011 end-check order (alert ≥ max first, else remaining turns 0 fail).
- Players choose among current ESCAPE rows for chance vs turn cost. Do not add extra safer escape rows, and do not change current `SuccessRate` / `ShowCondition` / turn values, until those rates and unlocks are approved.

**Why:** Owner corrected `ActionTable` `ACT_013` Notes (“성공 확률 판정 없이”) vs the GDD/WBS chance formula. Push Your Luck needs a real escape roll.

**Follow-through:** Keep `ActionResolver` chance formula and D-011 end checks. Fix `ACT_013` Notes only. EventEligible / event pick stay M1-7.

### D-015 — Event draw and option resolution (2026-11-09)

**Decision:** M1-7 event processing is closed.

- Event success is the chosen option’s success-chance roll succeeding. A failed option roll is event failure. Appearing, or merely picking an option, is not success. A 100% option still uses the final chance formula.
- `PhaseFilter` uses the **pre-action** phase of the action that triggered the check. Post-action `CurrentPhase` is not used for this filter. `ActionResolver` action-select logic stays unchanged; the caller snapshots the phase and passes it to `EventResolver`.
- Candidate filter (all required): `Enabled=1`, `PhaseFilter` (`ALL` or pre-action phase), `EventTags` ∩ `Shop.AllowedEventTags`, inclusive `MinAlert`–`MaxAlert`, inclusive `MinRemainingTurn`–`MaxRemainingTurn`, Repeatable (D-013). Alert/turns are read **after** the action’s D-011 end check.
- Draw eligibility (`EventCheckDue`), candidate eligibility, trigger chance, and `Weight` pick are separate. `Weight` is not trigger chance.
- Option chance `%` = `clamp(Option.SuccessRate + Shop.SuccessRateBonusPp, MIN_SUCCESS_RATE, MAX_SUCCESS_RATE)` — same formula as actions, bonus in percentage points. Do not change CSV numbers.
- A drawn event sets pending `EventID`. While pending, actions cannot be selected or applied. Only a valid option of that event may resolve it. After the option (including if the run ends), clear pending. A foreign `OptionID` cannot resolve the pending event.
- After an option, `LastActionID` stays the triggering action. Do not store `OptionID` as an ActionID. `successfulActionIds` and `successfulEventIds` are separate.
- No chain: `EVENT_CHAIN_ENABLED=0`, `MAX_EVENT_PER_ACTION=1`. At most one event per action. Do not draw after an option.
- Event options are not escape actions. Option success cannot win. D-011 end-check order is reused, not copied.

**Why:** Owner closed M1-7 planning questions (success timing, PhaseFilter snapshot, inclusive windows, option shop bonus, pending, LastActionID).

**Follow-through:** New `EventResolver`. `ActionResolver` still only reports `EventCheckDue` / `ComputeEventChance` and does not draw inside `TryResolve`. WBS M1-7 Done after Unity Editor 66/66 and QA PASS.

### D-016 — Result feedback is presentation-only (2026-11-09)

**Decision:** M1-8.5 result copy and reactions do not change Action / EventOption / Effect gameplay rules.

- Optional 10th table `ResultFeedbackTable.csv` (`FeedbackID, SourceType, SourceID, SuccessText, FailText, SuccessPresentationID, FailPresentationID, Enabled, Notes`). Missing or invalid rows warn and fall back to display name; they do not set `HasErrors`.
- UI shows `RunState` before/after snapshot deltas. Do not recompute CSV expected candy/alert/turn values for the result modal.
- After a choice: lock input → resolve → short anticipate → reveal → hold. Tap skips only during anticipate/reveal. Hold advances only with `[계속]`.
- Event **draw** timing stays D-015. Pending event UI appears after `[계속]` on the action result, not immediately.
- A run-ending action still shows success/fail feedback. After `[계속]`, stay ended (no more choices). Result screen / title-lobby return is M1-9 via `EndedRunContinued`.

**Why:** Owner approved M1-8.5 with P0 character reactions and `[계속]` hold UX.

## Superseded — v0.1

| Old decision | Status |
|---|---|
| MVP is a single daytime stealth stage | Superseded by D-001 |
| Night, companion, gacha, cooldown, auto-skip excluded | Still excluded, but for v0.2 reasons (D-004 / GDD out of scope) |
| WASD movement, guard FOV, key candy, gold-on-clear, WebGL loop | Superseded |
| Detection delay / shopper detection / persistent gold as open v0.1 items | Closed as obsolete. Do not implement |

## Pending (block M1/M2 if unanswered)

| ID | Topic | Needed by | Notes |
|---|---|---|---|
| P-001 | CSV path and columns | — | Closed: D-008, `Assets/Resources/Table` |
| P-002 | M1 events | — | Closed: EVT_001–EVT_005 Enabled |
| P-003 | Action IDs | — | Closed: ACT_001–ACT_013 |
| P-004 | Optional skip | — | Closed: only ACT_013 leaves LOOT; MOVE/INFILTRATE cannot be skipped |
| P-005 | M1 default shop | — | Closed: D-010. Test/M1 auto-select `SHOP_001` only; shop-select is M2 |
| P-006 | RNG seed / replay | M2 | Not required for M1 |
| P-007 | `risk_bonus` formula | M3 | Use `BASE_SCORE_MULTIPLIER` until then |
| P-008 | Cosmetic currency name | M4 | TEMP id `cosmetic_point` |
| P-009 | Shop count | — | Closed: 3 enabled shops |
| P-010 | Save backend | M4 | TEMP: PlayerPrefs |
| P-011 | Update `.cursor/rules/core.mdc` WebGL wording | anytime | Docs now Android-first; rule file still says WebGL MVP |
| P-012 | CandyMultiplier rounding | — | Closed: D-009. Integer run candy; `CeilToInt` once on escape success; escape fail = 0 |
| P-013 | Action with TurnCost > remaining turns | — | Closed: D-011. Cannot select if cost > remaining; last-turn escape can win; alert ≥ max beats escape |

## Conflict check (2026-11-05)

| Topic | Resolution |
|---|---|
| GDD v0.1 vs v0.2 loop | v0.2 current; v0.1 in GDD Appendix A |
| PLAN 6-week WebGL vs M1–M5 | PLAN uses M1–M5 |
| Score vs v0.1 gold | candy × risk_bonus; gold-on-clear retired |
| Detection FOV vs alert | alert meter; FOV not in v0.2 |
| core.mdc WebGL | Implementation still must not invent packages; platform goal is Android. Update the rule file in a later docs pass (P-011) |
| WBS vs GDD M1 | WBS M1 does not include shop select (that is M2), matching GDD |
| GDD TEMP schema vs CSV | CSV wins (D-008). Success rates are percent 0–100, not 0–1 |
| M1 “이벤트 3종” vs EventTable | Table has 5 Enabled events; load Enabled rows, do not hardcode 3 |
| EffectTable vs option columns | MVP uses Action/EventOption numeric columns; EffectTable rows are Implemented=0 |
| Candy × multiplier per action vs D-009 | D-009: integer accumulate during run; multiplier once on escape success with `CeilToInt`; floor discarded |
| Turns 0 always fail vs last-turn escape | D-011: turns 0 fails unless this action was a successful escape with candy ≥ min; alert ≥ max still wins the fail check |
| Repeatable=1-only fail retry vs D-013 | D-013: fail-retry for Repeatable 0 and 1; Repeatable=0 blocks only after success. EventID uses the same meaning (D-015) |
| ACT_013 Notes skip-roll vs GDD chance formula | D-014: escape and ACT_013 use the same success roll; meeting conditions does not auto-win; Notes updated; no new escape rows |
| Event success timing / PhaseFilter snapshot | D-015: option chance roll; pre-action phase; inclusive alert/turn windows; pending blocks actions |
