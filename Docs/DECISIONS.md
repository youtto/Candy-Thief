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

**Superseded in part by D-026:** `ShopRumorTable`, `ShopRumorCandidate`, and `QuestTable` are added for rumors and quests. `EffectTable` stays unimplemented. `ResultFeedbackTable` stays the optional presentation table from D-016.

### D-006 — UI stack

uGUI + TextMeshPro. Portrait, Canvas Scaler, 1080×1920, anchors, Safe Area. Action choices and event options share one choice UI. M1 is placeholder UI.

### D-007 — Existing project freeze for this pivot

Do not delete existing scenes, scripts, or assets. Do not change packages for the document pivot. v0.2 systems are added, not swapped in by removing v0.1 files.

### D-008 — CSV files are the column source of truth (2026-11-05)

**Decision:** Load tables from `CandyThief/Assets/Resources/Table/` (UTF-8 BOM). Column names and IDs in those CSVs replace the earlier GDD TEMP schema. `README.csv` is loader/rules notes, not a runtime table.

**Closed by the files:** P-001 columns; P-002 events EVT_001–EVT_005; P-003 actions ACT_001–ACT_013; P-004 optional is `ACT_013` only (LOOT → ESCAPE); P-005/P-009 three shops SHOP_001–SHOP_003.

**Superseded in part by D-021 / D-026:** Those three shops remain the on-disk prototype. The shipping roster target is 20. Their `ShopID`s and resolve numbers stay. New columns and rumor/quest tables are specified in D-026 and are not in the CSV files yet. Until that later edit, the files on disk are still what the loader reads.

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

**Superseded by D-018:** Title auto-enter Lobby is replaced by nickname login. Hub Title → Lobby → Shop Select still stands.

**Superseded in part by D-020:** Lobby energy / gold / candy widgets are no longer display-only placeholders. Gacha, companions, and other hub menus stay “준비 중”.

**Superseded in part by D-021:** Player shop select is a saved 4-card list, not every enabled `ShopTable` row. `작전 시작` still opens that step. Test auto-select of `SHOP_001` stays D-010.

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

**Not changed by D-022:** Shop rumors are a different table and lifetime. They do not use this draw, and they do not change this resolve order.

### D-016 — Result feedback is presentation-only (2026-11-09)

**Decision:** M1-8.5 result copy and reactions do not change Action / EventOption / Effect gameplay rules.

- Optional 10th table `ResultFeedbackTable.csv` (`FeedbackID, SourceType, SourceID, SuccessText, FailText, SuccessPresentationID, FailPresentationID, Enabled, Notes`). Missing or invalid rows warn and fall back to display name; they do not set `HasErrors`.
- UI shows `RunState` before/after snapshot deltas. Do not recompute CSV expected candy/alert/turn values for the result modal.
- After a choice: lock input → resolve → short anticipate → reveal → hold. Tap skips only during anticipate/reveal. Hold advances only with `[계속]`.
- Event **draw** timing stays D-015. Pending event UI appears after `[계속]` on the action result, not immediately.
- A run-ending action still shows success/fail feedback. After `[계속]`, stay ended (no more choices). Result screen / title-lobby return is M1-9 via `EndedRunContinued`.

**Why:** Owner approved M1-8.5 with P0 character reactions and `[계속]` hold UX.

### D-017 — Art production pipeline (2026-11-10)

**Decision:** All Candy Thief art (title, lobby, characters, backgrounds, props, UI) follows `Docs/Art/SCENARIO_PIPELINE.md`. Visual style stays `ART_STYLE_GUIDE.md`.

1. Prefer reusable independent assets (background, characters, props, logo, UI widgets). Composite plates are allowed when cost outweighs reuse; report the reason.
2. Image generation and visual edits go through Scenario AI. Cursor does not create or retouch pixels (Pillow, chroma-key, local inpaint, crop-from-comp, flood fill).
3. Approved comps are design references. Needed layers are regenerated as new Scenario assets, not extracted from the plate.
4. Decide layering per job (reuse, animation, Unity control, CU). Estimate CU with `dry_run` and wait for approval. No extra retries or model swaps without approval.
5. ART-TTL-002 v002 remains the approved title comp. Failed cutouts/inpaints are not final. Title layers, if made later, are new Scenario sprites. Passed independent UI (`ART-TTL-005` / `ART-TTL-006`) may be reused. Using v002 as a single Unity image is allowed.

**Why:** ART-TTL-002 local/851 extraction damaged outlines, dropped overlapping hands/props, and left background matte. Owner replaced that pipeline.

**Follow-through:** Historical local extracts (early ART-LOB-002, blink Closed v001, TTL-002 cutouts) stay on disk and are not a template. Existing Integrated art is unchanged until a separate Unity task.

**Exception (2026-11-12, owner):** Non-game geometric silhouette guides may be drawn locally (Python/SVG) and uploaded to Scenario as shape references only. Do not crop or retouch existing art. Do not use these guides as Unity sprites. This does not lift the D-017 ban on extracting or retouching production art.

### D-018 — Title nickname login (2026-11-10)

**Decision:** Title stays until the player submits a valid nickname. Lobby is entered only after a successful local profile login. D-012 auto-lobby after ~1 second is retired.

- Title plate is ART-TTL-002 v002 (`Assets/Art/Title/CT_Title_Day_v002.png`). `CT_Title_A_v001` is preserved and unused.
- Nickname frame is ART-TTL-005 v001. CTA remains ART-TTL-006 v002. No new Scenario images.
- Nickname 2–12 chars, Hangul / English / digits, trim, case-insensitive identity. Duplicate nickname loads the existing profile and keeps the stored display string.
- Profiles are JSON at `Application.persistentDataPath/CandyThief/profiles.json`. This is not P-010 (M4 PlayerPrefs). Do not overwrite other nicknames or wipe a corrupt file.
- Last nickname is restored into the input on relaunch. Do not auto-login.
- Save failure or invalid input stays on Title and shows TMP error copy.

