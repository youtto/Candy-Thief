using System;
using System.Collections.Generic;
using UnityEngine;

public struct ActionHandOption
{
    public string ActionID;
    public ActionStance Stance;
    public int SpawnWeight;
    public bool CanExecute;
}

// 일반 행동 라운드의 2장 추첨. 이벤트 선택지와 행동 성공/실패 판정은 바꾸지 않는다.
public sealed class ActionHandDrawer
{
    public const string LogPrefix = "[ActionHand] ";
    public const string ExcludedActionId = "ACT_013";
    public const int HandSize = 2;

    readonly CsvDatabase database;
    readonly ActionResolver resolver;
    readonly IIntRoll roll;

    public ActionHandDrawer(CsvDatabase database, ActionResolver resolver, IIntRoll roll)
    {
        this.database = database;
        this.resolver = resolver;
        this.roll = roll ?? new UnityIntRoll();
    }

    public bool Draw(RunState run, IList<string> avoidIds, List<PresentedAction> destination)
    {
        if (destination == null)
            return false;

        destination.Clear();
        if (run == null || database == null || resolver == null)
        {
            Debug.LogError(LogPrefix + "추첨 데이터가 없습니다.");
            return false;
        }

        var pool = new List<ActionHandOption>();
        var byId = new Dictionary<string, PresentedAction>(StringComparer.Ordinal);
        IReadOnlyList<ActionRow> actions = database.EnabledActions;
        for (int i = 0; i < actions.Count; i++)
        {
            ActionRow action = actions[i];
            if (action.SpawnWeight <= 0 || action.ActionID == ExcludedActionId)
                continue;

            ActionRow presentedRow;
            bool canExecute;
            ActionRejectReason reason;
            if (!resolver.IsActionPresented(run, action.ActionID, out presentedRow, out canExecute, out reason))
                continue;

            pool.Add(new ActionHandOption
            {
                ActionID = action.ActionID,
                Stance = action.ActionStance,
                SpawnWeight = action.SpawnWeight,
                CanExecute = canExecute
            });
            byId[action.ActionID] = new PresentedAction
            {
                Action = presentedRow,
                CanExecute = canExecute,
                ExecuteReason = reason
            };
        }

        if (pool.Count < HandSize)
        {
            Debug.LogError(LogPrefix + "유효 행동 후보가 2개 미만입니다. phase="
                + run.CurrentPhase + " count=" + pool.Count + ". 카드를 만들지 않습니다.");
            return false;
        }

        ActionHandOption first;
        ActionHandOption second;
        if (!TrySelectPair(pool, avoidIds, roll, out first, out second))
        {
            Debug.LogError(LogPrefix + "행동 2장을 고르지 못했습니다. phase=" + run.CurrentPhase);
            return false;
        }

        destination.Add(byId[first.ActionID]);
        destination.Add(byId[second.ActionID]);
        return true;
    }

    public static bool TrySelectPair(
        List<ActionHandOption> pool,
        IList<string> avoidIds,
        IIntRoll roll,
        out ActionHandOption first,
        out ActionHandOption second)
    {
        first = default(ActionHandOption);
        second = default(ActionHandOption);
        if (pool == null || roll == null)
            return false;

        int usable = 0;
        for (int i = 0; i < pool.Count; i++)
        {
            if (pool[i].SpawnWeight > 0)
                usable++;
        }

        if (usable < HandSize)
            return false;

        if (TrySelectFrom(pool, avoidIds, true, roll, out first, out second))
            return true;
        return TrySelectFrom(pool, null, false, roll, out first, out second);
    }

