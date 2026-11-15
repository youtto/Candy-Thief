using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public static class RunOutcomeSelfTest
{
    const string LogPrefix = "[RunOutcome] ";

    public static bool RunAll(CsvDatabase database)
    {
        if (database == null || !database.Loaded || database.HasErrors)
        {
            Debug.LogError(LogPrefix + "자가 검사 실패: CsvDatabase가 준비되지 않았습니다.");
            return false;
        }

        RunState previous = RunState.Current;
        int failed = 0;
        failed += Expect("1 탈출 성공이 턴 0과 경계도보다 앞선다", TestEscapeFirst(database));
        failed += Expect("2 실패가 경계도와 턴을 같이 쓰면 체포", TestCaptureBeforeNoTurns(database));
        failed += Expect("3 작전 성공만 사탕을 한 번 정산한다", TestSettlementOnce(database));
        failed += Expect("4 행동 결과 다음에도 카드 2장", TestActionThenCards(database));
        failed += Expect("5 종료 행동은 작전 결과로 넘어간다", TestActionThenOutcome(database));
        failed += Expect("6 결과 문구와 빈 전리품", TestCopyAndEmptyLoot(database));
        failed += Expect("7 획득 사탕과 정산 사탕을 구분한다", TestAcquiredVersusSettled(database));
        failed += Expect("8 재도전과 로비, 포기는 결과 팝업을 열지 않음", TestButtonsAndAbandon(database));

        RunState.Current = previous;
        if (failed == 0)
        {
            Debug.Log(LogPrefix + "자가 검사 8건 통과");
            return true;
        }

        Debug.LogError(LogPrefix + "자가 검사 실패: " + failed + "건");
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

    static bool TestEscapeFirst(CsvDatabase database)
    {
        RunState timed;
        if (!RunState.TryStart(database, "SHOP_001", out timed))
            return false;
        if (!timed.ApplyCandyDelta(4))
            return false;
        if (!timed.TryApplyResolved(Escape(true, 0, timed.RemainingTurns)))
            return false;
        if (timed.Outcome != RunOutcome.OperationSuccess || timed.RemainingTurns != 0)
            return false;

        RunState alerted;
        if (!RunState.TryStart(database, "SHOP_001", out alerted))
            return false;
        int max = database.GetInt("MAX_ALERT");
        if (!alerted.ApplyCandyDelta(2))
            return false;
        if (!alerted.TryApplyResolved(Escape(true, max - alerted.Alert, 0)))
            return false;
        return alerted.Outcome == RunOutcome.OperationSuccess && alerted.Alert == max;
    }

    static bool TestCaptureBeforeNoTurns(CsvDatabase database)
    {
        RunState run;
        if (!RunState.TryStart(database, "SHOP_001", out run))
            return false;
        int max = database.GetInt("MAX_ALERT");
        if (!run.TryApplyResolved(new ResolvedAction
        {
            ActionID = "ACT_003",
            TurnCost = run.RemainingTurns,
            Success = false,
            CandyDelta = 0,
            AlertDelta = max,
            TurnDelta = 0,
            SuccessNextPhase = "INFILTRATE",
            IsEscapeAction = false
        }))
            return false;
        return run.RemainingTurns == 0
            && run.Alert == max
            && run.Outcome == RunOutcome.Captured
            && run.FailReason == RunFailReason.AlertLimit
            && run.SettledCandy == 0;
    }

    static bool TestSettlementOnce(CsvDatabase database)
    {
        string dir = Path.Combine(Path.GetTempPath(), "ct_outcome_" + Guid.NewGuid().ToString("N"));
        try
        {
            Directory.CreateDirectory(dir);
            var service = new V02LocalProfileService(Path.Combine(dir, "profiles.json"));
            V02PlayerProfile profile;
            string error;
            if (!service.TryLogin("정산테스트", out profile, out error))
                return false;
            var mgr = new ShopListManager(service, database, new ShopListDrawerSelfTest.SeededIntRoll(7));
            if (!mgr.EnsureList().Ok)
                return false;
            RunState failed;
            if (!mgr.StartRun(mgr.CurrentShopIds[0], out failed).Ok)
                return false;
            if (!failed.ApplyAlertDelta(database.GetInt("MAX_ALERT")))
                return false;
            int before = service.CurrentProfile.candy;
            if (!mgr.SettleRun(failed).Ok || service.CurrentProfile.candy != before)
                return false;

            RunState won;
            if (!mgr.StartRun(mgr.CurrentShopIds[0], out won).Ok)
                return false;
            if (!won.SetPhase("ESCAPE") || !won.ApplyCandyDelta(10))
                return false;
            ActionRow escape;
            if (!database.TryGetAction("ACT_010", out escape) || !won.TryApplyAction(escape, true))
                return false;
            if (won.Outcome != RunOutcome.OperationSuccess)
                return false;
            int expected = before + won.SettledCandy;
            if (!mgr.SettleRun(won).Ok || service.CurrentProfile.candy != expected)
                return false;
            return mgr.SettleRun(won).Status == ShopListOpStatus.AlreadySettled
                && service.CurrentProfile.candy == expected;
        }
        finally
        {
            if (Directory.Exists(dir))
                Directory.Delete(dir, true);
        }
    }

    static bool TestActionThenCards(CsvDatabase database)
    {
        V02RunHud hud;
        RunState run;
        if (!TryHud(database, new FixedChanceRoll(false), out hud, out run))
            return false;
        try
        {
            string[] before = ActiveActionIds(hud);
            if (before.Length != 2 || !hud.TryChoose(before[0], false))
                return false;
            hud.ResultPresentation.SkipToHold();
            if (hud.ResultPresentation.TitleLabel != "성공!" && hud.ResultPresentation.TitleLabel != "실패…")
                return false;
            if (!hud.TryContinueResult() || run.IsEnded)
                return false;
            string[] next = ActiveActionIds(hud);
            return next.Length == 2 && (hud.OperationResult == null || !hud.OperationResult.IsOpen);
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(hud.gameObject);
        }
    }

    static bool TestActionThenOutcome(CsvDatabase database)
    {
        V02RunHud hud;
        RunState run;
        if (!TryHud(database, new FixedChanceRoll(true), out hud, out run))
            return false;
        try
        {
            if (!run.SetPhase("ESCAPE") || !run.ApplyCandyDelta(6))
                return false;
            hud.Refresh();
            if (!hud.TryChoose("ACT_012", false))
                return false;
            hud.ResultPresentation.SkipToHold();
            string actionTitle = hud.ResultPresentation.TitleLabel;
            if (actionTitle != "성공!" && actionTitle != "실패…")
                return false;
            if (!hud.TryContinueResult())
                return false;
            return !hud.IsPresenting
                && !hud.HasVisibleChoices
                && hud.OperationResult != null
                && hud.OperationResult.IsOpen
                && run.IsEnded;
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(hud.gameObject);
        }
    }

    static bool TestCopyAndEmptyLoot(CsvDatabase database)
    {
        V02RunResultPopup prefab = Resources.Load<V02RunResultPopup>(V02RunResultPopup.ResourcePath);
        if (prefab == null)
            return false;
        RunState run;
        if (!RunState.TryStart(database, "SHOP_001", out run) || !run.ApplyTurnDelta(-run.RemainingTurns))
            return false;
        V02RunResultPopup view = UnityEngine.Object.Instantiate(prefab);
        try
        {
            view.Present(run, null);
            var slots = new List<V02RunLootSlot>();
            view.CollectSlots(slots);
            bool hidden = true;
            for (int i = 0; i < slots.Count; i++)
            {
                if (slots[i].gameObject.activeSelf)
                    hidden = false;
            }

            if (run.Outcome != RunOutcome.NoTurns
                || view.Title != "탈출 실패 !"
                || view.Subtitle != "탈출 기회를 놓쳐버렸다!"
                || view.Message != "우물쭈물하다 기회를 다 써버렸어요. 망했다 !"
                || !view.EmptyLootVisible
                || view.EmptyLootLabel != V02RunResultPopup.EmptyLootCopy
                || slots.Count < 3
                || !hidden)
                return false;
            if (!PresentCopy(database, view, "Empty") || !PresentCopy(database, view, "Captured") || !PresentCopy(database, view, "Success"))
                return false;

            var grants = new List<RunLootGrant>();
            for (int i = 0; i < 4; i++)
                grants.Add(new RunLootGrant { ItemId = "SHOW_" + i, DisplayName = "표시" + i, Count = i + 1, Kept = i == 0 });
            view.Present(run, grants);
            view.CollectSlots(slots);
            int shown = 0;
            for (int i = 0; i < slots.Count; i++)
            {
                if (slots[i].gameObject.activeSelf)
                    shown++;
            }

            bool expanded = shown == 4 && !view.EmptyLootVisible;
            view.Present(run, null);
            return expanded && view.EmptyLootVisible && view.Title == "탈출 실패 !";
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(view.gameObject);
        }
    }

    static bool PresentCopy(CsvDatabase database, V02RunResultPopup view, string kind)
    {
        RunState run;
        if (!RunState.TryStart(database, "SHOP_001", out run))
            return false;
        if (kind == "Empty")
        {
            ActionRow escape;
            if (!database.TryGetAction("ACT_010", out escape) || !run.TryApplyAction(escape, true))
                return false;
            view.Present(run, null);
            return run.Outcome == RunOutcome.EmptyEscape
                && view.Title == "탈출 성공!"
                && view.Subtitle == "그런데 빈손이네..."
                && view.Message == "무사히 탈출하긴 했지만... 사탕은 하나도 못 챙겼어요."
                && view.SettledLabel == "0";
        }

        if (kind == "Captured")
        {
            if (!run.ApplyAlertDelta(database.GetInt("MAX_ALERT")))
                return false;
            view.Present(run, null);
            return run.Outcome == RunOutcome.Captured
                && view.Title == "들켰다!"
                && view.Subtitle == "경비에게 붙잡혔어요!"
                && view.Message == "경비에게 들켜 현장에서 체포되었어요!"
                && view.SettledLabel == "0";
        }

        ActionRow win;
        if (!run.ApplyCandyDelta(4) || !database.TryGetAction("ACT_010", out win) || !run.TryApplyAction(win, true))
            return false;
        view.Present(run, null);
        return run.Outcome == RunOutcome.OperationSuccess
            && view.Title == "작전 성공!"
            && view.Subtitle == "사탕을 왕창 훔쳤어요!"
            && view.Message == "가게의 사탕을 잔뜩 챙겨 무사히 탈출했어요!"
            && view.SettledLabel == "+" + run.SettledCandy;
    }

    static bool TestAcquiredVersusSettled(CsvDatabase database)
    {
        V02RunResultPopup prefab = Resources.Load<V02RunResultPopup>(V02RunResultPopup.ResourcePath);
        if (prefab == null)
            return false;
        RunState run;
        if (!RunState.TryStart(database, "SHOP_002", out run))
            return false;
        if (!run.SetPhase("ESCAPE") || !run.ApplyCandyDelta(10))
            return false;
        ActionRow escape;
        if (!database.TryGetAction("ACT_010", out escape) || !run.TryApplyAction(escape, true))
            return false;
        int settled = run.SettledCandy;
        if (settled <= run.Candy)
            return false;
        V02RunResultPopup view = UnityEngine.Object.Instantiate(prefab);
        try
        {
            view.Present(run, null);
            return view.Title == "작전 성공!"
                && view.AcquiredLabel == "+" + run.Candy
                && view.SettledLabel == "+" + settled
                && view.AcquiredLabel != view.SettledLabel;
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(view.gameObject);
        }
    }

    static bool TestButtonsAndAbandon(CsvDatabase database)
    {
        V02RunHud hud;
        RunState run;
        if (!TryHud(database, new FixedChanceRoll(true), out hud, out run))
            return false;
        int retry = 0;
        int lobby = 0;
        hud.SetOperationActions(() => retry++, () => lobby++);
        try
        {
            hud.Bind(database, run, null, new FixedChanceRoll(true), null, () =>
            {
                if (run.TryAbandon())
                    hud.CloseAbandonConfirm();
            }, new FixedIntRoll(0));
            hud.LobbyButton.onClick.Invoke();
            if (!hud.AbandonConfirmOpen || run.IsEnded)
                return false;
            hud.AbandonConfirm.ConfirmButton.onClick.Invoke();
            if (run.FailReason != RunFailReason.Abandon)
                return false;
            if (hud.OperationResult != null && hud.OperationResult.IsOpen)
                return false;

            RunState again;
            V02RunHud second;
            if (!TryHud(database, new FixedChanceRoll(true), out second, out again))
                return false;
            try
            {
                second.SetOperationActions(() => retry++, () => lobby++);
                if (!again.SetPhase("ESCAPE") || !again.ApplyCandyDelta(3))
                    return false;
                second.Refresh();
                if (!second.TryChoose("ACT_012", false))
                    return false;
                second.ResultPresentation.SkipToHold();
                if (!second.TryContinueResult() || second.OperationResult == null)
                    return false;
                second.OperationResult.RetryButton.onClick.Invoke();
                second.OperationResult.LobbyButton.onClick.Invoke();
                return retry == 1 && lobby == 1;
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(second.gameObject);
            }
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(hud.gameObject);
        }
    }

    static ResolvedAction Escape(bool success, int alert, int turnCost)
    {
        return new ResolvedAction
        {
            ActionID = "ACT_010",
            TurnCost = turnCost,
            Success = success,
            CandyDelta = 0,
            AlertDelta = alert,
            TurnDelta = 0,
            SuccessNextPhase = "RESULT",
            IsEscapeAction = true
        };
    }

    static string[] ActiveActionIds(V02RunHud hud)
    {
        var ids = new List<string>();
        var buttons = hud.GetComponentsInChildren<V02ChoiceButton>(true);
        for (int i = 0; i < buttons.Length; i++)
        {
            if (buttons[i].gameObject.activeInHierarchy && !buttons[i].IsOption)
                ids.Add(buttons[i].BoundId);
        }

        return ids.ToArray();
    }

    static bool TryHud(CsvDatabase database, IChanceRoll roll, out V02RunHud hud, out RunState run)
    {
        hud = null;
        run = null;
        V02RunHud prefab = Resources.Load<V02RunHud>(V02RunHud.ResourcePath);
        if (prefab == null || Resources.Load<V02RunResultPopup>(V02RunResultPopup.ResourcePath) == null)
            return false;
        if (!RunState.TryStart(database, "SHOP_001", out run))
            return false;
        hud = UnityEngine.Object.Instantiate(prefab);
        hud.Bind(database, run, null, roll, null, null, new FixedIntRoll(0));
        return true;
    }

#if UNITY_EDITOR
    [UnityEditor.MenuItem("Candy Thief/V02/Run Outcome Tests")]
    static void MenuRunAll()
    {
        CsvDatabase database = CsvDatabase.Current;
        if (database == null || !database.Loaded || database.HasErrors)
            database = CsvLoadRunner.LoadAndReport();
        if (database == null || database.HasErrors)
            return;
        RunAll(database);
    }
#endif
}
