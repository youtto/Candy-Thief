using System;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

public sealed class CsvDatabase
{
    public const string ResourceFolder = "Table";
    public const string LogPrefix = "[CsvDatabase] ";
    public const int MaxShopNameLength = 10; // D-021: spaces included

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

    static readonly string[] RequiredGameConfigKeys =
    {
        "MAX_ALERT",
        "EVENT_TRIGGER_RATE",
        "MAX_EVENT_PER_ACTION",
        "MIN_CANDY_TO_ESCAPE",
        "BASE_SCORE_MULTIPLIER",
        "EVENT_ON_FAILED_ACTION",
        "EVENT_CHAIN_ENABLED",
        "MAX_SUCCESS_RATE",
        "MIN_SUCCESS_RATE",
        "ACTION_SELECT_TIME_SEC"
    };

    public static CsvDatabase Current { get; private set; }

    readonly Dictionary<string, GameConfigRow> configs = new Dictionary<string, GameConfigRow>(StringComparer.Ordinal);
    readonly Dictionary<string, ShopRow> shops = new Dictionary<string, ShopRow>(StringComparer.Ordinal);
    readonly Dictionary<string, PhaseRow> phases = new Dictionary<string, PhaseRow>(StringComparer.Ordinal);
    readonly Dictionary<string, ActionRow> actions = new Dictionary<string, ActionRow>(StringComparer.Ordinal);
    readonly Dictionary<string, EventRow> events = new Dictionary<string, EventRow>(StringComparer.Ordinal);
    readonly Dictionary<string, EventOptionRow> eventOptions = new Dictionary<string, EventOptionRow>(StringComparer.Ordinal);
    readonly Dictionary<string, AchievementRow> achievements = new Dictionary<string, AchievementRow>(StringComparer.Ordinal);
    readonly Dictionary<string, AssetRow> assetsById = new Dictionary<string, AssetRow>(StringComparer.Ordinal);
    readonly Dictionary<string, AssetRow> assetsByImageKey = new Dictionary<string, AssetRow>(StringComparer.Ordinal);
    readonly Dictionary<string, EffectRow> effects = new Dictionary<string, EffectRow>(StringComparer.Ordinal);
    readonly Dictionary<string, ResultFeedbackRow> resultFeedbackByKey = new Dictionary<string, ResultFeedbackRow>(StringComparer.Ordinal);
    readonly Dictionary<string, string> feedbackImageKeyOwner = new Dictionary<string, string>(StringComparer.Ordinal);

    readonly List<ShopRow> shopsOrdered = new List<ShopRow>();
    readonly List<PhaseRow> phasesOrdered = new List<PhaseRow>();
    readonly List<ActionRow> actionsOrdered = new List<ActionRow>();
    readonly List<EventRow> eventsOrdered = new List<EventRow>();
    readonly List<EventOptionRow> eventOptionsOrdered = new List<EventOptionRow>();
    readonly List<AchievementRow> achievementsOrdered = new List<AchievementRow>();
    readonly List<AssetRow> assetsOrdered = new List<AssetRow>();
    readonly List<EffectRow> effectsOrdered = new List<EffectRow>();
    readonly List<ResultFeedbackRow> resultFeedbackOrdered = new List<ResultFeedbackRow>();

    // D-026 소문 테이블 (선택 로드). 후보 행은 FK 검증 통과분만 candidatesByShop에 들어간다.
    readonly Dictionary<string, ShopRumorRow> rumors = new Dictionary<string, ShopRumorRow>(StringComparer.Ordinal);
    readonly List<ShopRumorRow> rumorsOrdered = new List<ShopRumorRow>();
    readonly List<ShopRumorCandidateRow> rumorCandidatesRaw = new List<ShopRumorCandidateRow>();
    readonly Dictionary<string, List<ShopRumorCandidateRow>> rumorCandidatesByShop = new Dictionary<string, List<ShopRumorCandidateRow>>(StringComparer.Ordinal);
    readonly HashSet<string> rumorCandidatePairs = new HashSet<string>(StringComparer.Ordinal);
    readonly HashSet<string> allRumorIds = new HashSet<string>(StringComparer.Ordinal);
    static readonly IReadOnlyList<ShopRumorCandidateRow> NoRumorCandidates = new List<ShopRumorCandidateRow>();
    const int MaxRumorsPerShopForValidation = 2;

    readonly HashSet<string> allShopIds = new HashSet<string>(StringComparer.Ordinal);
    readonly HashSet<string> allPhaseIds = new HashSet<string>(StringComparer.Ordinal);
    readonly HashSet<string> allActionIds = new HashSet<string>(StringComparer.Ordinal);
    readonly HashSet<string> allEventIds = new HashSet<string>(StringComparer.Ordinal);
    readonly HashSet<string> allOptionIds = new HashSet<string>(StringComparer.Ordinal);
    readonly HashSet<string> allAchievementIds = new HashSet<string>(StringComparer.Ordinal);
    readonly HashSet<string> allAssetIds = new HashSet<string>(StringComparer.Ordinal);
    readonly HashSet<string> allImageKeys = new HashSet<string>(StringComparer.Ordinal);
    readonly HashSet<string> allEffectIds = new HashSet<string>(StringComparer.Ordinal);
    readonly HashSet<string> allFeedbackIds = new HashSet<string>(StringComparer.Ordinal);

    public bool Loaded { get; private set; }
    public bool HasErrors { get; private set; }
    public int ErrorCount { get; private set; }

    public IReadOnlyList<ShopRow> EnabledShops => shopsOrdered;
    public IReadOnlyList<PhaseRow> Phases => phasesOrdered;
    public IReadOnlyList<ActionRow> EnabledActions => actionsOrdered;
    public IReadOnlyList<EventRow> EnabledEvents => eventsOrdered;
    public IReadOnlyList<EventOptionRow> EnabledEventOptions => eventOptionsOrdered;
    public IReadOnlyList<AchievementRow> EnabledAchievements => achievementsOrdered;
    public IReadOnlyList<AssetRow> EnabledAssets => assetsOrdered;
    public IReadOnlyList<EffectRow> EnabledEffects => effectsOrdered;
    public IReadOnlyList<ResultFeedbackRow> EnabledResultFeedback => resultFeedbackOrdered;
    public IReadOnlyList<ShopRumorRow> EnabledRumors => rumorsOrdered;

    public bool TryGetRumor(string rumorId, out ShopRumorRow row)
    {
        row = null;
        return !string.IsNullOrEmpty(rumorId) && rumors.TryGetValue(rumorId, out row);
    }

    public IReadOnlyList<ShopRumorCandidateRow> GetRumorCandidates(string shopId)
    {
        List<ShopRumorCandidateRow> list;
        if (!string.IsNullOrEmpty(shopId) && rumorCandidatesByShop.TryGetValue(shopId, out list))
            return list;

        return NoRumorCandidates;
    }

    public bool IsRumorCandidate(string shopId, string rumorId)
    {
        return rumorCandidatePairs.Contains(RumorPairKey(shopId, rumorId));
    }

    static string RumorPairKey(string shopId, string rumorId)
    {
        return shopId + "|" + rumorId;
    }

    public static CsvDatabase LoadFromResources()
    {
        var database = new CsvDatabase();
        var texts = new Dictionary<string, string>(StringComparer.Ordinal);
        for (int i = 0; i < GameplayTables.Length; i++)
        {
            string tableName = GameplayTables[i];
            string csvText;
            if (database.TryLoadResourceText(tableName, out csvText))
                texts[tableName] = csvText;
        }

        database.ParseLoadedTexts(texts);
        database.TryLoadPresentationTableFromResources();
        database.TryLoadRumorTablesFromResources();
        Current = database;
        return database;
    }

