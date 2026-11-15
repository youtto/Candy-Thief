using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

// P-016 가게 데이터 + OP-DEV-04 소문 검사.
// 가게 20종은 정식 ShopTable.csv 를 검증한다. 소문 행은 코드 픽스처이며 정식 CSV(Resources/Table)에는 들어가지 않는다.
public static class ShopRumorSelfTest
{
    public const string LogPrefix = "[ShopRumor] ";

    static readonly string[] BaseTables =
    {
        "GameConfig", "ShopTable", "PhaseTable", "ActionTable", "EventTable",
        "EventOptionTable", "AchievementTable", "AssetTable", "EffectTable"
    };

    internal const string RumorHeader =
        "RumorID,RumorName,RumorText,HintText,ConflictGroup,SuccessRateBonusPp,AlertDelta,CandyMultiplierDelta,EventRateBonusPp,Enabled,Notes\r\n";
    internal const string CandidateHeader = "ShopID,RumorID,Weight\r\n";

    // 승인 로스터 (P-016, D-027) SHOP_004~020. 형식: ID|이름|가중치|거리|규모|경비|상품|턴|경계도|성공pp|배율|이벤트pp
    // SHOP_001~003은 TestLiveShops 에서 별도로 검증한다.
    static readonly string[] ApprovedRoster =
    {
        "SHOP_004|브라운 솜사탕|85|NEAR|MEDIUM|LOOSE|NORMAL|18|0|10|1.25|5",
        "SHOP_005|로즈 캔디샵|65|MID|SMALL|NORMAL|HIGH|15|10|0|1.25|0",
        "SHOP_006|밀러 초콜릿|45|MID|MEDIUM|STRICT|HIGH|18|20|-10|1.50|5",
        "SHOP_007|모리스 제과점|85|NEAR|MEDIUM|NORMAL|NORMAL|18|10|0|1.25|5",
        "SHOP_008|루시의 잡화점|45|FAR|SMALL|LOOSE|HIGH|15|0|10|1.25|0",
        "SHOP_009|벨라 디저트|60|MID|MEDIUM|NORMAL|HIGH|18|10|0|1.50|5",
        "SHOP_010|오스카 사탕창고|55|FAR|LARGE|NORMAL|NORMAL|22|10|0|1.75|10",
        "SHOP_011|피에르 공방|25|FAR|SMALL|STRICT|TOP|15|20|-10|1.75|0",
        "SHOP_012|에드가 상회|10|MID|SMALL|LOOSE|TOP|15|0|10|1.75|0",
        "SHOP_013|헨리의 사탕집|100|NEAR|SMALL|LOOSE|NORMAL|15|0|10|1.00|0",
        "SHOP_014|마담 코코|20|MID|MEDIUM|NORMAL|TOP|18|10|0|2.00|5",
        "SHOP_015|올리버 제과점|60|NEAR|MEDIUM|NORMAL|HIGH|18|10|0|1.50|5",
        "SHOP_016|블랙우드 상점|20|FAR|MEDIUM|STRICT|TOP|18|20|-10|2.00|5",
        "SHOP_017|베니의 젤리샵|45|NEAR|SMALL|LOOSE|HIGH|15|0|10|1.25|0",
        "SHOP_018|몽슈슈 과자점|80|MID|MEDIUM|LOOSE|NORMAL|18|0|10|1.25|5",
        "SHOP_019|윌슨 무역상|35|FAR|LARGE|STRICT|HIGH|22|20|-10|2.00|10",
        "SHOP_020|도로시 사탕집|25|MID|SMALL|NORMAL|TOP|15|10|0|1.75|0",
    };

    static string FixtureRumors =>
        RumorHeader
        + R("T_RUM_SUCC", "", 5, 0, 0f, 0, 1)
        + R("T_RUM_ALERT", "", 0, 10, 0f, 0, 1)
        + R("T_RUM_MULT", "", 0, 0, 0.5f, 0, 1)
        + R("T_RUM_EVT", "", 0, 0, 0f, 10, 1)
        + R("T_RUM_G1A", "G1", 3, 0, 0f, 0, 1)
        + R("T_RUM_G1B", "G1", 0, 5, 0f, 0, 1)
        + R("T_RUM_OFF", "", 5, 0, 0f, 0, 0)
        + R("T_RUM_NEG", "", 0, 0, -0.6f, 0, 1)
        + R("T_RUM_BIG", "", 0, 200, 0f, 0, 1);

