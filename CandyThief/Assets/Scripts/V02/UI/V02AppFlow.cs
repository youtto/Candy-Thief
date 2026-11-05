using System.Collections;
using System.Globalization;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;

public class V02AppFlow : MonoBehaviour
{
    const string TitleSpritePath = "Assets/Art/Title/CT_Title_A_v001.png";
    const string LobbySpritePath = "Assets/Art/Lobby/CT_Lobby_B_Background_v001.png";
    const string PlayerSpritePath = "Assets/Art/Lobby/CT_Player_Idle_v001.png";

    [SerializeField] float titleMinSeconds = 1f;
    [SerializeField] float fadeSeconds = 0.3f;
    [SerializeField] Sprite titleBackgroundSprite;
    [SerializeField] Sprite lobbyBackgroundSprite;
    [SerializeField] Sprite playerIdleSprite;
    [SerializeField] TMP_FontAsset koreanFont;

    RectTransform titleScreen;
    RectTransform lobbyScreen;
    RectTransform shopScreen;
    RectTransform debugScreen;
    RectTransform comingSoon;
    RectTransform currentScreen;
    TextMeshProUGUI comingSoonText;
    TextMeshProUGUI titleStatus;
    TextMeshProUGUI debugBody;
    Transform shopList;

    void Awake()
    {
        EnsureEventSystem();
        EnsureCamera();
        ResolveArt();
        V02Ui.UseFont(koreanFont);
        BuildUi();
        Show(titleScreen);
    }

    IEnumerator Start()
    {
        float elapsed = 0f;
        bool ready = false;
        while (elapsed < titleMinSeconds || !ready)
        {
            elapsed += Time.unscaledDeltaTime;
            CsvDatabase database = CsvDatabase.Current;
            if (database == null || !database.Loaded)
            {
                database = CsvDatabase.LoadFromResources();
                SetTitleStatus("로비 준비 중…");
            }

            if (database != null && database.Loaded && !database.HasErrors)
            {
                ready = true;
                SetTitleStatus("입장 중…");
            }
            else if (database != null && database.HasErrors)
            {
                SetTitleStatus("데이터 로딩 실패. CSV를 확인하세요.");
                Debug.LogError("[V02] CSV 로딩 실패. 타이틀에 머무릅니다.");
                yield break;
            }

            if (elapsed < titleMinSeconds || !ready)
                yield return null;
        }

        yield return FadeTo(lobbyScreen);
    }

    void EnsureCamera()
    {
        if (Camera.main != null)
            return;

        var go = new GameObject("Main Camera");
        go.tag = "MainCamera";
        Camera camera = go.AddComponent<Camera>();
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color(0.50f, 0.80f, 0.94f);
        camera.orthographic = true;
        camera.orthographicSize = 5f;
        go.AddComponent<UniversalAdditionalCameraData>();
    }

    void EnsureEventSystem()
    {
        if (FindAnyObjectByType<EventSystem>() != null)
            return;

        var go = new GameObject("EventSystem");
        go.AddComponent<EventSystem>();
        go.AddComponent<InputSystemUIInputModule>();
    }

    void BuildUi()
    {
        if (transform.Find("Canvas") != null)
            return;

        var canvasGo = new GameObject("Canvas", typeof(RectTransform));
        canvasGo.transform.SetParent(transform, false);
        Canvas canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.pixelPerfect = false;
        CanvasScaler scaler = canvasGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(V02Ui.RefWidth, V02Ui.RefHeight);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;
        canvasGo.AddComponent<GraphicRaycaster>();

        RectTransform safe = V02Ui.CreateStretch("SafeArea", canvasGo.transform);
        safe.gameObject.AddComponent<SafeAreaFitter>();

        titleScreen = BuildTitle(safe);
        lobbyScreen = BuildLobby(safe);
        shopScreen = BuildShopSelect(safe);
        debugScreen = BuildDebug(safe);
        comingSoon = BuildComingSoon(safe);
        comingSoon.gameObject.SetActive(false);
    }

