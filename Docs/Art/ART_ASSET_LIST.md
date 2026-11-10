# Candy Thief — Art Asset List

- Version: 0.2 (2026-11-09)
- Status legend:
  - `Approved Reference` = 아트 스타일 레퍼런스로 승인됨. **게임에 적용됨이 아님. `Integrated`로 올리지 말 것.**
  - `Reference approved` = 2026-11-05 이전 방향 메모 (파일 미확인). 신규 생성은 `Approved Reference`의 `REF-*`를 우선
  - `Planned` = 미제작
  - `In Review` / `In review` = 생성됨, 검수 전
  - `Approved` = 프로덕션 에셋으로 승인됨. **Unity 미적용. `Integrated`가 아님**
  - `Approved display derivative` = 마스터에서 만든 표시용 축소본. 마스터가 아님
  - `Preserved` = 이전 버전 파일 유지. 마스터가 아님
  - `Not adopted` = 후보였으나 채택하지 않음. 파일 유지
  - `Final` = 프로덕션 아트 확정
  - `Integrated` = Unity에 연결되어 사용 중
- ID format: 게임 에셋 `ART-<AREA>-###`. 공식 스타일 레퍼런스 `REF-<KIND>-###`.
- Scenario 운영: `Docs/Art/SCENARIO_PIPELINE.md`.