    // SHOP_001: 가중치 1/3/0/6/0 (G1A,G1B 상충). SHOP_002: 활성 6종 + 비활성 1종. SHOP_003: 큰 경계도 소문 1종.
    static readonly string FixtureCandidates =
        CandidateHeader
        + C("SHOP_001", "T_RUM_G1A", 1) + C("SHOP_001", "T_RUM_G1B", 3) + C("SHOP_001", "T_RUM_SUCC", 0)
        + C("SHOP_001", "T_RUM_EVT", 6) + C("SHOP_001", "T_RUM_NEG", 0)
        + C("SHOP_002", "T_RUM_SUCC", 10) + C("SHOP_002", "T_RUM_ALERT", 10) + C("SHOP_002", "T_RUM_MULT", 10)
        + C("SHOP_002", "T_RUM_EVT", 10) + C("SHOP_002", "T_RUM_G1A", 10) + C("SHOP_002", "T_RUM_G1B", 10)
        + C("SHOP_002", "T_RUM_OFF", 10)
        + C("SHOP_003", "T_RUM_BIG", 10);

    public static bool RunAll(CsvDatabase database)
    {
        if (database == null || !database.Loaded || database.HasErrors)
        {
            Debug.LogError(LogPrefix + "자가 검사 실패: CsvDatabase가 준비되지 않았습니다.");
            return false;
        }

        RunState previous = RunState.Current;
        int failed = 0;
        try
        {
            failed += Expect("1 정식 가게 3종: 승인값 반영, 판정 수치 유지, 배경 참조", TestLiveShops(database));
            failed += Expect("2 정식 가게 20종: ID/이름/등급/가중치/수치/배경 참조", TestLiveRoster(database));
            failed += Expect("3 정식 데이터 4장 가게 목록 추첨", TestLiveDraw(database));
            failed += Expect("4 정식 소문 테이블: 빈 테이블 로드, 개수 가중치 40/45/15", TestLiveRumorTables(database));
            failed += Expect("5 소문 테이블 검증 오류", TestRumorLoadErrors());
            failed += Expect("6 최악 조합 CandyMultiplier<=0 로드 오류", TestWorstCaseMultiplier());
            failed += Expect("7 소문 개수 가중치 경계 (고정 난수)", TestCountBoundaries());
            failed += Expect("8 소문 개수 0/1/2 분포 (시드 통계)", TestCountDistribution());
            failed += Expect("9 소문 가중치 추첨 (고정 난수열, 비복원)", TestWeightedRumorPick());
            failed += Expect("10 중복/상충 없음, 0가중치·비활성 제외 (시드 통계)", TestNoDuplicateNoConflict());
            failed += Expect("11 후보 부족 시 가능한 만큼만", TestShortage(database));
            failed += Expect("12 개수 가중치 설정 누락 시 실패", TestMissingCountWeights());
            failed += Expect("13 소문 없음 = 기존 결과와 동일", TestNoRumorIdentical(database));
            failed += Expect("14 소문 효과 적용 (확률/시작 경계도/배율), 원본 불변", TestEffectsApplied());
            failed += Expect("15 다중 소문 합산, 잘못된 소문 거절", TestStackAndRejects());
            failed += Expect("16 런 판정 순서 유지 (D-011)", TestResolveOrderKept());
            failed += Expect("17 목록 추첨, 시작 실패 시 Current 유지", TestListAndStartFailure());
        }
        finally
        {
            RunState.Current = previous;
        }

        if (failed == 0)
        {
            Debug.Log(LogPrefix + "P-016/OP-DEV-04 자가 검사 17건 통과");
            return true;
        }

        Debug.LogError(LogPrefix + "P-016/OP-DEV-04 자가 검사 실패: " + failed + "건");
        return false;
    }

    static int Expect(string name, bool passed)
    {
        if (passed)
        {
            Debug.Log(LogPrefix + "PASS " + name);
            return 0;
        }

        Debug.LogError(LogPrefix + "FAIL " + name);
        return 1;
    }

    // ---------- 1~3 가게 데이터 ----------

