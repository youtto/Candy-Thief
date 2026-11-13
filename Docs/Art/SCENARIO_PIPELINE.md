# Candy Thief — Art production pipeline (Scenario)

운영 규칙. 이미지 생성·유료 호출·Unity 적용은 이 문서만으로 실행하지 않는다. 각 단계는 사용자 요청과 승인이 있을 때만 진행한다.

관련 문서: `Docs/Art/ART_STYLE_GUIDE.md`, `Docs/Art/ART_ASSET_LIST.md`.  
게임 규칙 소스: `Docs/GDD.md`, `Docs/DECISIONS.md`, `Docs/TECH_SPEC.md`.  
승인 결정: `Docs/DECISIONS.md` D-017.

타이틀, 로비, 캐릭터, 배경, 소품, UI 등 **모든 아트 제작**에 적용한다.

## 0. Hard rules

- 로컬 파일 경로를 Scenario `model_run`의 image 참조로 쓰지 않는다. Scenario `asset_id`만 쓴다.
- 이미지 참조를 지원하는지는 `model_schema_get`으로 확인한다.
- `search`에 인라인 이미지를 넣으면 프로젝트에 업로드된다. 텍스트/`asset_id` 검색만 기본으로 쓴다.
- `prompt_spark`, `asset_caption`, `asset_describe`, `asset_analyze`, `asset_detect`, 업스케일, 배경 제거, `numOutputs` > 1은 추가 CU가 될 수 있다. 생성 전에 `dry_run`으로 견적을 내고 사용자 승인을 받는다.
- **사용자 승인 없이 CU가 발생하는 작업을 실행하지 않는다.** 승인 횟수·CU 한도 초과 재시도도 금지. 품질 미달 시 임의로 다른 모델을 호출하지 않고 보고한다.
- 생성과 Unity 적용을 한 작업으로 자동 연결하지 않는다. 사용자가 명시적으로 한 요청에 묶은 경우만 연속 실행한다.
- UI 레퍼런스(`REF-UI-*`)의 숫자·버튼·재화·시스템을 게임에 추가하지 않는다.
- Cursor는 이미지의 시각적 내용을 생성·수정하지 않는다. 다운로드·복사·Unity Import는 픽셀을 바꾸지 않는다.

## 1. Core principles

1. **독립 에셋 우선** — 가능하면 배경·캐릭터·소품·로고·UI를 재사용 가능한 독립 리소스로 제작한다.
2. **Scenario AI 전담** — 이미지 생성과 수정은 Scenario에서 한다. Cursor는 이미지를 직접 만들거나 고치지 않는다.
3. **시안 기반 개별 재생성** — 완성형 시안은 디자인 레퍼런스다. 레이어가 필요하면 시안에서 추출하지 않고 각 요소를 새로 생성한다.
4. **작업별 레이어 판단** — 재사용, 애니메이션, Unity 조절, UI 상호작용, 수정 가능성, CU/작업량을 보고 분리 여부를 정한다. 실익이 낮으면 통합 이미지를 허용하되 이유를 보고한다.
5. **사전 비용 승인** — Scenario 호출 전 예상 CU를 보고하고 승인받는다.

## 2. Independent assets vs composite

기본 선호 구조 예:

```text
Title
 ├─ Background
 ├─ Logo
 ├─ Characters (Thief, Guard)
 └─ LoginUI (NicknameInput, StartButton, StatusText = TMP)
```

작업 시작 전 검토:

- 다른 화면 재사용
- 개별 애니메이션
- Unity 위치·크기 조절
- UI 상호작용
- 향후 수정
- 개별 제작 CU·작업량

통합 이미지를 쓰는 경우 선택 이유를 짧게 보고한다.

## 3. Roles

### Scenario AI

신규 생성, 기존 이미지 수정, 캐릭터 디자인·표정·포즈, 배경, 소품, UI 위젯 아트, 투명 배경 Sprite, 인페인트, 업스케일, 배경 제거, 그 외 시각 결과물을 바꾸는 작업.

