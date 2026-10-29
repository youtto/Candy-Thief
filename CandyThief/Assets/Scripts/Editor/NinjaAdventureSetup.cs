using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEditor.U2D.Sprites;
using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEngine.SceneManagement;

[InitializeOnLoad]
public static class NinjaAdventureSetup
{
    const string Root = "Assets/Art/Temporary/NinjaAdventure";
    const string AnimationRoot = "Assets/Animations/Characters";
    const string CharacterPrefabRoot = "Assets/Prefabs/Characters";
    const string ItemPrefabRoot = "Assets/Prefabs/Items";
    const string TileRoot = Root + "/Tiles";
    const string ScenePath = "Assets/Scenes/SampleScene.unity";
    const string ImportMarker = "CandyThief.NinjaAdventure.v1";
    const string DoneKey = "CandyThief.NinjaAdventureSetup.v1";

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
        if (EditorApplication.isPlayingOrWillChangePlaymode ||
            SessionState.GetBool(DoneKey, false))
            return;

        if (!SourcesExist())
        {
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
        BuildItemPrefabs();
        BuildScene();
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        SessionState.SetBool(DoneKey, true);
        Debug.Log("Ninja Adventure prototype art applied to " + ScenePath);
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
               File.Exists(EnvironmentPath("TilesetWallSimple")) &&
               File.Exists(EnvironmentPath("TilesetInterior")) &&
               File.Exists(EnvironmentPath("Elements")) &&
               File.Exists(ItemPath("GoldKey")) &&
               File.Exists(ItemPath("GoldCoin"));
    }

    static bool ConfigureImporters()
    {
        bool changed = false;
        foreach (string character in Characters)
        {
            changed |= ConfigureCharacterSheet(CharacterPath(character, "Idle"), "Idle", 1);
            changed |= ConfigureCharacterSheet(CharacterPath(character, "Walk"), "Walk", 4);
        }

        changed |= ConfigureGridSheet(EnvironmentPath("TilesetInteriorFloor"), "Floor");
        changed |= ConfigureGridSheet(EnvironmentPath("TilesetWallSimple"), "Wall");
        changed |= ConfigureGridSheet(EnvironmentPath("TilesetInterior"), "Interior");
        changed |= ConfigureGridSheet(EnvironmentPath("Elements"), "Element");
        changed |= ConfigureSingleSprite(ItemPath("GoldKey"));
        changed |= ConfigureSingleSprite(ItemPath("GoldCoin"));
        return changed;
    }

    static bool ConfigureCharacterSheet(string path, string animationName, int frameRows)
    {
        TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer == null || ImporterIsReady(importer))
            return false;

