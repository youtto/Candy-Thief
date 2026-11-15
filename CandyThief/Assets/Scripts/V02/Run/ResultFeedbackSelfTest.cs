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
        failed += Expect("15 공식 이미지 키는 비어 있고 142슬롯", TestOfficialImageKeysStayEmpty(database));
        failed += Expect("16 이미지 키 중복은 오류, 미등록 키는 경고", TestDuplicateImageKeyIsError());
        failed += Expect("17 전용 스프라이트와 누락 폴백", TestDedicatedSpriteAndFallback(database));
        failed += Expect("18 위험도 경고는 전용 이미지를 바꾸지 않음", TestAlertFxDoesNotReplaceDedicatedSprite());

        RunState.Current = previous;
        if (failed == 0)
        {
            Debug.Log(LogPrefix + "자가 검사 18건 통과");
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
            if (hud.ResultPresentation.TitleLabel != "성공!")
                return false;
            if (!hud.TryContinueResult())
                return false;
            return run.IsEnded
                && !hud.IsPresenting
                && !hud.HasVisibleChoices
                && hud.OperationResult != null
                && hud.OperationResult.IsOpen
                && hud.OperationResult.Title == "작전 성공!";
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
            if (hud.ResultPresentation.TitleLabel != "실패…")
                return false;
            if (!hud.TryContinueResult())
                return false;
            return !hud.IsPresenting
                && !hud.HasVisibleChoices
                && hud.OperationResult != null
                && hud.OperationResult.Title == "들켰다!";
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
            if (hud.ResultPresentation.TitleLabel != "실패…")
                return false;
            if (!hud.TryContinueResult())
                return false;
            return hud.OperationResult != null
                && hud.OperationResult.Title == "탈출 실패 !"
                && !hud.HasVisibleChoices;
        }
        finally
        {
            Object.DestroyImmediate(hud.gameObject);
        }
    }

    static bool TestOfficialImageKeysStayEmpty(CsvDatabase database)
    {
        TextAsset asset = Resources.Load<TextAsset>("Table/ResultFeedbackTable");
        if (asset == null || asset.text.IndexOf("SuccessImageKey") < 0 || asset.text.IndexOf("FailImageKey") < 0)
            return false;
        if (database.EnabledActions.Count != 61 || database.EnabledEventOptions.Count != 10)
            return false;
        if (database.EnabledResultFeedback == null || database.EnabledResultFeedback.Count != 71)
            return false;

        FeedbackImageAuditReport report = FeedbackImageAudit.Evaluate(database);
        if (report.SlotCount != FeedbackImageAudit.ApprovedSlotCount || report.EmptyKeyCount != report.SlotCount)
            return false;
        if (report.DuplicateKeyCount != 0 || report.UnknownKeyCount != 0 || report.LoadedCount != 0 || report.Complete)
            return false;

        ResultFeedbackRow row;
        if (!database.TryGetResultFeedback("ACTION", "ACT_001", out row))
            return false;
        return string.IsNullOrEmpty(row.SuccessImageKey)
            && string.IsNullOrEmpty(row.FailImageKey)
            && row.SuccessText.Length > 0
            && row.SuccessPresentationID == V02ResultPresentation.SuccessDefault;
    }

    static bool TestDuplicateImageKeyIsError()
    {
        Dictionary<string, string> texts = LoadTableTexts();
        if (texts == null)
            return false;
        const string header = "FeedbackID,SourceType,SourceID,SuccessText,FailText,SuccessPresentationID,FailPresentationID,SuccessImageKey,FailImageKey,Enabled,Notes";
        texts["ResultFeedbackTable"] = header
            + "\nFB_A,ACTION,ACT_001,성공,실패,PRES_SUCCESS_DEFAULT,PRES_FAIL_ALERT,fb_shared,fb_a_fail,1,"
            + "\nFB_B,ACTION,ACT_002,성공,실패,PRES_SUCCESS_DEFAULT,PRES_FAIL_ALERT,fb_shared,fb_b_fail,1,";
        CsvDatabase duplicated = CsvDatabase.LoadFromTexts(texts);
        if (duplicated == null || !duplicated.HasErrors)
            return false;

        texts = LoadTableTexts();
        if (texts == null)
            return false;
        texts["ResultFeedbackTable"] = header
            + "\nFB_A,ACTION,ACT_001,성공,실패,PRES_SUCCESS_DEFAULT,PRES_FAIL_ALERT,fb_missing_probe,,1,";
        CsvDatabase missing = CsvDatabase.LoadFromTexts(texts);
        ResultFeedbackRow row;
        return missing != null
            && missing.Loaded
            && !missing.HasErrors
            && missing.TryGetResultFeedback("ACTION", "ACT_001", out row)
            && row.SuccessImageKey == "fb_missing_probe"
            && string.IsNullOrEmpty(row.FailImageKey);
    }

    static bool TestDedicatedSpriteAndFallback(CsvDatabase database)
    {
        Sprite unused;
        if (FeedbackImageResolver.TryResolve(new ResultFeedbackRow(), true, key => null, out unused))
            return false;

        Texture2D successTexture = new Texture2D(4, 4);
        Texture2D failTexture = new Texture2D(4, 4);
        Sprite successSprite = Sprite.Create(successTexture, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f));
        Sprite failSprite = Sprite.Create(failTexture, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f));
        successSprite.name = "DedicatedSuccessProbe";
        failSprite.name = "DedicatedFailProbe";
        ResultFeedbackRow live = null;
        string previous = null;
        V02RunHud hud = null;
        try
        {
            var row = new ResultFeedbackRow { SuccessImageKey = "fb_probe_success", FailImageKey = "fb_probe_fail" };
            Sprite success;
            Sprite fail;
            if (!FeedbackImageResolver.TryResolve(row, true, key => key == "fb_probe_success" ? successSprite : null, out success) || success != successSprite)
                return false;
            if (!FeedbackImageResolver.TryResolve(row, false, key => key == "fb_probe_fail" ? failSprite : null, out fail) || fail != failSprite)
                return false;
            if (FeedbackImageResolver.TryResolve(row, false, key => null, out fail))
                return false;

            Sprite loaded = V02FeedbackSprites.Load("Assets/Resources/V02/Feedback/CT_Feedback_Success_Cheer_v001.png");
            if (loaded == null || loaded.name.IndexOf("Cheer") < 0)
                return false;
            if (V02FeedbackSprites.Load("Assets/Resources/V02/Feedback/Dedicated/CT_FB_ACT_001_Success_v001.png") != null)
                return false;

            if (!database.TryGetResultFeedback("ACTION", "ACT_001", out live))
                return false;
            previous = live.SuccessImageKey;
            live.SuccessImageKey = "fb_not_registered";
            RunState run;
            if (!TryCreateHud(database, new FixedChanceRoll(true), null, out hud, out run))
                return false;
            if (!hud.TryChoose("ACT_001", false))
                return false;
            hud.ResultPresentation.SkipToHold();
            Sprite reaction = hud.ResultPresentation.ReactionSprite;
            return reaction != null && reaction.name.IndexOf("Cheer") >= 0;
        }
        finally
        {
            if (live != null)
                live.SuccessImageKey = previous;
            if (hud != null)
                Object.DestroyImmediate(hud.gameObject);
            Object.DestroyImmediate(successSprite);
            Object.DestroyImmediate(failSprite);
            Object.DestroyImmediate(successTexture);
            Object.DestroyImmediate(failTexture);
        }
    }

    static bool TestAlertFxDoesNotReplaceDedicatedSprite()
    {
        Texture2D texture = new Texture2D(4, 4);
        Texture2D frameTexture = new Texture2D(4, 4);
        Sprite dedicated = Sprite.Create(texture, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f));
        Sprite frame = Sprite.Create(frameTexture, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f));
        dedicated.name = "DedicatedAlertProbe";
        frame.name = "TemporaryFrameProbe";
        var plainObject = new GameObject("feedback-plain-probe", typeof(RectTransform), typeof(V02ResultPresentation));
        var alertObject = new GameObject("feedback-alert-probe", typeof(RectTransform), typeof(V02ResultPresentation));
        try
        {
            var plainView = plainObject.GetComponent<V02ResultPresentation>();
            plainView.BindSprites(new[] { frame, frame }, new[] { frame, frame }, null, null, null, null);
            var plain = new PresentedResolve { Success = false, AlertDelta = 0 };
            plainView.Present(plain, "본문", V02ResultPresentation.FailDefault);
            plainView.SkipToHold();
            if (plainView.ReactionSprite != frame || plainView.TitleLabel != "실패…")
                return false;

            var alertView = alertObject.GetComponent<V02ResultPresentation>();
            alertView.BindSprites(new[] { frame, frame }, new[] { frame, frame }, null, null, null, null);
            var alert = new PresentedResolve
            {
                Success = false,
                AlertDelta = 8,
                FailReason = RunFailReason.AlertLimit,
                DedicatedReaction = dedicated
            };
            alertView.Present(alert, "본문", V02ResultPresentation.FailAlert);
            alertView.SkipToHold();
            return alertView.ReactionSprite == dedicated
                && alertView.ReactionSprite != frame
                && alertView.TitleLabel == "실패…"
                && V02ResultPresentation.UsesAlertFx(V02ResultPresentation.FailAlert, alert)
                && V02ResultPresentation.UsesAlertFx(V02ResultPresentation.FailDefault, new PresentedResolve { Success = false, AlertDelta = 4 })
                && !V02ResultPresentation.UsesAlertFx(V02ResultPresentation.FailDefault, plain);
        }
        finally
        {
            Object.DestroyImmediate(plainObject);
            Object.DestroyImmediate(alertObject);
            Object.DestroyImmediate(dedicated);
            Object.DestroyImmediate(frame);
            Object.DestroyImmediate(texture);
            Object.DestroyImmediate(frameTexture);
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

    [UnityEditor.MenuItem("Candy Thief/V02/Run Feedback Image Completion Audit")]
    static void MenuAudit()
    {
        CsvDatabase database = CsvDatabase.Current;
        if (database == null || !database.Loaded || database.HasErrors)
            database = CsvLoadRunner.LoadAndReport();
        if (database == null)
            return;
        FeedbackImageAuditReport report = FeedbackImageAudit.Evaluate(database);
        string message = "슬롯 " + report.SlotCount
            + " / 빈 키 " + report.EmptyKeyCount
            + " / 중복 " + report.DuplicateKeyCount
            + " / 미등록 " + report.UnknownKeyCount
            + " / 파일 없음 " + report.MissingFileCount
            + " / 로드 " + report.LoadedCount;
        if (report.Complete)
            Debug.Log(LogPrefix + "완료 감사 통과. " + message);
        else
            Debug.LogWarning(LogPrefix + "완료 감사 미완. 개발 중에는 빈 키와 누락 파일을 임시 피드백으로 둡니다. " + message);
    }
#endif
}
