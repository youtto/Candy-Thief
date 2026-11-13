using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[InitializeOnLoad]
public static class V02UiSetup
{
    const string ScenePath = "Assets/Scenes/V02/Title.unity";
    const string LobbyScenePath = "Assets/Scenes/V02/Lobby.unity";
    const string LobbyPrefabPath = "Assets/Prefabs/V02/UI/Lobby.prefab";
    const string LobbyBackgroundPath = "Assets/Art/Lobby/Review/CT_Lobby_Background_v002.png";
    const string ThiefIdlePath = "Assets/Art/Lobby/Review/CT_Lobby_Thief_Idle_v007_display230.png";
    const string SampleScenePath = "Assets/Scenes/SampleScene.unity";
    const string DoneKey = "CandyThief.V02UiSetup.v2";
    const string TitleSpritePath = "Assets/Art/Title/CT_Title_Day_v002.png";
    const string TitleStartButtonPath = "Assets/Art/Title/CT_Title_StartButton_v002.png";
    const string TitleNicknameFramePath = "Assets/Art/Title/CT_Title_NicknameFrame_v001.png";
    const string LobbySpritePath = "Assets/Art/Lobby/CT_Lobby_B_Background_v001.png";
    const string PlayerSpritePath = "Assets/Art/Lobby/CT_Player_Idle_v001.png";
    const string HudSheetPath = "Assets/Art/Lobby/CT_Lobby_HUD_Sheet_v001.png";
    const string HudAvatarPath = "Assets/Art/Lobby/Review/CT_Lobby_Thief_Idle_v007_display230.png";
    const string BtnCrewPath = "Assets/Art/Lobby/CT_Lobby_Btn_Crew_v003.png";
    const string BtnStartPath = "Assets/Art/Lobby/CT_Lobby_Btn_Start_v003.png";
    const string BtnEnhancePath = "Assets/Art/Lobby/CT_Lobby_Btn_Enhance_v002.png";

    static V02UiSetup()
    {
        EditorApplication.delayCall += Run;
    }

    [MenuItem("Candy Thief/V02/Bake Title UI (if missing)")]
    static void MenuBakeTitleUi()
    {
        BakeTitleUi(false);
    }

    [MenuItem("Candy Thief/V02/Bake Lobby UI (if missing)")]
    static void MenuBakeLobbyUi()
    {
        BakeLobbyUi(true);
    }

    [MenuItem("Candy Thief/V02/Rebuild Title Scene")]
    static void MenuRebuild()
    {
        BakeTitleUi(true);
        EnsureBuildSettings();
        EnsurePortrait();
    }

    static void Run()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            return;

