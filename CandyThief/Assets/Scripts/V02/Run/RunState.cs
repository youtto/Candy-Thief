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
            FailReason = RunFailReason.None
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
        return action != null && CanAffordTurnCost(action.TurnCost);
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

        if (resolved.TurnCost > RemainingTurns)
        {
            Debug.LogError(LogPrefix + "턴이 부족합니다. cost=" + resolved.TurnCost
                + " remaining=" + RemainingTurns + " action=" + resolved.ActionID);
            return false;
        }

        RemainingTurns = ClampNonNegative(RemainingTurns - resolved.TurnCost);
        Candy = ClampNonNegative(Candy + resolved.CandyDelta);
        Alert = ClampAlert(Alert + resolved.AlertDelta);
        RemainingTurns = ClampNonNegative(RemainingTurns + resolved.TurnDelta);
        LastActionID = resolved.ActionID ?? string.Empty;
        LastActionResult = resolved.Success ? RunActionResult.Success : RunActionResult.Fail;

        EvaluateEndConditions(resolved.Success && resolved.IsEscapeAction);
        if (Status == RunStatus.Won && ShouldAdvancePhase(resolved))
            CurrentPhase = resolved.SuccessNextPhase;

        if (Status != RunStatus.InProgress)
            return true;

        if (resolved.Success && ShouldAdvancePhase(resolved))
            CurrentPhase = resolved.SuccessNextPhase;

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
