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
- 제작 파이프라인 (D-017): 독립 에셋 우선. 이미지 생성·수정은 Scenario. 시안 추출 금지. Cursor는 픽셀을 편집하지 않음. 상세 `Docs/Art/SCENARIO_PIPELINE.md`.

| ID | Area | Asset | Status | Suggested output | Notes |
|---|---|---|---|---|---|
| ART-REF-001 | Reference | Title A/B master image | Reference approved | PNG | Day chase + night rooftop; store in Project files |
| ART-REF-002 | Reference | Revised lobby A/B/C master image | Reference approved | PNG | Spacious shabby room, small player, empty ally slots |
| ART-TTL-001 | Title | Candy Thief logo | In Review | Transparent PNG 677×426 | Local extract from v002 — method retired (D-017). File kept. Title layers: regenerate via Scenario |
| ART-TTL-002 | Title | Daytime title composite | Integrated | PNG 1080×1920 | v002 plate used as a single Title image (D-017/D-018). Layers: regenerate, do not extract. v001 preserved |
| ART-TTL-003 | Title | Nighttime title background | Planned | PNG | Alternative to daytime |
| ART-TTL-004 | Title | Running thief + candy sack illustration | Not adopted | Transparent PNG 523×560 | v002 crop + 851 cutout failed. Not a final asset. Future: Scenario regen from v002+v007 refs |
| ART-TTL-005 | Title | Nickname input frame | Integrated | Transparent PNG 720×90 + 2x | Sunburst UI sheet 11 CU. Title nickname field (D-018). 2x master preserved |
| ART-TTL-006 | Title | Title start button | Integrated | Transparent PNG 1536×512 | v002 baked `도둑질 시작!`. `Title.unity` CTA. v001 blank pill preserved. Not ART-LOB-005 |
| ART-LOB-001 | Lobby | Shabby hideout background | Integrated | PNG 1080×1920 | Scenario Sunburst. `Title.unity` lobby plate |
| ART-LOB-002 | Lobby | Main thief idle sprite | Integrated | Transparent PNG 1280×1664 master | Master v007. `Title.unity` displays 230×299 derivative. v008 not adopted |
| ART-LOB-003 | Lobby | Companion slot empty state | Planned | Transparent PNG | Two `+` slots |
| ART-LOB-004 | Lobby | Companion assigned state | Planned | UI frame/PNG | Only display assigned companion |
| ART-LOB-005 | Lobby | `작전 시작` button | In Review | Transparent PNG 1024×768, baked Hangul | v003 trapezoid. v001/v002 preserved. Not ART-TTL-006. Unity not applied |
| ART-LOB-006 | Lobby | `크루` button | In Review | Transparent PNG 816×768, baked Hangul | v003 trapezoid. v001/v002 preserved. Unity not applied |
| ART-LOB-007 | Lobby | `강화` button | In Review | Transparent PNG 816×768, baked Hangul | v002 trapezoid. Enhance/Upgrade v001 preserved. Unity not applied |
| ART-LOB-008 | Lobby | Bottom button group proof | In Review | Transparent PNG 1536×512 | Design reference only. Do not crop. Not a Unity sprite |
| ART-LOB-009 | Lobby | Trapezoid silhouette guides | Reference only | Transparent PNG 816×768 / 1024×768 | Local geometry. Not a game sprite. Scenario shape refs |
| ART-UI-001 | Common UI | Profile frame + avatar hole | In Review | HUD sheet widget | D-020. On `CT_Lobby_HUD_Sheet_v001`. Nickname TMP |
| ART-UI-002 | Common UI | Energy lightning + chip | In Review | HUD sheet widget | D-020. Value TMP `n/20` |
| ART-UI-003 | Common UI | Gold icon + chip | In Review | HUD sheet widget | D-020. Value TMP |
| ART-UI-004 | Common UI | Candy icon + chip | In Review | HUD sheet widget | D-020 lobby candy, not gem. Value TMP |
| ART-UI-005 | Common UI | Settings icon | In Review | HUD sheet widget | D-020. 준비 중 |
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
- `REF-BG-001` → `asset_gbrUMnYMhg4s8RcqY34A7rUt` (CandyThief, 2026-11-10 업로드)
- `REF-ACT-004` → `asset_rNo8XCD6d6YMHY8WSjmo37RN` (CandyThief, 2026-11-10 업로드)
- `CT_Title_A_v001` 구도 참조 → `asset_LTTbUKtivejWRAxiG9XTvgiN` (CandyThief, 2026-11-10 업로드)

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

