using System;
using System.Collections.Generic;
using UnityEngine;

// OP-DEV-03 checks. Draw fixtures are built in code; they are never written to ShopTable.csv.
public static class ShopListDrawerSelfTest
{
    const int ListSize = 4;

    public static bool RunAll(CsvDatabase database)
    {
        if (database == null || !database.Loaded || database.HasErrors)
        {
            Debug.LogError(ShopListDrawer.LogPrefix + "자가 검사 실패: CsvDatabase가 준비되지 않았습니다.");
            return false;
        }

        int failed = 0;
        failed += Expect("1 신규 컬럼 로드, 기존 3종 판정 수치 유지", TestLiveColumnsAndFrozenNumbers(database));
        failed += Expect("2 정식 데이터 추첨 (4종 미만이면 명확한 실패)", TestLiveDatabaseDraw(database));
        failed += Expect("3 SpawnWeight 0 제외", TestZeroWeightExcluded());
        failed += Expect("4 비활성 가게 제외", TestDisabledExcluded());
        failed += Expect("5 목록 내 ID 중복 없음", TestNoDuplicateIds());
        failed += Expect("6 가중치 순서 (비복원, 고정 난수열)", TestWeightedPickOrder());
        failed += Expect("7 가중치 비율 (시드 고정 통계)", TestWeightRatio());
        failed += Expect("8 직전 목록 제외", TestPreviousListExcluded());
        failed += Expect("9 후보 부족 시 직전 제외만 완화", TestExclusionRelaxed());
        failed += Expect("10 유효 후보 4종 미만이면 실패", TestNotEnoughCandidates());
        failed += Expect("11 잘못된 요청", TestInvalidRequests());
        failed += Expect("12 UnityIntRoll 기본 난수 사용", TestDefaultRoll());

        if (failed == 0)
        {
            Debug.Log(ShopListDrawer.LogPrefix + "OP-DEV-03 자가 검사 12건 통과");
            return true;
        }

        Debug.LogError(ShopListDrawer.LogPrefix + "OP-DEV-03 자가 검사 실패: " + failed + "건");
        return false;
    }

    static int Expect(string name, bool passed)
    {
        if (passed)
        {
            Debug.Log(ShopListDrawer.LogPrefix + "PASS " + name);
            return 0;
        }

        Debug.LogError(ShopListDrawer.LogPrefix + "FAIL " + name);
        return 1;
    }

    static bool TestLiveColumnsAndFrozenNumbers(CsvDatabase database)
    {
        ShopRow s1, s2, s3;
        if (!database.TryGetShop("SHOP_001", out s1)
            || !database.TryGetShop("SHOP_002", out s2)
            || !database.TryGetShop("SHOP_003", out s3))
            return false;

        // D-021: ids and resolve numbers stay as approved.
        bool frozen = s1.StartTurn == 15 && s1.StartAlert == 0 && s1.SuccessRateBonusPp == 10
            && Mathf.Approximately(s1.CandyMultiplier, 1.0f) && s1.EventRateBonusPp == 0
            && s2.StartTurn == 18 && s2.StartAlert == 10 && s2.SuccessRateBonusPp == 0
            && Mathf.Approximately(s2.CandyMultiplier, 1.5f) && s2.EventRateBonusPp == 5
            && s3.StartTurn == 22 && s3.StartAlert == 20 && s3.SuccessRateBonusPp == -10
            && Mathf.Approximately(s3.CandyMultiplier, 2.5f) && s3.EventRateBonusPp == 10;
        if (!frozen)
            return false;

        for (int i = 0; i < database.EnabledShops.Count; i++)
        {
            ShopRow shop = database.EnabledShops[i];
            if (shop.SpawnWeight < 0 || shop.EnergyCost < 0
                || shop.ShopName.Length > CsvDatabase.MaxShopNameLength)
                return false;
        }

        return true;
    }

