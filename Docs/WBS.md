# WBS — Candy Thief v0.2

Rules: `Docs/GDD.md`. Sequence: `Docs/PLAN.md`. Constraints: `Docs/TECH_SPEC.md`.

Do not start a work package that contradicts GDD. CSV numeric values are not tasks; importing tables is.

## M1 — Core feel

| ID | Work | Done when |
|---|---|---|
| M1-1 | CSV contract | Closed: files in `Assets/Resources/Table`; see D-008. P-005/D-010, P-012/D-009, P-013/D-011 closed |
| M1-2 | CSV loader (UTF-8 BOM, header, lookup by ID) | Nine gameplay CSVs load; skip `README.csv`; missing/duplicate ID logs a clear error |
| M1-3 | Run state: turns, alert, candy, phase, last action result, run status | Done: `RunState` starts from ShopID; integer candy; D-011 end checks; ended runs reject mutation. No chance roll, no `finalReward` |
| M1-UI-1 | Title / Lobby / Shop Select hub | Done: portrait 1080×1920 Title waits for nickname login (D-018) then Lobby; Title UI is scene-authored (D-019); Shop Select uses enabled ShopTable rows; selected ShopID starts `RunState`; no run gameplay UI |
| M1-4 | Action resolve: chance, turn cost, success/fail effects | Done: `ActionResolver` rolls `SuccessRate + Shop.SuccessRateBonusPp` (clamped); applies CSV cost/deltas via `RunState`; D-011 end checks. Event roll is M1-7. StageTable.xlsx is not loaded |
| M1-5 | Required-action retry | After fail, the same required action can be chosen again until win/fail |
| M1-6 | End checks: turns ≤ 0 or alert ≥ 100 fail; win not required in M1 if Escape is stubbed | Fail paths fire before any event UI |
| M1-7 | Event roll after `EventEligible` actions; Enabled events; options; no chain | Done: `EventResolver` draws after `EventCheckDue`; EVT_001–EVT_005 can appear from data; pending options; no chain. Editor 66/66 (EventResolver 23 + RunState 11 + ActionResolver 32) |
| M1-8 | Portrait temp UI 1080×1920: top stats, center text, bottom choices | Done: `RunHud` prefab under Title SafeArea; shared ChoiceList for actions/events; chance, turn cost, success/fail outcomes from CSV. Editor 74/74 + Play Mode HUD smoke |
| M1-8.5 | Result feedback + presentation polish | Done: optional `ResultFeedbackTable`; snapshot deltas; lock → anticipate → reveal → hold → `[계속]`; event UI after Continue; success/fail chibi reactions; Hangul wrap/scroll. Editor 74 + ResultFeedback 11. Result screen is M1-9 |
| M1-9 | Editor click stands in for touch | M1 loop completable in Play Mode on PC. Result screen + title/lobby return hook from `EndedRunContinued` |

**M1 exit:** PLAN M1 “Done when” is met. Hub Title/Lobby/Shop Select is M1-UI-1. No full four-phase loop, no final art.

## M2 — Full loop

| ID | Work | Done when |
|---|---|---|
| M2-1 | Title → shop select → run → result → retry/title | Each arrow is a real screen or confirmed stub |
| M2-2 | ShopTable applied at run start | SHOP_001–SHOP_003 change StartTurn/StartAlert/SuccessRateBonusPp/CandyMultiplier/EventRateBonusPp |
| M2-3 | Phases MOVE → INFILTRATE → LOOT → ESCAPE | MOVE/INFILTRATE/ESCAPE: one REQUIRED success. LOOT: candy ≥ 1 then optional ACT_013 to ESCAPE |
| M2-4 | Win: candy ≥ 1 and Escape success | Exit without candy cannot win |
| M2-5 | Result screen: win/fail, candy, TEMP score | The four operation results, settled candy, retry, and lobby are PLAY-07 (Done, placeholder art). TEMP score stays M3-2 and is not on that popup |

**M2 exit:** One win and one fail from title to result, all numbers from CSV.

## M3 — Randomness and balance

| ID | Work | Done when |
|---|---|---|
| M3-1 | Expand EventTable / ActionTable | New rows play without code changes |
| M3-2 | Replace TEMP `risk_bonus = 1.0` with owner-approved formula | GDD score section no longer TEMP |
| M3-3 | Repeat-play pass | 3 consecutive editor runs; note broken weights/IDs |

**M3 exit:** Score formula approved; extra actions/events data-only.

## M4 — Meta and presentation

