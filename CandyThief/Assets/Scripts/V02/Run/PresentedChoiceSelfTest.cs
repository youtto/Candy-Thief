using System.Collections.Generic;
using UnityEngine;

public static class PresentedChoiceSelfTest
{
    public static bool RunAll(CsvDatabase database)
    {
        if (database == null || !database.Loaded || database.HasErrors)
        {
            Debug.LogError("[PresentedChoice] 자가 검사 실패: CsvDatabase가 준비되지 않았습니다.");
            return false;
        }

        RunState previous = RunState.Current;
        int failed = 0;
        failed += Expect("1 턴 부족 행동은 노출하되 실행 불가", TestInsufficientTurnsPresented(database));
        failed += Expect("2 다른 페이즈 행동은 숨김", TestWrongPhaseHidden(database));
        failed += Expect("3 ShowCondition 미충족은 숨김", TestShowConditionHidden(database));
        failed += Expect("4 CollectAvailable는 실행 가능만", TestAvailableExcludesDisabled(database));
        failed += Expect("5 Pending 중 행동은 숨김", TestPendingHidesActions(database));
        failed += Expect("6 Pending 이벤트 옵션은 노출", TestPendingOptionsPresented(database));
        failed += Expect("7 턴 부족 옵션은 노출하되 실행 불가", TestInsufficientTurnsOptionPresented(database));
        failed += Expect("8 Pending 없으면 옵션 숨김", TestNoPendingHidesOptions(database));

        RunState.Current = previous;
        if (failed == 0)
        {
            Debug.Log("[PresentedChoice] 자가 검사 8건 통과");
            return true;
        }

        Debug.LogError("[PresentedChoice] 자가 검사 실패: " + failed + "건");
        return false;
    }

    static int Expect(string name, bool passed)
    {
        if (passed)
        {
            Debug.Log("[PresentedChoice] PASS " + name);
            return 0;
        }

        Debug.LogError("[PresentedChoice] FAIL " + name);
        return 1;
    }

    static bool TestInsufficientTurnsPresented(CsvDatabase database)
    {
        RunState run;
        ActionRow action;
        if (!RunState.TryStart(database, "SHOP_001", out run) || !database.TryGetAction("ACT_001", out action))
            return false;
        if (!run.ApplyTurnDelta(-(run.RemainingTurns - (action.TurnCost - 1))))
            return false;

        var resolver = new ActionResolver(database);
        ActionRow presented;
        bool canExecute;
        ActionRejectReason reason;
        return resolver.IsActionPresented(run, "ACT_001", out presented, out canExecute, out reason)
            && presented != null
            && !canExecute
            && reason == ActionRejectReason.InsufficientTurns
            && !resolver.CanSelect(run, "ACT_001");
    }

    static bool TestWrongPhaseHidden(CsvDatabase database)
    {
        RunState run;
        if (!RunState.TryStart(database, "SHOP_001", out run))
            return false;

        var resolver = new ActionResolver(database);
        ActionRow presented;
        bool canExecute;
        ActionRejectReason reason;
        return !resolver.IsActionPresented(run, "ACT_004", out presented, out canExecute, out reason)
            && reason == ActionRejectReason.WrongPhase;
    }

    static bool TestShowConditionHidden(CsvDatabase database)
    {
        RunState run;
        if (!RunState.TryStart(database, "SHOP_001", out run) || !run.SetPhase("LOOT"))
            return false;
        if (run.Candy != 0)
            return false;

        var resolver = new ActionResolver(database);
        ActionRow presented;
        bool canExecute;
        ActionRejectReason reason;
        return !resolver.IsActionPresented(run, "ACT_013", out presented, out canExecute, out reason)
            && reason == ActionRejectReason.ShowCondition;
    }

    static bool TestAvailableExcludesDisabled(CsvDatabase database)
    {
        RunState run;
        ActionRow action;
        if (!RunState.TryStart(database, "SHOP_001", out run) || !database.TryGetAction("ACT_001", out action))
            return false;
        if (!run.ApplyTurnDelta(-(run.RemainingTurns - (action.TurnCost - 1))))
            return false;

        var resolver = new ActionResolver(database);
        var available = new List<ActionRow>();
        var presented = new List<PresentedAction>();
        resolver.CollectAvailableActions(run, available);
        resolver.CollectPresentedActions(run, presented);
        bool hasPresented = false;
        for (int i = 0; i < presented.Count; i++)
        {
            if (presented[i].Action.ActionID == "ACT_001" && !presented[i].CanExecute)
                hasPresented = true;
        }

        bool hasAvailable = false;
        for (int i = 0; i < available.Count; i++)
        {
            if (available[i].ActionID == "ACT_001")
                hasAvailable = true;
        }

        return hasPresented && !hasAvailable;
    }

    static bool TestPendingHidesActions(CsvDatabase database)
    {
        RunState run;
        if (!TryPendingEvt001(database, out run))
            return false;

        var resolver = new ActionResolver(database);
        ActionRow presented;
        bool canExecute;
        ActionRejectReason reason;
        return !resolver.IsActionPresented(run, "ACT_004", out presented, out canExecute, out reason)
            && reason == ActionRejectReason.EventPending;
    }

