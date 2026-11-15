using System.IO;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

// PLAY 화면 프리팹 (M4-6, PLAY-UI-MOCK-v001 기준, 1080×1920).
// 기존 RunHud.prefab / ChoiceButton.prefab 파일과 GUID 를 유지하고 컨트롤러(V02RunHud / V02ChoiceButton)는 그대로 둔다.
// Visual 하위 계층만 비우고 다시 만든 뒤 직렬화 참조를 다시 연결한다.
// ResultPresentation 과 ForecastLayer(사탕·위험도 툴팁)는 지우지 않고 유지하며 툴팁은 스타일만 맞춘다.
// 메뉴 Rebuild 는 프리팹을 이 코드의 배치로 되돌리므로 확인 창을 거친다. 평소에는 Edit Mode 에서 프리팹을 열어 직접 고친다 (D-019).
// 자동으로 호출되지 않는다. V02RunHud.layoutVersion 이 CurrentLayout 과 같은지로 최신 여부를 확인한다.
public static class V02RunHudPrefabBuilder
{
    const string Folder = "Assets/Resources/V02/UI";
    const string ButtonPath = Folder + "/ChoiceButton.prefab";
    const string HudPath = Folder + "/RunHud.prefab";

    const string ArtFolder = "Assets/Art/V02/UI/";
    const string ShapeFolder = "Assets/Art/V02/UI/Generated";
    const string BurstPath = ShapeFolder + "/CT_UI_Shape_Burst.png";
    const string BannerPath = ShapeFolder + "/CT_UI_Shape_Banner.png";
    const string RingPath = ShapeFolder + "/CT_UI_Shape_Ring.png";
    const float HudShadowOffset = 9f, HudShadowAlpha = 0.38f;
    static readonly Color PanelRing = new Color(0.47f, 0.31f, 0.21f, 1f);
    static readonly Color ZoneSafeDim = new Color(0.20f, 0.42f, 0.27f, 1f);
    static readonly Color ZoneCautionDim = new Color(0.52f, 0.42f, 0.14f, 1f);
    static readonly Color ZoneDangerDim = new Color(0.52f, 0.17f, 0.17f, 1f);
    const float BannerTip = 36f;
    const float BannerHeight = 64f;

    // 1080×1920 기준 픽셀 배치(좌상단 원점). 앵커로 변환해 저장하므로 Edit Mode 와 Play Mode 가 같다.
    static readonly Vector2 ScreenSize = new Vector2(1080f, 1920f);
    static readonly Vector2 CardSize = new Vector2(V02ChoiceButton.SlotWidth, V02ChoiceButton.SlotHeight);

    static readonly Rect PhaseRect = new Rect(300f, 16f, 480f, 83f);
    static readonly Rect TopRect = new Rect(18f, 112f, 1044f, 155f);
    static readonly Rect TurnChipRect = new Rect(0f, 0f, 270f, 155f);
    static readonly Rect AlertChipRect = new Rect(286f, 0f, 424f, 155f);
    static readonly Rect CandyChipRect = new Rect(726f, 0f, 318f, 155f);
    static readonly Rect CenterRect = new Rect(24f, 284f, 1032f, 728f);
    static readonly Rect CaptionRect = new Rect(14f, 636f, 1004f, 74f);
    // 타이머: 아랫변 y=1082 는 시안과 같고 높이만 78 → 56 으로 줄였다. 위쪽 경계만 아래로 옮겼다.
    static readonly Rect TimerRect = new Rect(24f, 1026f, 1032f, 56f);
    // 외곽선 두께(1080x1920 기준 px). 시안 실측: 주요 패널·카드 약 9~10, 문구·설명류 5~8, 칩 약 6.
    const float CardLine = 10f, PhaseLine = 10f, CenterLine = 10f, CaptionLine = 8f, TimerLine = 9f;
    const float HudChipLine = 9f, ExecuteLine = 9f, EscapeLine = 9f, BackLine = 6f, InfoChipLine = 6f, DescPanelLine = 5f;

    static readonly Rect ScrollArea = new Rect(0f, 1076f, 1080f, 669f);
    static readonly Rect EscapeRect = new Rect(306f, 1737f, 468f, 116f);

    static readonly Color Ink = new Color(0.16f, 0.08f, 0.045f, 1f);
    static readonly Color Background = new Color(0.969f, 0.914f, 0.761f, 1f);
    static readonly Color PhasePlate = new Color(0.267f, 0.169f, 0.122f, 1f);
    static readonly Color BackButton = new Color(0.50f, 0.32f, 0.235f, 1f);
    static readonly Color Mint = new Color(0.306f, 0.792f, 0.541f, 1f);
    static readonly Color Yellow = new Color(0.99f, 0.80f, 0.27f, 1f);
    static readonly Color CandyPill = new Color(0.27f, 0.70f, 0.47f, 1f);
    static readonly Color AlertPill = new Color(0.941f, 0.349f, 0.353f, 1f);
    static readonly Color StarGreen = new Color(0.20f, 0.62f, 0.34f, 1f);
    static readonly Color CardFace = new Color(0.98f, 0.933f, 0.804f, 1f);
    static readonly Color Tan = new Color(0.91f, 0.80f, 0.62f, 1f);
    static readonly Color ArtLine = new Color(0.62f, 0.45f, 0.30f, 1f);
    static readonly Color NameBrown = new Color(0.30f, 0.185f, 0.135f, 1f);
    static readonly Color DescFill = new Color(0.902f, 0.812f, 0.655f, 1f);
    static readonly Color DescLine = new Color(0.82f, 0.70f, 0.52f, 1f);
    static readonly Color ExecuteYellow = new Color(1f, 0.776f, 0.224f, 1f);
    static readonly Color TrackRim = new Color(0.42f, 0.28f, 0.20f, 1f);
    static readonly Color TrackDark = new Color(0.20f, 0.115f, 0.08f, 1f);
    static readonly Color TimerCream = new Color(0.97f, 0.925f, 0.78f, 1f);
    static readonly Color TimerGreen = new Color(0.29f, 0.71f, 0.53f, 1f);
    static readonly Color CaptionFill = new Color(0.98f, 0.93f, 0.77f, 1f);
    static readonly Color EscapeBrown = new Color(0.44f, 0.27f, 0.18f, 1f);
    static readonly Color PlaceholderArt = new Color(0.93f, 0.86f, 0.78f, 1f);
    static readonly Color DarkText = new Color(0.23f, 0.135f, 0.085f, 1f);
    static readonly Color ShadowTint = new Color(0.2f, 0.1f, 0.05f, 0.3f);

    [MenuItem("Candy Thief/V02/Rebuild Run HUD (PLAY UI v001)")]
    static void RebuildFromMenu()
    {
        if (!EditorUtility.DisplayDialog("PLAY 화면 프리팹 재생성",
                "RunHud / ChoiceButton 의 Visual 계층을 코드 배치(PLAY-UI-MOCK-v001)로 다시 만듭니다.\nInspector 에서 직접 고친 배치와 스타일은 사라집니다. 계속할까요?",
                "다시 만들기", "취소"))
            return;
        Rebuild();
    }