    public static CsvDatabase LoadFromTexts(IReadOnlyDictionary<string, string> tableTexts)
    {
        var database = new CsvDatabase();
        for (int i = 0; i < GameplayTables.Length; i++)
        {
            string tableName = GameplayTables[i];
            if (tableTexts == null || !tableTexts.ContainsKey(tableName))
                database.Error("{0} 텍스트가 없습니다.", tableName);
        }

        database.ParseLoadedTexts(tableTexts);
        string feedbackText;
        if (tableTexts != null && tableTexts.TryGetValue("ResultFeedbackTable", out feedbackText))
            database.LoadPresentationTableText(feedbackText);
        string rumorText = null;
        string rumorCandidateText = null;
        if (tableTexts != null)
        {
            tableTexts.TryGetValue("ShopRumorTable", out rumorText);
            tableTexts.TryGetValue("ShopRumorCandidate", out rumorCandidateText);
        }

        database.LoadRumorTableTexts(rumorText, rumorCandidateText);
        return database;
    }

    public bool TryGetConfig(string key, out GameConfigRow row)
    {
        return configs.TryGetValue(key, out row);
    }

    public bool TryGetShop(string shopId, out ShopRow row)
    {
        return shops.TryGetValue(shopId, out row);
    }

    public bool TryGetPhase(string phaseId, out PhaseRow row)
    {
        return phases.TryGetValue(phaseId, out row);
    }

    public bool TryGetAction(string actionId, out ActionRow row)
    {
        return actions.TryGetValue(actionId, out row);
    }

    public bool TryGetEvent(string eventId, out EventRow row)
    {
        return events.TryGetValue(eventId, out row);
    }

    public bool TryGetEventOption(string optionId, out EventOptionRow row)
    {
        return eventOptions.TryGetValue(optionId, out row);
    }

    public bool TryGetAchievement(string achievementId, out AchievementRow row)
    {
        return achievements.TryGetValue(achievementId, out row);
    }

    public bool TryGetAssetById(string assetId, out AssetRow row)
    {
        return assetsById.TryGetValue(assetId, out row);
    }

    public bool TryGetAssetByImageKey(string imageKey, out AssetRow row)
    {
        return assetsByImageKey.TryGetValue(imageKey, out row);
    }

    public bool TryGetEffect(string effectId, out EffectRow row)
    {
        return effects.TryGetValue(effectId, out row);
    }

    public bool TryGetResultFeedback(string sourceType, string sourceId, out ResultFeedbackRow row)
    {
        return resultFeedbackByKey.TryGetValue(FeedbackKey(sourceType, sourceId), out row);
    }

    public static string FeedbackKey(string sourceType, string sourceId)
    {
        return (sourceType ?? string.Empty) + "|" + (sourceId ?? string.Empty);
    }

    public int GetInt(string key)
    {
        if (!TryGetTypedValue(key, GameConfigValueType.INT, out GameConfigRow row))
            return 0;

        return ParseIntValue(row.Value);
    }

    public float GetFloat(string key)
    {
        if (!TryGetTypedValue(key, GameConfigValueType.FLOAT, out GameConfigRow row))
            return 0f;

        return ParseFloatValue(row.Value);
    }

    public bool GetBool(string key)
    {
        if (!TryGetTypedValue(key, GameConfigValueType.BOOL, out GameConfigRow row))
            return false;

        return ParseBoolValue(row.Value);
    }

    public void LogEnabledShops()
    {
        if (shopsOrdered.Count == 0)
        {
            Error("활성 가게가 없습니다. ShopTable을 확인하세요.");
            return;
        }

        for (int i = 0; i < shopsOrdered.Count; i++)
        {
            ShopRow shop = shopsOrdered[i];
            Info(
                "가게 {0}: {1} (Difficulty={2}, StartTurn={3}, StartAlert={4}, SuccessRateBonusPp={5}, CandyMultiplier={6}, EventRateBonusPp={7})",
                shop.ShopID,
                shop.ShopName,
                shop.Difficulty,
                shop.StartTurn,
                shop.StartAlert,
                shop.SuccessRateBonusPp,
                shop.CandyMultiplier.ToString(CultureInfo.InvariantCulture),
                shop.EventRateBonusPp);
        }
    }

    public static CsvDatabase LoadSingleTableFromText(string tableName, string csvText)
    {
        var database = new CsvDatabase();
        CsvSheet sheet;
        if (!database.TryReadSheet(tableName, csvText, out sheet))
            return database;

        database.LoadSheet(sheet);
        database.Loaded = true;
        return database;
    }

    public static int CountValidationErrors(string tableName, string csvText)
    {
        return LoadSingleTableFromText(tableName, csvText).ErrorCount;
    }

    void ParseLoadedTexts(IReadOnlyDictionary<string, string> tableTexts)
    {
        Info("CSV 로딩 시작. 경로=Resources/{0}/ (README.csv 제외)", ResourceFolder);

        for (int i = 0; i < GameplayTables.Length; i++)
        {
            string tableName = GameplayTables[i];
            string csvText;
            if (tableTexts == null || !tableTexts.TryGetValue(tableName, out csvText))
                continue;

            CsvSheet sheet;
            if (!TryReadSheet(tableName, csvText, out sheet))
                continue;

            LoadSheet(sheet);
        }

        ValidateReferences();
        ValidateRequiredConfigKeys();
        ValidateActionSelectTime();
        Loaded = true;

        if (HasErrors)
            Error("CSV 로딩 실패. 오류 {0}건. ID 조회 전에 테이블을 수정하세요.", ErrorCount);
        else
            Info("CSV 로딩 성공. 오류 0건. 가게 {0} / 페이즈 {1} / 행동 {2} / 이벤트 {3} / 선택지 {4}",
                shopsOrdered.Count,
                phasesOrdered.Count,
                actionsOrdered.Count,
                eventsOrdered.Count,
                eventOptionsOrdered.Count);
    }

    void LoadSheet(CsvSheet sheet)
    {
        switch (sheet.TableName)
        {
            case "GameConfig":
                LoadGameConfig(sheet);
                break;
            case "ShopTable":
                LoadShops(sheet);
                break;
            case "PhaseTable":
                LoadPhases(sheet);
                break;
            case "ActionTable":
                LoadActions(sheet);
                break;
            case "EventTable":
                LoadEvents(sheet);
                break;
            case "EventOptionTable":
                LoadEventOptions(sheet);
                break;
            case "AchievementTable":
                LoadAchievements(sheet);
                break;
            case "AssetTable":
                LoadAssets(sheet);
                break;
            case "EffectTable":
                LoadEffects(sheet);
                break;
            case "ResultFeedbackTable":
                LoadResultFeedback(sheet);
                break;
            case "ShopRumorTable":
                LoadRumors(sheet);
                break;
            case "ShopRumorCandidate":
                LoadRumorCandidates(sheet);
                break;
            default:
                Error("{0}은(는) 게임플레이 테이블이 아닙니다.", sheet.TableName);
                break;
        }
    }

    bool TryLoadResourceText(string tableName, out string csvText)
    {
        csvText = null;
        string resourcePath = ResourceFolder + "/" + tableName;
        TextAsset asset = Resources.Load<TextAsset>(resourcePath);
        if (asset == null)
        {
            Error("{0} 파일을 찾을 수 없습니다. 경로=Resources/{1}", tableName, resourcePath);
            return false;
        }

        csvText = CsvReader.DecodeUtf8(asset.bytes);
        if (string.IsNullOrWhiteSpace(csvText))
        {
            Error("{0} 파일이 비어 있습니다.", tableName);
            return false;
        }

        return true;
    }

    bool TryReadSheet(string tableName, string csvText, out CsvSheet sheet)
    {
        return TryReadSheet(tableName, csvText, out sheet, false);
    }

    bool TryReadSheet(string tableName, string csvText, out CsvSheet sheet, bool presentation)
    {
        sheet = null;
        List<string[]> records = CsvReader.ReadRecords(csvText);
        if (records.Count == 0)
        {
            if (presentation)
                Warn("{0} 헤더가 없습니다. 기본 결과 문구를 사용합니다.", tableName);
            else
                Error("{0} 헤더가 없습니다.", tableName);
            return false;
        }

        sheet = new CsvSheet(tableName, records[0], records);
        return true;
    }

