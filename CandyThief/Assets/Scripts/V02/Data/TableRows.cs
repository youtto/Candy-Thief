public enum ActionType
{
    REQUIRED,
    OPTIONAL
}

public enum ConditionKey
{
    NONE,
    ALWAYS,
    CANDY_AT_LEAST_1,
    REQUIRED_ACTION_SUCCESS,
    MOVE_COMPLETED,
    INFILTRATE_COMPLETED,
    GAME_ENDED
}

public enum GameConfigValueType
{
    INT,
    FLOAT,
    BOOL
}

public sealed class GameConfigRow
{
    public string Key;
    public string Value;
    public GameConfigValueType ValueType;
    public string Description;
    public string Notes;
}

public sealed class ShopRow
{
    public string ShopID;
    public string ShopName;
    public int Difficulty;
    public int StartTurn;
    public int StartAlert;
    public int SuccessRateBonusPp;
    public float CandyMultiplier;
    public int EventRateBonusPp;
    public string AllowedEventTags;
    public string BackgroundImageKey;
    public ConditionKey UnlockCondition;
    public bool Enabled;
    public string Notes;
}

public sealed class PhaseRow
{
    public string PhaseID;
    public string PhaseName;
    public int PhaseOrder;
    public bool IsRequired;
    public ConditionKey EntryCondition;
    public ConditionKey CompleteCondition;
    public string NextPhaseID;
    public bool AllowOptionalActions;
    public string Notes;
}

public sealed class ActionRow
{
    public string ActionID;
    public string PhaseID;
    public ActionType ActionType;
    public string ActionName;
    public string Description;
    public int SuccessRate;
    public int TurnCost;
    public int FailAlert;
    public int SuccessCandy;
    public int SuccessAlert;
    public int FailCandy;
    public int SuccessTurnDelta;
    public int FailTurnDelta;
    public bool Repeatable;
    public ConditionKey ShowCondition;
    public string SuccessNextPhase;
    public bool EventEligible;
    public bool Enabled;
    public string Notes;
}

public sealed class EventRow
{
    public string EventID;
    public string EventName;
    public string EventDescription;
    public string PhaseFilter;
    public string EventTags;
    public int Weight;
    public int MinAlert;
    public int MaxAlert;
    public int MinRemainingTurn;
    public int MaxRemainingTurn;
    public bool Repeatable;
    public string ImageKey;
    public bool Enabled;
    public string Notes;
}

public sealed class EventOptionRow
{
    public string OptionID;
    public string EventID;
    public int OptionOrder;
    public string OptionName;
    public string Description;
    public int SuccessRate;
    public int TurnCost;
    public int SuccessCandy;
    public int FailCandy;
    public int SuccessAlert;
    public int FailAlert;
    public int SuccessTurnDelta;
    public int FailTurnDelta;
    public string SuccessEffectID;
    public string FailEffectID;
    public bool Enabled;
    public string Notes;
}

public sealed class AchievementRow
{
    public string AchievementID;
    public string TitleName;
    public string Description;
    public string ConditionType;
    public int TargetValue;
    public string ConditionParams;
    public string Rarity;
    public string IconImageKey;
    public bool Hidden;
    public bool Enabled;
    public string Notes;
}

public sealed class AssetRow
{
    public string AssetID;
    public string ImageKey;
    public string AssetType;
    public string FilePath;
    public string PromptKey;
    public string ImagePromptKR;
    public string GenerationStatus;
    public string ReviewStatus;
    public bool Enabled;
    public string Notes;
}

public sealed class EffectRow
{
    public string EffectID;
    public string EffectType;
    public int Value;
    public int DurationActions;
    public string Target;
    public string Description;
    public bool Implemented;
    public bool Enabled;
    public string Notes;
}

public sealed class ResultFeedbackRow
{
    public string FeedbackID;
    public string SourceType;
    public string SourceID;
    public string SuccessText;
    public string FailText;
    public string SuccessPresentationID;
    public string FailPresentationID;
    public bool Enabled;
    public string Notes;
}