    static bool TestLiveShops(CsvDatabase db)
    {
        ShopRow s1, s2, s3;
        if (!db.TryGetShop("SHOP_001", out s1) || !db.TryGetShop("SHOP_002", out s2) || !db.TryGetShop("SHOP_003", out s3))
            return false;

        bool approved =
            s1.ShopName == "토마스 사탕점" && s1.SpawnWeight == 100 && s1.BackgroundImageKey == "shop_bg_001"
            && s1.DistanceGrade == DistanceGrade.NEAR && s1.ScaleGrade == ScaleGrade.SMALL
            && s1.SecurityGrade == SecurityGrade.LOOSE && s1.GoodsGrade == GoodsGrade.NORMAL
            && s2.ShopName == "잭의 과자점" && s2.SpawnWeight == 65 && s2.BackgroundImageKey == "shop_bg_002"
            && s2.DistanceGrade == DistanceGrade.MID && s2.ScaleGrade == ScaleGrade.MEDIUM
            && s2.SecurityGrade == SecurityGrade.NORMAL && s2.GoodsGrade == GoodsGrade.HIGH
            && s3.ShopName == "르블랑 백화점" && s3.SpawnWeight == 15 && s3.BackgroundImageKey == "shop_bg_003"
            && s3.DistanceGrade == DistanceGrade.FAR && s3.ScaleGrade == ScaleGrade.LARGE
            && s3.SecurityGrade == SecurityGrade.STRICT && s3.GoodsGrade == GoodsGrade.TOP;
        // 기존 3종 판정 수치 (변경 금지)
        bool frozen =
            s1.StartTurn == 15 && s1.StartAlert == 0 && s1.SuccessRateBonusPp == 10 && Mathf.Approximately(s1.CandyMultiplier, 1.0f) && s1.EventRateBonusPp == 0
            && s2.StartTurn == 18 && s2.StartAlert == 10 && s2.SuccessRateBonusPp == 0 && Mathf.Approximately(s2.CandyMultiplier, 1.5f) && s2.EventRateBonusPp == 5
            && s3.StartTurn == 22 && s3.StartAlert == 20 && s3.SuccessRateBonusPp == -10 && Mathf.Approximately(s3.CandyMultiplier, 2.5f) && s3.EventRateBonusPp == 10;
        bool common = s1.Enabled && s2.Enabled && s3.Enabled
            && s1.EnergyCost == 0 && s2.EnergyCost == 0 && s3.EnergyCost == 0
            && s1.UnlockCondition == ConditionKey.NONE && s2.UnlockCondition == ConditionKey.NONE && s3.UnlockCondition == ConditionKey.NONE;
        AssetRow asset;
        bool background =
            db.TryGetAssetByImageKey(s1.BackgroundImageKey, out asset)
            && db.TryGetAssetByImageKey(s2.BackgroundImageKey, out asset)
            && db.TryGetAssetByImageKey(s3.BackgroundImageKey, out asset);
        return approved && frozen && common && background;
    }

    static bool TestLiveRoster(CsvDatabase db)
    {
        if (db.EnabledShops.Count != 20)
            return false;

        var ids = new HashSet<string>(StringComparer.Ordinal);
        var keys = new HashSet<string>(StringComparer.Ordinal);
        for (int i = 0; i < db.EnabledShops.Count; i++)
        {
            ShopRow s = db.EnabledShops[i];
            AssetRow asset;
            if (!ids.Add(s.ShopID) || !keys.Add(s.BackgroundImageKey)
                || s.ShopName.Length == 0 || s.ShopName.Length > CsvDatabase.MaxShopNameLength
                || s.SpawnWeight <= 0 || s.EnergyCost != 0
                || s.DistanceGrade == DistanceGrade.NONE || s.ScaleGrade == ScaleGrade.NONE
                || s.SecurityGrade == SecurityGrade.NONE || s.GoodsGrade == GoodsGrade.NONE
                || !db.TryGetAssetByImageKey(s.BackgroundImageKey, out asset))
                return false;

            string expectedId = "SHOP_" + (i + 1).ToString("D3", CultureInfo.InvariantCulture);
            string expectedKey = "shop_bg_" + (i + 1).ToString("D3", CultureInfo.InvariantCulture);
            if (s.ShopID != expectedId || s.BackgroundImageKey != expectedKey
                || s.AllowedEventTags != (i < 3 ? s.AllowedEventTags : "COMMON")
                || s.UnlockCondition != ConditionKey.NONE || !s.Enabled)
                return false;
        }

        // 승인 로스터(SHOP_004~020)의 이름/가중치/등급/판정 수치 대조
        for (int i = 0; i < ApprovedRoster.Length; i++)
        {
            string[] f = ApprovedRoster[i].Split('|');
            ShopRow s;
            if (!db.TryGetShop(f[0], out s)
                || s.ShopName != f[1] || s.SpawnWeight != int.Parse(f[2], CultureInfo.InvariantCulture)
                || s.DistanceGrade.ToString() != f[3] || s.ScaleGrade.ToString() != f[4]
                || s.SecurityGrade.ToString() != f[5] || s.GoodsGrade.ToString() != f[6]
                || s.StartTurn != int.Parse(f[7], CultureInfo.InvariantCulture)
                || s.StartAlert != int.Parse(f[8], CultureInfo.InvariantCulture)
                || s.SuccessRateBonusPp != int.Parse(f[9], CultureInfo.InvariantCulture)
                || !Mathf.Approximately(s.CandyMultiplier, float.Parse(f[10], CultureInfo.InvariantCulture))
                || s.EventRateBonusPp != int.Parse(f[11], CultureInfo.InvariantCulture))
                return false;
        }

        return true;
    }