    void LoadGameConfig(CsvSheet sheet)
    {
        if (!sheet.RequireColumns("Key", "Value", "ValueType"))
        {
            ErrorMissingColumns(sheet, "Key", "Value", "ValueType");
            return;
        }

        int parsed = 0;
        for (int i = 0; i < sheet.Rows.Count; i++)
        {
            string[] record = sheet.Rows[i];
            int line = i + 2;
            string key = sheet.Get(record, "Key");
            if (string.IsNullOrWhiteSpace(key))
            {
                Error("{0} {1}행: 필수 컬럼 Key가 비어 있습니다.", sheet.TableName, line);
                continue;
            }

            if (configs.ContainsKey(key))
            {
                Error("{0} {1}행: Key 중복 '{2}'", sheet.TableName, line, key);
                continue;
            }

            GameConfigValueType valueType;
            if (!TryParseEnum(sheet, record, line, "ValueType", out valueType))
                continue;

            string value = sheet.Get(record, "Value");
            if (!ValidateConfigValue(sheet.TableName, line, key, value, valueType))
                continue;

            var row = new GameConfigRow
            {
                Key = key,
                Value = value,
                ValueType = valueType,
                Description = sheet.Get(record, "Description"),
                Notes = sheet.Get(record, "Notes")
            };
            configs.Add(key, row);
            parsed++;
        }

        Info("{0} 로딩: {1}키", sheet.TableName, parsed);
    }

    void LoadShops(CsvSheet sheet)
    {
        string[] required =
        {
            "ShopID", "ShopName", "Difficulty", "StartTurn", "StartAlert",
            "SuccessRateBonusPp", "CandyMultiplier", "EventRateBonusPp",
            "AllowedEventTags", "BackgroundImageKey", "UnlockCondition", "Enabled",
            "SpawnWeight", "DistanceGrade", "ScaleGrade", "SecurityGrade", "GoodsGrade", "EnergyCost"
        };
        if (!sheet.RequireColumns(required))
        {
            ErrorMissingColumns(sheet, required);
            return;
        }

        int parsed = 0;
        int enabled = 0;
        int skipped = 0;
        for (int i = 0; i < sheet.Rows.Count; i++)
        {
            string[] record = sheet.Rows[i];
            int line = i + 2;
            string id;
            if (!TryReadUniqueId(sheet, record, line, "ShopID", allShopIds, out id))
                continue;

            ShopRow row;
            if (!TryBuildShop(sheet, record, line, id, out row))
                continue;

            parsed++;
            if (!row.Enabled)
            {
                skipped++;
                Info("{0} {1}행: Enabled=0 이므로 건너뜀 ({2})", sheet.TableName, line, id);
                continue;
            }

            shops.Add(id, row);
            shopsOrdered.Add(row);
            enabled++;
        }

        Info("{0} 로딩: 파싱 {1}행, 활성 {2}행, 비활성 건너뜀 {3}행", sheet.TableName, parsed, enabled, skipped);
    }

    bool TryBuildShop(CsvSheet sheet, string[] record, int line, string id, out ShopRow row)
    {
        row = null;
        int difficulty, startTurn, startAlert, successBonus, eventBonus, spawnWeight, energyCost;
        float candyMultiplier;
        bool enabled;
        ConditionKey unlock;
        DistanceGrade distance;
        ScaleGrade scale;
        SecurityGrade security;
        GoodsGrade goods;
        if (!TryParseInt(sheet, record, line, "Difficulty", out difficulty)
            || !TryParseInt(sheet, record, line, "StartTurn", out startTurn)
            || !TryParseInt(sheet, record, line, "StartAlert", out startAlert)
            || !TryParseInt(sheet, record, line, "SuccessRateBonusPp", out successBonus)
            || !TryParseFloat(sheet, record, line, "CandyMultiplier", out candyMultiplier)
            || !TryParseInt(sheet, record, line, "EventRateBonusPp", out eventBonus)
            || !TryParseEnum(sheet, record, line, "UnlockCondition", out unlock)
            || !TryParseBool(sheet, record, line, "Enabled", out enabled)
            || !TryParseNonNegativeInt(sheet, record, line, "SpawnWeight", out spawnWeight)
            || !TryParseNonNegativeInt(sheet, record, line, "EnergyCost", out energyCost)
            || !TryParseGrade(sheet, record, line, "DistanceGrade", out distance)
            || !TryParseGrade(sheet, record, line, "ScaleGrade", out scale)
            || !TryParseGrade(sheet, record, line, "SecurityGrade", out security)
            || !TryParseGrade(sheet, record, line, "GoodsGrade", out goods))
            return false;

        string shopName = sheet.Get(record, "ShopName");
        if (shopName.Length > MaxShopNameLength)
        {
            Error("{0} {1}행: ShopName '{2}' 는 공백 포함 {3}자를 넘습니다. ({4}자)",
                sheet.TableName, line, shopName, MaxShopNameLength, shopName.Length);
            return false;
        }

        row = new ShopRow
        {
            ShopID = id,
            ShopName = shopName,
            Difficulty = difficulty,
            StartTurn = startTurn,
            StartAlert = startAlert,
            SuccessRateBonusPp = successBonus,
            CandyMultiplier = candyMultiplier,
            EventRateBonusPp = eventBonus,
            AllowedEventTags = sheet.Get(record, "AllowedEventTags"),
            BackgroundImageKey = sheet.Get(record, "BackgroundImageKey"),
            UnlockCondition = unlock,
            Enabled = enabled,
            SpawnWeight = spawnWeight,
            DistanceGrade = distance,
            ScaleGrade = scale,
            SecurityGrade = security,
            GoodsGrade = goods,
            EnergyCost = energyCost,
            Notes = sheet.Get(record, "Notes")
        };
        return true;
    }

    void LoadPhases(CsvSheet sheet)
    {
        string[] required =
        {
            "PhaseID", "PhaseName", "PhaseOrder", "IsRequired",
            "EntryCondition", "CompleteCondition", "NextPhaseID", "AllowOptionalActions",
            "SituationText"
        };
        if (!sheet.RequireColumns(required))
        {
            ErrorMissingColumns(sheet, required);
            return;
        }

        int parsed = 0;
        for (int i = 0; i < sheet.Rows.Count; i++)
        {
            string[] record = sheet.Rows[i];
            int line = i + 2;
            string id;
            if (!TryReadUniqueId(sheet, record, line, "PhaseID", allPhaseIds, out id))
                continue;

            int order;
            bool isRequired, allowOptional;
            ConditionKey entry, complete;
            if (!TryParseInt(sheet, record, line, "PhaseOrder", out order)
                || !TryParseBool(sheet, record, line, "IsRequired", out isRequired)
                || !TryParseEnum(sheet, record, line, "EntryCondition", out entry)
                || !TryParseEnum(sheet, record, line, "CompleteCondition", out complete)
                || !TryParseBool(sheet, record, line, "AllowOptionalActions", out allowOptional))
                continue;

            var row = new PhaseRow
            {
                PhaseID = id,
                PhaseName = sheet.Get(record, "PhaseName"),
                PhaseOrder = order,
                IsRequired = isRequired,
                EntryCondition = entry,
                CompleteCondition = complete,
                NextPhaseID = sheet.Get(record, "NextPhaseID"),
                AllowOptionalActions = allowOptional,
                SituationText = sheet.Get(record, "SituationText"),
                Notes = sheet.Get(record, "Notes")
            };
            phases.Add(id, row);
            phasesOrdered.Add(row);
            parsed++;
        }

        Info("{0} 로딩: {1}행", sheet.TableName, parsed);
    }

