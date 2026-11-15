using System.Collections.Generic;
using UnityEngine;

public static class ActionResolverSelfTest
{
    static readonly string[] GameplayTables =
    {
        "GameConfig",
        "ShopTable",
        "PhaseTable",
        "ActionTable",
        "EventTable",
        "EventOptionTable",
        "AchievementTable",
        "AssetTable",
        "EffectTable"
    };

    public static bool RunAll(CsvDatabase database)
    {
        if (database == null || !database.Loaded || database.HasErrors)
        {
            Debug.LogError(ActionResolver.LogPrefix + "자가 검사 실패: CsvDatabase가 준비되지 않았습니다.");
            return false;
        }

        RunState previous = RunState.Current;
        int failed = 0;
        failed += Expect("1 정상 행동 실행", TestNormalAction(database));
        failed += Expect("2 턴 부족 거부", TestInsufficientTurns(database));
        failed += Expect("3 성공/실패 결과", TestSuccessAndFail(database));
        failed += Expect("4 Shop 성공률 보정", TestShopBonus(database));
        failed += Expect("5 성공률 min/max 클램프", TestChanceClamp(database));
        failed += Expect("6 사탕/경계도 증감", TestCandyAndAlert(database));
        failed += Expect("7 경계도 실패", TestAlertFail(database));
        failed += Expect("8 탈출 성공 승리", TestEscapeWin(database));
        failed += Expect("9 턴 소진 실패", TestNoTurnsFail(database));
        failed += Expect("10 탈출 성공은 경계도 상한보다 우선", TestAlertBeatsEscape(database));
        failed += Expect("11 CSV 수치 변경 반영", TestCsvDrivesResult(database));
        failed += Expect("12 실패 후 동일 필수 행동 재시도 성공", TestRetrySameRequiredAfterFail(database));
        failed += Expect("13 실패 후 같은 페이즈 다른 필수 행동 선택", TestRetrySiblingRequiredAfterFail(database));
        failed += Expect("14 성공 후 이전 페이즈 행동 선택 불가", TestPreviousPhaseLockedAfterSuccess(database));
        failed += Expect("15 Repeatable=0 실패 후 재시도 성공", TestNotRepeatableFailAllowsRetry(database));
        failed += Expect("16 Repeatable=0 성공 후 재선택 불가", TestNotRepeatableSuccessBlocksReselect(database));
        failed += Expect("17 Repeatable=1 성공 후 재선택 가능", TestRepeatableSuccessAllowsReselect(database));
        failed += Expect("18 실행 거부는 성공 이력에 기록되지 않음", TestRejectedActionNotSucceeded(database));
        failed += Expect("19 런 종료 후 선택 목록 비어 있음", TestEndedRunAvailableEmpty(database));
        failed += Expect("20 런 종료 후 행동 실행 거부", TestEndedRunRejectsResolve(database));
        failed += Expect("21 선택 목록과 실행 조건 일치", TestAvailableMatchesEvaluateSelect(database));
        failed += Expect("22 새 런 시작 시 성공 이력 초기화", TestSuccessHistoryResetsOnNewRun(database));
        failed += Expect("23 턴 0과 경계도 상한 동시 발생은 경계도 실패", TestAlertBeatsNoTurns(database));
        failed += Expect("24 성공해도 턴 0이면 실패하고 페이즈 유지", TestSuccessNoTurnsKeepsPhase(database));
        failed += Expect("25 성공해도 경계도 상한이면 실패하고 페이즈 유지", TestSuccessAlertLimitKeepsPhase(database));
        // 26–32: M1-6 end checks / D-014. EventEligible and EventCheckDue live-path coverage is M1-7.
        failed += Expect("26 마지막 턴 탈출 성공 승리", TestLastTurnEscapeWin(database));
        failed += Expect("27 마지막 턴 탈출 실패는 턴 0 실패", TestLastTurnEscapeFail(database));
        failed += Expect("28 사탕 0 마지막 턴 탈출 성공은 빈손 탈출", TestLastTurnEscapeSuccessWithoutCandyFails(database));
        failed += Expect("29 탈출 조건 충족만으로 자동 성공하지 않음", TestEscapeConditionsDoNotAutoSucceed(database));
        failed += Expect("30 탈출 행동도 동일 성공률 공식", TestEscapeUsesSameSuccessChanceFormula(database));
        failed += Expect("31 턴 델타는 비용 차감 후 종료 판정 전", TestTurnDeltasApplyBeforeEndCheck(database));
        failed += Expect("32 ACT_013 성공은 ESCAPE 전환이지 승리가 아님", TestAct013SuccessMovesToEscapeDoesNotWin(database));

        RunState.Current = previous;
        if (failed == 0)
        {
            Debug.Log(ActionResolver.LogPrefix + "자가 검사 32건 통과 (M1-4 11 + M1-5 11 + M1-6 10)");
            return true;
        }

        Debug.LogError(ActionResolver.LogPrefix + "자가 검사 실패: " + failed + "건");
        return false;
    }

    static int Expect(string name, bool passed)
    {
        if (passed)
        {
            Debug.Log(ActionResolver.LogPrefix + "PASS " + name);
            return 0;
        }

        Debug.LogError(ActionResolver.LogPrefix + "FAIL " + name);
        return 1;
    }

    static bool TestNormalAction(CsvDatabase database)
    {
        RunState run;
        ActionRow action;
        ShopRow shop;
        if (!database.TryGetAction("ACT_001", out action) || !database.TryGetShop("SHOP_001", out shop))
            return false;
        if (!RunState.TryStart(database, "SHOP_001", out run))
            return false;

        var resolver = new ActionResolver(database, new FixedChanceRoll(true));
        ActionResolveResult result;
        if (!resolver.TryResolve(run, "ACT_001", out result) || !result.Applied || !result.Success)
            return false;

        return run.Status == RunStatus.InProgress
            && run.CurrentPhase == action.SuccessNextPhase
            && run.RemainingTurns == shop.StartTurn - action.TurnCost
            && run.LastActionID == "ACT_001"
            && run.LastActionResult == RunActionResult.Success
            && result.EventCheckDue
            && result.SuccessChance == resolver.ComputeSuccessChance(action, shop);
    }