    static bool TestLiveDraw(CsvDatabase db)
    {
        int drawable = 0;
        for (int i = 0; i < db.EnabledShops.Count; i++)
        {
            if (ShopListDrawer.IsDrawable(db.EnabledShops[i]))
                drawable++;
        }

        if (drawable != 20)
            return false;

        // 정식 데이터로 4장 추첨. 시드 100개: 항상 성공, 서로 다른 4종, 모두 정식 가게.
        for (int seed = 1; seed <= 100; seed++)
        {
            ShopListDrawResult r = new ShopListDrawer(new ShopListDrawerSelfTest.SeededIntRoll(seed)).Draw(db, null, 4);
            ShopRow shop;
            if (!r.Succeeded || r.ShopIds.Count != 4 || new HashSet<string>(r.ShopIds).Count != 4)
                return false;
            for (int i = 0; i < r.ShopIds.Count; i++)
            {
                if (!db.TryGetShop(r.ShopIds[i], out shop))
                    return false;
            }
        }

        // 직전 목록 제외 후에도 성공 (풀 20종 - 4종 = 16종, 완화 없음).
        ShopListDrawResult first = new ShopListDrawer(new ShopListDrawerSelfTest.SeededIntRoll(5)).Draw(db, null, 4);
        ShopListDrawResult second = new ShopListDrawer(new ShopListDrawerSelfTest.SeededIntRoll(6)).Draw(db, first.ShopIds, 4);
        if (!second.Succeeded || second.ExclusionRelaxed)
            return false;
        for (int i = 0; i < second.ShopIds.Count; i++)
        {
            if (first.ShopIds.Contains(second.ShopIds[i]))
                return false;
        }

        // 가중치 큰 가게가 더 자주 나온다 (SHOP_001=100 vs SHOP_012=10, 시드 고정 통계).
        var rng = new ShopListDrawerSelfTest.SeededIntRoll(31415);
        var drawer = new ShopListDrawer(rng);
        int heavy = 0;
        int light = 0;
        for (int i = 0; i < 4000; i++)
        {
            ShopListDrawResult r = drawer.Draw(db, null, 4);
            if (r.ShopIds.Contains("SHOP_001"))
                heavy++;
            if (r.ShopIds.Contains("SHOP_012"))
                light++;
        }

        return heavy > light * 3;
    }

    // ---------- 4~6 소문 테이블 ----------

    static bool TestLiveRumorTables(CsvDatabase db)
    {
        int[] w;
        return ShopRumorDrawer.TryReadCountWeights(db, out w)
            && w[0] == 40 && w[1] == 45 && w[2] == 15
            && db.EnabledRumors.Count == 0
            && db.GetRumorCandidates("SHOP_001").Count == 0;
    }

    static bool TestRumorLoadErrors()
    {
        string ok = R("T_OK", "", 1, 0, 0f, 0, 1);
        string cand = C("SHOP_001", "T_OK", 5);
        bool allRejected =
            LoadFails(ok + ok, cand)                                                        // RumorID 중복
            && LoadFails(ok, C("SHOP_999", "T_OK", 5))                                      // 없는 ShopID
            && LoadFails(ok, C("SHOP_001", "T_NONE", 5))                                    // 없는 RumorID
            && LoadFails(ok, cand + cand)                                                   // (ShopID,RumorID) 중복
            && LoadFails(ok, C("SHOP_001", "T_OK", -1))                                     // 음수 가중치
            && LoadFails(R("T_Z", "", 0, 0, 0f, 0, 1), C("SHOP_001", "T_Z", 5))             // 활성인데 효과 전부 0
            && LoadFails("T_BAD,이름,소문,힌트,,x,0,0,0,1,FIXTURE\r\n", C("SHOP_001", "T_BAD", 5)) // INT 아님
            && LoadFails("T_H,이름,소문,,,1,0,0,0,1,FIXTURE\r\n", C("SHOP_001", "T_H", 5));  // HintText 비어 있음
        // 정상 케이스는 오류 없이 로드되어야 한다.
        CsvDatabase good = LoadWith(RumorHeader + ok, CandidateHeader + cand, null);
        return allRejected && Ok(good) && good.EnabledRumors.Count == 1 && good.IsRumorCandidate("SHOP_001", "T_OK");
    }

    static bool TestWorstCaseMultiplier()
    {
        // SHOP_001 CandyMultiplier 1.0, 후보 두 소문이 각각 -0.6 → 두 개 합산 시 -0.2
        string rumors = R("T_N1", "", 0, 0, -0.6f, 0, 1) + R("T_N2", "", 0, 0, -0.6f, 0, 1);
        bool bad = LoadFails(rumors, C("SHOP_001", "T_N1", 5) + C("SHOP_001", "T_N2", 5));
        // 한쪽이 0가중치(추첨 불가)면 허용.
        CsvDatabase fine = LoadWith(RumorHeader + rumors, CandidateHeader + C("SHOP_001", "T_N1", 5) + C("SHOP_001", "T_N2", 0), null);
        return bad && Ok(fine);
    }

    // ---------- 7~12 추첨 ----------