**Why:** Owner requested M1 title art swap plus local nickname login in one pass.

**Follow-through:** Fade, lobby art/animation, and run flow stay as before. Progress/currency per profile is later work. P-010 remains M4.

### D-019 — Scene-authored static UI (2026-11-11)

**Decision:** Designers edit static UI in Unity Editor. Code owns behavior, not RectTransform layout.

- Static screens (title, lobby, settings, shop, fixed HUD) live in a scene or prefab. Hierarchy selection, Scene View move, Inspector size/color/sprite, and scene save must work in Edit Mode and persist through Play Mode.
- Dynamic UI (inventory slots, repeating lists, rewards, data-driven rows, toasts) may instantiate from prefabs at runtime.
- Do not hardcode RectTransform anchors/size in runtime `Awake`/`Start` unless the widget is generated from data. Do not rebuild a saved scene layout on play.
- Title UI is scene-authored on `Title.unity` (`TitleCanvas`). Lobby is scene- and Prefab-authored (`Assets/Prefabs/V02/UI/Lobby.prefab`, instance under Title SafeArea; edit/preview scene `Assets/Scenes/V02/Lobby.unity`). Shop Select / debug / Coming Soon remain runtime-built until a later pass. Bake menus create Title/Lobby UI only when absent.

**Why:** Runtime-built title UI could not be tuned in Edit Mode.

**Follow-through:** Keep sprites on the Image components. `V02LobbyArtController` binds Background/Thief and must not overwrite RectTransforms. Play the open scene; do not force Play Mode onto Title.unity.

**Extended by D-021 / D-023:** Operation prep (2×2 cards) and the quest board are static prefab screens when those tasks start. The four cards and six quest slots may instantiate from prefabs. The current runtime Shop Select builder stays in the project (D-007) and is no longer the player path once the prefab screen is wired. Do not delete it in the docs pass. The lobby control that opens the quest board is P-015; do not retarget `크루` or `강화`.

### D-020 — Lobby HUD currencies and bottom actions (2026-11-11)

**Decision:** Lobby HUD shows real saved values for nickname, energy, gold, and candy. D-012 placeholder policy for those four widgets is replaced. Other hub menus stay unimplemented.

- **Nickname:** D-018 `V02LocalProfileService` display string. No second identity store.
- **Energy (행동력):** integer, new player 10, max 20, regen 1 per 10 minutes from saved timestamp (including while the app is closed). Spend amount and spend timing are **not** defined; do not invent a consume rule.
- **Gold:** integer wallet on the profile. Display with the shared abbreviation helper. Grant source and formula are **not** defined; HUD shows 0 until a later decision. Do not add a gold grant.
- **Candy (lobby):** cumulative `finalReward` after a run ends. This pass stores and displays the integer only. Computing and granting `finalReward` stays M1-9. Fail / non-win grant is 0 (D-009). This is not `RunState.Candy` and not cosmetic `cosmetic_point`.
- **Save:** extend D-018 JSON (`profiles.json`) per nickname. Do not use P-010 PlayerPrefs for these fields. Do not wipe other nicknames or corrupt files.
- **Abbreviation:** gold full digits through 9,999 then K/M/B with one decimal, trim trailing `.0`, floor so display never exceeds the real integer. Candy full digits through 999, then the same K/M/B rule.
- **Bottom buttons, left → right:** 크루 (mint, 준비 중) · 작전 시작 (yellow, slightly emphasized, Shop Select) · 강화 (coral, 준비 중). Hangul and icons are baked into the button sprites (title CTA exception). No TMP duplicate on those three. Label was `업그레이드`; owner changed it to `강화` (2026-11-12).
- **HUD art:** chrome and icons only. Nickname, energy, gold, candy values are TMP. No XP bar, no currency `+` buttons. Settings icon stays and shows 준비 중. Profile avatar stays (reuse idle display sprite in the frame). Companion `+` slots are removed from the lobby layout.
- **Art reuse:** `CT_Lobby_Background_v002`, `CT_Lobby_Thief_Idle_v007_display230`, existing blink frames. Do not regenerate them. Idle/blink stay.

**Why:** Owner closed Phase A conflicts and asked for a REF-UI-003-like lobby with real HUD numbers, without implementing spend or gold income yet.

**Follow-through:** Crew v003 / Start v003 / Enhance v002 and HUD sheet `CT_Lobby_HUD_Sheet_v001` are art-approved and placed on the scene-authored lobby. Gold stays 0 until a grant rule exists. Do not grant candy until M1-9 `finalReward`.

**Not changed by D-024 / D-025:** No energy spend, no gold spend, no paid reroll, no BM purchase. Quest currency payout is P-017, not an exception in this decision.

### D-021 — Operation prep shop list (2026-11-14)

**Decision:** P-009’s three shops are the M1 prototype roster, not the shipping cap. The shipping target is 20 shops. The player sees 4 of them.

- `SHOP_001`–`SHOP_003` keep their IDs and their resolve numbers: `StartTurn`, `StartAlert`, `SuccessRateBonusPp`, `CandyMultiplier`, `EventRateBonusPp`.
- Do not rename those three, and do not assign their new grades or backgrounds, until the 20-shop name source is in the repo. It is not in the repo as of this decision (P-016). Do not invent names. (Superseded by D-027: the roster is approved and the three are renamed.)
- The other 17 shops are added from that same source. Each shop gets a distinct `BackgroundImageKey`. This decision does not generate images.
- Lobby `작전 시작` opens operation prep: portrait, 4 cards, 2×2. One list has no duplicate `ShopID`.
- Each shop has a relative `SpawnWeight`. Weights may differ from the start. Weight `0` is not drawn. An active quest does not change any shop’s weight.
- A new list is created only after a confirmed win, a confirmed fail, or an explicit abandon (D-025). Leaving the screen and coming back keeps the list.
- Shop display name length is at most 10 characters, spaces included (`string` length).
- Distance, scale, security, and goods are fixed labels on the shop. They do not replace the resolve columns. Resolvers keep reading the numeric columns. Grade tokens and the label-to-effect map are D-026. Tuned weights, grades, and `EnergyCost` values are P-016. Do not invent them.

