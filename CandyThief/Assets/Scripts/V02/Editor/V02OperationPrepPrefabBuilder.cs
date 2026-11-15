using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

// 작전 준비 화면과 공통 가게 카드 프리팹 (M4-6, COMP-OperationPrep-v003 기준, 1080×1920).
// 기존 프리팹 파일과 GUID 를 유지하고 컨트롤러(V02OperationPrepScreen / V02OperationPrepCard)는 그대로 둔다.
// 하위 Visual 계층만 비우고 다시 만든 뒤 직렬화 참조를 다시 연결한다. 가게 내용은 넣지 않는다.
// 이미 layoutVersion 이 현재와 같으면 EnsureCurrent 는 아무것도 하지 않는다. 직접 고친 프리팹은 보호된다.
// 메뉴 Rebuild 는 프리팹을 이 코드의 배치로 되돌리므로 확인 창을 거친다. Edit Mode 에서 프리팹을 열어 배치를 본다 (D-019).
public static class V02OperationPrepPrefabBuilder
{
    public const string Folder = "Assets/Resources/V02/UI";
    public const string PrefabPath = Folder + "/OperationPrep.prefab";
    public const string ShopCardPath = Folder + "/ShopCard.prefab";

    const string EnergyIconSheet = "Assets/Art/Lobby/CT_Lobby_HUD_Sheet_v001.png";
    const string EnergyIconName = "HUD_EnergyIcon";
    const string ChevronPath = "Assets/Art/V02/UI/CT_UI_Icon_ChevronLeft_v001.png";
    const string RefreshPath = "Assets/Art/V02/UI/CT_UI_Icon_RefreshWhite_v001.png";

    // 1080×1920 기준 픽셀 배치. 앵커로 변환해 저장하므로 Edit Mode 와 Play Mode 가 같다.
    static readonly Vector2 ScreenSize = new Vector2(1080f, 1920f);
    static readonly Vector2 CardSize = new Vector2(522f, 778f);
    static readonly Vector2 FaceSize = new Vector2(488f, 744f);
    static readonly Vector2[] CardPos =
    {
        new Vector2(10f, 158f),
        new Vector2(548f, 158f),
        new Vector2(10f, 960f),
        new Vector2(548f, 960f)
    };

    static readonly Color Ink = new Color(0.16f, 0.08f, 0.045f, 1f);
    static readonly Color Cream = new Color(1f, 0.894f, 0.690f, 1f);
    static readonly Color Face = new Color(0.988f, 0.922f, 0.796f, 1f);
    static readonly Color Frame = new Color(0.52f, 0.325f, 0.23f, 1f);
    static readonly Color Header = new Color(0.384f, 0.227f, 0.192f, 1f);
    static readonly Color HeaderEdge = new Color(0.15f, 0.08f, 0.067f, 1f);
    static readonly Color HeaderButton = new Color(0.5f, 0.32f, 0.235f, 1f);
    static readonly Color NameBar = new Color(0.25f, 0.14f, 0.095f, 1f);
    static readonly Color Chip = new Color(0.847f, 0.74f, 0.6f, 1f);
    static readonly Color ChipLine = new Color(0.725f, 0.59f, 0.42f, 1f);
    static readonly Color Panel = new Color(0.835f, 0.745f, 0.62f, 1f);
    static readonly Color Brown = new Color(0.23f, 0.135f, 0.085f, 1f);
    static readonly Color Grade = new Color(1f, 0.82f, 0.18f, 1f);
    static readonly Color Yellow = new Color(1f, 0.788f, 0.176f, 1f);
    static readonly Color Selected = new Color(0.98f, 0.82f, 0.28f, 1f);
    static readonly Color Green = new Color(0.36f, 0.68f, 0.36f, 1f);
    static readonly Color ShadowTint = new Color(0.2f, 0.1f, 0.05f, 0.3f);
    static readonly Color PlaceholderArt = new Color(0.93f, 0.86f, 0.78f, 1f);
    static readonly Color PipOff = new Color(0.85f, 0.78f, 0.70f, 1f);
    static readonly Color PipOn = new Color(0.85f, 0.36f, 0.40f, 1f);