    public static void Rebuild()
    {
        EnsureFolders();
        V02OperationPrepArt.EnsureAssets();
        EnsureShapes();
        RebuildChoiceButton();
        AssetDatabase.SaveAssets();
        AssetDatabase.ImportAsset(ButtonPath);
        RebuildHud();
        AssetDatabase.SaveAssets();
        AssetDatabase.ImportAsset(HudPath);
        Debug.Log("[V02] PLAY 프리팹 저장: " + HudPath + " / " + ButtonPath + " (layout " + V02RunHud.CurrentLayout + ")");
    }

    static void EnsureFolders()
    {
        if (!AssetDatabase.IsValidFolder("Assets/Resources"))
            AssetDatabase.CreateFolder("Assets", "Resources");
        if (!AssetDatabase.IsValidFolder("Assets/Resources/V02"))
            AssetDatabase.CreateFolder("Assets/Resources", "V02");
        if (!AssetDatabase.IsValidFolder(Folder))
            AssetDatabase.CreateFolder("Assets/Resources/V02", "UI");
    }

    // ───────── 카드 (ChoiceButton) ─────────

    static void RebuildChoiceButton()
    {
        bool fresh = AssetDatabase.LoadAssetAtPath<GameObject>(ButtonPath) == null;
        GameObject root = fresh
            ? new GameObject("ChoiceButton", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button), typeof(CanvasGroup), typeof(LayoutElement), typeof(V02ChoiceButton))
            : PrefabUtility.LoadPrefabContents(ButtonPath);
        ClearChildren(root.transform);

        // 예전 세로 레이아웃 컴포넌트는 쓰지 않는다. 자식은 전부 앵커 배치다.
        RemoveComponent<VerticalLayoutGroup>(root);
        RemoveComponent<ContentSizeFitter>(root);

        RectTransform rootRect = (RectTransform)root.transform;
        rootRect.anchorMin = new Vector2(0.5f, 0.5f);
        rootRect.anchorMax = new Vector2(0.5f, 0.5f);
        rootRect.pivot = new Vector2(0.5f, 0.5f);
        rootRect.sizeDelta = CardSize;
        rootRect.localScale = Vector3.one;

        // 루트 Image 는 보이지 않는 터치 영역이다. 외곽선·그림자는 자식 레이어가 그린다.
        Ensure<CanvasRenderer>(root).cullTransparentMesh = false;
        Image hit = Ensure<Image>(root);
        hit.sprite = null;
        hit.color = new Color(0f, 0f, 0f, 0f);
        hit.raycastTarget = true;
        Button button = Ensure<Button>(root);
        button.targetGraphic = hit;
        button.transition = Selectable.Transition.None;
        CanvasGroup group = Ensure<CanvasGroup>(root);
        LayoutElement layout = Ensure<LayoutElement>(root);
        layout.ignoreLayout = false;
        layout.minWidth = CardSize.x;
        layout.minHeight = CardSize.y;
        layout.preferredWidth = CardSize.x;
        layout.preferredHeight = CardSize.y;
        layout.flexibleWidth = 0f;
        layout.flexibleHeight = 0f;
        V02ChoiceButton view = Ensure<V02ChoiceButton>(root);

        Transform r = root.transform;
        Rect card = new Rect(0f, 0f, CardSize.x, CardSize.y);
        SoftShadow(r, CardSize, card, 2, 8f, 0.38f);
        NewImage("Outline", r, CardSize, card, 36f, Ink);
        NewImage("Face", r, CardSize, new Rect(CardLine, CardLine, CardSize.x - CardLine * 2f, CardSize.y - CardLine * 2f), 26f, CardFace);
        NewImage("InnerLine", r, CardSize, new Rect(CardLine + 7f, CardLine + 7f, CardSize.x - (CardLine + 7f) * 2f, CardSize.y - (CardLine + 7f) * 2f), 20f, Tan);
        NewImage("InnerFace", r, CardSize, new Rect(CardLine + 10f, CardLine + 10f, CardSize.x - (CardLine + 10f) * 2f, CardSize.y - (CardLine + 10f) * 2f), 17f, CardFace);

        // 일러스트: 행동별 그림은 아직 없다. 공통 플레이스홀더 면이고 Background 의 sprite 로 교체할 수 있다.
        Rect artRect = new Rect(27f, 30f, 453f, 208f);
        RectTransform artFrame = Plate("ArtFrame", r, CardSize, artRect, 18f, ArtLine, 3f, out Image artMaskImage);
        artMaskImage.gameObject.name = "ArtMask";
        artMaskImage.color = PlaceholderArt;
        Mask artMask = artMaskImage.gameObject.AddComponent<Mask>();
        artMask.showMaskGraphic = true;
        Vector2 artInner = new Vector2(artRect.width - 6f, artRect.height - 6f);
        RectTransform background = V02Ui.CreateStretch("Background", artMaskImage.transform);
        Image art = background.gameObject.AddComponent<Image>();
        art.color = PlaceholderArt;
        art.raycastTarget = false;
        Image typeIcon = NewImage("TypeIcon", artMaskImage.transform, artInner, new Rect(108f, 52f, 84f, 84f), 0f, Color.white);
        typeIcon.sprite = LoadSprite(ArtFolder + "CT_Hud_Candy_v001.png");
        typeIcon.preserveAspect = true;

        // 이름 판: 양 끝이 뾰족한 갈색 띠
        Rect nameRect = new Rect(28f, 224f, 451f, 74f);
        RectTransform namePlate = BannerPlate("NamePlate", r, CardSize, nameRect, Ink, NameBrown, 7f);
        TextMeshProUGUI nameText = NewText("Name", namePlate, nameRect.size, new Rect(40f, 5f, nameRect.width - 80f, nameRect.height - 12f), "행동 이름", 46f, Color.white, TextAlignmentOptions.Center, true);
        AutoSize(nameText, 24f, 46f, true);

        // 성공 확률 배지: 별 모양 초록 면
        Sprite burst = AssetDatabase.LoadAssetAtPath<Sprite>(BurstPath);
        Image badgeOutline = NewSprite("BadgeOutline", r, CardSize, new Rect(332f, 18f, 160f, 160f), burst, Ink);
        Image badgeFill = NewSprite("BadgeFill", r, CardSize, new Rect(341f, 27f, 142f, 142f), burst, StarGreen);
        NewText("BadgeLabel", r, CardSize, new Rect(344f, 52f, 136f, 32f), "성공확률", 24f, Color.white, TextAlignmentOptions.Center, true);
        TextMeshProUGUI chance = NewText("Chance", r, CardSize, new Rect(344f, 80f, 136f, 58f), "90%", 50f, Color.white, TextAlignmentOptions.Center, true);
        AutoSize(chance, 28f, 52f, false);