    RectTransform BuildTitle(Transform parent)
    {
        RectTransform root = V02Ui.CreatePanel("Title", parent, new Color(0.50f, 0.80f, 0.94f));
        Image bg = root.GetComponent<Image>();
        V02Ui.ApplySprite(bg, titleBackgroundSprite, true, false);
        V02Ui.AddCanvasGroup(root);

        titleStatus = V02Ui.CreateText(
            "Status", root, "로딩 중…", 28f, V02Ui.Ink, TextAlignmentOptions.Center);
        V02Ui.SetAnchors(titleStatus.rectTransform, new Vector2(0.1f, 0.04f), new Vector2(0.9f, 0.12f), Vector2.zero, Vector2.zero);
        return root;
    }

    RectTransform BuildLobby(Transform parent)
    {
        RectTransform root = V02Ui.CreatePanel("Lobby", parent, new Color(0.42f, 0.41f, 0.47f));
        Image bg = root.GetComponent<Image>();
        V02Ui.ApplySprite(bg, lobbyBackgroundSprite, true, false);
        V02Ui.AddCanvasGroup(root);

        RectTransform top = V02Ui.CreatePanel("TopBar", root, V02Ui.White);
        V02Ui.SetAnchors(top, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(24f, -160f), new Vector2(-24f, -24f));

        Button profile = V02Ui.CreateButton("Profile", top, "도둑", V02Ui.Pink, () => ShowComingSoon("프로필"));
        V02Ui.SetAnchors((RectTransform)profile.transform, new Vector2(0f, 0.1f), new Vector2(0.28f, 0.9f), Vector2.zero, Vector2.zero);

        Button energy = V02Ui.CreateButton("Energy", top, "에너지  —", V02Ui.Yellow, () => ShowComingSoon("에너지"));
        V02Ui.SetAnchors((RectTransform)energy.transform, new Vector2(0.30f, 0.1f), new Vector2(0.48f, 0.9f), Vector2.zero, Vector2.zero);

        Button gold = V02Ui.CreateButton("Gold", top, "골드  —", V02Ui.Yellow, () => ShowComingSoon("골드"));
        V02Ui.SetAnchors((RectTransform)gold.transform, new Vector2(0.50f, 0.1f), new Vector2(0.68f, 0.9f), Vector2.zero, Vector2.zero);

        Button goldCandy = V02Ui.CreateButton("GoldCandy", top, "골드사탕  —", V02Ui.Yellow, () => ShowComingSoon("골드 사탕"));
        V02Ui.SetAnchors((RectTransform)goldCandy.transform, new Vector2(0.70f, 0.1f), new Vector2(0.86f, 0.9f), Vector2.zero, Vector2.zero);

        Button settings = V02Ui.CreateButton("Settings", top, "설정", V02Ui.Purple, () => ShowComingSoon("설정"));
        V02Ui.SetAnchors((RectTransform)settings.transform, new Vector2(0.88f, 0.1f), new Vector2(1f, 0.9f), Vector2.zero, Vector2.zero);

        if (playerIdleSprite != null)
        {
            RectTransform player = V02Ui.CreateStretch("Player", root);
            Image playerImage = player.gameObject.AddComponent<Image>();
            V02Ui.ApplySprite(playerImage, playerIdleSprite, true, false);
            V02Ui.SetAnchors(player, new Vector2(0.5f, 0.33f), new Vector2(0.5f, 0.33f), new Vector2(-170f, 0f), new Vector2(170f, 300f));
            player.gameObject.AddComponent<V02IdleBob>();
        }

        Button slotA = V02Ui.CreateButton("AllySlotA", root, "+", V02Ui.White, () => ShowComingSoon("조력자"));
        V02Ui.SetAnchors((RectTransform)slotA.transform, new Vector2(0.16f, 0.46f), new Vector2(0.16f, 0.46f), new Vector2(0f, 0f), new Vector2(120f, 120f));

        Button slotB = V02Ui.CreateButton("AllySlotB", root, "+", V02Ui.White, () => ShowComingSoon("조력자"));
        V02Ui.SetAnchors((RectTransform)slotB.transform, new Vector2(0.84f, 0.46f), new Vector2(0.84f, 0.46f), new Vector2(-120f, 0f), new Vector2(0f, 120f));

        Button start = V02Ui.CreateButton("StartHeist", root, "도둑질 시작!", V02Ui.Pink, ShowShopSelect);
        V02Ui.SetAnchors((RectTransform)start.transform, new Vector2(0.1f, 0.16f), new Vector2(0.9f, 0.26f), Vector2.zero, Vector2.zero);
        start.GetComponentInChildren<TextMeshProUGUI>().fontSize = 48f;

        RectTransform bottom = V02Ui.CreatePanel("BottomBar", root, V02Ui.White);
        V02Ui.SetAnchors(bottom, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(16f, 16f), new Vector2(-16f, 150f));

        string[] menus = { "캐릭터", "뽑기", "도감", "상점", "미션" };
        for (int i = 0; i < menus.Length; i++)
        {
            string menuName = menus[i];
            Button item = V02Ui.CreateButton("Menu" + i, bottom, menuName, V02Ui.Mint, () => ShowComingSoon(menuName));
            float min = i / 5f;
            float max = (i + 1) / 5f;
            V02Ui.SetAnchors((RectTransform)item.transform, new Vector2(min, 0.08f), new Vector2(max, 0.92f), new Vector2(8f, 0f), new Vector2(-8f, 0f));
        }

        return root;
    }