| ID | Area | Asset | Status | Suggested output | Notes |
|---|---|---|---|---|---|
| ART-REF-001 | Reference | Title A/B master image | Reference approved | PNG | Day chase + night rooftop; store in Project files |
| ART-REF-002 | Reference | Revised lobby A/B/C master image | Reference approved | PNG | Spacious shabby room, small player, empty ally slots |
| ART-TTL-001 | Title | Candy Thief logo | Planned | Transparent PNG | Consistent wordmark |
| ART-TTL-002 | Title | Daytime title background | Planned | PNG | No UI/text/character baked in |
| ART-TTL-003 | Title | Nighttime title background | Planned | PNG | Alternative to daytime |
| ART-TTL-004 | Title | Running thief + candy sack illustration | Planned | Transparent PNG | May need day/night variants |
| ART-LOB-001 | Lobby | Shabby hideout background | Integrated | PNG 1080×1920 | Scenario Sunburst. `Title.unity` lobby plate |
| ART-LOB-002 | Lobby | Main thief idle sprite | Integrated | Transparent PNG 1280×1664 master | Master v007. `Title.unity` displays 230×299 derivative. v008 not adopted |
| ART-LOB-003 | Lobby | Companion slot empty state | Planned | Transparent PNG | Two `+` slots |
| ART-LOB-004 | Lobby | Companion assigned state | Planned | UI frame/PNG | Only display assigned companion |
| ART-LOB-005 | Lobby | `도둑질 시작!` button background | Planned | Transparent PNG / 9-slice | Text in Unity |
| ART-UI-001 | Common UI | Profile frame | Planned | Transparent PNG / 9-slice | Nickname/avatar |
| ART-UI-002 | Common UI | Energy icon and bar | Planned | Transparent PNG | Value in Unity |
| ART-UI-003 | Common UI | Gold icon | Planned | Transparent PNG | Value in Unity |
| ART-UI-004 | Common UI | Gold candy icon | Planned | Transparent PNG | Value in Unity |
| ART-UI-005 | Common UI | Settings icon | Planned | Transparent PNG | Restrained neutral palette |
| ART-UI-006 | Common UI | Bottom navigation bar | Planned | PNG / 9-slice | Charcoal/gray |
| ART-UI-007 | Common UI | Character menu icon | Planned | Transparent PNG | Functional, consistent |
| ART-UI-008 | Common UI | Gacha menu icon | Planned | Transparent PNG | Companions + items |
| ART-UI-009 | Common UI | Collection menu icon | Planned | Transparent PNG | Functional, consistent |
| ART-UI-010 | Common UI | Shop menu icon | Planned | Transparent PNG | Functional, consistent |
| ART-UI-011 | Common UI | Mission menu icon | Planned | Transparent PNG | Functional, consistent |
| ART-CHR-001 | Character | Player turnaround sheet | Planned | PNG + individual transparent PNGs | Front/side/back, proportions fixed |
| ART-CHR-002 | Character | Player expression sheet | Planned | PNG | Simple expressions |
| ART-CHR-003 | Character | Player idle animation parts/frames | Planned | Transparent PNGs | Pivot and frame consistency check |
| ART-CMP-001 | Companion | Companion A design | Planned | PNG | Design TBD |
| ART-CMP-002 | Companion | Companion B design | Planned | PNG | Design TBD |
| ART-SCR-001 | Screen | Stage selection concept | Planned | 9:16 PNG | Entered from lobby CTA |
| ART-SCR-002 | Screen | Character management concept | Planned | 9:16 PNG | UI flow TBD |
| ART-SCR-003 | Screen | Gacha concept | Planned | 9:16 PNG | UI flow TBD |
| ART-SCR-004 | Screen | Collection concept | Planned | 9:16 PNG | UI flow TBD |
| ART-SCR-005 | Screen | Shop concept | Planned | 9:16 PNG | UI flow TBD |
| ART-SCR-006 | Screen | Missions concept | Planned | 9:16 PNG | UI flow TBD |
| ART-FB-001 | Run feedback | Success jump reaction | Integrated | Transparent PNG | `CT_Feedback_Success_Jump_v001` |
| ART-FB-002 | Run feedback | Success cheer reaction | Integrated | Transparent PNG | `CT_Feedback_Success_Cheer_v001` |
| ART-FB-003 | Run feedback | Fail flinch reaction | Integrated | Transparent PNG | `CT_Feedback_Fail_Flinch_v001` |
| ART-FB-004 | Run feedback | Fail freeze reaction | Integrated | Transparent PNG | `CT_Feedback_Fail_Freeze_v001` |
| ART-FB-005 | Run feedback | Sparkle particle | Integrated | Transparent PNG | `CT_Feedback_Sparkle_v001` |
| ART-FB-006 | Run feedback | Candy particle | Integrated | Transparent PNG | `CT_Feedback_Candy_v001` |
| ART-FB-007 | Run feedback | Alert bang | Integrated | Transparent PNG | `CT_Feedback_Bang_v001` |
| ART-FB-008 | Run feedback | Sweat drop | Integrated | Transparent PNG | `CT_Feedback_Sweat_v001` |
| ART-RCT-CAPTURE-001 | Run feedback | AlertLimit capture reaction | Integrated | Transparent PNG | Scenario AI. `CT_Feedback_Captured_v001`. AlertLimit only; fallback to fail chibi if missing |

## Official style references (Approved Reference)

2026-11-09 승인. 경로 `Docs/Art/References/<AssetID>.png`. Unity 미적용.

확인된 Scenario Asset ID:
- `REF-UI-003` → `asset_8ZyE7Tj2m5BuUbJ4p1eXxg53` (CandyThief, 2026-11-09 업로드). 로컬 경로를 모델 참조로 쓰지 말 것.

