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
    AlertLimit
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
    public string CurrentPhase { get; private set; }
    public string PhaseID => CurrentPhase;
    public int RemainingTurns { get; private set; }
    public int Alert { get; private set; }
    public int Candy { get; private set; }
    public string LastActionID { get; private set; }
    public RunActionResult LastActionResult { get; private set; }
    public RunActionResult LastResult => LastActionResult;
    public RunStatus Status { get; private set; }
    public RunFailReason FailReason { get; private set; }
    public bool IsEnded => Status != RunStatus.InProgress;
    public string PendingEventID => pendingEventId ?? string.Empty;
    public bool HasPendingEvent => !string.IsNullOrEmpty(PendingEventID);

    public static bool TryStart(CsvDatabase database, string shopId, out RunState state)
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

        state = new RunState
        {
            database = database,
            maxAlert = database.GetInt("MAX_ALERT"),
            minCandyToEscape = database.GetInt("MIN_CANDY_TO_ESCAPE"),
            escapePhaseId = escapePhaseId,
            ShopID = shop.ShopID,
            CurrentPhase = opening.PhaseID,
            RemainingTurns = shop.StartTurn,
            Alert = ClampNonNegative(shop.StartAlert),
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
        if (Status == RunStatus.Won && ShouldAdvancePhase(resolved))
            CurrentPhase = resolved.SuccessNextPhase;

        if (Status != RunStatus.InProgress)
            return true;

        if (resolved.Success && ShouldAdvancePhase(resolved))
            CurrentPhase = resolved.SuccessNextPhase;

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

    bool ShouldAdvancePhase(ResolvedAction resolved)
    {
        if (string.IsNullOrEmpty(resolved.SuccessNextPhase)
            || string.Equals(resolved.SuccessNextPhase, "NONE", System.StringComparison.Ordinal))
            return false;

        if (resolved.IsEscapeAction && Candy < minCandyToEscape)
            return false;

        return true;
    }

    void EvaluateEndConditions(bool escapeSucceeded)
    {
        if (Status != RunStatus.InProgress)
            return;

        if (Alert >= maxAlert)
        {
            Status = RunStatus.Failed;
            FailReason = RunFailReason.AlertLimit;
            Debug.Log(LogPrefix + "실패: 경계도 " + Alert + " >= MAX_ALERT " + maxAlert);
            return;
        }

        if (escapeSucceeded && Candy >= minCandyToEscape)
        {
            Status = RunStatus.Won;
            FailReason = RunFailReason.None;
            Debug.Log(LogPrefix + "승리: 탈출 성공, Candy=" + Candy + " Turns=" + RemainingTurns);
            return;
        }

        if (RemainingTurns <= 0)
        {
            Status = RunStatus.Failed;
            FailReason = RunFailReason.NoTurns;
            Debug.Log(LogPrefix + "실패: 남은 턴 0");
        }
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
