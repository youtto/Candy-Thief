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
    const string LobbySpritePath = "Assets/Art/Lobby/CT_Lobby_B_Background_v001.png";
    const string PlayerSpritePath = "Assets/Art/Lobby/CT_Player_Idle_v001.png";
    const string HudSheetPath = "Assets/Art/Lobby/CT_Lobby_HUD_Sheet_v001.png";
    const string HudAvatarPath = "Assets/Art/Lobby/Review/CT_Lobby_Thief_Idle_v007_display230.png";
    const string BtnCrewPath = "Assets/Art/Lobby/CT_Lobby_Btn_Crew_v003.png";
    const string BtnStartPath = "Assets/Art/Lobby/CT_Lobby_Btn_Start_v003.png";
    const string BtnEnhancePath = "Assets/Art/Lobby/CT_Lobby_Btn_Enhance_v002.png";
    static readonly Color HudValueColor = new Color(1f, 0.96f, 0.86f);

    [SerializeField] float fadeSeconds = 0.3f;
    [SerializeField] Sprite lobbyBackgroundSprite;
    [SerializeField] Sprite playerIdleSprite;
    [SerializeField] Sprite hudAvatarPortrait;
    [SerializeField] Sprite hudAvatarFrame;
    [SerializeField] Sprite hudNicknamePlate;
    [SerializeField] Sprite hudEnergyIcon;
    [SerializeField] Sprite hudEnergyChip;
    [SerializeField] Sprite hudGoldIcon;
    [SerializeField] Sprite hudGoldChip;
    [SerializeField] Sprite hudCandyIcon;
    [SerializeField] Sprite hudCandyChip;
    [SerializeField] Sprite hudSettings;
    [SerializeField] Sprite btnCrew;
    [SerializeField] Sprite btnStart;
    [SerializeField] Sprite btnEnhance;
    [SerializeField] TMP_FontAsset koreanFont;
    [SerializeField] RectTransform titleCanvas;
    [SerializeField] RectTransform safeArea;
    [SerializeField] RectTransform titleScreen;
    [SerializeField] RectTransform lobbyScreen;
    [SerializeField] V02TitleLogin titleLogin;
    [SerializeField] V02LobbyHud lobbyHud;
    [SerializeField] TextMeshProUGUI titleStatus;
    RectTransform shopScreen;
    RectTransform debugScreen;
    RectTransform runHudScreen;
    RectTransform comingSoon;
    RectTransform currentScreen;
    TextMeshProUGUI comingSoonText;
    TextMeshProUGUI debugBody;
    Transform shopList;
    V02RunHud runHud;
    V02LocalProfileService profiles;
    Coroutine lobbyFade;

    void Awake()
    {
        EnsureEventSystem();
        EnsureCamera();
        profiles = V02LocalProfileService.CreateDefault();
        ResolveArt();
        V02Ui.UseFont(koreanFont);
        BuildUi();
        if (titleScreen != null)
            Show(titleScreen);
        else
        {
            BindLobbyPreview();
            Show(lobbyScreen);
        }
    }

    void Start()
    {
        if (titleLogin != null)
        {
            titleLogin.LoggedIn -= OnTitleLoggedIn;
            titleLogin.LoggedIn += OnTitleLoggedIn;
            titleLogin.Bind(profiles ?? V02LocalProfileService.CreateDefault());
        }

        CsvDatabase database = CsvDatabase.Current;
        if (database == null || !database.Loaded)
            database = CsvDatabase.LoadFromResources();

        if (database != null && database.HasErrors)
        {
            if (titleLogin != null)
                titleLogin.SetCsvBlocked(true, "데이터 로딩 실패. CSV를 확인하세요.");
            else
                SetTitleStatus("데이터 로딩 실패. CSV를 확인하세요.");
            Debug.LogError("[V02] CSV 로딩 실패. 타이틀에 머무릅니다.");
        }
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
        BindTitleFromScene();
        BindLobbyFromScene();
        EnsureRuntimeHubScreens();
        if (titleLogin != null)
        {
            titleLogin.LoggedIn -= OnTitleLoggedIn;
            titleLogin.LoggedIn += OnTitleLoggedIn;
            titleLogin.Bind(profiles);
        }
    }

    void BindTitleFromScene()
    {
        if (titleCanvas == null)
        {
            Transform found = transform.Find("TitleCanvas");
            if (found == null)
            {
                var sceneCanvas = GameObject.Find("TitleCanvas");
                found = sceneCanvas != null ? sceneCanvas.transform : null;
            }

            if (found != null)
                titleCanvas = found as RectTransform;
        }

        if (safeArea == null && titleCanvas != null)
        {
            Transform found = titleCanvas.Find("SafeArea");
            if (found != null)
                safeArea = found as RectTransform;
        }

        if (titleScreen == null && safeArea != null)
        {
            Transform found = safeArea.Find("Title");
            if (found != null)
                titleScreen = found as RectTransform;
        }

        if (titleLogin == null && titleScreen != null)
            titleLogin = titleScreen.GetComponentInChildren<V02TitleLogin>(true);

        if (titleStatus == null && titleScreen != null)
        {
            Transform found = titleScreen.Find("LoginPanel/ErrorMessage");
            if (found == null)
                found = titleScreen.Find("ErrorMessage");
            if (found != null)
                titleStatus = found.GetComponent<TextMeshProUGUI>();
        }

        if (titleScreen == null && lobbyScreen == null)
            Debug.LogError("[V02] Title 또는 Lobby UI가 씬에 없습니다. TitleCanvas/SafeArea 아래에 배치하세요.");
    }

    void BindLobbyFromScene()
    {
        if (lobbyScreen == null && safeArea != null)
        {
            Transform found = safeArea.Find("Lobby");
            if (found != null)
                lobbyScreen = found as RectTransform;
        }

        if (lobbyScreen == null)
        {
            Debug.LogError("[V02] Lobby UI가 씬에 없습니다. TitleCanvas/SafeArea/Lobby 를 배치하세요.");
            return;
        }

        V02Ui.AddCanvasGroup(lobbyScreen);
        if (lobbyHud == null)
            lobbyHud = lobbyScreen.GetComponent<V02LobbyHud>();
        if (lobbyHud == null)
            lobbyHud = lobbyScreen.GetComponentInChildren<V02LobbyHud>(true);

        WireLobbyButton("BottomActions/BtnCrew", OnLobbyCrewClicked);
        WireLobbyButton("BottomActions/BtnStart", OnLobbyStartClicked);
        WireLobbyButton("BottomActions/BtnEnhance", OnLobbyEnhanceClicked);
        WireLobbyButton("TopHud/Currencies/Settings", OnLobbySettingsClicked);
    }

    void WireLobbyButton(string path, UnityEngine.Events.UnityAction onClick)
    {
        if (lobbyScreen == null)
            return;

        Transform found = lobbyScreen.Find(path);
        if (found == null)
            return;

        Button button = found.GetComponent<Button>();
        if (button == null)
            return;

        button.onClick.RemoveListener(onClick);
        button.onClick.AddListener(onClick);
    }

    public void OnLobbyCrewClicked()
    {
        ShowComingSoon("크루");
    }

    public void OnLobbyStartClicked()
    {
        ShowShopSelect();
    }

    public void OnLobbyEnhanceClicked()
    {
        ShowComingSoon("강화");
    }

    public void OnLobbySettingsClicked()
    {
        ShowComingSoon("설정");
    }

    void BindLobbyPreview()
    {
        if (lobbyHud == null || profiles == null)
            return;

        string nickname;
        string error;
        V02PlayerProfile peeked;
        if (profiles.TryReadLastNickname(out nickname, out error)
            && !string.IsNullOrEmpty(nickname)
            && profiles.TryPeek(nickname, out peeked, out error))
        {
            lobbyHud.BindDisplayOnly(peeked);
            return;
        }

        lobbyHud.BindDisplayOnly(null);
    }

    void EnsureRuntimeHubScreens()
    {
        if (safeArea == null)
            return;

        shopScreen = FindOrBuild("ShopSelect", BuildShopSelect);
        debugScreen = FindOrBuild("RunDebug", BuildDebug);
        comingSoon = FindOrBuild("ComingSoon", BuildComingSoon);
        if (comingSoon != null)
            comingSoon.gameObject.SetActive(false);
    }

    RectTransform FindOrBuild(string childName, System.Func<Transform, RectTransform> build)
    {
        Transform existing = safeArea.Find(childName);
        if (existing != null)
            return existing as RectTransform;
        return build(safeArea);
    }