    RectTransform BuildShopSelect(Transform parent)
    {
        RectTransform root = V02Ui.CreatePanel("ShopSelect", parent, V02Ui.Yellow);
        V02Ui.AddCanvasGroup(root);

        Button back = V02Ui.CreateButton("Back", root, "로비", V02Ui.White, ShowLobby);
        V02Ui.SetAnchors((RectTransform)back.transform, new Vector2(0.04f, 0.91f), new Vector2(0.28f, 0.98f), Vector2.zero, Vector2.zero);

        TextMeshProUGUI header = V02Ui.CreateText(
            "Header", root, "가게 선택", 48f, V02Ui.Ink, TextAlignmentOptions.Center);
        V02Ui.SetAnchors(header.rectTransform, new Vector2(0.30f, 0.91f), new Vector2(0.96f, 0.98f), Vector2.zero, Vector2.zero);

        var scrollGo = new GameObject("ShopScroll", typeof(RectTransform));
        scrollGo.transform.SetParent(root, false);
        RectTransform scrollRect = (RectTransform)scrollGo.transform;
        V02Ui.SetAnchors(scrollRect, new Vector2(0.06f, 0.04f), new Vector2(0.94f, 0.88f), Vector2.zero, Vector2.zero);
        Image scrollImage = scrollGo.AddComponent<Image>();
        scrollImage.color = new Color(1f, 1f, 1f, 0.15f);
        ScrollRect scroll = scrollGo.AddComponent<ScrollRect>();
        scroll.horizontal = false;
        scroll.vertical = true;
        scroll.movementType = ScrollRect.MovementType.Elastic;

        RectTransform viewport = V02Ui.CreateStretch("Viewport", scrollRect);
        viewport.gameObject.AddComponent<RectMask2D>();
        scroll.viewport = viewport;

        shopList = V02Ui.CreateStretch("Content", viewport);
        VerticalLayoutGroup layout = shopList.gameObject.AddComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(12, 12, 12, 12);
        layout.spacing = 24f;
        layout.childAlignment = TextAnchor.UpperCenter;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;
        ContentSizeFitter fitter = shopList.gameObject.AddComponent<ContentSizeFitter>();
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        scroll.content = (RectTransform)shopList;
        return root;
    }