    static bool TrySelectFrom(
        List<ActionHandOption> pool,
        IList<string> avoidIds,
        bool unseenOnly,
        IIntRoll roll,
        out ActionHandOption first,
        out ActionHandOption second)
    {
        first = default(ActionHandOption);
        second = default(ActionHandOption);
        var universe = new List<ActionHandOption>();
        bool fullHasExec = false;
        bool fullHasStable = false;
        bool fullHasRisky = false;
        for (int i = 0; i < pool.Count; i++)
        {
            ActionHandOption option = pool[i];
            if (option.SpawnWeight <= 0)
                continue;
            if (option.CanExecute)
                fullHasExec = true;
            if (option.Stance == ActionStance.STABLE)
                fullHasStable = true;
            else
                fullHasRisky = true;
            if (unseenOnly && Contains(avoidIds, option.ActionID))
                continue;
            universe.Add(option);
        }

        if (universe.Count < HandSize)
            return false;

        bool requireMix = fullHasStable && fullHasRisky;
        bool universeHasExec = false;
        bool universeHasStable = false;
        bool universeHasRisky = false;
        for (int i = 0; i < universe.Count; i++)
        {
            if (universe[i].CanExecute)
                universeHasExec = true;
            if (universe[i].Stance == ActionStance.STABLE)
                universeHasStable = true;
            else
                universeHasRisky = true;
        }

        if (fullHasExec && !universeHasExec)
            return false;
        if (requireMix && (!universeHasStable || !universeHasRisky))
            return false;
        if (requireMix && fullHasExec && !CanMixWithExecutable(universe))
            return false;

        SortById(universe);
        List<ActionHandOption> firstPool = BuildFirstPool(universe, requireMix, fullHasExec);
        if (firstPool.Count == 0)
            return false;

        first = Weighted(firstPool, roll);
        var secondPool = new List<ActionHandOption>();
        for (int i = 0; i < universe.Count; i++)
        {
            ActionHandOption option = universe[i];
            if (option.ActionID == first.ActionID)
                continue;
            if (requireMix && option.Stance == first.Stance)
                continue;
            if (fullHasExec && !first.CanExecute && !option.CanExecute)
                continue;
            secondPool.Add(option);
        }

        if (secondPool.Count == 0)
            return false;

        second = Weighted(secondPool, roll);
        return true;
    }

    static List<ActionHandOption> BuildFirstPool(List<ActionHandOption> universe, bool requireMix, bool requireExec)
    {
        var pool = new List<ActionHandOption>();
        if (requireMix && requireExec)
        {
            for (int i = 0; i < universe.Count; i++)
            {
                if (universe[i].CanExecute && HasOpposite(universe, universe[i]))
                    pool.Add(universe[i]);
            }

            return pool;
        }

        if (requireMix)
        {
            for (int i = 0; i < universe.Count; i++)
            {
                if (universe[i].Stance == ActionStance.STABLE)
                    pool.Add(universe[i]);
            }

            return pool;
        }

        if (requireExec)
        {
            for (int i = 0; i < universe.Count; i++)
            {
                if (universe[i].CanExecute)
                    pool.Add(universe[i]);
            }

            return pool;
        }

        for (int i = 0; i < universe.Count; i++)
            pool.Add(universe[i]);
        return pool;
    }

    static bool CanMixWithExecutable(List<ActionHandOption> universe)
    {
        for (int i = 0; i < universe.Count; i++)
        {
            if (universe[i].CanExecute && HasOpposite(universe, universe[i]))
                return true;
        }

        return false;
    }

    static bool HasOpposite(List<ActionHandOption> universe, ActionHandOption option)
    {
        for (int i = 0; i < universe.Count; i++)
        {
            if (universe[i].ActionID != option.ActionID && universe[i].Stance != option.Stance)
                return true;
        }

        return false;
    }

    static ActionHandOption Weighted(List<ActionHandOption> options, IIntRoll roll)
    {
        int total = 0;
        for (int i = 0; i < options.Count; i++)
            total += options[i].SpawnWeight;
        int ticket = roll.Next(total);
        int cursor = 0;
        for (int i = 0; i < options.Count; i++)
        {
            cursor += options[i].SpawnWeight;
            if (ticket < cursor)
                return options[i];
        }

        return options[options.Count - 1];
    }

    static void SortById(List<ActionHandOption> options)
    {
        options.Sort(CompareId);
    }

    static int CompareId(ActionHandOption left, ActionHandOption right)
    {
        return string.CompareOrdinal(left.ActionID, right.ActionID);
    }

    static bool Contains(IList<string> ids, string id)
    {
        if (ids == null || string.IsNullOrEmpty(id))
            return false;
        for (int i = 0; i < ids.Count; i++)
        {
            if (ids[i] == id)
                return true;
        }

        return false;
    }
}
