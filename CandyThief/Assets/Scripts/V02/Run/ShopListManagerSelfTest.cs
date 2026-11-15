using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using UnityEngine;

// OP-DEV-05 검사. 프로필 파일은 임시 폴더에만 만들고 끝나면 지운다. 소문 행은 코드 픽스처이며 정식 CSV에 넣지 않는다.
public static class ShopListManagerSelfTest
{
    public const string LogPrefix = "[ShopListMgr] ";
    const string Nick = "테스트유저";

    static readonly List<string> TempDirs = new List<string>();

    sealed class Fx
    {
        public string Path;
        public V02LocalProfileService Service;
    }

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
            failed += Expect("1 4종 목록 생성, 중복 없음, 저장 후 복원", TestCreate(database));
            failed += Expect("2 직전 목록 우선 제외 (시드 다수)", TestPreviousExcluded(database));
            failed += Expect("3 후보 부족: 제외만 완화, 추첨 불가면 실패/불변", TestShortage(database));
            failed += Expect("4 무료 리롤 1회, 초과 거절, 재접속 유지", TestFreeReroll(database));
            failed += Expect("5 최대 횟수 설정 확장 (미설정=무료 횟수)", TestMaxConfig(database));
            failed += Expect("6 실패 시 목록/카운터 불변 (추첨/저장 실패)", TestFailureKeepsState(database));
            failed += Expect("7 소문 스냅샷 저장/복원, 런 적용", TestRumors());
            failed += Expect("8 기존 프로필 하위 호환, 손상 목록 복구", TestBackwardCompat(database));
            failed += Expect("9 런 성공/실패 후 목록 갱신, 카운터 리셋", TestSettle(database));
            failed += Expect("10 중복 결과 전달 시 한 번만 갱신", TestSettleOnce(database));
            failed += Expect("11 강제 종료 후 목록 유지, activeRunId만 정리", TestForceKill(database));
            failed += Expect("12 목록 밖 가게/목록 없음/프로필 없음 거절", TestRejects(database));
            failed += Expect("13 정산 추첨 실패 시 표식 없음, 재시도 가능", TestSettleRetry(database));
            failed += Expect("14 런 ID 고유, 소문 없는 정식 데이터는 소문 0개", TestRunIdAndLiveRumors(database));
        }
        finally
        {
            RunState.Current = previous;
            for (int i = 0; i < TempDirs.Count; i++)
            {
                try
                {
                    if (Directory.Exists(TempDirs[i]))
                        Directory.Delete(TempDirs[i], true);
                }
                catch (Exception)
                {
                }
            }

            TempDirs.Clear();
        }

        if (failed == 0)
        {
            Debug.Log(LogPrefix + "OP-DEV-05 자가 검사 14건 통과");
            return true;
        }

        Debug.LogError(LogPrefix + "OP-DEV-05 자가 검사 실패: " + failed + "건");
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

    // ---------- 1~6 ----------

    static bool TestCreate(CsvDatabase db)
    {
        Fx fx = Login();
        if (fx == null)
            return false;

        ShopListManager mgr = Mgr(fx, db, 11);
        ShopListResult r = mgr.EnsureList();
        V02ShopListState s = fx.Service.CurrentProfile.shopList;
        if (!r.Ok || !r.Changed || r.Recovered || !ValidList(db, s.shopIds, 4) || s.listId.Length == 0
            || s.freeRerollUsed != 0 || s.totalRerollUsed != 0)
            return false;

        // 같은 세션 재진입은 변경 없음.
        string listId = s.listId;
        ShopListResult again = mgr.EnsureList();
        if (!again.Ok || again.Changed || fx.Service.CurrentProfile.shopList.listId != listId)
            return false;

        // 파일에서 다시 읽어도 같은 목록.
        V02LocalProfileService reopened = Reopen(fx);
        return reopened != null && reopened.CurrentProfile.shopList.listId == listId
            && SameArray(reopened.CurrentProfile.shopList.shopIds, s.shopIds);
    }

    static bool TestPreviousExcluded(CsvDatabase db)
    {
        for (int seed = 1; seed <= 30; seed++)
        {
            Fx fx = Login();
            ShopListManager mgr = Mgr(fx, db, seed);
            if (fx == null || !mgr.EnsureList().Ok)
                return false;

            string[] old = (string[])fx.Service.CurrentProfile.shopList.shopIds.Clone();
            if (!mgr.TryReroll().Ok)
                return false;

            V02ShopListState now = fx.Service.CurrentProfile.shopList;
            if (!ValidList(db, now.shopIds, 4) || Overlaps(old, now.shopIds) || !SameArray(now.previousShopIds, old))
                return false;
        }

        return true;
    }

    static bool TestShortage(CsvDatabase live)
    {
        // 6종만 있는 데이터: 직전 4종을 빼면 2종 → 제외만 완화해 4종을 뽑는다.
        CsvDatabase db6 = ShopCountDb(6);
        Fx a = Login();
        if (db6 == null || a == null)
            return false;
        ShopListManager m6 = Mgr(a, db6, 3);
        if (!m6.EnsureList().Ok)
            return false;
        ShopListResult relaxed = m6.TryReroll();
        if (!relaxed.Ok || !ValidList(db6, a.Service.CurrentProfile.shopList.shopIds, 4)
            || a.Service.CurrentProfile.shopList.freeRerollUsed != 1)
            return false;

        // 3종뿐이면 추첨 자체가 불가: 목록은 만들어지지 않고 프로필은 그대로.
        CsvDatabase db3 = ShopCountDb(3);
        Fx b = Login();
        if (db3 == null || b == null)
            return false;
        ShopListResult none = Mgr(b, db3, 3).EnsureList();
        V02PlayerProfile p = b.Service.CurrentProfile;
        V02LocalProfileService reopened = Reopen(b);
        return none.Status == ShopListOpStatus.DrawFailed && !none.Changed
            && p.shopList.shopIds.Length == 0 && p.shopList.listId.Length == 0
            && reopened != null && reopened.CurrentProfile.shopList.shopIds.Length == 0;
    }

    static bool TestFreeReroll(CsvDatabase db)
    {
        Fx fx = Login();
        ShopListManager mgr = Mgr(fx, db, 21);
        if (fx == null || !mgr.EnsureList().Ok)
            return false;

        string[] first = (string[])fx.Service.CurrentProfile.shopList.shopIds.Clone();
        ShopListResult r1 = mgr.TryReroll();
        V02ShopListState s = fx.Service.CurrentProfile.shopList;
        if (!r1.Ok || !r1.Changed || s.freeRerollUsed != 1 || s.totalRerollUsed != 1 || SameArray(s.shopIds, first))
            return false;

        string[] second = (string[])s.shopIds.Clone();
        string listId = s.listId;

        // 2번째는 거절. 목록, 카운터, 저장본 모두 그대로.
        ShopListResult r2 = mgr.TryReroll();
        V02ShopListState after = fx.Service.CurrentProfile.shopList;
        if (r2.Status != ShopListOpStatus.LimitReached || r2.Changed || after.listId != listId
            || !SameArray(after.shopIds, second) || after.freeRerollUsed != 1 || after.totalRerollUsed != 1)
            return false;

        // 재접속 후에도 같은 목록 + 카운터 (리롤 횟수는 열기만 해서는 리셋되지 않는다).
        V02LocalProfileService reopened = Reopen(fx);
        if (reopened == null)
            return false;
        var mgr2 = new ShopListManager(reopened, db, new ShopListDrawerSelfTest.SeededIntRoll(22));
        ShopListResult ensure = mgr2.EnsureList();
        V02ShopListState loaded = reopened.CurrentProfile.shopList;
        return ensure.Ok && !ensure.Changed && loaded.listId == listId && SameArray(loaded.shopIds, second)
            && loaded.freeRerollUsed == 1 && loaded.totalRerollUsed == 1
            && mgr2.TryReroll().Status == ShopListOpStatus.LimitReached;
    }

    static bool TestMaxConfig(CsvDatabase live)
    {
        // 최대 < 무료 는 설정 오류: 아무것도 하지 않는다.
        CsvDatabase bad = ConfigDb(t => AppendLine(t, "SHOP_REROLL_MAX_PER_LIST,0,INT,test,"));
        Fx a = Login();
        if (bad == null || a == null)
            return false;
        ShopListResult err = Mgr(a, bad, 1).EnsureList();
        if (err.Status != ShopListOpStatus.DataError || a.Service.CurrentProfile.shopList.shopIds.Length != 0)
            return false;

        // 최대가 무료보다 커도 유료 리롤은 없으므로 무료 소진 후에는 거절.
        CsvDatabase wide = ConfigDb(t => AppendLine(t, "SHOP_REROLL_MAX_PER_LIST,3,INT,test,"));
        Fx b = Login();
        if (wide == null || b == null)
            return false;
        ShopListManager m = Mgr(b, wide, 5);
        return m.EnsureList().Ok && m.TryReroll().Ok
            && m.TryReroll().Status == ShopListOpStatus.LimitReached
            && b.Service.CurrentProfile.shopList.totalRerollUsed == 1;
    }

    static bool TestFailureKeepsState(CsvDatabase live)
    {
        // (a) 소문 설정 누락으로 추첨이 실패하면 리롤은 거절되고 목록/카운터/저장본이 그대로.
        CsvDatabase noWeights = ConfigDb(t => RemoveLines(t, "RUMOR_COUNT_WEIGHT_"));
        Fx fx = Login();
        if (noWeights == null || fx == null || !Mgr(fx, live, 31).EnsureList().Ok)
            return false;

        V02ShopListState before = fx.Service.CurrentProfile.shopList.Clone();
        ShopListResult failedDraw = Mgr(fx, noWeights, 32).TryReroll();
        V02ShopListState s = fx.Service.CurrentProfile.shopList;
        V02LocalProfileService reopened = Reopen(fx);
        if (failedDraw.Status != ShopListOpStatus.DrawFailed || failedDraw.Changed
            || !SameState(s, before) || reopened == null || !SameState(reopened.CurrentProfile.shopList, before))
            return false;

        // (b) 저장 실패: 메모리 상태도 이전으로 되돌린다.
        Fx fx2 = Login();
        ShopListManager mgr = Mgr(fx2, live, 33);
        if (fx2 == null || !mgr.EnsureList().Ok)
            return false;
        V02ShopListState before2 = fx2.Service.CurrentProfile.shopList.Clone();
        BreakStorage(fx2);
        ShopListResult saveFail = mgr.TryReroll();
        bool rerollRolledBack = saveFail.Status == ShopListOpStatus.SaveFailed
            && SameState(fx2.Service.CurrentProfile.shopList, before2);

        // 저장 실패 상태에서의 첫 생성도 목록을 남기지 않는다.
        Fx fx3 = Login();
        if (fx3 == null)
            return false;
        BreakStorage(fx3);
        ShopListResult createFail = Mgr(fx3, live, 34).EnsureList();
        return rerollRolledBack && createFail.Status == ShopListOpStatus.SaveFailed
            && fx3.Service.CurrentProfile.shopList.shopIds.Length == 0;
    }

    // ---------- 7 ----------

    static bool TestRumors()
    {
        CsvDatabase rdb = RumorDb();
        if (rdb == null)
            return false;

        // 목록 + 소문이 저장/복원 후에도 유효하고 변경 없음 (시드 다수, 상충/중복/개수 검사 포함).
        Fx found = null;
        for (int seed = 1; seed <= 120; seed++)
        {
            Fx fx = Login();
            if (fx == null || !Mgr(fx, rdb, seed).EnsureList().Ok)
                return false;

            V02LocalProfileService reopened = Reopen(fx);
            if (reopened == null)
                return false;
            ShopListResult ensure = new ShopListManager(reopened, rdb, new ShopListDrawerSelfTest.SeededIntRoll(seed)).EnsureList();
            if (!ensure.Ok || ensure.Changed || ensure.Recovered
                || !SamePairs(reopened.CurrentProfile.shopList.rumors, fx.Service.CurrentProfile.shopList.rumors))
                return false;

            if (found == null && fx.Service.CurrentProfile.shopList.rumors.Length > 0)
                found = fx;
        }

        if (found == null)
            return false;

        // 저장된 소문이 런 시작 스냅샷에 그대로 적용된다 (원본 가게 행은 불변).
        V02ShopListState state = found.Service.CurrentProfile.shopList;
        string shopId = state.rumors[0].shopId;
        var mgr = new ShopListManager(found.Service, rdb, new ShopListDrawerSelfTest.SeededIntRoll(1));
        List<string> ids = mgr.GetRumorIds(shopId);
        RunState run;
        ShopRow row;
        if (ids.Count == 0 || !mgr.StartRun(shopId, out run).Ok || !rdb.TryGetShop(shopId, out row))
            return false;

        int success = 0;
        int eventPp = 0;
        for (int i = 0; i < ids.Count; i++)
        {
            ShopRumorRow rumor;
            if (!rdb.TryGetRumor(ids[i], out rumor))
                return false;
            success += rumor.SuccessRateBonusPp;
            eventPp += rumor.EventRateBonusPp;
        }

        bool applied = run.RumorIds.Count == ids.Count
            && run.Shop.SuccessRateBonusPp == row.SuccessRateBonusPp + success
            && run.Shop.EventRateBonusPp == row.EventRateBonusPp + eventPp
            && !ReferenceEquals(run.Shop, row);

        // 리롤하면 새 목록과 소문이 함께 저장된다. 소문의 가게는 모두 새 목록 안에 있다.
        Fx fx4 = Login();
        ShopListManager m4 = Mgr(fx4, rdb, 77);
        if (fx4 == null || !m4.EnsureList().Ok || !m4.TryReroll().Ok)
            return false;
        V02LocalProfileService reopened4 = Reopen(fx4);
        V02ShopListState s4 = fx4.Service.CurrentProfile.shopList;
        if (reopened4 == null || !SamePairs(reopened4.CurrentProfile.shopList.rumors, s4.rumors))
            return false;
        for (int i = 0; i < s4.rumors.Length; i++)
        {
            if (Array.IndexOf(s4.shopIds, s4.rumors[i].shopId) < 0)
                return false;
        }

        return applied;
    }

    // ---------- 8 ----------

    static bool TestBackwardCompat(CsvDatabase db)
    {
        string now = DateTime.UtcNow.ToString("o");
        string oldJson =
            "{\"schema\":\"candythief.v02.profiles.v1\",\"lastNickname\":\"옛유저\",\"currentProfileId\":\"p1\",\"profiles\":["
            + Prof("p1", "옛유저", 7, 123, 45, now, "")
            + "," + Prof("p2", "다른유저", 3, 9, 8, now, "") + "]}";
        Fx fx = FromJson(oldJson);
        if (fx == null)
            return false;

        V02PlayerProfile p;
        string error;
        if (!fx.Service.TryLogin("옛유저", out p, out error))
            return false;
        if (p.shopList == null || p.shopList.shopIds.Length != 0 || p.activeRunId != "" || p.settledRunId != ""
            || p.gold != 123 || p.candy != 45 || p.energy != 7)
            return false;

        ShopListResult r = new ShopListManager(fx.Service, db, new ShopListDrawerSelfTest.SeededIntRoll(9)).EnsureList();
        V02LocalProfileService reopened = Reopen(fx, "옛유저");
        V02PlayerProfile other;
        if (!r.Ok || !r.Changed || r.Recovered || reopened == null || !ValidList(db, reopened.CurrentProfile.shopList.shopIds, 4)
            || reopened.CurrentProfile.gold != 123 || reopened.CurrentProfile.candy != 45 || reopened.CurrentProfile.energy != 7
            || !fx.Service.TryPeek("다른유저", out other, out error) || other.gold != 9 || other.candy != 8)
            return false;
        if (!File.ReadAllText(fx.Path).Contains(V02ProfileStoreData.SchemaId))
            return false;

        // 유효하지 않은 목록(중복/없는 가게/음수 카운터)은 목록만 새로 만들고 재화와 진행도는 지킨다.
        string badList = "\"shopList\":{\"listId\":\"x\",\"shopIds\":[\"SHOP_001\",\"SHOP_001\",\"NOPE\",\"SHOP_002\"],\"rumors\":[],\"freeRerollUsed\":5,\"totalRerollUsed\":-2,\"previousShopIds\":[]}";
        string partial = "\"shopList\":{\"listId\":\"abc\"}";
        string[] variants = { badList, partial };
        for (int i = 0; i < variants.Length; i++)
        {
            string json = "{\"schema\":\"candythief.v02.profiles.v1\",\"lastNickname\":\"복구유저\",\"currentProfileId\":\"p9\",\"profiles\":["
                + Prof("p9", "복구유저", 5, 77, 12, now, variants[i]) + "]}";
            Fx bad = FromJson(json);
            V02PlayerProfile bp;
            if (bad == null || !bad.Service.TryLogin("복구유저", out bp, out error))
                return false;

            ShopListResult fixedUp = new ShopListManager(bad.Service, db, new ShopListDrawerSelfTest.SeededIntRoll(4)).EnsureList();
            V02PlayerProfile after = bad.Service.CurrentProfile;
            if (!fixedUp.Ok || !fixedUp.Recovered || !ValidList(db, after.shopList.shopIds, 4)
                || after.shopList.freeRerollUsed != 0 || after.shopList.totalRerollUsed != 0
                || after.gold != 77 || after.candy != 12 || after.energy != 5)
                return false;
        }

        return true;
    }

    // ---------- 9~11 ----------

    static bool TestSettle(CsvDatabase db)
    {
        // 실패 정산
        Fx fx = Login();
        ShopListManager mgr = Mgr(fx, db, 41);
        RunState run;
        if (fx == null || !mgr.EnsureList().Ok || !mgr.TryReroll().Ok)
            return false;
        string[] listBefore = (string[])fx.Service.CurrentProfile.shopList.shopIds.Clone();
        if (!mgr.StartRun(listBefore[0], out run).Ok)
            return false;

        // 진행 중에는 정산하지 않는다.
        ShopListResult early = mgr.SettleRun(run);
        if (early.Status != ShopListOpStatus.NotEnded || !SameArray(fx.Service.CurrentProfile.shopList.shopIds, listBefore))
            return false;

        if (!PlayToFail(run, db))
            return false;
        ShopListResult failSettle = mgr.SettleRun(run);
        V02PlayerProfile p = fx.Service.CurrentProfile;
        if (!failSettle.Ok || !failSettle.Changed || Overlaps(listBefore, p.shopList.shopIds) || !ValidList(db, p.shopList.shopIds, 4)
            || !SameArray(p.shopList.previousShopIds, listBefore) || p.shopList.freeRerollUsed != 0 || p.shopList.totalRerollUsed != 0
            || p.settledRunId != run.RunId || p.activeRunId != "")
            return false;
        // 새 목록에서는 무료 리롤을 다시 쓸 수 있다.
        if (!mgr.TryReroll().Ok)
            return false;

        // 성공 정산
        string[] list2 = (string[])fx.Service.CurrentProfile.shopList.shopIds.Clone();
        RunState win;
        if (!mgr.StartRun(list2[0], out win).Ok || !PlayToWin(win, db))
            return false;
        ShopListResult winSettle = mgr.SettleRun(win);
        V02LocalProfileService reopened = Reopen(fx);
        return winSettle.Ok && winSettle.Changed && win.RunId != run.RunId
            && reopened != null && reopened.CurrentProfile.settledRunId == win.RunId
            && Overlaps(list2, reopened.CurrentProfile.shopList.shopIds) == false
            && ValidList(db, reopened.CurrentProfile.shopList.shopIds, 4);
    }

    static bool TestSettleOnce(CsvDatabase db)
    {
        Fx fx = Login();
        ShopListManager mgr = Mgr(fx, db, 51);
        RunState run;
        if (fx == null || !mgr.EnsureList().Ok || !mgr.StartRun(mgr.CurrentShopIds[0], out run).Ok || !PlayToFail(run, db))
            return false;

        if (!mgr.SettleRun(run).Ok)
            return false;
        V02ShopListState once = fx.Service.CurrentProfile.shopList.Clone();

        // 같은 결과를 여러 번 전달해도 목록은 한 번만 바뀐다 (저장본 포함).
        for (int i = 0; i < 3; i++)
        {
            ShopListResult again = mgr.SettleRun(run);
            if (again.Status != ShopListOpStatus.AlreadySettled || again.Changed
                || !SameState(fx.Service.CurrentProfile.shopList, once))
                return false;
        }

        // 재접속 후 전달(새 매니저/새 서비스)도 마찬가지.
        V02LocalProfileService reopened = Reopen(fx);
        if (reopened == null)
            return false;
        ShopListResult later = new ShopListManager(reopened, db, new ShopListDrawerSelfTest.SeededIntRoll(52)).SettleRun(run);
        return later.Status == ShopListOpStatus.AlreadySettled && SameState(reopened.CurrentProfile.shopList, once);
    }

    static bool TestForceKill(CsvDatabase db)
    {
        Fx fx = Login();
        ShopListManager mgr = Mgr(fx, db, 61);
        RunState run;
        if (fx == null || !mgr.EnsureList().Ok || !mgr.TryReroll().Ok)
            return false;
        V02ShopListState before = fx.Service.CurrentProfile.shopList.Clone();
        if (!mgr.StartRun(before.shopIds[0], out run).Ok)
            return false;

        // 런 도중 프로세스 종료 = 새 서비스로 다시 로그인.
        V02LocalProfileService restarted = Reopen(fx);
        if (restarted == null || restarted.CurrentProfile.activeRunId != run.RunId)
            return false;

        var mgr2 = new ShopListManager(restarted, db, new ShopListDrawerSelfTest.SeededIntRoll(62));
        // 살아 있는 런이 있으면 지우지 않는다.
        mgr2.ClearStaleActiveRun(true);
        if (restarted.CurrentProfile.activeRunId != run.RunId)
            return false;

        int gold = restarted.CurrentProfile.gold;
        int candy = restarted.CurrentProfile.candy;
        int energy = restarted.CurrentProfile.energy;
        ShopListResult cleared = mgr2.ClearStaleActiveRun(false);
        V02LocalProfileService third = Reopen(fx);
        V02ShopListState s = third.CurrentProfile.shopList;
        return cleared.Ok && third.CurrentProfile.activeRunId == "" && third.CurrentProfile.settledRunId == ""
            && SameState(s, before)
            && third.CurrentProfile.gold == gold && third.CurrentProfile.candy == candy && third.CurrentProfile.energy == energy;
    }

    // ---------- 12~14 ----------

    static bool TestRejects(CsvDatabase db)
    {
        // 프로필 없음
        var noLogin = new V02LocalProfileService(Path.Combine(NewDir(), "profiles.json"));
        RunState run;
        var anon = new ShopListManager(noLogin, db, new ShopListDrawerSelfTest.SeededIntRoll(1));
        if (anon.EnsureList().Status != ShopListOpStatus.NoProfile || anon.TryReroll().Status != ShopListOpStatus.NoProfile
            || anon.StartRun("SHOP_001", out run).Status != ShopListOpStatus.NoProfile
            || anon.ClearStaleActiveRun(false).Status != ShopListOpStatus.NoProfile)
            return false;

        // 목록 없음
        Fx fx = Login();
        ShopListManager mgr = Mgr(fx, db, 71);
        if (fx == null || mgr.TryReroll().Status != ShopListOpStatus.NoList
            || mgr.StartRun("SHOP_001", out run).Status != ShopListOpStatus.NoList || run != null)
            return false;

        // 목록 밖 가게
        if (!mgr.EnsureList().Ok)
            return false;
        string outside = null;
        for (int i = 1; i <= 20 && outside == null; i++)
        {
            string id = "SHOP_" + i.ToString("D3", CultureInfo.InvariantCulture);
            if (Array.IndexOf(fx.Service.CurrentProfile.shopList.shopIds, id) < 0)
                outside = id;
        }

        RunState outsideRun;
        ShopListResult notIn = mgr.StartRun(outside, out outsideRun);
        return notIn.Status == ShopListOpStatus.NotInList && outsideRun == null && fx.Service.CurrentProfile.activeRunId == "";
    }

    static bool TestSettleRetry(CsvDatabase live)
    {
        CsvDatabase noWeights = ConfigDb(t => RemoveLines(t, "RUMOR_COUNT_WEIGHT_"));
        Fx fx = Login();
        ShopListManager mgr = Mgr(fx, live, 81);
        RunState run;
        if (noWeights == null || fx == null || !mgr.EnsureList().Ok || !mgr.StartRun(mgr.CurrentShopIds[0], out run).Ok || !PlayToFail(run, live))
            return false;

        V02ShopListState before = fx.Service.CurrentProfile.shopList.Clone();
        string active = fx.Service.CurrentProfile.activeRunId;
        ShopListResult failed = Mgr(fx, noWeights, 82).SettleRun(run);
        if (failed.Status != ShopListOpStatus.DrawFailed || fx.Service.CurrentProfile.settledRunId != ""
            || !SameState(fx.Service.CurrentProfile.shopList, before) || fx.Service.CurrentProfile.activeRunId != active)
            return false;

        // 정산 표식이 없으므로 같은 런 결과를 다시 전달하면 이번에는 정산된다.
        ShopListResult retry = mgr.SettleRun(run);
        return retry.Ok && retry.Changed && fx.Service.CurrentProfile.settledRunId == run.RunId;
    }

    static bool TestRunIdAndLiveRumors(CsvDatabase db)
    {
        RunState a, b;
        if (!RunState.TryStart(db, "SHOP_001", out a) || !RunState.TryStart(db, "SHOP_001", out b))
            return false;
        if (string.IsNullOrEmpty(a.RunId) || a.RunId == b.RunId)
            return false;

        // 정식 소문 데이터가 없으면 모든 가게의 소문은 0개로 정상 처리된다.
        Fx fx = Login();
        ShopListManager mgr = Mgr(fx, db, 91);
        return fx != null && mgr.EnsureList().Ok && fx.Service.CurrentProfile.shopList.rumors.Length == 0
            && mgr.GetRumorIds(mgr.CurrentShopIds[0]).Count == 0;
    }

    // ---------- 런 진행 도우미 ----------

    static bool PlayToFail(RunState run, CsvDatabase db)
    {
        var resolver = new ActionResolver(db, new FixedChanceRoll(false));
        for (int i = 0; i < 40 && !run.IsEnded; i++)
        {
            ActionResolveResult result;
            if (!resolver.TryResolve(run, "ACT_003", out result))
                break;
        }

        return run.Status == RunStatus.Failed;
    }

    static bool PlayToWin(RunState run, CsvDatabase db)
    {
        var resolver = new ActionResolver(db, new FixedChanceRoll(true));
        for (int i = 0; i < 12 && !run.IsEnded; i++)
        {
            string action;
            switch (run.CurrentPhase)
            {
                case "MOVE": action = "ACT_001"; break;
                case "INFILTRATE": action = "ACT_004"; break;
                case "LOOT": action = run.Candy >= 1 ? "ACT_013" : "ACT_007"; break;
                default: action = "ACT_010"; break;
            }

            ActionResolveResult result;
            if (!resolver.TryResolve(run, action, out result))
                break;
        }

        return run.Status == RunStatus.Won;
    }

    // ---------- 프로필/DB 도우미 ----------

    static string NewDir()
    {
        string dir = Path.Combine(Path.GetTempPath(), "ct_shoplist_" + Guid.NewGuid().ToString("N"));
        TempDirs.Add(dir);
        return dir;
    }

    static Fx Login(string nickname = Nick)
    {
        string path = Path.Combine(NewDir(), "profiles.json");
        var fx = new Fx { Path = path, Service = new V02LocalProfileService(path) };
        V02PlayerProfile profile;
        string error;
        return fx.Service.TryLogin(nickname, out profile, out error) ? fx : null;
    }

    static Fx FromJson(string json)
    {
        string path = Path.Combine(NewDir(), "profiles.json");
        Directory.CreateDirectory(Path.GetDirectoryName(path));
        File.WriteAllText(path, json, new UTF8Encoding(false));
        return new Fx { Path = path, Service = new V02LocalProfileService(path) };
    }

    static string Prof(string id, string nick, int energy, int gold, int candy, string now, string extra)
    {
        return string.Format(CultureInfo.InvariantCulture,
            "{{\"profileId\":\"{0}\",\"nickname\":\"{1}\",\"createdAt\":\"{5}\",\"lastLoginAt\":\"{5}\",\"energy\":{2},\"gold\":{3},\"candy\":{4},\"energyUpdatedAt\":\"{5}\"{6}}}",
            id, nick, energy, gold, candy, now, extra.Length == 0 ? "" : "," + extra);
    }

    // 프로세스 재시작처럼 같은 파일을 새 서비스로 다시 연다.
    static V02LocalProfileService Reopen(Fx fx, string nickname = Nick)
    {
        var service = new V02LocalProfileService(fx.Path);
        V02PlayerProfile profile;
        string error;
        return service.TryLogin(nickname, out profile, out error) ? service : null;
    }

    static ShopListManager Mgr(Fx fx, CsvDatabase db, int seed)
    {
        return new ShopListManager(fx != null ? fx.Service : null, db, new ShopListDrawerSelfTest.SeededIntRoll(seed));
    }

    // 저장 파일 자리에 폴더를 만들어 이후 저장이 실패하게 한다.
    static void BreakStorage(Fx fx)
    {
        if (File.Exists(fx.Path))
            File.Delete(fx.Path);
        Directory.CreateDirectory(fx.Path);
    }

    static CsvDatabase ConfigDb(Func<string, string> editConfig)
    {
        Dictionary<string, string> texts = ShopRumorSelfTest.LiveTexts();
        if (texts == null)
            return null;
        texts["GameConfig"] = editConfig(texts["GameConfig"]);
        CsvDatabase db = CsvDatabase.LoadFromTexts(texts);
        return db != null && db.Loaded && !db.HasErrors ? db : null;
    }

    static CsvDatabase ShopCountDb(int count)
    {
        Dictionary<string, string> texts = ShopRumorSelfTest.LiveTexts();
        if (texts == null)
            return null;
        string[] lines = texts["ShopTable"].Replace("\r\n", "\n").Split('\n');
        var kept = new List<string>();
        for (int i = 0; i < lines.Length && kept.Count < count + 1; i++)
        {
            if (lines[i].Length > 0)
                kept.Add(lines[i]);
        }

        texts["ShopTable"] = string.Join("\r\n", kept.ToArray()) + "\r\n";
        CsvDatabase db = CsvDatabase.LoadFromTexts(texts);
        return db != null && db.Loaded && !db.HasErrors ? db : null;
    }

    // 20개 가게 모두에 후보가 있는 소문 픽스처 (정식 CSV 아님).
    static CsvDatabase RumorDb()
    {
        Dictionary<string, string> texts = ShopRumorSelfTest.LiveTexts();
        if (texts == null)
            return null;

        var candidates = new StringBuilder(ShopRumorSelfTest.CandidateHeader);
        for (int i = 1; i <= 20; i++)
        {
            string id = "SHOP_" + i.ToString("D3", CultureInfo.InvariantCulture);
            candidates.Append(ShopRumorSelfTest.C(id, "T_RUM_SUCC", 10));
            candidates.Append(ShopRumorSelfTest.C(id, "T_RUM_EVT", 10));
            candidates.Append(ShopRumorSelfTest.C(id, "T_RUM_G1A", 5));
            candidates.Append(ShopRumorSelfTest.C(id, "T_RUM_G1B", 5));
        }

        texts["ShopRumorTable"] = ShopRumorSelfTest.RumorHeader
            + ShopRumorSelfTest.R("T_RUM_SUCC", "", 5, 0, 0f, 0, 1)
            + ShopRumorSelfTest.R("T_RUM_EVT", "", 0, 0, 0f, 10, 1)
            + ShopRumorSelfTest.R("T_RUM_G1A", "G1", 3, 0, 0f, 0, 1)
            + ShopRumorSelfTest.R("T_RUM_G1B", "G1", 0, 5, 0f, 0, 1);
        texts["ShopRumorCandidate"] = candidates.ToString();
        CsvDatabase db = CsvDatabase.LoadFromTexts(texts);
        return db != null && db.Loaded && !db.HasErrors ? db : null;
    }

    static string AppendLine(string text, string line)
    {
        return text.TrimEnd('\r', '\n') + "\r\n" + line + "\r\n";
    }

    static string RemoveLines(string text, string prefix)
    {
        var kept = new List<string>();
        foreach (string line in text.Replace("\r\n", "\n").Split('\n'))
        {
            if (!line.StartsWith(prefix, StringComparison.Ordinal))
                kept.Add(line);
        }

        return string.Join("\r\n", kept.ToArray());
    }

    // ---------- 비교 ----------

    static bool ValidList(CsvDatabase db, string[] ids, int size)
    {
        if (ids == null || ids.Length != size || new HashSet<string>(ids).Count != size)
            return false;
        for (int i = 0; i < ids.Length; i++)
        {
            ShopRow shop;
            if (!db.TryGetShop(ids[i], out shop) || !ShopListDrawer.IsDrawable(shop))
                return false;
        }

        return true;
    }

    static bool Overlaps(string[] a, string[] b)
    {
        for (int i = 0; i < a.Length; i++)
        {
            if (Array.IndexOf(b, a[i]) >= 0)
                return true;
        }

        return false;
    }

    static bool SameArray(string[] a, string[] b)
    {
        if (a == null || b == null || a.Length != b.Length)
            return false;
        for (int i = 0; i < a.Length; i++)
        {
            if (a[i] != b[i])
                return false;
        }

        return true;
    }

    static bool SamePairs(V02ShopRumorPair[] a, V02ShopRumorPair[] b)
    {
        if (a == null || b == null || a.Length != b.Length)
            return false;
        for (int i = 0; i < a.Length; i++)
        {
            if (a[i].shopId != b[i].shopId || a[i].rumorId != b[i].rumorId)
                return false;
        }

        return true;
    }

    static bool SameState(V02ShopListState a, V02ShopListState b)
    {
        return a != null && b != null && a.listId == b.listId && SameArray(a.shopIds, b.shopIds)
            && SamePairs(a.rumors, b.rumors) && SameArray(a.previousShopIds, b.previousShopIds)
            && a.freeRerollUsed == b.freeRerollUsed && a.totalRerollUsed == b.totalRerollUsed;
    }
}