        ConfigureCommon(importer, SpriteImportMode.Multiple);
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
                    alignment = (int)SpriteAlignment.Center,
                    pivot = new Vector2(0.5f, 0.5f),
                    border = Vector4.zero
                };
            }
        }

        SetSpriteRects(importer, sprites);
        importer.userData = ImportMarker;
        importer.SaveAndReimport();
        return true;
    }

    static bool ConfigureGridSheet(string path, string prefix)
    {
        TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer == null || ImporterIsReady(importer))
            return false;

        Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        if (texture == null)
            return false;

        ConfigureCommon(importer, SpriteImportMode.Multiple);
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
        return true;
    }

    static bool ConfigureSingleSprite(string path)
    {
        TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer == null || ImporterIsReady(importer))
            return false;

        ConfigureCommon(importer, SpriteImportMode.Single);
        importer.spritePivot = new Vector2(0.5f, 0.5f);
        importer.userData = ImportMarker;
        importer.SaveAndReimport();
        return true;
    }

    static bool ImporterIsReady(TextureImporter importer)
    {
        return importer.userData == ImportMarker &&
               importer.textureType == TextureImporterType.Sprite &&
               importer.filterMode == FilterMode.Point &&
               importer.textureCompression == TextureImporterCompression.Uncompressed &&
               Mathf.Approximately(importer.spritePixelsPerUnit, 16f);
    }

    static void ConfigureCommon(TextureImporter importer, SpriteImportMode mode)
    {
        importer.textureType = TextureImporterType.Sprite;
        importer.spriteImportMode = mode;
        importer.spritePixelsPerUnit = 16f;
        importer.filterMode = FilterMode.Point;
        importer.mipmapEnabled = false;
        importer.alphaIsTransparency = true;
        importer.npotScale = TextureImporterNPOTScale.None;
        importer.textureCompression = TextureImporterCompression.Uncompressed;

        foreach (string platform in new[] { "DefaultTexturePlatform", "Standalone", "WebGL" })
        {
            TextureImporterPlatformSettings settings = importer.GetPlatformTextureSettings(platform);
            settings.overridden = true;
            settings.maxTextureSize = 2048;
            settings.textureCompression = TextureImporterCompression.Uncompressed;
            settings.crunchedCompression = false;
            settings.format = TextureImporterFormat.RGBA32;
            importer.SetPlatformTextureSettings(settings);
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
        provider.Apply();
    }

    static void EnsureFolders()
    {
        EnsureFolder("Assets/Animations");
        EnsureFolder(AnimationRoot);
        EnsureFolder("Assets/Prefabs");
        EnsureFolder(CharacterPrefabRoot);
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
        collider.size = new Vector2(0.6f, 0.35f);
        collider.offset = new Vector2(0f, -0.28f);

        var visual = new GameObject("Visual");
        visual.transform.SetParent(root.transform, false);
        SpriteRenderer renderer = visual.AddComponent<SpriteRenderer>();
        renderer.sprite = defaultSprite;
        renderer.sortingOrder = 10;
        Animator animator = visual.AddComponent<Animator>();
        animator.runtimeAnimatorController = controller;
        visual.AddComponent<CharacterVisualAnimator>();

        string path = CharacterPrefabRoot + "/" + character + ".prefab";
        PrefabUtility.SaveAsPrefabAsset(root, path);
        UnityEngine.Object.DestroyImmediate(root);
    }

    static void BuildItemPrefabs()
    {
        CreateItemPrefab("GoldKey", AssetDatabase.LoadAssetAtPath<Sprite>(ItemPath("GoldKey")));
        CreateItemPrefab("GoldCoin", AssetDatabase.LoadAssetAtPath<Sprite>(ItemPath("GoldCoin")));
    }

    static void CreateItemPrefab(string name, Sprite sprite)
    {
        var item = new GameObject(name);
        SpriteRenderer renderer = item.AddComponent<SpriteRenderer>();
        renderer.sprite = sprite;
        renderer.sortingOrder = 8;
        PrefabUtility.SaveAsPrefabAsset(item, ItemPrefabRoot + "/" + name + ".prefab");
        UnityEngine.Object.DestroyImmediate(item);
    }

    static void BuildScene()
    {
        Scene scene = SceneManager.GetActiveScene();
        if (!scene.IsValid() || scene.path != ScenePath)
            scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);

        PlayerController player = UnityEngine.Object.FindAnyObjectByType<PlayerController>();
        if (player == null)
        {
            Debug.LogError("PlayerController was not found in " + ScenePath);
            return;
        }

        SetupPlayerVisual(player);
        player.transform.position = new Vector3(0f, -2f, 0f);

        DestroySceneObject("TestWall");
        DestroySceneObject("Grid");
        DestroySceneObject("NPCs");
        DestroySceneObject("Items");
        DestroySceneObject("ExitMarker");

        BuildTilemapScene();
        PlaceCharacters(scene);
        PlaceItems(scene);

        var exit = new GameObject("ExitMarker");
        exit.transform.position = new Vector3(0f, -5f, 0f);

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
    }

    static void SetupPlayerVisual(PlayerController player)
    {
        SpriteRenderer rootRenderer = player.GetComponent<SpriteRenderer>();
        if (rootRenderer != null)
            UnityEngine.Object.DestroyImmediate(rootRenderer);

        Transform existing = player.transform.Find("Visual");
        GameObject visual = existing != null ? existing.gameObject : new GameObject("Visual");
        visual.transform.SetParent(player.transform, false);
        visual.transform.localPosition = Vector3.zero;

        SpriteRenderer renderer = GetOrAdd<SpriteRenderer>(visual);
        renderer.sprite = LoadSprites(CharacterPath("NinjaMasked", "Idle"))["Idle_Down"];
        renderer.sortingOrder = 10;

        Animator animator = GetOrAdd<Animator>(visual);
        animator.runtimeAnimatorController = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(
            AnimationRoot + "/NinjaMasked/NinjaMasked.controller");
        GetOrAdd<CharacterVisualAnimator>(visual);
    }

    static void BuildTilemapScene()
    {
        var gridObject = new GameObject("Grid");
        Grid grid = gridObject.AddComponent<Grid>();
        grid.cellSize = Vector3.one;

        Tilemap ground = CreateTilemap(gridObject.transform, "Ground", -10, false);
        Tilemap walls = CreateTilemap(gridObject.transform, "Walls", -5, true);
        Tilemap objects = CreateTilemap(gridObject.transform, "Objects", 0, true);
        Tilemap decorations = CreateTilemap(gridObject.transform, "Decorations", 1, false);

        Tile floor = CreateTile(
            "Floor",
            FindSprite(EnvironmentPath("TilesetInteriorFloor"), "Floor_1_1"),
            Tile.ColliderType.None);

        Tile top = CreateTile("WallTop", FindSprite(EnvironmentPath("TilesetWallSimple"), "Wall_1_0"), Tile.ColliderType.Grid);
        Tile bottom = CreateTile("WallBottom", FindSprite(EnvironmentPath("TilesetWallSimple"), "Wall_1_4"), Tile.ColliderType.Grid);
        Tile left = CreateTile("WallLeft", FindSprite(EnvironmentPath("TilesetWallSimple"), "Wall_0_1"), Tile.ColliderType.Grid);
        Tile right = CreateTile("WallRight", FindSprite(EnvironmentPath("TilesetWallSimple"), "Wall_4_1"), Tile.ColliderType.Grid);
        Tile topLeft = CreateTile("WallTopLeft", FindSprite(EnvironmentPath("TilesetWallSimple"), "Wall_0_0"), Tile.ColliderType.Grid);
        Tile topRight = CreateTile("WallTopRight", FindSprite(EnvironmentPath("TilesetWallSimple"), "Wall_4_0"), Tile.ColliderType.Grid);
        Tile bottomLeft = CreateTile("WallBottomLeft", FindSprite(EnvironmentPath("TilesetWallSimple"), "Wall_0_4"), Tile.ColliderType.Grid);
        Tile bottomRight = CreateTile("WallBottomRight", FindSprite(EnvironmentPath("TilesetWallSimple"), "Wall_4_4"), Tile.ColliderType.Grid);

        for (int y = -5; y <= 5; y++)
        {
            for (int x = -8; x <= 8; x++)
                ground.SetTile(new Vector3Int(x, y, 0), floor);
        }

        for (int x = -7; x <= 7; x++)
        {
            walls.SetTile(new Vector3Int(x, 5, 0), top);
            if (x != 0)
                walls.SetTile(new Vector3Int(x, -5, 0), bottom);
        }
        for (int y = -4; y <= 4; y++)
        {
            walls.SetTile(new Vector3Int(-8, y, 0), left);
            walls.SetTile(new Vector3Int(8, y, 0), right);
        }
        walls.SetTile(new Vector3Int(-8, 5, 0), topLeft);
        walls.SetTile(new Vector3Int(8, 5, 0), topRight);
        walls.SetTile(new Vector3Int(-8, -5, 0), bottomLeft);
        walls.SetTile(new Vector3Int(8, -5, 0), bottomRight);

        StampShelf(objects, new Vector3Int(-6, 0, 0));
        StampShelf(objects, new Vector3Int(3, -1, 0));

        Tile counter = CreateTile(
            "InteriorCounter",
            FindSprite(EnvironmentPath("TilesetInterior"), "Interior_2_5"),
            Tile.ColliderType.Grid);
        objects.SetTile(new Vector3Int(-1, 2, 0), counter);
        objects.SetTile(new Vector3Int(0, 2, 0), counter);

        Tile decoration = CreateTile(
            "TableDecoration",
            FindSprite(EnvironmentPath("Elements"), "Element_5_1"),
            Tile.ColliderType.None);
        decorations.SetTile(new Vector3Int(5, 3, 0), decoration);
        decorations.SetTile(new Vector3Int(-3, -3, 0), decoration);
    }

    static void StampShelf(Tilemap tilemap, Vector3Int origin)
    {
        for (int y = 0; y < 3; y++)
        {
            for (int x = 0; x < 3; x++)
            {
                Tile tile = CreateTile(
                    "Shelf_" + x + "_" + y,
                    FindSprite(EnvironmentPath("Elements"), "Element_" + x + "_" + y),
                    Tile.ColliderType.Grid);
                tilemap.SetTile(origin + new Vector3Int(x, 2 - y, 0), tile);
            }
        }
    }

    static Tilemap CreateTilemap(Transform parent, string name, int sortingOrder, bool collision)
    {
        var gameObject = new GameObject(name);
        gameObject.transform.SetParent(parent, false);
        Tilemap tilemap = gameObject.AddComponent<Tilemap>();
        TilemapRenderer renderer = gameObject.AddComponent<TilemapRenderer>();
        renderer.sortingOrder = sortingOrder;
        if (collision)
            gameObject.AddComponent<TilemapCollider2D>();
        return tilemap;
    }

    static Tile CreateTile(string name, Sprite sprite, Tile.ColliderType colliderType)
    {
        string path = TileRoot + "/" + name + ".asset";
        Tile tile = AssetDatabase.LoadAssetAtPath<Tile>(path);
        if (tile == null)
        {
            tile = ScriptableObject.CreateInstance<Tile>();
            AssetDatabase.CreateAsset(tile, path);
        }
        tile.sprite = sprite;
        tile.colliderType = colliderType;
        EditorUtility.SetDirty(tile);
        return tile;
    }

    static void PlaceCharacters(Scene scene)
    {
        var parent = new GameObject("NPCs");
        PlacePrefab(CharacterPrefabRoot + "/Inspector.prefab", "Guard", new Vector3(-3f, 2f, 0f), parent.transform, scene);
        PlacePrefab(CharacterPrefabRoot + "/Villager.prefab", "Shopper 1", new Vector3(3f, 2f, 0f), parent.transform, scene);
        PlacePrefab(CharacterPrefabRoot + "/Villager2.prefab", "Shopper 2", new Vector3(5f, 0f, 0f), parent.transform, scene);
        PlacePrefab(CharacterPrefabRoot + "/Villager3.prefab", "Shopper 3", new Vector3(-2f, -3f, 0f), parent.transform, scene);
    }

    static void PlaceItems(Scene scene)
    {
        var parent = new GameObject("Items");
        PlacePrefab(ItemPrefabRoot + "/GoldKey.prefab", "Key Candy (Temporary)", new Vector3(6f, 3f, 0f), parent.transform, scene);
        PlacePrefab(ItemPrefabRoot + "/GoldCoin.prefab", "Gold Candy 1 (Temporary)", new Vector3(2f, -3f, 0f), parent.transform, scene);
        PlacePrefab(ItemPrefabRoot + "/GoldCoin.prefab", "Gold Candy 2 (Temporary)", new Vector3(-5f, 3f, 0f), parent.transform, scene);
    }

    static void PlacePrefab(
        string path,
        string name,
        Vector3 position,
        Transform parent,
        Scene scene)
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        GameObject instance = PrefabUtility.InstantiatePrefab(prefab, scene) as GameObject;
        instance.name = name;
        instance.transform.SetParent(parent, true);
        instance.transform.position = position;
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
        foreach (UnityEngine.Object asset in AssetDatabase.LoadAllAssetsAtPath(path))
        {
            if (asset is Sprite sprite)
                sprites[sprite.name] = sprite;
        }
        return sprites;
    }

    static Sprite FindSprite(string path, string name)
    {
        Dictionary<string, Sprite> sprites = LoadSprites(path);
        if (!sprites.TryGetValue(name, out Sprite sprite))
            throw new InvalidOperationException(name + " was not found in " + path);
        return sprite;
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
