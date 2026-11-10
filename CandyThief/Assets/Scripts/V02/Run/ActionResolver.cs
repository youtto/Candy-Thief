using System.Collections.Generic;
using UnityEngine;

public interface IChanceRoll
{
    bool Succeeds(int percentChance);
}

public sealed class UnityChanceRoll : IChanceRoll
{
    public bool Succeeds(int percentChance)
    {
        if (percentChance <= 0)
            return false;
        if (percentChance >= 100)
            return true;
        return Random.Range(0, 100) < percentChance;
    }
}

public sealed class FixedChanceRoll : IChanceRoll
{
    readonly bool succeedIfRolled;

    public FixedChanceRoll(bool succeedIfRolled)
    {
        this.succeedIfRolled = succeedIfRolled;
    }

    public bool Succeeds(int percentChance)
    {
        if (percentChance <= 0)
            return false;
        if (percentChance >= 100)
            return true;
        return succeedIfRolled;
    }
}

public enum ActionRejectReason
{
    None,
    Ended,
    EventPending,
    MissingData,
    UnknownAction,
    WrongPhase,
    ShowCondition,
    InsufficientTurns,
    NotRepeatable
}

public struct ActionResolveResult
{
    public bool Applied;
    public ActionRejectReason RejectReason;
    public string ActionID;
    public bool Success;
    public int SuccessChance;
    public bool EventCheckDue;
    public int EventChance;
}

public struct PresentedAction
{
    public ActionRow Action;
    public bool CanExecute;
    public ActionRejectReason ExecuteReason;
}

public sealed class ActionResolver
{
    public const string LogPrefix = "[ActionResolver] ";

    readonly CsvDatabase database;
    readonly IChanceRoll chanceRoll;

    public ActionResolver(CsvDatabase database, IChanceRoll chanceRoll)
    {
        this.database = database;
        this.chanceRoll = chanceRoll ?? new UnityChanceRoll();
    }

    public ActionResolver(CsvDatabase database) : this(database, null)
    {
    }

    public int ComputeSuccessChance(ActionRow action, ShopRow shop)
    {
        int chance = action.SuccessRate + shop.SuccessRateBonusPp;
        int min = database.GetInt("MIN_SUCCESS_RATE");
        int max = database.GetInt("MAX_SUCCESS_RATE");
        if (chance < min)
            return min;
        if (chance > max)
            return max;
        return chance;
    }

    public int ComputeEventChance(ShopRow shop)
    {
        int chance = database.GetInt("EVENT_TRIGGER_RATE") + shop.EventRateBonusPp;
        if (chance < 0)
            return 0;
        if (chance > 100)
            return 100;
        return chance;
    }

    public ActionRejectReason EvaluateSelect(RunState run, string actionId, out ActionRow action)
    {
        action = null;
        if (run == null)
            return ActionRejectReason.MissingData;

        if (run.IsEnded)
            return ActionRejectReason.Ended;

        if (run.HasPendingEvent)
            return ActionRejectReason.EventPending;

        if (database == null || !database.Loaded || database.HasErrors)
            return ActionRejectReason.MissingData;

        if (!database.TryGetAction(actionId, out action))
            return ActionRejectReason.UnknownAction;

        if (action.PhaseID != run.CurrentPhase)
            return ActionRejectReason.WrongPhase;

        if (!MeetsShowCondition(run, action))
            return ActionRejectReason.ShowCondition;

        if (!action.Repeatable && run.HasSucceededAction(action.ActionID))
            return ActionRejectReason.NotRepeatable;

        if (!run.CanAffordTurnCost(action.TurnCost))
            return ActionRejectReason.InsufficientTurns;

        if (!database.TryGetShop(run.ShopID, out _))
            return ActionRejectReason.MissingData;

        return ActionRejectReason.None;
    }

    public bool CanSelect(RunState run, string actionId)
    {
        ActionRow action;
        return EvaluateSelect(run, actionId, out action) == ActionRejectReason.None;
    }

    public void CollectAvailableActions(RunState run, List<ActionRow> destination)
    {
        if (destination == null)
            return;

        destination.Clear();
        if (database == null || !database.Loaded || database.HasErrors)
            return;

        var actions = database.EnabledActions;
        for (int i = 0; i < actions.Count; i++)
        {
            ActionRow selected;
            if (EvaluateSelect(run, actions[i].ActionID, out selected) == ActionRejectReason.None)
                destination.Add(selected);
        }
    }

    public bool IsActionPresented(RunState run, string actionId, out ActionRow action, out bool canExecute, out ActionRejectReason executeReason)
    {
        executeReason = EvaluateSelect(run, actionId, out action);
        if (executeReason == ActionRejectReason.None)
        {
            canExecute = true;
            return true;
        }

        if (executeReason == ActionRejectReason.InsufficientTurns)
        {
            canExecute = false;
            return true;
        }

        canExecute = false;
        return false;
    }

