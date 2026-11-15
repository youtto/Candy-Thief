# Current task — M4-6 run-HUD formal UI pass (in progress)

Updated 2026-11-16. The old Week 3 vertical-slice notes were stale and are in git history.
Source of truth stays `Docs/GDD.md` and `Docs/DECISIONS.md`. WBS M4-6 is `Docs/WBS.md` line 56.
DECISIONS line 427 fixes the risk label `위험도`, the text `current/MAX_ALERT`, and the fill `Alert/MAX_ALERT`.

## Done so far
- PLAY UI v001 full rebuild. Builder: `CandyThief/Assets/Scripts/V02/Editor/V02RunHudPrefabBuilder.cs`
  (menu rebuild writes `Assets/Resources/V02/UI/RunHud.prefab` and `ChoiceButton.prefab`; GUIDs preserved).
- Outline thickening and unified cartoon panels (card, phase, center, timer, HUD chips).
- HUD polish:
  - Hourglass `CT_Hud_Hourglass_v003.png` is used on the turn panel and the card cost chip.
  - Warning icon `CT_Hud_Alert_v002.png` is used on the risk panel and the card risk chip.
  - Risk panel: ring line, thick-rimmed taller gauge, 3 dim zone segments, zone dividers, number in its own row.
  - Stronger shared shadow on turn, risk and candy panels.
  - Risk logic in `V02RunHud` is unchanged.
- Scenario: GPT Image 2.6 Sunburst, 2 images, about 26 CU (dry-run estimate; actual per-job CU not separable).
  Sources are in `Docs/Art/Source/Scenario/M4-6/PlayIcons/`. Older v001/v002 assets are kept.
- Verified: full self-tests 18/18, Play Mode risk 0 to 17 with fill 0.17, tooltip open/close, 0 console errors or warnings.
  Captures: `CandyThief/Captures/play_ui_polish_1080x1920.png` and `_540x960.png`.

## Not done / open
- Gauge fill is one colour by zone (existing logic). The mock shows per-block colours. Needs a `V02RunHud` colour-logic change and approval.
- Warning marker under the gauge was declined. The flame ring around the warning icon is not added.
- Candy, escape and back icons are still the old pixel icons. Generating them (about 48 CU) needs new approval.
- Popups (3 kinds) are still the flat cream style.
- Center info modal (WBS M4-6) status was not touched in this session; check before claiming it done.
- Console noise not from this work: `Selectable.OnDisable` IndexOutOfRange on domain reload (P-019), and one PlayerLoop recursion warning after a script-driven Play.

## Rules to keep
- Static screens stay scene/Prefab-authored (D-019). Runtime binds data and clicks only.
- Plan and approval before any paid Scenario generation. Version assets; never overwrite.
- No compile, Play Mode or MCP claims that were not actually run.