    [MenuItem("Candy Thief/V02/Bake Operation Prep (if missing)")]
    public static void BuildIfMissing()
    {
        if (AssetDatabase.LoadAssetAtPath<V02OperationPrepScreen>(PrefabPath) != null)
        {
            Debug.Log("[V02] 작전 준비 프리팹이 이미 있습니다: " + PrefabPath);
            return;
        }

        Rebuild();
    }

    [MenuItem("Candy Thief/V02/Rebuild Operation Prep")]
    static void RebuildFromMenu()
    {
        if (!EditorUtility.DisplayDialog("작전 준비 프리팹 재생성",
                "OperationPrep / ShopCard 의 Visual 계층을 코드 배치로 다시 만듭니다.\nInspector 에서 직접 고친 배치와 스타일은 사라집니다. 계속할까요?",
                "다시 만들기", "취소"))
            return;
        Rebuild();
    }

    public static void Rebuild()
    {
        EnsureFolder();
        V02OperationPrepArt.EnsureAssets();
        RebuildShopCard();
        RebuildScreen();
        AssetDatabase.SaveAssets();
        AssetDatabase.ImportAsset(ShopCardPath);
        AssetDatabase.ImportAsset(PrefabPath);
        Debug.Log("[V02] 작전 준비 프리팹 저장: " + PrefabPath + " (layout " + V02OperationPrepScreen.CurrentLayout + ")");
    }

    public static void EnsureCurrent()
    {
        V02OperationPrepScreen existing = AssetDatabase.LoadAssetAtPath<V02OperationPrepScreen>(PrefabPath);
        if (existing != null && existing.LayoutVersion >= V02OperationPrepScreen.CurrentLayout
            && AssetDatabase.LoadAssetAtPath<V02OperationPrepCard>(ShopCardPath) != null)
            return;

        Rebuild();
    }

    static void EnsureFolder()
    {
        if (!AssetDatabase.IsValidFolder("Assets/Resources"))
            AssetDatabase.CreateFolder("Assets", "Resources");
        if (!AssetDatabase.IsValidFolder("Assets/Resources/V02"))
            AssetDatabase.CreateFolder("Assets/Resources", "V02");
        if (!AssetDatabase.IsValidFolder(Folder))
            AssetDatabase.CreateFolder("Assets/Resources/V02", "UI");
    }

    // ───────── 화면 ─────────

    static void RebuildScreen()
    {
        bool fresh = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath) == null;
        GameObject root = fresh
            ? new GameObject("OperationPrep", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(CanvasGroup))
            : PrefabUtility.LoadPrefabContents(PrefabPath);
        ClearChildren(root.transform);

        RectTransform rootRect = (RectTransform)root.transform;
        V02Ui.Stretch(rootRect);
        Image background = Ensure<Image>(root);
        background.sprite = null;
        background.color = Cream;
        background.raycastTarget = true;
        Ensure<CanvasGroup>(root);
        V02OperationPrepScreen screen = Ensure<V02OperationPrepScreen>(root);

        // 상단 헤더: 화면 폭 전체, 위쪽 모서리와 바깥 여백 없음
        SoftShadow(root.transform, ScreenSize, new Rect(0f, 0f, ScreenSize.x, 128f), 1, 10f, 0.32f);
        RectTransform top = Plate("TopBar", root.transform, ScreenSize, new Rect(0f, 0f, ScreenSize.x, 128f), 0f, Header, 0f, out Image topFill);
        top.GetComponent<Image>().sprite = null;
        top.GetComponent<Image>().type = Image.Type.Simple;
        Image edge = NewImage("Edge", top, new Vector2(ScreenSize.x, 128f), new Rect(0f, 120f, ScreenSize.x, 8f), 0f, HeaderEdge);
        edge.sprite = null;
        edge.type = Image.Type.Simple;

