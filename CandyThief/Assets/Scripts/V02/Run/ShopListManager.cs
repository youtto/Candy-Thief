using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

public enum ShopListOpStatus
{
    Ok,
    NoProfile,
    DataError,      // GameConfig 키 누락/오류
    DrawFailed,     // 추첨 불가 (후보 부족 등). 기존 상태 유지
    SaveFailed,     // 저장 실패. 메모리 상태도 되돌린다
    NoList,         // 유효한 현재 목록 없음
    LimitReached,   // 무료 리롤 소진 / 최대 도달
    NotInList,      // 현재 목록에 없는 가게
    StartFailed,    // RunState 시작 실패
    NotEnded,       // 종료되지 않은 런은 정산하지 않는다
    InvalidRunId,
    AlreadySettled, // 같은 런 결과의 중복 전달. 변경 없음
    AbandonRefused  // 이미 끝난 런은 포기 정산하지 않는다. 변경 없음
}

public sealed class ShopListResult
{
    public ShopListOpStatus Status;
    public bool Changed;    // 프로필의 목록 상태가 바뀌었는지
    public bool Recovered;  // 저장된 목록이 유효하지 않아 새로 만들었는지
    public bool Ok => Status == ShopListOpStatus.Ok;

    internal static ShopListResult Of(ShopListOpStatus status, bool changed = false, bool recovered = false)
    {
        return new ShopListResult { Status = status, Changed = changed, Recovered = recovered };
    }
}

// OP-DEV-05: 가게 목록 생성 / 무료 리롤 / 런 정산 후 갱신 / 저장 (D-021, D-024, D-025, D-026).
// 추첨은 ShopListDrawer + ShopRumorDrawer 를 그대로 쓴다. 저장은 V02LocalProfileService.TrySaveCurrent 를 쓴다.
// 상태를 바꾸는 모든 동작은 "새 상태를 완전히 만든 뒤 적용 → 저장, 저장 실패 시 되돌림" 순서다.
public sealed class ShopListManager
{
    public const string LogPrefix = "[ShopList] ";
    public const string ListSizeKey = "SHOP_LIST_SIZE";
    public const string FreeRerollKey = "SHOP_REROLL_FREE_PER_LIST";
    public const string MaxRerollKey = "SHOP_REROLL_MAX_PER_LIST"; // P-014. 키가 없으면 최대 = 무료 횟수 (D-024)

    readonly V02LocalProfileService profiles;
    readonly CsvDatabase database;
    readonly IIntRoll intRoll;

    public ShopListManager(V02LocalProfileService profiles, CsvDatabase database, IIntRoll intRoll)
    {
        this.profiles = profiles;
        this.database = database;
        this.intRoll = intRoll ?? new UnityIntRoll();
    }

    // ---------- 조회 ----------

    public IReadOnlyList<string> CurrentShopIds
    {
        get
        {
            V02PlayerProfile profile = profiles != null ? profiles.CurrentProfile : null;
            return profile == null || profile.shopList == null || profile.shopList.shopIds == null
                ? new string[0]
                : profile.shopList.shopIds;
        }
    }

    public List<string> GetRumorIds(string shopId)
    {
        var ids = new List<string>();
        V02PlayerProfile profile = profiles != null ? profiles.CurrentProfile : null;
        if (profile == null || profile.shopList == null || profile.shopList.rumors == null)
            return ids;

        V02ShopRumorPair[] pairs = profile.shopList.rumors;
        for (int i = 0; i < pairs.Length; i++)
        {
            if (pairs[i] != null && pairs[i].shopId == shopId)
                ids.Add(pairs[i].rumorId);
        }

        return ids;
    }

    // 화면 표시용. 저장이나 정산 상태는 바꾸지 않는다. 리롤 가능 여부는 TryReroll 과 같은 조건이다.
    public bool TryReadRerollBudget(out int freeUsed, out int freeLimit, out int totalUsed, out int totalLimit)
    {
        freeUsed = 0;
        freeLimit = 0;
        totalUsed = 0;
        totalLimit = 0;
        V02PlayerProfile profile;
        int size, free, max;
        if (Prepare(out profile, out size, out free, out max) != null)
            return false;
        if (!IsStateValid(profile.shopList, size))
            return false;

        freeUsed = profile.shopList.freeRerollUsed;
        freeLimit = free;
        totalUsed = profile.shopList.totalRerollUsed;
        totalLimit = max;
        return true;
    }

    // ---------- 최초 진입: 목록이 없으면(또는 저장본이 유효하지 않으면) 생성 ----------

