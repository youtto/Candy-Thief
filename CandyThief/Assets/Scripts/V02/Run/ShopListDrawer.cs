using System;
using System.Collections.Generic;

public enum ShopListDrawStatus
{
    Ok,
    InvalidCount,
    NotEnoughCandidates,
    InvalidWeights
}

public sealed class ShopListDrawResult
{
    public ShopListDrawStatus Status;
    public int RequestedCount;
    // Shops that passed Enabled + SpawnWeight > 0 (before any exclusion).
    public int DrawablePoolCount;
    // True when the previous-list exclusion was dropped because the pool was too small (D-024).
    public bool ExclusionRelaxed;
    // Drawn ShopIDs in draw order. Empty unless Status is Ok.
    public List<string> ShopIds = new List<string>();

    public bool Succeeded => Status == ShopListDrawStatus.Ok;
}

// D-021 / D-024: weighted draw without replacement of distinct shops by ShopTable.SpawnWeight.
// Stateless. It does not save a list, spend a reroll, or read quests or rumors.
public sealed class ShopListDrawer
{
    public const string LogPrefix = "[ShopListDrawer] ";

    readonly IIntRoll intRoll;

    public ShopListDrawer(IIntRoll intRoll)
    {
        this.intRoll = intRoll ?? new UnityIntRoll();
    }

    public ShopListDrawer() : this(null)
    {
    }

    public static bool IsDrawable(ShopRow shop)
    {
        return shop != null
            && shop.Enabled
            && shop.SpawnWeight > 0
            && !string.IsNullOrEmpty(shop.ShopID);
    }

    public ShopListDrawResult Draw(CsvDatabase database, IReadOnlyCollection<string> excludedShopIds, int count)
    {
        if (database == null || !database.Loaded || database.HasErrors)
        {
            return new ShopListDrawResult
            {
                Status = ShopListDrawStatus.NotEnoughCandidates,
                RequestedCount = count
            };
        }

        return Draw(database.EnabledShops, excludedShopIds, count);
    }

    public ShopListDrawResult Draw(IReadOnlyList<ShopRow> shops, IReadOnlyCollection<string> excludedShopIds, int count)
    {
        var result = new ShopListDrawResult { RequestedCount = count };
        if (count < 1)
        {
            result.Status = ShopListDrawStatus.InvalidCount;
            return result;
        }

        // Pool: enabled, weight > 0, one entry per ShopID (first row wins).
        var pool = new List<ShopRow>();
        var seen = new HashSet<string>(StringComparer.Ordinal);
        if (shops != null)
        {
            for (int i = 0; i < shops.Count; i++)
            {
                ShopRow shop = shops[i];
                if (IsDrawable(shop) && seen.Add(shop.ShopID))
                    pool.Add(shop);
            }
        }

        result.DrawablePoolCount = pool.Count;
        if (pool.Count < count)
        {
            result.Status = ShopListDrawStatus.NotEnoughCandidates;
            return result;
        }

        // Prefer a pool without the previous list. If that leaves fewer than `count`,
        // drop only that exclusion (D-024). Never relax Enabled or SpawnWeight.
        List<ShopRow> source = pool;
        if (excludedShopIds != null && excludedShopIds.Count > 0)
        {
            var filtered = new List<ShopRow>();
            for (int i = 0; i < pool.Count; i++)
            {
                if (!Contains(excludedShopIds, pool[i].ShopID))
                    filtered.Add(pool[i]);
            }

            if (filtered.Count >= count)
                source = filtered;
            else
                result.ExclusionRelaxed = true;
        }

        long total = 0;
        for (int i = 0; i < source.Count; i++)
            total += source[i].SpawnWeight;
        if (total > int.MaxValue)
        {
            result.Status = ShopListDrawStatus.InvalidWeights;
            result.ExclusionRelaxed = false;
            return result;
        }

        // Draw without replacement: each pick removes the shop, then the next pick uses the remaining weights.
        var remaining = new List<ShopRow>(source);
        for (int pick = 0; pick < count; pick++)
        {
            int index = PickIndex(remaining);
            result.ShopIds.Add(remaining[index].ShopID);
            remaining.RemoveAt(index);
        }

        result.Status = ShopListDrawStatus.Ok;
        return result;
    }

    // Same cumulative-weight walk as WeightedEventPick, over ShopRow.SpawnWeight (all > 0 here).
    int PickIndex(List<ShopRow> candidates)
    {
        int total = 0;
        for (int i = 0; i < candidates.Count; i++)
            total += candidates[i].SpawnWeight;

        int roll = intRoll.Next(total);
        int cursor = 0;
        for (int i = 0; i < candidates.Count; i++)
        {
            cursor += candidates[i].SpawnWeight;
            if (roll < cursor)
                return i;
        }

        return candidates.Count - 1;
    }

    static bool Contains(IReadOnlyCollection<string> ids, string id)
    {
        foreach (string excluded in ids)
        {
            if (string.Equals(excluded, id, StringComparison.Ordinal))
                return true;
        }

        return false;
    }
}
