using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

public static class V02RunResultPopupBuilder
{
    const string Folder = "Assets/Resources/V02/UI";
    const string Path = Folder + "/RunResultPopup.prefab";

    [MenuItem("Candy Thief/V02/Bake Run Result Popup (if missing)")]
    public static void BakeIfMissing()
    {
        if (AssetDatabase.LoadAssetAtPath<V02RunResultPopup>(Path) != null)
            return;
        Bake();
    }

    [MenuItem("Candy Thief/V02/Rebuild Run Result Popup")]
    public static void Bake()
    {
        if (!AssetDatabase.IsValidFolder("Assets/Resources"))
            AssetDatabase.CreateFolder("Assets", "Resources");
        if (!AssetDatabase.IsValidFolder("Assets/Resources/V02"))
            AssetDatabase.CreateFolder("Assets/Resources", "V02");
        if (!AssetDatabase.IsValidFolder(Folder))
            AssetDatabase.CreateFolder("Assets/Resources/V02", "UI");

        V02RunResultPopup popup = Build();
        PrefabUtility.SaveAsPrefabAsset(popup.gameObject, Path);
        Object.DestroyImmediate(popup.gameObject);
        AssetDatabase.SaveAssets();
        Debug.Log("[V02] 작전 결과 프리팹 저장: " + Path);
    }

