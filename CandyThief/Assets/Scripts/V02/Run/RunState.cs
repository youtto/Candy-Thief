using System;
using System.Collections.Generic;
using UnityEngine;

public enum RunStatus
{
    InProgress,
    Won,
    Failed
}

public enum RunFailReason
{
    None,
    NoTurns,
    AlertLimit,
    Abandon,
    EmptyEscape
}

public enum RunOutcome
{
    Continue,
    OperationSuccess,
    EmptyEscape,
    Captured,
    NoTurns
}

// 표시용 전리품. 지금은 만드는 곳이 없고, 결과 팝업은 빈 목록을 빈 상태로 보여 준다.
public sealed class RunLootGrant
{
    public string ItemId;
    public string DisplayName;
    public int Count;
    public bool Kept;
}

public enum RunActionResult
{
    None,
    Success,
    Fail
}

public struct ResolvedAction
{
    public string ActionID;
    public int TurnCost;
    public bool Success;
    public int CandyDelta;
    public int AlertDelta;
    public int TurnDelta;
    public string SuccessNextPhase;
    public bool IsEscapeAction;
}

public sealed class RunState
{
    public const string LogPrefix = "[RunState] ";
    public const string DefaultShopId = "SHOP_001";

    public static RunState Current { get; internal set; }

    CsvDatabase database;
    int maxAlert;
    int minCandyToEscape;
    string escapePhaseId;
    readonly HashSet<string> successfulActionIds = new HashSet<string>(StringComparer.Ordinal);
    readonly HashSet<string> successfulEventIds = new HashSet<string>(StringComparer.Ordinal);
    string pendingEventId = string.Empty;
    bool eventCheckConsumed;

    public string ShopID { get; private set; }

    // 런 1회당 고유 ID. 결과 정산(가게 목록 갱신)이 같은 런에 대해 두 번 실행되지 않게 하는 키다 (D-025/D-026).
    public string RunId { get; private set; }

    // 런 시작 시점 가게 스냅샷 (소문 효과 반영 복사본). 판정/HUD는 이 값을 읽고, 원본 ShopRow는 수정하지 않는다.
    public ShopRow Shop { get; private set; }
    public IReadOnlyList<string> RumorIds { get; private set; }
    public string CurrentPhase { get; private set; }
    public string PhaseID => CurrentPhase;
    public int RemainingTurns { get; private set; }
    public int Alert { get; private set; }
    public int AlertMax { get { return maxAlert; } }
    public int Candy { get; private set; }
    public string LastActionID { get; private set; }
    public RunActionResult LastActionResult { get; private set; }
    public RunActionResult LastResult => LastActionResult;
    public RunStatus Status { get; private set; }
    public RunFailReason FailReason { get; private set; }
    public bool IsEnded => Status != RunStatus.InProgress;
    public string PendingEventID => pendingEventId ?? string.Empty;
    public bool HasPendingEvent => !string.IsNullOrEmpty(PendingEventID);

    // 행동 한 번이 적용되고 런이 계속될 때 증가한다. HUD 갱신만으로는 바뀌지 않는다.
    public int ChoiceEpoch { get; private set; }

    public RunOutcome Outcome
    {
        get
        {
            if (Status == RunStatus.Won)
                return RunOutcome.OperationSuccess;
            if (Status != RunStatus.Failed)
                return RunOutcome.Continue;
            if (FailReason == RunFailReason.EmptyEscape)
                return RunOutcome.EmptyEscape;
            if (FailReason == RunFailReason.AlertLimit)
                return RunOutcome.Captured;
            if (FailReason == RunFailReason.NoTurns)
                return RunOutcome.NoTurns;
            return RunOutcome.Continue;
        }
    }

    // 프로필에 넣는 사탕. 작전 성공만 배율을 올리고, 그 외에는 0이다.
    public int SettledCandy
    {
        get
        {
            if (Status != RunStatus.Won)
                return 0;
            float multiplier = Shop != null ? Shop.CandyMultiplier : 1f;
            return Mathf.CeilToInt(Candy * multiplier);
        }
    }

