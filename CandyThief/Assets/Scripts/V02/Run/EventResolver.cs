using System;
using System.Collections.Generic;
using UnityEngine;

public interface IIntRoll
{
    int Next(int maxExclusive);
}

public sealed class UnityIntRoll : IIntRoll
{
    public int Next(int maxExclusive)
    {
        if (maxExclusive <= 0)
            return 0;
        return UnityEngine.Random.Range(0, maxExclusive);
    }
}

public sealed class FixedIntRoll : IIntRoll
{
    readonly int value;

    public FixedIntRoll(int value)
    {
        this.value = value;
    }

    public int Next(int maxExclusive)
    {
        if (maxExclusive <= 0)
            return 0;
        if (value < 0)
            return 0;
        if (value >= maxExclusive)
            return maxExclusive - 1;
        return value;
    }
}

public interface IEventPick
{
    EventRow Pick(IReadOnlyList<EventRow> candidates);
}

public sealed class WeightedEventPick : IEventPick
{
    readonly IIntRoll intRoll;

    public WeightedEventPick(IIntRoll intRoll)
    {
        this.intRoll = intRoll ?? new UnityIntRoll();
    }

    public WeightedEventPick() : this(null)
    {
    }

    public EventRow Pick(IReadOnlyList<EventRow> candidates)
    {
        if (candidates == null || candidates.Count == 0)
            return null;

        int total = 0;
        for (int i = 0; i < candidates.Count; i++)
        {
            int weight = candidates[i].Weight;
            if (weight > 0)
                total += weight;
        }

        if (total <= 0)
            return null;

        int roll = intRoll.Next(total);
        int cursor = 0;
        for (int i = 0; i < candidates.Count; i++)
        {
            int weight = candidates[i].Weight;
            if (weight <= 0)
                continue;
            cursor += weight;
            if (roll < cursor)
                return candidates[i];
        }

        return candidates[candidates.Count - 1];
    }
}

public sealed class FixedEventPick : IEventPick
{
    readonly string eventId;

    public FixedEventPick(string eventId)
    {
        this.eventId = eventId;
    }

    public EventRow Pick(IReadOnlyList<EventRow> candidates)
    {
        if (candidates == null || string.IsNullOrEmpty(eventId))
            return null;

        for (int i = 0; i < candidates.Count; i++)
        {
            if (candidates[i].EventID == eventId)
                return candidates[i];
        }

        return null;
    }
}

public enum EventDrawRejectReason
{
    None,
    Ended,
    MissingData,
    NotDue,
    AlreadyPending,
    CheckConsumed,
    TriggerFailed,
    NoCandidates
}

public enum EventOptionRejectReason
{
    None,
    Ended,
    MissingData,
    NoPending,
    UnknownOption,
    WrongEvent,
    InsufficientTurns
}

public struct EventDrawResult
{
    public bool Drawn;
    public EventDrawRejectReason RejectReason;
    public bool CheckDue;
    public bool TriggerSucceeded;
    public int EventChance;
    public string EventID;
    public int CandidateCount;
}

public struct EventOptionResolveResult
{
    public bool Applied;
    public EventOptionRejectReason RejectReason;
    public string OptionID;
    public string EventID;
    public bool Success;
    public int SuccessChance;
}

public struct PresentedOption
{
    public EventOptionRow Option;
    public bool CanExecute;
    public EventOptionRejectReason ExecuteReason;
}

public sealed class EventResolver
{
    public const string LogPrefix = "[EventResolver] ";

    readonly CsvDatabase database;
    readonly IChanceRoll chanceRoll;
    readonly IEventPick eventPick;
    readonly List<EventRow> candidateBuffer = new List<EventRow>();

    public EventResolver(CsvDatabase database, IChanceRoll chanceRoll, IEventPick eventPick)
    {
        this.database = database;
        this.chanceRoll = chanceRoll ?? new UnityChanceRoll();
        this.eventPick = eventPick ?? new WeightedEventPick();
    }