    public static V02RunResultPopup Build()
    {
        RectTransform root = V02Ui.CreatePanel("RunResultPopup", null, new Color(0.55f, 0.86f, 0.62f, 1f));
        root.sizeDelta = new Vector2(V02Ui.RefWidth, V02Ui.RefHeight);
        V02RunResultPopup popup = root.gameObject.AddComponent<V02RunResultPopup>();

        RectTransform frame = V02Ui.CreatePanel("Frame", root, new Color(0.86f, 0.96f, 0.84f, 1f));
        V02Ui.SetAnchors(frame, Vector2.zero, Vector2.one, new Vector2(60f, 70f), new Vector2(-60f, -50f));

        Image decor = CreateImage("Decor", frame, new Color(1f, 0.93f, 0.55f, 0.9f));
        V02Ui.SetAnchors((RectTransform)decor.transform, new Vector2(0.08f, 0.90f), new Vector2(0.92f, 0.99f), Vector2.zero, Vector2.zero);
        Image starLeft = CreateImage("StarLeft", decor.transform, new Color(1f, 0.86f, 0.28f, 1f));
        V02Ui.SetAnchors((RectTransform)starLeft.transform, new Vector2(0.02f, 0.15f), new Vector2(0.16f, 0.85f), Vector2.zero, Vector2.zero);
        Image starRight = CreateImage("StarRight", decor.transform, new Color(1f, 0.86f, 0.28f, 1f));
        V02Ui.SetAnchors((RectTransform)starRight.transform, new Vector2(0.84f, 0.15f), new Vector2(0.98f, 0.85f), Vector2.zero, Vector2.zero);

        TextMeshProUGUI title = V02Ui.CreateText("Title", frame, "작전 성공!", 72f, V02Ui.Ink, TextAlignmentOptions.Center);
        V02Ui.SetAnchors(title.rectTransform, new Vector2(0.06f, 0.915f), new Vector2(0.94f, 0.985f), Vector2.zero, Vector2.zero);
        TextMeshProUGUI subtitle = V02Ui.CreateText("Subtitle", frame, "사탕을 왕창 훔쳤어요!", 34f, V02Ui.Ink, TextAlignmentOptions.Center);
        V02Ui.SetAnchors(subtitle.rectTransform, new Vector2(0.08f, 0.855f), new Vector2(0.92f, 0.91f), Vector2.zero, Vector2.zero);

        Image character = CreateImage("Character", frame, new Color(0.95f, 0.86f, 0.55f, 1f));
        V02Ui.SetAnchors((RectTransform)character.transform, new Vector2(0.22f, 0.60f), new Vector2(0.78f, 0.84f), Vector2.zero, Vector2.zero);
        character.preserveAspect = true;
        TextMeshProUGUI placeholder = V02Ui.CreateText("Placeholder", character.transform, "도둑", 42f, V02Ui.Ink, TextAlignmentOptions.Center);
        V02Ui.Stretch(placeholder.rectTransform);

        Image messagePlate = CreateImage("MessagePlate", frame, new Color(1f, 0.98f, 0.94f, 0.96f));
        V02Ui.SetAnchors((RectTransform)messagePlate.transform, new Vector2(0.08f, 0.50f), new Vector2(0.92f, 0.59f), Vector2.zero, Vector2.zero);
        TextMeshProUGUI message = V02Ui.CreateText("Message", messagePlate.transform, "가게의 사탕을 잔뜩 챙겨 무사히 탈출했어요!", 30f, V02Ui.Ink, TextAlignmentOptions.Center);
        V02Ui.SetAnchors(message.rectTransform, new Vector2(0.06f, 0.08f), new Vector2(0.94f, 0.92f), Vector2.zero, Vector2.zero);

        Image stats = CreateImage("Stats", frame, new Color(1f, 0.97f, 0.90f, 1f));
        V02Ui.SetAnchors((RectTransform)stats.transform, new Vector2(0.06f, 0.355f), new Vector2(0.94f, 0.49f), Vector2.zero, Vector2.zero);
        TextMeshProUGUI acquired = AddStat(stats.transform, "Acquired", "획득 사탕", "+0", 0.02f, 0.34f);
        TextMeshProUGUI settled = AddStat(stats.transform, "Settled", "정산 사탕", "+0", 0.34f, 0.66f);
        TextMeshProUGUI alert = AddStat(stats.transform, "Alert", "최종 위험도", "0/100", 0.66f, 0.98f);

        RectTransform loot = V02Ui.CreatePanel("Loot", frame, new Color(0.98f, 0.94f, 0.84f, 1f));
        V02Ui.SetAnchors(loot, new Vector2(0.06f, 0.20f), new Vector2(0.94f, 0.345f), Vector2.zero, Vector2.zero);
        TextMeshProUGUI lootTitle = V02Ui.CreateText("LootTitle", loot, "추가 전리품", 26f, V02Ui.Ink, TextAlignmentOptions.Center);
        V02Ui.SetAnchors(lootTitle.rectTransform, new Vector2(0.04f, 0.72f), new Vector2(0.96f, 0.96f), Vector2.zero, Vector2.zero);
        TextMeshProUGUI empty = V02Ui.CreateText("Empty", loot, V02RunResultPopup.EmptyLootCopy, 26f, V02Ui.Ink, TextAlignmentOptions.Center);
        V02Ui.SetAnchors(empty.rectTransform, new Vector2(0.06f, 0.08f), new Vector2(0.94f, 0.70f), Vector2.zero, Vector2.zero);

        ScrollRect scroll = loot.gameObject.AddComponent<ScrollRect>();
        scroll.horizontal = true;
        scroll.vertical = false;
        RectTransform viewport = V02Ui.CreatePanel("Viewport", loot, new Color(1f, 1f, 1f, 0f));
        V02Ui.SetAnchors(viewport, new Vector2(0.04f, 0.06f), new Vector2(0.96f, 0.70f), Vector2.zero, Vector2.zero);
        viewport.gameObject.AddComponent<RectMask2D>();
        RectTransform content = V02Ui.CreateStretch("Content", viewport);
        HorizontalLayoutGroup row = content.gameObject.AddComponent<HorizontalLayoutGroup>();
        row.spacing = 16f;
        row.childAlignment = TextAnchor.MiddleLeft;
        row.childControlWidth = false;
        row.childControlHeight = true;
        row.childForceExpandWidth = false;
        row.childForceExpandHeight = true;
        ContentSizeFitter fit = content.gameObject.AddComponent<ContentSizeFitter>();
        fit.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
        scroll.viewport = viewport;
        scroll.content = content;
        for (int i = 0; i < 3; i++)
            CreateSlot(content, i).gameObject.SetActive(false);

        TextMeshProUGUI turns = V02Ui.CreateText("Turns", frame, "남은 턴: 0턴", 28f, V02Ui.Ink, TextAlignmentOptions.Center);
        V02Ui.SetAnchors(turns.rectTransform, new Vector2(0.08f, 0.145f), new Vector2(0.92f, 0.195f), Vector2.zero, Vector2.zero);

        Button retry = V02Ui.CreateButton("Retry", frame, "재도전", V02Ui.Yellow, null);
        V02Ui.SetAnchors((RectTransform)retry.transform, new Vector2(0.06f, 0.025f), new Vector2(0.62f, 0.13f), Vector2.zero, Vector2.zero);
        Button lobby = V02Ui.CreateButton("Lobby", frame, "로비로", V02Ui.White, null);
        V02Ui.SetAnchors((RectTransform)lobby.transform, new Vector2(0.66f, 0.025f), new Vector2(0.94f, 0.13f), Vector2.zero, Vector2.zero);

        SerializedObject serialized = new SerializedObject(popup);
        Set(serialized, "backdrop", root.GetComponent<Image>());
        Set(serialized, "frame", frame.GetComponent<Image>());
        Set(serialized, "decor", decor);
        Set(serialized, "titleText", title);
        Set(serialized, "subtitleText", subtitle);
        Set(serialized, "characterImage", character);
        Set(serialized, "characterPlaceholder", placeholder);
        Set(serialized, "messagePlate", messagePlate);
        Set(serialized, "messageText", message);
        Set(serialized, "statsPlate", stats);
        Set(serialized, "acquiredValue", acquired);
        Set(serialized, "settledValue", settled);
        Set(serialized, "alertValue", alert);
        Set(serialized, "turnsText", turns);
        Set(serialized, "lootContent", content);
        Set(serialized, "emptyLootText", empty);
        Set(serialized, "retryButton", retry);
        Set(serialized, "lobbyButton", lobby);
        serialized.ApplyModifiedPropertiesWithoutUndo();
        return popup;
    }

