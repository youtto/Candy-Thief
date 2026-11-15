using UnityEngine;

public static class RunStateSelfTest
{
    public static bool RunAll(CsvDatabase database)
    {
        if (database == null || !database.Loaded || database.HasErrors)
        {
            Debug.LogError(RunState.LogPrefix + "자가 검사 실패: CsvDatabase가 준비되지 않았습니다.");
            return false;
        }

        RunState previous = RunState.Current;
        int failed = 0;
        failed += Expect("1 SHOP_001 초기화", TestShop001Init(database));
        failed += Expect("2 SHOP_002/SHOP_003 초기화", TestShop002And003Init(database));
        failed += Expect("3 잘못된 ShopID", TestInvalidShopId(database));
        failed += Expect("4 턴 감소 및 잔여 턴 부족", TestTurnCostGate(database));
        failed += Expect("5 경계도 상한 실패", TestAlertFail(database));
        failed += Expect("6 턴 0 실패", TestNoTurnsFail(database));
        failed += Expect("7 마지막 턴 탈출 승리", TestLastTurnEscapeWin(database));
        failed += Expect("8 사탕 0 탈출은 빈손 탈출", TestEscapeWithoutCandy(database));
        failed += Expect("9 탈출 성공이 경계도 상한보다 우선", TestEscapeBeatsAlert(database));
        failed += Expect("10 종료 후 상태 변경 방지", TestNoMutateAfterEnd(database));
        failed += Expect("11 재시작 시 초기화", TestRestart(database));

        RunState.Current = previous;
        if (failed == 0)
        {
            Debug.Log(RunState.LogPrefix + "M1-3 자가 검사 11건 통과");
            return true;
        }

        Debug.LogError(RunState.LogPrefix + "M1-3 자가 검사 실패: " + failed + "건");
        return false;
    }

    static int Expect(string name, bool passed)
    {
        if (passed)
        {
            Debug.Log(RunState.LogPrefix + "PASS " + name);
            return 0;
        }

        Debug.LogError(RunState.LogPrefix + "FAIL " + name);
        return 1;
    }

    static bool TestShop001Init(CsvDatabase database)
    {
        ShopRow shop;
        RunState run;
        if (!database.TryGetShop("SHOP_001", out shop))
            return false;
        if (!RunState.TryStart(database, "SHOP_001", out run))
            return false;

        return run.ShopID == "SHOP_001"
            && run.Candy == 0
            && run.RemainingTurns == shop.StartTurn
            && run.Alert == shop.StartAlert
            && run.CurrentPhase == "MOVE"
            && run.Status == RunStatus.InProgress
            && run.LastActionResult == RunActionResult.None;
    }

    static bool TestShop002And003Init(CsvDatabase database)
    {
        ShopRow shop2;
        ShopRow shop3;
        RunState run2;
        RunState run3;
        if (!database.TryGetShop("SHOP_002", out shop2) || !database.TryGetShop("SHOP_003", out shop3))
            return false;
        if (!RunState.TryStart(database, "SHOP_002", out run2))
            return false;
        bool ok2 = run2.ShopID == "SHOP_002"
            && run2.RemainingTurns == shop2.StartTurn
            && run2.Alert == shop2.StartAlert
            && run2.Candy == 0
            && run2.Status == RunStatus.InProgress;
        if (!RunState.TryStart(database, "SHOP_003", out run3))
            return false;
        bool ok3 = run3.ShopID == "SHOP_003"
            && run3.RemainingTurns == shop3.StartTurn
            && run3.Alert == shop3.StartAlert
            && run3.Candy == 0;
        if (!ok2 || !ok3)
            return false;

        if (!RunState.TryStart(database, "SHOP_002", out run2))
            return false;
        return run2.ApplyCandyDelta(100) && run2.Candy == 100;
    }

    static bool TestInvalidShopId(CsvDatabase database)
    {
        RunState before;
        if (!RunState.TryStart(database, "SHOP_001", out before))
            return false;

        RunState after;
        bool started = RunState.TryStart(database, "SHOP_MISSING", out after);
        return !started && after == null && RunState.Current == before;
    }

    static bool TestTurnCostGate(CsvDatabase database)
    {
        ActionRow action;
        RunState run;
        if (!database.TryGetAction("ACT_001", out action))
            return false;
        if (!RunState.TryStart(database, "SHOP_001", out run))
            return false;
        if (!run.CanSelectAction(action))
            return false;

        int keep = action.TurnCost - 1;
        if (keep < 0)
            return false;
        if (!run.ApplyTurnDelta(-(run.RemainingTurns - keep)))
            return false;
        if (run.RemainingTurns != keep || run.Status != RunStatus.InProgress)
            return false;
        if (run.CanAffordTurnCost(action.TurnCost))
            return false;

        int turnsBefore = run.RemainingTurns;
        int candyBefore = run.Candy;
        bool applied = run.TryApplyAction(action, true);
        return !applied
            && run.RemainingTurns == turnsBefore
            && run.Candy == candyBefore
            && run.Status == RunStatus.InProgress;
    }

