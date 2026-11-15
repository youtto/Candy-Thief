using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

// 작전 준비 화면과 공통 가게 카드 프리팹. 가게 내용은 넣지 않는다.
// 레이아웃 버전이 낮을 때만 덮어쓴다. Edit Mode 에서 프리팹을 열어 배치를 본다 (D-019).
public static class V02OperationPrepPrefabBuilder
{
    public const string Folder = "Assets/Resources/V02/UI";
    public const string PrefabPath = Folder + "/OperationPrep.prefab";
    public const string ShopCardPath = Folder + "/ShopCard.prefab";

    static readonly Color Outline = new Color(0.204f, 0.165f, 0.208f, 1f);
    static readonly Color Vanilla = new Color(1f, 0.894f, 0.690f, 1f);
    static readonly Color Sky = new Color(0.569f, 0.784f, 0.941f, 1f);
    static readonly Color Primary = new Color(0.98f, 0.82f, 0.28f, 1f);
    static readonly Color PipOff = new Color(0.85f, 0.78f, 0.70f, 1f);
    static readonly Color PipOn = new Color(0.85f, 0.36f, 0.40f, 1f);

    static readonly Vector2[] CardMin =
    {
        new Vector2(0.03f, 0.55f),
        new Vector2(0.515f, 0.55f),
        new Vector2(0.03f, 0.145f),
        new Vector2(0.515f, 0.145f)
    };