| AssetID | 에셋명 | 분류 | 파일 경로 | 승인 상태 | 사용 목적 | 주요 참조 대상 |
|---|---|---|---|---|---|---|
| REF-ACT-001 | 경비를 피해 탈출 | Character Reaction | `Docs/Art/References/REF-ACT-001.png` | Approved Reference | 경비를 피해 탈출 | 도둑 실루엣·코믹 모션. 보조 참조 |
| REF-ACT-002 | 경비에게 발각 | Character Reaction | `Docs/Art/References/REF-ACT-002.png` | Approved Reference | 발각 상황 | 도둑+경비 |
| REF-ACT-003 | 사탕 대량 획득 | Character Reaction | `Docs/Art/References/REF-ACT-003.png` | Approved Reference | 성공·획득 포즈 | 도둑 캐릭터 우선 참조 |
| REF-ACT-004 | 경비를 따돌림 | Character Reaction | `Docs/Art/References/REF-ACT-004.png` | Approved Reference | 따돌림·기습 성공 | 도둑 우선 참조, 경비 체형 |
| REF-ACT-005 | 경비에게 붙잡힘 | Character Reaction | `Docs/Art/References/REF-ACT-005.png` | Approved Reference | 체포·실패 연출 | 도둑+경비 |
| REF-BG-001 | 사탕 가게 뒷골목 | Background | `Docs/Art/References/REF-BG-001.png` | Approved Reference | 가게/골목 배경 스타일 | 배경 생성 |
| REF-UI-001 | 인게임 HUD | UI Reference | `Docs/Art/References/REF-UI-001.png` | Approved Reference | HUD·선택지 **룩** | 시각만. 수치/라벨/기능은 GDD |
| REF-UI-002 | 성공 팝업 | UI Reference | `Docs/Art/References/REF-UI-002.png` | Approved Reference | 결과 팝업 **룩** | 시각만. M1-8.5 흐름은 유지 |
| REF-UI-003 | 로비 | UI Reference | `Docs/Art/References/REF-UI-003.png` | Approved Reference | 로비 **룩** | 시각만. 위젯은 D-012 플레이스홀더 |

Scenario Asset ID / 생성 모델 / 프롬프트: `REF-UI-003` 업로드 ID 확인됨. `ART-LOB-001` v002와 `ART-LOB-002` display230은 `Title.unity`에 `Integrated`. 마스터 v007 1280×1664 보존. v008 미채택. 리뷰 씬 `Title_LobbyArtReview` 제거.

ID 충돌: `REF-UI-001` ≠ `ART-UI-001` (Planned 프로필 프레임). `REF-ACT-*` ≠ `ART-REF-001`/`ART-REF-002` (구 Title/Lobby 방향 메모). 기존 `ART-*` Integrated 행은 그대로 둔다.

## Scenario / AI pipeline assets

Keep the table above for the live inventory. Use this section when a new image is generated. Prefer reusing an existing ID before adding a row.

| Field | ART-LOB-001 |
|---|---|
| AssetID | ART-LOB-001 |
| AssetName | Lobby hideout background |
| Usage | Lobby room plate. UI and character composited in Unity. Applied on `Title.unity` |
| Source | Scenario AI |
| ReferenceImage | `REF-UI-003` / `asset_8ZyE7Tj2m5BuUbJ4p1eXxg53` |
| Model | `model_openai-gpt-image-2-5-sunburst` (GPT Image 2.5 Sunburst) |
| Job | `job_NxFDJcSqugUYYqTvPuew3HjR` |
| ResultAsset | `asset_Ho1fTc3F2wJo96v6MXBkwTs8` |
| Parameters | 1080×1920, quality high, background opaque, numOutputs 1 |
| CU | 13 (cuDiscount 0) |
| Prompt | Edit REF-UI-003 into a clean lobby plate: keep furniture/layout; remove thief, HUD, buttons, text; extend room into covered areas; preserve camera/perspective; not a redesign |
| SourceFile | `Docs/Art/Source/Scenario/ART-LOB-001_CT_Lobby_Background_v002_source.png` |
| UnityAssetPath | `Assets/Art/Lobby/Review/CT_Lobby_Background_v002.png` (`Title.unity`) |
| Status | Integrated |
| Version | v002 |