    public ShopListResult EnsureList()
    {
        V02PlayerProfile profile;
        int size, free, max;
        ShopListResult pre = Prepare(out profile, out size, out free, out max);
        if (pre != null)
            return pre;

        if (IsStateValid(profile.shopList, size))
            return ShopListResult.Of(ShopListOpStatus.Ok);

        bool hadData = profile.shopList.shopIds.Length > 0 || profile.shopList.listId.Length > 0;
        V02ShopListState next;
        if (!TryDraw(null, size, null, out next))
            return ShopListResult.Of(ShopListOpStatus.DrawFailed);

        if (hadData)
            Debug.LogWarning(LogPrefix + "저장된 가게 목록이 유효하지 않아 새로 만듭니다. 재화/진행도는 유지합니다.");

        return Commit(profile, next, null, null, hadData);
    }

    // ---------- 무료 리롤 ----------

    public ShopListResult TryReroll()
    {
        V02PlayerProfile profile;
        int size, free, max;
        ShopListResult pre = Prepare(out profile, out size, out free, out max);
        if (pre != null)
            return pre;

        V02ShopListState current = profile.shopList;
        if (!IsStateValid(current, size))
            return ShopListResult.Of(ShopListOpStatus.NoList);

        // 유료 리롤은 없다: 무료 횟수를 다 썼거나 최대에 도달하면 거절.
        if (current.freeRerollUsed >= free || current.totalRerollUsed >= max)
            return ShopListResult.Of(ShopListOpStatus.LimitReached);

        // 직전(=교체될) 목록의 가게를 우선 제외한다. 후보가 모자라면 이 제외만 완화한다 (ShopListDrawer).
        V02ShopListState next;
        if (!TryDraw(current.shopIds, size, current.shopIds, out next))
            return ShopListResult.Of(ShopListOpStatus.DrawFailed);

        next.freeRerollUsed = current.freeRerollUsed + 1;
        next.totalRerollUsed = current.totalRerollUsed + 1;
        return Commit(profile, next, null, null, false);
    }

    // ---------- 런 시작 / 정산 / 콜드 스타트 ----------

    // 현재 목록의 가게만 시작할 수 있다. 목록에 저장된 소문을 스냅샷으로 적용한다.
    public ShopListResult StartRun(string shopId, out RunState run)
    {
        run = null;
        V02PlayerProfile profile;
        int size, free, max;
        ShopListResult pre = Prepare(out profile, out size, out free, out max);
        if (pre != null)
            return pre;

        if (!IsStateValid(profile.shopList, size))
            return ShopListResult.Of(ShopListOpStatus.NoList);

        if (Array.IndexOf(profile.shopList.shopIds, shopId) < 0)
            return ShopListResult.Of(ShopListOpStatus.NotInList);

        if (!RunState.TryStart(database, shopId, GetRumorIds(shopId), out run))
        {
            run = null;
            return ShopListResult.Of(ShopListOpStatus.StartFailed);
        }

        string previousActive = profile.activeRunId;
        profile.activeRunId = run.RunId;
        string error;
        if (!profiles.TrySaveCurrent(out error))
        {
            // 런 자체는 유효하다. activeRunId 기록만 실패했으므로 되돌리고 경고만 남긴다.
            profile.activeRunId = previousActive;
            Debug.LogWarning(LogPrefix + "activeRunId 저장 실패. 런은 계속합니다.");
        }

        return ShopListResult.Of(ShopListOpStatus.Ok, true);
    }

    // 작전 성공/실패 확정 후 다음 목록을 만든다. 같은 런 ID는 한 번만 정산한다.
    public ShopListResult SettleRun(RunState run)
    {
        V02PlayerProfile profile;
        int size, free, max;
        ShopListResult pre = Prepare(out profile, out size, out free, out max);
        if (pre != null)
            return pre;

        if (run == null || !run.IsEnded)
            return ShopListResult.Of(ShopListOpStatus.NotEnded);
        if (string.IsNullOrEmpty(run.RunId))
            return ShopListResult.Of(ShopListOpStatus.InvalidRunId);
        if (profile.settledRunId == run.RunId)
            return ShopListResult.Of(ShopListOpStatus.AlreadySettled);

        bool valid = IsStateValid(profile.shopList, size);
        string[] previous = valid ? profile.shopList.shopIds : null;
        V02ShopListState next;
        if (!TryDraw(previous, size, previous, out next))
            return ShopListResult.Of(ShopListOpStatus.DrawFailed); // 정산 표식 없음 → 다시 시도 가능

        int grant = run.SettledCandy;
        int oldCandy = profile.candy;
        if (grant > 0)
            profile.candy = oldCandy + grant;
        ShopListResult committed = Commit(profile, next, run.RunId, run.RunId, false);
        if (!committed.Ok && grant > 0)
            profile.candy = oldCandy;
        return committed;
    }