    static readonly Vector2[] CardMax =
    {
        new Vector2(0.485f, 0.92f),
        new Vector2(0.97f, 0.92f),
        new Vector2(0.485f, 0.53f),
        new Vector2(0.97f, 0.53f)
    };

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
    public static void Rebuild()
    {
        EnsureFolder();
        BuildShopCardAsset();
        V02OperationPrepScreen screen = BuildScreen();
        PrefabUtility.SaveAsPrefabAsset(screen.gameObject, PrefabPath);
        Object.DestroyImmediate(screen.gameObject);
        AssetDatabase.SaveAssets();
        AssetDatabase.ImportAsset(ShopCardPath);
        AssetDatabase.ImportAsset(PrefabPath);
        Debug.Log("[V02] 작전 준비 프리팹 저장: " + PrefabPath);
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

    static void BuildShopCardAsset()
    {
        V02OperationPrepCard card = BuildCard(null, "ShopCard");
        PrefabUtility.SaveAsPrefabAsset(card.gameObject, ShopCardPath);
        Object.DestroyImmediate(card.gameObject);
    }

    static V02OperationPrepScreen BuildScreen()
    {
        RectTransform root = V02Ui.CreatePanel("OperationPrep", null, Vanilla);
        V02Ui.AddCanvasGroup(root);

        Button back = V02Ui.CreateButton("Back", root, "로비", V02Ui.White, null);
        V02Ui.SetAnchors((RectTransform)back.transform, new Vector2(0.04f, 0.945f), new Vector2(0.18f, 0.985f), Vector2.zero, Vector2.zero);

        TextMeshProUGUI header = V02Ui.CreateText("Header", root, "작전 준비", 40f, V02Ui.Ink, TextAlignmentOptions.Center);
        V02Ui.SetAnchors(header.rectTransform, new Vector2(0.20f, 0.935f), new Vector2(0.54f, 0.99f), Vector2.zero, Vector2.zero);

        Button reroll = V02Ui.CreateButton("Reroll", root, "새로고침 1/1", Sky, null);
        V02Ui.SetAnchors((RectTransform)reroll.transform, new Vector2(0.56f, 0.94f), new Vector2(0.96f, 0.985f), Vector2.zero, Vector2.zero);
        TextMeshProUGUI rerollText = reroll.GetComponentInChildren<TextMeshProUGUI>();
        rerollText.fontSize = 30f;
        rerollText.textWrappingMode = TextWrappingModes.NoWrap;
        rerollText.overflowMode = TextOverflowModes.Ellipsis;

        GameObject cardAsset = AssetDatabase.LoadAssetAtPath<GameObject>(ShopCardPath);
        var cardViews = new V02OperationPrepCard[4];
        for (int i = 0; i < 4; i++)
        {
            GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(cardAsset);
            instance.name = "Card" + i;
            instance.transform.SetParent(root, false);
            V02Ui.SetAnchors((RectTransform)instance.transform, CardMin[i], CardMax[i], Vector2.zero, Vector2.zero);
            cardViews[i] = instance.GetComponent<V02OperationPrepCard>();
        }

        Button start = V02Ui.CreateButton("Start", root, "작전 시작", Primary, null);
        V02Ui.SetAnchors((RectTransform)start.transform, new Vector2(0.22f, 0.025f), new Vector2(0.78f, 0.11f), Vector2.zero, Vector2.zero);
        start.interactable = false;

        TextMeshProUGUI message = V02Ui.CreateText("Message", root, "", 22f, V02Ui.Ink, TextAlignmentOptions.Center);
        V02Ui.SetAnchors(message.rectTransform, new Vector2(0.08f, 0.112f), new Vector2(0.92f, 0.142f), Vector2.zero, Vector2.zero);

        V02OperationPrepScreen screen = root.gameObject.AddComponent<V02OperationPrepScreen>();
        SerializedObject so = new SerializedObject(screen);
        SerializedProperty cards = so.FindProperty("cards");
        cards.arraySize = 4;
        for (int i = 0; i < 4; i++)
            cards.GetArrayElementAtIndex(i).objectReferenceValue = cardViews[i];
        so.FindProperty("rerollButton").objectReferenceValue = reroll;
        so.FindProperty("rerollLabel").objectReferenceValue = rerollText;
        so.FindProperty("startButton").objectReferenceValue = start;
        so.FindProperty("backButton").objectReferenceValue = back;
        so.FindProperty("messageText").objectReferenceValue = message;
        so.FindProperty("layoutVersion").intValue = V02OperationPrepScreen.CurrentLayout;
        so.ApplyModifiedPropertiesWithoutUndo();
        return screen;
    }

    static V02OperationPrepCard BuildCard(Transform parent, string name)
    {
        RectTransform root = V02Ui.CreatePanel(name, parent, Outline);
        Image chrome = root.GetComponent<Image>();
        Button button = root.gameObject.AddComponent<Button>();
        button.targetGraphic = chrome;

        RectTransform selected = V02Ui.CreateStretch("Selected", root);
        Image selectedImage = selected.gameObject.AddComponent<Image>();
        selectedImage.color = new Color(0.98f, 0.82f, 0.28f, 1f);
        selectedImage.raycastTarget = false;
        V02Ui.SetAnchors(selected, new Vector2(-0.02f, -0.015f), new Vector2(1.02f, 1.015f), Vector2.zero, Vector2.zero);
        selected.gameObject.SetActive(false);

        RectTransform face = V02Ui.CreateStretch("Face", root);
        Image faceImage = face.gameObject.AddComponent<Image>();
        faceImage.color = Vanilla;
        faceImage.raycastTarget = false;
        V02Ui.SetAnchors(face, new Vector2(0.018f, 0.012f), new Vector2(0.982f, 0.988f), Vector2.zero, Vector2.zero);

        RectTransform art = V02Ui.CreateStretch("Background", face);
        Image artImage = art.gameObject.AddComponent<Image>();
        artImage.color = new Color(0.93f, 0.86f, 0.78f, 1f);
        artImage.raycastTarget = false;
        V02Ui.SetAnchors(art, new Vector2(0.04f, 0.46f), new Vector2(0.96f, 0.97f), Vector2.zero, Vector2.zero);

        TextMeshProUGUI placeholder = V02Ui.CreateText("Placeholder", art, "배경", 28f, V02Ui.Ink, TextAlignmentOptions.Center);
        V02Ui.Stretch(placeholder.rectTransform);
        placeholder.raycastTarget = false;

        TextMeshProUGUI shopName = AddLabel(face, "Name", "가게 이름", 38f, new Vector2(0.05f, 0.355f), new Vector2(0.62f, 0.45f), TextAlignmentOptions.Left);
        shopName.textWrappingMode = TextWrappingModes.NoWrap;
        shopName.overflowMode = TextOverflowModes.Ellipsis;

        RectTransform check = V02Ui.CreateStretch("Check", face);
        Image checkImage = check.gameObject.AddComponent<Image>();
        checkImage.color = new Color(0.45f, 0.72f, 0.42f, 1f);
        checkImage.raycastTarget = false;
        V02Ui.SetAnchors(check, new Vector2(0.84f, 0.90f), new Vector2(0.96f, 0.97f), Vector2.zero, Vector2.zero);
        TextMeshProUGUI checkLabel = V02Ui.CreateText("Mark", check, "✓", 28f, Color.white, TextAlignmentOptions.Center);
        V02Ui.Stretch(checkLabel.rectTransform);
        checkLabel.raycastTarget = false;
        check.gameObject.SetActive(false);

        RectTransform risk = V02Ui.CreateStretch("Risk", face);
        GameObject[] pips = BuildRisk(risk);
        risk.gameObject.SetActive(false);
        TextMeshProUGUI distance = BuildInfo(face, "Distance", "거리", Sky, 0.04f, 0.27f);
        TextMeshProUGUI scale = BuildInfo(face, "Scale", "규모", new Color(0.95f, 0.72f, 0.45f, 1f), 0.27f, 0.50f);
        TextMeshProUGUI security = BuildInfo(face, "Security", "경비", new Color(0.85f, 0.45f, 0.48f, 1f), 0.50f, 0.73f);
        TextMeshProUGUI goods = BuildInfo(face, "Goods", "상품", V02Ui.Mint, 0.73f, 0.96f);

        TextMeshProUGUI rumor0 = AddLabel(face, "Rumor0", "", 22f, new Vector2(0.06f, 0.135f), new Vector2(0.94f, 0.195f), TextAlignmentOptions.Left);
        TextMeshProUGUI rumor1 = AddLabel(face, "Rumor1", "", 22f, new Vector2(0.06f, 0.075f), new Vector2(0.94f, 0.135f), TextAlignmentOptions.Left);
        rumor0.overflowMode = TextOverflowModes.Ellipsis;
        rumor1.overflowMode = TextOverflowModes.Ellipsis;
        rumor0.textWrappingMode = TextWrappingModes.NoWrap;
        rumor1.textWrappingMode = TextWrappingModes.NoWrap;

        TextMeshProUGUI empty = AddLabel(face, "EmptyRumor", V02OperationPrepCard.EmptyRumorLabel, 22f, new Vector2(0.06f, 0.075f), new Vector2(0.94f, 0.195f), TextAlignmentOptions.Left);
        empty.color = new Color(0.45f, 0.40f, 0.38f, 1f);

        RectTransform energyIcon = V02Ui.CreateStretch("EnergyIcon", face);
        Image energyImage = energyIcon.gameObject.AddComponent<Image>();
        energyImage.color = new Color(0.85f, 0.55f, 0.25f, 1f);
        energyImage.raycastTarget = false;
        V02Ui.SetAnchors(energyIcon, new Vector2(0.06f, 0.025f), new Vector2(0.14f, 0.075f), Vector2.zero, Vector2.zero);
        TextMeshProUGUI energy = AddLabel(face, "EnergyValue", "0", 28f, new Vector2(0.16f, 0.015f), new Vector2(0.50f, 0.07f), TextAlignmentOptions.Left);

        V02OperationPrepCard card = root.gameObject.AddComponent<V02OperationPrepCard>();
        SerializedObject so = new SerializedObject(card);
        so.FindProperty("background").objectReferenceValue = artImage;
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
        SerializedProperty filled = so.FindProperty("riskFilled");
        filled.arraySize = pips.Length;
        for (int i = 0; i < pips.Length; i++)
            filled.GetArrayElementAtIndex(i).objectReferenceValue = pips[i];
        so.ApplyModifiedPropertiesWithoutUndo();
        return card;
    }

    static GameObject[] BuildRisk(Transform parent)
    {
        TextMeshProUGUI caption = AddLabel(parent, "RiskCaption", "위험도", 18f, new Vector2(0.62f, 0.40f), new Vector2(0.96f, 0.445f), TextAlignmentOptions.Right);
        caption.color = new Color(0.35f, 0.28f, 0.30f, 1f);

        var ons = new GameObject[3];
        for (int i = 0; i < 3; i++)
        {
            float x0 = 0.64f + i * 0.11f;
            RectTransform pip = V02Ui.CreateStretch("Pip" + i, parent);
            Image frame = pip.gameObject.AddComponent<Image>();
            frame.color = PipOff;
            frame.raycastTarget = false;
            V02Ui.SetAnchors(pip, new Vector2(x0, 0.355f), new Vector2(x0 + 0.09f, 0.40f), Vector2.zero, Vector2.zero);

            RectTransform on = V02Ui.CreateStretch("On", pip);
            Image onImage = on.gameObject.AddComponent<Image>();
            onImage.color = PipOn;
            onImage.raycastTarget = false;
            V02Ui.SetAnchors(on, new Vector2(0.15f, 0.15f), new Vector2(0.85f, 0.85f), Vector2.zero, Vector2.zero);
            on.gameObject.SetActive(false);
            ons[i] = on.gameObject;
        }

        return ons;
    }

    static TextMeshProUGUI BuildInfo(Transform parent, string name, string caption, Color iconColor, float x0, float x1)
    {
        RectTransform column = V02Ui.CreateStretch(name, parent);
        V02Ui.SetAnchors(column, new Vector2(x0, 0.20f), new Vector2(x1, 0.35f), Vector2.zero, Vector2.zero);

        TextMeshProUGUI cap = V02Ui.CreateText("Caption", column, caption, 15f, new Color(0.40f, 0.34f, 0.36f, 1f), TextAlignmentOptions.Center);
        cap.raycastTarget = false;
        V02Ui.SetAnchors(cap.rectTransform, new Vector2(0f, 0.78f), new Vector2(1f, 1f), Vector2.zero, Vector2.zero);

        RectTransform icon = V02Ui.CreateStretch("Icon", column);
        Image image = icon.gameObject.AddComponent<Image>();
        image.color = iconColor;
        image.raycastTarget = false;
        V02Ui.SetAnchors(icon, new Vector2(0.38f, 0.52f), new Vector2(0.62f, 0.76f), Vector2.zero, Vector2.zero);

        TextMeshProUGUI value = V02Ui.CreateText("Value", column, "—", 26f, V02Ui.Ink, TextAlignmentOptions.Center);
        value.raycastTarget = false;
        value.overflowMode = TextOverflowModes.Ellipsis;
        value.textWrappingMode = TextWrappingModes.NoWrap;
        V02Ui.SetAnchors(value.rectTransform, new Vector2(0f, 0f), new Vector2(1f, 0.50f), Vector2.zero, Vector2.zero);
        return value;
    }

    static TextMeshProUGUI AddLabel(Transform parent, string name, string text, float size, Vector2 min, Vector2 max, TextAlignmentOptions align)
    {
        TextMeshProUGUI label = V02Ui.CreateText(name, parent, text, size, V02Ui.Ink, align);
        label.raycastTarget = false;
        V02Ui.SetAnchors(label.rectTransform, min, max, Vector2.zero, Vector2.zero);
        return label;
    }
}