    static bool TestLiveDatabaseDraw(CsvDatabase database)
    {
        int drawable = 0;
        for (int i = 0; i < database.EnabledShops.Count; i++)
        {
            if (ShopListDrawer.IsDrawable(database.EnabledShops[i]))
                drawable++;
        }

        ShopListDrawResult result = new ShopListDrawer(new SeededIntRoll(7)).Draw(database, null, ListSize);
        if (drawable < ListSize)
        {
            // Not enough registered content is a defined result, not a crash and not a test failure.
            return result.Status == ShopListDrawStatus.NotEnoughCandidates
                && result.DrawablePoolCount == drawable
                && result.ShopIds.Count == 0
                && !result.Succeeded;
        }

        return result.Succeeded && result.ShopIds.Count == ListSize && AllDistinct(result.ShopIds);
    }

    static bool TestZeroWeightExcluded()
    {
        List<ShopRow> shops = Shops(("A", 100), ("B", 0), ("C", 50), ("D", 50), ("E", 50));
        for (int seed = 0; seed < 200; seed++)
        {
            ShopListDrawResult result = new ShopListDrawer(new SeededIntRoll(seed)).Draw(shops, null, ListSize);
            if (!result.Succeeded || result.DrawablePoolCount != 4 || result.ShopIds.Contains("B"))
                return false;
        }

        // Only four are drawable, so five cannot be satisfied and weight 0 is never used to fill.
        ShopListDrawResult five = new ShopListDrawer(new SeededIntRoll(1)).Draw(shops, null, 5);
        return five.Status == ShopListDrawStatus.NotEnoughCandidates && five.DrawablePoolCount == 4;
    }

    static bool TestDisabledExcluded()
    {
        List<ShopRow> shops = Shops(("A", 10), ("B", 10), ("C", 10), ("D", 10), ("OFF", 1000));
        shops[4].Enabled = false;
        for (int seed = 0; seed < 200; seed++)
        {
            ShopListDrawResult result = new ShopListDrawer(new SeededIntRoll(seed)).Draw(shops, null, ListSize);
            if (!result.Succeeded || result.ShopIds.Contains("OFF") || result.DrawablePoolCount != 4)
                return false;
        }

        return true;
    }

    static bool TestNoDuplicateIds()
    {
        List<ShopRow> shops = Shops(("A", 100), ("B", 70), ("C", 60), ("D", 15), ("E", 100), ("F", 30), ("G", 5), ("H", 1));
        for (int seed = 0; seed < 2000; seed++)
        {
            ShopListDrawResult result = new ShopListDrawer(new SeededIntRoll(seed)).Draw(shops, null, ListSize);
            if (!result.Succeeded || result.ShopIds.Count != ListSize || !AllDistinct(result.ShopIds))
                return false;
        }

        // The same ShopID twice in the input still counts once.
        List<ShopRow> dup = Shops(("A", 10), ("A", 10), ("B", 10), ("C", 10));
        ShopListDrawResult dupResult = new ShopListDrawer(new SeededIntRoll(3)).Draw(dup, null, ListSize);
        if (dupResult.Succeeded || dupResult.DrawablePoolCount != 3)
            return false;

        // Taking every shop is a full permutation with no repeats.
        ShopListDrawResult all = new ShopListDrawer(new SeededIntRoll(11)).Draw(shops, null, shops.Count);
        return all.Succeeded && all.ShopIds.Count == shops.Count && AllDistinct(all.ShopIds);
    }

    static bool TestWeightedPickOrder()
    {
        // Total 5. roll 2 lands in B (cumulative A=1, B=3), then D (A=1, C=2, D=3), then A, then C.
        List<ShopRow> shops = Shops(("A", 1), ("B", 2), ("C", 1), ("D", 1));
        ShopListDrawResult result = new ShopListDrawer(new SequenceIntRoll(2, 2, 0, 0)).Draw(shops, null, 4);
        if (!result.Succeeded
            || result.ShopIds.Count != 4
            || result.ShopIds[0] != "B" || result.ShopIds[1] != "D"
            || result.ShopIds[2] != "A" || result.ShopIds[3] != "C")
            return false;

        // FixedIntRoll(0) always takes the first remaining; a huge roll clamps to the last remaining.
        ShopListDrawResult first = new ShopListDrawer(new FixedIntRoll(0)).Draw(shops, null, 4);
        ShopListDrawResult last = new ShopListDrawer(new FixedIntRoll(int.MaxValue)).Draw(shops, null, 4);
        return first.Succeeded && Join(first.ShopIds) == "A,B,C,D"
            && last.Succeeded && Join(last.ShopIds) == "D,C,B,A";
    }