    void LoadActions(CsvSheet sheet)
    {
        string[] required =
        {
            "ActionID", "PhaseID", "ActionType", "ActionName", "Description",
            "SuccessRate", "TurnCost", "FailAlert", "SuccessCandy", "SuccessAlert",
            "FailCandy", "SuccessTurnDelta", "FailTurnDelta", "Repeatable",
            "ShowCondition", "SuccessNextPhase", "EventEligible", "Enabled",
            "ActionStance", "SpawnWeight", "ImageKey", "SuccessCandyMin", "SuccessCandyMax"
        };
        if (!sheet.RequireColumns(required))
        {
            ErrorMissingColumns(sheet, required);
            return;
        }

        int parsed = 0;
        int enabled = 0;
        int skipped = 0;
        for (int i = 0; i < sheet.Rows.Count; i++)
        {
            string[] record = sheet.Rows[i];
            int line = i + 2;
            string id;
            if (!TryReadUniqueId(sheet, record, line, "ActionID", allActionIds, out id))
                continue;

            ActionType actionType;
            ActionStance stance;
            ConditionKey showCondition;
            int successRate, turnCost, failAlert, successCandy, successAlert, failCandy, successTurnDelta, failTurnDelta;
            int spawnWeight, successCandyMin, successCandyMax;
            bool repeatable, eventEligible, enabledFlag;
            if (!TryParseEnum(sheet, record, line, "ActionType", out actionType)
                || !TryParseEnum(sheet, record, line, "ActionStance", out stance)
                || !TryParseInt(sheet, record, line, "SuccessRate", out successRate)
                || !TryParseInt(sheet, record, line, "TurnCost", out turnCost)
                || !TryParseInt(sheet, record, line, "FailAlert", out failAlert)
                || !TryParseInt(sheet, record, line, "SuccessCandy", out successCandy)
                || !TryParseInt(sheet, record, line, "SuccessAlert", out successAlert)
                || !TryParseInt(sheet, record, line, "FailCandy", out failCandy)
                || !TryParseInt(sheet, record, line, "SuccessTurnDelta", out successTurnDelta)
                || !TryParseInt(sheet, record, line, "FailTurnDelta", out failTurnDelta)
                || !TryParseBool(sheet, record, line, "Repeatable", out repeatable)
                || !TryParseEnum(sheet, record, line, "ShowCondition", out showCondition)
                || !TryParseBool(sheet, record, line, "EventEligible", out eventEligible)
                || !TryParseBool(sheet, record, line, "Enabled", out enabledFlag)
                || !TryParseNonNegativeInt(sheet, record, line, "SpawnWeight", out spawnWeight)
                || !TryParseNonNegativeInt(sheet, record, line, "SuccessCandyMin", out successCandyMin)
                || !TryParseNonNegativeInt(sheet, record, line, "SuccessCandyMax", out successCandyMax))
                continue;

            if (successCandyMin > successCandyMax)
            {
                Error("{0} {1}행: SuccessCandyMin={2} 이 SuccessCandyMax={3} 보다 큽니다.", sheet.TableName, line, successCandyMin, successCandyMax);
                continue;
            }

            var row = new ActionRow
            {
                ActionID = id,
                PhaseID = sheet.Get(record, "PhaseID"),
                ActionType = actionType,
                ActionName = sheet.Get(record, "ActionName"),
                Description = sheet.Get(record, "Description"),
                SuccessRate = successRate,
                TurnCost = turnCost,
                FailAlert = failAlert,
                SuccessCandy = successCandy,
                SuccessAlert = successAlert,
                FailCandy = failCandy,
                SuccessTurnDelta = successTurnDelta,
                FailTurnDelta = failTurnDelta,
                Repeatable = repeatable,
                ShowCondition = showCondition,
                SuccessNextPhase = sheet.Get(record, "SuccessNextPhase"),
                EventEligible = eventEligible,
                Enabled = enabledFlag,
                Notes = sheet.Get(record, "Notes"),
                ActionStance = stance,
                SpawnWeight = spawnWeight,
                ImageKey = sheet.Get(record, "ImageKey"),
                SuccessCandyMin = successCandyMin,
                SuccessCandyMax = successCandyMax
            };
            parsed++;
            if (!row.Enabled)
            {
                skipped++;
                Info("{0} {1}행: Enabled=0 이므로 건너뜀 ({2})", sheet.TableName, line, id);
                continue;
            }

            actions.Add(id, row);
            actionsOrdered.Add(row);
            enabled++;
        }

        Info("{0} 로딩: 파싱 {1}행, 활성 {2}행, 비활성 건너뜀 {3}행", sheet.TableName, parsed, enabled, skipped);
    }

    void LoadEvents(CsvSheet sheet)
    {
        string[] required =
        {
            "EventID", "EventName", "EventDescription", "PhaseFilter", "EventTags",
            "Weight", "MinAlert", "MaxAlert", "MinRemainingTurn", "MaxRemainingTurn",
            "Repeatable", "ImageKey", "Enabled"
        };
        if (!sheet.RequireColumns(required))
        {
            ErrorMissingColumns(sheet, required);
            return;
        }

        int parsed = 0;
        int enabled = 0;
        int skipped = 0;
        for (int i = 0; i < sheet.Rows.Count; i++)
        {
            string[] record = sheet.Rows[i];
            int line = i + 2;
            string id;
            if (!TryReadUniqueId(sheet, record, line, "EventID", allEventIds, out id))
                continue;

            int weight, minAlert, maxAlert, minTurn, maxTurn;
            bool repeatable, enabledFlag;
            if (!TryParseInt(sheet, record, line, "Weight", out weight)
                || !TryParseInt(sheet, record, line, "MinAlert", out minAlert)
                || !TryParseInt(sheet, record, line, "MaxAlert", out maxAlert)
                || !TryParseInt(sheet, record, line, "MinRemainingTurn", out minTurn)
                || !TryParseInt(sheet, record, line, "MaxRemainingTurn", out maxTurn)
                || !TryParseBool(sheet, record, line, "Repeatable", out repeatable)
                || !TryParseBool(sheet, record, line, "Enabled", out enabledFlag))
                continue;

            var row = new EventRow
            {
                EventID = id,
                EventName = sheet.Get(record, "EventName"),
                EventDescription = sheet.Get(record, "EventDescription"),
                PhaseFilter = sheet.Get(record, "PhaseFilter"),
                EventTags = sheet.Get(record, "EventTags"),
                Weight = weight,
                MinAlert = minAlert,
                MaxAlert = maxAlert,
                MinRemainingTurn = minTurn,
                MaxRemainingTurn = maxTurn,
                Repeatable = repeatable,
                ImageKey = sheet.Get(record, "ImageKey"),
                Enabled = enabledFlag,
                Notes = sheet.Get(record, "Notes")
            };
            parsed++;
            if (!row.Enabled)
            {
                skipped++;
                Info("{0} {1}행: Enabled=0 이므로 건너뜀 ({2})", sheet.TableName, line, id);
                continue;
            }

            events.Add(id, row);
            eventsOrdered.Add(row);
            enabled++;
        }

        Info("{0} 로딩: 파싱 {1}행, 활성 {2}행, 비활성 건너뜀 {3}행", sheet.TableName, parsed, enabled, skipped);
    }

    void LoadEventOptions(CsvSheet sheet)
    {
        string[] required =
        {
            "OptionID", "EventID", "OptionOrder", "OptionName", "Description",
            "SuccessRate", "TurnCost", "SuccessCandy", "FailCandy", "SuccessAlert",
            "FailAlert", "SuccessTurnDelta", "FailTurnDelta", "SuccessEffectID",
            "FailEffectID", "Enabled"
        };
        if (!sheet.RequireColumns(required))
        {
            ErrorMissingColumns(sheet, required);
            return;
        }

        int parsed = 0;
        int enabled = 0;
        int skipped = 0;
        for (int i = 0; i < sheet.Rows.Count; i++)
        {
            string[] record = sheet.Rows[i];
            int line = i + 2;
            string id;
            if (!TryReadUniqueId(sheet, record, line, "OptionID", allOptionIds, out id))
                continue;

            int order, successRate, turnCost, successCandy, failCandy, successAlert, failAlert, successTurn, failTurn;
            bool enabledFlag;
            if (!TryParseInt(sheet, record, line, "OptionOrder", out order)
                || !TryParseInt(sheet, record, line, "SuccessRate", out successRate)
                || !TryParseInt(sheet, record, line, "TurnCost", out turnCost)
                || !TryParseInt(sheet, record, line, "SuccessCandy", out successCandy)
                || !TryParseInt(sheet, record, line, "FailCandy", out failCandy)
                || !TryParseInt(sheet, record, line, "SuccessAlert", out successAlert)
                || !TryParseInt(sheet, record, line, "FailAlert", out failAlert)
                || !TryParseInt(sheet, record, line, "SuccessTurnDelta", out successTurn)
                || !TryParseInt(sheet, record, line, "FailTurnDelta", out failTurn)
                || !TryParseBool(sheet, record, line, "Enabled", out enabledFlag))
                continue;

            var row = new EventOptionRow
            {
                OptionID = id,
                EventID = sheet.Get(record, "EventID"),
                OptionOrder = order,
                OptionName = sheet.Get(record, "OptionName"),
                Description = sheet.Get(record, "Description"),
                SuccessRate = successRate,
                TurnCost = turnCost,
                SuccessCandy = successCandy,
                FailCandy = failCandy,
                SuccessAlert = successAlert,
                FailAlert = failAlert,
                SuccessTurnDelta = successTurn,
                FailTurnDelta = failTurn,
                SuccessEffectID = sheet.Get(record, "SuccessEffectID"),
                FailEffectID = sheet.Get(record, "FailEffectID"),
                Enabled = enabledFlag,
                Notes = sheet.Get(record, "Notes")
            };
            parsed++;
            if (!row.Enabled)
            {
                skipped++;
                Info("{0} {1}행: Enabled=0 이므로 건너뜀 ({2})", sheet.TableName, line, id);
                continue;
            }

            eventOptions.Add(id, row);
            eventOptionsOrdered.Add(row);
            enabled++;
        }

        Info("{0} 로딩: 파싱 {1}행, 활성 {2}행, 비활성 건너뜀 {3}행", sheet.TableName, parsed, enabled, skipped);
    }

