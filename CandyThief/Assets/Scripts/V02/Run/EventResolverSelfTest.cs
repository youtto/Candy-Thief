using System.Collections.Generic;
using UnityEngine;

public static class EventResolverSelfTest
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
            Debug.LogError(EventResolver.LogPrefix + "자가 검사 실패: CsvDatabase가 준비되지 않았습니다.");
            return false;
        }

        RunState previous = RunState.Current;
        int failed = 0;
        failed += Expect("1 EventCheckDue 충족 및 미충족", TestEventCheckDue(database));
        failed += Expect("2 이벤트 발생 확률 성공/실패", TestTriggerChance(database));
        failed += Expect("3 이벤트 후보가 없는 경우", TestNoCandidates(database));
        failed += Expect("4 PhaseFilter는 행동 실행 전 페이즈", TestPhaseFilterUsesPreActionPhase(database));
        failed += Expect("5 EventTags 필터", TestEventTagsFilter(database));
        failed += Expect("6 Alert Min/Max 경계 포함", TestAlertWindowInclusive(database));
        failed += Expect("7 Turns Min/Max 경계 포함", TestTurnsWindowInclusive(database));
        failed += Expect("8 Weight 기반 이벤트 선정", TestWeightPick(database));
        failed += Expect("9 Repeatable=0 실패 후 재등장 가능", TestRepeatable0FailAllowsRedraw(database));
        failed += Expect("10 Repeatable=0 성공 후 재등장 불가", TestRepeatable0SuccessBlocksRedraw(database));
        failed += Expect("11 Repeatable=1 성공 후 재등장 가능", TestRepeatable1SuccessAllowsRedraw(database));
        failed += Expect("12 이벤트 옵션 성공·실패 판정", TestOptionSuccessAndFail(database));
        failed += Expect("13 이벤트 옵션 성공률 보너스 및 clamp", TestOptionChanceBonusAndClamp(database));
        failed += Expect("14 이벤트 옵션 TurnCost 및 상태 변경", TestOptionTurnCostAndDeltas(database));
        failed += Expect("15 옵션 결과에 따른 경계도 상한 실패", TestOptionAlertLimitFail(database));
        failed += Expect("16 옵션 결과에 따른 턴 0 실패", TestOptionNoTurnsFail(database));
        failed += Expect("17 종료된 런에서 이벤트 추첨 및 옵션 처리 불가", TestEndedRunRejectsDrawAndOption(database));
        failed += Expect("18 새 런 시작 시 이벤트 성공 이력 초기화", TestEventSuccessHistoryResetsOnNewRun(database));
        failed += Expect("19 Pending 이벤트 중 행동 선택 및 실행 차단", TestPendingBlocksActions(database));
        failed += Expect("20 다른 이벤트의 OptionID 선택 거부", TestForeignOptionRejected(database));
        failed += Expect("21 옵션 처리 후 LastActionID 유지", TestLastActionIdUnchangedAfterOption(database));
        failed += Expect("22 옵션 처리 후 이벤트 연쇄 발생하지 않음", TestNoEventChainAfterOption(database));
        failed += Expect("23 EVT_001~EVT_005 데이터 기반 등장 가능", TestAllEnabledEventsCanAppear(database));

        RunState.Current = previous;
        if (failed == 0)
        {
            Debug.Log(EventResolver.LogPrefix + "자가 검사 23건 통과");
            return true;
        }

        Debug.LogError(EventResolver.LogPrefix + "자가 검사 실패: " + failed + "건");
        return false;
    }

    static int Expect(string name, bool passed)
    {
        if (passed)
        {
            Debug.Log(EventResolver.LogPrefix + "PASS " + name);
            return 0;
        }

        Debug.LogError(EventResolver.LogPrefix + "FAIL " + name);
        return 1;
    }

    static bool TestEventCheckDue(CsvDatabase database)
    {
        RunState dueRun;
        ActionResolveResult dueAction;
        string duePhase;
        if (!TryResolveAction(database, "SHOP_001", "ACT_001", out dueRun, out dueAction, out duePhase))
            return false;
        ShopRow shop;
        if (!database.TryGetShop("SHOP_001", out shop))
            return false;
        int eventChance = new ActionResolver(database).ComputeEventChance(shop);
        if (!dueAction.EventCheckDue || dueAction.EventChance != eventChance)
            return false;

        var drawer = new EventResolver(database, new FixedChanceRoll(true), new FixedEventPick("EVT_001"));
        EventDrawResult skipped;
        if (drawer.TryDraw(dueRun, false, 100, duePhase, out skipped) || skipped.Drawn)
            return false;
        if (skipped.RejectReason != EventDrawRejectReason.NotDue || dueRun.HasPendingEvent)
            return false;

        EventDrawResult drawn;
        if (!drawer.TryDraw(dueRun, true, 100, duePhase, out drawn) || !drawn.Drawn)
            return false;
        if (drawn.EventID != "EVT_001" || dueRun.PendingEventID != "EVT_001")
            return false;

        RunState skipRun;
        ActionResolveResult skipAction;
        string skipPhase;
        if (!RunState.TryStart(database, "SHOP_001", out skipRun)
            || !skipRun.SetPhase("LOOT")
            || !skipRun.ApplyCandyDelta(database.GetInt("MIN_CANDY_TO_ESCAPE")))
            return false;
        skipPhase = skipRun.CurrentPhase;
        var actions = new ActionResolver(database, new FixedChanceRoll(true));
        if (!actions.TryResolve(skipRun, "ACT_013", out skipAction) || skipAction.EventCheckDue)
            return false;

        EventDrawResult ineligible;
        return !drawer.TryDraw(skipRun, skipAction.EventCheckDue, 100, skipPhase, out ineligible)
            && !ineligible.Drawn
            && ineligible.RejectReason == EventDrawRejectReason.NotDue
            && !skipRun.HasPendingEvent;
    }

    static bool TestTriggerChance(CsvDatabase database)
    {
        RunState failRun;
        ActionResolveResult failAction;
        string failPhase;
        if (!TryResolveAction(database, "SHOP_001", "ACT_001", out failRun, out failAction, out failPhase))
            return false;

        var failDrawer = new EventResolver(database, new FixedChanceRoll(false), new FixedEventPick("EVT_001"));
        EventDrawResult failed;
        if (failDrawer.TryDraw(failRun, failAction.EventCheckDue, failAction.EventChance, failPhase, out failed)
            || failed.Drawn
            || failed.TriggerSucceeded
            || failed.RejectReason != EventDrawRejectReason.TriggerFailed
            || failRun.HasPendingEvent)
            return false;

        RunState winRun;
        ActionResolveResult winAction;
        string winPhase;
        if (!TryResolveAction(database, "SHOP_001", "ACT_001", out winRun, out winAction, out winPhase))
            return false;

        var winDrawer = new EventResolver(database, new FixedChanceRoll(true), new FixedEventPick("EVT_001"));
        EventDrawResult won;
        return winDrawer.TryDraw(winRun, winAction.EventCheckDue, winAction.EventChance, winPhase, out won)
            && won.Drawn
            && won.TriggerSucceeded
            && won.EventChance == failAction.EventChance
            && winRun.PendingEventID == "EVT_001";
    }

    static bool TestNoCandidates(CsvDatabase database)
    {
        CsvDatabase patched = PatchShop(database, "SHOP_001", "AllowedEventTags", "UNUSED");
        if (patched == null)
            return false;

        RunState run;
        ActionResolveResult action;
        string phaseBefore;
        if (!TryResolveAction(patched, "SHOP_001", "ACT_001", out run, out action, out phaseBefore))
            return false;

        var candidates = new List<EventRow>();
        var drawer = new EventResolver(patched, new FixedChanceRoll(true), new WeightedEventPick(new FixedIntRoll(0)));
        drawer.CollectCandidates(run, phaseBefore, candidates);
        EventDrawResult result;
        return candidates.Count == 0
            && !drawer.TryDraw(run, true, 100, phaseBefore, out result)
            && !result.Drawn
            && result.RejectReason == EventDrawRejectReason.NoCandidates
            && result.CandidateCount == 0
            && !run.HasPendingEvent;
    }

    static bool TestPhaseFilterUsesPreActionPhase(CsvDatabase database)
    {
        RunState run;
        ActionResolveResult action;
        string phaseBefore;
        if (!TryResolveAction(database, "SHOP_001", "ACT_001", out run, out action, out phaseBefore))
            return false;
        if (phaseBefore != "MOVE" || run.CurrentPhase != "INFILTRATE")
            return false;

        var resolver = new EventResolver(database, new FixedChanceRoll(true), new FixedEventPick("EVT_004"));
        var before = new List<EventRow>();
        var after = new List<EventRow>();
        resolver.CollectCandidates(run, phaseBefore, before);
        resolver.CollectCandidates(run, run.CurrentPhase, after);
        if (!ContainsEventId(before, "EVT_004") || ContainsEventId(after, "EVT_004"))
            return false;

        EventDrawResult drawn;
        if (!resolver.TryDraw(run, true, 100, phaseBefore, out drawn) || drawn.EventID != "EVT_004")
            return false;

        RunState wrongPhaseRun;
        ActionResolveResult wrongAction;
        string wrongPhase;
        if (!TryResolveAction(database, "SHOP_001", "ACT_001", out wrongPhaseRun, out wrongAction, out wrongPhase))
            return false;
        EventDrawResult skipped;
        return !resolver.TryDraw(wrongPhaseRun, true, 100, wrongPhaseRun.CurrentPhase, out skipped)
            && !skipped.Drawn
            && skipped.RejectReason == EventDrawRejectReason.NoCandidates
            && !wrongPhaseRun.HasPendingEvent;
    }

    static bool TestEventTagsFilter(CsvDatabase database)
    {
        CsvDatabase patched = PatchEvent(database, "EVT_001", "EventTags", "WAREHOUSE");
        if (patched == null)
            return false;

        RunState run;
        ActionResolveResult action;
        string phaseBefore;
        if (!TryResolveAction(patched, "SHOP_001", "ACT_001", out run, out action, out phaseBefore))
            return false;

        var resolver = new EventResolver(patched);
        var candidates = new List<EventRow>();
        resolver.CollectCandidates(run, phaseBefore, candidates);
        return !ContainsEventId(candidates, "EVT_001")
            && ContainsEventId(candidates, "EVT_003")
            && ContainsEventId(candidates, "EVT_004")
            && ContainsEventId(candidates, "EVT_005");
    }

    static bool TestAlertWindowInclusive(CsvDatabase database)
    {
        RunState low;
        ActionResolveResult lowAction;
        string lowPhase;
        if (!TryResolveAction(database, "SHOP_001", "ACT_001", out low, out lowAction, out lowPhase))
            return false;
        if (low.Alert != 0)
            return false;

        var live = new EventResolver(database);
        var atZero = new List<EventRow>();
        live.CollectCandidates(low, lowPhase, atZero);
        if (!ContainsEventId(atZero, "EVT_001"))
            return false;

        CsvDatabase minPatched = PatchEvent(database, "EVT_001", "MinAlert", "1");
        if (minPatched == null)
            return false;
        var minResolver = new EventResolver(minPatched);
        var excludedMin = new List<EventRow>();
        minResolver.CollectCandidates(low, lowPhase, excludedMin);
        if (ContainsEventId(excludedMin, "EVT_001"))
            return false;

        RunState high;
        ActionResolveResult highAction;
        string highPhase;
        if (!TryResolveAction(database, "SHOP_001", "ACT_001", out high, out highAction, out highPhase))
            return false;
        if (!high.ApplyAlertDelta(99 - high.Alert) || high.Alert != 99)
            return false;

        var atMax = new List<EventRow>();
        live.CollectCandidates(high, highPhase, atMax);
        if (!ContainsEventId(atMax, "EVT_001"))
            return false;

        CsvDatabase maxPatched = PatchEvent(database, "EVT_001", "MaxAlert", "98");
        if (maxPatched == null)
            return false;
        var maxResolver = new EventResolver(maxPatched);
        var excludedMax = new List<EventRow>();
        maxResolver.CollectCandidates(high, highPhase, excludedMax);
        return !ContainsEventId(excludedMax, "EVT_001");
    }

    static bool TestTurnsWindowInclusive(CsvDatabase database)
    {
        RunState run;
        ActionResolveResult action;
        string phaseBefore;
        if (!TryResolveAction(database, "SHOP_001", "ACT_001", out run, out action, out phaseBefore))
            return false;

        int turns = run.RemainingTurns;
        var live = new EventResolver(database);
        var included = new List<EventRow>();
        live.CollectCandidates(run, phaseBefore, included);
        if (!ContainsEventId(included, "EVT_001"))
            return false;

        CsvDatabase minInside = PatchEvent(database, "EVT_001", "MinRemainingTurn", turns.ToString());
        CsvDatabase minOutside = PatchEvent(database, "EVT_001", "MinRemainingTurn", (turns + 1).ToString());
        CsvDatabase maxInside = PatchEvent(database, "EVT_001", "MaxRemainingTurn", turns.ToString());
        CsvDatabase maxOutside = PatchEvent(database, "EVT_001", "MaxRemainingTurn", (turns - 1).ToString());
        if (minInside == null || minOutside == null || maxInside == null || maxOutside == null)
            return false;

        if (!ContainsEventId(Collect(minInside, run, phaseBefore), "EVT_001"))
            return false;
        if (ContainsEventId(Collect(minOutside, run, phaseBefore), "EVT_001"))
            return false;
        if (!ContainsEventId(Collect(maxInside, run, phaseBefore), "EVT_001"))
            return false;
        if (ContainsEventId(Collect(maxOutside, run, phaseBefore), "EVT_001"))
            return false;

        int keep = 1;
        if (!run.ApplyTurnDelta(-(run.RemainingTurns - keep)) || run.RemainingTurns != 1)
            return false;
        var atOne = new List<EventRow>();
        live.CollectCandidates(run, phaseBefore, atOne);
        return ContainsEventId(atOne, "EVT_001") && run.Status == RunStatus.InProgress;
    }

    static bool TestWeightPick(CsvDatabase database)
    {
        RunState run;
        ActionResolveResult action;
        string phaseBefore;
        if (!TryResolveAction(database, "SHOP_001", "ACT_001", out run, out action, out phaseBefore))
            return false;

        var candidates = new List<EventRow>();
        new EventResolver(database).CollectCandidates(run, phaseBefore, candidates);
        if (candidates.Count != 4
            || candidates[0].EventID != "EVT_001"
            || candidates[1].EventID != "EVT_003"
            || candidates[2].EventID != "EVT_004"
            || candidates[3].EventID != "EVT_005")
            return false;
        if (candidates[0].Weight != 25 || candidates[1].Weight != 20
            || candidates[2].Weight != 15 || candidates[3].Weight != 10)
            return false;

        return PickId(database, run, phaseBefore, 0) == "EVT_001"
            && PickId(database, run, phaseBefore, 24) == "EVT_001"
            && PickId(database, run, phaseBefore, 25) == "EVT_003"
            && PickId(database, run, phaseBefore, 44) == "EVT_003"
            && PickId(database, run, phaseBefore, 45) == "EVT_004"
            && PickId(database, run, phaseBefore, 59) == "EVT_004"
            && PickId(database, run, phaseBefore, 60) == "EVT_005"
            && PickId(database, run, phaseBefore, 69) == "EVT_005"
            && action.EventChance != candidates[3].Weight;
    }

    static bool TestRepeatable0FailAllowsRedraw(CsvDatabase database)
    {
        CsvDatabase patched = PatchEvent(database, "EVT_001", "Repeatable", "0");
        if (patched == null)
            return false;

        RunState run;
        if (!TryDrawPending(patched, "EVT_001", out run))
            return false;

        var resolver = new EventResolver(patched, new FixedChanceRoll(false), new FixedEventPick("EVT_001"));
        EventOptionResolveResult option;
        if (!resolver.TryResolveOption(run, "OPT_002", out option) || option.Success)
            return false;
        if (run.HasSucceededEvent("EVT_001") || run.HasPendingEvent)
            return false;

        var candidates = new List<EventRow>();
        resolver.CollectCandidates(run, "MOVE", candidates);
        return ContainsEventId(candidates, "EVT_001");
    }

    static bool TestRepeatable0SuccessBlocksRedraw(CsvDatabase database)
    {
        CsvDatabase patched = PatchEvent(database, "EVT_001", "Repeatable", "0");
        if (patched == null)
            return false;

        RunState run;
        if (!TryDrawPending(patched, "EVT_001", out run))
            return false;

        var resolver = new EventResolver(patched, new FixedChanceRoll(true), new FixedEventPick("EVT_001"));
        EventOptionResolveResult option;
        if (!resolver.TryResolveOption(run, "OPT_001", out option) || !option.Success)
            return false;
        if (!run.HasSucceededEvent("EVT_001") || run.HasPendingEvent)
            return false;
        if (run.HasSucceededAction("OPT_001") || run.LastActionID == "OPT_001")
            return false;

        var candidates = new List<EventRow>();
        resolver.CollectCandidates(run, "MOVE", candidates);
        return !ContainsEventId(candidates, "EVT_001");
    }

    static bool TestRepeatable1SuccessAllowsRedraw(CsvDatabase database)
    {
        RunState run;
        if (!TryDrawPending(database, "EVT_001", out run))
            return false;

        var resolver = new EventResolver(database, new FixedChanceRoll(true), new FixedEventPick("EVT_001"));
        EventOptionResolveResult option;
        if (!resolver.TryResolveOption(run, "OPT_001", out option) || !option.Success)
            return false;
        if (!run.HasSucceededEvent("EVT_001"))
            return false;

        var candidates = new List<EventRow>();
        resolver.CollectCandidates(run, "MOVE", candidates);
        return ContainsEventId(candidates, "EVT_001");
    }

    static bool TestOptionSuccessAndFail(CsvDatabase database)
    {
        ShopRow shop;
        EventOptionRow option;
        if (!database.TryGetShop("SHOP_001", out shop) || !database.TryGetEventOption("OPT_002", out option))
            return false;

        RunState winRun;
        if (!TryDrawPending(database, "EVT_001", out winRun))
            return false;
        var winResolver = new EventResolver(database, new FixedChanceRoll(true), new FixedEventPick("EVT_001"));
        int chance = winResolver.ComputeOptionChance(option, shop);
        if (chance <= 0 || chance >= 100)
            return false;

        EventOptionResolveResult win;
        if (!winResolver.TryResolveOption(winRun, "OPT_002", out win) || !win.Success || win.SuccessChance != chance)
            return false;
        if (winRun.Candy != option.SuccessCandy || winRun.Alert != option.SuccessAlert)
            return false;
        if (winRun.Status != RunStatus.InProgress || winRun.HasPendingEvent)
            return false;
        if (!winRun.HasSucceededEvent("EVT_001"))
            return false;

        RunState failRun;
        if (!TryDrawPending(database, "EVT_001", out failRun))
            return false;
        var failResolver = new EventResolver(database, new FixedChanceRoll(false), new FixedEventPick("EVT_001"));
        EventOptionResolveResult fail;
        if (!failResolver.TryResolveOption(failRun, "OPT_002", out fail) || fail.Success)
            return false;
        return failRun.Candy == option.FailCandy
            && failRun.Alert == option.FailAlert
            && !failRun.HasSucceededEvent("EVT_001")
            && !failRun.HasPendingEvent
            && failRun.Status == RunStatus.InProgress;
    }

    static bool TestOptionChanceBonusAndClamp(CsvDatabase database)
    {
        ShopRow shop1;
        ShopRow shop3;
        EventOptionRow safe;
        EventOptionRow risky;
        if (!database.TryGetShop("SHOP_001", out shop1)
            || !database.TryGetShop("SHOP_003", out shop3)
            || !database.TryGetEventOption("OPT_001", out safe)
            || !database.TryGetEventOption("OPT_002", out risky))
            return false;

        var resolver = new EventResolver(database);
        int min = database.GetInt("MIN_SUCCESS_RATE");
        int max = database.GetInt("MAX_SUCCESS_RATE");
        int shop1Safe = resolver.ComputeOptionChance(safe, shop1);
        int shop3Safe = resolver.ComputeOptionChance(safe, shop3);
        int shop1Risky = resolver.ComputeOptionChance(risky, shop1);
        int shop3Risky = resolver.ComputeOptionChance(risky, shop3);
        if (shop1Safe != max || shop1Safe == safe.SuccessRate + shop1.SuccessRateBonusPp)
            return false;
        if (shop3Safe != safe.SuccessRate + shop3.SuccessRateBonusPp)
            return false;
        if (shop1Risky != risky.SuccessRate + shop1.SuccessRateBonusPp)
            return false;
        if (shop3Risky != risky.SuccessRate + shop3.SuccessRateBonusPp)
            return false;

        CsvDatabase patched = PatchOption(database, "OPT_002", "SuccessRate", "0");
        if (patched == null || !patched.TryGetEventOption("OPT_002", out risky) || !patched.TryGetShop("SHOP_003", out shop3))
            return false;
        int clampedMin = new EventResolver(patched).ComputeOptionChance(risky, shop3);
        if (clampedMin != min)
            return false;

        RunState run;
        if (!TryDrawPending(database, "EVT_001", out run))
            return false;
        var hundred = new EventResolver(database, new FixedChanceRoll(false), new FixedEventPick("EVT_001"));
        EventOptionResolveResult result;
        return hundred.ComputeOptionChance(safe, shop1) == max
            && hundred.TryResolveOption(run, "OPT_001", out result)
            && result.Success
            && result.SuccessChance == max;
    }

    static bool TestOptionTurnCostAndDeltas(CsvDatabase database)
    {
        EventOptionRow option;
        if (!database.TryGetEventOption("OPT_007", out option))
            return false;

        RunState run;
        ActionResolveResult action;
        string phaseBefore;
        if (!TryResolveAction(database, "SHOP_001", "ACT_001", out run, out action, out phaseBefore))
            return false;

        var drawer = new EventResolver(database, new FixedChanceRoll(true), new FixedEventPick("EVT_004"));
        EventDrawResult drawn;
        if (!drawer.TryDraw(run, true, 100, phaseBefore, out drawn) || drawn.EventID != "EVT_004")
            return false;

        int turnsBefore = run.RemainingTurns;
        int candyBefore = run.Candy;
        int alertBefore = run.Alert;
        EventOptionResolveResult resolved;
        if (!drawer.TryResolveOption(run, "OPT_007", out resolved) || !resolved.Success)
            return false;

        return run.RemainingTurns == turnsBefore - option.TurnCost + option.SuccessTurnDelta
            && run.Candy == candyBefore + option.SuccessCandy
            && run.Alert == alertBefore + option.SuccessAlert
            && !run.HasPendingEvent;
    }

    static bool TestOptionAlertLimitFail(CsvDatabase database)
    {
        EventOptionRow option;
        if (!database.TryGetEventOption("OPT_002", out option))
            return false;

        RunState run;
        if (!TryDrawPending(database, "EVT_001", out run))
            return false;
        int maxAlert = database.GetInt("MAX_ALERT");
        int need = maxAlert - option.FailAlert;
        if (need < run.Alert || !run.ApplyAlertDelta(need - run.Alert))
            return false;
        if (run.Alert != need || run.IsEnded)
            return false;

        var resolver = new EventResolver(database, new FixedChanceRoll(false), new FixedEventPick("EVT_001"));
        EventOptionResolveResult result;
        if (!resolver.TryResolveOption(run, "OPT_002", out result) || result.Success)
            return false;
        return run.Status == RunStatus.Failed
            && run.FailReason == RunFailReason.AlertLimit
            && run.Alert == maxAlert
            && !run.HasPendingEvent;
    }

    static bool TestOptionNoTurnsFail(CsvDatabase database)
    {
        EventOptionRow option;
        if (!database.TryGetEventOption("OPT_002", out option) || option.TurnCost <= 0)
            return false;

        RunState run;
        if (!TryDrawPending(database, "EVT_001", out run))
            return false;
        if (!run.ApplyTurnDelta(-(run.RemainingTurns - option.TurnCost)) || run.RemainingTurns != option.TurnCost)
            return false;

        var resolver = new EventResolver(database, new FixedChanceRoll(false), new FixedEventPick("EVT_001"));
        EventOptionResolveResult result;
        if (!resolver.TryResolveOption(run, "OPT_002", out result) || result.Success)
            return false;
        return run.Status == RunStatus.Failed
            && run.FailReason == RunFailReason.NoTurns
            && run.RemainingTurns == 0
            && !run.HasPendingEvent;
    }

    static bool TestEndedRunRejectsDrawAndOption(CsvDatabase database)
    {
        RunState run;
        if (!RunState.TryStart(database, "SHOP_001", out run))
            return false;
        int maxAlert = database.GetInt("MAX_ALERT");
        if (!run.ApplyAlertDelta(maxAlert - run.Alert) || run.Status != RunStatus.Failed)
            return false;

        var resolver = new EventResolver(database, new FixedChanceRoll(true), new FixedEventPick("EVT_001"));
        EventDrawResult draw;
        EventOptionResolveResult option;
        ActionRow ignored;
        var actions = new ActionResolver(database, new FixedChanceRoll(true));
        return !resolver.TryDraw(run, true, 100, "MOVE", out draw)
            && draw.RejectReason == EventDrawRejectReason.Ended
            && !run.HasPendingEvent
            && !resolver.TryResolveOption(run, "OPT_001", out option)
            && option.RejectReason == EventOptionRejectReason.Ended
            && actions.EvaluateSelect(run, "ACT_001", out ignored) == ActionRejectReason.Ended;
    }

    static bool TestEventSuccessHistoryResetsOnNewRun(CsvDatabase database)
    {
        CsvDatabase patched = PatchEvent(database, "EVT_001", "Repeatable", "0");
        if (patched == null)
            return false;

        RunState first;
        if (!TryDrawPending(patched, "EVT_001", out first))
            return false;
        var resolver = new EventResolver(patched, new FixedChanceRoll(true), new FixedEventPick("EVT_001"));
        EventOptionResolveResult option;
        if (!resolver.TryResolveOption(first, "OPT_001", out option) || !first.HasSucceededEvent("EVT_001"))
            return false;

        RunState second;
        ActionResolveResult action;
        string phaseBefore;
        if (!TryResolveAction(patched, "SHOP_001", "ACT_001", out second, out action, out phaseBefore))
            return false;
        var candidates = new List<EventRow>();
        resolver.CollectCandidates(second, phaseBefore, candidates);
        return second != first
            && !second.HasSucceededEvent("EVT_001")
            && ContainsEventId(candidates, "EVT_001")
            && !second.HasPendingEvent;
    }

    static bool TestPendingBlocksActions(CsvDatabase database)
    {
        RunState run;
        if (!TryDrawPending(database, "EVT_001", out run) || !run.HasPendingEvent)
            return false;

        ActionRow action;
        if (!database.TryGetAction("ACT_004", out action))
            return false;

        var actions = new ActionResolver(database, new FixedChanceRoll(true));
        ActionRow ignored;
        ActionResolveResult resolved;
        var available = new List<ActionRow>();
        actions.CollectAvailableActions(run, available);
        return available.Count == 0
            && !run.CanSelectAction(action)
            && !actions.CanSelect(run, "ACT_004")
            && actions.EvaluateSelect(run, "ACT_004", out ignored) == ActionRejectReason.EventPending
            && !actions.TryResolve(run, "ACT_004", out resolved)
            && resolved.RejectReason == ActionRejectReason.EventPending
            && run.PendingEventID == "EVT_001"
            && run.LastActionID == "ACT_001";
    }

    static bool TestForeignOptionRejected(CsvDatabase database)
    {
        RunState run;
        if (!TryDrawPending(database, "EVT_001", out run))
            return false;

        var resolver = new EventResolver(database, new FixedChanceRoll(true), new FixedEventPick("EVT_001"));
        EventOptionRow ignored;
        EventOptionResolveResult result;
        return resolver.EvaluateSelectOption(run, "OPT_003", out ignored) == EventOptionRejectReason.WrongEvent
            && !resolver.TryResolveOption(run, "OPT_003", out result)
            && result.RejectReason == EventOptionRejectReason.WrongEvent
            && run.PendingEventID == "EVT_001"
            && run.Status == RunStatus.InProgress;
    }

    static bool TestLastActionIdUnchangedAfterOption(CsvDatabase database)
    {
        RunState run;
        if (!TryDrawPending(database, "EVT_001", out run) || run.LastActionID != "ACT_001")
            return false;

        RunActionResult lastResult = run.LastActionResult;
        var resolver = new EventResolver(database, new FixedChanceRoll(true), new FixedEventPick("EVT_001"));
        EventOptionResolveResult option;
        if (!resolver.TryResolveOption(run, "OPT_001", out option) || !option.Success)
            return false;
        return run.LastActionID == "ACT_001"
            && run.LastActionResult == lastResult
            && !run.HasSucceededAction("OPT_001")
            && run.HasSucceededEvent("EVT_001");
    }

    static bool TestNoEventChainAfterOption(CsvDatabase database)
    {
        RunState run;
        if (!TryDrawPending(database, "EVT_001", out run))
            return false;

        var resolver = new EventResolver(database, new FixedChanceRoll(true), new FixedEventPick("EVT_001"));
        EventOptionResolveResult option;
        if (!resolver.TryResolveOption(run, "OPT_001", out option) || run.HasPendingEvent)
            return false;
        if (database.GetBool("EVENT_CHAIN_ENABLED") || database.GetInt("MAX_EVENT_PER_ACTION") != 1)
            return false;

        EventDrawResult again;
        return !resolver.TryDraw(run, true, 100, "MOVE", out again)
            && !again.Drawn
            && again.RejectReason == EventDrawRejectReason.CheckConsumed
            && !run.HasPendingEvent
            && run.Status == RunStatus.InProgress;
    }

    static bool TestAllEnabledEventsCanAppear(CsvDatabase database)
    {
        if (database.EnabledEvents.Count != 5)
            return false;

        string[] ids = { "EVT_001", "EVT_002", "EVT_003", "EVT_004", "EVT_005" };
        var seen = new HashSet<string>();
        CollectPhase(database, "MOVE", "ACT_001", seen);
        CollectPhase(database, "INFILTRATE", "ACT_004", seen);
        CollectPhase(database, "LOOT", "ACT_007", seen);
        for (int i = 0; i < ids.Length; i++)
        {
            if (!seen.Contains(ids[i]))
                return false;
            if (!CanDrawSpecific(database, ids[i]))
                return false;
        }

        return true;
    }

    static void CollectPhase(CsvDatabase database, string phaseId, string actionId, HashSet<string> seen)
    {
        RunState run;
        ActionResolveResult action;
        string phaseBefore;
        if (!RunState.TryStart(database, "SHOP_001", out run) || !run.SetPhase(phaseId))
            return;
        if (!TryResolveExisting(database, run, actionId, out action, out phaseBefore))
            return;

        var candidates = new List<EventRow>();
        new EventResolver(database).CollectCandidates(run, phaseBefore, candidates);
        for (int i = 0; i < candidates.Count; i++)
            seen.Add(candidates[i].EventID);
    }

    static bool CanDrawSpecific(CsvDatabase database, string eventId)
    {
        EventRow evt;
        if (!database.TryGetEvent(eventId, out evt))
            return false;

        string actionId = "ACT_001";
        string phaseId = "MOVE";
        if (evt.PhaseFilter == "LOOT")
        {
            actionId = "ACT_007";
            phaseId = "LOOT";
        }
        else if (evt.PhaseFilter == "INFILTRATE")
        {
            actionId = "ACT_004";
            phaseId = "INFILTRATE";
        }
        else if (evt.PhaseFilter == "MOVE")
        {
            actionId = "ACT_001";
            phaseId = "MOVE";
        }

        RunState run;
        if (!RunState.TryStart(database, "SHOP_001", out run) || !run.SetPhase(phaseId))
            return false;

        ActionResolveResult action;
        string phaseBefore;
        if (!TryResolveExisting(database, run, actionId, out action, out phaseBefore) || !action.EventCheckDue)
            return false;

        var drawer = new EventResolver(database, new FixedChanceRoll(true), new FixedEventPick(eventId));
        EventDrawResult drawn;
        return drawer.TryDraw(run, true, 100, phaseBefore, out drawn)
            && drawn.EventID == eventId
            && run.PendingEventID == eventId;
    }

    static bool TryResolveAction(
        CsvDatabase database,
        string shopId,
        string actionId,
        out RunState run,
        out ActionResolveResult result,
        out string phaseBefore)
    {
        result = default(ActionResolveResult);
        phaseBefore = string.Empty;
        if (!RunState.TryStart(database, shopId, out run))
            return false;
        return TryResolveExisting(database, run, actionId, out result, out phaseBefore);
    }

    static bool TryResolveExisting(
        CsvDatabase database,
        RunState run,
        string actionId,
        out ActionResolveResult result,
        out string phaseBefore)
    {
        phaseBefore = run.CurrentPhase;
        var resolver = new ActionResolver(database, new FixedChanceRoll(true));
        return resolver.TryResolve(run, actionId, out result) && result.Applied;
    }

    static bool TryDrawPending(CsvDatabase database, string eventId, out RunState run)
    {
        ActionResolveResult action;
        string phaseBefore;
        if (!TryResolveAction(database, "SHOP_001", "ACT_001", out run, out action, out phaseBefore))
            return false;

        var drawer = new EventResolver(database, new FixedChanceRoll(true), new FixedEventPick(eventId));
        EventDrawResult drawn;
        return drawer.TryDraw(run, true, 100, phaseBefore, out drawn)
            && drawn.EventID == eventId
            && run.HasPendingEvent;
    }

    static string PickId(CsvDatabase database, RunState run, string phaseBefore, int roll)
    {
        var drawer = new EventResolver(database, new FixedChanceRoll(true), new WeightedEventPick(new FixedIntRoll(roll)));
        EventDrawResult result;
        RunState copy;
        ActionResolveResult action;
        string phase;
        if (!TryResolveAction(database, "SHOP_001", "ACT_001", out copy, out action, out phase))
            return string.Empty;
        if (!drawer.TryDraw(copy, true, 100, phase, out result) || !result.Drawn)
            return string.Empty;
        return result.EventID;
    }

    static List<EventRow> Collect(CsvDatabase database, RunState run, string phaseBefore)
    {
        var list = new List<EventRow>();
        new EventResolver(database).CollectCandidates(run, phaseBefore, list);
        return list;
    }

    static bool ContainsEventId(List<EventRow> events, string eventId)
    {
        for (int i = 0; i < events.Count; i++)
        {
            if (events[i].EventID == eventId)
                return true;
        }

        return false;
    }

    static CsvDatabase PatchEvent(CsvDatabase live, string eventId, string column, string value)
    {
        return PatchTable(live, "EventTable", "EventID", eventId, column, value);
    }

    static CsvDatabase PatchShop(CsvDatabase live, string shopId, string column, string value)
    {
        return PatchTable(live, "ShopTable", "ShopID", shopId, column, value);
    }

    static CsvDatabase PatchOption(CsvDatabase live, string optionId, string column, string value)
    {
        return PatchTable(live, "EventOptionTable", "OptionID", optionId, column, value);
    }

    static CsvDatabase PatchTable(CsvDatabase live, string table, string idColumn, string id, string column, string value)
    {
        Dictionary<string, string> texts = LoadTableTexts();
        if (texts == null || live == null)
            return null;

        string patched;
        if (!TryPatchCsv(texts[table], idColumn, id, column, value, out patched))
            return null;

        texts[table] = patched;
        CsvDatabase database = CsvDatabase.LoadFromTexts(texts);
        return database != null && database.Loaded && !database.HasErrors ? database : null;
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
    [UnityEditor.MenuItem("Candy Thief/V02/Run EventResolver Tests")]
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
