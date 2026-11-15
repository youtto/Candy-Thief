using System.Collections.Generic;
using UnityEngine;

public static class CsvLoadRunner
{
    const string ShopHeader =
        "ShopID,ShopName,Difficulty,StartTurn,StartAlert,SuccessRateBonusPp,CandyMultiplier,EventRateBonusPp,AllowedEventTags,BackgroundImageKey,UnlockCondition,Enabled,SpawnWeight,DistanceGrade,ScaleGrade,SecurityGrade,GoodsGrade,EnergyCost,Notes";
    // Enabled=1, SpawnWeight=0, grades blank, EnergyCost=0, Notes blank (matches the on-disk prototype rows).
    const string ShopTail = "1,0,,,,,0,";

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void LoadOnPlay()
    {
        LoadAndReport();
    }

    public static CsvDatabase LoadAndReport()
    {
        CsvDatabase database = CsvDatabase.LoadFromResources();
        if (database.HasErrors)
            return database;

        ShopRow shop1;
        ShopRow shop2;
        ShopRow shop3;
        bool found1 = database.TryGetShop("SHOP_001", out shop1);
        bool found2 = database.TryGetShop("SHOP_002", out shop2);
        bool found3 = database.TryGetShop("SHOP_003", out shop3);

        if (!found1 || !found2 || !found3 || database.EnabledShops.Count < 3)
        {
            Debug.LogError(CsvDatabase.LogPrefix + "ShopTable 기본 가게(SHOP_001~003) 조회에 실패했습니다.");
            return database;
        }

        database.LogEnabledShops();
        Debug.Log(CsvDatabase.LogPrefix + "한국어 가게명 확인: "
            + shop1.ShopName + " / "
            + shop2.ShopName + " / "
            + shop3.ShopName);
        return database;
    }

    public static bool RunValidationSelfTest()
    {
        int failed = 0;
        failed += ExpectErrors(
            "ID 중복",
            ShopHeader + "\n"
            + "SHOP_001,동네 사탕 가게,1,15,0,10,1.0,0,COMMON,shop_neighborhood,NONE," + ShopTail + "\n"
            + "SHOP_001,대형 캔디숍,2,18,10,0,1.5,5,COMMON,shop_mall,NONE," + ShopTail,
            1);
        failed += ExpectErrors(
            "필수 컬럼 누락",
            "ShopName,Difficulty,StartTurn,StartAlert,SuccessRateBonusPp,CandyMultiplier,EventRateBonusPp,AllowedEventTags,BackgroundImageKey,UnlockCondition,Enabled,Notes\n"
            + "동네 사탕 가게,1,15,0,10,1.0,0,COMMON,shop_neighborhood,NONE,1,",
            1);
        failed += ExpectErrors(
            "신규 컬럼 누락",
            "ShopID,ShopName,Difficulty,StartTurn,StartAlert,SuccessRateBonusPp,CandyMultiplier,EventRateBonusPp,AllowedEventTags,BackgroundImageKey,UnlockCondition,Enabled,Notes\n"
            + "SHOP_001,동네 사탕 가게,1,15,0,10,1.0,0,COMMON,shop_neighborhood,NONE,1,",
            1);
        failed += ExpectErrors(
            "빈 ID",
            ShopHeader + "\n"
            + ",동네 사탕 가게,1,15,0,10,1.0,0,COMMON,shop_neighborhood,NONE," + ShopTail,
            1);
        failed += ExpectErrors(
            "잘못된 INT",
            ShopHeader + "\n"
            + "SHOP_001,동네 사탕 가게,1,abc,0,10,1.0,0,COMMON,shop_neighborhood,NONE," + ShopTail,
            1);
        failed += ExpectErrors(
            "잘못된 등급 토큰",
            ShopHeader + "\n"
            + "SHOP_001,동네 사탕 가게,1,15,0,10,1.0,0,COMMON,shop_neighborhood,NONE,1,10,FAR_AWAY,,,,0,",
            1);
        failed += ExpectErrors(
            "다른 축의 등급 토큰",
            ShopHeader + "\n"
            + "SHOP_001,동네 사탕 가게,1,15,0,10,1.0,0,COMMON,shop_neighborhood,NONE,1,10,,,,NEAR,0,",
            1);
        failed += ExpectErrors(
            "음수 SpawnWeight",
            ShopHeader + "\n"
            + "SHOP_001,동네 사탕 가게,1,15,0,10,1.0,0,COMMON,shop_neighborhood,NONE,1,-1,,,,,0,",
            1);
        failed += ExpectErrors(
            "음수 EnergyCost",
            ShopHeader + "\n"
            + "SHOP_001,동네 사탕 가게,1,15,0,10,1.0,0,COMMON,shop_neighborhood,NONE,1,10,,,,,-1,",
            1);
        failed += ExpectErrors(
            "SpawnWeight가 INT 아님",
            ShopHeader + "\n"
            + "SHOP_001,동네 사탕 가게,1,15,0,10,1.0,0,COMMON,shop_neighborhood,NONE,1,many,,,,,0,",
            1);
        failed += ExpectErrors(
            "가게 이름 10자 초과 (공백 포함)",
            ShopHeader + "\n"
            + "SHOP_001,가나다라 마바사아자차,1,15,0,10,1.0,0,COMMON,shop_neighborhood,NONE," + ShopTail,
            1);
        failed += ExpectKoreanShop();
        failed += ExpectNewShopColumns();
        failed += ExpectNameLengthBoundary();
        failed += ExpectQuotedCommaField();
        failed += ExpectBackgroundKeyRejected(",shop_bg_001,", ",shop_bg_missing,", "없는 배경 키");
        failed += ExpectBackgroundKeyRejected(",shop_bg_001,", ",,", "빈 배경 키");

        if (failed == 0)
        {
            Debug.Log(CsvDatabase.LogPrefix + "유효성 자가 검사 통과 (중복 ID, 필수 컬럼 누락, 빈 ID, 잘못된 수치, 한국어 텍스트, 신규 컬럼, 배경 키 참조)");
            return true;
        }

        Debug.LogError(CsvDatabase.LogPrefix + "유효성 자가 검사 실패: " + failed + "건");
        return false;
    }