    // 진행 중 런을 실패로 확정한 뒤 SettleRun 으로 한 번만 정산한다 (D-025).
    // 이미 끝난 런, 이미 정산된 런, 저장/추첨 실패는 목록을 다시 만들지 않는다.
    public ShopListResult AbandonRun(RunState run)
    {
        V02PlayerProfile profile;
        int size, free, max;
        ShopListResult pre = Prepare(out profile, out size, out free, out max);
        if (pre != null)
            return pre;

        if (run == null || string.IsNullOrEmpty(run.RunId))
            return ShopListResult.Of(ShopListOpStatus.InvalidRunId);
        if (profile.settledRunId == run.RunId)
            return ShopListResult.Of(ShopListOpStatus.AlreadySettled);
        if (run.IsEnded || !run.TryAbandon())
            return ShopListResult.Of(ShopListOpStatus.AbandonRefused);

        ShopListResult settled = SettleRun(run);
        if (!settled.Ok)
            run.RevertAbandon();
        return settled;
    }

    // 강제 종료 후 첫 진입: 남은 activeRunId 만 지운다 (D-025). 목록/카운터/정산 ID/재화는 건드리지 않는다.
    public ShopListResult ClearStaleActiveRun(bool liveRunExists)
    {
        V02PlayerProfile profile = profiles != null ? profiles.CurrentProfile : null;
        if (profile == null)
            return ShopListResult.Of(ShopListOpStatus.NoProfile);
        if (liveRunExists || string.IsNullOrEmpty(profile.activeRunId))
            return ShopListResult.Of(ShopListOpStatus.Ok);

        string previous = profile.activeRunId;
        profile.activeRunId = "";
        string error;
        if (!profiles.TrySaveCurrent(out error))
        {
            profile.activeRunId = previous;
            return ShopListResult.Of(ShopListOpStatus.SaveFailed);
        }

        return ShopListResult.Of(ShopListOpStatus.Ok, true);
    }

    // ---------- 내부 ----------

    ShopListResult Prepare(out V02PlayerProfile profile, out int size, out int free, out int max)
    {
        profile = profiles != null ? profiles.CurrentProfile : null;
        size = 0;
        free = 0;
        max = 0;
        if (profile == null)
            return ShopListResult.Of(ShopListOpStatus.NoProfile);

        Normalize(profile);
        if (database == null || !database.Loaded || database.HasErrors || !TryReadSettings(out size, out free, out max))
            return ShopListResult.Of(ShopListOpStatus.DataError);

        return null;
    }

    // 새 필드가 없는 기존 저장본(null 배열/객체)을 빈 상태로 맞춘다. 재화/진행도는 건드리지 않는다.
    static void Normalize(V02PlayerProfile profile)
    {
        if (profile.shopList == null)
            profile.shopList = new V02ShopListState();
        V02ShopListState s = profile.shopList;
        if (s.listId == null)
            s.listId = "";
        if (s.shopIds == null)
            s.shopIds = Array.Empty<string>();
        if (s.rumors == null)
            s.rumors = Array.Empty<V02ShopRumorPair>();
        if (s.previousShopIds == null)
            s.previousShopIds = Array.Empty<string>();
        if (profile.activeRunId == null)
            profile.activeRunId = "";
        if (profile.settledRunId == null)
            profile.settledRunId = "";
    }

    bool TryReadSettings(out int size, out int free, out int max)
    {
        size = 0;
        free = 0;
        max = 0;
        if (!TryReadInt(ListSizeKey, out size) || size < 1)
            return false;
        if (!TryReadInt(FreeRerollKey, out free) || free < 0)
            return false;

        GameConfigRow row;
        if (database.TryGetConfig(MaxRerollKey, out row))
        {
            // 최대 키가 있으면 무료 횟수 이상이어야 한다 (D-024). 아니면 설정 오류.
            if (!TryReadInt(MaxRerollKey, out max) || max < free)
                return false;
        }
        else
        {
            max = free;
        }

        return true;
    }

    bool TryReadInt(string key, out int value)
    {
        GameConfigRow row;
        value = 0;
        return database.TryGetConfig(key, out row)
            && int.TryParse(row.Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out value);
    }