| Field | ART-TTL-002 v001 |
|---|---|
| AssetID | ART-TTL-002 |
| AssetName | Daytime title composite draft |
| Usage | Full title illustration for login layout review. Not Unity-applied. Layers not split |
| Source | Scenario AI |
| ReferenceImage | Title A `asset_LTTbUKtivejWRAxiG9XTvgiN`; v007 `asset_YMpa5efat3QH5mYtZr3tjw4r`; REF-BG-001 `asset_gbrUMnYMhg4s8RcqY34A7rUt`; REF-ACT-004 `asset_rNo8XCD6d6YMHY8WSjmo37RN` |
| Model | `model_openai-gpt-image-2-5-sunburst` (GPT Image 2.5 Sunburst) |
| Job | `job_krUqiPzbGuAYqPtV5gZacZmj` |
| ResultAsset | `asset_Ps83NbbNNRSvMXJHwJNfukhP` |
| Parameters | 1080×1920, quality high, background opaque, numOutputs 1, 4 referenceImages |
| CU | 15 (cuDiscount 0). Dry-run 1-ref=13, 4-ref=15 |
| Prompt | Title chase plate for login overlay: keep old title composition; replace baker with guard; thief matches v007; muted lobby pastels; logo CANDY THIEF only; bottom 25% continues street with no characters/UI |
| SourceFile | `Docs/Art/Source/Scenario/ART-TTL-002_CT_Title_Day_v001_source.png` |
| UnityAssetPath | (none) |
| Status | In Review |
| Version | v001 |
| Notes | Login nickname/button not baked. `CT_Title_A_v001` preserved. Layer split not started. v002 is the current review candidate. |

| Field | ART-TTL-002 v002 |
|---|---|
| AssetID | ART-TTL-002 |
| AssetName | Daytime title composite draft |
| Usage | Full title illustration for login layout review. Not Unity-applied. Layers not split |
| Source | Scenario AI. Full regen (no mask). First ref = v001 plate |
| ReferenceImage | v001 `asset_Ps83NbbNNRSvMXJHwJNfukhP`; v007 `asset_YMpa5efat3QH5mYtZr3tjw4r`; REF-BG-001 `asset_gbrUMnYMhg4s8RcqY34A7rUt`; REF-ACT-004 `asset_rNo8XCD6d6YMHY8WSjmo37RN` |
| Model | `model_openai-gpt-image-2-5-sunburst` (GPT Image 2.5 Sunburst) |
| Job | `job_J83V2RWdHR6zJ2V4RfYbZ4vm` |
| ResultAsset | `asset_rePD49mohF2ctDyorUL2Cxjz` |
| Parameters | 1080×1920, quality high, background opaque, numOutputs 1, 4 referenceImages, no mask |
| CU | 15 (cuDiscount 0). Dry-run 15, then 1 plate |
| Prompt | Match v001 composition/logo/street/guard/running thief/login band. Change thief face to v007 closed U-smile. No sweat, no scream, no shush, no cheer-open-mouth |
| SourceFile | `Docs/Art/Source/Scenario/ART-TTL-002_CT_Title_Day_v002_source.png` |
| Compare | `Docs/Art/Source/Scenario/ART-TTL-002_CT_Title_Day_v001_v002_compare.png` |
| UnityAssetPath | `Assets/Art/Title/CT_Title_Day_v002.png` |
| Status | Integrated |
| Version | v002 |
| Notes | Plate approved 2026-11-10 as the title design comp. Applied as a single Title image (D-018). Do not extract layers from this file (D-017). Failed 26 CU cutout/inpaint package is not final. `CT_Title_A_v001` preserved unused. |

| Field | ART-TTL-002 background inpaint |
|---|---|
| AssetID | ART-TTL-002 |
| AssetName | Title day background (characters/logo removed) |
| Usage | Layered title background. Not Unity-applied |
| Source | Scenario AI. Sunburst inpaint of v002 |
| ReferenceImage | v002 `asset_rePD49mohF2ctDyorUL2Cxjz`; mask `asset_HCypbscTkBJq8W2xTVH8HXQr` |
| Model | `model_openai-gpt-image-2-5-sunburst` |
| Job | `job_Xw2gyWP236scCJDKTjwxxC7W` |
| ResultAsset | `asset_1mgXYN5yoiutnvW8boFZtUVV` |
| Parameters | 1080×1920, quality high, 1 referenceImage + mask, numOutputs 1 |
| CU | 13 |
| SourceFile | `Docs/Art/Source/Scenario/CT_Title_Background_v002.png` |
| UnityAssetPath | (none) |
| Status | Not adopted |
| Version | v002 inpaint raw |
| Notes | Comp inpaint retired (D-017). File kept. Future empty street: new Scenario background from v002 as reference, not character-erase restore. |