    static int ExpectErrors(string caseName, string csvText, int minimumErrors)
    {
        int errors = CsvDatabase.CountValidationErrors("ShopTable", csvText);
        if (errors >= minimumErrors)
        {
            Debug.Log(CsvDatabase.LogPrefix + "자가 검사 '" + caseName + "' 통과 (오류 " + errors + "건 감지)");
            return 0;
        }

        Debug.LogError(CsvDatabase.LogPrefix + "자가 검사 '" + caseName + "' 실패: 오류가 감지되지 않았습니다.");
        return 1;
    }

    static int ExpectKoreanShop()
    {
        string csv = ShopHeader + "\n"
            + "SHOP_001,동네 사탕 가게,1,15,0,10,1.0,0,COMMON,shop_neighborhood,NONE," + ShopTail;
        CsvDatabase database = CsvDatabase.LoadSingleTableFromText("ShopTable", csv);
        ShopRow shop;
        if (database.ErrorCount == 0
            && database.TryGetShop("SHOP_001", out shop)
            && shop.ShopName == "동네 사탕 가게")
        {
            Debug.Log(CsvDatabase.LogPrefix + "자가 검사 '정상 한국어 행' 통과: " + shop.ShopName);
            return 0;
        }

        Debug.LogError(CsvDatabase.LogPrefix + "자가 검사 '정상 한국어 행' 실패");
        return 1;
    }

    static int ExpectNewShopColumns()
    {
        // Row 1 assigns every new column; row 2 leaves grades blank (not assigned yet, NONE).
        string csv = ShopHeader + "\n"
            + "SHOP_T01,테스트 가게 하나,1,15,0,10,1.0,0,COMMON,shop_a,NONE,1,70,NEAR,LARGE,LOOSE,TOP,3,\n"
            + "SHOP_T02,테스트 가게 둘,1,15,0,10,1.0,0,COMMON,shop_b,NONE,1,0,,,,,0,";
        CsvDatabase database = CsvDatabase.LoadSingleTableFromText("ShopTable", csv);
        ShopRow a;
        ShopRow b;
        if (database.ErrorCount == 0
            && database.TryGetShop("SHOP_T01", out a)
            && database.TryGetShop("SHOP_T02", out b)
            && a.SpawnWeight == 70
            && a.DistanceGrade == DistanceGrade.NEAR
            && a.ScaleGrade == ScaleGrade.LARGE
            && a.SecurityGrade == SecurityGrade.LOOSE
            && a.GoodsGrade == GoodsGrade.TOP
            && a.EnergyCost == 3
            && b.SpawnWeight == 0
            && b.DistanceGrade == DistanceGrade.NONE
            && b.ScaleGrade == ScaleGrade.NONE
            && b.SecurityGrade == SecurityGrade.NONE
            && b.GoodsGrade == GoodsGrade.NONE
            && b.EnergyCost == 0)
        {
            Debug.Log(CsvDatabase.LogPrefix + "자가 검사 '신규 ShopTable 컬럼' 통과");
            return 0;
        }

        Debug.LogError(CsvDatabase.LogPrefix + "자가 검사 '신규 ShopTable 컬럼' 실패");
        return 1;
    }

