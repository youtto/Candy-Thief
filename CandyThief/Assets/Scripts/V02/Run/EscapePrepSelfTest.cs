using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public static class EscapePrepSelfTest
{
    const string LogPrefix = "[EscapePrep] ";

    public static bool RunAll(CsvDatabase database)
    {
        if (database == null || !database.Loaded || database.HasErrors)
        {
            Debug.LogError(LogPrefix + "자가 검사 실패: CsvDatabase가 준비되지 않았습니다.");
            return false;
        }

        RunState previous = RunState.Current;
        int failed = 0;
        failed += Expect("1 LOOT 완료와 ESCAPE 진입에 사탕 조건이 없음", TestPhaseGate(database));
        failed += Expect("2 사탕이 있으면 판정 없이 ESCAPE", TestImmediateEscape(database));
        failed += Expect("3 사탕 0개는 확인 팝업", TestEmptyPrompt(database));
        failed += Expect("4 빈손 확인은 ESCAPE", TestEmptyConfirm(database));
        failed += Expect("5 빈손 취소는 상태와 난수를 유지", TestEmptyCancel(database));
        failed += Expect("6 MOVE와 INFILTRATE는 안내만", TestEarlyNotice(database));
        failed += Expect("7 ESCAPE·이벤트·종료는 버튼 비활성", TestBlocked(database));
        failed += Expect("8 ACT_013은 남고 일반 손패에는 없음", TestAct013StaysOut(database));

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

    static bool TestPhaseGate(CsvDatabase database)
    {
        PhaseRow loot;
        PhaseRow escape;
        if (!database.TryGetPhase("LOOT", out loot) || !database.TryGetPhase("ESCAPE", out escape))
            return false;
        if (loot.CompleteCondition != ConditionKey.ALWAYS || loot.NextPhaseID != "ESCAPE")
            return false;
        if (escape.EntryCondition != ConditionKey.ALWAYS)
            return false;

        RunState blocked;
        if (!RunState.TryStart(database, "SHOP_001", out blocked) || blocked.TryPrepareEscape())
            return false;

        RunState ready;
        if (!RunState.TryStart(database, "SHOP_001", out ready) || !ready.SetPhase("LOOT"))
            return false;
        int turns = ready.RemainingTurns;
        int alert = ready.Alert;
        int epoch = ready.ChoiceEpoch;
        if (!ready.TryPrepareEscape())
            return false;
        return ready.CurrentPhase == "ESCAPE"
            && ready.Candy == 0
            && ready.RemainingTurns == turns
            && ready.Alert == alert
            && ready.ChoiceEpoch == epoch
            && ready.Status == RunStatus.InProgress
            && string.IsNullOrEmpty(ready.LastActionID);
    }

    static bool TestImmediateEscape(CsvDatabase database)
    {
        V02RunHud hud;
        RunState run;
        if (!TryHud(database, out hud, out run))
            return false;
        try
        {
            if (!run.SetPhase("LOOT") || !run.ApplyCandyDelta(3))
                return false;
            hud.Refresh();
            int turns = run.RemainingTurns;
            int alert = run.Alert;
            int candy = run.Candy;
            int epoch = run.ChoiceEpoch;
            if (hud.EscapePrepButton == null || !hud.EscapePrepButton.interactable)
                return false;
            hud.EscapePrepButton.onClick.Invoke();
            string[] ids = ActiveActionIds(hud);
            return run.CurrentPhase == "ESCAPE"
                && run.RemainingTurns == turns
                && run.Alert == alert
                && run.Candy == candy
                && run.ChoiceEpoch == epoch
                && run.LastActionResult == RunActionResult.None
                && !hud.IsPresenting
                && !hud.EmptyHandConfirmOpen
                && hud.PhaseLabel == "탈출"
                && ids.Length == 2
                && ids[0] != ids[1]
                && AllEscape(database, ids)
                && !hud.EscapePrepButton.interactable;
        }
        finally
        {
            Object.DestroyImmediate(hud.gameObject);
        }
    }

    static bool TestEmptyPrompt(CsvDatabase database)
    {
        V02RunHud hud;
        RunState run;
        if (!TryHud(database, out hud, out run))
            return false;
        try
        {
            if (!run.SetPhase("LOOT"))
                return false;
            hud.Refresh();
            hud.EscapePrepButton.onClick.Invoke();
            return hud.EmptyHandConfirmOpen
                && hud.EmptyHandConfirm.Title == V02EmptyHandEscapeConfirm.TitleCopy
                && hud.EmptyHandConfirm.Body == V02EmptyHandEscapeConfirm.BodyCopy
                && run.CurrentPhase == "LOOT"
                && run.Candy == 0
                && !hud.IsPresenting;
        }
        finally
        {
            Object.DestroyImmediate(hud.gameObject);
        }
    }

    static bool TestEmptyConfirm(CsvDatabase database)
    {
        V02RunHud hud;
        RunState run;
        if (!TryHud(database, out hud, out run))
            return false;
        try
        {
            if (!run.SetPhase("LOOT"))
                return false;
            hud.Refresh();
            int turns = run.RemainingTurns;
            hud.EscapePrepButton.onClick.Invoke();
            if (!hud.EmptyHandConfirmOpen)
                return false;
            hud.EmptyHandConfirm.LeaveButton.onClick.Invoke();
            string[] ids = ActiveActionIds(hud);
            return run.CurrentPhase == "ESCAPE"
                && run.Candy == 0
                && run.RemainingTurns == turns
                && run.Status == RunStatus.InProgress
                && !hud.EmptyHandConfirmOpen
                && !hud.IsPresenting
                && ids.Length == 2
                && AllEscape(database, ids);
        }
        finally
        {
            Object.DestroyImmediate(hud.gameObject);
        }
    }

    static bool TestEmptyCancel(CsvDatabase database)
    {
        V02RunHud hud;
        RunState run;
        if (!TryHud(database, out hud, out run))
            return false;
        try
        {
            if (!run.SetPhase("LOOT"))
                return false;
            hud.Refresh();
            int turns = run.RemainingTurns;
            int alert = run.Alert;
            int candy = run.Candy;
            string phase = run.CurrentPhase;
            UnityEngine.Random.State before = UnityEngine.Random.state;
            hud.EscapePrepButton.onClick.Invoke();
            hud.EmptyHandConfirm.StayButton.onClick.Invoke();
            UnityEngine.Random.State after = UnityEngine.Random.state;
            return !hud.EmptyHandConfirmOpen
                && !hud.ChoicesLocked
                && run.CurrentPhase == phase
                && run.RemainingTurns == turns
                && run.Alert == alert
                && run.Candy == candy
                && run.Status == RunStatus.InProgress
                && before.Equals(after);
        }
        finally
        {
            Object.DestroyImmediate(hud.gameObject);
        }
    }

    static bool TestEarlyNotice(CsvDatabase database)
    {
        V02RunHud hud;
        RunState run;
        if (!TryHud(database, out hud, out run))
            return false;
        try
        {
            hud.EscapePrepButton.onClick.Invoke();
            if (!hud.EscapePrepNoticeOpen || run.CurrentPhase != "MOVE" || hud.EmptyHandConfirmOpen)
                return false;
            hud.EscapePrepNotice.ConfirmButton.onClick.Invoke();
            if (hud.EscapePrepNoticeOpen || !run.SetPhase("INFILTRATE"))
                return false;
            hud.Refresh();
            hud.EscapePrepButton.onClick.Invoke();
            return hud.EscapePrepNoticeOpen
                && run.CurrentPhase == "INFILTRATE"
                && !hud.EmptyHandConfirmOpen
                && hud.EscapePrepNotice.ConfirmButton != null;
        }
        finally
        {
            Object.DestroyImmediate(hud.gameObject);
        }
    }

    static bool TestBlocked(CsvDatabase database)
    {
        V02RunHud hud;
        RunState run;
        if (!TryHud(database, out hud, out run))
            return false;
        try
        {
            if (!run.SetPhase("ESCAPE"))
                return false;
            hud.Refresh();
            hud.EscapePrepButton.onClick.Invoke();
            if (hud.EscapePrepButton.interactable || run.CurrentPhase != "ESCAPE")
                return false;

            if (!run.SetPhase("LOOT") || !run.TrySetPendingEvent("EVT_001"))
                return false;
            hud.Refresh();
            hud.EscapePrepButton.onClick.Invoke();
            if (hud.EscapePrepButton.interactable || run.CurrentPhase != "LOOT" || hud.EmptyHandConfirmOpen)
                return false;
            run.ClearPendingEvent();

            if (!run.ApplyAlertDelta(database.GetInt("MAX_ALERT")))
                return false;
            hud.Refresh();
            hud.EscapePrepButton.onClick.Invoke();
            return run.IsEnded
                && !hud.EscapePrepButton.interactable
                && !hud.EmptyHandConfirmOpen
                && !run.TryPrepareEscape();
        }
        finally
        {
            Object.DestroyImmediate(hud.gameObject);
        }
    }

    static bool TestAct013StaysOut(CsvDatabase database)
    {
        ActionRow hidden;
        if (!database.TryGetAction("ACT_013", out hidden)
            || hidden.SpawnWeight != 0
            || hidden.ShowCondition != ConditionKey.CANDY_AT_LEAST_1)
            return false;

        V02RunHud hud;
        RunState run;
        if (!TryHud(database, out hud, out run))
            return false;
        try
        {
            if (!run.SetPhase("LOOT") || !run.ApplyCandyDelta(4))
                return false;
            hud.Refresh();
            string[] loot = ActiveActionIds(hud);
            if (loot.Length != 2 || Contains(loot, "ACT_013"))
                return false;
            hud.EscapePrepButton.onClick.Invoke();
            string[] escape = ActiveActionIds(hud);
            return escape.Length == 2 && !Contains(escape, "ACT_013") && AllEscape(database, escape);
        }
        finally
        {
            Object.DestroyImmediate(hud.gameObject);
        }
    }

    static bool AllEscape(CsvDatabase database, string[] ids)
    {
        for (int i = 0; i < ids.Length; i++)
        {
            ActionRow action;
            if (!database.TryGetAction(ids[i], out action) || action.PhaseID != "ESCAPE")
                return false;
        }

        return true;
    }

    static bool Contains(string[] ids, string id)
    {
        for (int i = 0; i < ids.Length; i++)
        {
            if (ids[i] == id)
                return true;
        }

        return false;
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

    static bool TryHud(CsvDatabase database, out V02RunHud hud, out RunState run)
    {
        hud = null;
        run = null;
        V02RunHud prefab = Resources.Load<V02RunHud>(V02RunHud.ResourcePath);
        if (prefab == null || !RunState.TryStart(database, "SHOP_001", out run))
            return false;
        hud = Object.Instantiate(prefab);
        hud.Bind(database, run, null, new FixedChanceRoll(false), null, null, new FixedIntRoll(0));
        return hud.EscapePrepButton != null;
    }

#if UNITY_EDITOR
    [UnityEditor.MenuItem("Candy Thief/V02/Run EscapePrep Tests")]
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