Scenario가 제공하는 모델·기능을 우선한다. 승인되지 않은 모델로 바꾸지 않는다.

### Cursor — 허용

- 모델·기능 조사, 프롬프트, 레퍼런스 선정
- Scenario 호출·작업 관리, `dry_run` CU 견적
- 결과 다운로드, 메타데이터·해상도 **확인**(변경 아님)
- 파일명·버전, `ART_ASSET_LIST` 기록
- Unity Sprite Import, UI 구성, Sprite 연결
- RectTransform / Pivot / Anchor
- 애니메이션·스크립트
- Unity에서 Sprite 배치, UI 상태별 크기·색·알파 조절

Unity 배치나 tint/scale로 **새 PNG를 만들어 아트 원본을 대체하지 않는다.**

### Cursor — 금지

작은 수정도 예외 없다.

- Python/Pillow, OpenCV, 픽셀 직접 편집
- 코드로 도형·선 그리기
- 영역 지우기·덧그리기, 로컬 인페인트·리터치
- 눈·입·표정·깜빡임 프레임을 로컬에서 수정
- 크로마키, Flood Fill, 로컬 배경 제거
- 이미지 합성으로 신규 아트 결과물 제작
- 해상도 변경·크롭·색 보정을 새 PNG로 저장 (필요하면 Scenario 작업)

예외: 승인된 **겹치지 않는 투명 UI 시트**를 Unity Sprite Editor에서 영역 지정해 분리. 픽셀을 고치거나 배경을 지우지 않는다.

예외 (2026-11-12, 오너): 게임에 넣지 않는 **기하 실루엣 가이드**(단순 사다리꼴 등)는 Python/SVG로 로컬 제작 후 Scenario 업로드 가능. 기존 아트 크롭·리터치 금지. Unity Sprite로 쓰지 않음. 완성형 아트 추출 금지는 유지.

## 4. Composite comps

전체 구도 검수용 완성형 시안은 만들 수 있다. 승인된 시안은 후속 제작의 기준 레퍼런스다.

### 금지 (시안에서 추출)

ART-TTL-002에서 실패했던 방식은 파이프라인에서 제외한다.

- 완성형에서 캐릭터 크롭
- 크롭에 Background Remover
- 외곽선 검출·Flood Fill로 Sprite 추출
- 원본 픽셀을 잘라 독립 Sprite로 사용
- 캐릭터를 지운 뒤 배경을 복원해 레이어 제작

알려진 실패: 외곽선 손상, 겹친 손·소품 누락, 배경 잔상, 내부 투명 구멍, 배경 디자인 훼손.

### 레이어가 필요할 때

승인 시안을 레퍼런스로 **각 요소를 새로 생성**한다.

| 요소 | 방법 |
|---|---|
| Background | 시안의 거리·건물·색감 참조. 캐릭터·로고 없는 배경을 신규 생성. 시안에서 지워서 복원하지 않음 |
| Character | 시안의 디자인·포즈 참조. 투명 배경 Sprite 신규 생성. 잘라내지 않음 |
| Logo | 승인 로고 참조. 투명 리소스 신규 생성 |
| UI | 승인 UI 참조. 위젯은 독립 리소스. 텍스트는 TMP |

개별 에셋은 시안과 픽셀 단위로 같을 필요는 없다. 디자인·색감·비율·외곽선·스타일 일관성을 유지한다.

## 5. Reference priority

생성 시 이 순서:

1. 승인된 개별 캐릭터·오브젝트 마스터
2. 승인된 완성형 시안
3. `ART_STYLE_GUIDE.md`
4. 관련 `REF-*`

예: 타이틀 도둑 Sprite — v007 = 외형, 타이틀 v002 = 달리기 포즈·구도, 스타일 가이드 = 색·외곽선·명암. 프롬프트에 각 레퍼런스 목적을 적는다.

기존 표 (화면 종류별 기본 `REF-*`):