        // 정보 칩 3개: 사탕(민트) / 소모 턴(노랑) / 위험도(코랄). 사탕·위험도 칩은 전체가 버튼이다.
        Button candyInfo = BuildInfoChip("CandyInfo", r, new Rect(28f, 316f, 144f, 60f), CandyPill, ArtFolder + "CT_Hud_Candy_v001.png", out TextMeshProUGUI successText);
        RectTransform costChip = Plate("CostChip", r, CardSize, new Rect(184f, 316f, 150f, 60f), 24f, Ink, InfoChipLine, out Image costFill);
        costFill.color = Yellow;
        Image costIcon = NewImage("CostIcon", costChip, new Vector2(150f, 60f), new Rect(8f, 6f, 46f, 48f), 0f, Color.white);
        costIcon.sprite = LoadSprite(ArtFolder + "CT_Hud_Hourglass_v003.png");
        costIcon.preserveAspect = true;
        TextMeshProUGUI costText = NewText("Cost", costChip, new Vector2(150f, 60f), new Rect(56f, 4f, 88f, 50f), "2턴", 36f, DarkText, TextAlignmentOptions.Center, false);
        AutoSize(costText, 22f, 38f, false);
        Button alertInfo = BuildInfoChip("AlertInfo", r, new Rect(346f, 316f, 133f, 60f), AlertPill, ArtFolder + "CT_Hud_Alert_v002.png", out TextMeshProUGUI failText);

        // 설명 패널
        Rect descRect = new Rect(28f, 388f, 451f, 72f);
        RectTransform descPanel = Plate("DescPanel", r, CardSize, descRect, 20f, DescLine, DescPanelLine, out Image descFill);
        descFill.color = DescFill;
        TextMeshProUGUI description = NewText("Description", descFill.transform, new Vector2(descRect.width - DescPanelLine * 2f, descRect.height - DescPanelLine * 2f), new Rect(10f, 0f, descRect.width - DescPanelLine * 2f - 20f, descRect.height - DescPanelLine * 2f), "행동 설명", 30f, DarkText, TextAlignmentOptions.Center, false);
        AutoSize(description, 20f, 30f, true);

        // 실행하기 버튼. 카드 전체가 눌리므로 이 판은 모양과 색만 그린다. 사유 글자는 Fill 의 자식이라 Fill 색이 상태색이다.
        Rect executeRect = new Rect(28f, 474f, 451f, 116f);
        SoftShadow(r, CardSize, executeRect, 2, 5f, 0.3f);
        RectTransform execute = Plate("ExecutePlate", r, CardSize, executeRect, 30f, Ink, ExecuteLine, out Image executeFill);
        executeFill.color = ExecuteYellow;
        TextMeshProUGUI disabled = NewText("DisabledReason", executeFill.transform, new Vector2(executeRect.width - ExecuteLine * 2f, executeRect.height - ExecuteLine * 2f), new Rect(10f, 0f, executeRect.width - ExecuteLine * 2f - 20f, executeRect.height - ExecuteLine * 2f - 4f), V02ChoiceButton.ExecuteLabel, 64f, Color.white, TextAlignmentOptions.Center, true);
        AutoSize(disabled, 22f, 64f, true);

        // 정보 칩은 위에 올라와야 눌린다.
        candyInfo.transform.SetAsLastSibling();
        alertInfo.transform.SetAsLastSibling();

        SerializedObject so = new SerializedObject(view);
        so.FindProperty("button").objectReferenceValue = button;
        so.FindProperty("canvasGroup").objectReferenceValue = group;
        so.FindProperty("typeIcon").objectReferenceValue = typeIcon;
        so.FindProperty("costIcon").objectReferenceValue = costIcon;
        so.FindProperty("nameText").objectReferenceValue = nameText;
        so.FindProperty("descriptionText").objectReferenceValue = description;
        so.FindProperty("chanceText").objectReferenceValue = chance;
        so.FindProperty("costText").objectReferenceValue = costText;
        so.FindProperty("successText").objectReferenceValue = successText;
        so.FindProperty("failText").objectReferenceValue = failText;
        so.FindProperty("disabledReasonText").objectReferenceValue = disabled;
        so.FindProperty("candyInfoButton").objectReferenceValue = candyInfo;
        so.FindProperty("alertInfoButton").objectReferenceValue = alertInfo;
        so.ApplyModifiedPropertiesWithoutUndo();