    static TextMeshProUGUI AddStat(Transform parent, string name, string caption, string value, float minX, float maxX)
    {
        RectTransform column = V02Ui.CreatePanel(name, parent, new Color(1f, 1f, 1f, 0.35f));
        V02Ui.SetAnchors(column, new Vector2(minX, 0.08f), new Vector2(maxX, 0.92f), Vector2.zero, Vector2.zero);
        Image icon = CreateImage("Icon", column, new Color(0.95f, 0.55f, 0.45f, 1f));
        V02Ui.SetAnchors((RectTransform)icon.transform, new Vector2(0.32f, 0.62f), new Vector2(0.68f, 0.92f), Vector2.zero, Vector2.zero);
        icon.preserveAspect = true;
        TextMeshProUGUI label = V02Ui.CreateText("Caption", column, caption, 22f, V02Ui.Ink, TextAlignmentOptions.Center);
        V02Ui.SetAnchors(label.rectTransform, new Vector2(0.04f, 0.34f), new Vector2(0.96f, 0.58f), Vector2.zero, Vector2.zero);
        TextMeshProUGUI number = V02Ui.CreateText("Value", column, value, 32f, V02Ui.Ink, TextAlignmentOptions.Center);
        V02Ui.SetAnchors(number.rectTransform, new Vector2(0.04f, 0.06f), new Vector2(0.96f, 0.34f), Vector2.zero, Vector2.zero);
        return number;
    }

    static V02RunLootSlot CreateSlot(Transform parent, int index)
    {
        RectTransform plate = V02Ui.CreatePanel("Slot" + index, parent, new Color(1f, 0.98f, 0.92f, 1f));
        LayoutElement layout = plate.gameObject.AddComponent<LayoutElement>();
        layout.preferredWidth = 180f;
        layout.preferredHeight = 140f;
        V02RunLootSlot slot = plate.gameObject.AddComponent<V02RunLootSlot>();
        Image icon = CreateImage("Icon", plate, new Color(0.85f, 0.7f, 0.4f, 1f));
        V02Ui.SetAnchors((RectTransform)icon.transform, new Vector2(0.3f, 0.48f), new Vector2(0.7f, 0.92f), Vector2.zero, Vector2.zero);
        TextMeshProUGUI name = V02Ui.CreateText("Name", plate, "전리품", 20f, V02Ui.Ink, TextAlignmentOptions.Center);
        V02Ui.SetAnchors(name.rectTransform, new Vector2(0.04f, 0.24f), new Vector2(0.96f, 0.46f), Vector2.zero, Vector2.zero);
        TextMeshProUGUI count = V02Ui.CreateText("Count", plate, "×0", 20f, V02Ui.Ink, TextAlignmentOptions.Center);
        V02Ui.SetAnchors(count.rectTransform, new Vector2(0.04f, 0.04f), new Vector2(0.5f, 0.24f), Vector2.zero, Vector2.zero);
        TextMeshProUGUI kept = V02Ui.CreateText("Kept", plate, "획득", 18f, V02Ui.Ink, TextAlignmentOptions.Center);
        V02Ui.SetAnchors(kept.rectTransform, new Vector2(0.5f, 0.04f), new Vector2(0.96f, 0.24f), Vector2.zero, Vector2.zero);
        SerializedObject serialized = new SerializedObject(slot);
        Set(serialized, "icon", icon);
        Set(serialized, "nameText", name);
        Set(serialized, "countText", count);
        Set(serialized, "keptText", kept);
        serialized.ApplyModifiedPropertiesWithoutUndo();
        return slot;
    }

    static Image CreateImage(string name, Transform parent, Color color)
    {
        RectTransform rect = V02Ui.CreatePanel(name, parent, color);
        return rect.GetComponent<Image>();
    }

    static void Set(SerializedObject serialized, string property, Object value)
    {
        SerializedProperty prop = serialized.FindProperty(property);
        if (prop != null)
            prop.objectReferenceValue = value;
    }
}