    void LoadAchievements(CsvSheet sheet)
    {
        string[] required =
        {
            "AchievementID", "TitleName", "Description", "ConditionType",
            "TargetValue", "ConditionParams", "Rarity", "IconImageKey", "Hidden", "Enabled"
        };
        if (!sheet.RequireColumns(required))
        {
            ErrorMissingColumns(sheet, required);
            return;
        }

        int parsed = 0;
        int enabled = 0;
        int skipped = 0;
        for (int i = 0; i < sheet.Rows.Count; i++)
        {
            string[] record = sheet.Rows[i];
            int line = i + 2;
            string id;
            if (!TryReadUniqueId(sheet, record, line, "AchievementID", allAchievementIds, out id))
                continue;

            int targetValue;
            bool hidden, enabledFlag;
            if (!TryParseInt(sheet, record, line, "TargetValue", out targetValue)
                || !TryParseBool(sheet, record, line, "Hidden", out hidden)
                || !TryParseBool(sheet, record, line, "Enabled", out enabledFlag))
                continue;

            var row = new AchievementRow
            {
                AchievementID = id,
                TitleName = sheet.Get(record, "TitleName"),
                Description = sheet.Get(record, "Description"),
                ConditionType = sheet.Get(record, "ConditionType"),
                TargetValue = targetValue,
                ConditionParams = sheet.Get(record, "ConditionParams"),
                Rarity = sheet.Get(record, "Rarity"),
                IconImageKey = sheet.Get(record, "IconImageKey"),
                Hidden = hidden,
                Enabled = enabledFlag,
                Notes = sheet.Get(record, "Notes")
            };
            parsed++;
            if (!row.Enabled)
            {
                skipped++;
                Info("{0} {1}행: Enabled=0 이므로 건너뜀 ({2})", sheet.TableName, line, id);
                continue;
            }

            achievements.Add(id, row);
            achievementsOrdered.Add(row);
            enabled++;
        }

        Info("{0} 로딩: 파싱 {1}행, 활성 {2}행, 비활성 건너뜀 {3}행", sheet.TableName, parsed, enabled, skipped);
    }

    void LoadAssets(CsvSheet sheet)
    {
        string[] required =
        {
            "AssetID", "ImageKey", "AssetType", "FilePath", "PromptKey",
            "ImagePromptKR", "GenerationStatus", "ReviewStatus", "Enabled"
        };
        if (!sheet.RequireColumns(required))
        {
            ErrorMissingColumns(sheet, required);
            return;
        }

        int parsed = 0;
        int enabled = 0;
        int skipped = 0;
        for (int i = 0; i < sheet.Rows.Count; i++)
        {
            string[] record = sheet.Rows[i];
            int line = i + 2;
            string id;
            if (!TryReadUniqueId(sheet, record, line, "AssetID", allAssetIds, out id))
                continue;

            string imageKey = sheet.Get(record, "ImageKey");
            if (string.IsNullOrWhiteSpace(imageKey))
            {
                Error("{0} {1}행: 필수 컬럼 ImageKey가 비어 있습니다.", sheet.TableName, line);
                continue;
            }

            if (!allImageKeys.Add(imageKey))
            {
                Error("{0} {1}행: ImageKey 중복 '{2}'", sheet.TableName, line, imageKey);
                continue;
            }

            bool enabledFlag;
            if (!TryParseBool(sheet, record, line, "Enabled", out enabledFlag))
                continue;

            var row = new AssetRow
            {
                AssetID = id,
                ImageKey = imageKey,
                AssetType = sheet.Get(record, "AssetType"),
                FilePath = sheet.Get(record, "FilePath"),
                PromptKey = sheet.Get(record, "PromptKey"),
                ImagePromptKR = sheet.Get(record, "ImagePromptKR"),
                GenerationStatus = sheet.Get(record, "GenerationStatus"),
                ReviewStatus = sheet.Get(record, "ReviewStatus"),
                Enabled = enabledFlag,
                Notes = sheet.Get(record, "Notes")
            };
            parsed++;
            if (!row.Enabled)
            {
                skipped++;
                Info("{0} {1}행: Enabled=0 이므로 건너뜀 ({2})", sheet.TableName, line, id);
                continue;
            }

            assetsById.Add(id, row);
            assetsByImageKey.Add(imageKey, row);
            assetsOrdered.Add(row);
            enabled++;
        }

        Info("{0} 로딩: 파싱 {1}행, 활성 {2}행, 비활성 건너뜀 {3}행", sheet.TableName, parsed, enabled, skipped);
    }

    void LoadEffects(CsvSheet sheet)
    {
        string[] required =
        {
            "EffectID", "EffectType", "Value", "DurationActions", "Target",
            "Description", "Implemented", "Enabled"
        };
        if (!sheet.RequireColumns(required))
        {
            ErrorMissingColumns(sheet, required);
            return;
        }

        int parsed = 0;
        int enabled = 0;
        int skipped = 0;
        for (int i = 0; i < sheet.Rows.Count; i++)
        {
            string[] record = sheet.Rows[i];
            int line = i + 2;
            string id;
            if (!TryReadUniqueId(sheet, record, line, "EffectID", allEffectIds, out id))
                continue;

            int value, duration;
            bool implemented, enabledFlag;
            if (!TryParseInt(sheet, record, line, "Value", out value)
                || !TryParseInt(sheet, record, line, "DurationActions", out duration)
                || !TryParseBool(sheet, record, line, "Implemented", out implemented)
                || !TryParseBool(sheet, record, line, "Enabled", out enabledFlag))
                continue;

            var row = new EffectRow
            {
                EffectID = id,
                EffectType = sheet.Get(record, "EffectType"),
                Value = value,
                DurationActions = duration,
                Target = sheet.Get(record, "Target"),
                Description = sheet.Get(record, "Description"),
                Implemented = implemented,
                Enabled = enabledFlag,
                Notes = sheet.Get(record, "Notes")
            };
            parsed++;
            if (!row.Enabled || !row.Implemented)
            {
                skipped++;
                Info("{0} {1}행: Implemented=0/Enabled=0 이므로 런타임 적용에서 제외 ({2})", sheet.TableName, line, id);
                continue;
            }

            effects.Add(id, row);
            effectsOrdered.Add(row);
            enabled++;
        }

        Info("{0} 로딩: 파싱 {1}행, 활성 {2}행, 비활성 건너뜀 {3}행", sheet.TableName, parsed, enabled, skipped);
    }

    void TryLoadPresentationTableFromResources()
    {
        TextAsset asset = Resources.Load<TextAsset>(ResourceFolder + "/ResultFeedbackTable");
        if (asset == null)
        {
            Info("ResultFeedbackTable이 없어 기본 결과 문구를 사용합니다.");
            return;
        }

        LoadPresentationTableText(CsvReader.DecodeUtf8(asset.bytes));
    }