**Why:** Owner set the shipping roster, the 4-card list, and weighted draw. Replacing `StartTurn` or `SuccessRateBonusPp` with a grade enum would change D-009 / D-014 results for the current three shops.

**Follow-through:** Column contract is D-026. Draw rules and the candidate shortage fallback are D-024. UI authoring is D-019. WBS package is OP-DEV.

### D-022 — Shop rumors are not in-run events (2026-11-14)

**Decision:** A rumor is rolled onto a shop when the shop list is created. It is not an `EventTable` row and it is not an `EffectTable` row.

- Each listed shop has 0, 1, or 2 rumors. Count weights are 40 / 45 / 15. The same rumor id cannot appear twice on one shop. Two rumors that share a non-empty conflict group cannot both land on one shop.
- Reroll redraws rumors. After the run starts, rumors on that run do not change.
- A rumor’s effect is a delta on the existing shop snapshot only: `SuccessRateBonusPp`, `StartAlert`, `CandyMultiplier`, `EventRateBonusPp`. The snapshot is built once at run start. D-011 order and D-014 / D-015 formulas then use that snapshot. No rumors means the snapshot equals the shop row.
- `StartTurn` is not a rumor target. Collectible drop weight is not a rumor target; there is no item table.
- Do not set `EffectTable.Implemented` or `Enabled` to turn rumors on.
- Hint text must describe the same deltas the row applies. Do not enable a rumor whose only effect would be a collectible.

**Why:** `EventTable` is the mid-run choice after an action (D-003, D-015). Putting rumors in that table would change candidate filters, pending options, and chain rules. `EffectTable` is still unimplemented.

**Follow-through:** Tables are D-026. Count weights are confirmed `GameConfig` values. Per-rumor delta numbers are not confirmed; new rumor rows wait on content approval. Existing EVT_001–EVT_005 stay as they are.

### D-023 — Quest board (2026-11-14)

**Decision:** Quests are not `AchievementTable` titles. The board has at most 6 slots. At most 3 of those count as active. Active means accepted or ready to claim. An offered quest does not count as active.

- The board is a lobby screen (prefab, D-019). Which lobby control opens it is P-015. Do not reuse `작전 시작`, and do not reassign `크루` or `강화`.
- Accepting a quest keeps the same slot and marks it accepted. Accept is rejected when active count is already 3.
- Progress increments only when a run’s win is settled (D-025). Fail and abandon do not increment it. When progress reaches the target, the slot becomes claimable and stays active.
- Daily reset and manual reroll do not remove accepted or claimable slots. They replace offered slots only.
- Claim refills that slot immediately with a new offered quest. There is no daily claim cap. Claim does not spend a reroll. Currency paid by a claim is P-017; do not grant gold or lobby candy from this decision.
- A newly drawn quest must not target a shop that is disabled or not drawable (`SpawnWeight` ≤ 0). Duplicate quest ids inside the six slots are forbidden.
- Quest state does not change shop `SpawnWeight`.

**Why:** Achievements are permanent title rows with no slots, no accept, and no daily board. Overloading `AchievementTable` would mix M4 titles with a renewable board.

**Follow-through:** Objective types that can be counted now are clear-this-shop and clear-a-shop-of-this-grade. Item, quantity, and collectible objectives stay out. Slot storage is D-026. Operation-day reset and reroll caps are D-024.

### D-024 — Reroll caps and operation day (2026-11-14)

**Decision:** Shop-list reroll and quest-board reroll share the same attempt rules and use separate counters. Paid reroll and BM purchases are not in this implementation. Keys for later costs may exist; code must not charge gold.

Shop list:

- Free attempts per list: 1. This 1 counts toward the max.
- Max attempts per list: its own setting, greater than or equal to the free count. The number is P-014.
- Until P-014 is set, the effective max equals the free count. After free is used, further rerolls are refused. Do not offer a paid reroll.
- A new list from win, fail, or abandon resets both shop reroll counters. Opening the screen does not.
- Draw 4 shops, no duplicate ids, weights, skip weight 0. Prefer to exclude the previous list’s 4 ids. If that pool has fewer than 4, drop only that exclusion and draw again. If the drawable pool itself has fewer than 4, the reroll fails.
- On failure, keep the current list and do not increment either counter. On success, redraw rumors and increment the free counter if a free attempt was spent, and always increment the total counter.

Quest board:

- Free attempts per operation day: 2, counted inside the max.
- Max attempts per operation day: its own setting, P-014. Until that value exists, the effective max equals 2.
- Replace offered slots only. First exclude quests that remain on the board and the quests this attempt just replaced. If that pool is short, drop only the “just replaced” exclusion. If the board would still contain a duplicate or an offered slot cannot be filled, the reroll fails and counters stay unchanged.
- Claiming a reward and the daily refresh are not reroll attempts.

Both:

- Free allowance and max are separate settings. Max is at least the free allowance. A later BM bonus may raise the free allowance and must still not exceed the max. Whether BM may raise the max is not decided; do not raise it.
- Store free uses and total uses separately. Only a successful reroll increments them.
- At the effective max, the refresh control is disabled and shows that the limit is reached.
- Operation day boundary: 05:00 Asia/Seoul (UTC+9, no daylight saving), which is 20:00 UTC. The operation-day id is the KST calendar date of `now minus 5 hours`. Apply quest offered-slot refresh and quest reroll counter reset once per that id. A second call on the same id does nothing. Accepted and claimable slots stay.