| 제작 대상 | 우선 참조 |
|---|---|
| 도둑 캐릭터 | 마스터가 있으면 그것 우선. 없으면 REF-ACT-003, REF-ACT-004 |
| 도둑 + 경비 | REF-ACT-002, REF-ACT-005 |
| 사탕 가게 배경 | REF-BG-001 |
| 인게임 HUD 룩 | REF-UI-001 |
| 결과 팝업 룩 | REF-UI-002 |
| 로비 룩 | REF-UI-003 |
| 탈출/stealth 연출 | REF-ACT-001 (보조) + 도둑 참조 |

파일: `Docs/Art/References/<AssetID>.png`.

## 6. UI assets

재사용 가능성이 높으므로 독립 에셋을 우선한다. 버튼·입력창·팝업·아이콘·탭·슬롯·상태 위젯.

텍스트는 이미지에 넣지 않는다. TMP를 쓴다.

예외 (오너 승인): 타이틀 CTA `도둑질 시작!` (ART-TTL-006), 로비 하단 버튼 `크루` / `작전 시작` / `강화` (D-020). 이 버튼은 아이콘+한글을 스프라이트에 굽고 TMP를 중복하지 않는다. 로비 상단 HUD 숫자·닉네임은 예외가 아니다.

상태(Normal/Pressed/Disabled)가 필요하면 개별 Sprite를 검토한다. 색·크기만으로 충분하면 Unity UI로 처리한다.

동일 스타일 여러 위젯은 한 장의 투명 시트로 만들 수 있다. 조건: 서로 겹치지 않음, 개별 Sprite로 쓸 수 있음, 시각 수정 없이 Sprite Editor로 분리 가능.

## 7. Before generation

1. 목적과 대상 화면/상황을 확인한다.
2. `ART_ASSET_LIST.md`에서 기존 에셋과 재사용 가능 여부를 확인한다.
3. §2로 독립 에셋 vs 통합을 판단한다. 통합이면 이유를 적는다.
4. §5 레퍼런스 우선순위로 이미지를 고른다.
5. `ART_STYLE_GUIDE.md`의 캐릭터·경비·배경·UI 규칙을 적용한다.
6. `recommend` 또는 `model_schema_get`으로 모델과 입력 필드를 확인한다. 가능하면 `model_run(..., dry_run=true)`로 CU를 견적한다.
7. 아래 표를 보고한 뒤 실제 `model_run` 전에 사용자 승인을 받는다.

| 항목 | 내용 |
|---|---|
| 에셋 | 제작 대상 |
| 모델 | 사용할 Scenario 모델 |
| 방식 | 신규 생성 / 편집 / 투명 배경 생성 등 |
| 레퍼런스 | 사용할 이미지와 각 목적 |
| 해상도 | 최종 출력 크기 |
| 예상 CU | dry_run 기준 |
| 재사용 가능성 | 다른 화면 활용 여부 |

현재 CandyThief Scenario 프로젝트에는 커스텀 학습 모델이 없다. 공개 모델을 쓰되, 플랜 업그레이드가 필요한 모델은 표시만 하고 승인 없이 실행하지 않는다.

## 8. Using references in Scenario

1. `ART_ASSET_LIST`에서 해당 파일의 Scenario Asset ID가 있으면 재사용한다.
2. 없으면 필요한 레퍼런스만 `upload_asset` → (multipart면) `upload_asset_complete`.
3. 반환된 Scenario `asset_id`를 에셋 목록에 기록한다.
4. `model_run` parameters의 image/reference 필드에는 그 `asset_id`만 넣는다.
5. 캐릭터가 나오면 승인된 도둑/경비 마스터를 시안·`REF-*`보다 앞에 둔다.

## 9. After generation