    void LoadPresentationTableText(string csvText)
    {
        if (string.IsNullOrWhiteSpace(csvText))
        {
            Warn("ResultFeedbackTable 파일이 비어 있습니다. 기본 결과 문구를 사용합니다.");
            return;
        }

        CsvSheet sheet;
        if (!TryReadSheet("ResultFeedbackTable", csvText, out sheet, true))
            return;

        LoadResultFeedback(sheet);
    }

    void NoteFeedbackImageKey(string feedbackId, string branch, string imageKey)
    {
        if (string.IsNullOrEmpty(imageKey))
            return;

        string owner = feedbackId + ":" + branch;
        string previous;
        if (feedbackImageKeyOwner.TryGetValue(imageKey, out previous))
        {
            Error("ResultFeedbackTable: 이미지 키 '{0}' 가 {1} 와 {2} 에 동시에 있습니다. 결과 분기는 키를 공유하지 않습니다.", imageKey, previous, owner);
            return;
        }

        feedbackImageKeyOwner.Add(imageKey, owner);
        if (!allImageKeys.Contains(imageKey))
            Warn("ResultFeedbackTable: 이미지 키 '{0}' 가 AssetTable에 없습니다. 임시 피드백을 사용합니다. ({1})", imageKey, owner);
    }

    void LoadResultFeedback(CsvSheet sheet)
    {
        string[] required =
        {
            "FeedbackID", "SourceType", "SourceID", "SuccessText", "FailText",
            "SuccessPresentationID", "FailPresentationID", "Enabled"
        };
        if (!sheet.RequireColumns(required))
        {
            Warn("ResultFeedbackTable 필수 컬럼이 없어 기본 결과 문구를 사용합니다.");
            return;
        }

        int parsed = 0;
        int enabled = 0;
        int skipped = 0;
        for (int i = 0; i < sheet.Rows.Count; i++)
        {
            string[] record = sheet.Rows[i];
            int line = i + 2;
            string id = sheet.Get(record, "FeedbackID");
            if (string.IsNullOrWhiteSpace(id))
            {
                Warn("ResultFeedbackTable {0}행: FeedbackID가 비어 건너뜁니다.", line);
                continue;
            }

            if (!allFeedbackIds.Add(id))
            {
                Warn("ResultFeedbackTable {0}행: FeedbackID 중복 '{1}' 를 건너뜁니다.", line, id);
                continue;
            }

            string enabledRaw = sheet.Get(record, "Enabled");
            bool enabledFlag;
            if (!TryConvertBool(enabledRaw, out enabledFlag))
            {
                Warn("ResultFeedbackTable {0}행: Enabled='{1}' 는 BOOL(0/1)이 아닙니다.", line, enabledRaw);
                continue;
            }

            string sourceType = sheet.Get(record, "SourceType");
            string sourceId = sheet.Get(record, "SourceID");
            if (sourceType != "ACTION" && sourceType != "OPTION")
            {
                Warn("ResultFeedbackTable {0}행: SourceType '{1}' 는 ACTION 또는 OPTION 이어야 합니다.", line, sourceType);
                continue;
            }

            if (string.IsNullOrWhiteSpace(sourceId))
            {
                Warn("ResultFeedbackTable {0}행: SourceID가 비어 건너뜁니다.", line);
                continue;
            }

            var row = new ResultFeedbackRow
            {
                FeedbackID = id,
                SourceType = sourceType,
                SourceID = sourceId,
                SuccessText = sheet.Get(record, "SuccessText"),
                FailText = sheet.Get(record, "FailText"),
                SuccessPresentationID = sheet.Get(record, "SuccessPresentationID"),
                FailPresentationID = sheet.Get(record, "FailPresentationID"),
                SuccessImageKey = sheet.Get(record, "SuccessImageKey"),
                FailImageKey = sheet.Get(record, "FailImageKey"),
                Enabled = enabledFlag,
                Notes = sheet.Get(record, "Notes")
            };
            parsed++;
            if (!row.Enabled)
            {
                skipped++;
                continue;
            }

            string key = FeedbackKey(sourceType, sourceId);
            if (resultFeedbackByKey.ContainsKey(key))
            {
                Warn("ResultFeedbackTable {0}행: {1} {2} 중복을 건너뜁니다.", line, sourceType, sourceId);
                continue;
            }

            if (sourceType == "ACTION" && !allActionIds.Contains(sourceId))
                Warn("ResultFeedbackTable {0}: SourceID '{1}' 가 ActionTable에 없습니다. 문구는 로드합니다.", id, sourceId);
            if (sourceType == "OPTION" && !allOptionIds.Contains(sourceId))
                Warn("ResultFeedbackTable {0}: SourceID '{1}' 가 EventOptionTable에 없습니다. 문구는 로드합니다.", id, sourceId);

            resultFeedbackByKey.Add(key, row);
            resultFeedbackOrdered.Add(row);
            NoteFeedbackImageKey(id, "success", row.SuccessImageKey);
            NoteFeedbackImageKey(id, "fail", row.FailImageKey);
            enabled++;
        }

        Info("{0} 로딩: 파싱 {1}행, 활성 {2}행, 비활성 건너뜀 {3}행", sheet.TableName, parsed, enabled, skipped);
    }

    void TryLoadRumorTablesFromResources()
    {
        TextAsset rumorAsset = Resources.Load<TextAsset>(ResourceFolder + "/ShopRumorTable");
        TextAsset candidateAsset = Resources.Load<TextAsset>(ResourceFolder + "/ShopRumorCandidate");
        LoadRumorTableTexts(
            rumorAsset != null ? CsvReader.DecodeUtf8(rumorAsset.bytes) : null,
            candidateAsset != null ? CsvReader.DecodeUtf8(candidateAsset.bytes) : null);
    }

    // null 텍스트 = 파일 없음(소문 없음으로 동작). 파일이 있으면 오류는 HasErrors로 반영한다.
    void LoadRumorTableTexts(string rumorCsv, string candidateCsv)
    {
        if (rumorCsv == null && candidateCsv == null)
        {
            Info("ShopRumorTable/ShopRumorCandidate가 없어 소문 없이 동작합니다.");
            return;
        }

        CsvSheet sheet;
        if (rumorCsv != null && TryReadSheet("ShopRumorTable", rumorCsv, out sheet))
            LoadRumors(sheet);
        if (candidateCsv != null && TryReadSheet("ShopRumorCandidate", candidateCsv, out sheet))
            LoadRumorCandidates(sheet);

        ValidateRumorReferences();
    }

    void LoadRumors(CsvSheet sheet)
    {
        string[] required =
        {
            "RumorID", "RumorName", "RumorText", "HintText", "ConflictGroup",
            "SuccessRateBonusPp", "AlertDelta", "CandyMultiplierDelta", "EventRateBonusPp", "Enabled"
        };
        if (!sheet.RequireColumns(required))
        {
            ErrorMissingColumns(sheet, required);
            return;
        }

        int parsed = 0;
        int enabledCount = 0;
        for (int i = 0; i < sheet.Rows.Count; i++)
        {
            string[] record = sheet.Rows[i];
            int line = i + 2;
            string id;
            if (!TryReadUniqueId(sheet, record, line, "RumorID", allRumorIds, out id))
                continue;

            int successBonus, alertDelta, eventBonus;
            float multiplierDelta;
            bool enabled;
            if (!TryParseInt(sheet, record, line, "SuccessRateBonusPp", out successBonus)
                || !TryParseInt(sheet, record, line, "AlertDelta", out alertDelta)
                || !TryParseFloat(sheet, record, line, "CandyMultiplierDelta", out multiplierDelta)
                || !TryParseInt(sheet, record, line, "EventRateBonusPp", out eventBonus)
                || !TryParseBool(sheet, record, line, "Enabled", out enabled))
                continue;

            if (float.IsNaN(multiplierDelta) || float.IsInfinity(multiplierDelta))
            {
                Error("{0} {1}행: CandyMultiplierDelta 는 유한한 수여야 합니다.", sheet.TableName, line);
                continue;
            }

            string rumorText = sheet.Get(record, "RumorText");
            string hintText = sheet.Get(record, "HintText");
            if (string.IsNullOrEmpty(rumorText) || string.IsNullOrEmpty(hintText))
            {
                Error("{0} {1}행: RumorText/HintText 는 비어 있을 수 없습니다. ({2})", sheet.TableName, line, id);
                continue;
            }

            // 효과가 하나도 없는 활성 소문은 문구와 실제 효과가 어긋나므로 허용하지 않는다.
            if (enabled && successBonus == 0 && alertDelta == 0 && eventBonus == 0 && Mathf.Approximately(multiplierDelta, 0f))
            {
                Error("{0} {1}행: 활성 소문 {2} 의 효과가 모두 0 입니다.", sheet.TableName, line, id);
                continue;
            }

            parsed++;
            if (!enabled)
            {
                Info("{0} {1}행: Enabled=0 이므로 건너뜀 ({2})", sheet.TableName, line, id);
                continue;
            }

            var row = new ShopRumorRow
            {
                RumorID = id,
                RumorName = sheet.Get(record, "RumorName"),
                RumorText = rumorText,
                HintText = hintText,
                ConflictGroup = sheet.Get(record, "ConflictGroup"),
                SuccessRateBonusPp = successBonus,
                AlertDelta = alertDelta,
                CandyMultiplierDelta = multiplierDelta,
                EventRateBonusPp = eventBonus,
                Enabled = true,
                Notes = sheet.Get(record, "Notes")
            };
            rumors.Add(id, row);
            rumorsOrdered.Add(row);
            enabledCount++;
        }

        Info("{0} 로딩: 파싱 {1}행, 활성 {2}행", sheet.TableName, parsed, enabledCount);
    }