| Field | ART-LOB-002 |
|---|---|
| AssetID | ART-LOB-002 |
| AssetName | Lobby thief idle |
| Usage | Design extract from REF-UI-003. Preserved. Superseded as master by v007 |
| Source | v003 plus outer-shell wood remnant removal. Interior RGB unchanged |
| ReferenceImage | `REF-UI-003` / `asset_8ZyE7Tj2m5BuUbJ4p1eXxg53` |
| Model | (none) |
| CU | 0 |
| SourceFile | `Docs/Art/Source/Scenario/ART-LOB-002_CT_Lobby_Thief_Idle_v004_source.png` |
| Previous | v003 and v002 retained |
| Compare | `Docs/Art/Source/Scenario/ART-LOB-002_CT_Lobby_Thief_Idle_v003_v004_compare.png` |
| UnityAssetPath | `Assets/Art/Lobby/Review/CT_Lobby_Thief_Idle_v004.png` (preserved, not assigned) |
| Status | Preserved |
| Version | v004 |
| Notes | 100×130 RGBA extract. Not the master. `Title.unity` displays v007 230×299 derivative. |

| Field | ART-LOB-002 v005 |
|---|---|
| AssetID | ART-LOB-002 |
| AssetName | Lobby thief idle high-res candidate |
| Usage | Animation source candidate. Not adopted. Not Unity-applied |
| Source | Scenario Upscale V3 from v004 |
| ReferenceImage | v004 / `asset_5RqPkVPERGcaZPBx62gC2xvA` |
| Model | `model_upscale-v3` (cartoon, precise, 6×) |
| Job | `job_s7EEPz4tYeEHS4NTG51N79hg` |
| ResultAsset | `asset_7wYFWFAgt997PVVtRGHBJaTg` |
| CU | 10 (cuDiscount 0) |
| Parameters | style cartoon, preset precise, upscaleFactor 6, numOutputs 1 |
| SourceFile | `Docs/Art/Source/Scenario/ART-LOB-002_CT_Lobby_Thief_Idle_v005_source.png` |
| AlphaRestore | `Docs/Art/Source/Scenario/ART-LOB-002_CT_Lobby_Thief_Idle_v005_alpha.png` (local, original alpha scaled, RGB unchanged) |
| Compare | `Docs/Art/Source/Scenario/ART-LOB-002_CT_Lobby_Thief_Idle_v004_v005_compare.png` |
| UnityAssetPath | (none) |
| Status | In Review |
| Version | v005 |
| Notes | Output 1536×2016 RGB, not 600×780. No native alpha. Painterly drift vs v004. |

| Field | ART-LOB-002 v007 |
|---|---|
| AssetID | ART-LOB-002 |
| AssetName | Lobby thief idle master |
| Usage | Confirmed master original. Not the lobby Image sprite. Sprite animation not started |
| Source | GPT Image 2.5 Sunburst img2img from v004 |
| ReferenceImage | v004 / `asset_5RqPkVPERGcaZPBx62gC2xvA` |
| Model | `model_openai-gpt-image-2-5-sunburst` |
| Job | `job_4HdN1Lnb9UBCh8m8kvMBWXF1` |
| ResultAsset | `asset_YMpa5efat3QH5mYtZr3tjw4r` |
| CU | 13 (cuDiscount 0) |
| Parameters | width 1280, height 1664, quality high, background transparent, numOutputs 1 |
| SourceFile | `Docs/Art/Source/Scenario/ART-LOB-002_CT_Lobby_Thief_Idle_v007_source.png` |
| Compare | `Docs/Art/Source/Scenario/ART-LOB-002_CT_Lobby_Thief_Idle_v004_v007_compare.png` |
| UnityAssetPath | `Assets/Art/Lobby/Review/CT_Lobby_Thief_Idle_v007.png` (master copy, not assigned to Image) |
| Status | Approved |
| Version | v007 |
| Notes | Master. 1280×1664 RGBA. Native transparent BG. `Title.unity` Image uses the 230×299 display derivative. v008 not adopted. |

