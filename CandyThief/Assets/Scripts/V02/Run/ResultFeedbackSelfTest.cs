using System.Collections.Generic;
using UnityEngine;

public static class ResultFeedbackSelfTest
{
    public const string LogPrefix = "[ResultFeedback] ";

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
            Debug.LogError(LogPrefix + "자가 검사 실패: CsvDatabase가 준비되지 않았습니다.");
            return false;
        }

        RunState previous = RunState.Current;
        int failed = 0;
        failed += Expect("1 모든 ActionID 성공·실패 문구", TestAllActionsMapped(database));
        failed += Expect("2 모든 OptionID 성공·실패 문구", TestAllOptionsMapped(database));
        failed += Expect("3 누락 행은 기본 문구", TestMissingUsesDefault());
        failed += Expect("4 9개 게임플레이 테이블만으로 HasErrors 없음", TestGameplayLoadUnaffected());
        failed += Expect("5 잘못된 연출 테이블은 HasErrors를 올리지 않음", TestBadPresentationDoesNotError());
        failed += Expect("6 스냅샷 변화량은 CSV 재계산이 아님", TestSnapshotDeltas(database));
        failed += Expect("7 스킵은 긴장·공개 중에만", TestSkipPhases());
        failed += Expect("8 [계속]은 홀드에서만", TestContinueOnlyOnHold());
        failed += Expect("9 행동 결과 후 Continue 이전에는 이벤트 UI 숨김", TestPendingUiAfterContinue(database));
        failed += Expect("10 연출 중 중복 선택은 무시", TestLockPreventsDuplicate(database));
        failed += Expect("11 런 종료 행동도 피드백 후 선택지 없음", TestEndedRunKeepsEnded(database));
        failed += Expect("12 AlertLimit는 들켰다와 체포 스프라이트", TestAlertLimitCaptured(database));
        failed += Expect("13 일반 실패는 기존 리액션", TestRegularFailKeepsChibi(database));
        failed += Expect("14 NoTurns는 기존 종료 연출", TestNoTurnsKeepsExisting(database));

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

    static bool TestAllActionsMapped(CsvDatabase database)
    {
        IReadOnlyList<ActionRow> actions = database.EnabledActions;
        if (actions == null || actions.Count == 0)
            return false;
        for (int i = 0; i < actions.Count; i++)
        {
            ResultFeedbackRow row;
            if (!database.TryGetResultFeedback("ACTION", actions[i].ActionID, out row))
                return false;
            if (string.IsNullOrEmpty(row.SuccessText) || string.IsNullOrEmpty(row.FailText))
                return false;
            if (string.IsNullOrEmpty(row.SuccessPresentationID) || string.IsNullOrEmpty(row.FailPresentationID))
                return false;
        }

        return true;
    }

    static bool TestAllOptionsMapped(CsvDatabase database)
    {
        IReadOnlyList<EventOptionRow> options = database.EnabledEventOptions;
        if (options == null || options.Count == 0)
            return false;
        for (int i = 0; i < options.Count; i++)
        {
            ResultFeedbackRow row;
            if (!database.TryGetResultFeedback("OPTION", options[i].OptionID, out row))
                return false;
            if (string.IsNullOrEmpty(row.SuccessText) || string.IsNullOrEmpty(row.FailText))
                return false;
        }

        return true;
    }

    static bool TestMissingUsesDefault()
    {
        var resolve = new PresentedResolve
        {
            SourceType = "ACTION",
            SourceId = "ACT_MISSING",
            DisplayName = "없는 행동",
            Success = true
        };
        return string.IsNullOrEmpty(V02ResultPresentation.ResolveId(resolve))
            && V02ResultPresentation.FormatDeltas(resolve) == "변화 없음";
    }

    static bool TestGameplayLoadUnaffected()
    {
        Dictionary<string, string> texts = LoadTableTexts();
        if (texts == null)
            return false;
        CsvDatabase loaded = CsvDatabase.LoadFromTexts(texts);
        ResultFeedbackRow unused;
        return loaded != null
            && loaded.Loaded
            && !loaded.HasErrors
            && !loaded.TryGetResultFeedback("ACTION", "ACT_001", out unused);
    }

    static bool TestBadPresentationDoesNotError()
    {
        Dictionary<string, string> texts = LoadTableTexts();
        if (texts == null)
            return false;
        texts["ResultFeedbackTable"] = "NotAHeader\nfoo";
        CsvDatabase loaded = CsvDatabase.LoadFromTexts(texts);
        return loaded != null && loaded.Loaded && !loaded.HasErrors;
    }

    static bool TestSnapshotDeltas(CsvDatabase database)
    {
        RunState run;
        ActionRow action;
        if (!RunState.TryStart(database, "SHOP_001", out run) || !database.TryGetAction("ACT_007", out action))
            return false;
        if (!run.SetPhase("LOOT"))
            return false;

        int candyBefore = run.Candy;
        int alertBefore = run.Alert;
        int turnsBefore = run.RemainingTurns;
        var resolver = new ActionResolver(database, new FixedChanceRoll(true));
        ActionResolveResult result;
        if (!resolver.TryResolve(run, "ACT_007", out result) || !result.Applied || !result.Success)
            return false;

        var presented = new PresentedResolve
        {
            Success = true,
            CandyDelta = run.Candy - candyBefore,
            AlertDelta = run.Alert - alertBefore,
            TurnDelta = run.RemainingTurns - turnsBefore
        };
        if (presented.CandyDelta == action.SuccessCandy && presented.CandyDelta == 0)
            return false;
        if (presented.CandyDelta != run.Candy - candyBefore)
            return false;
        string text = V02ResultPresentation.FormatDeltas(presented);
        return text.IndexOf("사탕 +") >= 0 && text.IndexOf(presented.CandyDelta.ToString()) >= 0;
    }

    static bool TestSkipPhases()
    {
        var go = new GameObject("ResultPresentationTest", typeof(RectTransform));
        try
        {
            V02ResultPresentation view = go.AddComponent<V02ResultPresentation>();
            if (view.CanSkip)
                return false;
            view.SkipToHold();
            if (view.CurrentPhase != V02ResultPresentation.Phase.Idle)
                return false;

            var resolve = new PresentedResolve { Success = true, DisplayName = "테스트" };
            view.Present(resolve, "본문", V02ResultPresentation.SuccessDefault);
            if (!view.CanSkip && view.CurrentPhase != V02ResultPresentation.Phase.Holding)
                return false;
            view.SkipToHold();
            if (view.CurrentPhase != V02ResultPresentation.Phase.Holding)
                return false;
            view.SkipToHold();
            return view.CurrentPhase == V02ResultPresentation.Phase.Holding && !view.CanSkip;
        }
        finally
        {
            Object.DestroyImmediate(go);
        }
    }

    static bool TestContinueOnlyOnHold()
    {
        var go = new GameObject("ResultContinueTest", typeof(RectTransform));
        try
        {
            V02ResultPresentation view = go.AddComponent<V02ResultPresentation>();
            if (view.TryContinue())
                return false;
            view.Present(new PresentedResolve { Success = false }, "실패", V02ResultPresentation.FailDefault);
            view.SkipToHold();
            return view.CurrentPhase == V02ResultPresentation.Phase.Holding && view.TryContinue() && !view.IsPresenting;
        }
        finally
        {
            Object.DestroyImmediate(go);
        }
    }

    static bool TestPendingUiAfterContinue(CsvDatabase database)
    {
        V02RunHud hud;
        RunState run;
        if (!TryCreateHud(database, new FixedChanceRoll(true), new FixedEventPick("EVT_001"), out hud, out run))
            return false;
        try
        {
            if (!hud.TryChoose("ACT_001", false))
                return false;
            if (!run.HasPendingEvent || !hud.IsPresenting || hud.ShowingEventOptions)
                return false;
            hud.ResultPresentation.SkipToHold();
            hud.ResultPresentation.SkipToHold();
            if (!hud.TryContinueResult())
                return false;
            return !hud.IsPresenting && run.HasPendingEvent && hud.ShowingEventOptions;
        }
        finally
        {
            Object.DestroyImmediate(hud.gameObject);
        }
    }

    static bool TestLockPreventsDuplicate(CsvDatabase database)
    {
        V02RunHud hud;
        RunState run;
        if (!TryCreateHud(database, new FixedChanceRoll(true), null, out hud, out run))
            return false;
        try
        {
            int turnsBefore = run.RemainingTurns;
            if (!hud.TryChoose("ACT_001", false))
                return false;
            if (!hud.ChoicesLocked)
                return false;
            if (hud.TryChoose("ACT_001", false))
                return false;
            return run.RemainingTurns < turnsBefore;
        }
        finally
        {
            Object.DestroyImmediate(hud.gameObject);
        }
    }

    static bool TestEndedRunKeepsEnded(CsvDatabase database)
    {
        V02RunHud hud;
        RunState run;
        if (!TryCreateHud(database, new FixedChanceRoll(true), null, out hud, out run))
            return false;
        try
        {
            if (!run.ApplyCandyDelta(database.GetInt("MIN_CANDY_TO_ESCAPE")) || !run.SetPhase("ESCAPE"))
                return false;
            hud.Bind(database, run, null, new FixedChanceRoll(true), null);
            if (!hud.TryChoose("ACT_010", false))
                return false;
            if (!run.IsEnded || !hud.IsPresenting)
                return false;
            hud.ResultPresentation.SkipToHold();
            if (!hud.TryContinueResult())
                return false;
            return run.IsEnded && !hud.HasVisibleChoices && !hud.ShowingEventOptions;
        }
        finally
        {
            Object.DestroyImmediate(hud.gameObject);
        }
    }

    static bool TestAlertLimitCaptured(CsvDatabase database)
    {
        V02RunHud hud;
        RunState run;
        if (!TryCreateHud(database, new FixedChanceRoll(false), null, out hud, out run))
            return false;
        try
        {
            if (!run.ApplyAlertDelta(80))
                return false;
            hud.Refresh();
            if (!hud.TryChoose("ACT_003", false))
                return false;
            if (run.FailReason != RunFailReason.AlertLimit || !run.IsEnded)
                return false;
            hud.ResultPresentation.SkipToHold();
            Sprite reaction = hud.ResultPresentation.ReactionSprite;
            return hud.ResultPresentation.TitleLabel == "들켰다!"
                && hud.ChoicesLocked
                && reaction != null
                && reaction.name.IndexOf("Captured") >= 0
                && hud.TryContinueResult()
                && !hud.IsPresenting
                && !hud.HasVisibleChoices;
        }
        finally
        {
            Object.DestroyImmediate(hud.gameObject);
        }
    }

    static bool TestRegularFailKeepsChibi(CsvDatabase database)
    {
        V02RunHud hud;
        RunState run;
        if (!TryCreateHud(database, new FixedChanceRoll(false), null, out hud, out run))
            return false;
        try
        {
            if (!hud.TryChoose("ACT_003", false))
                return false;
            if (run.IsEnded || run.FailReason != RunFailReason.None)
                return false;
            hud.ResultPresentation.SkipToHold();
            Sprite reaction = hud.ResultPresentation.ReactionSprite;
            return hud.ResultPresentation.TitleLabel == "실패…"
                && reaction != null
                && reaction.name.IndexOf("Captured") < 0;
        }
        finally
        {
            Object.DestroyImmediate(hud.gameObject);
        }
    }

    static bool TestNoTurnsKeepsExisting(CsvDatabase database)
    {
        V02RunHud hud;
        RunState run;
        if (!TryCreateHud(database, new FixedChanceRoll(false), null, out hud, out run))
            return false;
        try
        {
            if (!run.ApplyTurnDelta(-(run.RemainingTurns - 1)))
                return false;
            hud.Refresh();
            if (!hud.TryChoose("ACT_003", false))
                return false;
            if (run.FailReason != RunFailReason.NoTurns)
                return false;
            hud.ResultPresentation.SkipToHold();
            Sprite reaction = hud.ResultPresentation.ReactionSprite;
            return hud.ResultPresentation.TitleLabel == "실패… · 턴"
                && reaction != null
                && reaction.name.IndexOf("Captured") < 0;
        }
        finally
        {
            Object.DestroyImmediate(hud.gameObject);
        }
    }

    static bool TryCreateHud(CsvDatabase database, IChanceRoll roll, IEventPick pick, out V02RunHud hud, out RunState run)
    {
        hud = null;
        run = null;
        V02RunHud prefab = Resources.Load<V02RunHud>(V02RunHud.ResourcePath);
        if (prefab == null || !RunState.TryStart(database, "SHOP_001", out run))
            return false;
        hud = Object.Instantiate(prefab);
        hud.Bind(database, run, null, roll, pick);
        return true;
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

#if UNITY_EDITOR
    [UnityEditor.MenuItem("Candy Thief/V02/Run ResultFeedback Tests")]
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