| Field | ART-TTL-004 thief cutout |
|---|---|
| AssetID | ART-TTL-004 |
| AssetName | Title running thief + candy sack |
| Usage | Title layer. Below quality gate |
| Source | Scenario AI. 851 Labs Background Remover on v002 crop |
| Model | 851 Labs Background Remover |
| Job | `job_CBvsJzrzaynyLRc9KrEDT1eR` |
| ResultAsset | `asset_sdAJfSgegbwRW6s9rPGZE2fR` |
| CU | 1 |
| SourceFile | `Docs/Art/Source/Scenario/CT_Title_Thief_v002.png` |
| UnityAssetPath | (none) |
| Status | Not adopted |
| Version | v002 |
| Notes | Comp crop + 851 + local CC drop. Method retired (D-017). File kept. Future: transparent thief regen from v007 (look) + v002 (pose). |

| Field | ART-TTL-002 guard cutout |
|---|---|
| AssetID | ART-TTL-002 |
| AssetName | Title chasing guard |
| Usage | Title layer. Below quality gate |
| Source | Scenario AI. 851 Labs Background Remover on v002 crop |
| Model | 851 Labs Background Remover |
| Job | `job_qgpqtgYqujNicC13fviwVLpv` |
| ResultAsset | `asset_SiwdQJfBuf9uurx22C4G4djN` |
| CU | 1 |
| SourceFile | `Docs/Art/Source/Scenario/CT_Title_Guard_v002.png` |
| UnityAssetPath | (none) |
| Status | Not adopted |
| Version | v002 |
| Notes | Comp crop + 851. Missing open palm. Method retired (D-017). File kept. Future: transparent guard regen from v002 pose. |

| Field | ART-TTL-005 / ART-TTL-006 login UI sheet |
|---|---|
| AssetID | ART-TTL-005, ART-TTL-006 |
| AssetName | Nickname frame + start button (one transparent sheet) |
| Usage | Title login chrome. Split locally. Not Unity-applied |
| Source | Scenario AI. Sunburst transparent sheet |
| Model | `model_openai-gpt-image-2-5-sunburst` |
| Job | `job_tWYSVKKkN65zusWyzpaBnRyN` |
| ResultAsset | `asset_o2gS9nC8AVLg4iWd8DgPLTwT` |
| Parameters | requested 1536×448, result 1536×512, quality high, background transparent, numOutputs 1 |
| CU | 11 |
| SourceFile | sheet `Docs/Art/Source/Scenario/CT_Title_LoginUI_sheet.png`; `CT_Title_NicknameFrame_v001.png` 720×90; `CT_Title_StartButton_v001.png` 720×110; 2x masters alongside |
| UnityAssetPath | nickname `Assets/Art/Title/CT_Title_NicknameFrame_v001.png`; CTA v002 separate |
| Status | Integrated (ART-TTL-005); ART-TTL-006 v002 already Integrated |
| Version | v001 |
| Notes | No baked text on the nickname frame. Not ART-LOB-005. Independent UI sheet (not a comp extract). Title login uses the 720×90 frame (D-018). |

| Field | ART-TTL-006 v002 |
|---|---|
| AssetID | ART-TTL-006 |
| AssetName | Title CTA `도둑질 시작!` |
| Usage | Title-only main CTA sprite with baked Hangul. Not lobby ART-LOB-005 |
| Source | Scenario AI. GPT Image 2.5 Sunburst transparent |
| ReferenceImage | casual CTA style `asset_uE3vGtHeHrBJb5zyHxuxPRkF`; title v002 `asset_rePD49mohF2ctDyorUL2Cxjz` |
| Model | `model_openai-gpt-image-2-5-sunburst` |
| Job | `job_TbcJvTkDxzgbjZPKwvYV7AG3` (pass). Attempt 1 `job_NgWGw7ShtNb6aYtXXdZrffbg` failed OpenAI illicit filter, 12 CU |
| ResultAsset | `asset_wmnWYTB4VJKwJb5qX86tnkHD` |
| Parameters | 1536×512, quality high, background transparent, numOutputs 1, 2 referenceImages |
| CU | 24 (12 fail + 12 pass). Budget 36. No third attempt |
| Prompt | Isolated candy-game start button, Hangul `도둑질 시작!` once, no characters, no 작전 시작 |
| SourceFile | `Docs/Art/Source/Scenario/CT_Title_StartButton_v002.png` |
| UnityAssetPath | `Assets/Art/Title/CT_Title_StartButton_v002.png` |
| Status | Integrated |
| Version | v002 |
| Notes | Baked text. No TMP duplicate. Press scale only. Login CTA under D-018 (no auto-lobby). v001 preserved |

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
| Usage | Confirmed master original. Not the lobby Image sprite. Closed blink frame v001 In Review; Unity blink not wired |
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