1. 결과를 기존 Integrated 에셋과 다른 경로에 둔다. 원본: `Docs/Art/Source/Scenario/`.
2. 중복되지 않는 AssetID를 부여한다. 신규 게임 에셋은 `ART-<AREA>-###`. 신규 스타일 레퍼런스는 `REF-<KIND>-###`. 기존 ID를 덮어쓰지 않는다.
3. 모델 ID, 프롬프트, 참조 `REF-*` / Scenario `asset_id`, 결과 Scenario `asset_id`를 `ART_ASSET_LIST.md`에 기록한다.
4. 확인 가능한 범위에서 CU를 `usage` 또는 job 응답에서 기록한다.
5. 초기 상태: `In Review`.
6. 사용자 검수·승인 후에만 `Approved Reference` 또는 Unity 적용 대상으로 올린다. 승인만으로 `Integrated`로 바꾸지 않는다.
7. 다운로드한 파일을 로컬에서 리터치하지 않는다.

## 10. Unity apply (separate task)

사용자 승인 이후, 별도 요청으로만:

1. `asset_download(format="png")` 후 저장. 저장 과정에서 리사이즈·크롭·색 보정을 하지 않는다.
2. 투명 배경이 필요하면 **생성 단계에서** Scenario로 만든다. 적용 중 로컬 크로마키·Flood Fill·크롭 컷아웃을 하지 않는다. Scenario 배경 제거가 필요하면 별도 CU 견적·승인.
3. 원본은 `Docs/Art/Source/`에 보존. `Assets/Art/`에는 검수 통과분만이 게임 리소스다.
4. 런타임 복제가 필요하면 `Assets/Resources/V02/...`
5. Sprite Import: 기존 프로젝트 관례. mipmap off, `alphaIsTransparency`. Import 설정(PPU, max size)은 원본 PNG를 덮어쓰지 않으면 허용.
6. 기존 프리팹/UI 확인. Rebuild 금지. Patch 또는 최소 필드 연결.
7. Inspector에서 Sprite 교체, Scene View에서 위치·크기, Anchor/Pivot, 화면 비율 왜곡 방지.
8. Play Mode 검증.
9. `ART_ASSET_LIST` 상태를 `Integrated`로 갱신.

## 11. ART-TTL-002

- 승인 시안: `Docs/Art/Source/Scenario/ART-TTL-002_CT_Title_Day_v002_source.png`. Status `Approved` 유지.
- 실패한 로컬/851 컷아웃·시안 인페인트 배경은 최종 에셋이 아니다. 파일은 보존한다.
- 타이틀을 레이어로 구성할 때는 Background, Thief, Guard, (필요 시) Logo를 Scenario로 **각각 재생성**한다. 시안 크롭·인페인트 복원을 다시 쓰지 않는다.
- 품질이 확인된 독립 UI(`ART-TTL-005`, `ART-TTL-006`)는 재사용 후보. Unity 적용은 별도 승인.
- v002 원본을 Unity에 **통합 이미지**로 쓰는 것은 허용한다. 재사용·애니메이션이 필요하면 개별 에셋을 우선 검토한다.

## 12. Status meanings

| Status | Meaning |
|---|---|
| Approved Reference | 아트 스타일 레퍼런스로 승인됨. 게임에 넣었다는 뜻이 아님 |
| In Review | 생성됨. 사용자 검수 전 |
| Planned | 아직 없음 |
| Approved | 프로덕션 에셋으로 승인. Unity 미적용 |
| Not adopted | 후보였으나 채택하지 않음. 파일 유지 |
| Final | 프로덕션 아트 확정, 아직 미적용일 수 있음 |
| Integrated | Unity에 연결되어 사용 중 |

## 13. Job checklist

생성 요청을 받으면 실행 전에:

1. 기존 에셋 재사용 가능한가?
2. 독립 Sprite가 필요한가, 통합으로 충분한가? (이유)
3. 레퍼런스 우선순위가 프롬프트에 반영됐는가?
4. dry_run CU 표를 보고하고 승인을 받았는가?
5. 시안 추출·로컬 픽셀 편집 계획이 없는가?
6. 텍스트를 이미지에 굽지 않는가?
7. 품질 미달 시 재시도 한도와 보고 계획이 있는가?
8. Unity 적용은 별도 승인 게이트인가?