    RectTransform BuildDebug(Transform parent)
    {
        RectTransform root = V02Ui.CreatePanel("RunDebug", parent, V02Ui.Purple);
        V02Ui.AddCanvasGroup(root);

        TextMeshProUGUI header = V02Ui.CreateText(
            "Header", root, "런 시작 확인 (임시)", 44f, V02Ui.Ink, TextAlignmentOptions.Center);
        V02Ui.SetAnchors(header.rectTransform, new Vector2(0.08f, 0.86f), new Vector2(0.92f, 0.96f), Vector2.zero, Vector2.zero);

        debugBody = V02Ui.CreateText(
            "Body", root, "", 34f, V02Ui.Ink, TextAlignmentOptions.TopLeft);
        V02Ui.SetAnchors(debugBody.rectTransform, new Vector2(0.1f, 0.22f), new Vector2(0.9f, 0.84f), Vector2.zero, Vector2.zero);

        Button lobby = V02Ui.CreateButton("BackLobby", root, "로비로 돌아가기", V02Ui.White, ShowLobby);
        V02Ui.SetAnchors((RectTransform)lobby.transform, new Vector2(0.1f, 0.06f), new Vector2(0.9f, 0.16f), Vector2.zero, Vector2.zero);
        return root;
    }

    RectTransform BuildComingSoon(Transform parent)
    {
        RectTransform root = V02Ui.CreatePanel("ComingSoon", parent, new Color(0.12f, 0.10f, 0.16f, 0.72f));
        RectTransform card = V02Ui.CreatePanel("Card", root, V02Ui.White);
        V02Ui.SetAnchors(card, new Vector2(0.12f, 0.36f), new Vector2(0.88f, 0.64f), Vector2.zero, Vector2.zero);

        comingSoonText = V02Ui.CreateText(
            "Message", card, "준비 중", 40f, V02Ui.Ink, TextAlignmentOptions.Center);
        V02Ui.SetAnchors(comingSoonText.rectTransform, new Vector2(0.08f, 0.38f), new Vector2(0.92f, 0.92f), Vector2.zero, Vector2.zero);

        Button ok = V02Ui.CreateButton("Ok", card, "확인", V02Ui.Mint, HideComingSoon);
        V02Ui.SetAnchors((RectTransform)ok.transform, new Vector2(0.2f, 0.08f), new Vector2(0.8f, 0.32f), Vector2.zero, Vector2.zero);
        return root;
    }

    void ShowLobby()
    {
        HideComingSoon();
        Show(lobbyScreen);
    }

    void ShowShopSelect()
    {
        HideComingSoon();
        RebuildShopCards();
        Show(shopScreen);
    }

    void RebuildShopCards()
    {
        for (int i = shopList.childCount - 1; i >= 0; i--)
            Destroy(shopList.GetChild(i).gameObject);

        CsvDatabase database = CsvDatabase.Current;
        if (database == null || !database.Loaded || database.HasErrors)
        {
            database = CsvDatabase.LoadFromResources();
            if (database == null || database.HasErrors)
            {
                ShowComingSoon("가게 데이터");
                return;
            }
        }

        var shops = database.EnabledShops;
        for (int i = 0; i < shops.Count; i++)
            CreateShopCard(shops[i]);
    }

    void CreateShopCard(ShopRow shop)
    {
        string shopId = shop.ShopID;
        Button card = V02Ui.CreateButton("Card_" + shopId, shopList, "", V02Ui.White, () => SelectShop(shopId));
        LayoutElement layout = card.gameObject.AddComponent<LayoutElement>();
        layout.minHeight = 260f;
        layout.preferredHeight = 280f;

        TextMeshProUGUI label = card.GetComponentInChildren<TextMeshProUGUI>();
        label.alignment = TextAlignmentOptions.Left;
        label.fontSize = 32f;
        label.text = shop.ShopName + "\n"
            + "난이도 " + shop.Difficulty
            + "  ·  시작 턴 " + shop.StartTurn
            + "  ·  시작 경계도 " + shop.StartAlert
            + "\n사탕 보상 배율 ×"
            + shop.CandyMultiplier.ToString("0.0", CultureInfo.InvariantCulture);
        RectTransform textRect = label.rectTransform;
        V02Ui.SetAnchors(textRect, Vector2.zero, Vector2.one, new Vector2(28f, 16f), new Vector2(-28f, -16f));
    }

