using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

// OP-DEV-10. 임시 프로필만 사용한다. 포기 확인과 SettleRun 재사용을 검사한다.
public static class V02AbandonSelfTest
{
    const string LogPrefix = "[Abandon] ";
    const string Nick = "포기테스트";

    static readonly List<string> TempDirs = new List<string>();
    static readonly List<GameObject> Spawned = new List<GameObject>();

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
            failed += Expect("1 로비 버튼은 확인 팝업 하나", TestPopupOnce(database));
            failed += Expect("2 취소는 런과 저장과 난수를 그대로 둠", TestCancel(database));
            failed += Expect("3 포기 확정은 실패 정산 1회와 새 목록, 재화 불변", TestConfirm(database));
            failed += Expect("4 중복 포기는 정산을 반복하지 않음", TestDuplicate(database));
            failed += Expect("5 이미 끝난 런은 포기 정산 불가, 결과 정산은 1회", TestEndedRefuses(database));
            failed += Expect("6 저장·추첨 실패는 런과 목록을 유지", TestFailureStays(database));
            failed += Expect("7 강제 종료는 실패 정산과 목록 갱신 없음", TestForceKill(database));
        }
        finally
        {
            RunState.Current = previous;
            for (int i = 0; i < Spawned.Count; i++)
            {
                if (Spawned[i] != null)
                    UnityEngine.Object.DestroyImmediate(Spawned[i]);
            }

            Spawned.Clear();
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
            Debug.Log(LogPrefix + "OP-DEV-10 자가 검사 7건 통과");
        else
            Debug.LogError(LogPrefix + "자가 검사 실패 " + failed + "건");
        return failed == 0;
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

    static bool TestPopupOnce(CsvDatabase db)
    {
        Fx fx = Login();
        ShopListManager mgr = Mgr(fx, db, 11);
        RunState run;
        V02RunHud hud = null;
        if (fx == null || !mgr.EnsureList().Ok || !mgr.StartRun(mgr.CurrentShopIds[0], out run).Ok)
            return false;
        int leave = 0;
        int abandon = 0;
        if (!TryHud(db, run, () => leave++, () => abandon++, out hud))
            return false;

        hud.LobbyButton.onClick.Invoke();
        hud.LobbyButton.onClick.Invoke();
        int views = hud.GetComponentsInChildren<V02AbandonConfirm>(true).Length;
        return views == 1
            && hud.AbandonConfirmOpen
            && hud.AbandonConfirm.Title == V02AbandonConfirm.TitleCopy
            && hud.AbandonConfirm.Body == V02AbandonConfirm.BodyCopy
            && hud.ChoicesLocked
            && hud.HasVisibleChoices
            && leave == 0
            && abandon == 0
            && run.Status == RunStatus.InProgress;
    }

    static bool TestCancel(CsvDatabase db)
    {
        Fx fx = Login();
        ShopListManager mgr = Mgr(fx, db, 21);
        RunState run;
        V02RunHud hud = null;
        if (fx == null || !mgr.EnsureList().Ok || !mgr.StartRun(mgr.CurrentShopIds[0], out run).Ok)
            return false;
        ActionResolveResult resolved;
        if (!new ActionResolver(db, new FixedChanceRoll(true)).TryResolve(run, "ACT_001", out resolved) || !resolved.Applied)
            return false;

        int turns = run.RemainingTurns;
        int alert = run.Alert;
        int candy = run.Candy;
        string last = run.LastActionID;
        string listId = fx.Service.CurrentProfile.shopList.listId;
        string active = fx.Service.CurrentProfile.activeRunId;
        int gold = fx.Service.CurrentProfile.gold;
        int energy = fx.Service.CurrentProfile.energy;
        int bank = fx.Service.CurrentProfile.candy;
        if (!TryHud(db, run, null, null, out hud))
            return false;

        UnityEngine.Random.State rng = UnityEngine.Random.state;
        hud.LobbyButton.onClick.Invoke();
        hud.AbandonConfirm.CancelButton.onClick.Invoke();
        V02LocalProfileService reopened = Reopen(fx);
        UnityEngine.Random.State after = UnityEngine.Random.state;
        return !hud.AbandonConfirmOpen
            && !hud.ChoicesLocked
            && run.Status == RunStatus.InProgress
            && run.FailReason == RunFailReason.None
            && run.RemainingTurns == turns
            && run.Alert == alert
            && run.Candy == candy
            && run.LastActionID == last
            && run.HasSucceededAction("ACT_001")
            && reopened != null
            && reopened.CurrentProfile.shopList.listId == listId
            && reopened.CurrentProfile.activeRunId == active
            && reopened.CurrentProfile.settledRunId == ""
            && reopened.CurrentProfile.gold == gold
            && reopened.CurrentProfile.energy == energy
            && reopened.CurrentProfile.candy == bank
            && SameRng(rng, after);
    }

    static bool TestConfirm(CsvDatabase db)
    {
        Fx fx = Login();
        ShopListManager mgr = Mgr(fx, db, 31);
        RunState run;
        V02RunHud hud = null;
        if (fx == null || !mgr.EnsureList().Ok || !mgr.StartRun(mgr.CurrentShopIds[0], out run).Ok)
            return false;
        string[] before = (string[])fx.Service.CurrentProfile.shopList.shopIds.Clone();
        int gold = fx.Service.CurrentProfile.gold;
        int energy = fx.Service.CurrentProfile.energy;
        int bank = fx.Service.CurrentProfile.candy;
        int turns = run.RemainingTurns;
        int alert = run.Alert;
        int candy = run.Candy;
        bool left = false;
        if (!TryHud(db, run, () => left = true, () =>
        {
            ShopListResult settled = mgr.AbandonRun(run);
            if (settled.Ok)
            {
                hud.CloseAbandonConfirm();
                left = true;
            }
            else
                hud.AbandonAttemptFailed();
        }, out hud))
            return false;

        hud.LobbyButton.onClick.Invoke();
        hud.AbandonConfirm.ConfirmButton.onClick.Invoke();
        V02PlayerProfile profile = fx.Service.CurrentProfile;
        return left
            && run.Status == RunStatus.Failed
            && run.FailReason == RunFailReason.Abandon
            && run.RemainingTurns == turns
            && run.Alert == alert
            && run.Candy == candy
            && profile.settledRunId == run.RunId
            && profile.activeRunId == ""
            && profile.gold == gold
            && profile.energy == energy
            && profile.candy == bank
            && profile.shopList.freeRerollUsed == 0
            && profile.shopList.shopIds.Length == 4
            && profile.shopList.listId.Length > 0
            && !SameArray(profile.shopList.shopIds, before)
            && SameArray(profile.shopList.previousShopIds, before);
    }

    static bool TestDuplicate(CsvDatabase db)
    {
        Fx fx = Login();
        ShopListManager mgr = Mgr(fx, db, 41);
        RunState run;
        V02RunHud hud = null;
        if (fx == null || !mgr.EnsureList().Ok || !mgr.StartRun(mgr.CurrentShopIds[0], out run).Ok)
            return false;
        int calls = 0;
        if (!TryHud(db, run, null, () =>
        {
            calls++;
            mgr.AbandonRun(run);
        }, out hud))
            return false;

        hud.LobbyButton.onClick.Invoke();
        hud.AbandonConfirm.ConfirmButton.onClick.Invoke();
        hud.AbandonConfirm.ConfirmButton.onClick.Invoke();
        string listId = fx.Service.CurrentProfile.shopList.listId;
        ShopListResult again = mgr.AbandonRun(run);
        ShopListResult settledAgain = mgr.SettleRun(run);
        return calls == 1
            && again.Status == ShopListOpStatus.AlreadySettled
            && !again.Changed
            && settledAgain.Status == ShopListOpStatus.AlreadySettled
            && !settledAgain.Changed
            && fx.Service.CurrentProfile.shopList.listId == listId
            && fx.Service.CurrentProfile.settledRunId == run.RunId;
    }

    static bool TestEndedRefuses(CsvDatabase db)
    {
        Fx fx = Login();
        ShopListManager mgr = Mgr(fx, db, 51);
        RunState run;
        V02RunHud hud = null;
        if (fx == null || !mgr.EnsureList().Ok || !mgr.StartRun(mgr.CurrentShopIds[0], out run).Ok)
            return false;
        string listId = fx.Service.CurrentProfile.shopList.listId;
        int gold = fx.Service.CurrentProfile.gold;
        if (!PlayToFail(run, db))
            return false;
        ShopListResult refused = mgr.AbandonRun(run);
        if (refused.Status != ShopListOpStatus.AbandonRefused || refused.Changed
            || fx.Service.CurrentProfile.shopList.listId != listId
            || fx.Service.CurrentProfile.gold != gold
            || run.FailReason == RunFailReason.Abandon)
            return false;

        ShopListResult once = mgr.SettleRun(run);
        ShopListResult twice = mgr.SettleRun(run);
        int leave = 0;
        int abandon = 0;
        if (!once.Ok || twice.Status != ShopListOpStatus.AlreadySettled || !TryHud(db, run, () => leave++, () => abandon++, out hud))
            return false;
        hud.LobbyButton.onClick.Invoke();
        return leave == 1 && abandon == 0 && !hud.AbandonConfirmOpen
            && fx.Service.CurrentProfile.settledRunId == run.RunId;
    }

    static bool TestFailureStays(CsvDatabase db)
    {
        Fx fx = Login();
        ShopListManager mgr = Mgr(fx, db, 61);
        RunState run;
        if (fx == null || !mgr.EnsureList().Ok || !mgr.StartRun(mgr.CurrentShopIds[0], out run).Ok)
            return false;
        string listId = fx.Service.CurrentProfile.shopList.listId;
        string active = fx.Service.CurrentProfile.activeRunId;
        int gold = fx.Service.CurrentProfile.gold;
        BreakStorage(fx);
        ShopListResult saveFailed = mgr.AbandonRun(run);
        if (saveFailed.Status != ShopListOpStatus.SaveFailed
            || run.Status != RunStatus.InProgress
            || run.FailReason != RunFailReason.None
            || fx.Service.CurrentProfile.shopList.listId != listId
            || fx.Service.CurrentProfile.activeRunId != active
            || fx.Service.CurrentProfile.settledRunId != ""
            || fx.Service.CurrentProfile.gold != gold)
            return false;

        Fx fx2 = Login();
        ShopListManager live = Mgr(fx2, db, 62);
        RunState run2;
        CsvDatabase noWeights = WithoutRumorWeights();
        if (fx2 == null || noWeights == null || !live.EnsureList().Ok || !live.StartRun(live.CurrentShopIds[0], out run2).Ok)
            return false;
        string list2 = fx2.Service.CurrentProfile.shopList.listId;
        ShopListResult drawFailed = new ShopListManager(fx2.Service, noWeights, new ShopListDrawerSelfTest.SeededIntRoll(63)).AbandonRun(run2);
        if (drawFailed.Status != ShopListOpStatus.DrawFailed
            || run2.Status != RunStatus.InProgress
            || fx2.Service.CurrentProfile.shopList.listId != list2
            || fx2.Service.CurrentProfile.settledRunId != "")
            return false;

        ShopListResult retry = live.AbandonRun(run2);
        return retry.Ok && fx2.Service.CurrentProfile.settledRunId == run2.RunId
            && run2.Status == RunStatus.Failed
            && run2.FailReason == RunFailReason.Abandon;
    }

    static bool TestForceKill(CsvDatabase db)
    {
        Fx fx = Login();
        ShopListManager mgr = Mgr(fx, db, 71);
        RunState run;
        if (fx == null || !mgr.EnsureList().Ok || !mgr.StartRun(mgr.CurrentShopIds[0], out run).Ok)
            return false;
        string listId = fx.Service.CurrentProfile.shopList.listId;
        int gold = fx.Service.CurrentProfile.gold;
        int energy = fx.Service.CurrentProfile.energy;
        int candy = fx.Service.CurrentProfile.candy;
        V02LocalProfileService restarted = Reopen(fx);
        if (restarted == null)
            return false;
        ShopListResult cleared = new ShopListManager(restarted, db, new ShopListDrawerSelfTest.SeededIntRoll(72)).ClearStaleActiveRun(false);
        V02LocalProfileService again = Reopen(fx);
        return cleared.Ok
            && run.Status == RunStatus.InProgress
            && again != null
            && again.CurrentProfile.activeRunId == ""
            && again.CurrentProfile.settledRunId == ""
            && again.CurrentProfile.shopList.listId == listId
            && again.CurrentProfile.gold == gold
            && again.CurrentProfile.energy == energy
            && again.CurrentProfile.candy == candy;
    }

    static bool TryHud(CsvDatabase db, RunState run, UnityEngine.Events.UnityAction onLeave, UnityEngine.Events.UnityAction onAbandon, out V02RunHud hud)
    {
        hud = null;
        V02RunHud prefab = Resources.Load<V02RunHud>(V02RunHud.ResourcePath);
        if (prefab == null)
            return false;
        hud = UnityEngine.Object.Instantiate(prefab);
        Spawned.Add(hud.gameObject);
        hud.Bind(db, run, onLeave, null, null, onAbandon);
        return hud.LobbyButton != null;
    }

    static bool PlayToFail(RunState run, CsvDatabase db)
    {
        var resolver = new ActionResolver(db, new FixedChanceRoll(false));
        for (int i = 0; i < 40 && !run.IsEnded; i++)
        {
            ActionResolveResult result;
            if (!resolver.TryResolve(run, "ACT_003", out result))
                break;
        }

        return run.Status == RunStatus.Failed && run.FailReason != RunFailReason.Abandon;
    }

    static CsvDatabase WithoutRumorWeights()
    {
        Dictionary<string, string> texts = ShopRumorSelfTest.LiveTexts();
        if (texts == null)
            return null;
        var kept = new List<string>();
        foreach (string line in texts["GameConfig"].Replace("\r\n", "\n").Split('\n'))
        {
            if (!line.StartsWith("RUMOR_COUNT_WEIGHT_", StringComparison.Ordinal))
                kept.Add(line);
        }

        texts["GameConfig"] = string.Join("\n", kept.ToArray());
        CsvDatabase database = CsvDatabase.LoadFromTexts(texts);
        return database != null && database.Loaded && !database.HasErrors ? database : null;
    }

    static Fx Login()
    {
        string dir = Path.Combine(Path.GetTempPath(), "ct_abandon_" + Guid.NewGuid().ToString("N"));
        TempDirs.Add(dir);
        string path = Path.Combine(dir, "profiles.json");
        var fx = new Fx { Path = path, Service = new V02LocalProfileService(path) };
        V02PlayerProfile profile;
        string error;
        return fx.Service.TryLogin(Nick, out profile, out error) ? fx : null;
    }

    static V02LocalProfileService Reopen(Fx fx)
    {
        var service = new V02LocalProfileService(fx.Path);
        V02PlayerProfile profile;
        string error;
        return service.TryLogin(Nick, out profile, out error) ? service : null;
    }

    static ShopListManager Mgr(Fx fx, CsvDatabase db, int seed)
    {
        return new ShopListManager(fx != null ? fx.Service : null, db, new ShopListDrawerSelfTest.SeededIntRoll(seed));
    }

    static void BreakStorage(Fx fx)
    {
        if (File.Exists(fx.Path))
            File.Delete(fx.Path);
        Directory.CreateDirectory(fx.Path);
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

    static bool SameRng(UnityEngine.Random.State before, UnityEngine.Random.State after)
    {
        return before.Equals(after);
    }
}