    public static bool TryStart(CsvDatabase database, string shopId, out RunState state)
    {
        return TryStart(database, shopId, null, out state);
    }

    // rumorIds: 가게 목록 확정 시 뽑아 둔 소문 (OP-DEV-04). null/빈 목록이면 기존 동작과 동일하다.
    public static bool TryStart(CsvDatabase database, string shopId, IReadOnlyList<string> rumorIds, out RunState state)
    {
        state = null;
        if (database == null || !database.Loaded || database.HasErrors)
        {
            Debug.LogError(LogPrefix + "CSV가 준비되지 않아 런을 시작할 수 없습니다.");
            return false;
        }

        ShopRow shop;
        if (!database.TryGetShop(shopId, out shop))
        {
            Debug.LogError(LogPrefix + "가게를 찾을 수 없습니다: " + shopId);
            return false;
        }

        PhaseRow opening;
        if (!TryGetOpeningPhase(database, out opening))
        {
            Debug.LogError(LogPrefix + "시작 페이즈를 찾을 수 없습니다.");
            return false;
        }

        string escapePhaseId;
        if (!TryGetEscapePhaseId(database, out escapePhaseId))
        {
            Debug.LogError(LogPrefix + "ESCAPE 페이즈를 PhaseTable에서 찾을 수 없습니다.");
            return false;
        }

        GameConfigRow maxAlertRow;
        GameConfigRow minCandyRow;
        if (!database.TryGetConfig("MAX_ALERT", out maxAlertRow)
            || !database.TryGetConfig("MIN_CANDY_TO_ESCAPE", out minCandyRow))
        {
            Debug.LogError(LogPrefix + "GameConfig에 MAX_ALERT 또는 MIN_CANDY_TO_ESCAPE가 없습니다.");
            return false;
        }

        ShopRow snapshot;
        string snapshotError;
        if (!ShopRumorSnapshot.TryBuild(database, shop, rumorIds, out snapshot, out snapshotError))
        {
            Debug.LogError(LogPrefix + "소문 스냅샷을 만들 수 없어 런을 시작하지 않습니다: " + snapshotError);
            return false;
        }

        state = new RunState
        {
            RunId = Guid.NewGuid().ToString("N"),
            Shop = snapshot,
            RumorIds = rumorIds == null ? new List<string>() : new List<string>(rumorIds),
            database = database,
            maxAlert = database.GetInt("MAX_ALERT"),
            minCandyToEscape = database.GetInt("MIN_CANDY_TO_ESCAPE"),
            escapePhaseId = escapePhaseId,
            ShopID = shop.ShopID,
            CurrentPhase = opening.PhaseID,
            RemainingTurns = snapshot.StartTurn,
            Alert = ClampNonNegative(snapshot.StartAlert),
            Candy = 0,
            LastActionID = string.Empty,
            LastActionResult = RunActionResult.None,
            Status = RunStatus.InProgress,
            FailReason = RunFailReason.None,
            pendingEventId = string.Empty,
            eventCheckConsumed = false
        };
        if (state.Alert > state.maxAlert)
            state.Alert = state.maxAlert;

        Current = state;
        return true;
    }

    public bool CanAffordTurnCost(int turnCost)
    {
        return Status == RunStatus.InProgress && turnCost <= RemainingTurns;
    }

    public bool CanSelectAction(ActionRow action)
    {
        return action != null && !HasPendingEvent && CanAffordTurnCost(action.TurnCost);
    }

    public bool HasSucceededAction(string actionId)
    {
        return !string.IsNullOrEmpty(actionId) && successfulActionIds.Contains(actionId);
    }

    public bool HasSucceededEvent(string eventId)
    {
        return !string.IsNullOrEmpty(eventId) && successfulEventIds.Contains(eventId);
    }

    public bool TryConsumeEventCheck()
    {
        if (IsEnded || HasPendingEvent || eventCheckConsumed)
            return false;

        eventCheckConsumed = true;
        return true;
    }

