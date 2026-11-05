# Development plan — Candy Thief v0.2

Gameplay rules: `Docs/GDD.md`. Task breakdown: `Docs/WBS.md`. Tech constraints: `Docs/TECH_SPEC.md`.

## Strategy

Keep the existing Unity 6 project, packages, Cursor / Unity MCP / GitHub setup, scenes, scripts, and assets. Build the v0.2 run loop beside the v0.1 prototype. Do not delete the old scene.

Ship a short Android portrait APK. Tune numbers in CSV, not C#.

## Milestones

| ID | Scope | Done when |
|---|---|---|
| M1 | Core feel: CSV loader from `Resources/Table`, run state, required/optional actions, retry, Enabled events, portrait temp UI | Editor play: fail/retry/succeed an action, see an event, fail by turns or alert, using table values only |
| M2 | Full loop: shop select, Move → Infiltrate → Loot → Escape, phase change, result, retry/title | One complete win and one fail path from title to result without code-side balance constants |
| M3 | Randomness and balance: more events/actions, score = candy × risk_bonus, repeat-play tests | 3 consecutive editor runs with distinct event/action mixes; score formula documented in GDD (no longer TEMP 1.0) |
| M4 | Meta and presentation: titles, local records, event illustrations, UI motion, sound | Achievements/titles persist; best score/cumulative records survive restart; placeholder art replaced where `AssetTable` points |
| M5 | QA and ship: Android build, device test, stabilize, APK | Portrait 1080×1920 APK installs, completes a run on device, no P0 blockers |

## Scope control

| Priority | Content |
|---|---|
| P0 | M1–M2 loop, CSV-driven actions/events, portrait UI, win/fail rules |
| P1 | M3 balance, M4 records/titles, illustration/SFX |
| P2 | Google Play store page, extra shops/events, cosmetics spend |

At the end of each milestone, defer unfinished P1/P2 rather than slipping P0.

## Superseded plan (v0.1)

The six-week WebGL stealth schedule is retired:

| Week | Was | Now |
|---|---|---|
| 3 | Movement/collision/title HUD | Covered by existing prototype; not the v0.2 loop |
| 4 | Guard FOV | Not in v0.2 |
| 5 | Key/gold/exit | Replaced by candy + escape action |
| 6 | Daytime shop map | Replaced by shop-select + phases |
| 7 | Tuning / optional save | M3–M4 |
| 8 | WebGL URL | Replaced by Android APK (M5) |

P0 is no longer “one guard + one map + WebGL”.
