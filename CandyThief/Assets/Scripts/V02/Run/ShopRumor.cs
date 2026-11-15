using System;
using System.Collections.Generic;
using UnityEngine;

// OP-DEV-04 (D-022, D-026): 가게별 소문 추첨 + 런 시작 스냅샷.
// 소문은 이벤트가 아니다. 효과는 SuccessRateBonusPp / StartAlert / CandyMultiplier / EventRateBonusPp 4종 보정치뿐이며
// 런 시작 시 ShopRow 복사본(스냅샷)에만 더해진다. 원본 테이블 행은 수정하지 않는다.
public sealed class ShopRumorDrawResult
{
    public string ShopID = string.Empty;
    public int RequestedCount;
    public readonly List<string> RumorIds = new List<string>();

    // 유효 후보 부족(또는 상충 그룹 제외)으로 뽑으려던 개수를 채우지 못했는지.
    public bool Shortfall => RumorIds.Count < RequestedCount;
}

public sealed class ShopRumorDrawer
{
    public const string LogPrefix = "[ShopRumorDrawer] ";
    public const string CountWeightKeyPrefix = "RUMOR_COUNT_WEIGHT_";
    public const int MaxRumorCount = 2; // 0~2개 (D-021/D-026)

    readonly CsvDatabase database;
    readonly IIntRoll intRoll;

    public ShopRumorDrawer(CsvDatabase database, IIntRoll intRoll)
    {
        this.database = database;
        this.intRoll = intRoll ?? new UnityIntRoll();
    }

    // GameConfig RUMOR_COUNT_WEIGHT_0/1/2 를 읽는다. 키 누락/음수/합 0 이면 false.
    public static bool TryReadCountWeights(CsvDatabase database, out int[] weights)
    {
        weights = new int[MaxRumorCount + 1];
        if (database == null || !database.Loaded || database.HasErrors)
            return false;

        long total = 0;
        for (int i = 0; i < weights.Length; i++)
        {
            GameConfigRow row;
            int value;
            if (!database.TryGetConfig(CountWeightKeyPrefix + i, out row)
                || !int.TryParse(row.Value, System.Globalization.NumberStyles.Integer, System.Globalization.CultureInfo.InvariantCulture, out value)
                || value < 0)
                return false;

            weights[i] = value;
            total += value;
        }

        return total > 0 && total <= int.MaxValue;
    }

    public bool TryDraw(string shopId, out ShopRumorDrawResult result)
    {
        result = null;
        ShopRow shop;
        int[] countWeights;
        if (database == null || !database.TryGetShop(shopId, out shop))
        {
            Debug.LogError(LogPrefix + "가게를 찾을 수 없어 소문을 뽑지 못했습니다: " + shopId);
            return false;
        }

        if (!TryReadCountWeights(database, out countWeights))
        {
            Debug.LogError(LogPrefix + "GameConfig RUMOR_COUNT_WEIGHT_0/1/2 가 없거나 올바르지 않습니다.");
            return false;
        }

        result = new ShopRumorDrawResult { ShopID = shopId };
        result.RequestedCount = PickIndex(countWeights);

        // 유효 후보: Weight>0, 활성 소문, 소문 ID 중복 제거.
        var pool = new List<ShopRumorCandidateRow>();
        var rumorOf = new List<ShopRumorRow>();
        var seen = new HashSet<string>(StringComparer.Ordinal);
        IReadOnlyList<ShopRumorCandidateRow> candidates = database.GetRumorCandidates(shopId);
        for (int i = 0; i < candidates.Count; i++)
        {
            ShopRumorRow rumor;
            if (candidates[i].Weight <= 0 || !database.TryGetRumor(candidates[i].RumorID, out rumor))
                continue;
            if (!seen.Add(candidates[i].RumorID))
                continue;

            pool.Add(candidates[i]);
            rumorOf.Add(rumor);
        }

        // 비복원 추출. 뽑힌 소문과 같은 ConflictGroup(비어 있지 않은) 후보는 이후 제외한다.
        while (result.RumorIds.Count < result.RequestedCount && pool.Count > 0)
        {
            var weights = new int[pool.Count];
            for (int i = 0; i < pool.Count; i++)
                weights[i] = pool[i].Weight;

            int picked = PickIndex(weights);
            ShopRumorRow chosen = rumorOf[picked];
            result.RumorIds.Add(chosen.RumorID);

            for (int i = pool.Count - 1; i >= 0; i--)
            {
                bool sameGroup = !string.IsNullOrEmpty(chosen.ConflictGroup)
                    && string.Equals(rumorOf[i].ConflictGroup, chosen.ConflictGroup, StringComparison.Ordinal);
                if (i == picked || sameGroup)
                {
                    pool.RemoveAt(i);
                    rumorOf.RemoveAt(i);
                }
            }
        }

        if (result.Shortfall)
            Debug.Log(LogPrefix + "후보 부족으로 소문 " + result.RumorIds.Count + "/" + result.RequestedCount + "개만 확정. shop=" + shopId);

        return true;
    }