    public bool TrySetPendingEvent(string eventId)
    {
        if (!EnsureInProgress("이벤트"))
            return false;

        if (HasPendingEvent)
        {
            Debug.LogError(LogPrefix + "이미 대기 중인 이벤트가 있습니다. pending=" + PendingEventID);
            return false;
        }

        if (string.IsNullOrEmpty(eventId))
            return false;

        pendingEventId = eventId;
        eventCheckConsumed = true;
        return true;
    }

    public void ClearPendingEvent()
    {
        pendingEventId = string.Empty;
    }

    public bool ApplyCandyDelta(int delta)
    {
        if (!EnsureInProgress("사탕"))
            return false;

        Candy = ClampNonNegative(Candy + delta);
        EvaluateEndConditions(false);
        return true;
    }

    public bool ApplyTurnDelta(int delta)
    {
        if (!EnsureInProgress("턴"))
            return false;

        RemainingTurns = ClampNonNegative(RemainingTurns + delta);
        EvaluateEndConditions(false);
        return true;
    }

    public bool ApplyAlertDelta(int delta)
    {
        if (!EnsureInProgress("경계도"))
            return false;

        Alert = ClampAlert(Alert + delta);
        EvaluateEndConditions(false);
        return true;
    }

    // LOOT의 다음 페이즈로 들어간다. 턴·위험도·사탕·행동 기록은 바꾸지 않는다.
    public bool TryPrepareEscape()
    {
        if (!EnsureInProgress("탈출 준비"))
            return false;

        if (HasPendingEvent)
        {
            Debug.LogError(LogPrefix + "이벤트 선택 중에는 탈출을 준비할 수 없습니다. pending=" + PendingEventID);
            return false;
        }

        if (database == null)
            return false;

        PhaseRow current;
        PhaseRow destination;
        if (!database.TryGetPhase(CurrentPhase, out current)
            || string.IsNullOrEmpty(current.NextPhaseID)
            || current.NextPhaseID != escapePhaseId
            || !database.TryGetPhase(escapePhaseId, out destination))
        {
            Debug.LogError(LogPrefix + "지금은 탈출을 준비할 수 없습니다. phase=" + CurrentPhase);
            return false;
        }

        if (!MeetsPhaseCondition(current.CompleteCondition) || !MeetsPhaseCondition(destination.EntryCondition))
        {
            Debug.LogError(LogPrefix + "탈출 준비 조건을 만족하지 않습니다. complete="
                + current.CompleteCondition + " entry=" + destination.EntryCondition + " candy=" + Candy);
            return false;
        }

        CurrentPhase = destination.PhaseID;
        return true;
    }

    public bool SetPhase(string phaseId)
    {
        if (!EnsureInProgress("페이즈"))
            return false;

        if (string.IsNullOrEmpty(phaseId) || string.Equals(phaseId, "NONE", System.StringComparison.Ordinal))
            return false;

        CurrentPhase = phaseId;
        return true;
    }

    public bool RecordActionResult(string actionId, bool success)
    {
        if (!EnsureInProgress("행동 결과"))
            return false;

        LastActionID = actionId ?? string.Empty;
        LastActionResult = success ? RunActionResult.Success : RunActionResult.Fail;
        return true;
    }

    public bool TryApplyAction(string actionId, bool success)
    {
        if (database == null)
        {
            Debug.LogError(LogPrefix + "CsvDatabase가 없어 행동을 적용할 수 없습니다.");
            return false;
        }

        ActionRow action;
        if (!database.TryGetAction(actionId, out action))
        {
            Debug.LogError(LogPrefix + "ActionID를 찾을 수 없습니다: " + actionId);
            return false;
        }

        return TryApplyAction(action, success);
    }

    public bool TryApplyAction(ActionRow action, bool success)
    {
        if (action == null)
            return false;

        var resolved = new ResolvedAction
        {
            ActionID = action.ActionID,
            TurnCost = action.TurnCost,
            Success = success,
            CandyDelta = success ? action.SuccessCandy : action.FailCandy,
            AlertDelta = success ? action.SuccessAlert : action.FailAlert,
            TurnDelta = success ? action.SuccessTurnDelta : action.FailTurnDelta,
            SuccessNextPhase = action.SuccessNextPhase,
            IsEscapeAction = action.PhaseID == escapePhaseId
        };
        return TryApplyResolved(resolved);
    }