        Button back = BuildHeaderButton("Back", root.transform, new Rect(16f, 21f, 204f, 82f), "로비", ChevronPath, new Vector2(27f, 40f), 28f, 46f, out TextMeshProUGUI backLabel);
        TextMeshProUGUI title = NewText("Header", root.transform, ScreenSize, new Rect(0f, 0f, ScreenSize.x, 120f), "작전 준비", 76f, Color.white, TextAlignmentOptions.Center, true);
        title.raycastTarget = false;
        Button reroll = BuildHeaderButton("Reroll", root.transform, new Rect(790f, 21f, 274f, 82f), "(1/1)", RefreshPath, new Vector2(48f, 50f), 28f, 34f, out TextMeshProUGUI rerollLabel);

        GameObject cardAsset = AssetDatabase.LoadAssetAtPath<GameObject>(ShopCardPath);
        var cardViews = new V02OperationPrepCard[4];
        for (int i = 0; i < 4; i++)
        {
            GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(cardAsset, root.transform);
            instance.name = "Card" + i;
            Place((RectTransform)instance.transform, ScreenSize, new Rect(CardPos[i].x, CardPos[i].y, CardSize.x, CardSize.y));
            cardViews[i] = instance.GetComponent<V02OperationPrepCard>();
        }

        TextMeshProUGUI message = NewText("Message", root.transform, ScreenSize, new Rect(60f, 1738f, 960f, 30f), "", 24f, Color.white, TextAlignmentOptions.Center, true);

        Rect startRect = new Rect(225f, 1770f, 630f, 130f);
        SoftShadow(root.transform, ScreenSize, startRect, 2, 6f, 0.3f);
        RectTransform startRoot = Plate("Start", root.transform, ScreenSize, startRect, 40f, Ink, 5f, out Image startFill);
        startFill.color = Yellow;
        startRoot.GetComponent<Image>().raycastTarget = true;
        Button start = startRoot.gameObject.AddComponent<Button>();
        StyleButton(start, startFill, new Color(0.82f, 0.74f, 0.5f, 1f));
        NewText("Label", startRoot, startRect.size, new Rect(0f, 0f, startRect.width, startRect.height - 4f), "작전 시작", 76f, Color.white, TextAlignmentOptions.Center, true);
        start.interactable = false;

        SerializedObject so = new SerializedObject(screen);
        SerializedProperty cards = so.FindProperty("cards");
        cards.arraySize = 4;
        for (int i = 0; i < 4; i++)
            cards.GetArrayElementAtIndex(i).objectReferenceValue = cardViews[i];
        so.FindProperty("rerollButton").objectReferenceValue = reroll;
        so.FindProperty("rerollLabel").objectReferenceValue = rerollLabel;
        so.FindProperty("startButton").objectReferenceValue = start;
        so.FindProperty("backButton").objectReferenceValue = back;
        so.FindProperty("messageText").objectReferenceValue = message;
        so.FindProperty("layoutVersion").intValue = V02OperationPrepScreen.CurrentLayout;
        so.ApplyModifiedPropertiesWithoutUndo();