    void LoadRumorCandidates(CsvSheet sheet)
    {
        string[] required = { "ShopID", "RumorID", "Weight" };
        if (!sheet.RequireColumns(required))
        {
            ErrorMissingColumns(sheet, required);
            return;
        }

        var seenPairs = new HashSet<string>(StringComparer.Ordinal);
        for (int i = 0; i < sheet.Rows.Count; i++)
        {
            string[] record = sheet.Rows[i];
            int line = i + 2;
            string shopId = sheet.Get(record, "ShopID");
            string rumorId = sheet.Get(record, "RumorID");
            if (string.IsNullOrEmpty(shopId) || string.IsNullOrEmpty(rumorId))
            {
                Error("{0} {1}행: ShopID/RumorID 는 비어 있을 수 없습니다.", sheet.TableName, line);
                continue;
            }

            if (!seenPairs.Add(RumorPairKey(shopId, rumorId)))
            {
                Error("{0} {1}행: (ShopID, RumorID) 중복 '{2}' '{3}'", sheet.TableName, line, shopId, rumorId);
                continue;
            }

            int weight;
            if (!TryParseNonNegativeInt(sheet, record, line, "Weight", out weight))
                continue;

            rumorCandidatesRaw.Add(new ShopRumorCandidateRow { ShopID = shopId, RumorID = rumorId, Weight = weight });
        }

        Info("{0} 로딩: {1}행", sheet.TableName, rumorCandidatesRaw.Count);
    }

    // FK 검증 + 가게별 후보 색인 + 최악 조합에서도 CandyMultiplier > 0 인지 확인한다.
    void ValidateRumorReferences()
    {
        for (int i = 0; i < rumorCandidatesRaw.Count; i++)
        {
            ShopRumorCandidateRow row = rumorCandidatesRaw[i];
            bool ok = true;
            if (!allShopIds.Contains(row.ShopID))
            {
                Error("ShopRumorCandidate: ShopID '{0}' 가 ShopTable에 없습니다.", row.ShopID);
                ok = false;
            }

            if (!allRumorIds.Contains(row.RumorID))
            {
                Error("ShopRumorCandidate: RumorID '{0}' 가 ShopRumorTable에 없습니다.", row.RumorID);
                ok = false;
            }

            if (!ok)
                continue;

            List<ShopRumorCandidateRow> list;
            if (!rumorCandidatesByShop.TryGetValue(row.ShopID, out list))
            {
                list = new List<ShopRumorCandidateRow>();
                rumorCandidatesByShop.Add(row.ShopID, list);
            }

            list.Add(row);
            rumorCandidatePairs.Add(RumorPairKey(row.ShopID, row.RumorID));
        }

        for (int i = 0; i < shopsOrdered.Count; i++)
        {
            ShopRow shop = shopsOrdered[i];
            List<ShopRumorCandidateRow> list;
            if (!rumorCandidatesByShop.TryGetValue(shop.ShopID, out list))
                continue;

            var deltas = new List<float>();
            for (int j = 0; j < list.Count; j++)
            {
                ShopRumorRow rumor;
                if (list[j].Weight > 0 && rumors.TryGetValue(list[j].RumorID, out rumor))
                    deltas.Add(rumor.CandyMultiplierDelta);
            }

            deltas.Sort();
            float worst = shop.CandyMultiplier;
            for (int j = 0; j < deltas.Count && j < MaxRumorsPerShopForValidation && deltas[j] < 0f; j++)
                worst += deltas[j];

            if (!(worst > 0f))
                Error("ShopRumorCandidate: 가게 {0} 에서 소문 최악 조합의 CandyMultiplier={1} 는 0 보다 커야 합니다.", shop.ShopID, worst);
        }
    }

    void ValidateReferences()
    {
        for (int i = 0; i < shopsOrdered.Count; i++)
        {
            ShopRow shop = shopsOrdered[i];
            if (string.IsNullOrEmpty(shop.BackgroundImageKey) || !allImageKeys.Contains(shop.BackgroundImageKey))
                Error("ShopTable {0}: BackgroundImageKey '{1}' 가 AssetTable.ImageKey에 없습니다.", shop.ShopID, shop.BackgroundImageKey);
        }

        for (int i = 0; i < actionsOrdered.Count; i++)
        {
            ActionRow action = actionsOrdered[i];
            if (!allPhaseIds.Contains(action.PhaseID))
                Error("ActionTable {0}: PhaseID '{1}' 가 PhaseTable에 없습니다.", action.ActionID, action.PhaseID);
            if (!IsNoneOrEmpty(action.SuccessNextPhase) && !allPhaseIds.Contains(action.SuccessNextPhase))
                Error("ActionTable {0}: SuccessNextPhase '{1}' 가 PhaseTable에 없습니다.", action.ActionID, action.SuccessNextPhase);
        }

        for (int i = 0; i < eventsOrdered.Count; i++)
        {
            EventRow evt = eventsOrdered[i];
            if (!string.IsNullOrEmpty(evt.ImageKey) && !allImageKeys.Contains(evt.ImageKey))
                Error("EventTable {0}: ImageKey '{1}' 가 AssetTable.ImageKey에 없습니다.", evt.EventID, evt.ImageKey);
        }

        for (int i = 0; i < eventOptionsOrdered.Count; i++)
        {
            EventOptionRow option = eventOptionsOrdered[i];
            if (!allEventIds.Contains(option.EventID))
                Error("EventOptionTable {0}: EventID '{1}' 가 EventTable에 없습니다.", option.OptionID, option.EventID);
            ValidateEffectRef(option.OptionID, "SuccessEffectID", option.SuccessEffectID);
            ValidateEffectRef(option.OptionID, "FailEffectID", option.FailEffectID);
        }

        for (int i = 0; i < phasesOrdered.Count; i++)
        {
            PhaseRow phase = phasesOrdered[i];
            if (!IsNoneOrEmpty(phase.NextPhaseID) && !allPhaseIds.Contains(phase.NextPhaseID))
                Error("PhaseTable {0}: NextPhaseID '{1}' 가 PhaseTable에 없습니다.", phase.PhaseID, phase.NextPhaseID);
            if (string.IsNullOrWhiteSpace(phase.SituationText))
                Error("PhaseTable {0}: SituationText가 비어 있습니다. Notes는 플레이어 문장이 아닙니다.", phase.PhaseID);
        }
    }

    void ValidateEffectRef(string optionId, string column, string effectId)
    {
        if (IsNoneOrEmpty(effectId))
            return;

        if (!allEffectIds.Contains(effectId))
            Error("EventOptionTable {0}: {1} '{2}' 가 EffectTable에 없습니다.", optionId, column, effectId);
    }