| Field | ART-LOB-002 Blink Closed v001 |
|---|---|
| AssetID | ART-LOB-002 |
| AssetName | Lobby thief idle blink closed frame |
| Usage | Open/Closed 2-frame blink on `Title.unity`. Half frame not made |
| Source | Local pixel edit of a copy of v007. White eye interiors + pupils filled with sampled mask-rim black; white closed-lid curves drawn. v007 master file not modified |
| CU | 0 |
| Parameters | Master 1280×1664 RGBA. Display 230×299 LANCZOS, same as v007 display230 |
| SourceFile | `Docs/Art/Source/Scenario/ART-LOB-002_CT_Lobby_Thief_Blink_Closed_v001_source.png` |
| DisplayFile | `Docs/Art/Source/Scenario/ART-LOB-002_CT_Lobby_Thief_Blink_Closed_v001_display230.png` |
| Compare | `Docs/Art/Source/Scenario/ART-LOB-002_CT_Lobby_Thief_Idle_v007_vs_Blink_Closed_v001_compare.png` |
| UnityAssetPath | `Assets/Art/Lobby/Review/CT_Lobby_Thief_Blink_Closed_v001_display230.png` (`Title.unity` `V02LobbyBlinkAnimator`) |
| Status | Integrated |
| Version | Blink Closed v001 |
| Notes | Pixel-identical to v007 outside the two eye interiors. Beanie/mask rim/skin/body unchanged. Open remains v007 display230. |

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

| Field | ART-UI-001–005 HUD sheet v001 |
|---|---|
| AssetID | ART-UI-001, ART-UI-002, ART-UI-003, ART-UI-004, ART-UI-005 |
| AssetName | Lobby top HUD widget sheet |
| Usage | Profile frame, empty nickname plate, energy/gold/candy icons+chips, settings hamburger. TMP for nickname and values. Unity Multiple Sprite. Not wired |
| Source | Scenario AI. GPT Image 2.5 Sunburst transparent |
| ReferenceImage | `REF-UI-003` / `asset_8ZyE7Tj2m5BuUbJ4p1eXxg53` |
| Model | `model_openai-gpt-image-2-5-sunburst` |
| Job | `job_XYFgsDpz9FEcgZQ3huu1LmpD` |
| ResultAsset | `asset_eDhULVAu8bR8Cn5LHHDPovQV` |
| Parameters | 1536×512, quality high, background transparent, numOutputs 1, 1 referenceImage |
| CU | 12 |
| SourceFile | `Docs/Art/Source/Scenario/CT_Lobby_HUD_Sheet_v001.png` |
| UnityAssetPath | `Assets/Art/Lobby/Review/CT_Lobby_HUD_Sheet_v001.png` (staging copy, not wired) |
| Status | In Review |
| Version | v001 |
| Notes | No baked digits/nickname. Empty avatar hole (transparent). Candy icon instead of REF gem (D-020). No `+`, no XP bar. 9 horizontal groups, gold/candy icon-to-chip gaps ~7–8px. Unity not applied |

| Field | ART-LOB-006 Crew button v001 |
|---|---|
| AssetID | ART-LOB-006 |
| AssetName | Lobby `크루` button |
| Usage | Independent mint button sprite. Baked Hangul. No TMP duplicate. Unity not applied |
| Source | Scenario AI. GPT Image 2.5 Sunburst transparent |
| ReferenceImage | `REF-UI-003` / `asset_8ZyE7Tj2m5BuUbJ4p1eXxg53` |
| Model | `model_openai-gpt-image-2-5-sunburst` |
| Job | `job_b7fcTnpNoPr9Hjhege8bFNEy` |
| ResultAsset | `asset_9NLTWvqnQEEqDjy2vhb3Bnhw` |
| Parameters | 1024×1024, quality high, background transparent, numOutputs 1, 1 referenceImage |
| CU | 12 |
| SourceFile | `Docs/Art/Source/Scenario/CT_Lobby_Btn_Crew_v001.png` |
| UnityAssetPath | `Assets/Art/Lobby/Review/CT_Lobby_Btn_Crew_v001.png` (staging copy, not wired) |
| Status | In Review |
| Version | v001 |
| Notes | Hangul `크루` readable. White 3D fill vs REF dark-brown fill. Landscape ~1.73:1 on square canvas. Vertical transparent pad ~200px (no local crop) |