    static bool TestInsufficientTurns(CsvDatabase database)
    {
        RunState run;
        ActionRow action;
        if (!database.TryGetAction("ACT_001", out action))
            return false;
        if (!RunState.TryStart(database, "SHOP_001", out run))
            return false;

        int keep = action.TurnCost - 1;
        if (keep < 0 || !run.ApplyTurnDelta(-(run.RemainingTurns - keep)))
            return false;

        int turns = run.RemainingTurns;
        int candy = run.Candy;
        int alert = run.Alert;
        string phase = run.CurrentPhase;
        var resolver = new ActionResolver(database, new FixedChanceRoll(true));
        ActionResolveResult result;
        bool applied = resolver.TryResolve(run, "ACT_001", out result);
        return !applied
            && !result.Applied
            && result.RejectReason == ActionRejectReason.InsufficientTurns
            && run.RemainingTurns == turns
            && run.Candy == candy
            && run.Alert == alert
            && run.CurrentPhase == phase
            && run.Status == RunStatus.InProgress
            && run.LastActionResult == RunActionResult.None;
    }

    static bool TestSuccessAndFail(CsvDatabase database)
    {
        RunState successRun;
        RunState failRun;
        ActionRow loot;
        ShopRow shop;
        if (!database.TryGetAction("ACT_008", out loot) || !database.TryGetShop("SHOP_001", out shop))
            return false;
        if (!RunState.TryStart(database, "SHOP_001", out successRun) || !successRun.SetPhase("LOOT"))
            return false;

        var winResolver = new ActionResolver(database, new FixedChanceRoll(true));
        int chance = winResolver.ComputeSuccessChance(loot, shop);
        if (chance <= 0 || chance >= 100)
            return false;

        ActionResolveResult win;
        if (!winResolver.TryResolve(successRun, "ACT_008", out win) || !win.Success)
            return false;
        if (successRun.Candy != loot.SuccessCandy || successRun.Alert != loot.SuccessAlert)
            return false;
        if (successRun.CurrentPhase != loot.SuccessNextPhase)
            return false;

        if (!RunState.TryStart(database, "SHOP_001", out failRun) || !failRun.SetPhase("LOOT"))
            return false;
        var failResolver = new ActionResolver(database, new FixedChanceRoll(false));
        ActionResolveResult fail;
        if (!failResolver.TryResolve(failRun, "ACT_008", out fail) || fail.Success)
            return false;
        return failRun.Candy == loot.FailCandy
            && failRun.Alert == shop.StartAlert + loot.FailAlert
            && failRun.CurrentPhase == "LOOT"
            && failRun.Status == RunStatus.InProgress
            && failRun.LastActionResult == RunActionResult.Fail;
    }

    static bool TestShopBonus(CsvDatabase database)
    {
        ActionRow action;
        ShopRow shop1;
        ShopRow shop3;
        if (!database.TryGetAction("ACT_003", out action)
            || !database.TryGetShop("SHOP_001", out shop1)
            || !database.TryGetShop("SHOP_003", out shop3))
            return false;

        var resolver = new ActionResolver(database, new FixedChanceRoll(true));
        int chance1 = resolver.ComputeSuccessChance(action, shop1);
        int chance3 = resolver.ComputeSuccessChance(action, shop3);
        return chance1 == action.SuccessRate + shop1.SuccessRateBonusPp
            && chance3 == action.SuccessRate + shop3.SuccessRateBonusPp
            && chance1 != chance3;
    }

    static bool TestChanceClamp(CsvDatabase database)
    {
        CsvDatabase high = PatchAction(database, "ACT_003", "SuccessRate", "1000");
        CsvDatabase low = PatchAction(database, "ACT_003", "SuccessRate", "-1000");
        if (high == null || high.HasErrors || low == null || low.HasErrors)
            return false;

        ActionRow highAction;
        ActionRow lowAction;
        ShopRow shop;
        if (!high.TryGetAction("ACT_003", out highAction)
            || !low.TryGetAction("ACT_003", out lowAction)
            || !database.TryGetShop("SHOP_001", out shop))
            return false;

        int max = database.GetInt("MAX_SUCCESS_RATE");
        int min = database.GetInt("MIN_SUCCESS_RATE");
        var highResolver = new ActionResolver(high, new FixedChanceRoll(false));
        var lowResolver = new ActionResolver(low, new FixedChanceRoll(true));
        return highResolver.ComputeSuccessChance(highAction, shop) == max
            && lowResolver.ComputeSuccessChance(lowAction, shop) == min;
    }

    static bool TestCandyAndAlert(CsvDatabase database)
    {
        RunState lootRun;
        RunState moveRun;
        ActionRow loot;
        ActionRow risky;
        if (!database.TryGetAction("ACT_007", out loot) || !database.TryGetAction("ACT_003", out risky))
            return false;
        if (!RunState.TryStart(database, "SHOP_001", out lootRun) || !lootRun.SetPhase("LOOT"))
            return false;
        var lootResolver = new ActionResolver(database, new FixedChanceRoll(true));
        ActionResolveResult lootResult;
        if (!lootResolver.TryResolve(lootRun, "ACT_007", out lootResult))
            return false;
        if (lootRun.Candy != loot.SuccessCandy || lootRun.Alert != loot.SuccessAlert)
            return false;

        if (!RunState.TryStart(database, "SHOP_001", out moveRun))
            return false;
        var failResolver = new ActionResolver(database, new FixedChanceRoll(false));
        ActionResolveResult fail;
        if (!failResolver.TryResolve(moveRun, "ACT_003", out fail) || fail.Success)
            return false;
        return moveRun.Candy == risky.FailCandy && moveRun.Alert == risky.FailAlert;
    }