    public EventResolver(CsvDatabase database) : this(database, null, null)
    {
    }

    public int ComputeOptionChance(EventOptionRow option, ShopRow shop)
    {
        int chance = option.SuccessRate + shop.SuccessRateBonusPp;
        int min = database.GetInt("MIN_SUCCESS_RATE");
        int max = database.GetInt("MAX_SUCCESS_RATE");
        if (chance < min)
            return min;
        if (chance > max)
            return max;
        return chance;
    }

    public bool IsCandidate(EventRow evt, RunState run, ShopRow shop, string phaseBeforeAction)
    {
        if (evt == null || run == null || shop == null || !evt.Enabled)
            return false;

        if (!MatchesPhaseFilter(evt.PhaseFilter, phaseBeforeAction))
            return false;

        if (!TagsOverlap(evt.EventTags, shop.AllowedEventTags))
            return false;

        if (run.Alert < evt.MinAlert || run.Alert > evt.MaxAlert)
            return false;

        if (run.RemainingTurns < evt.MinRemainingTurn || run.RemainingTurns > evt.MaxRemainingTurn)
            return false;

        if (!evt.Repeatable && run.HasSucceededEvent(evt.EventID))
            return false;

        return true;
    }

    public void CollectCandidates(RunState run, string phaseBeforeAction, List<EventRow> destination)
    {
        if (destination == null)
            return;

        destination.Clear();
        if (run == null || database == null || !database.Loaded || database.HasErrors)
            return;

        ShopRow shop;
        if (!database.TryGetShop(run.ShopID, out shop))
            return;
        shop = run.Shop ?? shop; // 런 시작 스냅샷. 이벤트 추첨 규칙은 그대로.

        var events = database.EnabledEvents;
        for (int i = 0; i < events.Count; i++)
        {
            if (IsCandidate(events[i], run, shop, phaseBeforeAction))
                destination.Add(events[i]);
        }
    }

    public EventOptionRejectReason EvaluateSelectOption(RunState run, string optionId, out EventOptionRow option)
    {
        option = null;
        if (run == null)
            return EventOptionRejectReason.MissingData;

        if (run.IsEnded)
            return EventOptionRejectReason.Ended;

        if (!run.HasPendingEvent)
            return EventOptionRejectReason.NoPending;

        if (database == null || !database.Loaded || database.HasErrors)
            return EventOptionRejectReason.MissingData;

        if (!database.TryGetEventOption(optionId, out option))
            return EventOptionRejectReason.UnknownOption;

        if (option.EventID != run.PendingEventID)
            return EventOptionRejectReason.WrongEvent;

        if (!run.CanAffordTurnCost(option.TurnCost))
            return EventOptionRejectReason.InsufficientTurns;

        if (!database.TryGetShop(run.ShopID, out _))
            return EventOptionRejectReason.MissingData;

        return EventOptionRejectReason.None;
    }

    public void CollectAvailableOptions(RunState run, List<EventOptionRow> destination)
    {
        if (destination == null)
            return;

        destination.Clear();
        if (database == null || !database.Loaded || database.HasErrors)
            return;

        var options = database.EnabledEventOptions;
        for (int i = 0; i < options.Count; i++)
        {
            EventOptionRow selected;
            if (EvaluateSelectOption(run, options[i].OptionID, out selected) == EventOptionRejectReason.None)
                destination.Add(selected);
        }
    }

    public bool IsOptionPresented(RunState run, string optionId, out EventOptionRow option, out bool canExecute, out EventOptionRejectReason executeReason)
    {
        executeReason = EvaluateSelectOption(run, optionId, out option);
        if (executeReason == EventOptionRejectReason.None)
        {
            canExecute = true;
            return true;
        }

        if (executeReason == EventOptionRejectReason.InsufficientTurns)
        {
            canExecute = false;
            return true;
        }

        canExecute = false;
        return false;
    }