    void SelectShop(string shopId)
    {
        CsvDatabase database = CsvDatabase.Current;
        if (database == null || !database.Loaded)
            database = CsvDatabase.LoadFromResources();

        RunState run;
        if (database == null || !RunState.TryStart(database, shopId, out run))
        {
            ShowComingSoon("런 시작 실패");
            return;
        }

        debugBody.text =
            "ShopID: " + run.ShopID + "\n"
            + "Phase: " + run.CurrentPhase + "\n"
            + "Turns: " + run.RemainingTurns + "\n"
            + "Alert: " + run.Alert + "\n"
            + "Candy: " + run.Candy + "\n"
            + "Status: " + run.Status + "\n\n"
            + "게임플레이 화면은 아직 없습니다.";
        run.LogSnapshot("shop selected");
        Show(debugScreen);
    }

    void ShowComingSoon(string featureName)
    {
        comingSoonText.text = featureName + "\n준비 중";
        comingSoon.gameObject.SetActive(true);
        comingSoon.SetAsLastSibling();
    }

    void HideComingSoon()
    {
        if (comingSoon != null)
            comingSoon.gameObject.SetActive(false);
    }

    void SetTitleStatus(string message)
    {
        if (titleStatus != null)
            titleStatus.text = message;
    }

    void ResolveArt()
    {
        titleBackgroundSprite = ResolveSprite(titleBackgroundSprite, TitleSpritePath);
        lobbyBackgroundSprite = ResolveSprite(lobbyBackgroundSprite, LobbySpritePath);
        playerIdleSprite = ResolveSprite(playerIdleSprite, PlayerSpritePath);
        if (koreanFont == null)
            koreanFont = V02Ui.Font();
    }

    static Sprite ResolveSprite(Sprite assigned, string assetPath)
    {
        if (assigned != null)
            return assigned;

#if UNITY_EDITOR
        Sprite loaded = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
        if (loaded == null)
            Debug.LogError("[V02] 스프라이트를 찾지 못했습니다: " + assetPath);
        return loaded;
#else
        Debug.LogError("[V02] 스프라이트가 할당되지 않았습니다: " + assetPath);
        return null;
#endif
    }

    void Show(RectTransform screen)
    {
        ApplyVisible(titleScreen, screen == titleScreen);
        ApplyVisible(lobbyScreen, screen == lobbyScreen);
        ApplyVisible(shopScreen, screen == shopScreen);
        ApplyVisible(debugScreen, screen == debugScreen);
        if (comingSoon != null && screen != comingSoon)
            comingSoon.gameObject.SetActive(false);
        currentScreen = screen;
    }

    static void ApplyVisible(RectTransform rect, bool visible)
    {
        if (rect == null)
            return;

        rect.gameObject.SetActive(visible);
        CanvasGroup group = rect.GetComponent<CanvasGroup>();
        if (group == null)
            return;

        group.alpha = visible ? 1f : 0f;
        group.blocksRaycasts = visible;
        group.interactable = visible;
    }

    IEnumerator FadeTo(RectTransform screen)
    {
        HideComingSoon();
        if (screen == null || currentScreen == screen)
        {
            Show(screen);
            yield break;
        }

        RectTransform from = currentScreen;
        CanvasGroup fromGroup = from != null ? from.GetComponent<CanvasGroup>() : null;
        CanvasGroup toGroup = screen.GetComponent<CanvasGroup>();

        screen.gameObject.SetActive(true);
        if (toGroup != null)
        {
            toGroup.alpha = 0f;
            toGroup.blocksRaycasts = false;
            toGroup.interactable = false;
        }

        float duration = Mathf.Max(0.01f, fadeSeconds);
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            if (fromGroup != null)
                fromGroup.alpha = 1f - t;
            if (toGroup != null)
                toGroup.alpha = t;
            yield return null;
        }

        if (from != null && from != screen)
            ApplyVisible(from, false);
        ApplyVisible(screen, true);
        currentScreen = screen;
    }
}