| ID | Work | Done when |
|---|---|---|
| M4-1 | AchievementTable + titles | Unlocks persist after editor restart |
| M4-2 | Best score + cumulative records | Survive restart (`TEMP` PlayerPrefs) |
| M4-3 | Cosmetic currency hook | Currency cannot buy run power |
| M4-4 | Event illustrations via AssetTable | Missing asset falls back to placeholder |
| M4-5 | UI motion + sound | No P0 overlap/unreadable text; SFX optional per `AssetTable` |
| M4-6 | Formal run-HUD UI. Replace the PLAY-01 forecast tooltip with a center information modal. Do not move that tooltip during PLAY-01 | Candy and alert info buttons open one center modal. Outside click or a close button dismisses it, and that input does not execute an action. The action-select timer keeps running while the modal is open. At 0 the modal closes and the existing timeout pick runs. The choice-card layout and the illustration area stay |

**M4 exit:** PLAN M4 “Done when”. M4-6 is the formal UI pass for the run-HUD forecast. The current tooltip stays until that pass.

## M5 — QA and APK

| ID | Work | Done when |
|---|---|---|
| M5-1 | PlayerSettings: portrait, 1080×1920, Android | Orientation locked; editor game view portrait |
| M5-2 | Android APK build | APK installs on a device or emulator |
| M5-3 | Device playthrough | One win, one fail, Safe Area readable |
| M5-4 | P0 bug pass | No crash, no soft-lock, no table-missing-ID in the happy path |
| M5-5 | Distribute APK | Owner has a shareable file |

**M5 exit:** PLAN M5 “Done when”. Google Play store listing is P2, not M5.

## Operation prep — OP-DEV (D-021–D-026)

Does not reopen closed M1 tasks. OP-DEV-01 through OP-DEV-07 and OP-DEV-10 are done. OP-DEV-08, OP-DEV-09, and OP-DEV-11 stay in this table and are deferred. PLAY-01 through PLAY-03 are ahead of them (D-028).

| ID | Work | Depends on | Done when |
|---|---|---|---|
| OP-DEV-01 | Read the current tables, resolvers, and profile save | — | Reported. No code or CSV edits |
| OP-DEV-02 | Record D-021–D-026 and the column contract | 01 | Decisions and GDD/WBS/TECH_SPEC match. Files on disk still have 3 shops |
| OP-DEV-03 | Weighted draw of 4 shops, no duplicates, relax then fail if the pool is short | 02, and a later CSV-edit approval | Done: `ShopTable` new columns + loader, `ShopListDrawer` (reuses `IIntRoll`), fixture tests. Existing resolve numbers unchanged. A list of 4 fails while fewer than 4 drawable shops exist. P-016 closed (D-027): 20 shops registered and a 4-card draw from official data is tested. Editor batch run: 9/9 groups, ShopListDrawer 12, ShopRumor 17 |
| OP-DEV-04 | Rumor count 40/45/15, conflict groups, snapshot deltas | 03 | Done: rumor tables (headers only), loader, `ShopRumorDrawer`, `RunState.Shop` snapshot, fixture tests. Zero rumors match today’s chance math. `EffectTable` stays off. In-run events unchanged. No official rumor rows (numbers unapproved). Editor batch run: 9/9 groups, ShopRumor 17 |
| OP-DEV-05 | Free shop reroll, effective max, save list and counters, refresh after a settled result | 03, 04 | Done: `ShopListManager` (create, free reroll, start run, settle once), profile `shopList` / `activeRunId` / `settledRunId` on the same schema id, `RunState.RunId`, `GameConfig` `SHOP_LIST_SIZE` / `SHOP_REROLL_FREE_PER_LIST`. Reopening keeps the list. A failed reroll spends nothing. Counters reset only on a new list. List refreshes when the ended-run feedback is confirmed (once per run id). The reroll control is OP-DEV-06. Editor batch run: 10/10 groups, ShopListManager 14 |
| OP-DEV-06 | Prefab 2×2 prep screen. Show free use and disable at the cap | 05 | Done: `OperationPrep` holds four `ShopCard` instances. Refresh reads `ShopListManager` and shows `새로고침 n/1`, disabled at 0. Select, start, and lobby return stay on the existing list path. Missing shop art uses the placeholder. Risk pips stay hidden. Editor batch: 11/11 groups. Play Mode review confirmed the refresh label, four cards, select, start, and lobby return |
| OP-DEV-07 | Bind `BackgroundImageKey`. Do not generate images | 02, 06 | Done: prep cards and the run HUD share `BackgroundImageKey` via `AssetTable` and `Resources.Load` (`V02/Shops`). A loaded event sprite still replaces the illustration. Blank or unknown keys fail validation. Missing files keep the placeholder. No new images or CSV path edits. Editor batch: 11/11 groups. HudPresentation 8. Play Mode review confirmed four card placeholders, the run HUD placeholder, and the existing stats and choices |
| OP-DEV-08 | Six slots, active cap 3, claim refills the slot | 02 | Deferred. Progress increments only on a settled win. No gold or candy grant until P-017. Entry control is P-015 |
| OP-DEV-09 | KST 05:00 refresh, 2 free quest rerolls, effective max, protect accepted and claimable | 08 | Deferred. One refresh per operation day. Daily refresh and claim refill do not spend a reroll. Shortage relaxes, then fails |
| OP-DEV-10 | Confirm on the run HUD lobby action, then settle as fail and refresh the list | 05 | Done: in-progress lobby opens a confirm. Cancel keeps the run. Confirm marks Failed and reuses `SettleRun` once per run id. Save or draw failure stays on the run. No energy or gold debit. Editor batch: 12/12 groups. Abandon self-test 7/7. Play Mode review confirmed the popup, cancel keeps state, fail settlement, a new shop list, and free refresh reset |
| OP-DEV-11 | Regression plus draw, shortage, cap, split counters, slots, operation day, double settle | 03–10 | Deferred. Existing action and event self-tests still pass |