    public void CollectPresentedActions(RunState run, List<PresentedAction> destination)
    {
        if (destination == null)
            return;

        destination.Clear();
        if (database == null || !database.Loaded || database.HasErrors)
            return;

        var actions = database.EnabledActions;
        for (int i = 0; i < actions.Count; i++)
        {
            ActionRow selected;
            bool canExecute;
            ActionRejectReason reason;
            if (!IsActionPresented(run, actions[i].ActionID, out selected, out canExecute, out reason))
                continue;

            destination.Add(new PresentedAction
            {
                Action = selected,
                CanExecute = canExecute,
                ExecuteReason = reason
            });
        }
    }

    public bool TryResolve(RunState run, string actionId, out ActionResolveResult result)
    {
        result = new ActionResolveResult
        {
            ActionID = actionId ?? string.Empty,
            RejectReason = ActionRejectReason.None
        };

        ActionRow action;
        result.RejectReason = EvaluateSelect(run, actionId, out action);
        if (result.RejectReason != ActionRejectReason.None)
        {
            LogReject(run, action, result.ActionID, result.RejectReason);
            return false;
        }

        ShopRow shop;
        database.TryGetShop(run.ShopID, out shop);

        int chance = ComputeSuccessChance(action, shop);
        bool success = chanceRoll.Succeeds(chance);
        if (!run.TryApplyAction(action, success))
        {
            result.RejectReason = ActionRejectReason.InsufficientTurns;
            return false;
        }

        result.Applied = true;
        result.Success = success;
        result.SuccessChance = chance;
        result.EventChance = ComputeEventChance(shop);
        result.EventCheckDue = run.Status == RunStatus.InProgress
            && action.EventEligible
            && (success || database.GetBool("EVENT_ON_FAILED_ACTION"));
        run.LogSnapshot(success ? "action success" : "action fail");
        if (result.EventCheckDue)
            Debug.Log(LogPrefix + "이벤트 판정 가능. chance=" + result.EventChance);
        return true;
    }

    void LogReject(RunState run, ActionRow action, string actionId, ActionRejectReason reason)
    {
        switch (reason)
        {
            case ActionRejectReason.MissingData:
                if (run == null)
                    Debug.LogError(LogPrefix + "RunState가 없습니다.");
                else if (database == null || !database.Loaded || database.HasErrors)
                    Debug.LogError(LogPrefix + "CSV가 준비되지 않았습니다.");
                else
                    Debug.LogError(LogPrefix + "가게를 찾을 수 없습니다: " + run.ShopID);
                break;
            case ActionRejectReason.Ended:
                Debug.LogError(LogPrefix + "종료된 런은 행동을 실행할 수 없습니다. Status=" + run.Status);
                break;
            case ActionRejectReason.EventPending:
                Debug.LogError(LogPrefix + "이벤트 선택 중에는 행동을 실행할 수 없습니다. pending="
                    + run.PendingEventID);
                break;
            case ActionRejectReason.UnknownAction:
                Debug.LogError(LogPrefix + "ActionID를 찾을 수 없습니다: " + actionId);
                break;
            case ActionRejectReason.WrongPhase:
                Debug.LogError(LogPrefix + "페이즈가 다른 행동입니다. action=" + action.ActionID
                    + " actionPhase=" + action.PhaseID
                    + " runPhase=" + run.CurrentPhase);
                break;
            case ActionRejectReason.ShowCondition:
                Debug.LogError(LogPrefix + "표시 조건 미충족. action=" + action.ActionID
                    + " ShowCondition=" + action.ShowCondition
                    + " Candy=" + run.Candy);
                break;
            case ActionRejectReason.NotRepeatable:
                Debug.LogError(LogPrefix + "반복할 수 없는 행동입니다. action=" + action.ActionID);
                break;
            case ActionRejectReason.InsufficientTurns:
                Debug.LogError(LogPrefix + "턴이 부족합니다. action=" + action.ActionID
                    + " cost=" + action.TurnCost
                    + " remaining=" + run.RemainingTurns);
                break;
        }
    }

    bool MeetsShowCondition(RunState run, ActionRow action)
    {
        switch (action.ShowCondition)
        {
            case ConditionKey.ALWAYS:
            case ConditionKey.NONE:
                return true;
            case ConditionKey.CANDY_AT_LEAST_1:
                return run.Candy >= database.GetInt("MIN_CANDY_TO_ESCAPE");
            default:
                Debug.LogError(LogPrefix + "미지원 ShowCondition: " + action.ShowCondition);
                return false;
        }
    }
}