    static bool TestAlertFail(CsvDatabase database)
    {
        RunState run;
        ActionRow action;
        if (!database.TryGetAction("ACT_003", out action))
            return false;
        if (!RunState.TryStart(database, "SHOP_001", out run))
            return false;

        int maxAlert = database.GetInt("MAX_ALERT");
        int raise = maxAlert - action.FailAlert - run.Alert;
        if (raise < 0 || !run.ApplyAlertDelta(raise))
            return false;

        var resolver = new ActionResolver(database, new FixedChanceRoll(false));
        ActionResolveResult result;
        if (!resolver.TryResolve(run, "ACT_003", out result) || result.Success)
            return false;
        return run.Status == RunStatus.Failed
            && run.FailReason == RunFailReason.AlertLimit
            && run.Alert == maxAlert
            && !result.EventCheckDue;
    }

    static bool TestEscapeWin(CsvDatabase database)
    {
        RunState run;
        ActionRow escape;
        if (!database.TryGetAction("ACT_010", out escape))
            return false;
        if (!RunState.TryStart(database, "SHOP_001", out run))
            return false;
        if (!run.ApplyCandyDelta(database.GetInt("MIN_CANDY_TO_ESCAPE")))
            return false;
        if (!run.SetPhase("ESCAPE"))
            return false;

        var resolver = new ActionResolver(database, new FixedChanceRoll(true));
        ActionResolveResult result;
        if (!resolver.TryResolve(run, "ACT_010", out result) || !result.Success)
            return false;
        return run.Status == RunStatus.Won
            && run.CurrentPhase == escape.SuccessNextPhase
            && !result.EventCheckDue;
    }

    static bool TestNoTurnsFail(CsvDatabase database)
    {
        RunState run;
        ActionRow action;
        if (!database.TryGetAction("ACT_003", out action))
            return false;
        if (!RunState.TryStart(database, "SHOP_001", out run))
            return false;
        if (!run.ApplyTurnDelta(-(run.RemainingTurns - action.TurnCost)))
            return false;

        var resolver = new ActionResolver(database, new FixedChanceRoll(false));
        ActionResolveResult result;
        if (!resolver.TryResolve(run, "ACT_003", out result) || result.Success)
            return false;
        return run.RemainingTurns == 0
            && run.Status == RunStatus.Failed
            && run.FailReason == RunFailReason.NoTurns
            && !result.EventCheckDue;
    }

    static bool TestAlertBeatsEscape(CsvDatabase database)
    {
        CsvDatabase patched = PatchAction(database, "ACT_010", "SuccessAlert", "100");
        if (patched == null || patched.HasErrors)
            return false;

        RunState run;
        ActionRow escape;
        if (!patched.TryGetAction("ACT_010", out escape))
            return false;
        if (!RunState.TryStart(patched, "SHOP_001", out run))
            return false;
        if (!run.ApplyCandyDelta(patched.GetInt("MIN_CANDY_TO_ESCAPE")) || !run.SetPhase("ESCAPE"))
            return false;

        var resolver = new ActionResolver(patched, new FixedChanceRoll(true));
        ActionResolveResult result;
        if (!resolver.TryResolve(run, "ACT_010", out result) || !result.Success)
            return false;
        return run.Status == RunStatus.Won
            && run.FailReason == RunFailReason.None
            && run.Outcome == RunOutcome.OperationSuccess
            && run.Alert == patched.GetInt("MAX_ALERT")
            && run.CurrentPhase == escape.SuccessNextPhase
            && !result.EventCheckDue;
    }

    static bool TestCsvDrivesResult(CsvDatabase database)
    {
        CsvDatabase zero = PatchAction(database, "ACT_001", "SuccessRate", "0");
        if (zero == null || zero.HasErrors)
            return false;

        ActionRow original;
        ActionRow patched;
        ShopRow shop;
        if (!database.TryGetAction("ACT_001", out original)
            || !zero.TryGetAction("ACT_001", out patched)
            || !database.TryGetShop("SHOP_002", out shop))
            return false;

        var liveResolver = new ActionResolver(database, new FixedChanceRoll(true));
        var patchedResolver = new ActionResolver(zero, new FixedChanceRoll(true));
        int liveChance = liveResolver.ComputeSuccessChance(original, shop);
        int patchedChance = patchedResolver.ComputeSuccessChance(patched, shop);
        if (liveChance == patchedChance || patched.SuccessRate != 0)
            return false;

        RunState liveRun;
        RunState patchedRun;
        if (!RunState.TryStart(database, "SHOP_002", out liveRun))
            return false;
        if (!RunState.TryStart(zero, "SHOP_002", out patchedRun))
            return false;

        ActionResolveResult liveResult;
        ActionResolveResult patchedResult;
        if (!liveResolver.TryResolve(liveRun, "ACT_001", out liveResult))
            return false;
        if (!patchedResolver.TryResolve(patchedRun, "ACT_001", out patchedResult))
            return false;

        bool patchedAlwaysFails = patchedChance <= 0;
        return liveResult.Success
            && liveRun.CurrentPhase == original.SuccessNextPhase
            && patchedAlwaysFails
            && !patchedResult.Success
            && patchedRun.CurrentPhase == "MOVE"
            && patchedRun.Alert == shop.StartAlert + patched.FailAlert;
    }

    static bool TestRetrySameRequiredAfterFail(CsvDatabase database)
    {
        RunState run;
        ActionRow action;
        if (!database.TryGetAction("ACT_003", out action) || action.Repeatable != true)
            return false;
        if (!RunState.TryStart(database, "SHOP_001", out run))
            return false;

        var failResolver = new ActionResolver(database, new FixedChanceRoll(false));
        ActionResolveResult fail;
        if (!failResolver.TryResolve(run, "ACT_003", out fail) || fail.Success)
            return false;
        if (run.CurrentPhase != "MOVE" || run.Status != RunStatus.InProgress)
            return false;
        if (!failResolver.CanSelect(run, "ACT_003") || run.HasSucceededAction("ACT_003"))
            return false;

        var winResolver = new ActionResolver(database, new FixedChanceRoll(true));
        ActionResolveResult win;
        if (!winResolver.TryResolve(run, "ACT_003", out win) || !win.Success)
            return false;
        return run.CurrentPhase == action.SuccessNextPhase
            && run.LastActionResult == RunActionResult.Success
            && run.HasSucceededAction("ACT_003");
    }

