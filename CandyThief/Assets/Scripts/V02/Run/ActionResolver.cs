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
    MissingData,
    UnknownAction,
    WrongPhase,
    ShowCondition,
    InsufficientTurns
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

    public bool TryResolve(RunState run, string actionId, out ActionResolveResult result)
    {
        result = new ActionResolveResult
        {
            ActionID = actionId ?? string.Empty,
            RejectReason = ActionRejectReason.None
        };

        if (run == null)
        {
            result.RejectReason = ActionRejectReason.MissingData;
            Debug.LogError(LogPrefix + "RunState가 없습니다.");
            return false;
        }

        if (run.IsEnded)
        {
            result.RejectReason = ActionRejectReason.Ended;
            Debug.LogError(LogPrefix + "종료된 런은 행동을 실행할 수 없습니다. Status=" + run.Status);
            return false;
        }

        if (database == null || !database.Loaded || database.HasErrors)
        {
            result.RejectReason = ActionRejectReason.MissingData;
            Debug.LogError(LogPrefix + "CSV가 준비되지 않았습니다.");
            return false;
        }

        ActionRow action;
        if (!database.TryGetAction(actionId, out action))
        {
            result.RejectReason = ActionRejectReason.UnknownAction;
            Debug.LogError(LogPrefix + "ActionID를 찾을 수 없습니다: " + actionId);
            return false;
        }

        if (action.PhaseID != run.CurrentPhase)
        {
            result.RejectReason = ActionRejectReason.WrongPhase;
            Debug.LogError(LogPrefix + "페이즈가 다른 행동입니다. action=" + action.ActionID
                + " actionPhase=" + action.PhaseID
                + " runPhase=" + run.CurrentPhase);
            return false;
        }

        if (!MeetsShowCondition(run, action))
        {
            result.RejectReason = ActionRejectReason.ShowCondition;
            Debug.LogError(LogPrefix + "표시 조건 미충족. action=" + action.ActionID
                + " ShowCondition=" + action.ShowCondition
                + " Candy=" + run.Candy);
            return false;
        }

        if (!run.CanAffordTurnCost(action.TurnCost))
        {
            result.RejectReason = ActionRejectReason.InsufficientTurns;
            Debug.LogError(LogPrefix + "턴이 부족합니다. action=" + action.ActionID
                + " cost=" + action.TurnCost
                + " remaining=" + run.RemainingTurns);
            return false;
        }

        ShopRow shop;
        if (!database.TryGetShop(run.ShopID, out shop))
        {
            result.RejectReason = ActionRejectReason.MissingData;
            Debug.LogError(LogPrefix + "가게를 찾을 수 없습니다: " + run.ShopID);
            return false;
        }

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
            Debug.Log(LogPrefix + "이벤트 판정 가능 (M1-7에서 추첨). chance=" + result.EventChance);
        return true;
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