    static bool TestWeightRatio()
    {
        // Seeded, so the result is repeatable. 100 vs 15 expects A about 87% of single picks.
        List<ShopRow> shops = Shops(("A", 100), ("B", 15));
        var drawer = new ShopListDrawer(new SeededIntRoll(20240101));
        const int trials = 20000;
        int a = 0;
        for (int i = 0; i < trials; i++)
        {
            ShopListDrawResult result = drawer.Draw(shops, null, 1);
            if (!result.Succeeded || result.ShopIds.Count != 1)
                return false;
            if (result.ShopIds[0] == "A")
                a++;
        }

        float share = a / (float)trials;
        float expected = 100f / 115f;
        return Mathf.Abs(share - expected) < 0.02f;
    }

    static bool TestPreviousListExcluded()
    {
        List<ShopRow> shops = Shops(("A", 100), ("B", 100), ("C", 100), ("D", 100), ("E", 100), ("F", 100), ("G", 100), ("H", 100));
        string[] previous = { "A", "B", "C", "D" };
        for (int seed = 0; seed < 300; seed++)
        {
            ShopListDrawResult result = new ShopListDrawer(new SeededIntRoll(seed)).Draw(shops, previous, ListSize);
            if (!result.Succeeded || result.ExclusionRelaxed || Join(Sorted(result.ShopIds)) != "E,F,G,H")
                return false;
        }

        return true;
    }

    static bool TestExclusionRelaxed()
    {
        // Five drawable, four excluded: only one is left, so the exclusion is dropped and four are drawn.
        List<ShopRow> five = Shops(("A", 10), ("B", 10), ("C", 10), ("D", 10), ("E", 10));
        string[] previous = { "A", "B", "C", "D" };
        bool sawExcluded = false;
        for (int seed = 0; seed < 300; seed++)
        {
            ShopListDrawResult result = new ShopListDrawer(new SeededIntRoll(seed)).Draw(five, previous, ListSize);
            if (!result.Succeeded || !result.ExclusionRelaxed
                || result.ShopIds.Count != ListSize || !AllDistinct(result.ShopIds))
                return false;
            for (int i = 0; i < result.ShopIds.Count; i++)
            {
                if (Array.IndexOf(previous, result.ShopIds[i]) >= 0)
                    sawExcluded = true;
            }
        }

        if (!sawExcluded)
            return false;

        // Relaxing the exclusion must not relax weight 0 or Enabled.
        List<ShopRow> mixed = Shops(("A", 10), ("B", 10), ("C", 10), ("D", 10), ("ZERO", 0), ("OFF", 10));
        mixed[5].Enabled = false;
        for (int seed = 0; seed < 200; seed++)
        {
            ShopListDrawResult result = new ShopListDrawer(new SeededIntRoll(seed)).Draw(mixed, previous, ListSize);
            if (!result.Succeeded || !result.ExclusionRelaxed
                || result.ShopIds.Contains("ZERO") || result.ShopIds.Contains("OFF"))
                return false;
        }

        // Exclusion ids that are not on the board do not trigger relaxing.
        ShopListDrawResult unknown = new ShopListDrawer(new SeededIntRoll(5))
            .Draw(Shops(("A", 10), ("B", 10), ("C", 10), ("D", 10)), new[] { "X", "Y" }, ListSize);
        return unknown.Succeeded && !unknown.ExclusionRelaxed;
    }

    static bool TestNotEnoughCandidates()
    {
        List<ShopRow> three = Shops(("A", 10), ("B", 10), ("C", 10));
        ShopListDrawResult plain = new ShopListDrawer(new SeededIntRoll(1)).Draw(three, null, ListSize);
        ShopListDrawResult withExclusion = new ShopListDrawer(new SeededIntRoll(1)).Draw(three, new[] { "A" }, ListSize);
        ShopListDrawResult empty = new ShopListDrawer(new SeededIntRoll(1)).Draw(new List<ShopRow>(), null, ListSize);
        ShopListDrawResult none = new ShopListDrawer(new SeededIntRoll(1)).Draw((IReadOnlyList<ShopRow>)null, null, ListSize);

        return IsNotEnough(plain, 3) && IsNotEnough(withExclusion, 3)
            && IsNotEnough(empty, 0) && IsNotEnough(none, 0);
    }