    static bool TestRetrySiblingRequiredAfterFail(CsvDatabase database)
    {
        RunState run;
        if (!RunState.TryStart(database, "SHOP_001", out run))
            return false;

        var failResolver = new ActionResolver(database, new FixedChanceRoll(false));
        ActionResolveResult fail;
        if (!failResolver.TryResolve(run, "ACT_003", out fail) || fail.Success)
            return false;

        List<ActionRow> available = CollectAvailable(failResolver, run);
        return run.CurrentPhase == "MOVE"
            && failResolver.CanSelect(run, "ACT_001")
            && failResolver.CanSelect(run, "ACT_002")
            && ContainsActionId(available, "ACT_001")
            && ContainsActionId(available, "ACT_002")
            && ContainsActionId(available, "ACT_003");
    }

    static bool TestPreviousPhaseLockedAfterSuccess(CsvDatabase database)
    {
        RunState run;
        if (!RunState.TryStart(database, "SHOP_001", out run))
            return false;

        var resolver = new ActionResolver(database, new FixedChanceRoll(true));
        ActionResolveResult result;
        if (!resolver.TryResolve(run, "ACT_001", out result) || !result.Success)
            return false;

        ActionRow action;
        ActionRejectReason reason = resolver.EvaluateSelect(run, "ACT_001", out action);
        List<ActionRow> available = CollectAvailable(resolver, run);
        return run.CurrentPhase == "INFILTRATE"
            && run.HasSucceededAction("ACT_001")
            && reason == ActionRejectReason.WrongPhase
            && reason != ActionRejectReason.NotRepeatable
            && !ContainsActionId(available, "ACT_001")
            && !resolver.CanSelect(run, "ACT_002")
            && !resolver.CanSelect(run, "ACT_003");
    }

    static bool TestNotRepeatableFailAllowsRetry(CsvDatabase database)
    {
        CsvDatabase patched = PatchAction(database, "ACT_003", "Repeatable", "0");
        if (patched == null || patched.HasErrors)
            return false;

        ActionRow action;
        if (!patched.TryGetAction("ACT_003", out action) || action.Repeatable)
            return false;

        RunState run;
        if (!RunState.TryStart(patched, "SHOP_001", out run))
            return false;

        var failResolver = new ActionResolver(patched, new FixedChanceRoll(false));
        ActionResolveResult fail;
        if (!failResolver.TryResolve(run, "ACT_003", out fail) || fail.Success)
            return false;

        List<ActionRow> available = CollectAvailable(failResolver, run);
        if (run.HasSucceededAction("ACT_003")
            || !failResolver.CanSelect(run, "ACT_003")
            || !ContainsActionId(available, "ACT_003")
            || run.CurrentPhase != "MOVE")
            return false;

        var winResolver = new ActionResolver(patched, new FixedChanceRoll(true));
        ActionResolveResult win;
        if (!winResolver.TryResolve(run, "ACT_003", out win) || !win.Success)
            return false;
        return run.CurrentPhase == action.SuccessNextPhase
            && run.HasSucceededAction("ACT_003");
    }

    static bool TestNotRepeatableSuccessBlocksReselect(CsvDatabase database)
    {
        CsvDatabase patched = PatchAction(database, "ACT_008", "Repeatable", "0");
        if (patched == null || patched.HasErrors)
            return false;

        ActionRow action;
        ShopRow shop;
        if (!patched.TryGetAction("ACT_008", out action)
            || action.Repeatable
            || action.SuccessNextPhase != "LOOT"
            || !patched.TryGetShop("SHOP_001", out shop))
            return false;

        RunState run;
        if (!RunState.TryStart(patched, "SHOP_001", out run) || !run.SetPhase("LOOT"))
            return false;

        var resolver = new ActionResolver(patched, new FixedChanceRoll(true));
        int chance = resolver.ComputeSuccessChance(action, shop);
        if (chance <= 0 || chance >= 100)
            return false;

        ActionResolveResult win;
        if (!resolver.TryResolve(run, "ACT_008", out win) || !win.Success)
            return false;

        int turns = run.RemainingTurns;
        int alert = run.Alert;
        int candy = run.Candy;
        ActionRow ignored;
        ActionRejectReason reason = resolver.EvaluateSelect(run, "ACT_008", out ignored);
        ActionResolveResult again;
        bool applied = resolver.TryResolve(run, "ACT_008", out again);
        List<ActionRow> available = CollectAvailable(resolver, run);
        return run.CurrentPhase == "LOOT"
            && run.HasSucceededAction("ACT_008")
            && reason == ActionRejectReason.NotRepeatable
            && !applied
            && !again.Applied
            && again.RejectReason == ActionRejectReason.NotRepeatable
            && !ContainsActionId(available, "ACT_008")
            && resolver.CanSelect(run, "ACT_007")
            && resolver.CanSelect(run, "ACT_009")
            && run.RemainingTurns == turns
            && run.Alert == alert
            && run.Candy == candy;
    }

    static bool TestRepeatableSuccessAllowsReselect(CsvDatabase database)
    {
        ActionRow action;
        ShopRow shop;
        if (!database.TryGetAction("ACT_008", out action)
            || !action.Repeatable
            || action.SuccessNextPhase != "LOOT"
            || !database.TryGetShop("SHOP_001", out shop))
            return false;

        RunState run;
        if (!RunState.TryStart(database, "SHOP_001", out run) || !run.SetPhase("LOOT"))
            return false;

        var resolver = new ActionResolver(database, new FixedChanceRoll(true));
        int chance = resolver.ComputeSuccessChance(action, shop);
        if (chance <= 0 || chance >= 100)
            return false;

        ActionResolveResult win;
        if (!resolver.TryResolve(run, "ACT_008", out win) || !win.Success)
            return false;

        List<ActionRow> available = CollectAvailable(resolver, run);
        ActionRow ignored;
        return run.CurrentPhase == "LOOT"
            && run.HasSucceededAction("ACT_008")
            && resolver.CanSelect(run, "ACT_008")
            && ContainsActionId(available, "ACT_008")
            && resolver.EvaluateSelect(run, "ACT_008", out ignored) == ActionRejectReason.None;
    }

