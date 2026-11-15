using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public static class ActionHandSelfTest
{
    const string LogPrefix = "[ActionHand] ";

    public static bool RunAll(CsvDatabase database)
    {
        if (database == null || !database.Loaded || database.HasErrors)
        {
            Debug.LogError(LogPrefix + "자가 검사 실패: CsvDatabase가 준비되지 않았습니다.");
            return false;
        }

        RunState previous = RunState.Current;
        int failed = 0;
        failed += Expect("1 페이즈별 서로 다른 2장", TestTwoDistinctPerPhase(database));
        failed += Expect("2 가중치 0과 ACT_013 제외", TestWeightAndEscapeExcluded(database));
        failed += Expect("3 태세 균형과 동일 태세 허용", TestStanceMixAndSameStance());
        failed += Expect("4 직전 카드 회피와 후보 부족 폴백", TestUnseenAndFallback());
        failed += Expect("5 실행 가능 최소 1장과 전부 불가 허용", TestExecutableGuarantee());
        failed += Expect("6 후보 2개 미만은 추첨 실패", TestTooFewCandidates());
        failed += Expect("7 가중치 추첨", TestSpawnWeight());
        failed += Expect("8 남은 턴 1에서도 실행 가능 카드 우선", TestOneTurn(database));
        failed += Expect("9 신규 48개 결과 문구", TestNewFeedback(database));
        failed += Expect("10 빈 ImageKey는 오류가 아님", TestEmptyImageKey(database));
        failed += Expect("11 성공 사탕은 기존 SuccessCandy", TestRewardUnchanged(database));
        failed += Expect("12 HUD 갱신은 재추첨하지 않음", TestHudKeepsHand(database));
        failed += Expect("13 행동 후 다음 라운드는 새 2장", TestHudRedrawsAfterAction(database));
        failed += Expect("14 이벤트 선택지 2개", TestEventOptionsStayTwo(database));

        RunState.Current = previous;
        if (failed == 0)
        {
            Debug.Log(LogPrefix + "자가 검사 14건 통과");
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

    static bool TestTwoDistinctPerPhase(CsvDatabase database)
    {
        string[] phases = { "MOVE", "INFILTRATE", "LOOT", "ESCAPE" };
        var resolver = new ActionResolver(database, new FixedChanceRoll(false));
        var drawer = new ActionHandDrawer(database, resolver, new FixedIntRoll(0));
        for (int i = 0; i < phases.Length; i++)
        {
            RunState run;
            if (!RunState.TryStart(database, "SHOP_001", out run) || !run.SetPhase(phases[i]))
                return false;
            var drawn = new List<PresentedAction>();
            if (!drawer.Draw(run, null, drawn) || drawn.Count != 2)
                return false;
            if (drawn[0].Action.ActionID == drawn[1].Action.ActionID)
                return false;
            if (drawn[0].Action.PhaseID != phases[i] || drawn[1].Action.PhaseID != phases[i])
                return false;
            if (drawn[0].Action.ActionStance == drawn[1].Action.ActionStance)
                return false;
        }

        return database.EnabledActions.Count == 61;
    }

    static bool TestWeightAndEscapeExcluded(CsvDatabase database)
    {
        ActionRow hidden;
        if (!database.TryGetAction("ACT_013", out hidden) || hidden.SpawnWeight != 0)
            return false;

        var resolver = new ActionResolver(database, new FixedChanceRoll(false));
        var drawer = new ActionHandDrawer(database, resolver, new FixedIntRoll(0));
        RunState run;
        if (!RunState.TryStart(database, "SHOP_001", out run) || !run.SetPhase("LOOT"))
            return false;
        if (!run.ApplyCandyDelta(5))
            return false;
        var drawn = new List<PresentedAction>();
        if (!drawer.Draw(run, null, drawn))
            return false;
        for (int i = 0; i < drawn.Count; i++)
        {
            if (drawn[i].Action.ActionID == "ACT_013" || drawn[i].Action.SpawnWeight <= 0)
                return false;
        }

        return true;
    }

    static bool TestStanceMixAndSameStance()
    {
        var mixed = new List<ActionHandOption>
        {
            Option("A", ActionStance.STABLE, 1, true),
            Option("B", ActionStance.RISKY, 1, false),
            Option("C", ActionStance.STABLE, 1, false)
        };
        ActionHandOption first;
        ActionHandOption second;
        if (!ActionHandDrawer.TrySelectPair(mixed, null, new FixedIntRoll(0), out first, out second))
            return false;
        if (first.ActionID != "A" || second.ActionID != "B")
            return false;

        var stableOnly = new List<ActionHandOption>
        {
            Option("A", ActionStance.STABLE, 1, true),
            Option("C", ActionStance.STABLE, 1, true)
        };
        if (!ActionHandDrawer.TrySelectPair(stableOnly, null, new FixedIntRoll(0), out first, out second))
            return false;
        return first.Stance == ActionStance.STABLE
            && second.Stance == ActionStance.STABLE
            && first.ActionID != second.ActionID;
    }

    static bool TestUnseenAndFallback()
    {
        var pool = new List<ActionHandOption>
        {
            Option("A", ActionStance.STABLE, 1, true),
            Option("B", ActionStance.RISKY, 1, true),
            Option("C", ActionStance.STABLE, 1, true),
            Option("D", ActionStance.RISKY, 1, true)
        };
        ActionHandOption first;
        ActionHandOption second;
        if (!ActionHandDrawer.TrySelectPair(pool, new[] { "A", "B" }, new FixedIntRoll(0), out first, out second))
            return false;
        if (first.ActionID == "A" || first.ActionID == "B" || second.ActionID == "A" || second.ActionID == "B")
            return false;

        var tight = new List<ActionHandOption>
        {
            Option("A", ActionStance.STABLE, 1, true),
            Option("B", ActionStance.RISKY, 1, true),
            Option("C", ActionStance.STABLE, 1, false)
        };
        if (!ActionHandDrawer.TrySelectPair(tight, new[] { "A", "B" }, new FixedIntRoll(0), out first, out second))
            return false;
        bool reused = first.ActionID == "A" || first.ActionID == "B" || second.ActionID == "A" || second.ActionID == "B";
        return reused && first.ActionID != second.ActionID;
    }

    static bool TestExecutableGuarantee()
    {
        var pool = new List<ActionHandOption>
        {
            Option("A", ActionStance.STABLE, 1, false),
            Option("B", ActionStance.RISKY, 1, false),
            Option("C", ActionStance.STABLE, 1, true)
        };
        ActionHandOption first;
        ActionHandOption second;
        if (!ActionHandDrawer.TrySelectPair(pool, null, new FixedIntRoll(0), out first, out second))
            return false;
        if (!first.CanExecute && !second.CanExecute)
            return false;
        if (first.ActionID == second.ActionID)
            return false;

        var hiddenExec = new List<ActionHandOption>
        {
            Option("A", ActionStance.STABLE, 1, false),
            Option("B", ActionStance.RISKY, 1, false),
            Option("C", ActionStance.STABLE, 1, true)
        };
        if (!ActionHandDrawer.TrySelectPair(hiddenExec, new[] { "C" }, new FixedIntRoll(0), out first, out second))
            return false;
        if (first.ActionID != "C" && second.ActionID != "C")
            return false;

        var none = new List<ActionHandOption>
        {
            Option("A", ActionStance.STABLE, 1, false),
            Option("B", ActionStance.RISKY, 1, false),
            Option("C", ActionStance.STABLE, 1, false)
        };
        if (!ActionHandDrawer.TrySelectPair(none, null, new FixedIntRoll(0), out first, out second))
            return false;
        return !first.CanExecute && !second.CanExecute && first.Stance != second.Stance;
    }

    static bool TestTooFewCandidates()
    {
        var one = new List<ActionHandOption>
        {
            Option("A", ActionStance.STABLE, 1, true)
        };
        ActionHandOption first;
        ActionHandOption second;
        if (ActionHandDrawer.TrySelectPair(one, null, new FixedIntRoll(0), out first, out second))
            return false;

        var zeroed = new List<ActionHandOption>
        {
            Option("A", ActionStance.STABLE, 0, true),
            Option("B", ActionStance.RISKY, 1, true)
        };
        return !ActionHandDrawer.TrySelectPair(zeroed, null, new FixedIntRoll(0), out first, out second);
    }

    static bool TestSpawnWeight()
    {
        var pool = new List<ActionHandOption>
        {
            Option("A", ActionStance.STABLE, 1, true),
            Option("B", ActionStance.RISKY, 10, true)
        };
        ActionHandOption first;
        ActionHandOption second;
        if (!ActionHandDrawer.TrySelectPair(pool, null, new FixedIntRoll(9), out first, out second))
            return false;
        return first.ActionID == "B" && second.ActionID == "A";
    }

    static bool TestOneTurn(CsvDatabase database)
    {
        var resolver = new ActionResolver(database, new FixedChanceRoll(false));
        var drawer = new ActionHandDrawer(database, resolver, new FixedIntRoll(0));
        RunState run;
        if (!RunState.TryStart(database, "SHOP_001", out run) || !run.SetPhase("LOOT"))
            return false;
        if (!run.ApplyTurnDelta(-(run.RemainingTurns - 1)) || run.RemainingTurns != 1)
            return false;
        var drawn = new List<PresentedAction>();
        if (!drawer.Draw(run, null, drawn) || drawn.Count != 2)
            return false;
        int executable = 0;
        bool saw045 = false;
        for (int i = 0; i < drawn.Count; i++)
        {
            if (drawn[i].Action.ActionID == "ACT_045")
            {
                saw045 = true;
                if (!drawn[i].CanExecute || drawn[i].Action.TurnCost != 1)
                    return false;
            }

            if (drawn[i].CanExecute)
                executable++;
            else if (drawn[i].Action.TurnCost <= run.RemainingTurns)
                return false;
        }

        return saw045 && executable >= 1;
    }

    static bool TestNewFeedback(CsvDatabase database)
    {
        for (int number = 14; number <= 61; number++)
        {
            string id = "ACT_" + number.ToString("000");
            ActionRow action;
            ResultFeedbackRow feedback;
            if (!database.TryGetAction(id, out action))
                return false;
            if (!database.TryGetResultFeedback("ACTION", id, out feedback))
                return false;
            if (string.IsNullOrEmpty(feedback.SuccessText) || string.IsNullOrEmpty(feedback.FailText))
                return false;
        }

        ResultFeedbackRow existing;
        return database.TryGetResultFeedback("ACTION", "ACT_001", out existing)
            && database.TryGetResultFeedback("OPTION", "OPT_001", out existing);
    }

    static bool TestEmptyImageKey(CsvDatabase database)
    {
        if (database.HasErrors)
            return false;
        IReadOnlyList<ActionRow> actions = database.EnabledActions;
        for (int i = 0; i < actions.Count; i++)
        {
            if (!string.IsNullOrEmpty(actions[i].ImageKey))
                return false;
            if (actions[i].SuccessCandyMin != actions[i].SuccessCandy || actions[i].SuccessCandyMax != actions[i].SuccessCandy)
                return false;
        }

        return true;
    }

    static bool TestRewardUnchanged(CsvDatabase database)
    {
        ActionRow action;
        if (!database.TryGetAction("ACT_007", out action))
            return false;
        RunState run;
        if (!RunState.TryStart(database, "SHOP_001", out run) || !run.SetPhase("LOOT"))
            return false;
        int before = run.Candy;
        var resolver = new ActionResolver(database, new FixedChanceRoll(true));
        ActionResolveResult resolved;
        if (!resolver.TryResolve(run, "ACT_007", out resolved) || !resolved.Applied || !resolved.Success)
            return false;
        return run.Candy == before + action.SuccessCandy;
    }

    static bool TestHudKeepsHand(CsvDatabase database)
    {
        V02RunHud hud;
        CountingIntRoll roll;
        if (!TryCreateHud(database, new FixedChanceRoll(false), out hud, out roll))
            return false;
        try
        {
            string[] first = ActiveActionIds(hud);
            int rolls = roll.Calls;
            if (first.Length != 2 || first[0] == first[1] || rolls != 2)
                return false;
            ScrollRect scroll = hud.GetComponentInChildren<ScrollRect>(true);
            if (scroll == null || scroll.horizontal || scroll.vertical)
                return false;
            hud.Refresh();
            string[] second = ActiveActionIds(hud);
            return roll.Calls == rolls
                && second.Length == 2
                && second[0] == first[0]
                && second[1] == first[1];
        }
        finally
        {
            Object.DestroyImmediate(hud.gameObject);
        }
    }

    static bool TestHudRedrawsAfterAction(CsvDatabase database)
    {
        V02RunHud hud;
        CountingIntRoll roll;
        if (!TryCreateHud(database, new FixedChanceRoll(false), out hud, out roll))
            return false;
        try
        {
            string[] first = ActiveActionIds(hud);
            if (first.Length != 2 || (first[0] != "ACT_003" && first[1] != "ACT_003"))
                return false;
            if (!hud.TryChoose("ACT_003", false))
                return false;
            hud.ResultPresentation.SkipToHold();
            if (!hud.TryContinueResult())
                return false;
            if (RunState.Current == null || RunState.Current.CurrentPhase != "MOVE")
                return false;
            string[] next = ActiveActionIds(hud);
            if (next.Length != 2 || next[0] == next[1])
                return false;
            if (next[0] == first[0] || next[0] == first[1] || next[1] == first[0] || next[1] == first[1])
                return false;
            int rolls = roll.Calls;
            hud.Refresh();
            string[] held = ActiveActionIds(hud);
            return roll.Calls == rolls && held[0] == next[0] && held[1] == next[1];
        }
        finally
        {
            Object.DestroyImmediate(hud.gameObject);
        }
    }

    static bool TestEventOptionsStayTwo(CsvDatabase database)
    {
        V02RunHud hud;
        CountingIntRoll roll;
        if (!TryCreateHud(database, new FixedChanceRoll(false), out hud, out roll))
            return false;
        try
        {
            string[] actions = ActiveActionIds(hud);
            int rolls = roll.Calls;
            RunState run = RunState.Current;
            if (run == null || !run.TrySetPendingEvent("EVT_001"))
                return false;
            hud.Refresh();
            int options = 0;
            int actionCards = 0;
            var buttons = hud.GetComponentsInChildren<V02ChoiceButton>(true);
            for (int i = 0; i < buttons.Length; i++)
            {
                if (!buttons[i].gameObject.activeInHierarchy)
                    continue;
                if (buttons[i].IsOption)
                    options++;
                else
                    actionCards++;
            }

            run.ClearPendingEvent();
            hud.Refresh();
            string[] restored = ActiveActionIds(hud);
            return options == 2
                && actionCards == 0
                && roll.Calls == rolls
                && restored.Length == 2
                && restored[0] == actions[0]
                && restored[1] == actions[1];
        }
        finally
        {
            Object.DestroyImmediate(hud.gameObject);
        }
    }

    static ActionHandOption Option(string id, ActionStance stance, int weight, bool canExecute)
    {
        return new ActionHandOption
        {
            ActionID = id,
            Stance = stance,
            SpawnWeight = weight,
            CanExecute = canExecute
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

    static bool TryCreateHud(CsvDatabase database, IChanceRoll chance, out V02RunHud hud, out CountingIntRoll roll)
    {
        hud = null;
        roll = new CountingIntRoll();
        V02RunHud prefab = Resources.Load<V02RunHud>(V02RunHud.ResourcePath);
        RunState run;
        if (prefab == null || !RunState.TryStart(database, "SHOP_001", out run))
            return false;
        hud = Object.Instantiate(prefab);
        hud.Bind(database, run, null, chance, null, null, roll);
        return true;
    }

#if UNITY_EDITOR
    [UnityEditor.MenuItem("Candy Thief/V02/Run ActionHand Tests")]
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

    sealed class CountingIntRoll : IIntRoll
    {
        public int Calls;

        public int Next(int maxExclusive)
        {
            Calls++;
            return 0;
        }
    }
}