    public void CollectPresentedOptions(RunState run, List<PresentedOption> destination)
    {
        if (destination == null)
            return;

        destination.Clear();
        if (database == null || !database.Loaded || database.HasErrors)
            return;

        var options = database.EnabledEventOptions;
        for (int i = 0; i < options.Count; i++)
        {
            EventOptionRow selected;
            bool canExecute;
            EventOptionRejectReason reason;
            if (!IsOptionPresented(run, options[i].OptionID, out selected, out canExecute, out reason))
                continue;

            destination.Add(new PresentedOption
            {
                Option = selected,
                CanExecute = canExecute,
                ExecuteReason = reason
            });
        }

        destination.Sort(CompareOptionOrder);
    }

    static int CompareOptionOrder(PresentedOption a, PresentedOption b)
    {
        int orderA = a.Option != null ? a.Option.OptionOrder : 0;
        int orderB = b.Option != null ? b.Option.OptionOrder : 0;
        return orderA.CompareTo(orderB);
    }

    public bool TryDraw(RunState run, bool eventCheckDue, int eventChance, string phaseBeforeAction, out EventDrawResult result)
    {
        result = new EventDrawResult
        {
            CheckDue = eventCheckDue,
            EventChance = eventChance,
            EventID = string.Empty
        };

        if (run == null || database == null || !database.Loaded || database.HasErrors)
        {
            result.RejectReason = EventDrawRejectReason.MissingData;
            Debug.LogError(LogPrefix + "이벤트 추첨 데이터가 없습니다.");
            return false;
        }

        if (run.IsEnded)
        {
            result.RejectReason = EventDrawRejectReason.Ended;
            Debug.Log(LogPrefix + "종료된 런은 이벤트를 추첨하지 않습니다. Status=" + run.Status);
            return false;
        }

        if (run.HasPendingEvent)
        {
            result.RejectReason = EventDrawRejectReason.AlreadyPending;
            Debug.LogError(LogPrefix + "이미 대기 중인 이벤트가 있습니다. pending=" + run.PendingEventID);
            return false;
        }

        if (!eventCheckDue)
        {
            result.RejectReason = EventDrawRejectReason.NotDue;
            return false;
        }

        if (!run.TryConsumeEventCheck())
        {
            result.RejectReason = EventDrawRejectReason.CheckConsumed;
            Debug.Log(LogPrefix + "이번 행동의 이벤트 추첨은 이미 소모되었습니다.");
            return false;
        }

        bool triggered = chanceRoll.Succeeds(eventChance);
        result.TriggerSucceeded = triggered;
        if (!triggered)
        {
            result.RejectReason = EventDrawRejectReason.TriggerFailed;
            Debug.Log(LogPrefix + "이벤트 발생 확률 실패. chance=" + eventChance);
            return false;
        }

        CollectCandidates(run, phaseBeforeAction, candidateBuffer);
        result.CandidateCount = candidateBuffer.Count;
        if (candidateBuffer.Count == 0)
        {
            result.RejectReason = EventDrawRejectReason.NoCandidates;
            Debug.Log(LogPrefix + "이벤트 후보가 없습니다.");
            return false;
        }

        EventRow picked = eventPick.Pick(candidateBuffer);
        if (picked == null)
        {
            result.RejectReason = EventDrawRejectReason.NoCandidates;
            Debug.Log(LogPrefix + "가중치 선정 결과가 없습니다.");
            return false;
        }

        if (!run.TrySetPendingEvent(picked.EventID))
        {
            result.RejectReason = EventDrawRejectReason.AlreadyPending;
            return false;
        }

        result.Drawn = true;
        result.EventID = picked.EventID;
        result.RejectReason = EventDrawRejectReason.None;
        Debug.Log(LogPrefix + "이벤트 발생. EventID=" + picked.EventID
            + " candidates=" + candidateBuffer.Count
            + " chance=" + eventChance);
        return true;
    }