        Save(root, PrefabPath, fresh);
    }

    // 로비 / 새로고침 버튼. 플랫한 갈색 면 + 어두운 외곽선 + 아이콘 + 라벨.
    static Button BuildHeaderButton(string name, Transform parent, Rect rect, string label, string iconPath, Vector2 iconSize, float iconX, float labelSize, out TextMeshProUGUI labelText)
    {
        SoftShadow(parent, ScreenSize, rect, 2, 4f, 0.28f);
        RectTransform root = Plate(name, parent, ScreenSize, rect, 20f, Ink, 4f, out Image fill);
        fill.color = HeaderButton;
        root.GetComponent<Image>().raycastTarget = true;
        Button button = root.gameObject.AddComponent<Button>();
        StyleButton(button, fill, new Color(0.7f, 0.65f, 0.6f, 1f));

        Sprite icon = AssetDatabase.LoadAssetAtPath<Sprite>(iconPath);
        Image iconImage = NewImage("Icon", root, rect.size, new Rect(iconX, (rect.height - iconSize.y) * 0.5f, iconSize.x, iconSize.y), 0f, Color.white);
        iconImage.sprite = icon;
        iconImage.type = Image.Type.Simple;
        iconImage.preserveAspect = true;

        float labelX = iconX + iconSize.x + 8f;
        labelText = NewText("Label", root, rect.size, new Rect(labelX, 0f, rect.width - labelX - 10f, rect.height - 4f), label, labelSize, Color.white, TextAlignmentOptions.Center, true);
        return button;
    }

    // ───────── 카드 ─────────

    static void RebuildShopCard()
    {
        bool fresh = AssetDatabase.LoadAssetAtPath<GameObject>(ShopCardPath) == null;
        GameObject root = fresh
            ? new GameObject("ShopCard", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button), typeof(V02OperationPrepCard))
            : PrefabUtility.LoadPrefabContents(ShopCardPath);
        ClearChildren(root.transform);

        RectTransform rootRect = (RectTransform)root.transform;
        rootRect.anchorMin = new Vector2(0.5f, 0.5f);
        rootRect.anchorMax = new Vector2(0.5f, 0.5f);
        rootRect.pivot = new Vector2(0.5f, 0.5f);
        rootRect.sizeDelta = CardSize;
        rootRect.localScale = Vector3.one;

        // 루트 Image 는 보이지 않는 터치 영역이다. 외곽선·그림자·선택 링은 자식 레이어가 그린다.
        Ensure<CanvasRenderer>(root).cullTransparentMesh = false;
        Image hit = Ensure<Image>(root);
        hit.sprite = null;
        hit.color = new Color(0f, 0f, 0f, 0f);
        hit.raycastTarget = true;
        Button button = Ensure<Button>(root);
        button.targetGraphic = hit;
        button.transition = Selectable.Transition.None;
        V02OperationPrepCard card = Ensure<V02OperationPrepCard>(root);

        Rect cardRect = new Rect(0f, 0f, CardSize.x, CardSize.y);
        SoftShadow(root.transform, CardSize, cardRect, 1, 12f, 0.4f);
        Image selected = NewImage("Selected", root.transform, CardSize, new Rect(-9f, -9f, CardSize.x + 18f, CardSize.y + 18f), 46f, Selected);
        selected.gameObject.SetActive(false);
        NewImage("Outline", root.transform, CardSize, cardRect, 36f, Ink);
        NewImage("Frame", root.transform, CardSize, new Rect(6f, 6f, CardSize.x - 12f, CardSize.y - 12f), 30f, Frame);
        Image face = NewImage("Face", root.transform, CardSize, new Rect(17f, 17f, FaceSize.x, FaceSize.y), 24f, Face);
        Transform f = face.transform;

        // 일러스트: 어두운 외곽선 위에 둥근 마스크. 실제 그림은 Background 에 들어간다.
        NewImage("ArtFrame", f, FaceSize, new Rect(5f, 5f, 478f, 368f), 20f, Ink);
        Image artMask = NewImage("ArtMask", f, FaceSize, new Rect(8f, 8f, 472f, 362f), 17f, Color.white);
        artMask.gameObject.AddComponent<Mask>().showMaskGraphic = false;
        RectTransform artRect = V02Ui.CreateStretch("Background", artMask.transform);
        Image art = artRect.gameObject.AddComponent<Image>();
        art.color = PlaceholderArt;
        art.raycastTarget = false;
        TextMeshProUGUI placeholder = NewText("Placeholder", artRect, new Vector2(472f, 362f), new Rect(0f, 0f, 472f, 362f), "일러스트", 38f, new Color(Brown.r, Brown.g, Brown.b, 0.55f), TextAlignmentOptions.Center, false);

        // 가게 이름 바
        NewImage("NameBar", f, FaceSize, new Rect(8f, 378f, 472f, 70f), 18f, NameBar);
        TextMeshProUGUI shopName = NewText("Name", f, FaceSize, new Rect(20f, 378f, 448f, 68f), "가게 이름", 50f, Color.white, TextAlignmentOptions.Center, true);
        shopName.enableAutoSizing = true;
        shopName.fontSizeMin = 28f;
        shopName.fontSizeMax = 50f;
        shopName.overflowMode = TextOverflowModes.Ellipsis;

        // 정보 위젯 4개
        TextMeshProUGUI distance = BuildChip(f, "Distance", "거리", 0);
        TextMeshProUGUI scale = BuildChip(f, "Scale", "규모", 1);
        TextMeshProUGUI security = BuildChip(f, "Security", "경비", 2);
        TextMeshProUGUI goods = BuildChip(f, "Goods", "상품", 3);

        // 소문 패널: 한 줄이든 두 줄이든 세로 가운데 정렬
        RectTransform rumorRoot = Plate("RumorPanel", f, FaceSize, new Rect(11f, 556f, 466f, 90f), 16f, ChipLine, 2f, out Image rumorFill);
        rumorFill.color = Panel;
        rumorFill.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;
        VerticalLayoutGroup list = rumorRoot.gameObject.AddComponent<VerticalLayoutGroup>();
        list.padding = new RectOffset(14, 14, 6, 6);
        list.childAlignment = TextAnchor.MiddleCenter;
        list.childControlWidth = true;
        list.childControlHeight = true;
        list.childForceExpandWidth = true;
        list.childForceExpandHeight = false;
        TextMeshProUGUI rumor0 = NewText("Rumor0", rumorRoot, new Vector2(466f, 90f), new Rect(0f, 0f, 466f, 40f), "", 28f, Brown, TextAlignmentOptions.Center, false);
        TextMeshProUGUI rumor1 = NewText("Rumor1", rumorRoot, new Vector2(466f, 90f), new Rect(0f, 40f, 466f, 40f), "", 28f, Brown, TextAlignmentOptions.Center, false);
        TextMeshProUGUI empty = NewText("EmptyRumor", rumorRoot, new Vector2(466f, 90f), new Rect(0f, 0f, 466f, 90f), V02OperationPrepCard.EmptyRumorLabel, 34f, Brown, TextAlignmentOptions.Center, false);
        empty.gameObject.AddComponent<LayoutElement>().ignoreLayout = true;
        V02Ui.Stretch(empty.rectTransform);
        foreach (TextMeshProUGUI line in new[] { rumor0, rumor1 })
        {
            line.enableAutoSizing = true;
            line.fontSizeMin = 20f;
            line.fontSizeMax = 30f;
            line.overflowMode = TextOverflowModes.Ellipsis;
        }

        // 행동력 배지 (번개 아이콘 + 행동력 + 소모량)
        Rect badge = new Rect(78f, 656f, 332f, 74f);
        SoftShadow(f, FaceSize, badge, 2, 4f, 0.28f);
        Plate("EnergyBadge", f, FaceSize, badge, 24f, Ink, 4f, out Image badgeFill);
        badgeFill.color = Yellow;
        Image energyIcon = NewImage("EnergyIcon", f, FaceSize, new Rect(134f, 664f, 46f, 54f), 0f, Color.white);
        energyIcon.sprite = LoadEnergyIcon();
        energyIcon.type = Image.Type.Simple;
        energyIcon.preserveAspect = true;
        NewText("EnergyLabel", f, FaceSize, new Rect(188f, 656f, 110f, 72f), "행동력", 31f, Color.white, TextAlignmentOptions.Left, true);
        TextMeshProUGUI energy = NewText("EnergyValue", f, FaceSize, new Rect(318f, 656f, 60f, 72f), "0", 42f, Color.white, TextAlignmentOptions.Left, true);

        // 선택 표시
        Image check = NewImage("Check", f, FaceSize, new Rect(392f, 16f, 84f, 42f), 21f, Green);
        NewText("Mark", check.transform, new Vector2(84f, 42f), new Rect(0f, 0f, 84f, 40f), "선택", 24f, Color.white, TextAlignmentOptions.Center, true);
        check.gameObject.SetActive(false);

        // 위험도 위젯 (규칙이 정해질 때까지 숨김)
        Image risk = NewImage("Risk", f, FaceSize, new Rect(16f, 316f, 200f, 44f), 14f, new Color(Ink.r, Ink.g, Ink.b, 0.85f));
        NewText("RiskCaption", risk.transform, new Vector2(200f, 44f), new Rect(10f, 0f, 80f, 44f), "위험도", 20f, Color.white, TextAlignmentOptions.Left, false);
        var pips = new GameObject[3];
        for (int i = 0; i < 3; i++)
        {
            Image pip = NewImage("Pip" + i, risk.transform, new Vector2(200f, 44f), new Rect(96f + i * 34f, 12f, 28f, 20f), 8f, PipOff);
            Image on = NewImage("On", pip.transform, new Vector2(28f, 20f), new Rect(4f, 4f, 20f, 12f), 5f, PipOn);
            on.gameObject.SetActive(false);
            pips[i] = on.gameObject;
        }

        risk.gameObject.SetActive(false);

        SerializedObject so = new SerializedObject(card);
        so.FindProperty("background").objectReferenceValue = art;
        so.FindProperty("placeholder").objectReferenceValue = placeholder.gameObject;
        so.FindProperty("nameText").objectReferenceValue = shopName;
        so.FindProperty("distanceText").objectReferenceValue = distance;
        so.FindProperty("scaleText").objectReferenceValue = scale;
        so.FindProperty("securityText").objectReferenceValue = security;
        so.FindProperty("goodsText").objectReferenceValue = goods;
        so.FindProperty("energyValue").objectReferenceValue = energy;
        so.FindProperty("rumorLine0").objectReferenceValue = rumor0;
        so.FindProperty("rumorLine1").objectReferenceValue = rumor1;
        so.FindProperty("emptyRumor").objectReferenceValue = empty.gameObject;
        so.FindProperty("selectedMark").objectReferenceValue = selected.gameObject;
        so.FindProperty("checkMark").objectReferenceValue = check.gameObject;
        so.FindProperty("riskRoot").objectReferenceValue = risk.gameObject;
        so.FindProperty("button").objectReferenceValue = button;
        so.FindProperty("gradeText").objectReferenceValue = null;
        so.FindProperty("energyText").objectReferenceValue = null;
        so.FindProperty("rumorText").objectReferenceValue = null;
        SerializedProperty filled = so.FindProperty("riskFilled");
        filled.arraySize = pips.Length;
        for (int i = 0; i < pips.Length; i++)
            filled.GetArrayElementAtIndex(i).objectReferenceValue = pips[i];
        so.ApplyModifiedPropertiesWithoutUndo();

        Save(root, ShopCardPath, fresh);
    }

    // 정보 위젯: 위 정보명(어두운 갈색), 아래 등급(노란색 + 어두운 외곽선).
    static TextMeshProUGUI BuildChip(Transform parent, string name, string caption, int index)
    {
        var rect = new Rect(11f + index * 119f, 458f, 109f, 88f);
        RectTransform root = Plate(name, parent, FaceSize, rect, 16f, ChipLine, 2f, out Image fill);
        fill.color = Chip;
        NewText("Caption", root, rect.size, new Rect(0f, 6f, rect.width, 28f), caption, 24f, Brown, TextAlignmentOptions.Center, false);
        TextMeshProUGUI value = NewText("Value", root, rect.size, new Rect(0f, 34f, rect.width, 50f), "보통", 30f, Grade, TextAlignmentOptions.Center, true);
        value.enableAutoSizing = true;
        value.fontSizeMin = 22f;
        value.fontSizeMax = 30f;
        return value;
    }

    static Sprite LoadEnergyIcon()
    {
        foreach (Object asset in AssetDatabase.LoadAllAssetsAtPath(EnergyIconSheet))
        {
            Sprite sprite = asset as Sprite;
            if (sprite != null && sprite.name == EnergyIconName)
                return sprite;
        }

        Debug.LogWarning("[V02] 로비 행동력 아이콘을 찾지 못했습니다: " + EnergyIconSheet + " / " + EnergyIconName);
        return null;
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

    static void Place(RectTransform rect, Vector2 parent, Rect r)
    {
        rect.anchorMin = new Vector2(r.x / parent.x, 1f - r.yMax / parent.y);
        rect.anchorMax = new Vector2(r.xMax / parent.x, 1f - r.y / parent.y);
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        rect.localScale = Vector3.one;
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