#if UNITY_EDITOR
    public RectTransform EditorBuildLobby(Transform parent)
    {
        ResolveArt();
        return BuildLobby(parent);
    }
#endif

    RectTransform BuildLobby(Transform parent)
    {
        RectTransform root = V02Ui.CreatePanel("Lobby", parent, new Color(0.42f, 0.41f, 0.47f));
        Image bg = root.GetComponent<Image>();
        V02Ui.ApplySprite(bg, lobbyBackgroundSprite, true, false);
        V02Ui.AddCanvasGroup(root);

        if (playerIdleSprite != null || hudAvatarPortrait != null)
        {
            RectTransform player = V02Ui.CreateStretch("Player", root);
            Image playerImage = player.gameObject.AddComponent<Image>();
            V02Ui.ApplySprite(playerImage, playerIdleSprite != null ? playerIdleSprite : hudAvatarPortrait, true, false);
            V02Ui.SetAnchors(player, new Vector2(0.5f, 0.33f), new Vector2(0.5f, 0.33f), new Vector2(-170f, 0f), new Vector2(170f, 300f));
            player.gameObject.AddComponent<V02IdleBob>();
        }

        BuildLobbyHudAndButtons(root);
        return root;
    }

    void EnsureLobbyChrome(RectTransform lobby)
    {
        if (lobby == null)
            return;

        StripLegacyLobbyWidget(lobby, "TopBar");
        StripLegacyLobbyWidget(lobby, "BottomBar");
        StripLegacyLobbyWidget(lobby, "AllySlotA");
        StripLegacyLobbyWidget(lobby, "AllySlotB");
        StripLegacyLobbyWidget(lobby, "StartHeist");

        if (lobby.Find("TopHud") == null || lobby.Find("BottomActions") == null)
            BuildLobbyHudAndButtons(lobby);

        lobbyHud = lobby.GetComponent<V02LobbyHud>();
        if (lobbyHud == null)
            lobbyHud = lobby.gameObject.AddComponent<V02LobbyHud>();
    }

    static void StripLegacyLobbyWidget(Transform lobby, string childName)
    {
        Transform child = lobby.Find(childName);
        if (child == null)
            return;
        Object.DestroyImmediate(child.gameObject);
    }

    void BuildLobbyHudAndButtons(RectTransform root)
    {
        Transform existingHud = root.Find("TopHud");
        if (existingHud != null)
            Object.DestroyImmediate(existingHud.gameObject);
        Transform existingButtons = root.Find("BottomActions");
        if (existingButtons != null)
            Object.DestroyImmediate(existingButtons.gameObject);

        RectTransform hud = V02Ui.CreateStretch("TopHud", root);
        V02Ui.SetAnchors(hud, new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(20f, -128f), new Vector2(-20f, -16f));

        RectTransform profile = CreateHudCluster("Profile", hud, false);
        RectTransform avatarRoot = CreateHudSized("AvatarRoot", profile, new Vector2(80f, 80f));
        Image avatar = CreateHudImage("Avatar", avatarRoot, hudAvatarPortrait != null ? hudAvatarPortrait : playerIdleSprite, false);
        V02Ui.SetAnchors((RectTransform)avatar.transform, new Vector2(0.12f, 0.12f), new Vector2(0.88f, 0.88f), Vector2.zero, Vector2.zero);
        Image frame = CreateHudImage("Frame", avatarRoot, hudAvatarFrame, false);
        V02Ui.Stretch((RectTransform)frame.transform);

        Image nicknamePlate = CreateHudImage("NicknamePlate", profile, hudNicknamePlate, false);
        LayoutElement nicknameLayout = nicknamePlate.gameObject.AddComponent<LayoutElement>();
        nicknameLayout.preferredWidth = 208f;
        nicknameLayout.preferredHeight = 52f;
        nicknameLayout.minWidth = 208f;
        nicknameLayout.minHeight = 52f;
        TextMeshProUGUI nicknameText = CreateHudValue(
            "NicknameText", nicknamePlate.transform, "", 28f, TextAlignmentOptions.Midline);
        V02Ui.SetAnchors(nicknameText.rectTransform, Vector2.zero, Vector2.one, new Vector2(18f, 6f), new Vector2(-18f, -6f));
        nicknameText.enableAutoSizing = true;
        nicknameText.fontSizeMin = 18f;
        nicknameText.fontSizeMax = 28f;
        nicknameText.overflowMode = TextOverflowModes.Ellipsis;
        nicknameText.textWrappingMode = TextWrappingModes.NoWrap;

        RectTransform currencies = CreateHudCluster("Currencies", hud, true);
        CreateHudImage("EnergyIcon", currencies, hudEnergyIcon, false);
        FitHudSprite(currencies.Find("EnergyIcon"), 46f, 46f);
        Image energyChip = CreateHudImage("EnergyChip", currencies, hudEnergyChip, false);
        FitHudSprite(energyChip.transform, 100f, 48f);
        TextMeshProUGUI energyText = CreateHudValue(
            "EnergyText", energyChip.transform, "10/20", 24f, TextAlignmentOptions.Center);
        V02Ui.SetAnchors(energyText.rectTransform, Vector2.zero, Vector2.one, new Vector2(8f, 4f), new Vector2(-8f, -4f));

        CreateHudImage("GoldIcon", currencies, hudGoldIcon, false);
        FitHudSprite(currencies.Find("GoldIcon"), 50f, 50f);
        Image goldChip = CreateHudImage("GoldChip", currencies, hudGoldChip, false);
        FitHudSprite(goldChip.transform, 100f, 48f);
        TextMeshProUGUI goldText = CreateHudValue(
            "GoldText", goldChip.transform, "0", 24f, TextAlignmentOptions.Center);
        V02Ui.SetAnchors(goldText.rectTransform, Vector2.zero, Vector2.one, new Vector2(8f, 4f), new Vector2(-8f, -4f));

        CreateHudImage("CandyIcon", currencies, hudCandyIcon, false);
        FitHudSprite(currencies.Find("CandyIcon"), 54f, 50f);
        Image candyChip = CreateHudImage("CandyChip", currencies, hudCandyChip, false);
        FitHudSprite(candyChip.transform, 100f, 48f);
        TextMeshProUGUI candyText = CreateHudValue(
            "CandyText", candyChip.transform, "0", 24f, TextAlignmentOptions.Center);
        V02Ui.SetAnchors(candyText.rectTransform, Vector2.zero, Vector2.one, new Vector2(8f, 4f), new Vector2(-8f, -4f));

        Button settings = V02Ui.CreateImageButton("Settings", currencies, hudSettings, () => ShowComingSoon("설정"));
        FitHudSprite(settings.transform, 52f, 52f);

        RectTransform bottom = V02Ui.CreateStretch("BottomActions", root);
        V02Ui.SetAnchors(bottom, new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0f, 0f), new Vector2(0f, 300f));

        Button crew = V02Ui.CreateImageButton("BtnCrew", bottom, btnCrew, () => ShowComingSoon("크루"));
        PlaceBottomButton((RectTransform)crew.transform, new Vector2(-312f, 8f), new Vector2(256f, 241f));

        Button start = V02Ui.CreateImageButton("BtnStart", bottom, btnStart, ShowShopSelect);
        PlaceBottomButton((RectTransform)start.transform, new Vector2(0f, 22f), new Vector2(320f, 240f));

        Button enhance = V02Ui.CreateImageButton("BtnEnhance", bottom, btnEnhance, () => ShowComingSoon("강화"));
        PlaceBottomButton((RectTransform)enhance.transform, new Vector2(312f, 8f), new Vector2(256f, 241f));

        lobbyHud = root.GetComponent<V02LobbyHud>();
        if (lobbyHud == null)
            lobbyHud = root.gameObject.AddComponent<V02LobbyHud>();
        lobbyHud.Configure(nicknameText, energyText, goldText, candyText);
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
        if (lobbyHud != null)
            lobbyHud.Bind(profiles);
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
            + "  ·  시작 위험도 " + shop.StartAlert
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

        run.LogSnapshot("shop selected");
        if (!TryShowRunHud(database, run))
        {
            debugBody.text =
                "ShopID: " + run.ShopID + "\n"
                + "Phase: " + run.CurrentPhase + "\n"
                + "Turns: " + run.RemainingTurns + "\n"
                + "Alert: " + run.Alert + "\n"
                + "Candy: " + run.Candy + "\n"
                + "Status: " + run.Status + "\n\n"
                + "런 HUD 프리팹을 찾지 못했습니다.";
            Show(debugScreen);
        }
    }

    bool TryShowRunHud(CsvDatabase database, RunState run)
    {
        if (runHud == null)
        {
            V02RunHud prefab = Resources.Load<V02RunHud>(V02RunHud.ResourcePath);
            if (prefab == null)
            {
                Debug.LogError("[V02] 런 HUD 프리팹이 없습니다: Resources/" + V02RunHud.ResourcePath);
                return false;
            }

            runHud = Instantiate(prefab, safeArea != null ? safeArea : transform);
            runHud.name = "RunHud";
            runHudScreen = runHud.Rect;
            V02Ui.Stretch(runHudScreen);
            V02Ui.AddCanvasGroup(runHudScreen);
        }

        runHud.Bind(database, run, ShowLobby);
        if (runHud.ResultPresentation != null)
        {
            runHud.ResultPresentation.EndedRunContinued.RemoveListener(OnEndedRunContinued);
            runHud.ResultPresentation.EndedRunContinued.AddListener(OnEndedRunContinued);
        }

        Show(runHudScreen);
        return true;
    }

    void OnEndedRunContinued()
    {
        Debug.Log("[V02] 런 종료 피드백 확인. Result 화면은 M1-9에서 연결합니다.");
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

    void OnTitleLoggedIn(V02PlayerProfile profile)
    {
        if (profile == null || lobbyFade != null)
            return;

        lobbyFade = StartCoroutine(EnterLobbyAfterLogin());
    }

    IEnumerator EnterLobbyAfterLogin()
    {
        if (lobbyHud != null)
            lobbyHud.Bind(profiles);
        yield return FadeTo(lobbyScreen);
        lobbyFade = null;
    }

    void ResolveArt()
    {
        lobbyBackgroundSprite = ResolveSprite(lobbyBackgroundSprite, LobbySpritePath);
        playerIdleSprite = ResolveSprite(playerIdleSprite, PlayerSpritePath);
        hudAvatarPortrait = ResolveSprite(hudAvatarPortrait, HudAvatarPath);
        btnCrew = ResolveSprite(btnCrew, BtnCrewPath);
        btnStart = ResolveSprite(btnStart, BtnStartPath);
        btnEnhance = ResolveSprite(btnEnhance, BtnEnhancePath);
        hudAvatarFrame = ResolveNamedSprite(hudAvatarFrame, HudSheetPath, "HUD_AvatarFrame");
        hudNicknamePlate = ResolveNamedSprite(hudNicknamePlate, HudSheetPath, "HUD_NicknamePlate");
        hudEnergyIcon = ResolveNamedSprite(hudEnergyIcon, HudSheetPath, "HUD_EnergyIcon");
        hudEnergyChip = ResolveNamedSprite(hudEnergyChip, HudSheetPath, "HUD_EnergyChip");
        hudGoldIcon = ResolveNamedSprite(hudGoldIcon, HudSheetPath, "HUD_GoldIcon");
        hudGoldChip = ResolveNamedSprite(hudGoldChip, HudSheetPath, "HUD_GoldChip");
        hudCandyIcon = ResolveNamedSprite(hudCandyIcon, HudSheetPath, "HUD_CandyIcon");
        hudCandyChip = ResolveNamedSprite(hudCandyChip, HudSheetPath, "HUD_CandyChip");
        hudSettings = ResolveNamedSprite(hudSettings, HudSheetPath, "HUD_Settings");
        if (koreanFont == null)
            koreanFont = V02Ui.Font();
    }

    static Sprite ResolveNamedSprite(Sprite assigned, string assetPath, string spriteName)
    {
        if (assigned != null)
            return assigned;
        return LoadNamedSprite(assetPath, spriteName);
    }

    static Sprite LoadNamedSprite(string assetPath, string spriteName)
    {
#if UNITY_EDITOR
        Object[] assets = UnityEditor.AssetDatabase.LoadAllAssetsAtPath(assetPath);
        if (assets == null)
        {
            Debug.LogError("[V02] 스프라이트 시트를 찾지 못했습니다: " + assetPath);
            return null;
        }

        for (int i = 0; i < assets.Length; i++)
        {
            Sprite sprite = assets[i] as Sprite;
            if (sprite != null && sprite.name == spriteName)
                return sprite;
        }

        Debug.LogError("[V02] 스프라이트를 찾지 못했습니다: " + assetPath + " / " + spriteName);
        return null;
#else
        Debug.LogError("[V02] 스프라이트가 할당되지 않았습니다: " + spriteName);
        return null;
#endif
    }

    static RectTransform CreateHudCluster(string name, Transform parent, bool fromRight)
    {
        RectTransform rect = V02Ui.CreateStretch(name, parent);
        rect.anchorMin = fromRight ? new Vector2(1f, 0.5f) : new Vector2(0f, 0.5f);
        rect.anchorMax = rect.anchorMin;
        rect.pivot = fromRight ? new Vector2(1f, 0.5f) : new Vector2(0f, 0.5f);
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = new Vector2(10f, 96f);

        HorizontalLayoutGroup layout = rect.gameObject.AddComponent<HorizontalLayoutGroup>();
        layout.spacing = fromRight ? 10f : 8f;
        layout.childAlignment = fromRight ? TextAnchor.MiddleRight : TextAnchor.MiddleLeft;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;
        layout.padding = new RectOffset(0, 0, 0, 0);

        ContentSizeFitter fitter = rect.gameObject.AddComponent<ContentSizeFitter>();
        fitter.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        return rect;
    }

    static RectTransform CreateHudSized(string name, Transform parent, Vector2 size)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        RectTransform rect = (RectTransform)go.transform;
        rect.sizeDelta = size;
        LayoutElement layout = go.AddComponent<LayoutElement>();
        layout.preferredWidth = size.x;
        layout.preferredHeight = size.y;
        layout.minWidth = size.x;
        layout.minHeight = size.y;
        return rect;
    }

    static Image CreateHudImage(string name, Transform parent, Sprite sprite, bool raycast)
    {
        var go = new GameObject(name, typeof(RectTransform));
        go.transform.SetParent(parent, false);
        Image image = go.AddComponent<Image>();
        V02Ui.ApplySprite(image, sprite, true, raycast);
        return image;
    }

    static void FitHudSprite(Transform target, float width, float height)
    {
        if (target == null)
            return;

        RectTransform rect = target as RectTransform;
        if (rect != null)
            rect.sizeDelta = new Vector2(width, height);

        LayoutElement layout = target.gameObject.GetComponent<LayoutElement>();
        if (layout == null)
            layout = target.gameObject.AddComponent<LayoutElement>();
        layout.preferredWidth = width;
        layout.preferredHeight = height;
        layout.minWidth = width;
        layout.minHeight = height;
    }

    static TextMeshProUGUI CreateHudValue(
        string name,
        Transform parent,
        string content,
        float size,
        TextAlignmentOptions align)
    {
        TextMeshProUGUI text = V02Ui.CreateText(name, parent, content, size, HudValueColor, align);
        text.textWrappingMode = TextWrappingModes.NoWrap;
        text.overflowMode = TextOverflowModes.Overflow;
        return text;
    }

    static void PlaceBottomButton(RectTransform rect, Vector2 anchored, Vector2 size)
    {
        rect.anchorMin = new Vector2(0.5f, 0f);
        rect.anchorMax = new Vector2(0.5f, 0f);
        rect.pivot = new Vector2(0.5f, 0f);
        rect.anchoredPosition = anchored;
        rect.sizeDelta = size;
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
        ApplyVisible(runHudScreen, screen == runHudScreen);
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
        if (fromGroup != null)
        {
            fromGroup.blocksRaycasts = false;
            fromGroup.interactable = false;
        }

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