    static bool TestCountBoundaries()
    {
        CsvDatabase db = Fixture();
        if (db == null)
            return false;

        // 개수 롤 범위: 0~39 → 0개, 40~84 → 1개, 85~99 → 2개. 이후 롤은 후보 선택.
        return CountFor(db, 0) == 0 && CountFor(db, 39) == 0
            && CountFor(db, 40) == 1 && CountFor(db, 84) == 1
            && CountFor(db, 85) == 2 && CountFor(db, 99) == 2;
    }

    static bool TestCountDistribution()
    {
        CsvDatabase db = Fixture();
        if (db == null)
            return false;

        var drawer = new ShopRumorDrawer(db, new ShopListDrawerSelfTest.SeededIntRoll(20261114));
        const int trials = 20000;
        var counts = new int[3];
        for (int i = 0; i < trials; i++)
        {
            ShopRumorDrawResult r;
            if (!drawer.TryDraw("SHOP_002", out r) || r.RequestedCount < 0 || r.RequestedCount > 2 || r.Shortfall)
                return false;
            counts[r.RumorIds.Count]++;
        }

        return Near(counts[0], trials, 0.40) && Near(counts[1], trials, 0.45) && Near(counts[2], trials, 0.15);
    }

    static bool TestWeightedRumorPick()
    {
        CsvDatabase db = Fixture();
        if (db == null)
            return false;

        // SHOP_001 유효 후보: G1A(1) G1B(3) EVT(6). 0가중치 SUCC/NEG 는 풀에 없다.
        return Draw(db, "SHOP_001", 40, 0) == "T_RUM_G1A"
            && Draw(db, "SHOP_001", 40, 1) == "T_RUM_G1B"
            && Draw(db, "SHOP_001", 40, 3) == "T_RUM_G1B"
            && Draw(db, "SHOP_001", 40, 4) == "T_RUM_EVT"
            && Draw(db, "SHOP_001", 40, 9) == "T_RUM_EVT"
            // 2개: G1A 선택 후 상충 G1B 제외 → EVT 만 남음
            && Draw(db, "SHOP_001", 85, 0, 0) == "T_RUM_G1A,T_RUM_EVT"
            && Draw(db, "SHOP_001", 85, 1, 0) == "T_RUM_G1B,T_RUM_EVT"
            // EVT 먼저 선택 후 비복원: 남은 G1A(1) G1B(3) 중 선택
            && Draw(db, "SHOP_001", 85, 9, 0) == "T_RUM_EVT,T_RUM_G1A"
            && Draw(db, "SHOP_001", 85, 9, 1) == "T_RUM_EVT,T_RUM_G1B";
    }

    static bool TestNoDuplicateNoConflict()
    {
        CsvDatabase db = Fixture();
        if (db == null)
            return false;

        string[] shops = { "SHOP_001", "SHOP_002" };
        for (int s = 0; s < shops.Length; s++)
        {
            var drawer = new ShopRumorDrawer(db, new ShopListDrawerSelfTest.SeededIntRoll(77 + s));
            for (int i = 0; i < 5000; i++)
            {
                ShopRumorDrawResult r;
                if (!drawer.TryDraw(shops[s], out r))
                    return false;

                var seen = new HashSet<string>(StringComparer.Ordinal);
                for (int j = 0; j < r.RumorIds.Count; j++)
                {
                    string id = r.RumorIds[j];
                    if (!seen.Add(id) || id == "T_RUM_OFF" || !db.IsRumorCandidate(shops[s], id))
                        return false;
                    if (shops[s] == "SHOP_001" && (id == "T_RUM_SUCC" || id == "T_RUM_NEG"))
                        return false; // 가중치 0
                }

                if (seen.Contains("T_RUM_G1A") && seen.Contains("T_RUM_G1B"))
                    return false; // 같은 ConflictGroup
            }
        }

        return true;
    }

    static bool TestShortage(CsvDatabase live)
    {
        CsvDatabase db = Fixture();
        if (db == null)
            return false;

        // SHOP_003 후보 1개: 2개 요청해도 1개만.
        ShopRumorDrawResult r3 = Result(db, "SHOP_003", 85, 0, 0);
        // 후보 행이 없는 가게(정식 데이터): 요청 2개여도 0개, 오류 아님.
        ShopRumorDrawResult none = Result(live, "SHOP_001", 85, 0, 0);
        // 후보가 모두 한 상충 그룹이면 1개.
        string rumors = R("T_A", "GX", 1, 0, 0f, 0, 1) + R("T_B", "GX", 2, 0, 0f, 0, 1);
        CsvDatabase conflictOnly = LoadWith(RumorHeader + rumors, CandidateHeader + C("SHOP_001", "T_A", 5) + C("SHOP_001", "T_B", 5), null);
        ShopRumorDrawResult g = Ok(conflictOnly) ? Result(conflictOnly, "SHOP_001", 85, 0, 0) : null;
        return r3 != null && r3.RequestedCount == 2 && r3.RumorIds.Count == 1 && r3.Shortfall
            && none != null && none.RequestedCount == 2 && none.RumorIds.Count == 0 && none.Shortfall
            && g != null && g.RumorIds.Count == 1 && g.Shortfall;
    }