| Field | ART-LOB-005 Start button v001 |
|---|---|
| AssetID | ART-LOB-005 |
| AssetName | Lobby `작전 시작` button |
| Usage | Independent yellow button sprite. Baked Hangul. Not ART-TTL-006. Unity not applied |
| Source | Scenario AI. GPT Image 2.5 Sunburst transparent |
| ReferenceImage | `REF-UI-003` / `asset_8ZyE7Tj2m5BuUbJ4p1eXxg53` |
| Model | `model_openai-gpt-image-2-5-sunburst` |
| Job | `job_uY94kqigsoV2dVatTHfAHAJA` |
| ResultAsset | `asset_4Z8edQhjaXdDf1mMFezNeszP` |
| Parameters | 1024×1024, quality high, background transparent, numOutputs 1, 1 referenceImage |
| CU | 12 |
| SourceFile | `Docs/Art/Source/Scenario/CT_Lobby_Btn_Start_v001.png` |
| UnityAssetPath | `Assets/Art/Lobby/Review/CT_Lobby_Btn_Start_v001.png` (staging copy, not wired) |
| Status | In Review |
| Version | v001 |
| Notes | Hangul `작전 시작` readable with space. Candy icon overlaps top edge (sticker). Landscape ~1.79:1 |

| Field | ART-LOB-007 Upgrade button v001 |
|---|---|
| AssetID | ART-LOB-007 |
| AssetName | Lobby `업그레이드` button |
| Usage | Independent coral button sprite. Baked Hangul. Unity not applied |
| Source | Scenario AI. GPT Image 2.5 Sunburst transparent |
| ReferenceImage | `REF-UI-003` / `asset_8ZyE7Tj2m5BuUbJ4p1eXxg53` |
| Model | `model_openai-gpt-image-2-5-sunburst` |
| Job | `job_deATSMHtqsmvkWvpdUUoQPKZ` |
| ResultAsset | `asset_ftKZ3x5kHYZCPPC9GG7Q6CAv` |
| Parameters | 1024×1024, quality high, background transparent, numOutputs 1, 1 referenceImage |
| CU | 12 |
| SourceFile | `Docs/Art/Source/Scenario/CT_Lobby_Btn_Upgrade_v001.png` |
| UnityAssetPath | `Assets/Art/Lobby/Review/CT_Lobby_Btn_Upgrade_v001.png` (staging copy, not wired) |
| Status | In Review |
| Version | v001 |
| Notes | Hangul `업그레이드` readable. Wrench + green up-arrow match REF. Landscape ~1.63:1. Preserve. Next independent file is Enhance `강화` if group proof is approved |

| Field | ART-LOB-008 Button group proof v001 |
|---|---|
| AssetID | ART-LOB-008 |
| AssetName | Lobby bottom button group proof (`크루` / `작전 시작` / `강화`) |
| Usage | Design reference for independent v002/Enhance sprites. Not Unity-applied. Do not crop or slice into final PNGs |
| Source | Scenario AI. GPT Image 2.5 Sunburst transparent |
| ReferenceImage | `REF-UI-003` `asset_8ZyE7Tj2m5BuUbJ4p1eXxg53`; Crew v001 `asset_9NLTWvqnQEEqDjy2vhb3Bnhw`; Start v001 `asset_4Z8edQhjaXdDf1mMFezNeszP`; Upgrade v001 `asset_ftKZ3x5kHYZCPPC9GG7Q6CAv` |
| Model | `model_openai-gpt-image-2-5-sunburst` |
| Job | `job_cSxMq4wdXV1PKDrDPZikwNc6` |
| ResultAsset | `asset_aQVfgvt2CMQ2JgWnHwyU72fe` |
| Parameters | 1536×512, quality high, background transparent, numOutputs 1, 4 referenceImages |
| CU | 13 |
| SourceFile | `Docs/Art/Source/Scenario/CT_Lobby_Btn_Group_Proof_v001.png` |
| UnityAssetPath | (none) |
| Status | In Review |
| Version | v001 |
| Notes | One-shot group proof. Center slightly wider/taller. Hangul `강화`. Independent Crew v002 / Start v002 / Enhance v001 generated from this proof; do not crop |