| Field | ART-LOB-002 v007 display230 |
|---|---|
| AssetID | ART-LOB-002 |
| AssetName | Lobby thief idle Unity display derivative |
| Usage | `Title.unity` lobby Image sprite. LANCZOS pre-downscale of v007. Display size 100×130 × (lobbyWidth/940) × Character Scale |
| Source | Local LANCZOS resize of v007 master. Not a new generation |
| SourceFile | `Docs/Art/Source/Scenario/ART-LOB-002_CT_Lobby_Thief_Idle_v007_display230.png` |
| Compare | `Docs/Art/Source/Scenario/ART-LOB-002_v007_unity_A_vs_B_downscale230_compare.png` |
| UnityAssetPath | `Assets/Art/Lobby/Review/CT_Lobby_Thief_Idle_v007_display230.png` (`Title.unity`) |
| Import | Sprite Single FullRect, Bilinear, uncompressed RGBA32, mip off, max 512 |
| Status | Integrated |
| Version | v007 display230 |
| Notes | 230×299 RGBA. Master 1280×1664 unchanged. Comparison test file kept. Character Scale 1.5 on Title. |

| Field | ART-LOB-002 v008 |
|---|---|
| AssetID | ART-LOB-002 |
| AssetName | Lobby thief idle local edit candidate |
| Usage | v007 eyes/blush/beanie correction. Not adopted. Not Unity-applied |
| Source | GPT Image 2.5 Sunburst inpaint of v007, then local composite of keep-mask + mouth restore from v007 |
| ReferenceImage | v007 / `asset_YMpa5efat3QH5mYtZr3tjw4r` plus v004 / `asset_5RqPkVPERGcaZPBx62gC2xvA` |
| Model | `model_openai-gpt-image-2-5-sunburst` |
| Job | `job_v3GxJaUNfHLqEn6EbQdtdhjR` |
| ResultAsset | `asset_gS4wdGkWGPXikxWxGmrpMDvK` |
| MaskAsset | `asset_McJ6SNNLxKMTT64YFmbFLJRk` |
| CU | 14 (cuDiscount 0) |
| Parameters | width 1280, height 1664, quality high, background transparent, numOutputs 1, mask inpaint |
| SourceFile | `Docs/Art/Source/Scenario/ART-LOB-002_CT_Lobby_Thief_Idle_v008_source.png` |
| ScenarioRaw | `Docs/Art/Source/Scenario/ART-LOB-002_CT_Lobby_Thief_Idle_v008_scenario.png` |
| Compare | `Docs/Art/Source/Scenario/ART-LOB-002_CT_Lobby_Thief_Idle_v004_v007_v008_compare.png` |
| UnityAssetPath | (none) |
| Status | Not adopted |
| Version | v008 |
| Notes | Not adopted. Knit beanie stronger than requested. Right blush present; left blush reduced after mouth restore. Source preserved. No further correction. |

| Field | ART-RCT-CAPTURE-001 |
|---|---|
| AssetID | ART-RCT-CAPTURE-001 |
| AssetName | AlertLimit capture / 들켰다 |
| Usage | `PRES_RUN_FAIL` when `RunState.FailReason == AlertLimit` |
| Source | Scenario AI |
| ReferenceImage | 공식 캐릭터 참조는 `REF-ACT-003`/`REF-ACT-005`. 후보 시트 `Docs/Art/References/Scenario/CT_StyleSheet_Candidate_v001.png`는 아직 파일 없음 |
| Prompt | Angry police officer in blue uniform holds a crying Candy Thief (red beanie, black mask, striped shirt) by the shirt. Thick rounded outlines, SD chibi, transparent background. |
| SourceFile | `Docs/Art/Source/Scenario/CT_Feedback_Captured_v001_source.png` |
| UnityAssetPath | `Assets/Art/V02/Feedback/CT_Feedback_Captured_v001.png` and `Assets/Resources/V02/Feedback/CT_Feedback_Captured_v001.png` |
| Status | Integrated |
| Version | v001 |