    static bool TestMissingCountWeights()
    {
        Dictionary<string, string> texts = LiveTexts();
        if (texts == null)
            return false;

        var kept = new List<string>();
        foreach (string line in texts["GameConfig"].Replace("\r\n", "\n").Split('\n'))
        {
            if (!line.StartsWith("RUMOR_COUNT_WEIGHT_", StringComparison.Ordinal))
                kept.Add(line);
        }

        texts["GameConfig"] = string.Join("\r\n", kept.ToArray());
        texts["ShopRumorTable"] = FixtureRumors;
        texts["ShopRumorCandidate"] = FixtureCandidates;
        CsvDatabase db = CsvDatabase.LoadFromTexts(texts);
        ShopRumorDrawResult r;
        int[] w;
        return Ok(db)
            && !ShopRumorDrawer.TryReadCountWeights(db, out w)
            && !new ShopRumorDrawer(db, new FixedIntRoll(0)).TryDraw("SHOP_002", out r);
    }

    // ---------- 13~17 스냅샷/런 ----------

    static bool TestNoRumorIdentical(CsvDatabase db)
    {
        string[] shops = { "SHOP_001", "SHOP_002", "SHOP_003" };
        var resolver = new ActionResolver(db, new FixedChanceRoll(true));
        for (int i = 0; i < shops.Length; i++)
        {
            ShopRow row;
            RunState a, b, c;
            if (!db.TryGetShop(shops[i], out row)
                || !RunState.TryStart(db, shops[i], out a)
                || !RunState.TryStart(db, shops[i], new List<string>(), out b)
                || !RunState.TryStart(db, shops[i], null, out c))
                return false;

            if (a.Shop == row || !SameNumbers(a.Shop, row) || !SameNumbers(b.Shop, row) || !SameNumbers(c.Shop, row))
                return false;
            if (a.RemainingTurns != row.StartTurn || a.Alert != row.StartAlert || b.RemainingTurns != a.RemainingTurns || b.Alert != a.Alert)
                return false;
            if (a.RumorIds.Count != 0 || b.RumorIds.Count != 0)
                return false;
            if (resolver.ComputeEventChance(a.Shop) != resolver.ComputeEventChance(row))
                return false;

            for (int j = 0; j < db.EnabledActions.Count; j++)
            {
                if (resolver.ComputeSuccessChance(db.EnabledActions[j], a.Shop) != resolver.ComputeSuccessChance(db.EnabledActions[j], row))
                    return false;
            }
        }

        return true;
    }

    static bool TestEffectsApplied()
    {
        CsvDatabase db = Fixture();
        ShopRow row;
        ActionRow move;
        if (db == null || !db.TryGetShop("SHOP_002", out row) || !db.TryGetAction("ACT_001", out move))
            return false;

        var resolver = new ActionResolver(db, new FixedChanceRoll(true));
        int baseSuccess = resolver.ComputeSuccessChance(move, row); // 90 + 0
        int baseEvent = resolver.ComputeEventChance(row);           // 25 + 5

        RunState run;
        if (!RunState.TryStart(db, "SHOP_002", new[] { "T_RUM_SUCC", "T_RUM_ALERT" }, out run))
            return false;

        // 성공률 +5, 시작 경계도 +10 (10 → 20)
        bool chance = resolver.ComputeSuccessChance(move, run.Shop) == baseSuccess + 5;
        bool alert = run.Alert == row.StartAlert + 10 && run.Shop.StartAlert == row.StartAlert + 10;

        RunState run2;
        if (!RunState.TryStart(db, "SHOP_002", new[] { "T_RUM_MULT", "T_RUM_EVT" }, out run2))
            return false;

        bool eventChance = resolver.ComputeEventChance(run2.Shop) == baseEvent + 10;
        bool mult = Mathf.Approximately(run2.Shop.CandyMultiplier, row.CandyMultiplier + 0.5f);
        // D-009: finalReward = CeilToInt(runCandy × CandyMultiplier). 10개 기준 15 → 20.
        bool reward = Mathf.CeilToInt(10 * row.CandyMultiplier) == 15 && Mathf.CeilToInt(10 * run2.Shop.CandyMultiplier) == 20;

        // 원본 가게 행은 변하지 않는다.
        ShopRow after;
        bool untouched = db.TryGetShop("SHOP_002", out after) && ReferenceEquals(after, row)
            && row.SuccessRateBonusPp == 0 && row.StartAlert == 10 && row.EventRateBonusPp == 5 && Mathf.Approximately(row.CandyMultiplier, 1.5f)
            && !ReferenceEquals(run.Shop, row);
        return chance && alert && eventChance && mult && reward && untouched
            && run.RumorIds.Count == 2 && run.RumorIds[0] == "T_RUM_SUCC";
    }