**Why:** Owner rejected unlimited rerolls, excluded paid reroll from this slice, and fixed the operation day at KST 05:00. One shortage rule (relax the previous-list exclusion, then fail) avoids an empty board when the pool is only slightly larger than 4.

**Follow-through:** `GameConfig` key names are D-026. Do not write a guessed max or a guessed price into CSV.

**Implemented in OP-DEV-05:** `GameConfig` has `SHOP_LIST_SIZE=4` and `SHOP_REROLL_FREE_PER_LIST=1`. `SHOP_REROLL_MAX_PER_LIST` is still absent (P-014), so the effective max is the free count. The shop list, per-shop rumor ids, both counters, and the previous list are saved in the profile (`shopList`), with `activeRunId` and `settledRunId`, on the same schema id. A run gets a `RunState.RunId`. The next list is drawn when the ended-run feedback is confirmed (win or fail), once per run id. A killed process keeps the list and clears `activeRunId` only. Quest fields and `lastOperationDay` are not added yet.

### D-025 — Abandon versus process kill (2026-11-14)

**Decision:** Explicit abandon and a killed process are different. Neither one spends energy. D-020 still has no spend rule.

Explicit abandon:

- The run HUD’s lobby action opens a confirm dialog. Cancel leaves the run as it is.
- Confirm settles the run as `Failed`, once. Settlement refreshes the shop list (D-021, D-024). Quest progress does not increase (D-023).
- The same settlement id cannot be applied twice.

Process kill or cold start with no live run:

- Do not treat it as abandon. Do not restore turn-by-turn `RunState`. Do not mark fail. Do not refresh the shop list. Do not change quest progress. Do not grant rewards.
- If a departure cost is added later, refund and abuse rules need a new decision. This decision does not refund anything, because nothing is spent.

**Why:** The current lobby button on the run HUD only calls `ShowLobby`. Making that button a silent fail would change today’s behavior without a confirm. There is no saved run to resume.

**Follow-through:** Persist the settlement id and, while a run is on screen, an active run id (D-026). On the next launch, clear a leftover active run id and do nothing else.

### D-026 — OP-DEV-02 data contract (2026-11-14)

**Decision:** This is the column and save contract. It does not edit CSV, C#, scenes, or prefabs. The loader keeps failing loud on duplicate ids and broken references. `Enabled=0` still drops a row from runtime lookup. Lookup stays by id.

`ShopTable` keeps every current column. Add:

| Column | Type | Rule |
|---|---|---|
| `SpawnWeight` | INT ≥ 0 | Relative weight. `0` is not drawn. Values are P-016 |
| `DistanceGrade` | `NEAR` / `MID` / `FAR` | Labels: 가까움 / 보통 / 멀리. Does not change `StartTurn` |
| `ScaleGrade` | `SMALL` / `MEDIUM` / `LARGE` | 소형 / 중형 / 대형. Payout stays `CandyMultiplier` (D-009) |
| `SecurityGrade` | `LOOSE` / `NORMAL` / `STRICT` | 허술 / 보통 / 삼엄. Chance and alert stay the numeric columns |
| `GoodsGrade` | `NORMAL` / `HIGH` / `TOP` | 보통 / 고급 / 최고급. No collectible weight |
| `EnergyCost` | INT ≥ 0 | Stored only. Resolvers and the profile must not subtract it until a spend decision exists. Placeholder `0` on the current three shops is not a tuned price |

`GameConfig` adds these keys. Confirmed values may be written when CSV edits are allowed. Pending keys are not given a guessed number.

| Key | Value | ValueType | Role |
|---|---|---|---|
| `SHOP_LIST_SIZE` | 4 | INT | Cards per list |
| `SHOP_REROLL_FREE_PER_LIST` | 1 | INT | Free shop rerolls per list |
| `SHOP_REROLL_MAX_PER_LIST` | P-014 | INT | Max shop rerolls per list, includes free |
| `QUEST_SLOT_COUNT` | 6 | INT | Board size |
| `QUEST_ACTIVE_MAX` | 3 | INT | Accepted + claimable cap |
| `QUEST_REROLL_FREE_PER_DAY` | 2 | INT | Free quest rerolls per operation day |
| `QUEST_REROLL_MAX_PER_DAY` | P-014 | INT | Max quest rerolls per operation day |
| `OPERATION_DAY_RESET_HOUR_KST` | 5 | INT | D-024 boundary |
| `RUMOR_COUNT_WEIGHT_0` | 40 | INT | Rumor-count weight |
| `RUMOR_COUNT_WEIGHT_1` | 45 | INT | Rumor-count weight |
| `RUMOR_COUNT_WEIGHT_2` | 15 | INT | Rumor-count weight |

Reserved key names, unread until a paid/BM decision names their values: `SHOP_REROLL_PAID_BASE`, `SHOP_REROLL_PAID_STEP`, `SHOP_REROLL_PAID_CAP`, `QUEST_REROLL_PAID_BASE`, `QUEST_REROLL_PAID_STEP`, `QUEST_REROLL_PAID_CAP`, `SHOP_REROLL_BM_FREE_BONUS`, `QUEST_REROLL_BM_FREE_BONUS`.

`ShopRumorTable` — `RumorID` PK. Columns: `RumorName`, `RumorText`, `HintText`, `ConflictGroup` (empty means no conflict), `SuccessRateBonusPp`, `AlertDelta`, `CandyMultiplierDelta`, `EventRateBonusPp`, `Enabled`, `Notes`. No weight column. No `EffectID`.