    // 가게 목록 한 번의 추첨(리스트 확정/리롤)에 쓴다. 한 가게라도 실패하면 false.
    public bool TryDrawForList(IReadOnlyList<string> shopIds, List<ShopRumorDrawResult> results)
    {
        if (results == null)
            return false;

        results.Clear();
        if (shopIds == null)
            return false;

        for (int i = 0; i < shopIds.Count; i++)
        {
            ShopRumorDrawResult one;
            if (!TryDraw(shopIds[i], out one))
            {
                results.Clear();
                return false;
            }

            results.Add(one);
        }

        return true;
    }

    // ShopListDrawer와 같은 누적 가중치 걷기. 호출 측이 합 > 0 을 보장한다.
    int PickIndex(int[] weights)
    {
        long total = 0;
        for (int i = 0; i < weights.Length; i++)
            total += weights[i];

        int roll = intRoll.Next((int)total);
        long cursor = 0;
        for (int i = 0; i < weights.Length; i++)
        {
            cursor += weights[i];
            if (roll < cursor)
                return i;
        }

        return weights.Length - 1;
    }
}

public static class ShopRumorSnapshot
{
    public const int MaxRumorsPerShop = ShopRumorDrawer.MaxRumorCount;

    // 가게 행 복사본에 소문 효과를 합산한다. 소문이 없으면 원본과 같은 값의 복사본을 돌려준다.
    // 실패: 개수 초과, 빈/중복 ID, 알 수 없는(또는 비활성) 소문, 해당 가게 후보가 아닌 소문, 상충 그룹 충돌, 최종 CandyMultiplier <= 0.
    public static bool TryBuild(CsvDatabase database, ShopRow shop, IReadOnlyList<string> rumorIds, out ShopRow snapshot, out string error)
    {
        snapshot = null;
        error = string.Empty;
        if (shop == null)
        {
            error = "가게 데이터가 없습니다.";
            return false;
        }

        ShopRow copy = shop.Clone();
        if (rumorIds == null || rumorIds.Count == 0)
        {
            snapshot = copy;
            return true;
        }

        if (database == null)
        {
            error = "CsvDatabase가 없어 소문을 적용할 수 없습니다.";
            return false;
        }

        if (rumorIds.Count > MaxRumorsPerShop)
        {
            error = "소문은 가게당 최대 " + MaxRumorsPerShop + "개입니다. 요청=" + rumorIds.Count;
            return false;
        }

        var seenIds = new HashSet<string>(StringComparer.Ordinal);
        var seenGroups = new HashSet<string>(StringComparer.Ordinal);
        int successBonus = 0;
        int alertDelta = 0;
        int eventBonus = 0;
        float multiplierDelta = 0f;
        for (int i = 0; i < rumorIds.Count; i++)
        {
            string id = rumorIds[i];
            ShopRumorRow rumor;
            if (string.IsNullOrEmpty(id))
            {
                error = "빈 소문 ID";
                return false;
            }

            if (!seenIds.Add(id))
            {
                error = "소문 중복: " + id;
                return false;
            }

            if (!database.TryGetRumor(id, out rumor))
            {
                error = "알 수 없거나 비활성인 소문: " + id;
                return false;
            }

            if (!database.IsRumorCandidate(shop.ShopID, id))
            {
                error = "가게 " + shop.ShopID + " 의 후보가 아닌 소문: " + id;
                return false;
            }

            if (!string.IsNullOrEmpty(rumor.ConflictGroup) && !seenGroups.Add(rumor.ConflictGroup))
            {
                error = "상충 그룹 충돌: " + rumor.ConflictGroup;
                return false;
            }

            successBonus += rumor.SuccessRateBonusPp;
            alertDelta += rumor.AlertDelta;
            eventBonus += rumor.EventRateBonusPp;
            multiplierDelta += rumor.CandyMultiplierDelta;
        }

        copy.SuccessRateBonusPp += successBonus;
        copy.EventRateBonusPp += eventBonus;
        copy.CandyMultiplier += multiplierDelta;
        copy.StartAlert = Mathf.Clamp(copy.StartAlert + alertDelta, 0, database.GetInt("MAX_ALERT"));
        if (!(copy.CandyMultiplier > 0f))
        {
            error = "소문 적용 후 CandyMultiplier=" + copy.CandyMultiplier + " 는 0 보다 커야 합니다.";
            return false;
        }

        snapshot = copy;
        return true;
    }
}
