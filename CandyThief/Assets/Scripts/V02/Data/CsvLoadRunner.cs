using UnityEngine;

public static class CsvLoadRunner
{
    const string ShopHeader =
        "ShopID,ShopName,Difficulty,StartTurn,StartAlert,SuccessRateBonusPp,CandyMultiplier,EventRateBonusPp,AllowedEventTags,BackgroundImageKey,UnlockCondition,Enabled,Notes";

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

        if (!found1 || !found2 || !found3 || database.EnabledShops.Count != 3)
        {
            Debug.LogError(CsvDatabase.LogPrefix + "ShopTable 가게 3종 조회에 실패했습니다.");
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
            + "SHOP_001,동네 사탕 가게,1,15,0,10,1.0,0,COMMON,shop_neighborhood,NONE,1,\n"
            + "SHOP_001,대형 캔디숍,2,18,10,0,1.5,5,COMMON,shop_mall,NONE,1,",
            1);
        failed += ExpectErrors(
            "필수 컬럼 누락",
            "ShopName,Difficulty,StartTurn,StartAlert,SuccessRateBonusPp,CandyMultiplier,EventRateBonusPp,AllowedEventTags,BackgroundImageKey,UnlockCondition,Enabled,Notes\n"
            + "동네 사탕 가게,1,15,0,10,1.0,0,COMMON,shop_neighborhood,NONE,1,",
            1);
        failed += ExpectErrors(
            "빈 ID",
            ShopHeader + "\n"
            + ",동네 사탕 가게,1,15,0,10,1.0,0,COMMON,shop_neighborhood,NONE,1,",
            1);
        failed += ExpectErrors(
            "잘못된 INT",
            ShopHeader + "\n"
            + "SHOP_001,동네 사탕 가게,1,abc,0,10,1.0,0,COMMON,shop_neighborhood,NONE,1,",
            1);
        failed += ExpectKoreanShop();
        failed += ExpectQuotedCommaField();

        if (failed == 0)
        {
            Debug.Log(CsvDatabase.LogPrefix + "유효성 자가 검사 통과 (중복 ID, 필수 컬럼 누락, 빈 ID, 잘못된 수치, 한국어 텍스트)");
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
            + "SHOP_001,동네 사탕 가게,1,15,0,10,1.0,0,COMMON,shop_neighborhood,NONE,1,";
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
