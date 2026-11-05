using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

[InitializeOnLoad]
public static class V02UiSetup
{
    const string ScenePath = "Assets/Scenes/V02/Title.unity";
    const string SampleScenePath = "Assets/Scenes/SampleScene.unity";
    const string DoneKey = "CandyThief.V02UiSetup.v2";
    const string TitleSpritePath = "Assets/Art/Title/CT_Title_A_v001.png";
    const string LobbySpritePath = "Assets/Art/Lobby/CT_Lobby_B_Background_v001.png";
    const string PlayerSpritePath = "Assets/Art/Lobby/CT_Player_Idle_v001.png";

    static V02UiSetup()
    {
        EditorApplication.delayCall += Run;
    }

    [MenuItem("Candy Thief/V02/Rebuild Title Scene")]
    static void MenuRebuild()
    {
        EditorPrefs.DeleteKey(DoneKey);
        Run();
    }

    static void Run()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            return;

        EnsureSpriteImports();
        EnsureKoreanFont();
        EnsureScene();
        EnsureBuildSettings();
        EnsurePortrait();
        EditorPrefs.SetBool(DoneKey, true);
    }

    static void EnsureSpriteImports()
    {
        EnsureSpriteImport(TitleSpritePath);
        EnsureSpriteImport(LobbySpritePath);
        EnsureSpriteImport(PlayerSpritePath);
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

    static void AssignArt(V02AppFlow flow)
    {
        var so = new SerializedObject(flow);
        so.FindProperty("titleBackgroundSprite").objectReferenceValue = AssetDatabase.LoadAssetAtPath<Sprite>(TitleSpritePath);
        so.FindProperty("lobbyBackgroundSprite").objectReferenceValue = AssetDatabase.LoadAssetAtPath<Sprite>(LobbySpritePath);
        so.FindProperty("playerIdleSprite").objectReferenceValue = AssetDatabase.LoadAssetAtPath<Sprite>(PlayerSpritePath);
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    static void EnsureScene()
    {
        if (System.IO.File.Exists(ScenePath) && EditorPrefs.GetBool(DoneKey, false))
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
        AssignArt(flow);
        SceneManager.MoveGameObjectToScene(app, scene);

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene, ScenePath);
        AssetDatabase.Refresh();
        EditorSceneManager.CloseScene(scene, true);

        if (!string.IsNullOrEmpty(previousPath) && previousPath != ScenePath)
            EditorSceneManager.OpenScene(previousPath, OpenSceneMode.Single);
    }

    static void EnsureBuildSettings()
    {
        var scenes = new List<EditorBuildSettingsScene>
        {
            new EditorBuildSettingsScene(ScenePath, true),
            new EditorBuildSettingsScene(SampleScenePath, true)
        };
        EditorBuildSettings.scenes = scenes.ToArray();

        SceneAsset titleAsset = AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath);
        if (titleAsset != null)
            EditorSceneManager.playModeStartScene = titleAsset;
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
