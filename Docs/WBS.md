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
| M2-5 | Result screen: win/fail, candy, TEMP score | Retry resets run; title returns to start |

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

**M4 exit:** PLAN M4 “Done when”.

## M5 — QA and APK

| ID | Work | Done when |
|---|---|---|
| M5-1 | PlayerSettings: portrait, 1080×1920, Android | Orientation locked; editor game view portrait |
| M5-2 | Android APK build | APK installs on a device or emulator |
| M5-3 | Device playthrough | One win, one fail, Safe Area readable |
| M5-4 | P0 bug pass | No crash, no soft-lock, no table-missing-ID in the happy path |
| M5-5 | Distribute APK | Owner has a shareable file |

**M5 exit:** PLAN M5 “Done when”. Google Play store listing is P2, not M5.

## Explicitly not in this WBS

- Deleting `SampleScene`, `PlayerController`, or Ninja Adventure assets
- New Unity project or unapproved packages
- v0.1 guard FOV / key candy / WebGL deploy
- Inventing CSV numbers in C#