        successText.text = "+100";
        failText.text = "+5";
        Save(root, ButtonPath, fresh);
    }

    // 사탕 / 위험도 정보 칩. 컨테이너 Image 가 터치 영역이고 아이콘·글자는 Raycast 를 막지 않는다.
    static Button BuildInfoChip(string name, Transform parent, Rect rect, Color color, string iconPath, out TextMeshProUGUI value)
    {
        RectTransform chip = Plate(name, parent, CardSize, rect, 24f, Ink, InfoChipLine, out Image fill);
        fill.color = color;
        Image hit = chip.GetComponent<Image>();
        hit.raycastTarget = true;
        Button button = chip.gameObject.AddComponent<Button>();
        button.targetGraphic = hit;
        button.transition = Selectable.Transition.None;
        Image icon = NewImage("Icon", chip, rect.size, new Rect(9f, 8f, 44f, 44f), 0f, Color.white);
        icon.sprite = LoadSprite(iconPath);
        icon.preserveAspect = true;
        value = NewText("Value", chip, rect.size, new Rect(54f, 4f, rect.width - 60f, rect.height - 10f), "+0", 36f, Color.white, TextAlignmentOptions.Center, true);
        AutoSize(value, 20f, 38f, false);
        return button;
    }

    // ───────── 화면 (RunHud) ─────────

    static void RebuildHud()
    {
        bool fresh = AssetDatabase.LoadAssetAtPath<GameObject>(HudPath) == null;
        GameObject root = fresh
            ? new GameObject("RunHud", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(CanvasGroup), typeof(V02RunHud))
            : PrefabUtility.LoadPrefabContents(HudPath);

        // 연출(ResultPresentation)과 툴팁(ForecastLayer)은 유지하고 나머지 Visual 만 지운다.
        Transform presentation = root.transform.Find("ResultPresentation");
        Transform forecast = root.transform.Find("ForecastLayer");
        for (int i = root.transform.childCount - 1; i >= 0; i--)
        {
            Transform child = root.transform.GetChild(i);
            if (child != presentation && child != forecast)
                Object.DestroyImmediate(child.gameObject);
        }

        RectTransform rootRect = (RectTransform)root.transform;
        V02Ui.Stretch(rootRect);
        Image bg = Ensure<Image>(root);
        bg.sprite = null;
        bg.color = Background;
        bg.raycastTarget = true;
        Ensure<CanvasGroup>(root);
        V02RunHud hud = Ensure<V02RunHud>(root);
        Transform r = root.transform;

        // 상단 페이즈 바
        SoftShadow(r, ScreenSize, PhaseRect, 2, 5f, 0.3f);
        RectTransform phaseBar = Plate("PhaseBar", r, ScreenSize, PhaseRect, 34f, Ink, PhaseLine, out Image phaseFill);
        phaseFill.color = PhasePlate;
        Button back = BuildBackButton(phaseBar, PhaseRect.size);
        TextMeshProUGUI phaseLabel = NewText("PhaseLabel", phaseBar, PhaseRect.size, new Rect(92f, PhaseLine, PhaseRect.width - 104f, PhaseRect.height - PhaseLine * 2f - 2f), "사탕 훔치기", 54f, Color.white, TextAlignmentOptions.Center, true);
        AutoSize(phaseLabel, 36f, 54f, false);

        // 상단 칩 3개
        RectTransform top = NewRect("TopBar", r, ScreenSize, TopRect);
        Vector2 topSize = TopRect.size;
        BuildTurnChip(top, topSize, out Image turnChip, out Image turnIcon, out TextMeshProUGUI turnsText);
        BuildAlertChip(top, topSize, out Image alertChip, out Image alertIcon, out Image alertTrack, out Image alertFill, out TextMeshProUGUI alertText);
        BuildCandyChip(top, topSize, out Image candyChip, out Image candyIcon, out TextMeshProUGUI candyText);

        // 중앙 일러스트 프레임 + 상황 문구
        SoftShadow(r, ScreenSize, CenterRect, 1, 10f, 0.3f);
        RectTransform center = Plate("Center", r, ScreenSize, CenterRect, 38f, Ink, CenterLine, out Image mask);
        mask.gameObject.name = "IllustrationMask";
        mask.color = PlaceholderArt;
        Mask illustrationMask = mask.gameObject.AddComponent<Mask>();
        illustrationMask.showMaskGraphic = true;
        RectTransform illustrationRect = V02Ui.CreateStretch("Illustration", mask.transform);
        Image illustration = illustrationRect.gameObject.AddComponent<Image>();
        illustration.color = PlaceholderArt;
        illustration.raycastTarget = false;
        illustration.preserveAspect = false;
        AspectRatioFitter fit = illustrationRect.gameObject.AddComponent<AspectRatioFitter>();
        fit.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
        fit.aspectRatio = (CenterRect.width - CenterLine * 2f) / (CenterRect.height - CenterLine * 2f);

        RectTransform caption = Plate("Caption", center, CenterRect.size, CaptionRect, 24f, Ink, CaptionLine, out Image captionFill);
        captionFill.color = CaptionFill;
        TextMeshProUGUI body = NewText("Body", captionFill.transform, new Vector2(CaptionRect.width - CaptionLine * 2f, CaptionRect.height - CaptionLine * 2f), new Rect(16f, 0f, CaptionRect.width - CaptionLine * 2f - 32f, CaptionRect.height - CaptionLine * 2f), "사탕을 얼마나 가져갈지 정한다.", 38f, DarkText, TextAlignmentOptions.Center, false);
        AutoSize(body, 24f, 38f, true);
        caption.SetAsLastSibling();

        // 타이머: 얇은 바. 아랫변은 시안과 같다.
        // 그림자와 바를 TimerRoot 한 덩어리로 묶어 HUD 가 타이머를 숨길 때 그림자만 남지 않게 한다.
        RectTransform timerRoot = V02Ui.CreateStretch("TimerRoot", r);
        SoftShadow(timerRoot, ScreenSize, TimerRect, 2, 4f, 0.28f);
        RectTransform timerBar = Plate("TimerBar", timerRoot, ScreenSize, TimerRect, 28f, Ink, TimerLine, out Image timerTrack);
        timerTrack.gameObject.name = "TimerTrack";
        timerTrack.color = TimerCream;
        Mask timerMask = timerTrack.gameObject.AddComponent<Mask>();
        timerMask.showMaskGraphic = true;
        RectTransform timerFillRect = V02Ui.CreateStretch("TimerFill", timerTrack.transform);
        Image timerFill = timerFillRect.gameObject.AddComponent<Image>();
        timerFill.sprite = LoadSprite(ArtFolder + "CT_Hud_Fill_v001.png");
        timerFill.color = TimerGreen;
        timerFill.raycastTarget = false;
        timerFill.type = Image.Type.Filled;
        timerFill.fillMethod = Image.FillMethod.Horizontal;
        timerFill.fillOrigin = (int)Image.OriginHorizontal.Left;
        timerFill.fillAmount = 0.77f;
        TextMeshProUGUI timerSeconds = NewText("TimerSeconds", timerBar, TimerRect.size, new Rect(TimerRect.width - 180f, TimerLine, 158f, TimerRect.height - TimerLine * 2f), "23초", 34f, DarkText, TextAlignmentOptions.Right, false);
        AutoSize(timerSeconds, 22f, 34f, false);

        // 행동 카드 2장: 카드가 잘리거나 그림자가 잘리지 않도록 스크롤 영역에 여백을 둔다.
        var scrollGo = new GameObject("ChoiceScroll", typeof(RectTransform), typeof(CanvasGroup));
        scrollGo.transform.SetParent(r, false);
        RectTransform scrollRect = (RectTransform)scrollGo.transform;
        Place(scrollRect, ScreenSize, ScrollArea);
        ScrollRect scroll = scrollGo.AddComponent<ScrollRect>();
        scroll.horizontal = false;
        scroll.vertical = false;
        scroll.movementType = UnityEngine.UI.ScrollRect.MovementType.Elastic;
        RectTransform viewport = V02Ui.CreateStretch("Viewport", scrollRect);
        viewport.gameObject.AddComponent<RectMask2D>();
        scroll.viewport = viewport;
        RectTransform list = V02Ui.CreateStretch("ChoiceList", viewport);
        HorizontalLayoutGroup listLayout = list.gameObject.AddComponent<HorizontalLayoutGroup>();
        listLayout.padding = new RectOffset(24, 24, 20, 26);
        listLayout.spacing = 18f;
        listLayout.childAlignment = TextAnchor.UpperLeft;
        listLayout.childControlWidth = true;
        listLayout.childControlHeight = true;
        listLayout.childForceExpandWidth = false;
        listLayout.childForceExpandHeight = false;
        ContentSizeFitter fitter = list.gameObject.AddComponent<ContentSizeFitter>();
        fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
        fitter.verticalFit = ContentSizeFitter.FitMode.Unconstrained;
        list.pivot = new Vector2(0f, 0.5f);
        list.anchorMin = new Vector2(0f, 0f);
        list.anchorMax = new Vector2(0f, 1f);
        list.anchoredPosition = Vector2.zero;
        scroll.content = list;

        // Edit Mode 에서 카드 배치를 보기 위한 견본 2장. 실행 중에는 V02RunHud 가 이 카드를 그대로 재사용한다.
        GameObject cardAsset = AssetDatabase.LoadAssetAtPath<GameObject>(ButtonPath);
        for (int i = 0; i < 2; i++)
        {
            GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(cardAsset, list);
            instance.name = "Choice" + i;
        }

        // 탈출 준비하기
        SoftShadow(r, ScreenSize, EscapeRect, 2, 6f, 0.3f);
        RectTransform escape = Plate("Lobby", r, ScreenSize, EscapeRect, 40f, Ink, EscapeLine, out Image escapeFill);
        escapeFill.color = EscapeBrown;
        escape.GetComponent<Image>().raycastTarget = true;
        Button lobby = escape.gameObject.AddComponent<Button>();
        StyleButton(lobby, escapeFill, Color.white);
        Image door = NewImage("Icon", escape, EscapeRect.size, new Rect(22f, 16f, 80f, 80f), 0f, Color.white);
        door.sprite = LoadSprite(ArtFolder + "CT_Icon_Escape_v001.png");
        door.preserveAspect = true;
        TextMeshProUGUI escapeLabel = NewText("Label", escape, EscapeRect.size, new Rect(108f, 0f, EscapeRect.width - 128f, EscapeRect.height - 4f), "탈출 준비하기", 54f, Color.white, TextAlignmentOptions.Center, true);
        AutoSize(escapeLabel, 34f, 54f, false);

        // 연출과 툴팁은 항상 맨 위
        if (presentation == null)
            presentation = V02ResultPresentation.Ensure((RectTransform)root.transform).transform;
        if (forecast == null)
            forecast = BuildForecastLayer(root.transform);
        StyleForecast(forecast);
        // 결과 연출 막은 Edit Mode 에서 화면을 덮지 않도록 투명 + 입력 차단 해제로 저장한다. 런타임 Show 가 alpha 를 올린다.
        CanvasGroup presentationGroup = Ensure<CanvasGroup>(presentation.gameObject);
        presentationGroup.alpha = 0f;
        presentationGroup.blocksRaycasts = false;
        presentationGroup.interactable = false;
        presentation.SetAsLastSibling();
        forecast.SetAsLastSibling();

        SerializedObject so = new SerializedObject(hud);
        so.FindProperty("layoutVersion").intValue = V02RunHud.CurrentLayout;
        so.FindProperty("turnsText").objectReferenceValue = turnsText;
        so.FindProperty("alertText").objectReferenceValue = alertText;
        so.FindProperty("candyText").objectReferenceValue = candyText;
        so.FindProperty("illustration").objectReferenceValue = illustration;
        so.FindProperty("placeholderColor").colorValue = PlaceholderArt;
        so.FindProperty("titleText").objectReferenceValue = null;
        so.FindProperty("bodyText").objectReferenceValue = body;
        so.FindProperty("resultRoot").objectReferenceValue = null;
        so.FindProperty("resultStatusText").objectReferenceValue = null;
        so.FindProperty("resultMessageText").objectReferenceValue = null;
        so.FindProperty("resultDeltaText").objectReferenceValue = null;
        so.FindProperty("choiceList").objectReferenceValue = list;
        so.FindProperty("choiceScroll").objectReferenceValue = scroll;
        so.FindProperty("choiceCanvasGroup").objectReferenceValue = scrollGo.GetComponent<CanvasGroup>();
        so.FindProperty("choiceButtonPrefab").objectReferenceValue = cardAsset.GetComponent<V02ChoiceButton>();
        so.FindProperty("lobbyButton").objectReferenceValue = lobby;
        so.FindProperty("backButton").objectReferenceValue = back;
        so.FindProperty("resultPresentation").objectReferenceValue = presentation.GetComponent<V02ResultPresentation>();
        so.FindProperty("turnChip").objectReferenceValue = turnChip;
        so.FindProperty("turnIcon").objectReferenceValue = turnIcon;
        so.FindProperty("alertChip").objectReferenceValue = alertChip;
        so.FindProperty("alertIcon").objectReferenceValue = alertIcon;
        so.FindProperty("alertTrack").objectReferenceValue = alertTrack;
        so.FindProperty("alertFill").objectReferenceValue = alertFill;
        so.FindProperty("candyChip").objectReferenceValue = candyChip;
        so.FindProperty("candyIcon").objectReferenceValue = candyIcon;
        so.FindProperty("hourglassSprite").objectReferenceValue = LoadSprite(ArtFolder + "CT_Hud_Hourglass_v003.png");
        so.FindProperty("alertSprite").objectReferenceValue = LoadSprite(ArtFolder + "CT_Hud_Alert_v002.png");
        so.FindProperty("candySprite").objectReferenceValue = LoadSprite(ArtFolder + "CT_Hud_Candy_v001.png");
        so.FindProperty("moveIcon").objectReferenceValue = LoadSprite(ArtFolder + "CT_Icon_Move_v001.png");
        so.FindProperty("infiltrateIcon").objectReferenceValue = LoadSprite(ArtFolder + "CT_Icon_Infiltrate_v001.png");
        so.FindProperty("lootIcon").objectReferenceValue = LoadSprite(ArtFolder + "CT_Hud_Candy_v001.png");
        so.FindProperty("escapeIcon").objectReferenceValue = LoadSprite(ArtFolder + "CT_Icon_Escape_v001.png");
        so.FindProperty("eventIcon").objectReferenceValue = LoadSprite(ArtFolder + "CT_Icon_Event_v001.png");
        so.FindProperty("phaseText").objectReferenceValue = phaseLabel;
        so.FindProperty("timerRoot").objectReferenceValue = timerRoot.gameObject;
        so.FindProperty("timerFill").objectReferenceValue = timerFill;
        so.FindProperty("timerSecondsText").objectReferenceValue = timerSeconds;
        so.ApplyModifiedPropertiesWithoutUndo();
        AssignFeedbackSprites(presentation.GetComponent<V02ResultPresentation>());

        // 레이아웃이 만든 위치를 프리팹에 저장해 Edit Mode 와 Play Mode 가 같게 한다.
        Canvas.ForceUpdateCanvases();
        LayoutRebuilder.ForceRebuildLayoutImmediate(list);
        for (int i = 0; i < list.childCount; i++)
            EditorUtility.SetDirty(list.GetChild(i));

        Save(root, HudPath, fresh);
    }

    static Button BuildBackButton(Transform bar, Vector2 barSize)
    {
        Rect rect = new Rect(16f, 14f, 66f, 55f);
        RectTransform root = Plate("Back", bar, barSize, rect, 18f, Ink, BackLine, out Image fill);
        fill.color = BackButton;
        root.GetComponent<Image>().raycastTarget = true;
        Button button = root.gameObject.AddComponent<Button>();
        StyleButton(button, fill, new Color(0.7f, 0.65f, 0.6f, 1f));
        Image chevron = NewImage("Icon", root, rect.size, new Rect(17f, 8f, 32f, 39f), 0f, Color.white);
        chevron.sprite = LoadSprite(ArtFolder + "CT_UI_Icon_ChevronLeft_v001.png");
        chevron.preserveAspect = true;
        return button;
    }

    static void BuildTurnChip(Transform top, Vector2 topSize, out Image chip, out Image icon, out TextMeshProUGUI value)
    {
        SoftShadow(top, topSize, TurnChipRect, 2, HudShadowOffset, HudShadowAlpha);
        RectTransform root = Plate("TurnChip", top, topSize, TurnChipRect, 30f, Ink, HudChipLine, out chip);
        chip.color = Mint;
        Vector2 size = TurnChipRect.size;
        icon = NewImage("TurnChipIcon", root, size, new Rect(12f, 18f, 104f, 120f), 0f, Color.white);
        icon.sprite = LoadSprite(ArtFolder + "CT_Hud_Hourglass_v003.png");
        icon.preserveAspect = true;
        NewText("TurnChipCaption", root, size, new Rect(112f, 16f, 150f, 40f), "남은 턴", 30f, Color.white, TextAlignmentOptions.Center, true);
        value = NewText("TurnChipValue", root, size, new Rect(112f, 50f, 150f, 96f), "10", 88f, Color.white, TextAlignmentOptions.Center, true);
        AutoSize(value, 54f, 88f, false);
    }

    // 위험도 칩: 윗줄 = 아이콘 + 라벨 + 큰 수치(독립 영역), 아랫줄 = 게이지. 수치가 게이지와 겹치지 않는다.
    static void BuildAlertChip(Transform top, Vector2 topSize, out Image chip, out Image icon, out Image track, out Image fill, out TextMeshProUGUI value)
    {
        SoftShadow(top, topSize, AlertChipRect, 2, HudShadowOffset, HudShadowAlpha);
        RectTransform root = Plate("AlertChip", top, topSize, AlertChipRect, 30f, Ink, HudChipLine, out chip);
        chip.color = PhasePlate;
        Vector2 size = AlertChipRect.size;

        // 안쪽 밝은 갈색 선: 카드와 같은 이중 프레임. 칩 색(위험도 100 에서 바뀜)과 별개의 장식 층이다.
        Sprite ring = AssetDatabase.LoadAssetAtPath<Sprite>(RingPath);
        Image innerLine = NewSprite("InnerLine", root, size, new Rect(HudChipLine + 5f, HudChipLine + 5f, size.x - (HudChipLine + 5f) * 2f, size.y - (HudChipLine + 5f) * 2f), ring, PanelRing);
        innerLine.type = Image.Type.Sliced;
        innerLine.preserveAspect = false;
        innerLine.pixelsPerUnitMultiplier = 1.6f;

        icon = NewImage("AlertIcon", root, size, new Rect(26f, 14f, 58f, 56f), 0f, Color.white);
        icon.sprite = LoadSprite(ArtFolder + "CT_Hud_Alert_v002.png");
        icon.preserveAspect = true;
        NewText("AlertCaption", root, size, new Rect(92f, 18f, 120f, 48f), "위험도", 34f, Color.white, TextAlignmentOptions.Left, true);
        value = NewText("AlertValue", root, size, new Rect(196f, 16f, 200f, 52f), "45/100", 48f, Color.white, TextAlignmentOptions.Right, true);
        AutoSize(value, 28f, 50f, false);

        // 게이지: 굵은 테두리 + 어두운 3구간 바닥 + 위에 올라오는 선명한 채움. 채움 색·구간 임계값은 V02RunHud 가 정한다.
        const float rim = 7f;
        Rect rimRect = new Rect(26f, 74f, 372f, 62f);
        Rect trackRect = new Rect(rimRect.x + rim, rimRect.y + rim, rimRect.width - rim * 2f, rimRect.height - rim * 2f);
        NewImage("AlertTrackRim", root, size, rimRect, 28f, Ink);
        track = NewImage("AlertTrack", root, size, trackRect, 21f, TrackDark);
        track.gameObject.AddComponent<Mask>().showMaskGraphic = true;

        float safeEnd = trackRect.width * 0.34f;
        float cautionEnd = trackRect.width * 0.67f;
        NewImage("AlertZoneSafe", track.transform, trackRect.size, new Rect(0f, 0f, safeEnd, trackRect.height), 0f, ZoneSafeDim).raycastTarget = false;
        NewImage("AlertZoneCaution", track.transform, trackRect.size, new Rect(safeEnd, 0f, cautionEnd - safeEnd, trackRect.height), 0f, ZoneCautionDim).raycastTarget = false;
        NewImage("AlertZoneDanger", track.transform, trackRect.size, new Rect(cautionEnd, 0f, trackRect.width - cautionEnd, trackRect.height), 0f, ZoneDangerDim).raycastTarget = false;

        RectTransform fillRect = V02Ui.CreateStretch("AlertFill", track.transform);
        fillRect.offsetMin = Vector2.zero;
        fillRect.offsetMax = Vector2.zero;
        fill = fillRect.gameObject.AddComponent<Image>();
        fill.sprite = LoadSprite(ArtFolder + "CT_Hud_Fill_v001.png");
        fill.color = new Color(0.96f, 0.72f, 0.28f, 1f);
        fill.raycastTarget = false;
        fill.type = Image.Type.Filled;
        fill.fillMethod = Image.FillMethod.Horizontal;
        fill.fillOrigin = (int)Image.OriginHorizontal.Left;
        fill.fillAmount = 0.45f;

        // 구간 경계(0.34, 0.67): 시안의 칸 나눔처럼 굵은 갈색 선. 색 구간 규칙은 V02RunHud.AlertColor 와 같다.
        for (int i = 1; i <= 2; i++)
        {
            float x = (i == 1 ? safeEnd : cautionEnd) - 3f;
            Image tick = NewImage("AlertTick" + i, track.transform, trackRect.size, new Rect(x, 0f, 6f, trackRect.height), 0f, Ink);
            tick.raycastTarget = false;
        }
    }
    static void BuildCandyChip(Transform top, Vector2 topSize, out Image chip, out Image icon, out TextMeshProUGUI value)
    {
        SoftShadow(top, topSize, CandyChipRect, 2, HudShadowOffset, HudShadowAlpha);
        RectTransform root = Plate("CandyChip", top, topSize, CandyChipRect, 30f, Ink, HudChipLine, out chip);
        chip.color = Yellow;
        Vector2 size = CandyChipRect.size;
        icon = NewImage("CandyChipIcon", root, size, new Rect(14f, 26f, 100f, 100f), 0f, Color.white);
        icon.sprite = LoadSprite(ArtFolder + "CT_Hud_Candy_v001.png");
        icon.preserveAspect = true;
        NewText("CandyChipCaption", root, size, new Rect(116f, 16f, 190f, 40f), "사탕", 30f, Color.white, TextAlignmentOptions.Center, true);
        value = NewText("CandyChipValue", root, size, new Rect(116f, 50f, 190f, 96f), "100", 88f, Color.white, TextAlignmentOptions.Center, true);
        AutoSize(value, 40f, 88f, false);
    }

    // ───────── 툴팁 ─────────

    static Transform BuildForecastLayer(Transform root)
    {
        var layer = new GameObject("ForecastLayer", typeof(RectTransform));
        layer.transform.SetParent(root, false);
        V02Ui.Stretch((RectTransform)layer.transform);
        layer.SetActive(false);

        var dismiss = new GameObject("ForecastDismiss", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(V02ForecastTip));
        dismiss.transform.SetParent(layer.transform, false);
        V02Ui.Stretch((RectTransform)dismiss.transform);
        Image dismissImage = dismiss.GetComponent<Image>();
        dismissImage.color = new Color(0f, 0f, 0f, 0f);
        dismissImage.raycastTarget = true;

        var tooltip = new GameObject("ForecastTooltip", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        tooltip.transform.SetParent(layer.transform, false);
        TextMeshProUGUI text = V02Ui.CreateText("Text", tooltip.transform, string.Empty, 28f, DarkText, TextAlignmentOptions.Center);

        SerializedObject so = new SerializedObject(dismiss.GetComponent<V02ForecastTip>());
        so.FindProperty("layer").objectReferenceValue = layer;
        so.FindProperty("panel").objectReferenceValue = tooltip.transform;
        so.FindProperty("label").objectReferenceValue = text;
        so.ApplyModifiedPropertiesWithoutUndo();
        return layer.transform;
    }

    // 툴팁 판은 일러스트 프레임 아래쪽, 상황 문구 위에 둔다. 카드·타이머·상황 문구를 가리지 않는다.
    static void StyleForecast(Transform layer)
    {
        Transform tooltip = layer.Find("ForecastTooltip");
        if (tooltip == null)
            return;
        RectTransform tipRect = (RectTransform)tooltip;
        tipRect.anchorMin = new Vector2(0.08f, 0.535f);
        tipRect.anchorMax = new Vector2(0.92f, 0.598f);
        tipRect.offsetMin = Vector2.zero;
        tipRect.offsetMax = Vector2.zero;
        Vector2 size = new Vector2(ScreenSize.x * 0.84f, ScreenSize.y * 0.063f);

        Image outline = Ensure<Image>(tooltip.gameObject);
        outline.sprite = V02OperationPrepArt.RoundLarge();
        outline.type = Image.Type.Sliced;
        outline.pixelsPerUnitMultiplier = V02OperationPrepArt.LargeRadius / 26f;
        outline.color = Ink;
        outline.raycastTarget = true;

        Transform old = tooltip.Find("Fill");
        if (old != null)
            Object.DestroyImmediate(old.gameObject);
        Image fill = NewImage("Fill", tooltip, size, new Rect(5f, 5f, size.x - 10f, size.y - 10f), 21f, CardFace);
        fill.transform.SetAsFirstSibling();

        Transform text = tooltip.Find("Text");
        if (text != null)
        {
            RectTransform textRect = (RectTransform)text;
            textRect.anchorMin = new Vector2(0.04f, 0.1f);
            textRect.anchorMax = new Vector2(0.96f, 0.9f);
            textRect.offsetMin = Vector2.zero;
            textRect.offsetMax = Vector2.zero;
            TextMeshProUGUI label = text.GetComponent<TextMeshProUGUI>();
            if (label != null)
            {
                label.color = DarkText;
                label.alignment = TextAlignmentOptions.Center;
                label.raycastTarget = false;
                AutoSize(label, 22f, 34f, true);
            }
        }
    }

    static void AssignFeedbackSprites(V02ResultPresentation presentation)
    {
        if (presentation == null)
            return;
        presentation.BindSprites(
            new[] { LoadSprite("Assets/Art/V02/Feedback/CT_Feedback_Success_Jump_v001.png"), LoadSprite("Assets/Art/V02/Feedback/CT_Feedback_Success_Cheer_v001.png") },
            new[] { LoadSprite("Assets/Art/V02/Feedback/CT_Feedback_Fail_Flinch_v001.png"), LoadSprite("Assets/Art/V02/Feedback/CT_Feedback_Fail_Freeze_v001.png") },
            LoadSprite("Assets/Art/V02/Feedback/CT_Feedback_Sparkle_v001.png"),
            LoadSprite("Assets/Art/V02/Feedback/CT_Feedback_Candy_v001.png"),
            LoadSprite("Assets/Art/V02/Feedback/CT_Feedback_Bang_v001.png"),
            LoadSprite("Assets/Art/V02/Feedback/CT_Feedback_Sweat_v001.png"),
            LoadSprite("Assets/Art/V02/Feedback/CT_Feedback_Captured_v001.png"));
    }

    // ───────── 도형 리소스 (별 배지, 뾰족한 이름 판) ─────────
    // 단순 도형을 코드로 그린 흰색 PNG 이다. 완성 시안을 자르지 않는다. 색은 프리팹의 Image 가 정한다.

    static void EnsureShapes()
    {
        if (!AssetDatabase.IsValidFolder(ShapeFolder))
            AssetDatabase.CreateFolder("Assets/Art/V02/UI", "Generated");

        EnsureShapeFile(BurstPath, 256, 256, (x, y) =>
        {
            float half = 128f;
            float dx = x - half;
            float dy = y - half;
            float radius = Mathf.Sqrt(dx * dx + dy * dy);
            float phase = Mathf.Atan2(dy, dx) * 16f / (Mathf.PI * 2f);
            float f = phase - Mathf.Floor(phase);
            float k = 1f - Mathf.Abs(2f * f - 1f);
            k = k * k * (3f - 2f * k);
            return radius <= Mathf.Lerp(half * 0.83f, half * 0.97f, k);
        }, Vector4.zero);

        EnsureShapeFile(RingPath, 64, 64, (x, y) => InsideRound(x, y, 0f, 24f) && !InsideRound(x, y, 6f, 18f), new Vector4(28f, 28f, 28f, 28f));

        EnsureShapeFile(BannerPath, 128, (int)BannerHeight, (x, y) =>
        {
            float half = BannerHeight * 0.5f;
            float d = Mathf.Abs(y - half) / half;
            float edge = BannerTip * d;
            return y >= 0f && y <= BannerHeight && x >= edge && x <= 128f - edge;
        }, new Vector4(BannerTip, 0f, BannerTip, 0f));
    }

    // 64x64 안의 둥근 사각형. inset 만큼 안으로 줄인 사각형의 모서리 반경은 radius.
    static bool InsideRound(float x, float y, float inset, float radius)
    {
        float min = inset;
        float max = 64f - inset;
        if (x < min || x > max || y < min || y > max)
            return false;
        float cx = Mathf.Clamp(x, min + radius, max - radius);
        float cy = Mathf.Clamp(y, min + radius, max - radius);
        float dx = x - cx;
        float dy = y - cy;
        return dx * dx + dy * dy <= radius * radius;
    }

    static void EnsureShapeFile(string assetPath, int width, int height, System.Func<float, float, bool> inside, Vector4 border)
    {
        if (AssetDatabase.LoadAssetAtPath<Sprite>(assetPath) == null)
        {
            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
            const int samples = 4;
            var pixels = new Color32[width * height];
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    int hits = 0;
                    for (int sy = 0; sy < samples; sy++)
                    {
                        for (int sx = 0; sx < samples; sx++)
                        {
                            if (inside(x + (sx + 0.5f) / samples, y + (sy + 0.5f) / samples))
                                hits++;
                        }
                    }

                    byte alpha = (byte)Mathf.RoundToInt(255f * hits / (samples * samples));
                    pixels[y * width + x] = new Color32(255, 255, 255, alpha);
                }
            }

            texture.SetPixels32(pixels);
            texture.Apply();
            File.WriteAllBytes(Path.Combine(Directory.GetCurrentDirectory(), assetPath), texture.EncodeToPNG());
            Object.DestroyImmediate(texture);
            AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceSynchronousImport);
        }

        var importer = (TextureImporter)AssetImporter.GetAtPath(assetPath);
        if (importer == null)
            return;
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.spritePixelsPerUnit = 100f;
        importer.spriteBorder = border;
        importer.mipmapEnabled = false;
        importer.filterMode = FilterMode.Bilinear;
        importer.wrapMode = TextureWrapMode.Clamp;
        importer.alphaIsTransparency = true;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        var settings = new TextureImporterSettings();
        importer.ReadTextureSettings(settings);
        settings.spriteMeshType = SpriteMeshType.FullRect;
        importer.SetTextureSettings(settings);
        importer.SaveAndReimport();
    }

    // ───────── 도형 도우미 ─────────

    static void Save(GameObject root, string path, bool fresh)
    {
        PrefabUtility.SaveAsPrefabAsset(root, path);
        if (fresh)
            Object.DestroyImmediate(root);
        else
            PrefabUtility.UnloadPrefabContents(root);
    }

    static void ClearChildren(Transform root)
    {
        for (int i = root.childCount - 1; i >= 0; i--)
            Object.DestroyImmediate(root.GetChild(i).gameObject);
    }

    static T Ensure<T>(GameObject go) where T : Component
    {
        T component = go.GetComponent<T>();
        return component != null ? component : go.AddComponent<T>();
    }

    static void RemoveComponent<T>(GameObject go) where T : Component
    {
        T component = go.GetComponent<T>();
        if (component != null)
            Object.DestroyImmediate(component);
    }

    static Sprite LoadSprite(string path)
    {
        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }

    static void Place(RectTransform rect, Vector2 parent, Rect r)
    {
        rect.anchorMin = new Vector2(r.x / parent.x, 1f - r.yMax / parent.y);
        rect.anchorMax = new Vector2(r.xMax / parent.x, 1f - r.y / parent.y);
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        rect.localScale = Vector3.one;
    }

    static RectTransform NewRect(string name, Transform parent, Vector2 parentSize, Rect rect)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        RectTransform rt = (RectTransform)go.transform;
        Place(rt, parentSize, rect);
        return rt;
    }

    // 둥근 사각형 Image. radius 0 이면 직각. 코너 반경은 9-slice 배율로 맞춘다.
    static Image NewImage(string name, Transform parent, Vector2 parentSize, Rect rect, float radius, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        go.transform.SetParent(parent, false);
        Place((RectTransform)go.transform, parentSize, rect);
        Image image = go.GetComponent<Image>();
        image.color = color;
        image.raycastTarget = false;
        if (radius > 0f)
        {
            bool large = radius >= 28f;
            image.sprite = large ? V02OperationPrepArt.RoundLarge() : V02OperationPrepArt.RoundSmall();
            image.type = Image.Type.Sliced;
            image.pixelsPerUnitMultiplier = (large ? V02OperationPrepArt.LargeRadius : V02OperationPrepArt.SmallRadius) / radius;
        }

        return image;
    }

    static Image NewSprite(string name, Transform parent, Vector2 parentSize, Rect rect, Sprite sprite, Color color)
    {
        Image image = NewImage(name, parent, parentSize, rect, 0f, color);
        image.sprite = sprite;
        image.type = Image.Type.Simple;
        image.preserveAspect = true;
        return image;
    }

    // 외곽선 면(컨테이너) + 안쪽 면(Fill). 컨테이너 Image 가 외곽선 색이다.
    static RectTransform Plate(string name, Transform parent, Vector2 parentSize, Rect rect, float radius, Color outline, float line, out Image fill)
    {
        Image container = NewImage(name, parent, parentSize, rect, radius, outline);
        if (line <= 0f)
        {
            fill = container;
            return (RectTransform)container.transform;
        }

        fill = NewImage("Fill", container.transform, rect.size, new Rect(line, line, rect.width - line * 2f, rect.height - line * 2f), Mathf.Max(radius - line, 0f), Color.white);
        return (RectTransform)container.transform;
    }

    // 양 끝이 뾰족한 띠. 높이가 달라도 뾰족한 끝의 각도가 같도록 배율을 높이에 맞춘다.
    static RectTransform BannerPlate(string name, Transform parent, Vector2 parentSize, Rect rect, Color outline, Color fillColor, float line)
    {
        Sprite banner = AssetDatabase.LoadAssetAtPath<Sprite>(BannerPath);
        Image container = NewImage(name, parent, parentSize, rect, 0f, outline);
        StyleBanner(container, banner, rect.height);
        Image fill = NewImage("Fill", container.transform, rect.size, new Rect(line, line, rect.width - line * 2f, rect.height - line * 2f), 0f, fillColor);
        StyleBanner(fill, banner, rect.height - line * 2f);
        return (RectTransform)container.transform;
    }

    static void StyleBanner(Image image, Sprite banner, float height)
    {
        image.sprite = banner;
        image.type = Image.Type.Sliced;
        image.pixelsPerUnitMultiplier = BannerHeight / height;
    }

    // 독립 그림자 레이어. 요소 뒤(앞선 형제)에 둔다. k 는 블러 크기 배율(1 크게, 2 작게).
    static void SoftShadow(Transform parent, Vector2 parentSize, Rect element, int k, float offsetY, float alpha)
    {
        float spread = V02OperationPrepArt.ShadowInset / k;
        var rect = new Rect(element.x - spread, element.y - spread + offsetY, element.width + spread * 2f, element.height + spread * 2f);
        var go = new GameObject("Shadow", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        go.transform.SetParent(parent, false);
        Place((RectTransform)go.transform, parentSize, rect);
        Image image = go.GetComponent<Image>();
        image.sprite = V02OperationPrepArt.SoftShadow();
        image.type = Image.Type.Sliced;
        image.pixelsPerUnitMultiplier = k;
        image.color = new Color(ShadowTint.r, ShadowTint.g, ShadowTint.b, alpha);
        image.raycastTarget = false;
    }

    static TextMeshProUGUI NewText(string name, Transform parent, Vector2 parentSize, Rect rect, string content, float size, Color color, TextAlignmentOptions align, bool outlined)
    {
        TextMeshProUGUI label = V02Ui.CreateText(name, parent, content, size, color, align);
        Place(label.rectTransform, parentSize, rect);
        label.raycastTarget = false;
        label.textWrappingMode = TextWrappingModes.NoWrap;
        label.overflowMode = TextOverflowModes.Overflow;
        if (outlined)
        {
            Material outline = V02OperationPrepArt.OutlineMaterial();
            if (outline != null)
                label.fontSharedMaterial = outline;
        }

        return label;
    }

    static void AutoSize(TextMeshProUGUI label, float min, float max, bool wrap)
    {
        label.enableAutoSizing = true;
        label.fontSizeMin = min;
        label.fontSizeMax = max;
        label.textWrappingMode = wrap ? TextWrappingModes.Normal : TextWrappingModes.NoWrap;
        label.overflowMode = TextOverflowModes.Overflow;
    }

    static void StyleButton(Button button, Graphic target, Color disabled)
    {
        button.targetGraphic = target;
        button.transition = Selectable.Transition.ColorTint;
        ColorBlock colors = ColorBlock.defaultColorBlock;
        colors.normalColor = Color.white;
        colors.highlightedColor = Color.white;
        colors.selectedColor = Color.white;
        colors.pressedColor = new Color(0.82f, 0.82f, 0.82f, 1f);
        colors.disabledColor = disabled;
        colors.colorMultiplier = 1f;
        colors.fadeDuration = 0.08f;
        button.colors = colors;
    }
}