| Field | ART-LOB-006 Crew button v002 |
|---|---|
| AssetID | ART-LOB-006 |
| AssetName | Lobby `크루` button |
| Usage | Independent mint button sprite. Baked Hangul. Unity not applied |
| Source | Scenario AI. GPT Image 2.5 Sunburst transparent |
| ReferenceImage | `REF-UI-003` `asset_8ZyE7Tj2m5BuUbJ4p1eXxg53`; group proof `asset_aQVfgvt2CMQ2JgWnHwyU72fe`; Crew v001 `asset_9NLTWvqnQEEqDjy2vhb3Bnhw` |
| Model | `model_openai-gpt-image-2-5-sunburst` |
| Job | `job_xBm3Y29rsfec66hCFvoxm5eA` |
| ResultAsset | `asset_FaJK2X2yTY6sXz4VtYKyAwv1` |
| Parameters | 1024×768, quality high, background transparent, numOutputs 1, 3 referenceImages |
| CU | 13 |
| SourceFile | `Docs/Art/Source/Scenario/CT_Lobby_Btn_Crew_v002.png` |
| UnityAssetPath | `Assets/Art/Lobby/Review/CT_Lobby_Btn_Crew_v002.png` (staging copy, not wired) |
| Status | In Review |
| Version | v002 |
| Notes | Hangul `크루` OK. Body outline still merges around the beanie (same issue as group proof). v001 preserved |

| Field | ART-LOB-005 Start button v002 |
|---|---|
| AssetID | ART-LOB-005 |
| AssetName | Lobby `작전 시작` button |
| Usage | Independent yellow button sprite. Baked Hangul. Not ART-TTL-006. Unity not applied |
| Source | Scenario AI. GPT Image 2.5 Sunburst transparent |
| ReferenceImage | `REF-UI-003` `asset_8ZyE7Tj2m5BuUbJ4p1eXxg53`; group proof `asset_aQVfgvt2CMQ2JgWnHwyU72fe`; Start v001 `asset_4Z8edQhjaXdDf1mMFezNeszP` |
| Model | `model_openai-gpt-image-2-5-sunburst` |
| Job | `job_5oCAadEiP5L6JrZgbzA9aXdu` |
| ResultAsset | `asset_7Tk3uJyLfmfDWuEtEepME5nV` |
| Parameters | 1024×768, quality high, background transparent, numOutputs 1, 3 referenceImages |
| CU | 13 |
| SourceFile | `Docs/Art/Source/Scenario/CT_Lobby_Btn_Start_v002.png` |
| UnityAssetPath | `Assets/Art/Lobby/Review/CT_Lobby_Btn_Start_v002.png` (staging copy, not wired) |
| Status | In Review |
| Version | v002 |
| Notes | Hangul `작전 시작` OK. Candy sticker outline separate from complete plate. v001 preserved |

| Field | ART-LOB-007 Enhance button v001 |
|---|---|
| AssetID | ART-LOB-007 |
| AssetName | Lobby `강화` button |
| Usage | Independent coral button sprite. Baked Hangul. Unity not applied |
| Source | Scenario AI. GPT Image 2.5 Sunburst transparent |
| ReferenceImage | `REF-UI-003` `asset_8ZyE7Tj2m5BuUbJ4p1eXxg53`; group proof `asset_aQVfgvt2CMQ2JgWnHwyU72fe`; Upgrade v001 `asset_ftKZ3x5kHYZCPPC9GG7Q6CAv` |
| Model | `model_openai-gpt-image-2-5-sunburst` |
| Job | `job_nxGA7hJpPHcp7kmEAUF3AEe1` |
| ResultAsset | `asset_i5b2tzo6LgQRNTM5qM1QPsk4` |
| Parameters | 1024×768, quality high, background transparent, numOutputs 1, 3 referenceImages |
| CU | 13 |
| SourceFile | `Docs/Art/Source/Scenario/CT_Lobby_Btn_Enhance_v001.png` |
| UnityAssetPath | `Assets/Art/Lobby/Review/CT_Lobby_Btn_Enhance_v001.png` (staging copy, not wired) |
| Status | In Review |
| Version | v001 |
| Notes | Hangul `강화` OK. Wrench/arrow have their own outlines on a complete plate. Upgrade v001 preserved |