    public bool TryResolveOption(RunState run, string optionId, out EventOptionResolveResult result)
    {
        result = new EventOptionResolveResult
        {
            OptionID = optionId ?? string.Empty,
            EventID = run != null ? run.PendingEventID : string.Empty
        };

        EventOptionRow option;
        result.RejectReason = EvaluateSelectOption(run, optionId, out option);
        if (result.RejectReason != EventOptionRejectReason.None)
        {
            LogOptionReject(run, option, result.OptionID, result.RejectReason);
            return false;
        }

        ShopRow shop;
        database.TryGetShop(run.ShopID, out shop);
        shop = run.Shop ?? shop;

        int chance = ComputeOptionChance(option, shop);
        bool success = chanceRoll.Succeeds(chance);
        string lastActionId = run.LastActionID;
        if (!run.TryApplyEventOption(option, success))
        {
            result.RejectReason = EventOptionRejectReason.InsufficientTurns;
            return false;
        }

        result.Applied = true;
        result.Success = success;
        result.SuccessChance = chance;
        result.EventID = option.EventID;
        run.LogSnapshot(success ? "event option success" : "event option fail");
        Debug.Log(LogPrefix + "옵션 처리. option=" + option.OptionID
            + " event=" + option.EventID
            + " success=" + success
            + " lastAction=" + lastActionId);
        return true;
    }

    void LogOptionReject(RunState run, EventOptionRow option, string optionId, EventOptionRejectReason reason)
    {
        switch (reason)
        {
            case EventOptionRejectReason.MissingData:
                Debug.LogError(LogPrefix + "이벤트 옵션 데이터가 없습니다.");
                break;
            case EventOptionRejectReason.Ended:
                Debug.LogError(LogPrefix + "종료된 런은 이벤트 옵션을 처리할 수 없습니다. Status=" + run.Status);
                break;
            case EventOptionRejectReason.NoPending:
                Debug.LogError(LogPrefix + "대기 중인 이벤트가 없습니다. option=" + optionId);
                break;
            case EventOptionRejectReason.UnknownOption:
                Debug.LogError(LogPrefix + "OptionID를 찾을 수 없습니다: " + optionId);
                break;
            case EventOptionRejectReason.WrongEvent:
                Debug.LogError(LogPrefix + "다른 이벤트의 옵션입니다. pending=" + run.PendingEventID
                    + " option=" + optionId
                    + " optionEvent=" + (option != null ? option.EventID : "-"));
                break;
            case EventOptionRejectReason.InsufficientTurns:
                Debug.LogError(LogPrefix + "턴이 부족합니다. option=" + option.OptionID
                    + " cost=" + option.TurnCost
                    + " remaining=" + run.RemainingTurns);
                break;
        }
    }

    static bool MatchesPhaseFilter(string phaseFilter, string phaseBeforeAction)
    {
        if (string.IsNullOrEmpty(phaseFilter))
            return false;
        if (string.Equals(phaseFilter, "ALL", StringComparison.Ordinal))
            return true;
        return string.Equals(phaseFilter, phaseBeforeAction, StringComparison.Ordinal);
    }

    static bool TagsOverlap(string eventTags, string allowedTags)
    {
        if (string.IsNullOrEmpty(eventTags) || string.IsNullOrEmpty(allowedTags))
            return false;

        string[] eventParts = eventTags.Split('|');
        string[] allowedParts = allowedTags.Split('|');
        for (int i = 0; i < eventParts.Length; i++)
        {
            string eventTag = eventParts[i].Trim();
            if (eventTag.Length == 0)
                continue;
            for (int j = 0; j < allowedParts.Length; j++)
            {
                if (string.Equals(eventTag, allowedParts[j].Trim(), StringComparison.Ordinal))
                    return true;
            }
        }

        return false;
    }
}
