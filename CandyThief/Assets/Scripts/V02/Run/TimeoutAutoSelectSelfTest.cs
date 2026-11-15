using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public static class TimeoutAutoSelectSelfTest
{
    const string LogPrefix = "[TimeoutAutoSelect] ";

    public static bool RunAll(CsvDatabase database)
    {
        if (database == null || !database.Loaded || database.HasErrors)
        {
            Debug.LogError(LogPrefix + "자가 검사 실패: CsvDatabase가 준비되지 않았습니다.");
            return false;
        }

        RunState previous = RunState.Current;
        int failed = 0;
        failed += Expect("1 일반 행동 30초 만료 시 자동 선택", TestActionTimeout(database));
        failed += Expect("2 실행 가능한 카드 1장만 선택", TestSingleExecutable(database));
        failed += Expect("3 실행 가능한 2장과 3장 이상은 균등 추첨", TestEqualRoll(database));
        failed += Expect("4 실행 불가능한 카드는 제외", TestSkipsDisabled(database));
        failed += Expect("5 클릭과 시간 초과는 한 번만 실행", TestOneClaim(database));
        failed += Expect("6 이벤트 선택은 새 30초로 시작", TestEventTimerStarts(database));
        failed += Expect("7 이벤트 시간 초과 시 자동 선택", TestEventTimeout(database));
        failed += Expect("8 이벤트 선택 완료 시 타이머 중지", TestEventCompleteStops(database));
        failed += Expect("9 이벤트 갱신은 남은 시간을 유지", TestEventRefreshKeepsTime(database));
        failed += Expect("10 확인 팝업은 멈추고 취소 후 재개", TestConfirmPauses(database));
        failed += Expect("11 결과 팝업에서는 타이머가 멈춤", TestResultPopupsStop(database));
        failed += Expect("12 툴팁 중 만료면 툴팁을 닫고 자동 선택", TestTooltipThenTimeout(database));
        failed += Expect("13 실행 가능 후보 0개는 기록만", TestEmptyCandidates(database));
        failed += Expect("14 새 선택 라운드는 30초로 초기화", TestNewRoundResets(database));

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

    static bool TestActionTimeout(CsvDatabase database)
    {
        V02RunHud hud;
        RunState run;
        if (!TryHud(database, new FixedChanceRoll(true), null, 0, out hud, out run))
            return false;
        try
        {
            List<string> executable = ExecutableIds(hud, false);
            int turns = run.RemainingTurns;
            int alert = run.Alert;
            int candy = run.Candy;
            if (executable.Count < 1)
                return false;
            hud.TickActionSelect(30f);
            ActionRow action;
            if (!database.TryGetAction(run.LastActionID, out action))
                return false;
            return hud.IsPresenting
                && !hud.TimerCountdownRunning
                && hud.TimerExpiryRecords == 1
                && run.LastActionID == executable[0]
                && run.RemainingTurns == turns - action.TurnCost + action.SuccessTurnDelta
                && run.Alert == alert + action.SuccessAlert
                && run.Candy == candy + action.SuccessCandy;
        }
        finally
        {
            Object.DestroyImmediate(hud.gameObject);
        }
    }

    static bool TestSingleExecutable(CsvDatabase database)
    {
        V02RunHud hud;
        RunState run;
        if (!TryHud(database, new FixedChanceRoll(true), null, 0, out hud, out run))
            return false;
        try
        {
            if (!run.ApplyTurnDelta(-(run.RemainingTurns - 1)))
                return false;
            hud.Bind(database, run, null, new FixedChanceRoll(true), null, null, new FixedIntRoll(0), new FixedIntRoll(1));
            List<string> executable = ExecutableIds(hud, false);
            List<string> shown = ShownIds(hud, false);
            if (executable.Count != 1 || shown.Count < 2 || !shown.Contains(executable[0]))
                return false;
            int turns = run.RemainingTurns;
            hud.TickActionSelect(30f);
            ActionRow picked;
            if (!database.TryGetAction(run.LastActionID, out picked))
                return false;
            return run.LastActionID == executable[0]
                && shown.Contains(executable[0])
                && run.RemainingTurns == turns - picked.TurnCost + picked.SuccessTurnDelta
                && hud.IsPresenting;
        }
        finally
        {
            Object.DestroyImmediate(hud.gameObject);
        }
    }

    static bool TestEqualRoll(CsvDatabase database)
    {
        TimeoutCandidate weighted;
        var three = new List<TimeoutCandidate>
        {
            new TimeoutCandidate { Id = "A", CanExecute = true, SpawnWeight = 100 },
            new TimeoutCandidate { Id = "B", CanExecute = false, SpawnWeight = 50 },
            new TimeoutCandidate { Id = "C", CanExecute = true, SpawnWeight = 1 }
        };
        if (!TimeoutChoice.TryPick(three, new FixedIntRoll(0), out weighted) || weighted.Id != "A")
            return false;
        if (!TimeoutChoice.TryPick(three, new FixedIntRoll(1), out weighted) || weighted.Id != "C")
            return false;
        if (TimeoutChoice.TryPick(three, new FixedIntRoll(2), out weighted) && weighted.Id == "B")
            return false;

        V02RunHud firstHud;
        RunState firstRun;
        V02RunHud secondHud;
        RunState secondRun;
        if (!TryHud(database, new FixedChanceRoll(true), null, 0, out firstHud, out firstRun))
            return false;
        if (!TryHud(database, new FixedChanceRoll(true), null, 1, out secondHud, out secondRun))
        {
            Object.DestroyImmediate(firstHud.gameObject);
            return false;
        }

        try
        {
            List<string> executable = ExecutableIds(firstHud, false);
            if (executable.Count != 2)
                return false;
            firstHud.TickActionSelect(30f);
            secondHud.TickActionSelect(30f);
            return firstRun.LastActionID == executable[0]
                && secondRun.LastActionID == executable[1]
                && firstRun.LastActionID != secondRun.LastActionID;
        }
        finally
        {
            Object.DestroyImmediate(firstHud.gameObject);
            Object.DestroyImmediate(secondHud.gameObject);
        }
    }

    static bool TestSkipsDisabled(CsvDatabase database)
    {
        var candidates = new List<TimeoutCandidate>
        {
            new TimeoutCandidate { Id = "BLOCKED", CanExecute = false, SpawnWeight = 99 },
            new TimeoutCandidate { Id = "ONLY", CanExecute = true, SpawnWeight = 1 }
        };
        TimeoutCandidate picked;
        if (!TimeoutChoice.TryPick(candidates, new FixedIntRoll(0), out picked) || picked.Id != "ONLY")
            return false;

        V02RunHud hud;
        RunState run;
        if (!TryHud(database, new FixedChanceRoll(false), null, 0, out hud, out run))
            return false;
        try
        {
            if (!run.ApplyTurnDelta(-(run.RemainingTurns - 1)))
                return false;
            hud.Bind(database, run, null, new FixedChanceRoll(false), null, null, new FixedIntRoll(0), new FixedIntRoll(0));
            List<string> executable = ExecutableIds(hud, false);
            List<string> shown = ShownIds(hud, false);
            if (executable.Count != 1)
                return false;
            string blocked = string.Empty;
            for (int i = 0; i < shown.Count; i++)
            {
                if (shown[i] != executable[0])
                    blocked = shown[i];
            }

            hud.TickActionSelect(30f);
            return run.LastActionID == executable[0]
                && run.LastActionID != blocked
                && hud.IsPresenting;
        }
        finally
        {
            Object.DestroyImmediate(hud.gameObject);
        }
    }

    static bool TestOneClaim(CsvDatabase database)
    {
        V02RunHud hud;
        RunState run;
        if (!TryHud(database, new FixedChanceRoll(true), null, 0, out hud, out run))
            return false;
        try
        {
            hud.TickActionSelect(30f);
            string action = run.LastActionID;
            int turns = run.RemainingTurns;
            if (!hud.IsPresenting || string.IsNullOrEmpty(action))
                return false;
            if (hud.TryChoose("ACT_003", false) || run.LastActionID != action || run.RemainingTurns != turns)
                return false;
        }
        finally
        {
            Object.DestroyImmediate(hud.gameObject);
        }

        if (!TryHud(database, new FixedChanceRoll(true), null, 0, out hud, out run))
            return false;
        try
        {
            hud.TickActionSelect(29f);
            if (!hud.TryChoose("ACT_003", false) || !hud.IsPresenting)
                return false;
            int turns = run.RemainingTurns;
            hud.TickActionSelect(5f);
            if (run.RemainingTurns != turns || run.LastActionID != "ACT_003" || hud.TimerExpiryRecords != 0)
                return false;
        }
        finally
        {
            Object.DestroyImmediate(hud.gameObject);
        }

        if (!TryHud(database, new FixedChanceRoll(true), null, 0, out hud, out run))
            return false;
        try
        {
            hud.TickActionSelect(29f);
            if (hud.TryChoose("ACT_999", false) || !hud.TimerCountdownRunning || !Near(hud.TimerRemaining, 1f))
                return false;
            hud.TickActionSelect(1f);
            return hud.IsPresenting && hud.TimerExpired && !string.IsNullOrEmpty(run.LastActionID);
        }
        finally
        {
            Object.DestroyImmediate(hud.gameObject);
        }
    }

    static bool TestEventTimerStarts(CsvDatabase database)
    {
        V02RunHud hud;
        RunState run;
        if (!OpenEvent(database, 0, out hud, out run))
            return false;
        try
        {
            return hud.ShowingEventOptions
                && hud.TimerSlotVisible
                && hud.TimerCountdownRunning
                && Near(hud.TimerRemaining, 30f)
                && hud.TimerSecondsLabel == "30초";
        }
        finally
        {
            Object.DestroyImmediate(hud.gameObject);
        }
    }

    static bool TestEventTimeout(CsvDatabase database)
    {
        V02RunHud first;
        RunState firstRun;
        V02RunHud second;
        RunState secondRun;
        if (!OpenEvent(database, 0, out first, out firstRun))
            return false;
        if (!OpenEvent(database, 1, out second, out secondRun))
        {
            Object.DestroyImmediate(first.gameObject);
            return false;
        }

        try
        {
            List<string> firstOptions = ExecutableIds(first, true);
            List<string> secondOptions = ExecutableIds(second, true);
            if (firstOptions.Count != 2 || secondOptions.Count != 2)
                return false;
            int firstCandy = firstRun.Candy;
            int secondCandy = secondRun.Candy;
            int firstTurns = firstRun.RemainingTurns;
            int secondTurns = secondRun.RemainingTurns;
            first.TickActionSelect(30f);
            second.TickActionSelect(30f);
            return first.IsPresenting
                && second.IsPresenting
                && firstRun.HasSucceededEvent("EVT_001")
                && secondRun.HasSucceededEvent("EVT_001")
                && !firstRun.HasPendingEvent
                && !secondRun.HasPendingEvent
                && OptionDeltas(database, firstOptions[0], firstCandy, firstTurns, firstRun)
                && OptionDeltas(database, secondOptions[1], secondCandy, secondTurns, secondRun);
        }
        finally
        {
            Object.DestroyImmediate(first.gameObject);
            Object.DestroyImmediate(second.gameObject);
        }
    }

    static bool TestEventCompleteStops(CsvDatabase database)
    {
        V02RunHud hud;
        RunState run;
        if (!OpenEvent(database, 0, out hud, out run))
            return false;
        try
        {
            int turns = run.RemainingTurns;
            int candy = run.Candy;
            int alert = run.Alert;
            if (!hud.TryChoose("OPT_001", true) || !hud.IsPresenting)
                return false;
            hud.TickActionSelect(20f);
            return !hud.TimerSlotVisible
                && !hud.TimerCountdownRunning
                && !hud.TryChoose("OPT_002", true)
                && run.RemainingTurns == turns
                && run.Candy == candy
                && run.Alert == alert
                && run.HasSucceededEvent("EVT_001");
        }
        finally
        {
            Object.DestroyImmediate(hud.gameObject);
        }
    }

    static bool TestEventRefreshKeepsTime(CsvDatabase database)
    {
        V02RunHud hud;
        RunState run;
        if (!OpenEvent(database, 0, out hud, out run))
            return false;
        try
        {
            hud.TickActionSelect(3f);
            hud.Refresh();
            return hud.ShowingEventOptions
                && hud.TimerCountdownRunning
                && Near(hud.TimerRemaining, 27f)
                && !hud.IsPresenting;
        }
        finally
        {
            Object.DestroyImmediate(hud.gameObject);
        }
    }

    static bool TestConfirmPauses(CsvDatabase database)
    {
        V02RunHud hud;
        RunState run;
        if (!TryHud(database, new FixedChanceRoll(true), null, 0, out hud, out run))
            return false;
        try
        {
            hud.TickActionSelect(25f);
            hud.LobbyButton.onClick.Invoke();
            hud.TickActionSelect(10f);
            if (!hud.AbandonConfirmOpen || hud.TimerExpired || hud.IsPresenting || !Near(hud.TimerRemaining, 5f))
                return false;
            hud.AbandonConfirm.CancelButton.onClick.Invoke();
            if (hud.AbandonConfirmOpen || !hud.TimerCountdownRunning)
                return false;
            hud.TickActionSelect(5f);
            return hud.IsPresenting && hud.TimerExpired && !string.IsNullOrEmpty(run.LastActionID);
        }
        finally
        {
            Object.DestroyImmediate(hud.gameObject);
        }
    }

    static bool TestResultPopupsStop(CsvDatabase database)
    {
        V02RunHud hud;
        RunState run;
        if (!TryHud(database, new FixedChanceRoll(false), null, 0, out hud, out run))
            return false;
        try
        {
            if (!hud.TryChoose("ACT_003", false) || !hud.IsPresenting)
                return false;
            int turns = run.RemainingTurns;
            hud.TickActionSelect(40f);
            if (hud.TimerSlotVisible || hud.TimerCountdownRunning || run.RemainingTurns != turns || hud.TimerExpiryRecords != 0)
                return false;
        }
        finally
        {
            Object.DestroyImmediate(hud.gameObject);
        }

        if (!TryHud(database, new FixedChanceRoll(true), null, 0, out hud, out run))
            return false;
        try
        {
            if (!run.ApplyCandyDelta(database.GetInt("MIN_CANDY_TO_ESCAPE")) || !run.SetPhase("ESCAPE"))
                return false;
            hud.Bind(database, run, null, new FixedChanceRoll(true), null, null, new FixedIntRoll(0), new FixedIntRoll(0));
            if (!hud.TryChoose("ACT_010", false))
                return false;
            hud.ResultPresentation.SkipToHold();
            if (!hud.TryContinueResult())
                return false;
            string action = run.LastActionID;
            hud.TickActionSelect(40f);
            return hud.OperationResult != null
                && hud.OperationResult.IsOpen
                && !hud.TimerSlotVisible
                && !hud.TimerCountdownRunning
                && run.Status == RunStatus.Won
                && run.LastActionID == action
                && hud.TimerExpiryRecords == 0;
        }
        finally
        {
            Object.DestroyImmediate(hud.gameObject);
        }
    }

    static bool TestTooltipThenTimeout(CsvDatabase database)
    {
        V02RunHud hud;
        RunState run;
        if (!TryHud(database, new FixedChanceRoll(true), null, 0, out hud, out run))
            return false;
        try
        {
            V02ChoiceButton choice = FindChoice(hud);
            if (choice == null)
                return false;
            Transform info = choice.transform.Find("CandyInfo");
            Button button = info != null ? info.GetComponent<Button>() : null;
            if (button == null)
                return false;
            button.onClick.Invoke();
            if (!choice.TooltipOpen)
                return false;
            hud.TickActionSelect(30f);
            return !choice.TooltipOpen
                && hud.IsPresenting
                && !string.IsNullOrEmpty(run.LastActionID)
                && hud.TimerExpired;
        }
        finally
        {
            Object.DestroyImmediate(hud.gameObject);
        }
    }

    static bool TestEmptyCandidates(CsvDatabase database)
    {
        TimeoutCandidate ignored;
        if (TimeoutChoice.TryPick(new List<TimeoutCandidate>
        {
            new TimeoutCandidate { Id = "NO", CanExecute = false, SpawnWeight = 5 }
        }, new FixedIntRoll(0), out ignored))
            return false;

        V02RunHud hud;
        RunState run;
        if (!TryHud(database, new FixedChanceRoll(true), null, 0, out hud, out run))
            return false;
        try
        {
            if (!run.SetPhase("RESULT"))
                return false;
            hud.Bind(database, run, null, new FixedChanceRoll(true), null, null, new FixedIntRoll(0), new FixedIntRoll(0));
            if (ExecutableIds(hud, false).Count != 0)
                return false;
            int turns = run.RemainingTurns;
            int alert = run.Alert;
            int candy = run.Candy;
            hud.TickActionSelect(30f);
            hud.TickActionSelect(10f);
            hud.Refresh();
            return hud.TimerEmptyRecords == 1
                && hud.TimerExpired
                && !hud.TimerCountdownRunning
                && !hud.IsPresenting
                && string.IsNullOrEmpty(run.LastActionID)
                && run.Status == RunStatus.InProgress
                && run.FailReason == RunFailReason.None
                && run.RemainingTurns == turns
                && run.Alert == alert
                && run.Candy == candy
                && !hud.TryChoose("ACT_001", false);
        }
        finally
        {
            Object.DestroyImmediate(hud.gameObject);
        }
    }

    static bool TestNewRoundResets(CsvDatabase database)
    {
        V02RunHud hud;
        RunState run;
        if (!TryHud(database, new FixedChanceRoll(false), null, 0, out hud, out run))
            return false;
        try
        {
            hud.TickActionSelect(6f);
            if (!hud.TryChoose("ACT_003", false) || !hud.IsPresenting || hud.TimerSlotVisible)
                return false;
            hud.TickActionSelect(10f);
            if (!Near(hud.TimerRemaining, 24f))
                return false;
            hud.ResultPresentation.SkipToHold();
            if (!hud.TryContinueResult())
                return false;
            return !run.HasPendingEvent
                && hud.TimerSlotVisible
                && hud.TimerCountdownRunning
                && Near(hud.TimerRemaining, 30f)
                && hud.TimerSecondsLabel == "30초"
                && !hud.TimerExpired;
        }
        finally
        {
            Object.DestroyImmediate(hud.gameObject);
        }
    }

    static bool OpenEvent(CsvDatabase database, int timeoutIndex, out V02RunHud hud, out RunState run)
    {
        hud = null;
        run = null;
        if (!TryHud(database, new FixedChanceRoll(true), new FixedEventPick("EVT_001"), timeoutIndex, out hud, out run))
            return false;
        hud.TickActionSelect(10f);
        if (!hud.TryChoose("ACT_001", false) || !hud.IsPresenting || Near(hud.TimerRemaining, 20f) == false)
        {
            Object.DestroyImmediate(hud.gameObject);
            hud = null;
            return false;
        }

        hud.ResultPresentation.SkipToHold();
        if (!hud.TryContinueResult() || !hud.ShowingEventOptions || !Near(hud.TimerRemaining, 30f))
        {
            Object.DestroyImmediate(hud.gameObject);
            hud = null;
            return false;
        }

        return true;
    }

    static bool OptionDeltas(CsvDatabase database, string optionId, int candyBefore, int turnsBefore, RunState run)
    {
        EventOptionRow option;
        if (!database.TryGetEventOption(optionId, out option))
            return false;
        return run.Candy == candyBefore + option.SuccessCandy
            && run.Alert == option.SuccessAlert
            && run.RemainingTurns == turnsBefore - option.TurnCost + option.SuccessTurnDelta;
    }

    static List<string> ExecutableIds(V02RunHud hud, bool options)
    {
        var ids = new List<string>();
        V02ChoiceButton[] buttons = hud.GetComponentsInChildren<V02ChoiceButton>(true);
        for (int i = 0; i < buttons.Length; i++)
        {
            if (buttons[i] == null || !buttons[i].gameObject.activeInHierarchy || buttons[i].IsOption != options)
                continue;
            Button button = buttons[i].GetComponent<Button>();
            if (button != null && button.interactable)
                ids.Add(buttons[i].BoundId);
        }

        return ids;
    }

    static List<string> ShownIds(V02RunHud hud, bool options)
    {
        var ids = new List<string>();
        V02ChoiceButton[] buttons = hud.GetComponentsInChildren<V02ChoiceButton>(true);
        for (int i = 0; i < buttons.Length; i++)
        {
            if (buttons[i] == null || !buttons[i].gameObject.activeInHierarchy || buttons[i].IsOption != options)
                continue;
            ids.Add(buttons[i].BoundId);
        }

        return ids;
    }

    static V02ChoiceButton FindChoice(V02RunHud hud)
    {
        V02ChoiceButton[] buttons = hud.GetComponentsInChildren<V02ChoiceButton>(true);
        for (int i = 0; i < buttons.Length; i++)
        {
            if (buttons[i] != null && buttons[i].gameObject.activeInHierarchy && !buttons[i].IsOption)
                return buttons[i];
        }

        return null;
    }

    static bool Near(float value, float expected)
    {
        return Mathf.Abs(value - expected) < 0.001f;
    }

    static bool TryHud(CsvDatabase database, IChanceRoll chance, IEventPick events, int timeoutIndex, out V02RunHud hud, out RunState run)
    {
        hud = null;
        run = null;
        V02RunHud prefab = Resources.Load<V02RunHud>(V02RunHud.ResourcePath);
        if (prefab == null || !RunState.TryStart(database, "SHOP_001", out run))
            return false;
        hud = Object.Instantiate(prefab);
        hud.Bind(database, run, null, chance, events, null, new FixedIntRoll(0), new FixedIntRoll(timeoutIndex));
        return true;
    }

#if UNITY_EDITOR
    [UnityEditor.MenuItem("Candy Thief/V02/Run Timeout Auto Select Tests")]
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