        EnsureSpriteImports();
        EnsureLobbyUiImports();
        EnsureKoreanFont();
        EnsureScene();
        BakeTitleUi(true);
        BakeLobbyUi(true);
        V02AppFlow flow = Object.FindAnyObjectByType<V02AppFlow>();
        if (flow != null)
            AssignLobbyHudArt(flow);
        EnsureBuildSettings();
        EnsurePortrait();
        EditorPrefs.SetBool(DoneKey, true);
    }

    static void EnsureSpriteImports()
    {
        EnsureSpriteImport(TitleSpritePath);
        EnsureSpriteImport(TitleStartButtonPath);
        EnsureSpriteImport(TitleNicknameFramePath);
        EnsureSpriteImport(LobbySpritePath);
        EnsureSpriteImport(PlayerSpritePath);
    }

    static void EnsureLobbyUiImports()
    {
        EnsureReadableUiSprite(BtnCrewPath);
        EnsureReadableUiSprite(BtnStartPath);
        EnsureReadableUiSprite(BtnEnhancePath);
        EnsureHudSheetImport();
    }

    static void EnsureReadableUiSprite(string path)
    {
        var importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer == null)
        {
            AssetDatabase.Refresh();
            importer = AssetImporter.GetAtPath(path) as TextureImporter;
        }

        if (importer == null)
        {
            Debug.LogError("[V02] 아트 파일을 찾지 못했습니다: " + path);
            return;
        }

        var settings = new TextureImporterSettings();
        importer.ReadTextureSettings(settings);
        bool dirty = importer.textureType != TextureImporterType.Sprite
            || importer.spriteImportMode != SpriteImportMode.Single
            || importer.mipmapEnabled
            || !importer.alphaIsTransparency
            || !importer.isReadable
            || settings.spriteMeshType != SpriteMeshType.FullRect
            || importer.maxTextureSize < 2048
            || importer.textureCompression != TextureImporterCompression.Uncompressed;

        if (!dirty)
            return;

        settings.textureType = TextureImporterType.Sprite;
        settings.spriteMode = (int)SpriteImportMode.Single;
        settings.spriteMeshType = SpriteMeshType.FullRect;
        settings.alphaIsTransparency = true;
        settings.mipmapEnabled = false;
        settings.readable = true;
        importer.SetTextureSettings(settings);
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.mipmapEnabled = false;
        importer.alphaIsTransparency = true;
        importer.isReadable = true;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        if (importer.maxTextureSize < 2048)
            importer.maxTextureSize = 2048;
        importer.SaveAndReimport();
    }

    static void EnsureHudSheetImport()
    {
        var importer = AssetImporter.GetAtPath(HudSheetPath) as TextureImporter;
        if (importer == null)
        {
            AssetDatabase.Refresh();
            importer = AssetImporter.GetAtPath(HudSheetPath) as TextureImporter;
        }

        if (importer == null)
        {
            Debug.LogError("[V02] 아트 파일을 찾지 못했습니다: " + HudSheetPath);
            return;
        }

        SpriteMetaData[] slices =
        {
            HudSlice("HUD_AvatarFrame", 22f, 179f, 145f, 144f),
            HudSlice("HUD_NicknamePlate", 184f, 208f, 321f, 94f),
            HudSlice("HUD_EnergyIcon", 533f, 203f, 89f, 102f),
            HudSlice("HUD_EnergyChip", 629f, 215f, 146f, 81f),
            HudSlice("HUD_GoldIcon", 805f, 202f, 101f, 104f),
            HudSlice("HUD_GoldChip", 908f, 215f, 155f, 81f),
            HudSlice("HUD_CandyIcon", 1099f, 209f, 144f, 94f),
            HudSlice("HUD_CandyChip", 1246f, 215f, 143f, 81f),
            HudSlice("HUD_Settings", 1413f, 201f, 103f, 105f)
        };

        bool dirty = importer.textureType != TextureImporterType.Sprite
            || importer.spriteImportMode != SpriteImportMode.Multiple
            || importer.mipmapEnabled
            || !importer.alphaIsTransparency
            || importer.maxTextureSize < 2048
            || importer.spritesheet == null
            || importer.spritesheet.Length != slices.Length;

        if (!dirty && importer.spritesheet != null)
        {
            for (int i = 0; i < slices.Length; i++)
            {
                if (importer.spritesheet[i].name != slices[i].name
                    || importer.spritesheet[i].rect != slices[i].rect)
                {
                    dirty = true;
                    break;
                }
            }
        }

        if (!dirty)
            return;

        var settings = new TextureImporterSettings();
        importer.ReadTextureSettings(settings);
        settings.textureType = TextureImporterType.Sprite;
        settings.spriteMode = (int)SpriteImportMode.Multiple;
        settings.spriteMeshType = SpriteMeshType.FullRect;
        settings.alphaIsTransparency = true;
        settings.mipmapEnabled = false;
        importer.SetTextureSettings(settings);
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Multiple;
        importer.mipmapEnabled = false;
        importer.alphaIsTransparency = true;
        importer.spritesheet = slices;
        if (importer.maxTextureSize < 2048)
            importer.maxTextureSize = 2048;
        importer.SaveAndReimport();
    }

    static SpriteMetaData HudSlice(string name, float x, float y, float w, float h)
    {
        return new SpriteMetaData
        {
            name = name,
            rect = new Rect(x, y, w, h),
            alignment = (int)SpriteAlignment.Center,
            pivot = new Vector2(0.5f, 0.5f)
        };
    }

    static void EnsureSpriteImport(string path)
    {
        var importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer == null)
        {
            Debug.LogError("[V02] 아트 파일을 찾지 못했습니다: " + path);
            return;
        }

        var settings = new TextureImporterSettings();
        importer.ReadTextureSettings(settings);
        bool dirty = importer.textureType != TextureImporterType.Sprite
            || importer.spriteImportMode != SpriteImportMode.Single
            || importer.mipmapEnabled
            || !importer.alphaIsTransparency
            || settings.spriteMeshType != SpriteMeshType.FullRect
            || importer.maxTextureSize < 2048;

        if (!dirty)
            return;

        settings.textureType = TextureImporterType.Sprite;
        settings.spriteMode = (int)SpriteImportMode.Single;
        settings.spriteMeshType = SpriteMeshType.FullRect;
        settings.alphaIsTransparency = true;
        settings.mipmapEnabled = false;
        importer.SetTextureSettings(settings);
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = SpriteImportMode.Single;
        importer.mipmapEnabled = false;
        importer.alphaIsTransparency = true;
        if (importer.maxTextureSize < 2048)
            importer.maxTextureSize = 2048;
        importer.SaveAndReimport();
    }

    static TMP_FontAsset EnsureKoreanFont()
    {
        const string sdfPath = "Assets/font/NotoSansKR-Black SDF.asset";
        const string ttfPath = "Assets/font/NotoSansKR-Black.ttf";
        TMP_FontAsset sdf = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(sdfPath);
        Font ttf = AssetDatabase.LoadAssetAtPath<Font>(ttfPath);
        if (sdf == null)
            Debug.LogError("[V02] 한글 TMP Font Asset이 없습니다: " + sdfPath);
        if (ttf == null)
            Debug.LogError("[V02] 한글 TTF가 없습니다: " + ttfPath);

        return sdf;
    }

    static void AssignLobbyArt(V02AppFlow flow)
    {
        var so = new SerializedObject(flow);
        so.FindProperty("lobbyBackgroundSprite").objectReferenceValue = AssetDatabase.LoadAssetAtPath<Sprite>(LobbySpritePath);
        so.FindProperty("playerIdleSprite").objectReferenceValue = AssetDatabase.LoadAssetAtPath<Sprite>(PlayerSpritePath);
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    static void AssignLobbyHudArt(V02AppFlow flow)
    {
        if (flow == null)
            return;

        var so = new SerializedObject(flow);
        AssignSpriteIfEmpty(so, "hudAvatarPortrait", AssetDatabase.LoadAssetAtPath<Sprite>(HudAvatarPath));
        AssignSpriteIfEmpty(so, "btnCrew", AssetDatabase.LoadAssetAtPath<Sprite>(BtnCrewPath));
        AssignSpriteIfEmpty(so, "btnStart", AssetDatabase.LoadAssetAtPath<Sprite>(BtnStartPath));
        AssignSpriteIfEmpty(so, "btnEnhance", AssetDatabase.LoadAssetAtPath<Sprite>(BtnEnhancePath));
        AssignNamedSpriteIfEmpty(so, "hudAvatarFrame", HudSheetPath, "HUD_AvatarFrame");
        AssignNamedSpriteIfEmpty(so, "hudNicknamePlate", HudSheetPath, "HUD_NicknamePlate");
        AssignNamedSpriteIfEmpty(so, "hudEnergyIcon", HudSheetPath, "HUD_EnergyIcon");
        AssignNamedSpriteIfEmpty(so, "hudEnergyChip", HudSheetPath, "HUD_EnergyChip");
        AssignNamedSpriteIfEmpty(so, "hudGoldIcon", HudSheetPath, "HUD_GoldIcon");
        AssignNamedSpriteIfEmpty(so, "hudGoldChip", HudSheetPath, "HUD_GoldChip");
        AssignNamedSpriteIfEmpty(so, "hudCandyIcon", HudSheetPath, "HUD_CandyIcon");
        AssignNamedSpriteIfEmpty(so, "hudCandyChip", HudSheetPath, "HUD_CandyChip");
        AssignNamedSpriteIfEmpty(so, "hudSettings", HudSheetPath, "HUD_Settings");
        if (!so.hasModifiedProperties)
            return;

        so.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(flow);
    }

    static void AssignSpriteIfEmpty(SerializedObject so, string propertyName, Sprite sprite)
    {
        SerializedProperty property = so.FindProperty(propertyName);
        if (property != null && property.objectReferenceValue == null && sprite != null)
            property.objectReferenceValue = sprite;
    }

    static void AssignNamedSpriteIfEmpty(SerializedObject so, string propertyName, string assetPath, string spriteName)
    {
        SerializedProperty property = so.FindProperty(propertyName);
        if (property == null || property.objectReferenceValue != null)
            return;

        Object[] assets = AssetDatabase.LoadAllAssetsAtPath(assetPath);
        if (assets == null)
            return;

        for (int i = 0; i < assets.Length; i++)
        {
            Sprite sprite = assets[i] as Sprite;
            if (sprite != null && sprite.name == spriteName)
            {
                property.objectReferenceValue = sprite;
                return;
            }
        }
    }

    static void EnsureScene()
    {
        if (System.IO.File.Exists(ScenePath))
            return;

        System.IO.Directory.CreateDirectory("Assets/Scenes/V02");

        Scene previous = SceneManager.GetActiveScene();
        string previousPath = previous.path;
        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
        scene.name = "Title";

        var cameraGo = new GameObject("Main Camera");
        cameraGo.tag = "MainCamera";
        Camera camera = cameraGo.AddComponent<Camera>();
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color(0.50f, 0.80f, 0.94f);
        camera.orthographic = true;
        camera.orthographicSize = 5f;
        camera.nearClipPlane = -10f;
        camera.farClipPlane = 100f;
        cameraGo.AddComponent<UniversalAdditionalCameraData>();
        SceneManager.MoveGameObjectToScene(cameraGo, scene);

        var app = new GameObject("V02App");
        V02AppFlow flow = app.AddComponent<V02AppFlow>();
        AssignLobbyArt(flow);
        SceneManager.MoveGameObjectToScene(app, scene);

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene, ScenePath);
        AssetDatabase.Refresh();
        EditorSceneManager.CloseScene(scene, true);

        if (!string.IsNullOrEmpty(previousPath) && previousPath != ScenePath)
            EditorSceneManager.OpenScene(previousPath, OpenSceneMode.Single);
    }

    static void BakeTitleUi(bool skipIfPresent)
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            return;

        Scene scene = SceneManager.GetActiveScene();
        if (scene.path != ScenePath)
        {
            if (skipIfPresent)
                return;
            if (!System.IO.File.Exists(ScenePath))
                return;
            scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        }

        V02AppFlow flow = Object.FindAnyObjectByType<V02AppFlow>();
        if (flow == null)
        {
            Debug.LogError("[V02] V02AppFlow가 Title 씬에 없습니다.");
            return;
        }

        Transform existing = null;
        GameObject[] roots = scene.GetRootGameObjects();
        for (int i = 0; i < roots.Length; i++)
        {
            if (roots[i].name == "TitleCanvas")
                existing = roots[i].transform;
        }

        if (existing == null)
            existing = flow.transform.Find("TitleCanvas");

        if (existing != null)
        {
            if (skipIfPresent)
            {
                WireTitleReferences(flow, existing as RectTransform);
                return;
            }

            Debug.Log("[V02] TitleCanvas가 이미 있어 베이크를 건너뜁니다.");
            return;
        }

        EnsureEventSystem(scene);
        RectTransform canvas = CreateTitleCanvas(scene);
        RectTransform safe = V02Ui.CreateStretch("SafeArea", canvas);
        safe.gameObject.AddComponent<SafeAreaFitter>();
        CreateTitleScreen(safe);
        WireTitleReferences(flow, canvas);
        AssignLobbyArt(flow);

        EditorUtility.SetDirty(flow);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        Debug.Log("[V02] Title UI를 Title.unity에 저장했습니다. 이후 배치는 Editor에서 수정하세요.");
    }

    static void EnsureEventSystem(Scene scene)
    {
        if (Object.FindAnyObjectByType<EventSystem>() != null)
            return;

        var go = new GameObject("EventSystem");
        go.AddComponent<EventSystem>();
        go.AddComponent<InputSystemUIInputModule>();
        SceneManager.MoveGameObjectToScene(go, scene);
    }

    static RectTransform CreateTitleCanvas(Scene scene)
    {
        var canvasGo = new GameObject("TitleCanvas", typeof(RectTransform));
        SceneManager.MoveGameObjectToScene(canvasGo, scene);
        Canvas canvas = canvasGo.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.pixelPerfect = false;
        CanvasScaler scaler = canvasGo.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(V02Ui.RefWidth, V02Ui.RefHeight);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;
        canvasGo.AddComponent<GraphicRaycaster>();
        return (RectTransform)canvasGo.transform;
    }

    static RectTransform CreateTitleScreen(RectTransform safe)
    {
        V02Ui.UseFont(EnsureKoreanFont());
        RectTransform root = V02Ui.CreatePanel("Title", safe, new Color(0.50f, 0.80f, 0.94f));
        Image letterbox = root.GetComponent<Image>();
        letterbox.raycastTarget = false;
        V02Ui.AddCanvasGroup(root);

        RectTransform background = V02Ui.CreateStretch("Background", root);
        Image bgImage = background.gameObject.AddComponent<Image>();
        V02Ui.ApplySprite(bgImage, AssetDatabase.LoadAssetAtPath<Sprite>(TitleSpritePath), true, false);

        RectTransform login = V02Ui.CreateStretch("LoginPanel", root);
        login.gameObject.AddComponent<CanvasGroup>();

        TMP_InputField nickname = CreateNicknameInput(login, AssetDatabase.LoadAssetAtPath<Sprite>(TitleNicknameFramePath));
        V02Ui.SetAnchors(
            (RectTransform)nickname.transform,
            new Vector2(0.5f, 0.255f),
            new Vector2(0.5f, 0.255f),
            new Vector2(-360f, -45f),
            new Vector2(360f, 45f));

        RectTransform cta = V02Ui.CreateStretch("StartButton", login);
        Image ctaImage = cta.gameObject.AddComponent<Image>();
        V02Ui.ApplySprite(ctaImage, AssetDatabase.LoadAssetAtPath<Sprite>(TitleStartButtonPath), true, true);
        Button ctaButton = cta.gameObject.AddComponent<Button>();
        ctaButton.targetGraphic = ctaImage;
        ctaButton.transition = Selectable.Transition.None;
        cta.gameObject.AddComponent<V02UiPressScale>();
        V02Ui.SetAnchors(cta, new Vector2(0.12f, 0.09f), new Vector2(0.88f, 0.21f), Vector2.zero, Vector2.zero);

        TextMeshProUGUI error = V02Ui.CreateText(
            "ErrorMessage", login, "", 28f, V02Ui.Ink, TextAlignmentOptions.Center);
        V02Ui.SetAnchors(error.rectTransform, new Vector2(0.1f, 0.02f), new Vector2(0.9f, 0.085f), Vector2.zero, Vector2.zero);

        V02TitleLogin loginUi = root.gameObject.AddComponent<V02TitleLogin>();
        var so = new SerializedObject(loginUi);
        so.FindProperty("nicknameField").objectReferenceValue = nickname;
        so.FindProperty("startButton").objectReferenceValue = ctaButton;
        so.FindProperty("status").objectReferenceValue = error;
        so.ApplyModifiedPropertiesWithoutUndo();
        return root;
    }

    static TMP_InputField CreateNicknameInput(Transform parent, Sprite frame)
    {
        RectTransform root = V02Ui.CreateStretch("NicknameInput", parent);
        Image image = root.gameObject.AddComponent<Image>();
        if (frame != null)
            V02Ui.ApplySprite(image, frame, true, true);
        else
            image.color = new Color(0.96f, 0.93f, 0.86f, 0.96f);

        TMP_InputField field = root.gameObject.AddComponent<TMP_InputField>();
        field.targetGraphic = image;

        RectTransform viewport = V02Ui.CreateStretch("Text Area", root);
        viewport.offsetMin = new Vector2(48f, 10f);
        viewport.offsetMax = new Vector2(-48f, -10f);
        viewport.gameObject.AddComponent<RectMask2D>();

        Color placeholderColor = new Color(V02Ui.Ink.r, V02Ui.Ink.g, V02Ui.Ink.b, 0.45f);
        TextMeshProUGUI placeholder = V02Ui.CreateText(
            "Placeholder", viewport, "닉네임을 입력해 주세요", 32f, placeholderColor, TextAlignmentOptions.MidlineLeft);
        V02Ui.Stretch(placeholder.rectTransform);
        placeholder.textWrappingMode = TextWrappingModes.NoWrap;
        placeholder.overflowMode = TextOverflowModes.Overflow;

        TextMeshProUGUI text = V02Ui.CreateText(
            "Text", viewport, "", 32f, V02Ui.Ink, TextAlignmentOptions.MidlineLeft);
        V02Ui.Stretch(text.rectTransform);
        text.textWrappingMode = TextWrappingModes.NoWrap;
        text.overflowMode = TextOverflowModes.Overflow;

        field.textViewport = viewport;
        field.textComponent = text;
        field.placeholder = placeholder;
        field.characterLimit = V02LocalProfileService.MaxNicknameLength;
        field.lineType = TMP_InputField.LineType.SingleLine;
        field.contentType = TMP_InputField.ContentType.Standard;
        field.customCaretColor = true;
        field.caretColor = V02Ui.Ink;
        field.selectionColor = new Color(0.98f, 0.78f, 0.86f, 0.55f);
        field.fontAsset = V02Ui.Font();
        field.pointSize = 32f;
        field.richText = false;
        field.onFocusSelectAll = true;
        return field;
    }

    static void WireTitleReferences(V02AppFlow flow, RectTransform canvas)
    {
        if (flow == null || canvas == null)
            return;

        Transform safe = canvas.Find("SafeArea");
        Transform title = safe != null ? safe.Find("Title") : null;
        Transform lobby = safe != null ? safe.Find("Lobby") : null;
        V02TitleLogin login = title != null ? title.GetComponent<V02TitleLogin>() : null;
        Transform error = title != null ? title.Find("LoginPanel/ErrorMessage") : null;
        var so = new SerializedObject(flow);
        so.FindProperty("titleCanvas").objectReferenceValue = canvas;
        so.FindProperty("safeArea").objectReferenceValue = safe;
        so.FindProperty("titleScreen").objectReferenceValue = title;
        so.FindProperty("lobbyScreen").objectReferenceValue = lobby;
        so.FindProperty("titleLogin").objectReferenceValue = login;
        so.FindProperty("lobbyHud").objectReferenceValue = lobby != null ? lobby.GetComponent<V02LobbyHud>() : null;
        so.FindProperty("titleStatus").objectReferenceValue = error != null ? error.GetComponent<TextMeshProUGUI>() : null;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    static void BakeLobbyUi(bool skipIfPresent)
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            return;

        Scene scene = SceneManager.GetActiveScene();
        if (scene.path != ScenePath)
        {
            if (skipIfPresent)
                return;
            if (!System.IO.File.Exists(ScenePath))
                return;
            scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        }

        V02AppFlow flow = Object.FindAnyObjectByType<V02AppFlow>();
        if (flow == null)
            return;

        Transform canvas = null;
        GameObject[] roots = scene.GetRootGameObjects();
        for (int i = 0; i < roots.Length; i++)
        {
            if (roots[i].name == "TitleCanvas")
                canvas = roots[i].transform;
        }

        if (canvas == null)
            return;

        Transform safe = canvas.Find("SafeArea");
        if (safe == null)
            return;

        Transform existing = safe.Find("Lobby");
        bool ready = existing != null
            && existing.Find("TopHud") != null
            && existing.Find("Background") != null
            && (existing.Find("Thief") != null || existing.Find("Player") != null);

        if (ready)
        {
            WireTitleReferences(flow, canvas as RectTransform);
            if (!System.IO.File.Exists(LobbyPrefabPath))
                SaveLobbyPrefab(existing.gameObject, true);
            EnsureLobbyPreviewScene();
            return;
        }

        if (existing != null)
            Object.DestroyImmediate(existing.gameObject);

        AssignLobbyHudArt(flow);
        RectTransform lobby = flow.EditorBuildLobby(safe);
        FinalizeLobbyLayout(lobby);
        WireTitleReferences(flow, canvas as RectTransform);
        SaveLobbyPrefab(lobby.gameObject, true);
        lobby.gameObject.SetActive(false);
        EditorUtility.SetDirty(flow);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        EnsureLobbyPreviewScene();
        Debug.Log("[V02] Lobby UI를 Title.unity와 " + LobbyPrefabPath + " 에 저장했습니다.");
    }

    static void FinalizeLobbyLayout(RectTransform lobby)
    {
        if (lobby == null)
            return;

        Image rootImage = lobby.GetComponent<Image>();
        Transform background = lobby.Find("Background");
        if (background == null)
        {
            RectTransform bg = V02Ui.CreateStretch("Background", lobby);
            Image bgImage = bg.gameObject.AddComponent<Image>();
            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(LobbyBackgroundPath);
            if (sprite == null && rootImage != null)
                sprite = rootImage.sprite;
            V02Ui.ApplySprite(bgImage, sprite, true, false);
            background = bg;
        }

        background.SetSiblingIndex(0);
        if (rootImage != null)
        {
            rootImage.sprite = null;
            rootImage.color = new Color(0f, 0f, 0f, 0f);
            rootImage.raycastTarget = false;
        }

        Transform thief = lobby.Find("Thief");
        if (thief == null)
            thief = lobby.Find("Player");
        if (thief != null)
        {
            thief.name = "Thief";
            RectTransform rect = (RectTransform)thief;
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0f);
            rect.sizeDelta = new Vector2(172.34f, 224.04f);
            rect.anchoredPosition = new Vector2(-11.45f, -180.37f);
            rect.SetSiblingIndex(1);

            var bob = thief.GetComponent<V02IdleBob>();
            if (bob != null)
                bob.enabled = false;

            Image thiefImage = thief.GetComponent<Image>();
            Sprite idle = AssetDatabase.LoadAssetAtPath<Sprite>(ThiefIdlePath);
            if (thiefImage != null && idle != null)
                V02Ui.ApplySprite(thiefImage, idle, true, false);
        }

        Transform hud = lobby.Find("TopHud");
        if (hud != null)
            hud.SetSiblingIndex(lobby.childCount - 2);
        Transform buttons = lobby.Find("BottomActions");
        if (buttons != null)
            buttons.SetSiblingIndex(lobby.childCount - 1);

        if (lobby.GetComponent<V02LobbyHud>() == null)
            lobby.gameObject.AddComponent<V02LobbyHud>();

        BindLobbyHudTexts(lobby.GetComponent<V02LobbyHud>(), lobby);
    }

    static void BindLobbyHudTexts(V02LobbyHud hud, Transform lobby)
    {
        if (hud == null || lobby == null)
            return;

        TextMeshProUGUI nickname = FindText(lobby, "TopHud/Profile/NicknamePlate/NicknameText");
        TextMeshProUGUI energy = FindText(lobby, "TopHud/Currencies/EnergyChip/EnergyText");
        TextMeshProUGUI gold = FindText(lobby, "TopHud/Currencies/GoldChip/GoldText");
        TextMeshProUGUI candy = FindText(lobby, "TopHud/Currencies/CandyChip/CandyText");
        hud.Configure(nickname, energy, gold, candy);
        EditorUtility.SetDirty(hud);
    }

    static TextMeshProUGUI FindText(Transform root, string path)
    {
        Transform found = root.Find(path);
        return found != null ? found.GetComponent<TextMeshProUGUI>() : null;
    }

    static void SaveLobbyPrefab(GameObject lobby, bool connect)
    {
        if (lobby == null)
            return;

        System.IO.Directory.CreateDirectory("Assets/Prefabs/V02/UI");
        if (connect)
            PrefabUtility.SaveAsPrefabAssetAndConnect(lobby, LobbyPrefabPath, InteractionMode.AutomatedAction);
        else if (!System.IO.File.Exists(LobbyPrefabPath))
            PrefabUtility.SaveAsPrefabAsset(lobby, LobbyPrefabPath);

        AssetDatabase.SaveAssets();
    }

    static void EnsureLobbyPreviewScene()
    {
        if (System.IO.File.Exists(LobbyScenePath))
            return;
        if (AssetDatabase.LoadAssetAtPath<GameObject>(LobbyPrefabPath) == null)
            return;

        V02AppFlow sourceFlow = Object.FindAnyObjectByType<V02AppFlow>();
        GameObject sourceApp = sourceFlow != null ? sourceFlow.gameObject : null;
        Scene preview = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
        preview.name = "Lobby";

        var cameraGo = new GameObject("Main Camera");
        cameraGo.tag = "MainCamera";
        Camera camera = cameraGo.AddComponent<Camera>();
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color(0.50f, 0.80f, 0.94f);
        camera.orthographic = true;
        camera.orthographicSize = 5f;
        camera.nearClipPlane = -10f;
        camera.farClipPlane = 100f;
        cameraGo.AddComponent<UniversalAdditionalCameraData>();
        SceneManager.MoveGameObjectToScene(cameraGo, preview);

        var eventGo = new GameObject("EventSystem");
        eventGo.AddComponent<EventSystem>();
        eventGo.AddComponent<InputSystemUIInputModule>();
        SceneManager.MoveGameObjectToScene(eventGo, preview);

        var app = new GameObject("V02App");
        V02AppFlow flow = app.AddComponent<V02AppFlow>();
        CopyComponent<V02LobbyArtController>(sourceApp, app);
        CopyComponent<V02LobbyIdleAnimator>(sourceApp, app);
        CopyComponent<V02LobbyBlinkAnimator>(sourceApp, app);
        SceneManager.MoveGameObjectToScene(app, preview);

        RectTransform canvas = CreateTitleCanvas(preview);
        RectTransform safe = V02Ui.CreateStretch("SafeArea", canvas);
        safe.gameObject.AddComponent<SafeAreaFitter>();
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(LobbyPrefabPath);
        GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, safe);
        instance.name = "Lobby";
        instance.SetActive(true);

        var so = new SerializedObject(flow);
        so.FindProperty("titleCanvas").objectReferenceValue = canvas;
        so.FindProperty("safeArea").objectReferenceValue = safe;
        so.FindProperty("titleScreen").objectReferenceValue = null;
        so.FindProperty("lobbyScreen").objectReferenceValue = instance.GetComponent<RectTransform>();
        so.FindProperty("lobbyHud").objectReferenceValue = instance.GetComponent<V02LobbyHud>();
        so.FindProperty("koreanFont").objectReferenceValue = EnsureKoreanFont();
        so.ApplyModifiedPropertiesWithoutUndo();
        AssignLobbyHudArt(flow);

        EditorSceneManager.SaveScene(preview, LobbyScenePath);
        EditorSceneManager.CloseScene(preview, true);
    }

    static void CopyComponent<T>(GameObject source, GameObject dest) where T : Component
    {
        if (source == null || dest == null)
            return;

        T from = source.GetComponent<T>();
        T to = dest.GetComponent<T>();
        if (from == null)
            return;
        if (to == null)
            to = dest.AddComponent<T>();

        UnityEditorInternal.ComponentUtility.CopyComponent(from);
        UnityEditorInternal.ComponentUtility.PasteComponentValues(to);
    }

    static void EnsureBuildSettings()
    {
        var scenes = new List<EditorBuildSettingsScene>
        {
            new EditorBuildSettingsScene(ScenePath, true),
            new EditorBuildSettingsScene(SampleScenePath, true)
        };
        EditorBuildSettings.scenes = scenes.ToArray();

        EditorSceneManager.playModeStartScene = null;
    }

    static void EnsurePortrait()
    {
        PlayerSettings.defaultInterfaceOrientation = UIOrientation.Portrait;
        PlayerSettings.allowedAutorotateToPortrait = true;
        PlayerSettings.allowedAutorotateToPortraitUpsideDown = true;
        PlayerSettings.allowedAutorotateToLandscapeLeft = false;
        PlayerSettings.allowedAutorotateToLandscapeRight = false;
    }
}
