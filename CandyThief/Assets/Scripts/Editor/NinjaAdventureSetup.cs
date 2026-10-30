using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEditor.U2D.Sprites;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;

[InitializeOnLoad]
public static class NinjaAdventureSetup
{
    const string Root = "Assets/Art/Temporary/NinjaAdventure";
    const string AnimationRoot = "Assets/Animations/Characters";
    const string CharacterPrefabRoot = "Assets/Prefabs/Characters";
    const string EnvironmentPrefabRoot = "Assets/Prefabs/Environment";
    const string ItemPrefabRoot = "Assets/Prefabs/Items";
    const string TileRoot = Root + "/Tiles";
    const string ScenePath = "Assets/Scenes/SampleScene.unity";
    const string ImportMarker = "CandyThief.NinjaAdventure.v4";
    const string DoneKey = "CandyThief.NinjaAdventureSetup.v4";
    const int VisualOrder = 1000;

    const int RoomXMin = -5;
    const int RoomXMax = 5;
    const int RoomYMin = -3;
    const int RoomYMax = 3;

    static readonly string[] Characters =
    {
        "NinjaMasked", "Inspector", "Villager", "Villager2", "Villager3"
    };

    static readonly string[] Directions = { "Down", "Up", "Left", "Right" };

    static NinjaAdventureSetup()
    {
        EditorApplication.delayCall += Run;
    }

    [MenuItem("Candy Thief/Apply Ninja Adventure Prototype Art")]
    static void RunFromMenu()
    {
        SessionState.SetBool(DoneKey, false);
        Run();
    }

    static void Run()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            return;

        if (SessionState.GetBool(DoneKey, false) &&
            File.Exists(EnvironmentPrefabRoot + "/CounterObstacle.prefab"))
        {
            if (!File.Exists(ScreenshotPath()))
            {
                try
                {
                    CaptureGameCamera();
                }
                catch (Exception exception)
                {
                    WriteStatus("screenshot error: " + exception.Message);
                    Debug.LogException(exception);
                }
            }
            return;
        }

        WriteStatus("running");

