# Candy Thief — Art Style Guide

- Version: 0.2 (2026-11-09)
- Status: Official visual style approved
- Target: mobile portrait, 9:16; design reference 1080 × 1920
- Official references: `Docs/Art/References/REF-*.png`

## 1. Art direction

Candy Thief의 공식 아트 스타일은 한국 캐주얼 모바일 게임풍의 키치한 2D 카툰이다.

- 크고 둥근 SD 캐릭터
- 굵고 일정한 짙은 갈색 외곽선
- 선명하고 명확한 색상 구분
- 단순한 기하학적 형태
- 최소한의 셀 셰이딩
- 과장된 표정과 코믹한 상황 연출
- 작은 화면에서도 읽히는 실루엣
- 귀엽지만 지나치게 여성스럽거나 장식적이지 않은 디자인

도둑은 초보 실패자이지 멋진 범죄자가 아니다. 귀여움의 핵심은 세련되거나 잘생긴 외모가 아니라, **멍청하고 하찮은 표정과 우스꽝스러운 행동**이다.

핑크·민트·바닐라 계열은 유지할 수 있으나 UI 전체가 분홍색으로 치우치면 안 된다. 하트, 리본, 과도한 스파클, 글로시 그라데이션은 피한다.

## 2. Official references

파일명 = AssetID. 이미지 내용으로 다른 ID를 부여하지 않는다.

| AssetID | File | Use |
|---|---|---|
| REF-ACT-001 | `Docs/Art/References/REF-ACT-001.png` | 경비를 피해 탈출 |
| REF-ACT-002 | `Docs/Art/References/REF-ACT-002.png` | 경비에게 발각 |
| REF-ACT-003 | `Docs/Art/References/REF-ACT-003.png` | 사탕 대량 획득 |
| REF-ACT-004 | `Docs/Art/References/REF-ACT-004.png` | 경비를 따돌림 |
| REF-ACT-005 | `Docs/Art/References/REF-ACT-005.png` | 경비에게 붙잡힘 |
| REF-BG-001 | `Docs/Art/References/REF-BG-001.png` | 사탕 가게 뒷골목 |
| REF-UI-001 | `Docs/Art/References/REF-UI-001.png` | 인게임 HUD 시각 기준 |
| REF-UI-002 | `Docs/Art/References/REF-UI-002.png` | 성공 팝업 시각 기준 |
| REF-UI-003 | `Docs/Art/References/REF-UI-003.png` | 로비 시각 기준 |

이 9종이 현재 공식 스타일 소스다. 생성 전에 관련 파일을 연다. 파일이 없으면 비슷한 이미지로 대체하지 말고 사용자에게 알린다.

2026-11-05 Title A/B, Lobby A/B/C 방향은 역사적 메모로 남긴다. 해당 마스터 파일(`REF_TITLE_AB.png` 등)은 이 폴더에 없다. 신규 생성은 위 9종을 우선한다.

**UI 레퍼런스는 시각 기준이지 기획·기능 명세가 아니다.** 이미지에 보이는 수치, 버튼, 재화, 시스템(에너지, 골드, 가챠, `경계도` 표기, 사탕 3/15 등)을 게임에 임의로 넣지 않는다. 실제 UI 기능과 데이터는 GDD, TECH_SPEC, DECISIONS, 현재 구현이 우선한다.

## 3. Thief character

주요 참조: `REF-ACT-003`, `REF-ACT-004`. 경비와 같이 나올 때는 `REF-ACT-002`, `REF-ACT-005`.

고정 디자인:

- 매우 크고 동그란 머리. **머리가 전체 실루엣에서 압도적 비중**
- 극단적으로 짧고 통통한 몸통
- 짧고 뭉툭한 팔다리
- 목이 거의 보이지 않음
- 빨간색 줄무늬 비니
- 검은색 눈가 마스크
- 검정·흰색 줄무늬 상의
- 짙은 갈색 하의
- 크고 동그란 눈
- 단순하고 과장된 표정

하지 말 것:

- 일반 인간 비율, 길쭉한 치비, 긴 팔다리, 날씬한 몸통
- 잘생기거나 세련된 얼굴
- 비니·마스크·줄무늬 실루엣을 깨는 의상

로비 기본 연출(GDD): 플레이어 혼자. 동료 슬롯은 배정 전까지 빈 `+`. 배경에 동료를 상시 그리지 않는다. 아이들 모션은 어색한 흔들림, 작은 바운스, 깜빡임, 고개 기울임. 정지 PNG 자체가 애니메이션이 되지는 않는다.

## 4. Guard character

주요 참조: `REF-ACT-002`, `REF-ACT-004`, `REF-ACT-005`.

- 도둑보다 크고 묵직한 체형
- 파란색 경비 제복과 모자
- 짙은 벨트와 넥타이
- 두꺼운 외곽선
- 화난 눈매와 과장된 표정
- 도둑과 같은 2D 카툰 스타일

위협적이되 무섭기보다 우스꽝스럽게. 사실적 경찰·호러 연출은 쓰지 않는다.

## 5. Environment

기준: `REF-BG-001`.