    public bool TryApplyResolved(ResolvedAction resolved)
    {
        if (!EnsureInProgress("행동"))
            return false;

        if (HasPendingEvent)
        {
            Debug.LogError(LogPrefix + "이벤트 선택 중에는 행동을 실행할 수 없습니다. pending="
                + PendingEventID + " action=" + resolved.ActionID);
            return false;
        }

        if (resolved.TurnCost > RemainingTurns)
        {
            Debug.LogError(LogPrefix + "턴이 부족합니다. cost=" + resolved.TurnCost
                + " remaining=" + RemainingTurns + " action=" + resolved.ActionID);
            return false;
        }

        eventCheckConsumed = false;
        RemainingTurns = ClampNonNegative(RemainingTurns - resolved.TurnCost);
        Candy = ClampNonNegative(Candy + resolved.CandyDelta);
        Alert = ClampAlert(Alert + resolved.AlertDelta);
        RemainingTurns = ClampNonNegative(RemainingTurns + resolved.TurnDelta);
        LastActionID = resolved.ActionID ?? string.Empty;
        LastActionResult = resolved.Success ? RunActionResult.Success : RunActionResult.Fail;
        if (resolved.Success && !string.IsNullOrEmpty(LastActionID))
            successfulActionIds.Add(LastActionID);

        EvaluateEndConditions(resolved.Success && resolved.IsEscapeAction);
        bool ended = Status != RunStatus.InProgress;
        if (resolved.Success && ShouldAdvancePhase(resolved)
            && (!ended || Status == RunStatus.Won || FailReason == RunFailReason.EmptyEscape))
            CurrentPhase = resolved.SuccessNextPhase;

        if (ended)
            return true;

        ChoiceEpoch++;
        return true;
    }

    public bool TryApplyEventOption(EventOptionRow option, bool success)
    {
        if (option == null)
            return false;

        if (!EnsureInProgress("이벤트 옵션"))
            return false;

        if (!HasPendingEvent)
        {
            Debug.LogError(LogPrefix + "대기 중인 이벤트가 없어 옵션을 적용할 수 없습니다. option="
                + option.OptionID);
            return false;
        }

        if (option.EventID != PendingEventID)
        {
            Debug.LogError(LogPrefix + "다른 이벤트의 옵션입니다. pending=" + PendingEventID
                + " option=" + option.OptionID + " optionEvent=" + option.EventID);
            return false;
        }

        if (option.TurnCost > RemainingTurns)
        {
            Debug.LogError(LogPrefix + "턴이 부족합니다. cost=" + option.TurnCost
                + " remaining=" + RemainingTurns + " option=" + option.OptionID);
            return false;
        }

        RemainingTurns = ClampNonNegative(RemainingTurns - option.TurnCost);
        Candy = ClampNonNegative(Candy + (success ? option.SuccessCandy : option.FailCandy));
        Alert = ClampAlert(Alert + (success ? option.SuccessAlert : option.FailAlert));
        RemainingTurns = ClampNonNegative(RemainingTurns + (success ? option.SuccessTurnDelta : option.FailTurnDelta));
        if (success)
            successfulEventIds.Add(PendingEventID);

        EvaluateEndConditions(false);
        pendingEventId = string.Empty;
        eventCheckConsumed = true;
        return true;
    }

    public void LogSnapshot(string label)
    {
        Debug.Log(LogPrefix + label
            + " ShopID=" + ShopID
            + " Phase=" + CurrentPhase
            + " Turns=" + RemainingTurns
            + " Alert=" + Alert
            + " Candy=" + Candy
            + " Status=" + Status
            + " FailReason=" + FailReason
            + " LastAction=" + (string.IsNullOrEmpty(LastActionID) ? "-" : LastActionID)
            + " LastResult=" + LastActionResult);
    }