**OP-DEV exit:** A player can open a saved 4-card list, reroll inside the free cap, start a run, and see rumors affect the existing formulas. Quest claim does not pay currency until P-017. Departure energy is not spent. PLAY-03 is reviewed Done. Quest slots (OP-DEV-08, OP-DEV-09) and the full OP-DEV-11 pass stay deferred until a separate approval.

## In-run play — PLAY (D-028)

New IDs. Do not reuse OP-DEV or M1–M5 numbers. OP-DEV-08, OP-DEV-09, and OP-DEV-11 stay deferred above.

| ID | Work | Depends on | Done when |
|---|---|---|---|
| PLAY-01 | Run HUD layout on 1080×1920: phase name, `Alert/MAX_ALERT`, situation line, timer slot. Keep turns, candy, vertical choice list, lobby button, and event-image priority | D-028 | Done 2026-11-15, functional close. Owner review: the remaining Unity self-tests and Play Mode passed. Candy and alert info stay on the choice card. The shared forecast tooltip stays where PLAY-01 left it; it overlaps the future official illustration, so this row does not move it. The center information modal is M4-6. Card spacing, text readability, and placeholder art stay on that later UI pass. `Selectable.OnDisable` remains P-019 |
| PLAY-02 | `ACTION_SELECT_TIME_SEC` countdown on action select. Pause while a confirm is open. Stop on a confirmed action, and start at 30 again on the next action select. Result feedback and an ended run stay hidden | PLAY-01 slot | Done 2026-11-15. Owner review: ActionSelectTimer 8/8, full self-tests 16/16, Play Mode countdown, pause, and reset. D-031 later adds the event countdown and the timeout pick |
| PLAY-03 | Timeout picks one `CanExecute` action or event option with equal probability and reuses the existing resolve. One claim per round. Event options use the same 30 seconds (D-031). Empty candidates log once and do not fail | PLAY-02. P-018 stays open | Done 2026-11-15. Owner review: TimeoutAutoSelect 14/14, full self-tests 17/17, Play Mode auto-pick for actions and events, confirm pause, tooltip expiry, and one claim per round. P-018 stays open |
| PLAY-05 | Normal rounds draw 2 distinct action cards. `ACT_014`–`ACT_061` and their feedback rows are in the tables. `ACT_013` stays out of that draw | Action data | Done 2026-11-15. Owner review: ActionHand 14/14, then the full suite that included it. Rewards still use `SuccessCandy`. `SuccessCandyMin` / `SuccessCandyMax` are not a random payout |
| PLAY-06 | LOOT `탈출 준비하기` enters ESCAPE with no turn cost and no roll. Candy 0 confirms. MOVE and INFILTRATE keep the notice | D-029 | Done 2026-11-15. Owner review: LOOT → ESCAPE, empty-hand confirm, MOVE/INFILTRATE notice, repeated LOOT stays LOOT. EscapePrep 8. Suite at that review was 14/14 groups |
| PLAY-07 | Separate the action result from the operation result. Four endings, candy grant only on operation success, empty loot row | D-030 | Done 2026-11-15. Owner review: RunOutcome 8/8, full self-tests 15/15, four result popups played, retry and lobby return, layout approved. Placeholder character art stays. No loot is granted |

**PLAY exit:** PLAY-01, PLAY-02, PLAY-03, PLAY-05, PLAY-06, and PLAY-07 are Done. PLAY-01’s functional close passed on 2026-11-15. Its forecast tooltip is not moved again; the center information modal is M4-6. P-018 stays open. A live run can already finish through the four PLAY-07 results.

## Explicitly not in this WBS

- Deleting `SampleScene`, `PlayerController`, or Ninja Adventure assets
- New Unity project or unapproved packages
- v0.1 guard FOV / key candy / WebGL deploy
- Inventing CSV numbers in C#
- Inventing `EnergyCost` or rumor effect numbers (P-016 remainder; shop names are D-027)
- Energy spend, paid reroll, BM purchase, quest currency payout (D-020, P-014, P-017)
- Item or collectible systems, tilemaps, patrol AI, or a new resolver order