    static bool TestRejectedActionNotSucceeded(CsvDatabase database)
    {
        RunState run;
        ActionRow action;
        if (!database.TryGetAction("ACT_001", out action))
            return false;
        if (!RunState.TryStart(database, "SHOP_001", out run))
            return false;

        int keep = action.TurnCost - 1;
        if (keep < 0 || !run.ApplyTurnDelta(-(run.RemainingTurns - keep)))
            return false;

        var resolver = new ActionResolver(database, new FixedChanceRoll(true));
        ActionResolveResult turnsReject;
        bool turnsApplied = resolver.TryResolve(run, "ACT_001", out turnsReject);
        ActionResolveResult phaseReject;
        bool phaseApplied = resolver.TryResolve(run, "ACT_010", out phaseReject);
        return !turnsApplied
            && turnsReject.RejectReason == ActionRejectReason.InsufficientTurns
            && !phaseApplied
            && phaseReject.RejectReason == ActionRejectReason.WrongPhase
            && !run.HasSucceededAction("ACT_001")
            && !run.HasSucceededAction("ACT_010")
            && run.LastActionResult == RunActionResult.None;
    }

    static bool TestEndedRunAvailableEmpty(CsvDatabase database)
    {
        RunState run;
        ActionRow action;
        if (!database.TryGetAction("ACT_003", out action))
            return false;
        if (!RunState.TryStart(database, "SHOP_001", out run))
            return false;

        int maxAlert = database.GetInt("MAX_ALERT");
        int raise = maxAlert - action.FailAlert - run.Alert;
        if (raise < 0 || !run.ApplyAlertDelta(raise))
            return false;

        var resolver = new ActionResolver(database, new FixedChanceRoll(false));
        ActionResolveResult result;
        if (!resolver.TryResolve(run, "ACT_003", out result) || result.Success)
            return false;
        if (run.Status != RunStatus.Failed)
            return false;

        List<ActionRow> available = CollectAvailable(resolver, run);
        return available.Count == 0;
    }

    static bool TestEndedRunRejectsResolve(CsvDatabase database)
    {
        RunState run;
        ActionRow action;
        if (!database.TryGetAction("ACT_003", out action))
            return false;
        if (!RunState.TryStart(database, "SHOP_001", out run))
            return false;

        int maxAlert = database.GetInt("MAX_ALERT");
        int raise = maxAlert - action.FailAlert - run.Alert;
        if (raise < 0 || !run.ApplyAlertDelta(raise))
            return false;

        var resolver = new ActionResolver(database, new FixedChanceRoll(false));
        ActionResolveResult fail;
        if (!resolver.TryResolve(run, "ACT_003", out fail) || run.Status != RunStatus.Failed)
            return false;

        int turns = run.RemainingTurns;
        int alert = run.Alert;
        string phase = run.CurrentPhase;
        RunActionResult last = run.LastActionResult;
        ActionResolveResult again;
        bool applied = resolver.TryResolve(run, "ACT_001", out again);
        return !applied
            && !again.Applied
            && again.RejectReason == ActionRejectReason.Ended
            && run.RemainingTurns == turns
            && run.Alert == alert
            && run.CurrentPhase == phase
            && run.LastActionResult == last
            && run.Status == RunStatus.Failed;
    }

    static bool TestAvailableMatchesEvaluateSelect(CsvDatabase database)
    {
        RunState run;
        if (!RunState.TryStart(database, "SHOP_001", out run))
            return false;

        var resolver = new ActionResolver(database, new FixedChanceRoll(true));
        List<ActionRow> available = CollectAvailable(resolver, run);
        var enabled = database.EnabledActions;
        for (int i = 0; i < enabled.Count; i++)
        {
            ActionRow row;
            ActionRejectReason reason = resolver.EvaluateSelect(run, enabled[i].ActionID, out row);
            bool listed = ContainsActionId(available, enabled[i].ActionID);
            if (listed != (reason == ActionRejectReason.None))
                return false;
            if (listed && !resolver.CanSelect(run, enabled[i].ActionID))
                return false;
        }

        if (available.Count == 0)
            return false;

        int turns = run.RemainingTurns;
        int alert = run.Alert;
        ActionResolveResult blocked;
        bool applied = resolver.TryResolve(run, "ACT_010", out blocked);
        return !applied
            && blocked.RejectReason == ActionRejectReason.WrongPhase
            && run.RemainingTurns == turns
            && run.Alert == alert
            && run.CurrentPhase == "MOVE"
            && ContainsActionId(available, "ACT_001")
            && ContainsActionId(available, "ACT_002")
            && ContainsActionId(available, "ACT_003")
            && !ContainsActionId(available, "ACT_013");
    }

    static bool TestSuccessHistoryResetsOnNewRun(CsvDatabase database)
    {
        CsvDatabase patched = PatchAction(database, "ACT_008", "Repeatable", "0");
        if (patched == null || patched.HasErrors)
            return false;

        ActionRow action;
        if (!patched.TryGetAction("ACT_008", out action) || action.Repeatable)
            return false;

        RunState first;
        if (!RunState.TryStart(patched, "SHOP_001", out first) || !first.SetPhase("LOOT"))
            return false;

        var resolver = new ActionResolver(patched, new FixedChanceRoll(true));
        ActionResolveResult win;
        if (!resolver.TryResolve(first, "ACT_008", out win) || !win.Success)
            return false;
        if (!first.HasSucceededAction("ACT_008") || resolver.CanSelect(first, "ACT_008"))
            return false;
        if (first.CurrentPhase != "LOOT")
            return false;

        RunState second;
        if (!RunState.TryStart(patched, "SHOP_001", out second) || !second.SetPhase("LOOT"))
            return false;
        return second != first
            && !second.HasSucceededAction("ACT_008")
            && resolver.CanSelect(second, "ACT_008")
            && ContainsActionId(CollectAvailable(resolver, second), "ACT_008");
    }