    static bool TestStackAndRejects()
    {
        CsvDatabase db = Fixture();
        ShopRow shop1, shop2;
        if (db == null || !db.TryGetShop("SHOP_001", out shop1) || !db.TryGetShop("SHOP_002", out shop2))
            return false;

        ShopRow snap;
        string error;
        // 합산: SUCC(+5) + G1A(+3) = +8, 서로 상충 없음.
        bool stacked = ShopRumorSnapshot.TryBuild(db, shop2, new[] { "T_RUM_SUCC", "T_RUM_G1A" }, out snap, out error)
            && snap.SuccessRateBonusPp == shop2.SuccessRateBonusPp + 8;
        // 알 수 없는/비활성/후보 아님/중복/상충/3개/배율<=0
        ShopRow low = shop1.Clone();
        low.CandyMultiplier = 0.5f;
        bool rejects =
            !ShopRumorSnapshot.TryBuild(db, shop2, new[] { "T_RUM_NOPE" }, out snap, out error)
            && !ShopRumorSnapshot.TryBuild(db, shop2, new[] { "T_RUM_OFF" }, out snap, out error)
            && !ShopRumorSnapshot.TryBuild(db, shop1, new[] { "T_RUM_ALERT" }, out snap, out error)
            && !ShopRumorSnapshot.TryBuild(db, shop2, new[] { "T_RUM_SUCC", "T_RUM_SUCC" }, out snap, out error)
            && !ShopRumorSnapshot.TryBuild(db, shop2, new[] { "T_RUM_G1A", "T_RUM_G1B" }, out snap, out error)
            && !ShopRumorSnapshot.TryBuild(db, shop2, new[] { "T_RUM_SUCC", "T_RUM_ALERT", "T_RUM_EVT" }, out snap, out error)
            && !ShopRumorSnapshot.TryBuild(db, shop2, new[] { "" }, out snap, out error)
            && !ShopRumorSnapshot.TryBuild(db, low, new[] { "T_RUM_NEG" }, out snap, out error);
        // 성공률 상한: 소문이 커도 최종 확률은 MAX_SUCCESS_RATE 로 제한된다 (기존 ActionResolver 공식).
        ActionRow move;
        var resolver = new ActionResolver(db, new FixedChanceRoll(true));
        ShopRow boosted = shop2.Clone();
        boosted.SuccessRateBonusPp = 500;
        bool clamped = db.TryGetAction("ACT_001", out move) && resolver.ComputeSuccessChance(move, boosted) == db.GetInt("MAX_SUCCESS_RATE");
        return stacked && rejects && clamped;
    }

    static bool TestResolveOrderKept()
    {
        CsvDatabase db = Fixture();
        ShopRow row;
        ActionRow move;
        if (db == null || !db.TryGetShop("SHOP_003", out row) || !db.TryGetAction("ACT_001", out move))
            return false;

        var resolver = new ActionResolver(db, new FixedChanceRoll(true));

        // 소문 없는 런: TurnCost 차감 후 결과 반영.
        RunState plain;
        ActionResolveResult plainResult;
        if (!RunState.TryStart(db, "SHOP_002", out plain) || !resolver.TryResolve(plain, "ACT_001", out plainResult))
            return false;
        bool plainOk = plain.RemainingTurns == 18 - move.TurnCost && plain.Alert == 10 + move.SuccessAlert && plain.Status == RunStatus.InProgress;

        // 소문으로 시작 경계도가 상한(100)에 도달: 행동은 먼저 TurnCost 를 차감하고, 종료 판정이 이벤트보다 먼저다.
        RunState capped;
        ActionResolveResult cappedResult;
        if (!RunState.TryStart(db, "SHOP_003", new[] { "T_RUM_BIG" }, out capped) || capped.Alert != db.GetInt("MAX_ALERT"))
            return false;
        if (!resolver.TryResolve(capped, "ACT_001", out cappedResult))
            return false;

        return plainOk && cappedResult.Applied && cappedResult.Success
            && capped.RemainingTurns == row.StartTurn - move.TurnCost
            && capped.Status == RunStatus.Failed && capped.FailReason == RunFailReason.AlertLimit
            && !cappedResult.EventCheckDue && !capped.HasPendingEvent;
    }