`ShopRumorCandidate` — pair PK `ShopID` + `RumorID`. Column: `Weight` (INT, the only rumor draw weight). `ShopID` must exist on `ShopTable`. `RumorID` must exist on `ShopRumorTable`.

`QuestTable` — `QuestID` PK. Columns: `Title`, `Description`, `GiverName` (display only), `ObjectiveType` (`CLEAR_SHOP` or `CLEAR_SHOP_GRADE`), `TargetShopID`, `TargetGradeAxis` (`NONE` / `DISTANCE` / `SCALE` / `SECURITY` / `GOODS`), `TargetGrade` (grade token or empty), `TargetCount` (INT ≥ 1), `RewardGold`, `RewardCandy`, `Weight`, `Enabled`, `Notes`. `RewardGold` and `RewardCandy` are not paid out until P-017. A `CLEAR_SHOP` target must be drawable. A `CLEAR_SHOP_GRADE` target must match at least one drawable shop. No other objective type.

`AssetTable` stays the image registry. New shops add rows; this decision adds no files.

`ActionTable`, `EventTable`, `EventOptionTable`, `AchievementTable`, and `PhaseTable` are unchanged.

Save: keep schema id `candythief.v02.profiles.v1`. Add fields on the profile. Do not bump the schema id; a bump makes existing files fail as corrupt. Missing new fields mean an empty board and zero counters.

- Shop list: `listId`, four `shopIds`, rumor pairs (`shopId`, `rumorId`), `freeRerollUsed`, `totalRerollUsed`, previous list ids used only as the next exclusion set.
- Quests: six slots of `questId`, state `OFFERED` / `ACCEPTED` / `CLAIMABLE`, `progress`.
- Quest counters: `questFreeRerollUsed`, `questTotalRerollUsed`.
- `lastOperationDay`: the D-024 operation-day id already applied.
- `activeRunId`: set when a run is showing. Cleared on settlement and on a cold start that has no live run.
- `settledRunId`: last run id whose win/fail/abandon settlement finished. The same id does not settle again.

Validation to add beside the current reference checks, when the tables exist: duplicate ids; shop name length; grade tokens; `SpawnWeight` ≥ 0; `EnergyCost` ≥ 0; background key in `AssetTable`; rumor and quest foreign keys; rumor snapshot `CandyMultiplier` > 0; quest targets that cannot be drawn; max key present and smaller than the matching free key; paid keys, if present, with a negative cost.

**Why:** Owner asked for the smallest fit. Grades as extra columns keep today’s numbers authoritative. Rumor weight lives only on the candidate row, so shop-specific rates do not fork the rumor text. A second effect table would duplicate `EffectTable` or turn it on early.

**Follow-through:** OP-DEV-03 may add loader support and the draw after a separate implementation approval. It must not fill P-016 names or weights by invention. With only three drawable shops, a 4-card list cannot succeed; that failure is correct until at least four drawable shops exist.

**Implemented in OP-DEV-03 (2026-11-14):** The six `ShopTable` columns are required by the loader. A blank grade cell means “not assigned” (`NONE`); any other grade text must be an exact token above. `SpawnWeight` and `EnergyCost` must be integers ≥ 0. `ShopName` over 10 characters (spaces included) is a load error. The three on-disk shops carry `SpawnWeight=0`, blank grades, and `EnergyCost=0` because P-016 has no values yet. Weight `0` means not drawn, so none of them is drawable until P-016 lands. `GameConfig` keys from the table above are not added yet; the draw takes the list size as an argument.

**Implemented in OP-DEV-04 (2026-11-14):** `ShopRumorTable.csv` and `ShopRumorCandidate.csv` are on disk with headers only (no rumor rows; effect numbers are not approved). The loader reads them as optional tables like `ResultFeedbackTable`: missing files mean no rumors, present files are validated fail-loud (unique `RumorID`, FKs, `Weight` ≥ 0, non-empty text and hint, enabled rumor must change at least one value, and the worst two-rumor combination must keep `CandyMultiplier` > 0). `GameConfig` now holds `RUMOR_COUNT_WEIGHT_0/1/2 = 40/45/15`; they are not in the required-key list. `ShopRumorDrawer` (reuses `IIntRoll`) draws the count, then candidates by `Weight` without replacement, skipping a picked rumor’s `ConflictGroup`; a short pool yields fewer rumors. `RunState.TryStart(database, shopId, rumorIds, out state)` builds a cloned `ShopRow` snapshot (`RunState.Shop`) with summed deltas; `ActionResolver`, `EventResolver`, and the run HUD read that snapshot. `StartAlert` is clamped to `[0, MAX_ALERT]`; success and event chance keep their existing clamps. Original `ShopRow` objects are never changed. Formulas and resolve order are unchanged.

### D-027 — P-016 first roster: names, grades, weights (2026-11-14)

Approved as 1st-pass planning values. **All 20 shops are registered in `ShopTable.csv` (2026-11-14).** `SHOP_001`–`SHOP_003`: name, grades, `SpawnWeight`, background changed; resolve numbers unchanged. `SHOP_004`–`SHOP_020`: the `Proposed_*` resolve numbers in `Docs/Data/P016_ShopRoster_Draft.csv` are approved as **1st-pass test balance** (`AllowedEventTags=COMMON`, `UnlockCondition=NONE`, `Enabled=1`, `EnergyCost=0`). The first-pass numbers derive from the three prototype shops: scale sets `StartTurn`, `EventRateBonusPp`, and the base multiplier; security sets `StartAlert` and `SuccessRateBonusPp`; goods adds to `CandyMultiplier`; distance has no resolve effect.

**`EnergyCost` stays `0` on all 20 shops.** Proposal, not approved: NEAR 1, MID 2, FAR 3. No spend, shortage check, or refund exists (D-020). Rumor rows stay unregistered until a separate rumor design.

