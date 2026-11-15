using UnityEngine;
using UnityEngine.UI;

public static class ActionSelectTimerSelfTest
{
    const string LogPrefix = "[ActionSelectTimer] ";

    public static bool RunAll(CsvDatabase database)
    {
        if (database == null || !database.Loaded || database.HasErrors)
        {
            Debug.LogError(LogPrefix + "자가 검사 실패: CsvDatabase가 준비되지 않았습니다.");
            return false;
        }

        RunState previous = RunState.Current;
        int failed = 0;
        failed += Expect("1 행동 선택만 감소하고 Refresh는 초기화하지 않음", TestCountdownAndRefresh(database));
        failed += Expect("2 툴팁은 남은 시간을 초기화하지 않음", TestTooltipDoesNotReset(database));
        failed += Expect("3 포기 확인은 보이되 멈추고 취소 후 재개", TestAbandonPauses(database));
        failed += Expect("4 행동 결과 중에는 숨기고 멈추며 다음 라운드만 초기화", TestActionResultThenNewRound(database));
        failed += Expect("5 이벤트 선택은 새 30초이고 갱신해도 유지", TestEventOptionsPause(database));
        failed += Expect("6 작전 결과는 숨기고 멈춤", TestOperationResultPauses(database));
        failed += Expect("7 만료는 자동 선택이고 추가 입력은 막힘", TestExpiryFreezes(database));
        failed += Expect("8 탈출 안내와 빈손 확인은 멈추고 취소 후 재개", TestChoiceBlockersPause(database));

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

    static bool TestCountdownAndRefresh(CsvDatabase database)
    {
        V02RunHud hud;
        RunState run;
        if (!TryHud(database, null, null, out hud, out run))
            return false;
        try
        {
            if (!AtFull(hud) || !hud.TimerCountdownRunning)
                return false;
            hud.TickActionSelect(0.1f);
            if (!Near(hud.TimerRemaining, 29.9f) || hud.TimerSecondsLabel != "30초" || hud.TimerSlotFillAmount >= 0.999f)
                return false;
            hud.TickActionSelect(4.9f);
            if (!Near(hud.TimerRemaining, 25f) || hud.TimerSecondsLabel != "25초")
                return false;
            int turns = run.RemainingTurns;
            int alert = run.Alert;
            int candy = run.Candy;
            hud.Refresh();
            if (!hud.TimerSlotVisible || !hud.TimerCountdownRunning || !Near(hud.TimerRemaining, 25f))
                return false;
            if (hud.TryChoose("ACT_999", false) || !hud.TimerCountdownRunning || !Near(hud.TimerRemaining, 25f))
                return false;
            hud.TickActionSelect(1f);
            return Near(hud.TimerRemaining, 24f)
                && hud.TimerSecondsLabel == "24초"
                && run.RemainingTurns == turns
                && run.Alert == alert
                && run.Candy == candy
                && run.Status == RunStatus.InProgress;
        }
        finally
        {
            Object.DestroyImmediate(hud.gameObject);
        }
    }

    static bool TestTooltipDoesNotReset(CsvDatabase database)
    {
        V02RunHud hud;
        RunState run;
        if (!TryHud(database, null, null, out hud, out run))
            return false;
        try
        {
            hud.TickActionSelect(3f);
            V02ChoiceButton choice = FindChoice(hud);
            if (choice == null)
                return false;
            Transform info = choice.transform.Find("CandyInfo");
            Button button = info != null ? info.GetComponent<Button>() : null;
            if (button == null)
                return false;
            button.onClick.Invoke();
            if (!choice.TooltipOpen || !Near(hud.TimerRemaining, 27f))
                return false;
            hud.Refresh();
            if (!Near(hud.TimerRemaining, 27f) || !hud.TimerCountdownRunning)
                return false;
            hud.TickActionSelect(1f);
            return Near(hud.TimerRemaining, 26f) && hud.TimerSecondsLabel == "26초";
        }
        finally
        {
            Object.DestroyImmediate(hud.gameObject);
        }
    }

    static bool TestAbandonPauses(CsvDatabase database)
    {
        V02RunHud hud;
        RunState run;
        if (!TryHud(database, null, null, out hud, out run))
            return false;
        try
        {
            hud.TickActionSelect(6f);
            hud.LobbyButton.onClick.Invoke();
            if (!hud.AbandonConfirmOpen || !hud.TimerSlotVisible || hud.TimerCountdownRunning)
                return false;
            hud.TickActionSelect(5f);
            if (!Near(hud.TimerRemaining, 24f) || hud.TimerExpiryRecords != 0)
                return false;
            hud.AbandonConfirm.CancelButton.onClick.Invoke();
            if (hud.AbandonConfirmOpen || !hud.TimerCountdownRunning || !Near(hud.TimerRemaining, 24f))
                return false;
            hud.TickActionSelect(2f);
            return Near(hud.TimerRemaining, 22f)
                && hud.TimerSlotVisible
                && run.Status == RunStatus.InProgress
                && run.FailReason == RunFailReason.None;
        }
        finally
        {
            Object.DestroyImmediate(hud.gameObject);
        }
    }

    static bool TestActionResultThenNewRound(CsvDatabase database)
    {
        V02RunHud hud;
        RunState run;
        if (!TryHud(database, new FixedChanceRoll(false), null, out hud, out run))
            return false;
        try
        {
            hud.TickActionSelect(8f);
            if (!hud.TryChoose("ACT_003", false) || !hud.IsPresenting)
                return false;
            if (hud.TimerSlotVisible || hud.TimerCountdownRunning || !Near(hud.TimerRemaining, 22f))
                return false;
            hud.TickActionSelect(3f);
            if (!Near(hud.TimerRemaining, 22f) || hud.TimerSecondsLabel != "22초")
                return false;
            hud.ResultPresentation.SkipToHold();
            if (!hud.TryContinueResult())
                return false;
            return !hud.IsPresenting
                && !run.HasPendingEvent
                && run.CurrentPhase == "MOVE"
                && hud.TimerSlotVisible
                && hud.TimerCountdownRunning
                && AtFull(hud);
        }
        finally
        {
            Object.DestroyImmediate(hud.gameObject);
        }
    }

    static bool TestEventOptionsPause(CsvDatabase database)
    {
        V02RunHud hud;
        RunState run;
        if (!TryHud(database, new FixedChanceRoll(true), new FixedEventPick("EVT_001"), out hud, out run))
            return false;
        try
        {
            hud.TickActionSelect(7f);
            if (!hud.TryChoose("ACT_001", false) || !hud.IsPresenting)
                return false;
            hud.TickActionSelect(2f);
            if (hud.TimerSlotVisible || hud.TimerCountdownRunning || !Near(hud.TimerRemaining, 23f))
                return false;
            hud.ResultPresentation.SkipToHold();
            if (!hud.TryContinueResult() || !hud.ShowingEventOptions || !AtFull(hud) || !hud.TimerCountdownRunning)
                return false;
            hud.TickActionSelect(4f);
            if (!Near(hud.TimerRemaining, 26f) || !hud.TimerCountdownRunning)
                return false;
            hud.Refresh();
            if (!hud.ShowingEventOptions || !Near(hud.TimerRemaining, 26f))
                return false;
            if (!hud.TryChoose("OPT_001", true) || !hud.IsPresenting || hud.TimerSlotVisible || hud.TimerCountdownRunning)
                return false;
            hud.TickActionSelect(1f);
            if (!Near(hud.TimerRemaining, 26f))
                return false;
            hud.ResultPresentation.SkipToHold();
            if (!hud.TryContinueResult())
                return false;
            return !hud.ShowingEventOptions
                && hud.TimerSlotVisible
                && hud.TimerCountdownRunning
                && AtFull(hud)
                && run.Status == RunStatus.InProgress;
        }
        finally
        {
            Object.DestroyImmediate(hud.gameObject);
        }
    }

    static bool TestOperationResultPauses(CsvDatabase database)
    {
        V02RunHud hud;
        RunState run;
        if (!TryHud(database, new FixedChanceRoll(true), null, out hud, out run))
            return false;
        try
        {
            if (!run.ApplyCandyDelta(database.GetInt("MIN_CANDY_TO_ESCAPE")) || !run.SetPhase("ESCAPE"))
                return false;
            hud.Bind(database, run, null, new FixedChanceRoll(true), null, null, new FixedIntRoll(0));
            hud.TickActionSelect(4f);
            if (!hud.TryChoose("ACT_010", false) || !hud.IsPresenting || hud.TimerSlotVisible)
                return false;
            hud.TickActionSelect(2f);
            if (!Near(hud.TimerRemaining, 26f))
                return false;
            hud.ResultPresentation.SkipToHold();
            if (!hud.TryContinueResult())
                return false;
            if (hud.OperationResult == null || !hud.OperationResult.IsOpen || hud.IsPresenting)
                return false;
            hud.TickActionSelect(9f);
            return !hud.TimerSlotVisible
                && !hud.TimerCountdownRunning
                && Near(hud.TimerRemaining, 26f)
                && hud.TimerExpiryRecords == 0
                && run.Status == RunStatus.Won
                && run.FailReason == RunFailReason.None;
        }
        finally
        {
            Object.DestroyImmediate(hud.gameObject);
        }
    }

    static bool TestExpiryFreezes(CsvDatabase database)
    {
        V02RunHud hud;
        RunState run;
        if (!TryHud(database, new FixedChanceRoll(true), null, out hud, out run))
            return false;
        try
        {
            int turns = run.RemainingTurns;
            hud.TickActionSelect(29f);
            if (hud.TimerExpired || hud.TimerExpiryRecords != 0 || hud.TimerSecondsLabel != "1초")
                return false;
            hud.TickActionSelect(1f);
            if (!hud.TimerExpired || hud.TimerExpiryRecords != 1 || hud.TimerCountdownRunning || !hud.IsPresenting)
                return false;
            if (hud.TimerSlotVisible || hud.TimerSecondsLabel != "0초" || run.RemainingTurns == turns)
                return false;
            int after = run.RemainingTurns;
            string action = run.LastActionID;
            hud.TickActionSelect(10f);
            return !hud.TryChoose("ACT_003", false)
                && run.RemainingTurns == after
                && run.LastActionID == action
                && hud.TimerExpiryRecords == 1;
        }
        finally
        {
            Object.DestroyImmediate(hud.gameObject);
        }
    }

    static bool TestChoiceBlockersPause(CsvDatabase database)
    {
        V02RunHud hud;
        RunState run;
        if (!TryHud(database, null, null, out hud, out run))
            return false;
        try
        {
            int turns = run.RemainingTurns;
            hud.TickActionSelect(5f);
            hud.EscapePrepButton.onClick.Invoke();
            if (!hud.EscapePrepNoticeOpen || !hud.TimerSlotVisible || hud.TimerCountdownRunning)
                return false;
            hud.TickActionSelect(2f);
            if (!Near(hud.TimerRemaining, 25f))
                return false;
            hud.EscapePrepNotice.ConfirmButton.onClick.Invoke();
            if (hud.EscapePrepNoticeOpen || !hud.TimerCountdownRunning || !Near(hud.TimerRemaining, 25f))
                return false;
            if (!run.SetPhase("LOOT"))
                return false;
            hud.Refresh();
            if (!AtFull(hud))
                return false;
            hud.TickActionSelect(6f);
            hud.EscapePrepButton.onClick.Invoke();
            if (!hud.EmptyHandConfirmOpen || !hud.TimerSlotVisible || hud.TimerCountdownRunning)
                return false;
            hud.TickActionSelect(3f);
            if (!Near(hud.TimerRemaining, 24f) || run.RemainingTurns != turns || run.Candy != 0)
                return false;
            hud.EmptyHandConfirm.StayButton.onClick.Invoke();
            if (hud.EmptyHandConfirmOpen || run.CurrentPhase != "LOOT" || !Near(hud.TimerRemaining, 24f))
                return false;
            hud.TickActionSelect(1f);
            return Near(hud.TimerRemaining, 23f)
                && hud.TimerCountdownRunning
                && run.Status == RunStatus.InProgress
                && run.Candy == 0
                && run.RemainingTurns == turns;
        }
        finally
        {
            Object.DestroyImmediate(hud.gameObject);
        }
    }

    static bool AtFull(V02RunHud hud)
    {
        return hud.TimerSlotVisible
            && Near(hud.TimerRemaining, 30f)
            && hud.TimerSecondsLabel == "30초"
            && hud.TimerSlotFillAmount >= 0.99f;
    }

    static bool Near(float value, float expected)
    {
        return Mathf.Abs(value - expected) < 0.001f;
    }

    static V02ChoiceButton FindChoice(V02RunHud hud)
    {
        V02ChoiceButton[] buttons = hud.GetComponentsInChildren<V02ChoiceButton>(true);
        for (int i = 0; i < buttons.Length; i++)
        {
            if (buttons[i] != null && buttons[i].gameObject.activeInHierarchy)
                return buttons[i];
        }

        return null;
    }

    static bool TryHud(CsvDatabase database, IChanceRoll chance, IEventPick events, out V02RunHud hud, out RunState run)
    {
        hud = null;
        run = null;
        V02RunHud prefab = Resources.Load<V02RunHud>(V02RunHud.ResourcePath);
        if (prefab == null || !RunState.TryStart(database, "SHOP_001", out run))
            return false;
        hud = Object.Instantiate(prefab);
        hud.Bind(database, run, null, chance, events, null, new FixedIntRoll(0), new FixedIntRoll(0));
        return true;
    }

#if UNITY_EDITOR
    [UnityEditor.MenuItem("Candy Thief/V02/Run Action Select Timer Tests")]
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