    // 저장 목록이 지금 데이터와 맞는지: 크기, 중복, 활성+추첨 가능, 소문 후보/상충/개수, 카운터 부호.
    bool IsStateValid(V02ShopListState state, int size)
    {
        if (state == null || string.IsNullOrEmpty(state.listId) || state.shopIds == null || state.shopIds.Length != size)
            return false;
        if (state.freeRerollUsed < 0 || state.totalRerollUsed < state.freeRerollUsed)
            return false;

        var seen = new HashSet<string>(StringComparer.Ordinal);
        for (int i = 0; i < state.shopIds.Length; i++)
        {
            ShopRow shop;
            string id = state.shopIds[i];
            if (string.IsNullOrEmpty(id) || !seen.Add(id)
                || !database.TryGetShop(id, out shop) || !ShopListDrawer.IsDrawable(shop))
                return false;
        }

        var perShop = new Dictionary<string, List<string>>(StringComparer.Ordinal);
        V02ShopRumorPair[] pairs = state.rumors ?? Array.Empty<V02ShopRumorPair>();
        for (int i = 0; i < pairs.Length; i++)
        {
            if (pairs[i] == null || !seen.Contains(pairs[i].shopId) || string.IsNullOrEmpty(pairs[i].rumorId))
                return false;

            List<string> list;
            if (!perShop.TryGetValue(pairs[i].shopId, out list))
            {
                list = new List<string>();
                perShop.Add(pairs[i].shopId, list);
            }

            list.Add(pairs[i].rumorId);
        }

        foreach (KeyValuePair<string, List<string>> entry in perShop)
        {
            ShopRow shop;
            ShopRow snapshot;
            string error;
            if (!database.TryGetShop(entry.Key, out shop)
                || !ShopRumorSnapshot.TryBuild(database, shop, entry.Value, out snapshot, out error))
                return false;
        }

        return true;
    }

    // 목록 + 소문을 함께 추첨한다. 둘 중 하나라도 실패하면 아무것도 만들지 않는다.
    bool TryDraw(IReadOnlyCollection<string> excluded, int size, string[] previousIds, out V02ShopListState next)
    {
        next = null;
        ShopListDrawResult drawn = new ShopListDrawer(intRoll).Draw(database, excluded, size);
        if (!drawn.Succeeded)
        {
            Debug.LogWarning(LogPrefix + "가게 목록 추첨 실패: " + drawn.Status + " (추첨 가능 " + drawn.DrawablePoolCount + "종)");
            return false;
        }

        var rumorResults = new List<ShopRumorDrawResult>();
        if (!new ShopRumorDrawer(database, intRoll).TryDrawForList(drawn.ShopIds, rumorResults))
            return false;

        var pairs = new List<V02ShopRumorPair>();
        for (int i = 0; i < rumorResults.Count; i++)
        {
            for (int j = 0; j < rumorResults[i].RumorIds.Count; j++)
                pairs.Add(new V02ShopRumorPair { shopId = rumorResults[i].ShopID, rumorId = rumorResults[i].RumorIds[j] });
        }

        next = new V02ShopListState
        {
            listId = Guid.NewGuid().ToString("N"),
            shopIds = drawn.ShopIds.ToArray(),
            rumors = pairs.ToArray(),
            freeRerollUsed = 0,
            totalRerollUsed = 0,
            previousShopIds = previousIds == null ? Array.Empty<string>() : (string[])previousIds.Clone()
        };
        return true;
    }

    // 새 목록(+정산 ID)을 적용하고 저장한다. 저장에 실패하면 이전 상태로 되돌린다.
    ShopListResult Commit(V02PlayerProfile profile, V02ShopListState next, string settledRunId, string clearActiveIfEquals, bool recovered)
    {
        V02ShopListState oldList = profile.shopList.Clone();
        string oldSettled = profile.settledRunId;
        string oldActive = profile.activeRunId;

        profile.shopList = next;
        if (settledRunId != null)
            profile.settledRunId = settledRunId;
        if (clearActiveIfEquals != null && profile.activeRunId == clearActiveIfEquals)
            profile.activeRunId = "";

        string error;
        if (!profiles.TrySaveCurrent(out error))
        {
            // TrySaveCurrent 는 실패 시 CurrentProfile 참조를 바꾸지 않는다.
            profile.shopList = oldList;
            profile.settledRunId = oldSettled;
            profile.activeRunId = oldActive;
            Debug.LogError(LogPrefix + "가게 목록 저장 실패. 이전 상태로 되돌렸습니다.");
            return ShopListResult.Of(ShopListOpStatus.SaveFailed);
        }

        return ShopListResult.Of(ShopListOpStatus.Ok, true, recovered);
    }
}