| ShopID | Name | Dist | Scale | Security | Goods | Weight |
|---|---|---|---|---|---|---|
| SHOP_001 | 토마스 사탕점 | NEAR | SMALL | LOOSE | NORMAL | 100 |
| SHOP_002 | 잭의 과자점 | MID | MEDIUM | NORMAL | HIGH | 65 |
| SHOP_003 | 르블랑 백화점 | FAR | LARGE | STRICT | TOP | 15 |
| SHOP_004 | 브라운 솜사탕 | NEAR | MEDIUM | LOOSE | NORMAL | 85 |
| SHOP_005 | 로즈 캔디샵 | MID | SMALL | NORMAL | HIGH | 65 |
| SHOP_006 | 밀러 초콜릿 | MID | MEDIUM | STRICT | HIGH | 45 |
| SHOP_007 | 모리스 제과점 | NEAR | MEDIUM | NORMAL | NORMAL | 85 |
| SHOP_008 | 루시의 잡화점 | FAR | SMALL | LOOSE | HIGH | 45 |
| SHOP_009 | 벨라 디저트 | MID | MEDIUM | NORMAL | HIGH | 60 |
| SHOP_010 | 오스카 사탕창고 | FAR | LARGE | NORMAL | NORMAL | 55 |
| SHOP_011 | 피에르 공방 | FAR | SMALL | STRICT | TOP | 25 |
| SHOP_012 | 에드가 상회 | MID | SMALL | LOOSE | TOP | 10 |
| SHOP_013 | 헨리의 사탕집 | NEAR | SMALL | LOOSE | NORMAL | 100 |
| SHOP_014 | 마담 코코 | MID | MEDIUM | NORMAL | TOP | 20 |
| SHOP_015 | 올리버 제과점 | NEAR | MEDIUM | NORMAL | HIGH | 60 |
| SHOP_016 | 블랙우드 상점 | FAR | MEDIUM | STRICT | TOP | 20 |
| SHOP_017 | 베니의 젤리샵 | NEAR | SMALL | LOOSE | HIGH | 45 |
| SHOP_018 | 몽슈슈 과자점 | MID | MEDIUM | LOOSE | NORMAL | 80 |
| SHOP_019 | 윌슨 무역상 | FAR | LARGE | STRICT | HIGH | 35 |
| SHOP_020 | 도로시 사탕집 | MID | SMALL | NORMAL | TOP | 25 |

Backgrounds: `AssetTable` image keys are lower snake case (`shop_neighborhood`), so the 20 keys are `shop_bg_001`–`shop_bg_020` (`AssetID` `IMG_009`–`IMG_028`, `PromptKey` `SHOP_BG_001`–`SHOP_BG_020`). No images exist; paths are planned only. `IMG_001`–`IMG_003` (`shop_neighborhood`, `shop_mall`, `shop_warehouse`) stay in the table, unreferenced.

### D-028 — In-run HUD and action-select time (2026-11-15)

**Decision:** After a run starts, phase, alert, situation text, and the action-select limit are part of the portrait HUD. OP-DEV-08, OP-DEV-09, and OP-DEV-11 stay deferred. New work is PLAY-01, PLAY-02, PLAY-03. `REF-UI-001` is the look. The choice list stays the existing vertical list.

- The top of the run HUD shows `PhaseTable.PhaseName` for `RunState.CurrentPhase`.
- Alert text is `current/MAX_ALERT`. The fill stays `Alert / MAX_ALERT`. The label stays `위험도`. Candy stays the integer run candy and has no max.
- Under the illustration, action select shows `PhaseTable.SituationText`. A pending event shows `EventTable.EventDescription`. `Notes` is not player copy. A blank `SituationText` fails the load.
- First registered `SituationText` rows: MOVE `가게로 가는 길 위다.` / INFILTRATE `가게 안으로 들어갈 곳을 고른다.` / LOOT `사탕을 얼마나 가져갈지 정한다.` / ESCAPE `빠져나갈 길을 고른다.` / RESULT `작전이 끝났다.` Replace them in CSV, not in code.
- `GameConfig.ACTION_SELECT_TIME_SEC` is a required INT. The approved value is 30. Values below 1 fail the load. `GetInt` must not turn a missing key into a 0-second pick.
- The limit applies only while action choices are on screen. Event options, result feedback, and an ended run do not run it. The abandon confirm pauses it; cancel continues the remaining time. Display uses `Ceil`.
- On timeout, every `CanExecute` action has an equal chance. The pick calls the existing `ActionResolver` and the existing result feedback. No extra success or alert penalty. One claim per round, so a click and a timeout cannot both apply.
- PLAY-01 places the timer slot and shows the full allowance. It does not count down and does not auto-pick. Those are PLAY-02 and PLAY-03. PLAY-01 is not Done until a manual Unity review.

**Not decided (P-018):** What to do when no action is `CanExecute` and the run is still `InProgress`. Do not add a fail rule in this decision.

With the current tables the hole is reachable. LOOT’s cheapest steal costs 2 turns, and `ACT_013` is hidden until candy ≥ `MIN_CANDY_TO_ESCAPE`. An infiltrate success can enter LOOT with 1 turn and 0 candy (a 2-turn success on `ACT_006` leaves 1 turn, which is still above the D-011 turn-fail check, so the phase advances). MOVE, INFILTRATE, and ESCAPE each have a 1-turn action, and a run cannot be `InProgress` at 0 turns after an applied action. D-011 rejects an unaffordable action with no state change, and the turn fail runs only after an action is applied. So this state stays `InProgress`. The cards already show the turn shortfall, and D-025 abandon is the existing exit. Closing the hole without a new fail means a later content change so LOOT still has an affordable action at 1 turn and 0 candy. That balance change is not approved here. PLAY-03 must log the empty candidate list and must not invent a loss.