| Field | ART-LOB-009 Trapezoid guides |
|---|---|
| AssetID | ART-LOB-009 |
| AssetName | Lobby button trapezoid silhouette guides |
| Usage | Scenario shape reference only. Not Unity. Not production art |
| Source | Local Python geometry (D-017 owner exception 2026-11-12). 0 Scenario CU |
| ResultAsset | Top-wide `asset_oMJjh59pXoRN6U2EheFr89SY`; bottom-wide `asset_SdRoBY4KW7GH9e79gZiv9xR8` |
| Parameters | Top-wide 816×768, top:bottom 720:612 (85%); bottom-wide 1024×768, top:bottom 788.8:928 (85%) |
| SourceFile | `Docs/Art/Source/Scenario/CT_Lobby_Btn_TrapezoidGuide_TopWide_v001.png`; `CT_Lobby_Btn_TrapezoidGuide_BottomWide_v001.png` |
| UnityAssetPath | (none) |
| Status | Reference only |
| Version | v001 |
| Notes | Filled dark-brown trapezoids, no icons/text. Reused for Crew v003 / Start v003 / Enhance v002 (directions reversed vs first plan) |

| Field | ART-LOB-006 Crew button v003 |
|---|---|
| AssetID | ART-LOB-006 |
| AssetName | Lobby `크루` button |
| Usage | Independent mint button. Baked Hangul. Unity not applied |
| Source | Scenario AI. GPT Image 2.5 Sunburst transparent |
| ReferenceImage | BottomWide guide `asset_SdRoBY4KW7GH9e79gZiv9xR8`; Crew v002 `asset_FaJK2X2yTY6sXz4VtYKyAwv1`; Start v001 `asset_4Z8edQhjaXdDf1mMFezNeszP`; REF-UI-003 `asset_8ZyE7Tj2m5BuUbJ4p1eXxg53` |
| Model | `model_openai-gpt-image-2-5-sunburst` |
| Job | `job_93UnBsXBR4SemfiDz3Zv45x9` |
| ResultAsset | `asset_fc8MvPGiPqqnCuDTJPNjb1Fa` |
| Parameters | 816×768, quality high, background transparent, numOutputs 1, 4 referenceImages |
| CU | 13 |
| SourceFile | `Docs/Art/Source/Scenario/CT_Lobby_Btn_Crew_v003.png` |
| UnityAssetPath | `Assets/Art/Lobby/Review/CT_Lobby_Btn_Crew_v003.png` (staging, not wired) |
| Status | In Review |
| Version | v003 |
| Notes | Bottom-wide trapezoid. Hangul `크루` OK. Body outline still merges around beanie. v001/v002 preserved |

| Field | ART-LOB-005 Start button v003 |
|---|---|
| AssetID | ART-LOB-005 |
| AssetName | Lobby `작전 시작` button |
| Usage | Independent yellow button. Baked Hangul. Not ART-TTL-006. Unity not applied |
| Source | Scenario AI. GPT Image 2.5 Sunburst transparent |
| ReferenceImage | TopWide guide `asset_oMJjh59pXoRN6U2EheFr89SY`; Start v001 `asset_4Z8edQhjaXdDf1mMFezNeszP`; REF-UI-003 `asset_8ZyE7Tj2m5BuUbJ4p1eXxg53` |
| Model | `model_openai-gpt-image-2-5-sunburst` |
| Job | `job_nVQsh2FhRCwBcD6D2JXRbn9N` |
| ResultAsset | `asset_oTfL7jGvrndWGhfSVXBETrut` |
| Parameters | 1024×768, quality high, background transparent, numOutputs 1, 3 referenceImages |
| CU | 13 |
| SourceFile | `Docs/Art/Source/Scenario/CT_Lobby_Btn_Start_v003.png` |
| UnityAssetPath | `Assets/Art/Lobby/Review/CT_Lobby_Btn_Start_v003.png` (staging, not wired) |
| Status | In Review |
| Version | v003 |
| Notes | Top-wide trapezoid. Hangul `작전 시작` OK. Candy outline separate. v001/v002 preserved |