    static bool TestListAndStartFailure()
    {
        CsvDatabase db = Fixture();
        if (db == null)
            return false;

        // 목록 한 번 추첨: 가게마다 결과 1개, 순서 유지. SHOP_001(40→1개, 롤 0 → G1A), SHOP_002(85→2개), SHOP_003(0→0개)
        var results = new List<ShopRumorDrawResult>();
        var drawer = new ShopRumorDrawer(db, new ShopListDrawerSelfTest.SequenceIntRoll(40, 0, 85, 0, 0, 0));
        bool listOk = drawer.TryDrawForList(new[] { "SHOP_001", "SHOP_002", "SHOP_003" }, results)
            && results.Count == 3 && results[0].ShopID == "SHOP_001" && results[2].ShopID == "SHOP_003"
            && results[0].RumorIds.Count == 1 && results[0].RumorIds[0] == "T_RUM_G1A"
            && results[1].RumorIds.Count == 2 && results[2].RumorIds.Count == 0;
        // 없는 가게가 섞이면 실패하고 결과는 비운다.
        bool badList = !drawer.TryDrawForList(new[] { "SHOP_001", "SHOP_404" }, results) && results.Count == 0;

        // 잘못된 소문으로 런 시작이 실패하면 RunState.Current 는 그대로다.
        RunState current;
        if (!RunState.TryStart(db, "SHOP_001", out current))
            return false;
        RunState failedStart;
        bool startRejected = !RunState.TryStart(db, "SHOP_001", new[] { "T_RUM_ALERT" }, out failedStart)
            && failedStart == null && ReferenceEquals(RunState.Current, current);
        return listOk && badList && startRejected;
    }

    // ---------- 헬퍼 ----------

    internal static string R(string id, string group, int success, int alert, float mult, int evt, int enabled)
    {
        return string.Format(CultureInfo.InvariantCulture, "{0},{0} 이름,{0} 소문,{0} 힌트,{1},{2},{3},{4},{5},{6},FIXTURE\r\n",
            id, group, success, alert, mult, evt, enabled);
    }

    internal static string C(string shopId, string rumorId, int weight)
    {
        return string.Format(CultureInfo.InvariantCulture, "{0},{1},{2}\r\n", shopId, rumorId, weight);
    }

    internal static Dictionary<string, string> LiveTexts()
    {
        var texts = new Dictionary<string, string>(StringComparer.Ordinal);
        for (int i = 0; i < BaseTables.Length; i++)
        {
            TextAsset asset = Resources.Load<TextAsset>("Table/" + BaseTables[i]);
            if (asset == null)
                return null;
            texts[BaseTables[i]] = asset.text;
        }

        return texts;
    }

    // 정식 9개 테이블 + 선택한 픽스처 텍스트로 로드한다. null 인자는 정식 텍스트(소문 테이블은 없음)를 쓴다.
    static CsvDatabase LoadWith(string rumorCsv, string candidateCsv, string shopCsv)
    {
        Dictionary<string, string> texts = LiveTexts();
        if (texts == null)
            return null;

        if (rumorCsv != null)
            texts["ShopRumorTable"] = rumorCsv;
        if (candidateCsv != null)
            texts["ShopRumorCandidate"] = candidateCsv;
        if (shopCsv != null)
            texts["ShopTable"] = shopCsv;
        return CsvDatabase.LoadFromTexts(texts);
    }

    static CsvDatabase Fixture()
    {
        CsvDatabase db = LoadWith(FixtureRumors, FixtureCandidates, null);
        return Ok(db) ? db : null;
    }

    static bool Ok(CsvDatabase db)
    {
        return db != null && db.Loaded && !db.HasErrors;
    }

    // 오류가 있어야 정상인 입력. (의도된 LogError 가 출력된다.)
    static bool LoadFails(string rumorRows, string candidateRows)
    {
        CsvDatabase db = LoadWith(RumorHeader + rumorRows, CandidateHeader + candidateRows, null);
        return db != null && db.HasErrors;
    }

    static ShopRumorDrawResult Result(CsvDatabase db, string shopId, params int[] rolls)
    {
        ShopRumorDrawResult r;
        return new ShopRumorDrawer(db, new ShopListDrawerSelfTest.SequenceIntRoll(rolls)).TryDraw(shopId, out r) ? r : null;
    }

    static string Draw(CsvDatabase db, string shopId, params int[] rolls)
    {
        ShopRumorDrawResult r = Result(db, shopId, rolls);
        return r == null ? "<fail>" : string.Join(",", r.RumorIds.ToArray());
    }

    static int CountFor(CsvDatabase db, int countRoll)
    {
        ShopRumorDrawResult r = Result(db, "SHOP_002", countRoll, 0, 0);
        return r == null ? -1 : r.RequestedCount;
    }

    static bool Near(int count, int total, double expected)
    {
        return Math.Abs((double)count / total - expected) < 0.02;
    }

    static bool SameNumbers(ShopRow a, ShopRow b)
    {
        return a != null && b != null
            && a.ShopID == b.ShopID && a.StartTurn == b.StartTurn && a.StartAlert == b.StartAlert
            && a.SuccessRateBonusPp == b.SuccessRateBonusPp && a.EventRateBonusPp == b.EventRateBonusPp
            && Mathf.Approximately(a.CandyMultiplier, b.CandyMultiplier)
            && a.AllowedEventTags == b.AllowedEventTags && a.BackgroundImageKey == b.BackgroundImageKey;
    }
}