**Why:** The owner approved the HUD and the shared 30-second action-select limit, and asked to inspect the empty-candidate case before any new fail.

**Follow-through:** GDD, TECH_SPEC, the art guide, and WBS record this. On 2026-11-15 the owner’s Unity review passed the PLAY-01 screen structure, and D-032 closes the functional review. The shared tooltip is not moved again in PLAY-01. LOOT escape prep staying on LOOT was the intended gap until PLAY-06. D-029 is that transition.

### D-029 — Escape prep enters ESCAPE without candy (2026-11-15)

**Decision:** On LOOT, `탈출 준비하기` enters ESCAPE. It spends 0 turns and does not roll success or show result feedback. Candy 0 opens a confirm; candy 1 or more enters immediately. MOVE and INFILTRATE keep the existing notice. The button stays disabled on ESCAPE, during an event, and after the run ends.

- `PhaseTable`: LOOT `CompleteCondition=ALWAYS`, ESCAPE `EntryCondition=ALWAYS`. `TryPrepareEscape` reads those conditions. It does not skip them in code.
- `MIN_CANDY_TO_ESCAPE` stays 1 and still gates an escape-action **win**. Entering ESCAPE with 0 candy does not win or fail. End priority and result settlement stay PLAY-07.
- `ACT_013` stays in `ActionTable` with `ShowCondition=CANDY_AT_LEAST_1` and `SpawnWeight=0`. The normal hand does not draw it.
- Cancel on the empty-hand confirm changes no run numbers and no `UnityEngine.Random.state`.

**Why:** The owner approved empty-hand escape prep as a phase change, separate from the later win check.

**Follow-through:** GDD and the phase CSV match this. PLAY-07 is Done (D-030).

### D-030 — End priority and separate result popups (2026-11-15)

**Decision:** An escape-action success is judged before alert and before 0 turns. Candy ≥ `MIN_CANDY_TO_ESCAPE` is an operation success. Candy 0 is an empty-hand escape: the escape succeeded, the operation failed, and settled candy is 0. Otherwise alert max is capture, then 0 turns stays `NoTurns`. A failed escape that hits both alert and 0 turns is capture.

The action popup shows only that action’s success or failure, its feedback line, and the candy, alert, and turn deltas. Continue then opens the operation-result popup when the run has ended. That popup does not appear for abandon. Only an operation success adds settled candy to the profile, once per run. There is no loot table yet, so the loot row shows the empty line and does not grant items.

**Why:** The owner approved the four result screens and the new end order.

**Follow-through:** GDD end order matches this. The operation popup is `RunResultPopup.prefab`. On 2026-11-15 the owner’s Unity review passed PLAY-07: RunOutcome 8/8, full self-tests 15/15, the four result popups, retry and lobby, and the layout. Placeholder art is replaced later without rebuilding the layout.

### D-031 — Event select uses the same timer (2026-11-15)

**Decision:** D-028’s sentence that event options do not run the select timer stays in that record. From this decision on, event options use the same `ACTION_SELECT_TIME_SEC` countdown and the same timeout pick.

- The event countdown starts when the event options are shown, at the full allowance. It does not keep the leftover action-select time.
- Refreshing that same event does not restart it. Completing an option stops the timer. The next action select starts a new allowance.
- Timeout picks one `CanExecute` event option with equal probability and calls the existing option resolve. No extra turn, alert, or candy penalty. `SpawnWeight` is not used.
- Abandon, escape-prep notice, and empty-hand confirm pause the current countdown. Cancel resumes the remainder. Result feedback and the operation result hide and stop it. A tooltip does not pause it; if time expires while it is open, the tooltip closes and the timeout pick runs.
- One claim per round. A click and a timeout cannot both apply. After that claim, the current choices take no more input. A rejected click does not claim the round and does not stop the clock.
- Zero `CanExecute` choices, action or event, log once per round and do not pick, spend, or fail the run. That does not close P-018.

**Why:** The owner asked for the action timeout and for event choices to share the 30-second limit.

**Follow-through:** GDD, TECH_SPEC, and WBS record the current rule. PLAY-02 and PLAY-03 were reviewed Done on 2026-11-15. P-018 stays open. The forecast tooltip’s later presentation is D-032.

### D-032 — Forecast tooltip stays until the formal UI modal (2026-11-15)

**Decision:** PLAY-01’s functional close is Done. The shared candy and alert tooltip stays where this milestone left it. Do not move it again for PLAY-01. It overlaps the area the official illustration will use, so the formal UI pass replaces it with a center information modal (WBS M4-6).

That modal, when built:

- Candy and alert info buttons open one center modal.
- An outside click or a close button dismisses it.
- Input that closes the modal does not execute an action.
- The action-select timer keeps running while the modal is open.
- At 0 the modal closes and the existing timeout pick runs.
- The choice-card layout and the illustration area stay.

This does not change the timer, the timeout pick, or CSV. Card spacing, text readability, placeholder art, and P-019 stay outside PLAY-01.

**Why:** The owner accepted the current tooltip for PLAY-01 after the remaining Unity self-tests and Play Mode passed, and deferred the overlap to the formal UI pass.