- 따뜻한 색감의 사탕 가게와 골목
- 단순화된 건물과 소품
- 둥글고 명확한 외곽선
- 캐릭터보다 낮은 시각적 강조
- 과도하게 사실적인 질감과 복잡한 디테일 배제

로비 배경(GDD): 허름하고 비어 보이는 은신처. 보물 더미, 금괴, 호화 가구, “세계 최고의 사탕 도둑이 되자” 포스터는 그리지 않는다.

## 6. Color and UI look

작업용 팔레트(샘플이 아닌 출발점): pink `#E98BA8`, sky `#91C8F0`, vanilla `#FFE4B0`, red `#D85B65`, outline `#342A35` 또는 레퍼런스의 짙은 갈색 외곽선, charcoal `#41404C`.

UI 시각 기준:

- `REF-UI-001`: 인게임 HUD 및 선택지
- `REF-UI-002`: 성공 결과 팝업
- `REF-UI-003`: 로비

공통: 굵은 외곽선, 명확한 버튼 실루엣, 큰 숫자, 상태별 색, 분명한 버튼 위계, 모바일 세로 구성, 캐릭터·일러스트와 조화.

텍스트와 조작 컨트롤은 Unity UI(TextMeshPro 등)로 만들고 배경에 굽지 않는다.

## 7. Screen layout (GDD, not the PNG)

스플래시/타이틀: 로고 `CANDY THIEF`. START/설정 버튼을 이미지에 넣지 않는다. 최소 1초 표시 후 로비 로딩이 끝나면 전환.

로비 기능 레이아웃은 GDD/D-012: 상단 프로필·에너지·골드 등은 **플레이스홀더 위젯**이며 탭 시 “준비 중”. `REF-UI-003`을 이유로 가챠·재화 시스템을 구현하지 않는다. CTA는 `도둑질 시작!` → 가게 선택.

런 HUD 기능 레이아웃은 GDD/M1-8: 상단 남은 턴·위험도·사탕, 하단 선택지. `REF-UI-001`의 `경계도` 문구와 사탕 `3/15` 같은 숫자는 카피·밸런스가 아니다. 플레이어 노출 라벨은 `위험도`이며 Alert 규칙은 바꾸지 않는다.

## 8. Asset production rules

1. 탐색 요청 시 개념 3안(A/B/C). 하나를 고른 뒤 변경 범위만 명시하고 나머지를 보존한다.
2. 제작 에셋은 분리한다: UI 없는 배경, 투명 캐릭터, 아이콘, 9-slice 버튼/패널.
3. 생성 결과는 검수 전까지 레퍼런스다. 기하·레이어 추출이 완벽하다고 가정하지 않는다.
4. 투명 배경, 실루엣, 가독성, 피벗은 Unity에서 확인한다.
5. 승인된 항목만 `ART_ASSET_LIST.md`에 기록한다. 신규 ID는 기존 ID와 겹치지 않게 한다.
6. Scenario 생성과 Unity 적용은 기본적으로 별도 작업이다. 사용자가 한 요청으로 묶지 않으면 연속 실행하지 않는다.
7. 운영 절차는 `Docs/Art/SCENARIO_PIPELINE.md`.

## 9. Standard prompt footer

> Follow `ART_STYLE_GUIDE.md` and the official files in `Docs/Art/References/` (`REF-ACT-*`, `REF-BG-001`, `REF-UI-*`). Korean casual-mobile kitsch 2D cartoon. Extreme SD: huge round head dominating the silhouette, tiny chubby body, stubby limbs, almost no neck. Thief: red striped beanie, black eye mask, black-and-white striped shirt, dark brown pants, goofy dumb expression — not handsome, not slender. Guard: larger/heavier, blue uniform and cap, comic angry face. Thick even dark-brown outlines, simple shapes, minimal cell shading. Mobile portrait 9:16. Do not bake UI text or extra game systems into the image. Do not add lobby companions unless assigned. Avoid posters, wealth piles, pink overload, realistic textures, long-limbed chibi, or adult proportions. State which existing elements must remain unchanged.

## 10. Change control

- 이 가이드는 사용자가 아트 방향을 승인한 뒤에만 바꾼다.
- 공식 레퍼런스 파일이 바뀌면 버전·날짜와 `ART_ASSET_LIST.md`를 같이 갱신한다.
- 현재 게임에 들어 있는 M1-8.5 치비 리액션(`ART-FB-*`, `ART-RCT-CAPTURE-001`)은 **Integrated 구현물**이다. 공식 스타일과 다를 수 있다. Unity 교체는 별도 승인 작업이다.

## 11. Scenario generation

생성 전: 목적 확인 → 기존 에셋 재사용 여부 → 관련 `REF-*` 선택 → 이 가이드 적용 → 모델·예상 CU 확인 → **사용자 승인**.

로컬 경로를 Scenario 이미지 ID처럼 넣지 않는다. 확인된 Scenario Asset ID가 있으면 재사용하고, 없으면 필요한 파일만 업로드한 뒤 반환 ID를 기록한다.

상세 절차: `Docs/Art/SCENARIO_PIPELINE.md`.