    bool MeetsPhaseCondition(ConditionKey key)
    {
        switch (key)
        {
            case ConditionKey.ALWAYS:
            case ConditionKey.NONE:
                return true;
            case ConditionKey.CANDY_AT_LEAST_1:
                return Candy >= minCandyToEscape;
            default:
                Debug.LogError(LogPrefix + "탈출 준비에서 지원하지 않는 조건입니다. condition=" + key);
                return false;
        }
    }

    bool ShouldAdvancePhase(ResolvedAction resolved)
    {
        if (string.IsNullOrEmpty(resolved.SuccessNextPhase)
            || string.Equals(resolved.SuccessNextPhase, "NONE", System.StringComparison.Ordinal))
            return false;

        return true;
    }

    void EvaluateEndConditions(bool escapeSucceeded)
    {
        if (Status != RunStatus.InProgress)
            return;

        if (escapeSucceeded)
        {
            if (Candy >= minCandyToEscape)
            {
                Status = RunStatus.Won;
                FailReason = RunFailReason.None;
                Debug.Log(LogPrefix + "작전 성공: 탈출 성공, Candy=" + Candy);
            }
            else
            {
                Status = RunStatus.Failed;
                FailReason = RunFailReason.EmptyEscape;
                Debug.Log(LogPrefix + "빈손 탈출: Candy=0");
            }

            return;
        }

        if (Alert >= maxAlert)
        {
            Status = RunStatus.Failed;
            FailReason = RunFailReason.AlertLimit;
            Debug.Log(LogPrefix + "체포: 경계도 " + Alert + " >= MAX_ALERT " + maxAlert);
            return;
        }

        if (RemainingTurns <= 0)
        {
            Status = RunStatus.Failed;
            FailReason = RunFailReason.NoTurns;
            Debug.Log(LogPrefix + "탈출 실패: 남은 턴 0");
        }
    }

    // 명시적 포기 (D-025). 턴, 위험도, 사탕, 행동 기록은 바꾸지 않는다.
    public bool TryAbandon()
    {
        if (IsEnded)
            return false;

        Status = RunStatus.Failed;
        FailReason = RunFailReason.Abandon;
        Debug.Log(LogPrefix + "포기: RunId=" + RunId);
        return true;
    }

    // 포기 정산이 저장되지 않았을 때만 진행 중으로 되돌린다.
    public bool RevertAbandon()
    {
        if (Status != RunStatus.Failed || FailReason != RunFailReason.Abandon)
            return false;

        Status = RunStatus.InProgress;
        FailReason = RunFailReason.None;
        return true;
    }

    bool EnsureInProgress(string field)
    {
        if (Status == RunStatus.InProgress)
            return true;

        Debug.LogError(LogPrefix + "종료된 런은 변경할 수 없습니다. field=" + field + " Status=" + Status);
        return false;
    }

    int ClampAlert(int value)
    {
        if (value < 0)
            return 0;
        if (value > maxAlert)
            return maxAlert;
        return value;
    }

    static int ClampNonNegative(int value)
    {
        return value < 0 ? 0 : value;
    }

    static bool TryGetOpeningPhase(CsvDatabase database, out PhaseRow phase)
    {
        phase = null;
        var phases = database.Phases;
        for (int i = 0; i < phases.Count; i++)
        {
            if (phase == null || phases[i].PhaseOrder < phase.PhaseOrder)
                phase = phases[i];
        }

        return phase != null;
    }

    static bool TryGetEscapePhaseId(CsvDatabase database, out string escapePhaseId)
    {
        escapePhaseId = null;
        string resultPhaseId = null;
        var phases = database.Phases;
        for (int i = 0; i < phases.Count; i++)
        {
            if (string.IsNullOrEmpty(phases[i].NextPhaseID)
                || string.Equals(phases[i].NextPhaseID, "NONE", System.StringComparison.Ordinal))
                resultPhaseId = phases[i].PhaseID;
        }

        if (string.IsNullOrEmpty(resultPhaseId))
            return false;

        for (int i = 0; i < phases.Count; i++)
        {
            if (phases[i].NextPhaseID == resultPhaseId)
            {
                escapePhaseId = phases[i].PhaseID;
                return true;
            }
        }

        return false;
    }
}