## Asset acceptance checklist
- [ ] Matches master references and style guide
- [ ] Correct portrait size / transparent background as required
- [ ] No unwanted baked-in text or UI
- [ ] Player silhouette and eye/mask/beanie consistent
- [ ] Lobby retains open space and shows no unassigned companions
- [ ] No wealth props or motivational poster
- [ ] Readable at actual mobile size
- [ ] Imported/tested in Unity, including pivot/9-slice if relevant

## Workflow / version notes
| Date | Item | Decision |
|---|---|---|
| 2026-11-05 | Title | A and B art directions liked; splash auto-transitions after minimum 1 s |
| 2026-11-05 | Lobby | Revised A/B/C direction liked; larger empty room, smaller player, neutral icons, companion slots empty by default |
| 2026-11-09 | Run feedback | M1-8.5 chibi thief reactions (red beanie, mask, striped shirt) + FX icons; Cursor GenerateImage then chroma-key. P0 in-run result, not final production |
| 2026-11-09 | Capture | Scenario AI `ART-RCT-CAPTURE-001` applied to AlertLimit only. Source kept under `Docs/Art/Source/Scenario/`. |
| 2026-11-09 | Official style | Nine Scenario references `REF-ACT-001`–`005`, `REF-BG-001`, `REF-UI-001`–`003` approved as style only (`Approved Reference`). Not Unity-integrated. Pipeline: `SCENARIO_PIPELINE.md`. |
| 2026-11-09 | ART-LOB-001 | Sunburst lobby plate from `REF-UI-003`. 13 CU. User approved. Status `Approved`. Not regenerated. Unity not applied. |
| 2026-11-09 | ART-LOB-002 | Idle thief extracted from `REF-UI-003` (no AI). Shadow removed. Status `In Review`. Unity not applied. |
| 2026-11-09 | ART-LOB-002 v003 | New 106×130 RGBA from `REF-UI-003` with 1:1 interior RGB. v002 kept. No Scenario. In Review. |
| 2026-11-09 | ART-LOB-002 v004 | Removed 99 wood-fringe pixels from v003 shell. Interior RGB unchanged. v003 kept. In Review. |
| 2026-11-10 | ART-LOB-002 v007 | GPT Image 2.5 Sunburst from v004. 1280×1664 transparent, 13 CU, 1 plate. Confirmed master (`Approved`). Review scene `Title_LobbyArtReview.unity`. Production `Title.unity` unchanged. |
| 2026-11-10 | ART-LOB-002 v008 | Sunburst inpaint of v007 eyes/blush/beanie. 14 CU, 1 plate. Not adopted. Source preserved. No further gen/correction. |
| 2026-11-10 | ART-LOB-002 display230 | Adopted pre-downscale B for Unity lobby display. 230×299 LANCZOS from v007. Review `thiefIdle` retargeted. Master 1280×1664 preserved. |
| 2026-11-10 | Lobby art on Title | `Title_LobbyArtReview` merged into `Title.unity`. ART-LOB-001 v002 + display230 Integrated. Review scene deleted. Play Mode Start Scene = Title. |

## Suggested file names
`CT_<Area>_<Asset>_<Variant>_v###.png`, e.g. `CT_Lobby_Background_A_v001.png`.

## Unity project placement
- `Docs/Art/ART_STYLE_GUIDE.md`
- `Docs/Art/ART_ASSET_LIST.md`
- `Docs/Art/SCENARIO_PIPELINE.md`
- `Docs/Art/References/` (official `REF-*.png`)
- `Docs/Art/Source/Scenario/` (untouched generator originals)
- `Assets/Art/` (validated game-ready assets only)
- `Assets/Resources/V02/` (runtime copies of in-game sprites)

생성 절차는 `SCENARIO_PIPELINE.md`. 생성과 Unity 적용은 기본으로 분리한다.