    static int ExpectNameLengthBoundary()
    {
        // 4 + space + 5 = exactly 10 characters, spaces included, is allowed.
        string csv = ShopHeader + "\n"
            + "SHOP_T01,가나다라 마바사아자,1,15,0,10,1.0,0,COMMON,shop_a,NONE," + ShopTail;
        CsvDatabase database = CsvDatabase.LoadSingleTableFromText("ShopTable", csv);
        ShopRow shop;
        if (database.ErrorCount == 0
            && database.TryGetShop("SHOP_T01", out shop)
            && shop.ShopName.Length == CsvDatabase.MaxShopNameLength)
        {
            Debug.Log(CsvDatabase.LogPrefix + "자가 검사 '가게 이름 10자 경계' 통과");
            return 0;
        }

        Debug.LogError(CsvDatabase.LogPrefix + "자가 검사 '가게 이름 10자 경계' 실패");
        return 1;
    }

    static int ExpectQuotedCommaField()
    {
        var records = CsvReader.ReadRecords("A,B\n\"파스텔톤 동네 사탕 가게, 키치한 2D 일러스트\",x");
        if (records.Count == 2
            && records[1].Length >= 2
            && records[1][0] == "파스텔톤 동네 사탕 가게, 키치한 2D 일러스트")
        {
            Debug.Log(CsvDatabase.LogPrefix + "자가 검사 '따옴표 콤마 필드' 통과");
            return 0;
        }

        Debug.LogError(CsvDatabase.LogPrefix + "자가 검사 '따옴표 콤마 필드' 실패");
        return 1;
    }

    static int ExpectBackgroundKeyRejected(string from, string to, string caseName)
    {
        Dictionary<string, string> texts;
        if (!TryCopyGameplayTexts(out texts))
        {
            Debug.LogError(CsvDatabase.LogPrefix + "자가 검사 '" + caseName + "' 실패: 테이블 텍스트를 읽지 못했습니다.");
            return 1;
        }

        CsvDatabase clean = CsvDatabase.LoadFromTexts(texts);
        if (clean.HasErrors)
        {
            Debug.LogError(CsvDatabase.LogPrefix + "자가 검사 '" + caseName + "' 실패: 원본 테이블에 오류 " + clean.ErrorCount + "건");
            return 1;
        }

        if (texts["ShopTable"].IndexOf(from, System.StringComparison.Ordinal) < 0)
        {
            Debug.LogError(CsvDatabase.LogPrefix + "자가 검사 '" + caseName + "' 실패: 치환 대상을 찾지 못했습니다.");
            return 1;
        }

        texts["ShopTable"] = texts["ShopTable"].Replace(from, to);
        CsvDatabase broken = CsvDatabase.LoadFromTexts(texts);
        if (broken.HasErrors && broken.ErrorCount >= 1)
        {
            Debug.Log(CsvDatabase.LogPrefix + "자가 검사 '" + caseName + "' 통과 (오류 " + broken.ErrorCount + "건 감지)");
            return 0;
        }

        Debug.LogError(CsvDatabase.LogPrefix + "자가 검사 '" + caseName + "' 실패: 오류가 감지되지 않았습니다.");
        return 1;
    }

    static bool TryCopyGameplayTexts(out Dictionary<string, string> texts)
    {
        texts = new Dictionary<string, string>(System.StringComparer.Ordinal);
        string[] names =
        {
            "GameConfig", "ShopTable", "PhaseTable", "ActionTable", "EventTable",
            "EventOptionTable", "AchievementTable", "AssetTable", "EffectTable",
            "ShopRumorTable", "ShopRumorCandidate"
        };
        for (int i = 0; i < names.Length; i++)
        {
            TextAsset asset = Resources.Load<TextAsset>(CsvDatabase.ResourceFolder + "/" + names[i]);
            if (asset == null)
                return false;
            texts[names[i]] = CsvReader.DecodeUtf8(asset.bytes);
        }

        return true;
    }

#if UNITY_EDITOR
    [UnityEditor.MenuItem("Candy Thief/V02/Load CSV Tables")]
    static void MenuLoad()
    {
        LoadAndReport();
    }

    [UnityEditor.MenuItem("Candy Thief/V02/Run CSV Validation Self-Test")]
    static void MenuSelfTest()
    {
        RunValidationSelfTest();
    }
#endif
}