    static bool TestAlertBeatsNoTurns(CsvDatabase database)
    {
        RunState run;
        ActionRow action;
        if (!database.TryGetAction("ACT_003", out action))
            return false;
        if (!RunState.TryStart(database, "SHOP_001", out run))
            return false;

        int maxAlert = database.GetInt("MAX_ALERT");
        int raise = maxAlert - action.FailAlert - run.Alert;
        if (raise < 0 || !run.ApplyAlertDelta(raise))
            return false;
        if (!run.ApplyTurnDelta(-(run.RemainingTurns - action.TurnCost)))
            return false;
        if (run.Status != RunStatus.InProgress)
            return false;

        var resolver = new ActionResolver(database, new FixedChanceRoll(false));
        ActionResolveResult result;
        if (!resolver.TryResolve(run, "ACT_003", out result) || result.Success)
            return false;
        return run.RemainingTurns == 0
            && run.Alert == maxAlert
            && run.Status == RunStatus.Failed
            && run.FailReason == RunFailReason.AlertLimit
            && !result.EventCheckDue;
    }

    static bool TestSuccessNoTurnsKeepsPhase(CsvDatabase database)
    {
        RunState run;
        ActionRow action;
        if (!database.TryGetAction("ACT_001", out action) || action.SuccessNextPhase != "INFILTRATE")
            return false;
        if (!RunState.TryStart(database, "SHOP_001", out run))
            return false;
        if (!run.ApplyTurnDelta(-(run.RemainingTurns - action.TurnCost)))
            return false;

        var resolver = new ActionResolver(database, new FixedChanceRoll(true));
        ActionResolveResult result;
        if (!resolver.TryResolve(run, "ACT_001", out result) || !result.Success)
            return false;
        if (run.RemainingTurns != 0
            || run.Status != RunStatus.Failed
            || run.FailReason != RunFailReason.NoTurns
            || run.CurrentPhase != "MOVE"
            || run.LastActionResult != RunActionResult.Success
            || result.EventCheckDue)
            return false;

        return EndedRunRejectsFurtherPlay(resolver, run);
    }

    static bool TestSuccessAlertLimitKeepsPhase(CsvDatabase database)
    {
        CsvDatabase patched = PatchAction(database, "ACT_001", "SuccessAlert", "100");
        if (patched == null || patched.HasErrors)
            return false;

        ActionRow action;
        if (!patched.TryGetAction("ACT_001", out action)
            || action.SuccessNextPhase != "INFILTRATE")
            return false;

        RunState run;
        if (!RunState.TryStart(patched, "SHOP_001", out run))
            return false;

        var resolver = new ActionResolver(patched, new FixedChanceRoll(true));
        ActionResolveResult result;
        if (!resolver.TryResolve(run, "ACT_001", out result) || !result.Success)
            return false;
        return run.Status == RunStatus.Failed
            && run.FailReason == RunFailReason.AlertLimit
            && run.Alert == patched.GetInt("MAX_ALERT")
            && run.CurrentPhase == "MOVE"
            && run.LastActionResult == RunActionResult.Success
            && !result.EventCheckDue
            && EndedRunRejectsFurtherPlay(resolver, run);
    }

    static bool TestLastTurnEscapeWin(CsvDatabase database)
    {
        RunState run;
        ActionRow escape;
        if (!TryStartEscapeRun(database, "SHOP_001", true, true, out run, out escape))
            return false;

        var resolver = new ActionResolver(database, new FixedChanceRoll(true));
        ActionResolveResult result;
        if (!resolver.TryResolve(run, escape.ActionID, out result) || !result.Success)
            return false;
        return run.RemainingTurns == 0
            && run.Status == RunStatus.Won
            && run.FailReason == RunFailReason.None
            && run.CurrentPhase == escape.SuccessNextPhase
            && run.LastActionResult == RunActionResult.Success
            && EndedRunRejectsFurtherPlay(resolver, run);
    }

    static bool TestLastTurnEscapeFail(CsvDatabase database)
    {
        RunState run;
        ActionRow escape;
        ShopRow shop;
        if (!TryStartEscapeRun(database, "SHOP_001", true, true, out run, out escape))
            return false;
        if (!database.TryGetShop("SHOP_001", out shop))
            return false;

        var resolver = new ActionResolver(database, new FixedChanceRoll(false));
        int chance = resolver.ComputeSuccessChance(escape, shop);
        if (chance <= 0 || chance >= 100)
            return false;

        ActionResolveResult result;
        if (!resolver.TryResolve(run, escape.ActionID, out result) || result.Success)
            return false;
        return run.RemainingTurns == 0
            && run.Status == RunStatus.Failed
            && run.FailReason == RunFailReason.NoTurns
            && run.CurrentPhase == "ESCAPE"
            && run.LastActionResult == RunActionResult.Fail
            && EndedRunRejectsFurtherPlay(resolver, run);
    }

    static bool TestLastTurnEscapeSuccessWithoutCandyFails(CsvDatabase database)
    {
        RunState run;
        ActionRow escape;
        if (!TryStartEscapeRun(database, "SHOP_001", false, true, out run, out escape))
            return false;
        if (run.Candy != 0)
            return false;

        var resolver = new ActionResolver(database, new FixedChanceRoll(true));
        ActionResolveResult result;
        if (!resolver.TryResolve(run, escape.ActionID, out result) || !result.Success)
            return false;
        return run.RemainingTurns == 0
            && run.Candy == 0
            && run.Status == RunStatus.Failed
            && run.FailReason == RunFailReason.EmptyEscape
            && run.Outcome == RunOutcome.EmptyEscape
            && run.CurrentPhase == escape.SuccessNextPhase
            && run.LastActionResult == RunActionResult.Success
            && run.SettledCandy == 0
            && EndedRunRejectsFurtherPlay(resolver, run);
    }

