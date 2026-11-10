using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public static class V02RunHudPrefabBuilder
{
    const string Folder = "Assets/Resources/V02/UI";
    const string ButtonPath = Folder + "/ChoiceButton.prefab";
    const string HudPath = Folder + "/RunHud.prefab";

    [MenuItem("Candy Thief/V02/Rebuild Run HUD Prefabs")]
    public static void Rebuild()
    {
        if (!EditorUtility.DisplayDialog(
            "Rebuild Run HUD Prefabs",
            "RunHud.prefab 과 ChoiceButton.prefab 을 코드 기본값으로 덮어씁니다.\nM1-8.5 연출 슬롯과 수동 Inspector 조정이 초기화됩니다.",
            "덮어쓰기",
            "취소"))
            return;

        if (!AssetDatabase.IsValidFolder("Assets/Resources"))
            AssetDatabase.CreateFolder("Assets", "Resources");
        if (!AssetDatabase.IsValidFolder("Assets/Resources/V02"))
            AssetDatabase.CreateFolder("Assets/Resources", "V02");
        if (!AssetDatabase.IsValidFolder(Folder))
            AssetDatabase.CreateFolder("Assets/Resources/V02", "UI");

        V02ChoiceButton button = BuildChoiceButton();
        V02ChoiceButton buttonPrefab = PrefabUtility.SaveAsPrefabAsset(button.gameObject, ButtonPath).GetComponent<V02ChoiceButton>();
        Object.DestroyImmediate(button.gameObject);

        V02RunHud hud = BuildHud(buttonPrefab);
        PrefabUtility.SaveAsPrefabAsset(hud.gameObject, HudPath);
        Object.DestroyImmediate(hud.gameObject);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[V02] Run HUD 프리팹 저장: " + HudPath + " / " + ButtonPath);
    }

    static V02ChoiceButton BuildChoiceButton()
    {
        RectTransform root = V02Ui.CreatePanel("ChoiceButton", null, V02Ui.White);
        Image image = root.GetComponent<Image>();
        Button button = root.gameObject.AddComponent<Button>();
        button.targetGraphic = image;
        CanvasGroup group = V02Ui.AddCanvasGroup(root);
        LayoutElement layout = root.gameObject.AddComponent<LayoutElement>();
        layout.minHeight = 200f;
        layout.preferredHeight = -1f;
        ContentSizeFitter rootFit = root.gameObject.AddComponent<ContentSizeFitter>();
        rootFit.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        VerticalLayoutGroup vertical = root.gameObject.AddComponent<VerticalLayoutGroup>();
        vertical.padding = new RectOffset(28, 28, 20, 20);
        vertical.spacing = 6f;
        vertical.childAlignment = TextAnchor.UpperLeft;
        vertical.childControlWidth = true;
        vertical.childControlHeight = true;
        vertical.childForceExpandWidth = true;
        vertical.childForceExpandHeight = false;

        TextMeshProUGUI name = AddLabel(root, "Name", "이름", 34f, V02Ui.Ink);
        TextMeshProUGUI description = AddLabel(root, "Description", "설명", 26f, V02Ui.Ink);
        TextMeshProUGUI chance = AddLabel(root, "Chance", "성공률", 26f, V02Ui.Ink);
        TextMeshProUGUI cost = AddLabel(root, "Cost", "턴 비용", 26f, V02Ui.Ink);
        TextMeshProUGUI success = AddLabel(root, "Success", "성공", 24f, V02Ui.Ink);
        TextMeshProUGUI fail = AddLabel(root, "Fail", "실패", 24f, V02Ui.Ink);
        TextMeshProUGUI disabled = AddLabel(root, "DisabledReason", "", 24f, new Color(0.55f, 0.18f, 0.22f));
        disabled.gameObject.SetActive(false);

        V02ChoiceButton view = root.gameObject.AddComponent<V02ChoiceButton>();
        SerializedObject so = new SerializedObject(view);
        so.FindProperty("button").objectReferenceValue = button;
        so.FindProperty("canvasGroup").objectReferenceValue = group;
        so.FindProperty("nameText").objectReferenceValue = name;
        so.FindProperty("descriptionText").objectReferenceValue = description;
        so.FindProperty("chanceText").objectReferenceValue = chance;
        so.FindProperty("costText").objectReferenceValue = cost;
        so.FindProperty("successText").objectReferenceValue = success;
        so.FindProperty("failText").objectReferenceValue = fail;
        so.FindProperty("disabledReasonText").objectReferenceValue = disabled;
        so.ApplyModifiedPropertiesWithoutUndo();
        return view;
    }

    static V02RunHud BuildHud(V02ChoiceButton buttonPrefab)
    {
        RectTransform root = V02Ui.CreatePanel("RunHud", null, V02Ui.Mint);
        V02Ui.AddCanvasGroup(root);

        RectTransform top = V02Ui.CreatePanel("TopBar", root, V02Ui.White);
        V02Ui.SetAnchors(top, new Vector2(0.04f, 0.90f), new Vector2(0.96f, 0.98f), Vector2.zero, Vector2.zero);
        HorizontalLayoutGroup topLayout = top.gameObject.AddComponent<HorizontalLayoutGroup>();
        topLayout.padding = new RectOffset(16, 16, 8, 8);
        topLayout.spacing = 12f;
        topLayout.childAlignment = TextAnchor.MiddleCenter;
        topLayout.childControlWidth = true;
        topLayout.childControlHeight = true;
        topLayout.childForceExpandWidth = true;
        topLayout.childForceExpandHeight = true;
        TextMeshProUGUI turns = AddLabel(top, "Turns", "턴", 32f, V02Ui.Ink, TextAlignmentOptions.Center);
        TextMeshProUGUI alert = AddLabel(top, "Alert", "위험도", 32f, V02Ui.Ink, TextAlignmentOptions.Center);
        TextMeshProUGUI candy = AddLabel(top, "Candy", "사탕", 32f, V02Ui.Ink, TextAlignmentOptions.Center);

        RectTransform center = V02Ui.CreatePanel("Center", root, new Color(1f, 1f, 1f, 0.35f));
        V02Ui.SetAnchors(center, new Vector2(0.06f, 0.46f), new Vector2(0.94f, 0.88f), Vector2.zero, Vector2.zero);

        Image illustration = CreateImage("Illustration", center, V02Ui.Hideout);
        V02Ui.SetAnchors((RectTransform)illustration.transform, new Vector2(0.08f, 0.42f), new Vector2(0.92f, 0.96f), Vector2.zero, Vector2.zero);
        illustration.preserveAspect = true;

        TextMeshProUGUI title = AddLabel(center, "Title", "상황", 40f, V02Ui.Ink, TextAlignmentOptions.Center);
        V02Ui.SetAnchors(title.rectTransform, new Vector2(0.06f, 0.28f), new Vector2(0.94f, 0.40f), Vector2.zero, Vector2.zero);
        Object.DestroyImmediate(title.GetComponent<LayoutElement>());

        TextMeshProUGUI body = AddLabel(center, "Body", "", 28f, V02Ui.Ink, TextAlignmentOptions.Top);
        V02Ui.SetAnchors(body.rectTransform, new Vector2(0.06f, 0.12f), new Vector2(0.94f, 0.28f), Vector2.zero, Vector2.zero);
        Object.DestroyImmediate(body.GetComponent<LayoutElement>());

        RectTransform result = V02Ui.CreatePanel("Result", center, V02Ui.Yellow);
        V02Ui.SetAnchors(result, new Vector2(0.06f, 0.02f), new Vector2(0.94f, 0.12f), Vector2.zero, Vector2.zero);
        result.gameObject.SetActive(false);
        TextMeshProUGUI resultStatus = AddLabel(result, "Status", "", 26f, V02Ui.Ink, TextAlignmentOptions.MidlineLeft);
        V02Ui.SetAnchors(resultStatus.rectTransform, new Vector2(0.04f, 0.52f), new Vector2(0.96f, 0.96f), Vector2.zero, Vector2.zero);
        TextMeshProUGUI resultMessage = AddLabel(result, "Message", "", 24f, V02Ui.Ink, TextAlignmentOptions.MidlineLeft);
        V02Ui.SetAnchors(resultMessage.rectTransform, new Vector2(0.04f, 0.28f), new Vector2(0.50f, 0.52f), Vector2.zero, Vector2.zero);
        TextMeshProUGUI resultDelta = AddLabel(result, "Deltas", "", 24f, V02Ui.Ink, TextAlignmentOptions.MidlineLeft);
        V02Ui.SetAnchors(resultDelta.rectTransform, new Vector2(0.04f, 0.02f), new Vector2(0.96f, 0.28f), Vector2.zero, Vector2.zero);

        var scrollGo = new GameObject("ChoiceScroll", typeof(RectTransform));
        scrollGo.transform.SetParent(root, false);
        RectTransform scrollRect = (RectTransform)scrollGo.transform;
        V02Ui.SetAnchors(scrollRect, new Vector2(0.06f, 0.10f), new Vector2(0.94f, 0.44f), Vector2.zero, Vector2.zero);
        Image scrollImage = scrollGo.AddComponent<Image>();
        scrollImage.color = new Color(1f, 1f, 1f, 0.12f);
        ScrollRect scroll = scrollGo.AddComponent<ScrollRect>();
        scroll.horizontal = false;
        scroll.vertical = true;
        scroll.movementType = ScrollRect.MovementType.Elastic;
        RectTransform viewport = V02Ui.CreateStretch("Viewport", scrollRect);
        viewport.gameObject.AddComponent<RectMask2D>();
        scroll.viewport = viewport;
        RectTransform list = V02Ui.CreateStretch("ChoiceList", viewport);
        VerticalLayoutGroup listLayout = list.gameObject.AddComponent<VerticalLayoutGroup>();
        listLayout.padding = new RectOffset(8, 8, 8, 8);
        listLayout.spacing = 16f;
        listLayout.childAlignment = TextAnchor.UpperCenter;
        listLayout.childControlWidth = true;
        listLayout.childControlHeight = true;
        listLayout.childForceExpandWidth = true;
        listLayout.childForceExpandHeight = false;
        ContentSizeFitter fitter = list.gameObject.AddComponent<ContentSizeFitter>();
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        list.pivot = new Vector2(0.5f, 1f);
        list.anchorMin = new Vector2(0f, 1f);
        list.anchorMax = new Vector2(1f, 1f);
        list.anchoredPosition = Vector2.zero;
        scroll.content = list;
        scroll.verticalNormalizedPosition = 1f;

        Button lobby = V02Ui.CreateButton("Lobby", root, "로비", V02Ui.White, null);
        V02Ui.SetAnchors((RectTransform)lobby.transform, new Vector2(0.08f, 0.02f), new Vector2(0.92f, 0.08f), Vector2.zero, Vector2.zero);

        V02RunHud hud = root.gameObject.AddComponent<V02RunHud>();
        SerializedObject so = new SerializedObject(hud);
        so.FindProperty("turnsText").objectReferenceValue = turns;
        so.FindProperty("alertText").objectReferenceValue = alert;
        so.FindProperty("candyText").objectReferenceValue = candy;
        so.FindProperty("illustration").objectReferenceValue = illustration;
        so.FindProperty("placeholderColor").colorValue = V02Ui.Hideout;
        so.FindProperty("titleText").objectReferenceValue = title;
        so.FindProperty("bodyText").objectReferenceValue = body;
        so.FindProperty("resultRoot").objectReferenceValue = result.gameObject;
        so.FindProperty("resultStatusText").objectReferenceValue = resultStatus;
        so.FindProperty("resultMessageText").objectReferenceValue = resultMessage;
        so.FindProperty("resultDeltaText").objectReferenceValue = resultDelta;
        so.FindProperty("choiceList").objectReferenceValue = list;
        so.FindProperty("choiceScroll").objectReferenceValue = scroll;
        so.FindProperty("choiceCanvasGroup").objectReferenceValue = scrollGo.AddComponent<CanvasGroup>();
        so.FindProperty("choiceButtonPrefab").objectReferenceValue = buttonPrefab;
        so.FindProperty("lobbyButton").objectReferenceValue = lobby;
        V02ResultPresentation presentation = V02ResultPresentation.Ensure(root);
        so.FindProperty("resultPresentation").objectReferenceValue = presentation;
        so.ApplyModifiedPropertiesWithoutUndo();
        AssignFeedbackSprites(presentation);
        return hud;
    }

    [MenuItem("Candy Thief/V02/Patch Run HUD Prefabs")]
    public static void PatchExisting()
    {
        PatchChoiceButton();
        PatchRunHud();
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[V02] Run HUD 프리팹을 덮어쓰지 않고 M1-8.6 HUD/선택지 레이아웃만 패치했습니다.");
    }

    static void PatchChoiceButton()
    {
        GameObject root = PrefabUtility.LoadPrefabContents(ButtonPath);
        try
        {
            Image background = root.GetComponent<Image>();
            if (background != null)
                background.color = new Color(1f, 0.97f, 0.90f, 1f);

            LayoutElement layout = root.GetComponent<LayoutElement>();
            if (layout != null)
            {
                layout.minHeight = 220f;
                layout.preferredHeight = -1f;
            }

            ContentSizeFitter fitter = root.GetComponent<ContentSizeFitter>();
            if (fitter == null)
                fitter = root.AddComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            Transform description = root.transform.Find("Description");
            if (description != null)
            {
                TextMeshProUGUI label = description.GetComponent<TextMeshProUGUI>();
                if (label != null)
                {
                    label.textWrappingMode = TextWrappingModes.Normal;
                    label.overflowMode = TextOverflowModes.Overflow;
                }
            }

            V02ChoiceButton view = root.GetComponent<V02ChoiceButton>();
            if (view != null)
                view.EnsureRuntimeChrome();

            SerializedObject so = new SerializedObject(view);
            Transform header = root.transform.Find("Header");
            Transform typeIcon = header != null ? header.Find("TypeIcon") : root.transform.Find("TypeIcon");
            Transform costRow = root.transform.Find("CostRow");
            Transform costIcon = costRow != null ? costRow.Find("CostIcon") : null;
            if (typeIcon != null)
                so.FindProperty("typeIcon").objectReferenceValue = typeIcon.GetComponent<Image>();
            if (costIcon != null)
                so.FindProperty("costIcon").objectReferenceValue = costIcon.GetComponent<Image>();
            so.ApplyModifiedPropertiesWithoutUndo();

            PrefabUtility.SaveAsPrefabAsset(root, ButtonPath);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }

    static void PatchRunHud()
    {
        GameObject root = PrefabUtility.LoadPrefabContents(HudPath);
        try
        {
            Transform list = FindDeep(root.transform, "ChoiceList");
            if (list != null)
            {
                RectTransform listRect = (RectTransform)list;
                listRect.pivot = new Vector2(0.5f, 1f);
                listRect.anchorMin = new Vector2(0f, 1f);
                listRect.anchorMax = new Vector2(1f, 1f);
                listRect.anchoredPosition = Vector2.zero;
            }

            Transform scroll = FindDeep(root.transform, "ChoiceScroll");
            CanvasGroup group = null;
            ScrollRect scrollRect = null;
            if (scroll != null)
            {
                scrollRect = scroll.GetComponent<ScrollRect>();
                group = scroll.GetComponent<CanvasGroup>();
                if (group == null)
                    group = scroll.gameObject.AddComponent<CanvasGroup>();
            }

            V02ResultPresentation presentation = root.GetComponentInChildren<V02ResultPresentation>(true);
            if (presentation == null)
                presentation = V02ResultPresentation.Ensure((RectTransform)root.transform);
            AssignFeedbackSprites(presentation);

            Transform top = FindDeep(root.transform, "TopBar");
            if (top != null)
                V02Ui.SetAnchors((RectTransform)top, new Vector2(0.04f, 0.885f), new Vector2(0.96f, 0.985f), Vector2.zero, Vector2.zero);

            Transform center = FindDeep(root.transform, "Center");
            if (center != null)
            {
                V02Ui.SetAnchors((RectTransform)center, new Vector2(0.06f, 0.455f), new Vector2(0.94f, 0.875f), Vector2.zero, Vector2.zero);
                Image centerImage = center.GetComponent<Image>();
                if (centerImage != null)
                    centerImage.color = new Color(1f, 0.94f, 0.82f, 1f);
            }

            V02RunHud hud = root.GetComponent<V02RunHud>();
            SerializedObject so = new SerializedObject(hud);
            if (list != null)
                so.FindProperty("choiceList").objectReferenceValue = list;
            if (scrollRect != null)
                so.FindProperty("choiceScroll").objectReferenceValue = scrollRect;
            if (group != null)
                so.FindProperty("choiceCanvasGroup").objectReferenceValue = group;
            so.FindProperty("resultPresentation").objectReferenceValue = presentation;
            so.FindProperty("hourglassSprite").objectReferenceValue = LoadSprite("Assets/Art/V02/UI/CT_Hud_Hourglass_v001.png");
            so.FindProperty("alertSprite").objectReferenceValue = LoadSprite("Assets/Art/V02/UI/CT_Hud_Alert_v001.png");
            so.FindProperty("candySprite").objectReferenceValue = LoadSprite("Assets/Art/V02/UI/CT_Hud_Candy_v001.png");
            so.FindProperty("moveIcon").objectReferenceValue = LoadSprite("Assets/Art/V02/UI/CT_Icon_Move_v001.png");
            so.FindProperty("infiltrateIcon").objectReferenceValue = LoadSprite("Assets/Art/V02/UI/CT_Icon_Infiltrate_v001.png");
            so.FindProperty("lootIcon").objectReferenceValue = LoadSprite("Assets/Art/V02/UI/CT_Hud_Candy_v001.png");
            so.FindProperty("escapeIcon").objectReferenceValue = LoadSprite("Assets/Art/V02/UI/CT_Icon_Escape_v001.png");
            so.FindProperty("eventIcon").objectReferenceValue = LoadSprite("Assets/Art/V02/UI/CT_Icon_Event_v001.png");
            so.ApplyModifiedPropertiesWithoutUndo();

            if (hud != null)
                hud.EnsureRuntimeChrome();

            so.Update();
            AssignIfFound(so, "turnChip", FindDeep(root.transform, "TurnChip"));
            AssignIfFound(so, "turnIcon", FindDeep(root.transform, "TurnChipIcon"));
            AssignIfFound(so, "alertChip", FindDeep(root.transform, "AlertChip"));
            AssignIfFound(so, "alertIcon", FindDeep(root.transform, "AlertIcon"));
            AssignIfFound(so, "alertTrack", FindDeep(root.transform, "AlertTrack"));
            AssignIfFound(so, "alertFill", FindDeep(root.transform, "AlertFill"));
            AssignIfFound(so, "candyChip", FindDeep(root.transform, "CandyChip"));
            AssignIfFound(so, "candyIcon", FindDeep(root.transform, "CandyChipIcon"));
            AssignTmp(so, "turnsText", FindDeep(root.transform, "TurnChipValue"));
            AssignTmp(so, "alertText", FindDeep(root.transform, "AlertValue"));
            AssignTmp(so, "candyText", FindDeep(root.transform, "CandyChipValue"));
            so.ApplyModifiedPropertiesWithoutUndo();

            PrefabUtility.SaveAsPrefabAsset(root, HudPath);
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
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

    static void AssignIfFound(SerializedObject so, string property, Transform target)
    {
        if (target == null)
            return;
        Image image = target.GetComponent<Image>();
        if (image != null)
            so.FindProperty(property).objectReferenceValue = image;
    }

    static void AssignTmp(SerializedObject so, string property, Transform target)
    {
        if (target == null)
            return;
        TextMeshProUGUI label = target.GetComponent<TextMeshProUGUI>();
        if (label != null)
            so.FindProperty(property).objectReferenceValue = label;
    }

    static Sprite LoadSprite(string path)
    {
        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }

    static Transform FindDeep(Transform root, string name)
    {
        if (root.name == name)
            return root;
        for (int i = 0; i < root.childCount; i++)
        {
            Transform found = FindDeep(root.GetChild(i), name);
            if (found != null)
                return found;
        }

        return null;
    }

    static TextMeshProUGUI AddLabel(Transform parent, string name, string text, float size, Color color, TextAlignmentOptions align = TextAlignmentOptions.Left)
    {
        TextMeshProUGUI label = V02Ui.CreateText(name, parent, text, size, color, align);
        LayoutElement element = label.gameObject.AddComponent<LayoutElement>();
        element.minHeight = size + 8f;
        element.preferredHeight = size + 10f;
        return label;
    }

    static Image CreateImage(string name, Transform parent, Color color)
    {
        RectTransform rect = V02Ui.CreatePanel(name, parent, color);
        return rect.GetComponent<Image>();
    }
}