    static bool TestAlertFail(CsvDatabase database)
    {
        RunState run;
        if (!RunState.TryStart(database, "SHOP_001", out run))
            return false;

        int maxAlert = database.GetInt("MAX_ALERT");
        if (!run.ApplyAlertDelta(maxAlert - run.Alert))
            return false;
        return run.Status == RunStatus.Failed
            && run.FailReason == RunFailReason.AlertLimit
            && run.Alert == maxAlert;
    }

    static bool TestNoTurnsFail(CsvDatabase database)
    {
        RunState run;
        if (!RunState.TryStart(database, "SHOP_001", out run))
            return false;
        if (!run.ApplyTurnDelta(-run.RemainingTurns))
            return false;
        return run.RemainingTurns == 0
            && run.Status == RunStatus.Failed
            && run.FailReason == RunFailReason.NoTurns;
    }

    static bool TestLastTurnEscapeWin(CsvDatabase database)
    {
        ActionRow escape;
        RunState run;
        if (!database.TryGetAction("ACT_010", out escape))
            return false;
        if (!RunState.TryStart(database, "SHOP_001", out run))
            return false;
        if (!run.ApplyCandyDelta(database.GetInt("MIN_CANDY_TO_ESCAPE")))
            return false;
        if (!run.ApplyTurnDelta(-(run.RemainingTurns - escape.TurnCost)))
            return false;
        if (run.RemainingTurns != escape.TurnCost || run.Status != RunStatus.InProgress)
            return false;
        if (!run.TryApplyAction(escape, true))
            return false;
        return run.Status == RunStatus.Won
            && run.RemainingTurns == 0
            && run.Candy >= database.GetInt("MIN_CANDY_TO_ESCAPE")
            && run.CurrentPhase == escape.SuccessNextPhase;
    }

    static bool TestEscapeWithoutCandy(CsvDatabase database)
    {
        ActionRow escape;
        RunState run;
        if (!database.TryGetAction("ACT_010", out escape))
            return false;
        if (!RunState.TryStart(database, "SHOP_001", out run))
            return false;
        if (run.Candy != 0)
            return false;
        if (!run.TryApplyAction(escape, true))
            return false;
        return run.Status == RunStatus.Failed
            && run.FailReason == RunFailReason.EmptyEscape
            && run.Outcome == RunOutcome.EmptyEscape
            && run.Candy == 0
            && run.SettledCandy == 0
            && run.CurrentPhase == escape.SuccessNextPhase;
    }

    static bool TestEscapeBeatsAlert(CsvDatabase database)
    {
        RunState run;
        if (!RunState.TryStart(database, "SHOP_001", out run))
            return false;

        int minCandy = database.GetInt("MIN_CANDY_TO_ESCAPE");
        int maxAlert = database.GetInt("MAX_ALERT");
        if (!run.ApplyCandyDelta(minCandy))
            return false;

        var resolved = new ResolvedAction
        {
            ActionID = "ACT_010",
            TurnCost = 1,
            Success = true,
            CandyDelta = 0,
            AlertDelta = maxAlert - run.Alert,
            TurnDelta = 0,
            SuccessNextPhase = "RESULT",
            IsEscapeAction = true
        };
        if (!run.TryApplyResolved(resolved))
            return false;

        return run.Status == RunStatus.Won
            && run.FailReason == RunFailReason.None
            && run.Outcome == RunOutcome.OperationSuccess
            && run.Alert == maxAlert
            && run.SettledCandy >= minCandy;
    }

    static bool TestNoMutateAfterEnd(CsvDatabase database)
    {
        RunState run;
        if (!RunState.TryStart(database, "SHOP_001", out run))
            return false;
        if (!run.ApplyTurnDelta(-run.RemainingTurns))
            return false;
        if (run.Status != RunStatus.Failed)
            return false;

        int candy = run.Candy;
        int turns = run.RemainingTurns;
        int alert = run.Alert;
        string phase = run.CurrentPhase;
        bool mutated = run.ApplyCandyDelta(100)
            || run.ApplyTurnDelta(-1)
            || run.ApplyAlertDelta(1)
            || run.SetPhase("LOOT")
            || run.TryApplyAction("ACT_001", true);
        return !mutated
            && run.Candy == candy
            && run.RemainingTurns == turns
            && run.Alert == alert
            && run.CurrentPhase == phase
            && run.Status == RunStatus.Failed;
    }

    static bool TestRestart(CsvDatabase database)
    {
        ShopRow shop;
        RunState first;
        RunState second;
        if (!database.TryGetShop("SHOP_001", out shop))
            return false;
        if (!RunState.TryStart(database, "SHOP_001", out first))
            return false;
        if (!first.ApplyCandyDelta(100) || first.Candy != 100)
            return false;
        if (!RunState.TryStart(database, "SHOP_001", out second))
            return false;
        return second != first
            && second.Candy == 0
            && second.RemainingTurns == shop.StartTurn
            && second.Alert == shop.StartAlert
            && second.Status == RunStatus.InProgress
            && RunState.Current == second;
    }

#if UNITY_EDITOR
    [UnityEditor.MenuItem("Candy Thief/V02/Run State Tests")]
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