| Field | ART-LOB-007 Enhance button v002 |
|---|---|
| AssetID | ART-LOB-007 |
| AssetName | Lobby `강화` button |
| Usage | Independent coral button. Baked Hangul. Unity not applied |
| Source | Scenario AI. GPT Image 2.5 Sunburst transparent |
| ReferenceImage | BottomWide guide `asset_SdRoBY4KW7GH9e79gZiv9xR8`; Enhance v001 `asset_i5b2tzo6LgQRNTM5qM1QPsk4`; REF-UI-003 `asset_8ZyE7Tj2m5BuUbJ4p1eXxg53` |
| Model | `model_openai-gpt-image-2-5-sunburst` |
| Job | `job_JpXPG1pneVv4uVo2hULkuLC3` |
| ResultAsset | `asset_wfuP2K2pSKCaBLEcxMnpqeRr` |
| Parameters | 816×768, quality high, background transparent, numOutputs 1, 3 referenceImages |
| CU | 13 |
| SourceFile | `Docs/Art/Source/Scenario/CT_Lobby_Btn_Enhance_v002.png` |
| UnityAssetPath | `Assets/Art/Lobby/Review/CT_Lobby_Btn_Enhance_v002.png` (staging, not wired) |
| Status | In Review |
| Version | v002 |
| Notes | Bottom-wide trapezoid. Hangul `강화` OK. Icon outlines separate. Enhance v001 preserved |

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
| 2026-11-10 | ART-LOB-002 Blink Closed v001 | Local Closed frame from v007 copy. 1280×1664 + 230×299 LANCZOS. In Review. No Scenario. `Title.unity` not wired. |
| 2026-11-10 | ART-LOB-002 blink on Title | `V02LobbyBlinkAnimator` on `Title.unity`. Open display230 / Closed display230. 3–6s / 0.1s. Idle breath-sway unchanged. |
| 2026-11-10 | ART-TTL-002 v001 | Sunburst 1080×1920 title composite. Guard chase, v007 thief, login band without baked UI. 15 CU. In Review. Unity not applied. |
| 2026-11-10 | ART-TTL-002 v002 | Sunburst full regen from v001 (no mask). Playful closed smile, sweat removed. 15 CU. Plate Approved. v001 preserved. |
| 2026-11-10 | ART-TTL-002 layers | Local split of v002 attempted (no Scenario). Background smear, character fringe/overlap, sack hole. Below quality gate. Unity not applied. No extra CU. |
| 2026-11-10 | ART-TTL-002 26 CU package | BG Sunburst inpaint 13 + Thief 851 1 + Guard 851 1 + Logo local 0 + Login UI sheet 11 = 26 CU. One shot each. Cutout quality fail. Unity not applied. |
| 2026-11-10 | Art pipeline D-017 | Independent assets preferred. Scenario-only image gen/edit. Comp extract banned. Per-job layer decision + CU approval. Failed TTL-002 cutouts/inpaint `Not adopted`. UI 005/006 remain reuse candidates. |
| 2026-11-10 | ART-TTL-006 v002 | Sunburst 1536×512 baked `도둑질 시작!`. 24 CU (1 moderation fail + 1 pass). Integrated on `Title.unity`. v001 preserved. |
| 2026-11-10 | Title login D-018 | v002 plate + nickname frame Integrated. Local JSON profiles. Auto-lobby removed. 0 CU. |
| 2026-11-11 | Lobby HUD D-020 | Nickname/energy/gold/candy HUD live. Button order 크루 · 작전 시작 · 업그레이드. Sheets planned. Dry-run 12+12 CU. Generation not started. |
| 2026-11-12 | Phase B-1 HUD+buttons | Sunburst High transparent. HUD sheet 12 + Crew 12 + Start 12 + Upgrade 12 = 48 CU. One shot each. In Review. Unity not applied. Hangul QA pass. |
| 2026-11-12 | D-020 `강화` + group proof | Right button label `업그레이드` → `강화`. Group proof 13 CU `ART-LOB-008`. Independent v002/Enhance gens not started. v001 buttons preserved. HUD unchanged. |
| 2026-11-12 | Lobby buttons v002/Enhance | Sunburst 1024×768 High transparent. Crew 13 + Start 13 + Enhance 13 = 39 CU. One shot each. In Review. v001 + proof + HUD preserved. Unity not applied. |
| 2026-11-12 | Trapezoid guides ART-LOB-009 | Local Python 85% trapezoids, 0 CU. Uploaded as Scenario shape refs. Button gens not started. D-017 exception is guide-only. |
| 2026-11-12 | Lobby buttons trapezoid v003 | Crew v003 + Start v003 + Enhance v002. 39 CU. Guides reused, directions reversed vs first plan. In Review. Unity not applied. |

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

생성 절차는 `SCENARIO_PIPELINE.md`. 생성과 Unity 적용은 기본으로 분리한다. `Assets/Art/`로 복사할 때 리사이즈·크롭·색 보정·배경 제거를 하지 않는다.