**Follow-through:** WBS M4-6 and PLAN M4 record the modal. PLAY-01 is functionally Done and does not include that move.

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
| P-004 | Optional skip | — | Closed: D-029. The escape-prep button leaves LOOT. MOVE/INFILTRATE cannot be skipped. ACT_013 is not drawn |
| P-005 | M1 default shop | — | Closed: D-010. Test/M1 auto-select `SHOP_001` only; shop-select is M2 |
| P-006 | RNG seed / replay | M2 | Not required for M1 |
| P-007 | `risk_bonus` formula | M3 | Use `BASE_SCORE_MULTIPLIER` until then |
| P-008 | Cosmetic currency name | M4 | TEMP id `cosmetic_point` |
| P-009 | Shop count | — | Closed for the M1 prototype: 3 enabled shops. Shipping target is 20 (D-021). `SHOP_001`–`SHOP_003` keep ids and resolve numbers |
| P-010 | Save backend | M4 | TEMP: PlayerPrefs |
| P-011 | Update `.cursor/rules/core.mdc` WebGL wording | anytime | Docs now Android-first; rule file still says WebGL MVP |
| P-012 | CandyMultiplier rounding | — | Closed: D-009. Integer run candy; `CeilToInt` once on escape success; escape fail = 0 |
| P-013 | Action with TurnCost > remaining turns | — | Closed: D-011. Cannot select if cost > remaining; last-turn escape can win. End order is D-030, which judges escape success before alert |
| P-014 | Reroll max and paid prices | OP-DEV-05 / OP-DEV-09 | Max counts and paid base/step/cap are unnamed. Until the max is set, effective max equals the free count (D-024). Do not charge |
| P-015 | Lobby control for the quest board | OP-DEV-08 UI | Board is a lobby prefab screen (D-023). Do not retarget `크루`, `강화`, or `작전 시작` |
| P-016 | Twenty shop names, grades, weights, energy | OP-DEV-03 content | **Closed (D-027):** 20 shops registered (names, grades, weights, backgrounds, 1st-pass resolve numbers). **Still open, tracked separately:** `EnergyCost` values (NEAR 1 / MID 2 / FAR 3 proposed, kept at 0) and rumor rows with effect numbers |
| P-017 | Quest reward payout | OP-DEV-08 | Claim may refill a slot (D-023). Do not grant `RewardGold` or `RewardCandy` until this overrides D-020 |
| P-018 | Zero executable actions while `InProgress` | later | Open. D-028 and D-031: do not add a fail rule. Timeout logs once and does not pick. D-011 still ends on turns only after an action is applied |
| P-019 | `Selectable.OnDisable` IndexOutOfRange during script reload | later | Open. Seen in `Logs/Editor.log` at `Begin MonoManager ReloadAssembly`, stack `TMP_InputField.OnDisable` → `Selectable.OnDisable` (package uGUI `Selectable.cs:570`, `TMP_InputField.cs:1264`), then a second `Selectable.OnDisable` with no InputField frame. The only project `TMP_InputField` is Title `NicknameInput`. Out of the PLAY-07 loot-slot fix. Do not treat the static selectable-list damage path as confirmed, and do not change UI from this note |

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
| Turns 0 always fail vs last-turn escape | D-011, order updated by D-030: escape success is judged first. Candy ≥ min wins over alert and 0 turns. Candy 0 is empty-hand escape. Alert then beats a failed escape that also hits 0 turns |
| Repeatable=1-only fail retry vs D-013 | D-013: fail-retry for Repeatable 0 and 1; Repeatable=0 blocks only after success. EventID uses the same meaning (D-015) |
| ACT_013 Notes skip-roll vs GDD chance formula | D-014: escape and ACT_013 use the same success roll; meeting conditions does not auto-win; Notes updated; no new escape rows |
| Event success timing / PhaseFilter snapshot | D-015: option chance roll; pre-action phase; inclusive alert/turn windows; pending blocks actions |
| Comp crop / local retouch vs independent sprites | D-017: Scenario regen only. Historical extracts stay on disk, not a template |
| Always-split layers vs cost | D-017: independent assets preferred; composite allowed when reuse/anim value is low — report why |
| Title auto-lobby vs nickname login | D-018: Title waits for valid nickname login; D-012 auto-enter is retired. P-010 PlayerPrefs is still M4 |
| Runtime UI build vs Editor layout | D-019: static UI is scene/prefab authored; runtime instantiate is for repeating/data-driven widgets only |
| Lobby placeholder HUD vs live currencies | D-020: nickname / energy / gold / candy HUD are live saved values. Gold stays 0 until a grant rule exists. Lobby candy is cumulative `finalReward` (grant in M1-9). Gacha and companions stay “준비 중” |
| REF-UI-003 button order vs lobby CTA | D-020: left 크루, center 작전 시작 (yellow, slight emphasis), right 강화. Not the reference’s left-start layout |

## Conflict check (2026-11-14)

| Topic | Resolution |
|---|---|
| P-009 three shops vs a 20-shop roster | D-021: three shops stay the prototype ids and numbers. Shipping target is 20. Names for the 17, and renames, wait on P-016 |
| D-008 file set vs new tables | D-026 specifies columns. CSV files are unchanged until a later approved edit. On-disk CSV remains the loader input |
| D-005 nine tables vs rumors and quests | D-026 adds three tables. `EffectTable` stays off. Achievements stay titles |
| D-015 events vs shop rumors | D-022: rumors are a list-time snapshot. In-run event draw and order stay |
| D-019 runtime Shop Select vs 2×2 prep | Prefab screen becomes the player path. Runtime builder is not deleted |
| D-020 spend vs departure energy and paid reroll | D-024 / D-025: no energy debit, no gold debit, no paid reroll in this slice |
| D-020 lobby buttons vs quest board | D-023: board is a lobby screen. The opening control is P-015 |
| Unlimited reroll vs a cap | D-024: separate free and max settings. Paid room is inside the max later. This slice stops at the free count |
| Crash vs abandon | D-025: confirm then fail refreshes the list. A killed process does not |
| Run HUD top bar vs phase, fraction, narrative, timer | D-028: those four are on the 1080×1920 run HUD. Choice cards stay a vertical list. `경계도` and candy `3/15` stay off the spec. Timeout fail-on-empty is P-018 |
| D-028 event options skip the timer vs a shared limit | D-031: event options use `ACTION_SELECT_TIME_SEC` too. The original D-028 sentence stays in that record |