    static bool IsNotEnough(ShopListDrawResult result, int pool)
    {
        return result.Status == ShopListDrawStatus.NotEnoughCandidates
            && result.DrawablePoolCount == pool
            && result.RequestedCount == ListSize
            && result.ShopIds.Count == 0
            && !result.ExclusionRelaxed
            && !result.Succeeded;
    }

    static bool TestInvalidRequests()
    {
        List<ShopRow> shops = Shops(("A", 10), ("B", 10), ("C", 10), ("D", 10));
        ShopListDrawResult zero = new ShopListDrawer(new SeededIntRoll(1)).Draw(shops, null, 0);
        ShopListDrawResult negative = new ShopListDrawer(new SeededIntRoll(1)).Draw(shops, null, -1);
        if (zero.Status != ShopListDrawStatus.InvalidCount || negative.Status != ShopListDrawStatus.InvalidCount
            || zero.ShopIds.Count != 0 || negative.ShopIds.Count != 0)
            return false;

        // Weights whose sum cannot fit the integer roll interface are rejected, not wrapped.
        List<ShopRow> huge = Shops(("A", int.MaxValue), ("B", int.MaxValue), ("C", 1), ("D", 1));
        ShopListDrawResult overflow = new ShopListDrawer(new SeededIntRoll(1)).Draw(huge, null, ListSize);
        return overflow.Status == ShopListDrawStatus.InvalidWeights && overflow.ShopIds.Count == 0;
    }

    static bool TestDefaultRoll()
    {
        List<ShopRow> shops = Shops(("A", 100), ("B", 70), ("C", 60), ("D", 15), ("E", 100), ("F", 30));
        var drawer = new ShopListDrawer();
        for (int i = 0; i < 200; i++)
        {
            ShopListDrawResult result = drawer.Draw(shops, null, ListSize);
            if (!result.Succeeded || result.ShopIds.Count != ListSize || !AllDistinct(result.ShopIds))
                return false;
        }

        return true;
    }

    static List<ShopRow> Shops(params (string id, int weight)[] entries)
    {
        var list = new List<ShopRow>();
        for (int i = 0; i < entries.Length; i++)
        {
            list.Add(new ShopRow
            {
                ShopID = entries[i].id,
                ShopName = entries[i].id,
                SpawnWeight = entries[i].weight,
                CandyMultiplier = 1f,
                Enabled = true
            });
        }

        return list;
    }

    static bool AllDistinct(List<string> ids)
    {
        var seen = new HashSet<string>(StringComparer.Ordinal);
        for (int i = 0; i < ids.Count; i++)
        {
            if (!seen.Add(ids[i]))
                return false;
        }

        return true;
    }

    static List<string> Sorted(List<string> ids)
    {
        var copy = new List<string>(ids);
        copy.Sort(StringComparer.Ordinal);
        return copy;
    }

    static string Join(List<string> ids)
    {
        return string.Join(",", ids.ToArray());
    }

    // Test rolls. FixedIntRoll (production file) covers constant rolls.
    internal sealed class SequenceIntRoll : IIntRoll
    {
        readonly int[] values;
        int next;

        public SequenceIntRoll(params int[] values)
        {
            this.values = values;
        }

        public int Next(int maxExclusive)
        {
            if (maxExclusive <= 0)
                return 0;
            int value = values[next % values.Length];
            next++;
            if (value < 0)
                return 0;
            return value >= maxExclusive ? maxExclusive - 1 : value;
        }
    }

    internal sealed class SeededIntRoll : IIntRoll
    {
        readonly System.Random random;

        public SeededIntRoll(int seed)
        {
            random = new System.Random(seed);
        }

        public int Next(int maxExclusive)
        {
            return maxExclusive <= 0 ? 0 : random.Next(maxExclusive);
        }
    }
}