    void ValidateRequiredConfigKeys()
    {
        for (int i = 0; i < RequiredGameConfigKeys.Length; i++)
        {
            string key = RequiredGameConfigKeys[i];
            if (!configs.ContainsKey(key))
                Error("GameConfig에 필수 키 '{0}' 가 없습니다.", key);
        }
    }

    void ValidateActionSelectTime()
    {
        GameConfigRow row;
        if (!TryGetConfig("ACTION_SELECT_TIME_SEC", out row))
            return;

        int seconds = GetInt("ACTION_SELECT_TIME_SEC");
        if (row.ValueType != GameConfigValueType.INT || seconds < 1)
            Error("GameConfig ACTION_SELECT_TIME_SEC는 1 이상의 INT여야 합니다.");
    }

    bool TryReadUniqueId(CsvSheet sheet, string[] record, int line, string column, HashSet<string> seen, out string id)
    {
        id = sheet.Get(record, column);
        if (string.IsNullOrWhiteSpace(id))
        {
            Error("{0} {1}행: 필수 컬럼 {2}가 비어 있습니다.", sheet.TableName, line, column);
            return false;
        }

        if (!seen.Add(id))
        {
            Error("{0} {1}행: {2} 중복 '{3}'", sheet.TableName, line, column, id);
            return false;
        }

        return true;
    }

    bool TryParseInt(CsvSheet sheet, string[] record, int line, string column, out int value)
    {
        string raw = sheet.Get(record, column);
        if (int.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out value))
            return true;

        Error("{0} {1}행: {2}='{3}' 는 INT가 아닙니다.", sheet.TableName, line, column, raw);
        value = 0;
        return false;
    }

    bool TryParseNonNegativeInt(CsvSheet sheet, string[] record, int line, string column, out int value)
    {
        if (!TryParseInt(sheet, record, line, column, out value))
            return false;

        if (value >= 0)
            return true;

        Error("{0} {1}행: {2}={3} 는 0 이상이어야 합니다.", sheet.TableName, line, column, value);
        value = 0;
        return false;
    }

    // Blank cell = grade not assigned yet (NONE). Any other text must be an exact enum token.
    bool TryParseGrade<T>(CsvSheet sheet, string[] record, int line, string column, out T value) where T : struct
    {
        if (string.IsNullOrEmpty(sheet.Get(record, column)))
        {
            value = default;
            return true;
        }

        return TryParseEnum(sheet, record, line, column, out value);
    }

    bool TryParseFloat(CsvSheet sheet, string[] record, int line, string column, out float value)
    {
        string raw = sheet.Get(record, column);
        if (float.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out value))
            return true;

        Error("{0} {1}행: {2}='{3}' 는 FLOAT가 아닙니다.", sheet.TableName, line, column, raw);
        value = 0f;
        return false;
    }

    bool TryParseBool(CsvSheet sheet, string[] record, int line, string column, out bool value)
    {
        string raw = sheet.Get(record, column);
        if (TryConvertBool(raw, out value))
            return true;

        Error("{0} {1}행: {2}='{3}' 는 BOOL(0/1)이 아닙니다.", sheet.TableName, line, column, raw);
        value = false;
        return false;
    }

    bool TryParseEnum<T>(CsvSheet sheet, string[] record, int line, string column, out T value) where T : struct
    {
        string raw = sheet.Get(record, column);
        if (Enum.TryParse(raw, false, out value)
            && Enum.IsDefined(typeof(T), value)
            && string.Equals(raw, value.ToString(), StringComparison.Ordinal))
            return true;

        Error("{0} {1}행: {2}='{3}' 는 허용된 값이 아닙니다.", sheet.TableName, line, column, raw);
        value = default;
        return false;
    }

    bool ValidateConfigValue(string tableName, int line, string key, string value, GameConfigValueType valueType)
    {
        switch (valueType)
        {
            case GameConfigValueType.INT:
                int intValue;
                if (int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out intValue))
                    return true;
                Error("{0} {1}행: Key '{2}' 값 '{3}' 는 INT가 아닙니다.", tableName, line, key, value);
                return false;
            case GameConfigValueType.FLOAT:
                float floatValue;
                if (float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out floatValue))
                    return true;
                Error("{0} {1}행: Key '{2}' 값 '{3}' 는 FLOAT가 아닙니다.", tableName, line, key, value);
                return false;
            case GameConfigValueType.BOOL:
                bool boolValue;
                if (TryConvertBool(value, out boolValue))
                    return true;
                Error("{0} {1}행: Key '{2}' 값 '{3}' 는 BOOL이 아닙니다.", tableName, line, key, value);
                return false;
            default:
                Error("{0} {1}행: Key '{2}' 의 ValueType이 지원되지 않습니다.", tableName, line, key);
                return false;
        }
    }

    bool TryGetTypedValue(string key, GameConfigValueType expected, out GameConfigRow row)
    {
        if (!configs.TryGetValue(key, out row))
        {
            Error("GameConfig 키 '{0}' 를 찾을 수 없습니다.", key);
            return false;
        }

        if (row.ValueType != expected)
        {
            Error("GameConfig 키 '{0}' 의 ValueType은 {1} 이어야 합니다. 실제={2}", key, expected, row.ValueType);
            return false;
        }

        return true;
    }

    static int ParseIntValue(string value)
    {
        int parsed;
        return int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out parsed) ? parsed : 0;
    }

    static float ParseFloatValue(string value)
    {
        float parsed;
        return float.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out parsed) ? parsed : 0f;
    }

    static bool ParseBoolValue(string value)
    {
        bool parsed;
        return TryConvertBool(value, out parsed) && parsed;
    }

    static bool TryConvertBool(string raw, out bool value)
    {
        if (raw == "1" || string.Equals(raw, "true", StringComparison.OrdinalIgnoreCase))
        {
            value = true;
            return true;
        }

        if (raw == "0" || string.Equals(raw, "false", StringComparison.OrdinalIgnoreCase))
        {
            value = false;
            return true;
        }

        value = false;
        return false;
    }

    static bool IsNoneOrEmpty(string value)
    {
        return string.IsNullOrEmpty(value) || string.Equals(value, "NONE", StringComparison.Ordinal);
    }

    void ErrorMissingColumns(CsvSheet sheet, params string[] required)
    {
        var missing = new List<string>();
        for (int i = 0; i < required.Length; i++)
        {
            if (!sheet.HasColumn(required[i]))
                missing.Add(required[i]);
        }

        Error("{0} 필수 컬럼 누락: {1}", sheet.TableName, string.Join(", ", missing.ToArray()));
    }

    void Info(string format, params object[] args)
    {
        Debug.Log(LogPrefix + string.Format(CultureInfo.InvariantCulture, format, args));
    }

    void Warn(string format, params object[] args)
    {
        Debug.LogWarning(LogPrefix + string.Format(CultureInfo.InvariantCulture, format, args));
    }

    void Error(string format, params object[] args)
    {
        HasErrors = true;
        ErrorCount++;
        Debug.LogError(LogPrefix + string.Format(CultureInfo.InvariantCulture, format, args));
    }

    sealed class CsvSheet
    {
        public readonly string TableName;
        public readonly string[] Header;
        public readonly List<string[]> Rows = new List<string[]>();
        readonly Dictionary<string, int> columnIndex = new Dictionary<string, int>(StringComparer.Ordinal);

        public CsvSheet(string tableName, string[] header, List<string[]> records)
        {
            TableName = tableName;
            Header = header;
            for (int i = 0; i < header.Length; i++)
            {
                string name = header[i] != null ? header[i].Trim() : string.Empty;
                if (name.Length == 0 || columnIndex.ContainsKey(name))
                    continue;

                columnIndex.Add(name, i);
            }

            for (int i = 1; i < records.Count; i++)
                Rows.Add(records[i]);
        }

        public bool HasColumn(string column)
        {
            return columnIndex.ContainsKey(column);
        }

        public bool RequireColumns(params string[] columns)
        {
            for (int i = 0; i < columns.Length; i++)
            {
                if (!HasColumn(columns[i]))
                    return false;
            }

            return true;
        }

        public string Get(string[] record, string column)
        {
            int index;
            if (!columnIndex.TryGetValue(column, out index) || index >= record.Length)
                return string.Empty;

            return record[index] != null ? record[index].Trim() : string.Empty;
        }
    }
}
