# Candy Thief — Scenario MCP pipeline

운영 규칙. 이미지 생성·유료 호출·Unity 적용은 이 문서만으로 실행하지 않는다. 각 단계는 사용자 요청과 승인이 있을 때만 진행한다.

관련 문서: `Docs/Art/ART_STYLE_GUIDE.md`, `Docs/Art/ART_ASSET_LIST.md`.  
게임 규칙 소스: `Docs/GDD.md`, `Docs/DECISIONS.md`, `Docs/TECH_SPEC.md`.

## 0. Hard rules

- 로컬 파일 경로를 Scenario `model_run`의 image 참조로 쓰지 않는다. Scenario `asset_id`만 쓴다.
- 이미지 참조를 지원하는지는 `model_schema_get`으로 확인한다.
- `search`에 인라인 이미지를 넣으면 프로젝트에 업로드된다. 텍스트/`asset_id` 검색만 기본으로 쓴다.
- `prompt_spark`, `asset_caption`, `asset_describe`, `asset_analyze`, `asset_detect`, 업스케일, 배경 제거, `numOutputs` > 1은 추가 CU가 될 수 있다. 생성 전에 `dry_run`으로 견적을 내고 사용자 승인을 받는다.
- 생성과 Unity 적용을 한 작업으로 자동 연결하지 않는다. 사용자가 명시적으로 한 요청에 묶은 경우만 연속 실행한다.
- UI 레퍼런스(`REF-UI-*`)의 숫자·버튼·재화·시스템을 게임에 추가하지 않는다.

## 1. Before generation

1. 목적과 대상 화면/상황을 확인한다.
2. `ART_ASSET_LIST.md`에서 기존 에셋과 재사용 가능 여부를 확인한다.
3. 아래 표에서 공식 레퍼런스를 고른다.
4. `ART_STYLE_GUIDE.md`의 캐릭터·경비·배경·UI 규칙을 적용한다.
5. `recommend` 또는 `model_schema_get`으로 모델과 입력 필드를 확인한다. 가능하면 `model_run(..., dry_run=true)`로 CU를 견적한다.
6. 실제 `model_run` 전에 사용자 승인을 받는다.

### Preferred references

| 제작 대상 | 우선 참조 |
|---|---|
| 도둑 캐릭터 | REF-ACT-003, REF-ACT-004 |
| 도둑 + 경비 | REF-ACT-002, REF-ACT-005 |
| 사탕 가게 배경 | REF-BG-001 |
| 인게임 HUD 룩 | REF-UI-001 |
| 결과 팝업 룩 | REF-UI-002 |
| 로비 룩 | REF-UI-003 |
| 탈출/ stealth 연출 | REF-ACT-001 (보조), 위 도둑 참조와 함께 |

파일: `Docs/Art/References/<AssetID>.png`.

## 2. Using references in Scenario

1. `ART_ASSET_LIST`에서 해당 `REF-*`의 Scenario Asset ID가 있으면 재사용한다.
2. 없으면 필요한 레퍼런스만 `upload_asset` → (multipart면) `upload_asset_complete`.
3. 반환된 Scenario `asset_id`를 에셋 목록에 기록한다.
4. `model_run` parameters의 image/reference 필드에는 그 `asset_id`만 넣는다.
5. 캐릭터가 나오면 승인된 도둑/경비 레퍼런스를 우선한다.

현재 CandyThief Scenario 프로젝트에는 커스텀 학습 모델이 없다. 공개 모델을 쓰되, 플랜 업그레이드가 필요한 모델은 표시만 하고 승인 없이 실행하지 않는다.

## 3. After generation

1. 결과를 기존 Integrated 에셋과 다른 경로에 둔다. 원본: `Docs/Art/Source/Scenario/`.
2. 중복되지 않는 AssetID를 부여한다. 신규 게임 에셋은 `ART-<AREA>-###`. 신규 스타일 레퍼런스는 `REF-<AREA>-###`. 기존 ID를 덮어쓰지 않는다.
3. 모델 ID, 프롬프트, 참조 `REF-*` / Scenario `asset_id`, 결과 Scenario `asset_id`를 `ART_ASSET_LIST.md`에 기록한다.
4. 확인 가능한 범위에서 CU를 `usage` 또는 job 응답에서 기록한다.
5. 초기 상태: `In Review`.
6. 사용자 검수·승인 후에만 `Approved Reference` 또는 Unity 적용 대상으로 올린다. 승인만으로 `Integrated`로 바꾸지 않는다.

## 4. Unity apply (separate task)

사용자 승인 이후, 별도 요청으로만:

1. `asset_download(format="png")` 후 `curl -L`로 저장
2. 캐릭터/아이콘은 필요 시 배경 제거 모델. 외곽선이 상할 것 같으면 강제 크로마키하지 말고 보고
3. 원본은 `Docs/Art/Source/`에 보존
4. 게임용은 `Assets/Art/...` 및 런타임 복제가 필요하면 `Assets/Resources/V02/...`
5. Sprite Import: Point, mipmap off, `alphaIsTransparency`
6. 기존 프리팹/UI 확인. Rebuild 금지. Patch 또는 최소 필드 연결
7. Play Mode 검증
8. `ART_ASSET_LIST` 상태를 `Integrated`로 갱신

## 5. Status meanings

| Status | Meaning |
|---|---|
| Approved Reference | 아트 스타일 레퍼런스로 승인됨. 게임에 넣었다는 뜻이 아님 |
| In Review | 생성됨. 사용자 검수 전 |
| Planned | 아직 없음 |
| Final | 프로덕션 아트로 확정, 아직 미적용일 수 있음 |
| Integrated | Unity에 연결되어 사용 중 |