        try
        {
            if (!SourcesExist())
            {
                WriteStatus("error: missing source files under " + Root);
                Debug.LogError("Ninja Adventure prototype source files are missing under " + Root);
                return;
            }

            if (ConfigureImporters())
            {
                EditorApplication.delayCall += Run;
                return;
            }

            EnsureFolders();
            BuildCharacterAssets();
            BuildEnvironmentPrefabs();
            BuildItemPrefabs();
            BuildScene();
            CaptureGameCamera();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            SessionState.SetBool(DoneKey, true);
            WriteStatus("done");
            Debug.Log("Ninja Adventure v4 art applied to " + ScenePath);
        }
        catch (Exception exception)
        {
            WriteStatus("error: " + exception);
            Debug.LogException(exception);
        }
    }

    static bool SourcesExist()
    {
        foreach (string character in Characters)
        {
            if (!File.Exists(CharacterPath(character, "Idle")) ||
                !File.Exists(CharacterPath(character, "Walk")))
                return false;
        }

        return File.Exists(EnvironmentPath("TilesetInteriorFloor")) &&
               File.Exists(EnvironmentPath("TilesetInterior")) &&
               File.Exists(EnvironmentPath("Elements")) &&
               File.Exists(ItemPath("GoldKey")) &&
               File.Exists(ItemPath("GoldCoin"));
    }

    static bool ConfigureImporters()
    {
        foreach (string character in Characters)
        {
            if (ConfigureCharacterSheet(CharacterPath(character, "Idle"), "Idle", 1) ||
                ConfigureCharacterSheet(CharacterPath(character, "Walk"), "Walk", 4))
                return true;
        }

        return ConfigureGridSheet(EnvironmentPath("TilesetInteriorFloor"), "Floor") ||
               ConfigureGridSheet(EnvironmentPath("TilesetInterior"), "Interior") ||
               ConfigureGridSheet(EnvironmentPath("TilesetWallSimple"), "Wall") ||
               ConfigureElementObjects(EnvironmentPath("Elements")) ||
               ConfigureSingleSprite(ItemPath("GoldKey"), false) ||
               ConfigureSingleSprite(ItemPath("GoldCoin"), false);
    }

    static bool ConfigureCharacterSheet(string path, string animationName, int frameRows)
    {
        TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer == null || ImporterIsReady(importer, true, null))
            return false;

        ConfigureCommon(importer, SpriteImportMode.Multiple, SpriteAlignment.BottomCenter);
        var sprites = new SpriteMetaData[4 * frameRows];
        int index = 0;
        for (int row = 0; row < frameRows; row++)
        {
            for (int column = 0; column < Directions.Length; column++)
            {
                string name = animationName + "_" + Directions[column];
                if (animationName == "Walk")
                    name += "_" + row;

                sprites[index++] = new SpriteMetaData
                {
                    name = name,
                    rect = new Rect(column * 16, (frameRows - row - 1) * 16, 16, 16),
                    alignment = (int)SpriteAlignment.BottomCenter,
                    pivot = new Vector2(0.5f, 0f),
                    border = Vector4.zero
                };
            }
        }

        SetSpriteRects(importer, sprites);
        importer.userData = ImportMarker;
        importer.SaveAndReimport();
        WriteStatus("imported " + path);
        return true;
    }

    static bool ConfigureGridSheet(string path, string prefix)
    {
        TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer == null || ImporterIsReady(importer, false, null))
            return false;

        Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        if (texture == null)
            return true;

        ConfigureCommon(importer, SpriteImportMode.Multiple, SpriteAlignment.Center);
        int columns = texture.width / 16;
        int rows = texture.height / 16;
        var sprites = new SpriteMetaData[columns * rows];
        int index = 0;
        for (int row = 0; row < rows; row++)
        {
            for (int column = 0; column < columns; column++)
            {
                sprites[index++] = new SpriteMetaData
                {
                    name = prefix + "_" + column + "_" + row,
                    rect = new Rect(column * 16, texture.height - ((row + 1) * 16), 16, 16),
                    alignment = (int)SpriteAlignment.Center,
                    pivot = new Vector2(0.5f, 0.5f),
                    border = Vector4.zero
                };
            }
        }

        SetSpriteRects(importer, sprites);
        importer.userData = ImportMarker;
        importer.SaveAndReimport();
        WriteStatus("imported " + path);
        return true;
    }

    static bool ConfigureElementObjects(string path)
    {
        TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer == null || ImporterIsReady(importer, true, "Element_Table"))
            return false;

        ConfigureCommon(importer, SpriteImportMode.Multiple, SpriteAlignment.BottomCenter);
        var sprites = new[]
        {
            ObjectSlice("Element_Counter", 43, 0, 26, 12, 48),
            ObjectSlice("Element_Table", 84, 11, 24, 26, 48),
            ObjectSlice("Element_RoundTable", 112, 0, 16, 15, 48)
        };

        SetSpriteRects(importer, sprites);
        importer.userData = ImportMarker;
        importer.SaveAndReimport();
        WriteStatus("imported " + path);
        return true;
    }

    static SpriteMetaData ObjectSlice(string name, int imageX, int imageY, int width, int height, int textureHeight)
    {
        return new SpriteMetaData
        {
            name = name,
            rect = new Rect(imageX, textureHeight - imageY - height, width, height),
            alignment = (int)SpriteAlignment.BottomCenter,
            pivot = new Vector2(0.5f, 0f),
            border = Vector4.zero
        };
    }

    static bool ConfigureSingleSprite(string path, bool bottomPivot)
    {
        TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer == null || ImporterIsReady(importer, bottomPivot, null))
            return false;

        ConfigureCommon(importer, SpriteImportMode.Single, SpriteAlignment.BottomCenter);
        importer.userData = ImportMarker;
        importer.SaveAndReimport();
        WriteStatus("imported " + path);
        return true;
    }

    static bool ImporterIsReady(TextureImporter importer, bool bottomPivot, string requiredSprite)
    {
        TextureImporterSettings current = new TextureImporterSettings();
        importer.ReadTextureSettings(current);
        if (importer.userData != ImportMarker ||
            importer.textureType != TextureImporterType.Sprite ||
            importer.filterMode != FilterMode.Point ||
            importer.textureCompression != TextureImporterCompression.Uncompressed ||
            !Mathf.Approximately(importer.spritePixelsPerUnit, 16f) ||
            current.spriteExtrude != 0)
            return false;

        if (requiredSprite != null && !HasSprite(importer.assetPath, requiredSprite))
            return false;

        if (!bottomPivot)
            return true;

        Sprite[] sprites = LoadSpriteArray(importer.assetPath);
        if (sprites.Length == 0)
            return false;

        foreach (Sprite sprite in sprites)
        {
            if (Mathf.Abs(sprite.pivot.y) > 0.05f)
                return false;
        }

        return true;
    }

    static void ConfigureCommon(
        TextureImporter importer,
        SpriteImportMode mode,
        SpriteAlignment alignment)
    {
        Vector2 pivot = alignment == SpriteAlignment.BottomCenter
            ? new Vector2(0.5f, 0f)
            : new Vector2(0.5f, 0.5f);

        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = mode;
        importer.spritePixelsPerUnit = 16f;
        importer.filterMode = FilterMode.Point;
        importer.mipmapEnabled = false;
        importer.alphaIsTransparency = true;
        importer.npotScale = TextureImporterNPOTScale.None;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.spritePivot = pivot;

        TextureImporterSettings settings = new TextureImporterSettings();
        importer.ReadTextureSettings(settings);
        settings.textureType = TextureImporterType.Sprite;
        settings.spriteMode = (int)mode;
        settings.spritePixelsPerUnit = 16f;
        settings.spriteMeshType = SpriteMeshType.FullRect;
        settings.spriteAlignment = (int)alignment;
        settings.spritePivot = pivot;
        settings.spriteExtrude = 0;
        settings.filterMode = FilterMode.Point;
        settings.mipmapEnabled = false;
        settings.alphaIsTransparency = true;
        importer.SetTextureSettings(settings);
        importer.spriteImportMode = mode;
        importer.spritePixelsPerUnit = 16f;

        foreach (string platform in new[] { "DefaultTexturePlatform", "Standalone", "WebGL" })
        {
            TextureImporterPlatformSettings platformSettings = importer.GetPlatformTextureSettings(platform);
            platformSettings.overridden = true;
            platformSettings.maxTextureSize = 2048;
            platformSettings.textureCompression = TextureImporterCompression.Uncompressed;
            platformSettings.crunchedCompression = false;
            platformSettings.format = TextureImporterFormat.RGBA32;
            importer.SetPlatformTextureSettings(platformSettings);
        }
    }

    static void SetSpriteRects(TextureImporter importer, SpriteMetaData[] sprites)
    {
        var factory = new SpriteDataProviderFactories();
        factory.Init();
        ISpriteEditorDataProvider provider = factory.GetSpriteEditorDataProviderFromObject(importer);
        if (provider == null)
            throw new InvalidOperationException("Sprite data provider is unavailable for " + importer.assetPath);

        provider.InitSpriteEditorDataProvider();
        var rects = new SpriteRect[sprites.Length];
        for (int i = 0; i < sprites.Length; i++)
        {
            rects[i] = new SpriteRect
            {
                name = sprites[i].name,
                rect = sprites[i].rect,
                alignment = (SpriteAlignment)sprites[i].alignment,
                pivot = sprites[i].pivot,
                border = sprites[i].border,
                spriteID = GUID.Generate()
            };
        }

        provider.SetSpriteRects(rects);
        ISpriteNameFileIdDataProvider names = provider.GetDataProvider<ISpriteNameFileIdDataProvider>();
        if (names != null)
        {
            var pairs = new SpriteNameFileIdPair[rects.Length];
            for (int i = 0; i < rects.Length; i++)
                pairs[i] = new SpriteNameFileIdPair(rects[i].name, rects[i].spriteID);
            names.SetNameFileIdPairs(pairs);
        }

        provider.Apply();
    }

    static void EnsureFolders()
    {
        EnsureFolder("Assets/Animations");
        EnsureFolder(AnimationRoot);
        EnsureFolder("Assets/Prefabs");
        EnsureFolder(CharacterPrefabRoot);
        EnsureFolder(EnvironmentPrefabRoot);
        EnsureFolder(ItemPrefabRoot);
        EnsureFolder(TileRoot);
    }

    static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path))
            return;

        string parent = Path.GetDirectoryName(path).Replace('\\', '/');
        string name = Path.GetFileName(path);
        EnsureFolder(parent);
        AssetDatabase.CreateFolder(parent, name);
    }

    static void BuildCharacterAssets()
    {
        foreach (string character in Characters)
        {
            string folder = AnimationRoot + "/" + character;
            EnsureFolder(folder);
            Dictionary<string, Sprite> idle = LoadSprites(CharacterPath(character, "Idle"));
            Dictionary<string, Sprite> walk = LoadSprites(CharacterPath(character, "Walk"));
            var clips = new Dictionary<string, AnimationClip>();

            foreach (string direction in Directions)
            {
                clips["Idle_" + direction] = CreateClip(
                    folder + "/Idle_" + direction + ".anim",
                    new[] { idle["Idle_" + direction] },
                    1f);

                var frames = new Sprite[4];
                for (int i = 0; i < frames.Length; i++)
                    frames[i] = walk["Walk_" + direction + "_" + i];
                clips["Walk_" + direction] = CreateClip(
                    folder + "/Walk_" + direction + ".anim",
                    frames,
                    8f);
            }

            RuntimeAnimatorController controller = CreateController(
                folder + "/" + character + ".controller",
                clips);
            CreateCharacterPrefab(character, controller, idle["Idle_Down"]);
        }
    }

    static AnimationClip CreateClip(string path, Sprite[] frames, float frameRate)
    {
        AssetDatabase.DeleteAsset(path);
        var clip = new AnimationClip { frameRate = frameRate };
        var keys = new ObjectReferenceKeyframe[frames.Length + 1];
        for (int i = 0; i < frames.Length; i++)
        {
            keys[i] = new ObjectReferenceKeyframe
            {
                time = i / frameRate,
                value = frames[i]
            };
        }

        keys[keys.Length - 1] = new ObjectReferenceKeyframe
        {
            time = frames.Length / frameRate,
            value = frames[0]
        };

        EditorCurveBinding binding = new EditorCurveBinding
        {
            path = string.Empty,
            type = typeof(SpriteRenderer),
            propertyName = "m_Sprite"
        };
        AnimationUtility.SetObjectReferenceCurve(clip, binding, keys);
        AssetDatabase.CreateAsset(clip, path);

        SerializedObject serialized = new SerializedObject(clip);
        SerializedProperty loop = serialized.FindProperty("m_AnimationClipSettings.m_LoopTime");
        if (loop != null)
            loop.boolValue = true;
        serialized.ApplyModifiedPropertiesWithoutUndo();
        return clip;
    }

    static RuntimeAnimatorController CreateController(
        string path,
        Dictionary<string, AnimationClip> clips)
    {
        AssetDatabase.DeleteAsset(path);
        AnimatorController controller = AnimatorController.CreateAnimatorControllerAtPath(path);
        AnimatorStateMachine stateMachine = controller.layers[0].stateMachine;
        foreach (KeyValuePair<string, AnimationClip> pair in clips)
        {
            AnimatorState state = stateMachine.AddState(pair.Key);
            state.motion = pair.Value;
            if (pair.Key == "Idle_Down")
                stateMachine.defaultState = state;
        }

        return controller;
    }

    static void CreateCharacterPrefab(
        string character,
        RuntimeAnimatorController controller,
        Sprite defaultSprite)
    {
        var root = new GameObject(character);
        BoxCollider2D collider = root.AddComponent<BoxCollider2D>();
        collider.size = new Vector2(0.5f, 0.28f);
        collider.offset = new Vector2(0f, 0.14f);

        GameObject visual = CreateSortedSprite("Visual", defaultSprite, root.transform, 0);
        Animator animator = visual.AddComponent<Animator>();
        animator.runtimeAnimatorController = controller;
        visual.AddComponent<CharacterVisualAnimator>();

        PrefabUtility.SaveAsPrefabAsset(root, CharacterPrefabRoot + "/" + character + ".prefab");
        UnityEngine.Object.DestroyImmediate(root);
    }

    static void BuildEnvironmentPrefabs()
    {
        Dictionary<string, Sprite> elements = LoadSprites(EnvironmentPath("Elements"));
        CreateObstaclePrefab("CounterObstacle", elements["Element_Counter"], new Vector2(1.5f, 0.35f), new Vector2(0f, 0.18f));
        CreateObstaclePrefab("TableObstacle", elements["Element_Table"], new Vector2(1.3f, 0.4f), new Vector2(0f, 0.2f));
        CreateObstaclePrefab("RoundTableObstacle", elements["Element_RoundTable"], new Vector2(0.85f, 0.35f), new Vector2(0f, 0.18f));
    }

    static void CreateObstaclePrefab(string name, Sprite sprite, Vector2 colliderSize, Vector2 colliderOffset)
    {
        var obstacle = new GameObject(name);
        CreateSortedSprite(name, sprite, obstacle.transform, 0, true);
        BoxCollider2D collider = obstacle.AddComponent<BoxCollider2D>();
        collider.size = colliderSize;
        collider.offset = colliderOffset;
        PrefabUtility.SaveAsPrefabAsset(obstacle, EnvironmentPrefabRoot + "/" + name + ".prefab");
        UnityEngine.Object.DestroyImmediate(obstacle);
    }

    static void BuildItemPrefabs()
    {
        CreateItemPrefab("GoldKey", AssetDatabase.LoadAssetAtPath<Sprite>(ItemPath("GoldKey")));
        CreateItemPrefab("GoldCoin", AssetDatabase.LoadAssetAtPath<Sprite>(ItemPath("GoldCoin")));
    }

    static void CreateItemPrefab(string name, Sprite sprite)
    {
        var item = new GameObject(name);
        CreateSortedSprite(name, sprite, item.transform, 1, true);
        PrefabUtility.SaveAsPrefabAsset(item, ItemPrefabRoot + "/" + name + ".prefab");
        UnityEngine.Object.DestroyImmediate(item);
    }

    static GameObject CreateSortedSprite(
        string name,
        Sprite sprite,
        Transform parent,
        int orderOffset,
        bool reuseRoot = false)
    {
        GameObject gameObject = reuseRoot ? parent.gameObject : new GameObject(name);
        if (!reuseRoot)
            gameObject.transform.SetParent(parent, false);

        SpriteRenderer renderer = GetOrAdd<SpriteRenderer>(gameObject);
        renderer.sprite = sprite;
        renderer.spriteSortPoint = SpriteSortPoint.Pivot;
        renderer.sortingOrder = VisualOrder + orderOffset;
        GetOrAdd<YSortRenderer>(gameObject).Configure(parent, 0f, VisualOrder + orderOffset);
        return gameObject;
    }

    static void BuildScene()
    {
        Scene scene = SceneManager.GetActiveScene();
        if (!scene.IsValid() || scene.path != ScenePath)
            scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

        PlayerController player = UnityEngine.Object.FindAnyObjectByType<PlayerController>();
        if (player == null)
            throw new InvalidOperationException("PlayerController was not found in " + ScenePath);

        SetupPlayerVisual(player);
        player.transform.position = new Vector3(0f, -1f, 0f);

        DestroySceneObject("TestWall");
        DestroySceneObject("Grid");
        DestroySceneObject("NPCs");
        DestroySceneObject("Obstacles");
        DestroySceneObject("Items");
        DestroySceneObject("ExitMarker");

        BuildTilemapScene();
        PlaceObstacles(scene);
        PlaceCharacters(scene);
        PlaceItems(scene);

        var exit = new GameObject("ExitMarker");
        exit.transform.position = new Vector3(0f, RoomYMin - 0.5f, 0f);

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
    }

    static void SetupPlayerVisual(PlayerController player)
    {
        SpriteRenderer rootRenderer = player.GetComponent<SpriteRenderer>();
        if (rootRenderer != null)
            UnityEngine.Object.DestroyImmediate(rootRenderer);

        BoxCollider2D collider = player.GetComponent<BoxCollider2D>();
        if (collider != null)
        {
            collider.size = new Vector2(0.5f, 0.28f);
            collider.offset = new Vector2(0f, 0.14f);
        }

        Transform existing = player.transform.Find("Visual");
        GameObject visual = existing != null ? existing.gameObject : new GameObject("Visual");
        visual.transform.SetParent(player.transform, false);
        visual.transform.localPosition = Vector3.zero;
        visual.transform.localScale = Vector3.one;

        SpriteRenderer renderer = GetOrAdd<SpriteRenderer>(visual);
        renderer.sprite = LoadSprites(CharacterPath("NinjaMasked", "Idle"))["Idle_Down"];
        renderer.spriteSortPoint = SpriteSortPoint.Pivot;
        renderer.sortingOrder = VisualOrder;

        Animator animator = GetOrAdd<Animator>(visual);
        animator.runtimeAnimatorController = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(
            AnimationRoot + "/NinjaMasked/NinjaMasked.controller");
        GetOrAdd<CharacterVisualAnimator>(visual);
        GetOrAdd<YSortRenderer>(visual).Configure(player.transform, 0f, VisualOrder);
    }

    static void BuildTilemapScene()
    {
        if (AssetDatabase.IsValidFolder(TileRoot))
            AssetDatabase.DeleteAsset(TileRoot);
        EnsureFolder(TileRoot);

        var gridObject = new GameObject("Grid");
        Grid grid = gridObject.AddComponent<Grid>();
        grid.cellSize = Vector3.one;

        Tilemap ground = CreateTilemap(gridObject.transform, "Ground", -20, false);
        Tilemap walls = CreateTilemap(gridObject.transform, "Walls", -10, true);
        Tilemap decorations = CreateTilemap(gridObject.transform, "Decorations", -9, false);

        Tile floor = CreateTile("Floor", FindSprite(EnvironmentPath("TilesetInteriorFloor"), "Floor_5_1"), Tile.ColliderType.None);
        Tile top = CreateTile("WallTop", Interior("3_0"), Tile.ColliderType.Grid);
        Tile left = CreateTile("WallLeft", Interior("1_4"), Tile.ColliderType.Grid);
        Tile right = CreateTile("WallRight", Interior("6_4"), Tile.ColliderType.Grid);
        Tile bottom = CreateTile("WallBottom", Interior("3_9"), Tile.ColliderType.Grid);
        Tile topLeft = CreateTile("WallTopLeft", Interior("1_0"), Tile.ColliderType.Grid);
        Tile topRight = CreateTile("WallTopRight", Interior("6_0"), Tile.ColliderType.Grid);
        Tile bottomLeft = CreateTile("WallBottomLeft", Interior("0_8"), Tile.ColliderType.Grid);
        Tile bottomRight = CreateTile("WallBottomRight", Interior("6_8"), Tile.ColliderType.Grid);
        Tile door = CreateTile("Door", Interior("2_8"), Tile.ColliderType.None);

        for (int y = RoomYMin; y <= RoomYMax; y++)
        {
            for (int x = RoomXMin; x <= RoomXMax; x++)
                ground.SetTile(new Vector3Int(x, y, 0), floor);
        }

        walls.SetTile(new Vector3Int(RoomXMin, RoomYMax, 0), topLeft);
        walls.SetTile(new Vector3Int(RoomXMax, RoomYMax, 0), topRight);
        for (int x = RoomXMin + 1; x <= RoomXMax - 1; x++)
            walls.SetTile(new Vector3Int(x, RoomYMax, 0), top);

        for (int y = RoomYMin + 1; y <= RoomYMax - 1; y++)
        {
            walls.SetTile(new Vector3Int(RoomXMin, y, 0), left);
            walls.SetTile(new Vector3Int(RoomXMax, y, 0), right);
        }

        walls.SetTile(new Vector3Int(RoomXMin, RoomYMin, 0), bottomLeft);
        walls.SetTile(new Vector3Int(RoomXMax, RoomYMin, 0), bottomRight);
        for (int x = RoomXMin + 1; x <= RoomXMax - 1; x++)
        {
            if (x == 0)
                continue;
            walls.SetTile(new Vector3Int(x, RoomYMin, 0), bottom);
        }

        decorations.SetTile(new Vector3Int(0, RoomYMin, 0), door);
    }

    static Sprite Interior(string id)
    {
        return FindSprite(EnvironmentPath("TilesetInterior"), "Interior_" + id);
    }

    static void PlaceObstacles(Scene scene)
    {
        var parent = new GameObject("Obstacles");
        PlacePrefab(EnvironmentPrefabRoot + "/CounterObstacle.prefab", "Shop Counter", new Vector3(-3f, 0.8f, 0f), parent.transform, scene);
        PlacePrefab(EnvironmentPrefabRoot + "/TableObstacle.prefab", "Display Table", new Vector3(0.5f, 1.3f, 0f), parent.transform, scene);
        PlacePrefab(EnvironmentPrefabRoot + "/RoundTableObstacle.prefab", "Round Table", new Vector3(3f, 0.6f, 0f), parent.transform, scene);
    }

    static Tilemap CreateTilemap(Transform parent, string name, int sortingOrder, bool collision)
    {
        var gameObject = new GameObject(name);
        gameObject.transform.SetParent(parent, false);
        Tilemap tilemap = gameObject.AddComponent<Tilemap>();
        TilemapRenderer renderer = gameObject.AddComponent<TilemapRenderer>();
        renderer.sortingOrder = sortingOrder;
        renderer.mode = TilemapRenderer.Mode.Chunk;
        if (collision)
            gameObject.AddComponent<TilemapCollider2D>();
        return tilemap;
    }

    static Tile CreateTile(string name, Sprite sprite, Tile.ColliderType colliderType)
    {
        string path = TileRoot + "/" + name + ".asset";
        Tile tile = ScriptableObject.CreateInstance<Tile>();
        tile.sprite = sprite;
        tile.colliderType = colliderType;
        AssetDatabase.CreateAsset(tile, path);
        return tile;
    }

    static void PlaceCharacters(Scene scene)
    {
        var parent = new GameObject("NPCs");
        PlacePrefab(CharacterPrefabRoot + "/Inspector.prefab", "Guard", new Vector3(-3.5f, 2f, 0f), parent.transform, scene);
        PlacePrefab(CharacterPrefabRoot + "/Villager.prefab", "Shopper 1", new Vector3(3.5f, 2f, 0f), parent.transform, scene);
        PlacePrefab(CharacterPrefabRoot + "/Villager2.prefab", "Shopper 2", new Vector3(-2.2f, -1.4f, 0f), parent.transform, scene);
    }

    static void PlaceItems(Scene scene)
    {
        var parent = new GameObject("Items");
        PlacePrefab(ItemPrefabRoot + "/GoldKey.prefab", "Key Candy (Temporary)", new Vector3(4f, 2.2f, 0f), parent.transform, scene);
        PlacePrefab(ItemPrefabRoot + "/GoldCoin.prefab", "Gold Candy 1 (Temporary)", new Vector3(1.6f, -1.4f, 0f), parent.transform, scene);
        PlacePrefab(ItemPrefabRoot + "/GoldCoin.prefab", "Gold Candy 2 (Temporary)", new Vector3(-4f, 0.4f, 0f), parent.transform, scene);
    }

    static void PlacePrefab(string path, string name, Vector3 position, Transform parent, Scene scene)
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if (prefab == null)
            throw new InvalidOperationException("Prefab was not found: " + path);

        GameObject instance = PrefabUtility.InstantiatePrefab(prefab, scene) as GameObject;
        instance.name = name;
        instance.transform.SetParent(parent, true);
        instance.transform.position = position;
        instance.transform.localScale = Vector3.one;
    }

    static void CaptureGameCamera()
    {
        Camera camera = Camera.main;
        if (camera == null)
        {
            WriteStatus("done without screenshot: Main Camera missing");
            return;
        }

        Vector3 oldPosition = camera.transform.position;
        float oldSize = camera.orthographicSize;
        camera.transform.position = new Vector3(0f, 0f, oldPosition.z);
        camera.orthographicSize = 5f;

        const int width = 1280;
        const int height = 720;
        RenderTexture rt = new RenderTexture(width, height, 24);
        Texture2D tex = new Texture2D(width, height, TextureFormat.RGB24, false);
        camera.targetTexture = rt;
        camera.Render();
        RenderTexture.active = rt;
        tex.ReadPixels(new Rect(0, 0, width, height), 0, 0);
        tex.Apply();
        camera.targetTexture = null;
        RenderTexture.active = null;
        camera.transform.position = oldPosition;
        camera.orthographicSize = oldSize;

        byte[] png = tex.EncodeToPNG();
        UnityEngine.Object.DestroyImmediate(tex);
        rt.Release();
        UnityEngine.Object.DestroyImmediate(rt);

        string logsPath = ScreenshotPath();
        Directory.CreateDirectory(Path.GetDirectoryName(logsPath));
        File.WriteAllBytes(logsPath, png);

        string docsFolder = Path.GetFullPath(Path.Combine(Application.dataPath, "../../Docs/qa"));
        Directory.CreateDirectory(docsFolder);
        File.WriteAllBytes(Path.Combine(docsFolder, "ninja-adventure-gameview.png"), png);
        WriteStatus("screenshot " + logsPath);
    }

    static T GetOrAdd<T>(GameObject gameObject) where T : Component
    {
        T component = gameObject.GetComponent<T>();
        return component != null ? component : gameObject.AddComponent<T>();
    }

    static void DestroySceneObject(string name)
    {
        GameObject gameObject = GameObject.Find(name);
        if (gameObject != null)
            UnityEngine.Object.DestroyImmediate(gameObject);
    }

    static Dictionary<string, Sprite> LoadSprites(string path)
    {
        var sprites = new Dictionary<string, Sprite>();
        foreach (Sprite sprite in LoadSpriteArray(path))
            sprites[sprite.name] = sprite;
        return sprites;
    }

    static Sprite[] LoadSpriteArray(string path)
    {
        var sprites = new List<Sprite>();
        foreach (UnityEngine.Object asset in AssetDatabase.LoadAllAssetsAtPath(path))
        {
            if (asset is Sprite sprite)
                sprites.Add(sprite);
        }

        return sprites.ToArray();
    }

    static bool HasSprite(string path, string name)
    {
        return LoadSprites(path).ContainsKey(name);
    }

    static Sprite FindSprite(string path, string name)
    {
        Dictionary<string, Sprite> sprites = LoadSprites(path);
        if (!sprites.TryGetValue(name, out Sprite sprite))
            throw new InvalidOperationException(name + " was not found in " + path);
        return sprite;
    }

    static string ScreenshotPath()
    {
        return Path.GetFullPath(Path.Combine(Application.dataPath, "../Logs/ninja-adventure-gameview.png"));
    }

    static void WriteStatus(string message)
    {
        string folder = Path.GetFullPath(Path.Combine(Application.dataPath, "../Logs"));
        Directory.CreateDirectory(folder);
        File.WriteAllText(Path.Combine(folder, "ninja-setup-status.txt"), DateTime.Now.ToString("o") + " " + message);
    }

    static string CharacterPath(string character, string animation)
    {
        return Root + "/Characters/" + character + "/" + animation + ".png";
    }

    static string EnvironmentPath(string fileName)
    {
        return Root + "/Environment/" + fileName + ".png";
    }

    static string ItemPath(string fileName)
    {
        return Root + "/Items/" + fileName + ".png";
    }
}