    static bool TestPendingOptionsPresented(CsvDatabase database)
    {
        RunState run;
        if (!TryPendingEvt001(database, out run))
            return false;

        var resolver = new EventResolver(database);
        var presented = new List<PresentedOption>();
        resolver.CollectPresentedOptions(run, presented);
        return presented.Count == 2
            && presented[0].Option.OptionID == "OPT_001"
            && presented[1].Option.OptionID == "OPT_002"
            && presented[0].CanExecute
            && presented[1].CanExecute;
    }

    static bool TestInsufficientTurnsOptionPresented(CsvDatabase database)
    {
        CsvDatabase patched = PatchOptionTurnCost(database, "OPT_002", "5");
        if (patched == null)
            return false;

        RunState run;
        if (!TryPendingEvt001(patched, out run))
            return false;
        if (run.RemainingTurns >= 5)
        {
            int keep = 4;
            if (!run.ApplyTurnDelta(-(run.RemainingTurns - keep)))
                return false;
        }

        var resolver = new EventResolver(patched);
        EventOptionRow option;
        bool canExecute;
        EventOptionRejectReason reason;
        var available = new List<EventOptionRow>();
        resolver.CollectAvailableOptions(run, available);
        bool listedExecutable = false;
        for (int i = 0; i < available.Count; i++)
        {
            if (available[i].OptionID == "OPT_002")
                listedExecutable = true;
        }

        return resolver.IsOptionPresented(run, "OPT_002", out option, out canExecute, out reason)
            && !canExecute
            && reason == EventOptionRejectReason.InsufficientTurns
            && !listedExecutable;
    }

    static bool TestNoPendingHidesOptions(CsvDatabase database)
    {
        RunState run;
        if (!RunState.TryStart(database, "SHOP_001", out run))
            return false;

        var resolver = new EventResolver(database);
        EventOptionRow option;
        bool canExecute;
        EventOptionRejectReason reason;
        return !resolver.IsOptionPresented(run, "OPT_001", out option, out canExecute, out reason)
            && reason == EventOptionRejectReason.NoPending;
    }

    static bool TryPendingEvt001(CsvDatabase database, out RunState run)
    {
        ActionResolveResult action;
        if (!RunState.TryStart(database, "SHOP_001", out run))
            return false;

        string phaseBefore = run.CurrentPhase;
        var actions = new ActionResolver(database, new FixedChanceRoll(true));
        if (!actions.TryResolve(run, "ACT_001", out action) || !action.Applied)
            return false;

        var events = new EventResolver(database, new FixedChanceRoll(true), new FixedEventPick("EVT_001"));
        EventDrawResult drawn;
        return events.TryDraw(run, true, 100, phaseBefore, out drawn)
            && run.PendingEventID == "EVT_001";
    }

    static CsvDatabase PatchOptionTurnCost(CsvDatabase live, string optionId, string turnCost)
    {
        string[] tables =
        {
            "GameConfig", "ShopTable", "PhaseTable", "ActionTable", "EventTable",
            "EventOptionTable", "AchievementTable", "AssetTable", "EffectTable"
        };
        var texts = new Dictionary<string, string>();
        for (int i = 0; i < tables.Length; i++)
        {
            TextAsset asset = Resources.Load<TextAsset>("Table/" + tables[i]);
            if (asset == null)
                return null;
            texts[tables[i]] = asset.text;
        }

        string csv = texts["EventOptionTable"];
        if (!string.IsNullOrEmpty(csv) && csv[0] == '\uFEFF')
            csv = csv.Substring(1);
        string[] lines = csv.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n');
        if (lines.Length < 2)
            return null;
        string[] header = lines[0].Split(',');
        int idIndex = -1;
        int costIndex = -1;
        for (int i = 0; i < header.Length; i++)
        {
            if (header[i] == "OptionID")
                idIndex = i;
            if (header[i] == "TurnCost")
                costIndex = i;
        }

        if (idIndex < 0 || costIndex < 0)
            return null;

        var rebuilt = new List<string>();
        rebuilt.Add(lines[0]);
        bool found = false;
        for (int i = 1; i < lines.Length; i++)
        {
            if (string.IsNullOrEmpty(lines[i]))
                continue;
            string[] cells = lines[i].Split(',');
            if (cells.Length > idIndex && cells[idIndex] == optionId && cells.Length > costIndex)
            {
                cells[costIndex] = turnCost;
                rebuilt.Add(string.Join(",", cells));
                found = true;
            }
            else
            {
                rebuilt.Add(lines[i]);
            }
        }

        if (!found)
            return null;

        texts["EventOptionTable"] = string.Join("\n", rebuilt.ToArray());
        CsvDatabase patched = CsvDatabase.LoadFromTexts(texts);
        return patched != null && patched.Loaded && !patched.HasErrors ? patched : null;
    }

#if UNITY_EDITOR
    [UnityEditor.MenuItem("Candy Thief/V02/Run PresentedChoice Tests")]
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