    static bool TestEscapeConditionsDoNotAutoSucceed(CsvDatabase database)
    {
        RunState run;
        ActionRow escape;
        ShopRow shop;
        if (!TryStartEscapeRun(database, "SHOP_001", true, false, out run, out escape))
            return false;
        if (!database.TryGetShop("SHOP_001", out shop))
            return false;
        if (run.RemainingTurns <= escape.TurnCost)
            return false;

        int turns = run.RemainingTurns;
        var resolver = new ActionResolver(database, new FixedChanceRoll(false));
        int chance = resolver.ComputeSuccessChance(escape, shop);
        if (chance <= 0 || chance >= 100)
            return false;

        ActionResolveResult result;
        if (!resolver.TryResolve(run, escape.ActionID, out result) || result.Success)
            return false;
        return run.Status == RunStatus.InProgress
            && run.CurrentPhase == "ESCAPE"
            && run.RemainingTurns == turns - escape.TurnCost
            && run.LastActionResult == RunActionResult.Fail
            && resolver.CanSelect(run, escape.ActionID);
    }

    static bool TestEscapeUsesSameSuccessChanceFormula(CsvDatabase database)
    {
        ShopRow shop1;
        ShopRow shop3;
        ActionRow escapeFront;
        ActionRow escapeWall;
        ActionRow escapeSewer;
        ActionRow prepare;
        if (!database.TryGetShop("SHOP_001", out shop1)
            || !database.TryGetShop("SHOP_003", out shop3)
            || !database.TryGetAction("ACT_010", out escapeFront)
            || !database.TryGetAction("ACT_011", out escapeWall)
            || !database.TryGetAction("ACT_012", out escapeSewer)
            || !database.TryGetAction("ACT_013", out prepare))
            return false;

        var resolver = new ActionResolver(database, new FixedChanceRoll(true));
        int min = database.GetInt("MIN_SUCCESS_RATE");
        int max = database.GetInt("MAX_SUCCESS_RATE");
        return MatchesClampedChance(resolver, escapeFront, shop1, min, max)
            && MatchesClampedChance(resolver, escapeFront, shop3, min, max)
            && MatchesClampedChance(resolver, escapeWall, shop1, min, max)
            && MatchesClampedChance(resolver, escapeSewer, shop1, min, max)
            && MatchesClampedChance(resolver, prepare, shop1, min, max)
            && MatchesClampedChance(resolver, prepare, shop3, min, max)
            && resolver.ComputeSuccessChance(escapeFront, shop1)
                != resolver.ComputeSuccessChance(escapeFront, shop3);
    }

    static bool TestTurnDeltasApplyBeforeEndCheck(CsvDatabase database)
    {
        CsvDatabase successPatched = PatchAction(database, "ACT_001", "SuccessTurnDelta", "2");
        CsvDatabase failPatched = PatchAction(database, "ACT_003", "FailTurnDelta", "1");
        if (successPatched == null || successPatched.HasErrors
            || failPatched == null || failPatched.HasErrors)
            return false;

        ActionRow move;
        ActionRow risky;
        if (!successPatched.TryGetAction("ACT_001", out move)
            || move.SuccessTurnDelta != 2
            || !failPatched.TryGetAction("ACT_003", out risky)
            || risky.FailTurnDelta != 1)
            return false;

        RunState successRun;
        if (!RunState.TryStart(successPatched, "SHOP_001", out successRun))
            return false;
        if (!successRun.ApplyTurnDelta(-(successRun.RemainingTurns - move.TurnCost)))
            return false;

        var winResolver = new ActionResolver(successPatched, new FixedChanceRoll(true));
        ActionResolveResult win;
        if (!winResolver.TryResolve(successRun, "ACT_001", out win) || !win.Success)
            return false;
        if (successRun.RemainingTurns != 2
            || successRun.Status != RunStatus.InProgress
            || successRun.CurrentPhase != move.SuccessNextPhase)
            return false;

        RunState failRun;
        if (!RunState.TryStart(failPatched, "SHOP_001", out failRun))
            return false;
        if (!failRun.ApplyTurnDelta(-(failRun.RemainingTurns - risky.TurnCost)))
            return false;

        var failResolver = new ActionResolver(failPatched, new FixedChanceRoll(false));
        ActionResolveResult fail;
        if (!failResolver.TryResolve(failRun, "ACT_003", out fail) || fail.Success)
            return false;
        return failRun.RemainingTurns == 1
            && failRun.Status == RunStatus.InProgress
            && failRun.CurrentPhase == "MOVE";
    }

    static bool TestAct013SuccessMovesToEscapeDoesNotWin(CsvDatabase database)
    {
        ActionRow prepare;
        ShopRow shop1;
        ShopRow shop3;
        if (!database.TryGetAction("ACT_013", out prepare)
            || !database.TryGetShop("SHOP_001", out shop1)
            || !database.TryGetShop("SHOP_003", out shop3))
            return false;

        RunState blocked;
        if (!RunState.TryStart(database, "SHOP_001", out blocked) || !blocked.SetPhase("LOOT"))
            return false;
        var resolver = new ActionResolver(database, new FixedChanceRoll(true));
        ActionRow ignored;
        if (blocked.Candy != 0
            || resolver.EvaluateSelect(blocked, "ACT_013", out ignored) != ActionRejectReason.ShowCondition
            || resolver.CanSelect(blocked, "ACT_013"))
            return false;

        RunState lootRun;
        if (!RunState.TryStart(database, "SHOP_001", out lootRun) || !lootRun.SetPhase("LOOT"))
            return false;
        if (!lootRun.ApplyCandyDelta(database.GetInt("MIN_CANDY_TO_ESCAPE")))
            return false;
        if (!resolver.CanSelect(lootRun, "ACT_013"))
            return false;
        if (!MatchesClampedChance(
            resolver,
            prepare,
            shop1,
            database.GetInt("MIN_SUCCESS_RATE"),
            database.GetInt("MAX_SUCCESS_RATE")))
            return false;

        ActionResolveResult win;
        if (!resolver.TryResolve(lootRun, "ACT_013", out win) || !win.Success)
            return false;
        if (lootRun.Status != RunStatus.InProgress
            || lootRun.CurrentPhase != "ESCAPE"
            || lootRun.FailReason != RunFailReason.None)
            return false;

        RunState hardShop;
        if (!RunState.TryStart(database, "SHOP_003", out hardShop) || !hardShop.SetPhase("LOOT"))
            return false;
        if (!hardShop.ApplyCandyDelta(database.GetInt("MIN_CANDY_TO_ESCAPE")))
            return false;

        var failResolver = new ActionResolver(database, new FixedChanceRoll(false));
        int chance = failResolver.ComputeSuccessChance(prepare, shop3);
        if (chance <= 0 || chance >= 100 || !failResolver.CanSelect(hardShop, "ACT_013"))
            return false;

        ActionResolveResult fail;
        if (!failResolver.TryResolve(hardShop, "ACT_013", out fail) || fail.Success)
            return false;
        return hardShop.Status == RunStatus.InProgress
            && hardShop.CurrentPhase == "LOOT"
            && failResolver.CanSelect(hardShop, "ACT_013");
    }

    static bool TryStartEscapeRun(
        CsvDatabase database,
        string shopId,
        bool withMinCandy,
        bool lastTurn,
        out RunState run,
        out ActionRow escape)
    {
        run = null;
        escape = null;
        if (!database.TryGetAction("ACT_010", out escape))
            return false;
        if (!RunState.TryStart(database, shopId, out run))
            return false;
        if (withMinCandy)
        {
            int need = database.GetInt("MIN_CANDY_TO_ESCAPE") - run.Candy;
            if (need > 0 && !run.ApplyCandyDelta(need))
                return false;
        }

        if (!run.SetPhase("ESCAPE"))
            return false;
        if (lastTurn)
        {
            int keep = escape.TurnCost;
            if (keep < 0 || !run.ApplyTurnDelta(-(run.RemainingTurns - keep)))
                return false;
            if (run.RemainingTurns != keep)
                return false;
        }

        return run.Status == RunStatus.InProgress;
    }

    static bool MatchesClampedChance(
        ActionResolver resolver,
        ActionRow action,
        ShopRow shop,
        int min,
        int max)
    {
        int raw = action.SuccessRate + shop.SuccessRateBonusPp;
        int expected = raw;
        if (expected < min)
            expected = min;
        if (expected > max)
            expected = max;
        return resolver.ComputeSuccessChance(action, shop) == expected;
    }

    static bool EndedRunRejectsFurtherPlay(ActionResolver resolver, RunState run)
    {
        if (run == null || !run.IsEnded)
            return false;

        int turns = run.RemainingTurns;
        int alert = run.Alert;
        int candy = run.Candy;
        string phase = run.CurrentPhase;
        RunStatus status = run.Status;
        RunFailReason failReason = run.FailReason;
        RunActionResult last = run.LastActionResult;

        ActionRow ignored;
        ActionResolveResult again;
        bool applied = resolver.TryResolve(run, "ACT_001", out again);
        List<ActionRow> available = CollectAvailable(resolver, run);
        return available.Count == 0
            && !resolver.CanSelect(run, "ACT_001")
            && resolver.EvaluateSelect(run, "ACT_001", out ignored) == ActionRejectReason.Ended
            && !applied
            && !again.Applied
            && again.RejectReason == ActionRejectReason.Ended
            && run.RemainingTurns == turns
            && run.Alert == alert
            && run.Candy == candy
            && run.CurrentPhase == phase
            && run.Status == status
            && run.FailReason == failReason
            && run.LastActionResult == last;
    }

    static List<ActionRow> CollectAvailable(ActionResolver resolver, RunState run)
    {
        var list = new List<ActionRow>();
        resolver.CollectAvailableActions(run, list);
        return list;
    }

    static bool ContainsActionId(List<ActionRow> actions, string actionId)
    {
        for (int i = 0; i < actions.Count; i++)
        {
            if (actions[i].ActionID == actionId)
                return true;
        }

        return false;
    }

    static CsvDatabase PatchAction(CsvDatabase live, string actionId, string column, string value)
    {
        Dictionary<string, string> texts = LoadTableTexts();
        if (texts == null)
            return null;

        string patched;
        if (!TryPatchCsv(texts["ActionTable"], "ActionID", actionId, column, value, out patched))
            return null;

        texts["ActionTable"] = patched;
        CsvDatabase database = CsvDatabase.LoadFromTexts(texts);
        return database;
    }

    static Dictionary<string, string> LoadTableTexts()
    {
        var texts = new Dictionary<string, string>();
        for (int i = 0; i < GameplayTables.Length; i++)
        {
            TextAsset asset = Resources.Load<TextAsset>("Table/" + GameplayTables[i]);
            if (asset == null)
                return null;
            texts[GameplayTables[i]] = asset.text;
        }

        return texts;
    }

    static bool TryPatchCsv(string csv, string idColumn, string id, string valueColumn, string value, out string patched)
    {
        patched = null;
        if (!string.IsNullOrEmpty(csv) && csv[0] == '\uFEFF')
            csv = csv.Substring(1);
        string[] lines = csv.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n');
        if (lines.Length < 2)
            return false;

        string[] header = lines[0].Split(',');
        int idIndex = -1;
        int valueIndex = -1;
        for (int i = 0; i < header.Length; i++)
        {
            if (header[i] == idColumn)
                idIndex = i;
            if (header[i] == valueColumn)
                valueIndex = i;
        }

        if (idIndex < 0 || valueIndex < 0)
            return false;

        var rebuilt = new List<string>();
        rebuilt.Add(lines[0]);
        bool found = false;
        for (int i = 1; i < lines.Length; i++)
        {
            if (string.IsNullOrEmpty(lines[i]))
                continue;
            string[] cells = lines[i].Split(',');
            if (cells.Length > idIndex && cells[idIndex] == id && cells.Length > valueIndex)
            {
                cells[valueIndex] = value;
                rebuilt.Add(string.Join(",", cells));
                found = true;
            }
            else
            {
                rebuilt.Add(lines[i]);
            }
        }

        if (!found)
            return false;

        patched = string.Join("\n", rebuilt.ToArray());
        return true;
    }

#if UNITY_EDITOR
    [UnityEditor.MenuItem("Candy Thief/V02/Run ActionResolver Tests")]
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
