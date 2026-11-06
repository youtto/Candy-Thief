using System.Collections.Generic;
using UnityEngine;

public static class ActionResolverSelfTest
{
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
            Debug.LogError(ActionResolver.LogPrefix + "자가 검사 실패: CsvDatabase가 준비되지 않았습니다.");
            return false;
        }

        RunState previous = RunState.Current;
        int failed = 0;
        failed += Expect("1 정상 행동 실행", TestNormalAction(database));
        failed += Expect("2 턴 부족 거부", TestInsufficientTurns(database));
        failed += Expect("3 성공/실패 결과", TestSuccessAndFail(database));
        failed += Expect("4 Shop 성공률 보정", TestShopBonus(database));
        failed += Expect("5 성공률 min/max 클램프", TestChanceClamp(database));
        failed += Expect("6 사탕/경계도 증감", TestCandyAndAlert(database));
        failed += Expect("7 경계도 실패", TestAlertFail(database));
        failed += Expect("8 탈출 성공 승리", TestEscapeWin(database));
        failed += Expect("9 턴 소진 실패", TestNoTurnsFail(database));
        failed += Expect("10 D-011 경계도가 탈출 승리보다 우선", TestAlertBeatsEscape(database));
        failed += Expect("11 CSV 수치 변경 반영", TestCsvDrivesResult(database));

        RunState.Current = previous;
        if (failed == 0)
        {
            Debug.Log(ActionResolver.LogPrefix + "M1-4 자가 검사 11건 통과");
            return true;
        }

        Debug.LogError(ActionResolver.LogPrefix + "M1-4 자가 검사 실패: " + failed + "건");
        return false;
    }

    static int Expect(string name, bool passed)
    {
        if (passed)
        {
            Debug.Log(ActionResolver.LogPrefix + "PASS " + name);
            return 0;
        }

        Debug.LogError(ActionResolver.LogPrefix + "FAIL " + name);
        return 1;
    }

    static bool TestNormalAction(CsvDatabase database)
    {
        RunState run;
        ActionRow action;
        ShopRow shop;
        if (!database.TryGetAction("ACT_001", out action) || !database.TryGetShop("SHOP_001", out shop))
            return false;
        if (!RunState.TryStart(database, "SHOP_001", out run))
            return false;

        var resolver = new ActionResolver(database, new FixedChanceRoll(true));
        ActionResolveResult result;
        if (!resolver.TryResolve(run, "ACT_001", out result) || !result.Applied || !result.Success)
            return false;

        return run.Status == RunStatus.InProgress
            && run.CurrentPhase == action.SuccessNextPhase
            && run.RemainingTurns == shop.StartTurn - action.TurnCost
            && run.LastActionID == "ACT_001"
            && run.LastActionResult == RunActionResult.Success
            && result.EventCheckDue
            && result.SuccessChance == resolver.ComputeSuccessChance(action, shop);
    }

    static bool TestInsufficientTurns(CsvDatabase database)
    {
        RunState run;
        ActionRow action;
        if (!database.TryGetAction("ACT_001", out action))
            return false;
        if (!RunState.TryStart(database, "SHOP_001", out run))
            return false;

        int keep = action.TurnCost - 1;
        if (keep < 0 || !run.ApplyTurnDelta(-(run.RemainingTurns - keep)))
            return false;

        int turns = run.RemainingTurns;
        int candy = run.Candy;
        int alert = run.Alert;
        string phase = run.CurrentPhase;
        var resolver = new ActionResolver(database, new FixedChanceRoll(true));
        ActionResolveResult result;
        bool applied = resolver.TryResolve(run, "ACT_001", out result);
        return !applied
            && !result.Applied
            && result.RejectReason == ActionRejectReason.InsufficientTurns
            && run.RemainingTurns == turns
            && run.Candy == candy
            && run.Alert == alert
            && run.CurrentPhase == phase
            && run.Status == RunStatus.InProgress
            && run.LastActionResult == RunActionResult.None;
    }

    static bool TestSuccessAndFail(CsvDatabase database)
    {
        RunState successRun;
        RunState failRun;
        ActionRow loot;
        ShopRow shop;
        if (!database.TryGetAction("ACT_008", out loot) || !database.TryGetShop("SHOP_001", out shop))
            return false;
        if (!RunState.TryStart(database, "SHOP_001", out successRun) || !successRun.SetPhase("LOOT"))
            return false;

        var winResolver = new ActionResolver(database, new FixedChanceRoll(true));
        int chance = winResolver.ComputeSuccessChance(loot, shop);
        if (chance <= 0 || chance >= 100)
            return false;

        ActionResolveResult win;
        if (!winResolver.TryResolve(successRun, "ACT_008", out win) || !win.Success)
            return false;
        if (successRun.Candy != loot.SuccessCandy || successRun.Alert != loot.SuccessAlert)
            return false;
        if (successRun.CurrentPhase != loot.SuccessNextPhase)
            return false;

        if (!RunState.TryStart(database, "SHOP_001", out failRun) || !failRun.SetPhase("LOOT"))
            return false;
        var failResolver = new ActionResolver(database, new FixedChanceRoll(false));
        ActionResolveResult fail;
        if (!failResolver.TryResolve(failRun, "ACT_008", out fail) || fail.Success)
            return false;
        return failRun.Candy == loot.FailCandy
            && failRun.Alert == shop.StartAlert + loot.FailAlert
            && failRun.CurrentPhase == "LOOT"
            && failRun.Status == RunStatus.InProgress
            && failRun.LastActionResult == RunActionResult.Fail;
    }

    static bool TestShopBonus(CsvDatabase database)
    {
        ActionRow action;
        ShopRow shop1;
        ShopRow shop3;
        if (!database.TryGetAction("ACT_003", out action)
            || !database.TryGetShop("SHOP_001", out shop1)
            || !database.TryGetShop("SHOP_003", out shop3))
            return false;

        var resolver = new ActionResolver(database, new FixedChanceRoll(true));
        int chance1 = resolver.ComputeSuccessChance(action, shop1);
        int chance3 = resolver.ComputeSuccessChance(action, shop3);
        return chance1 == action.SuccessRate + shop1.SuccessRateBonusPp
            && chance3 == action.SuccessRate + shop3.SuccessRateBonusPp
            && chance1 != chance3;
    }

    static bool TestChanceClamp(CsvDatabase database)
    {
        CsvDatabase high = PatchAction(database, "ACT_003", "SuccessRate", "1000");
        CsvDatabase low = PatchAction(database, "ACT_003", "SuccessRate", "-1000");
        if (high == null || high.HasErrors || low == null || low.HasErrors)
            return false;

        ActionRow highAction;
        ActionRow lowAction;
        ShopRow shop;
        if (!high.TryGetAction("ACT_003", out highAction)
            || !low.TryGetAction("ACT_003", out lowAction)
            || !database.TryGetShop("SHOP_001", out shop))
            return false;

        int max = database.GetInt("MAX_SUCCESS_RATE");
        int min = database.GetInt("MIN_SUCCESS_RATE");
        var highResolver = new ActionResolver(high, new FixedChanceRoll(false));
        var lowResolver = new ActionResolver(low, new FixedChanceRoll(true));
        return highResolver.ComputeSuccessChance(highAction, shop) == max
            && lowResolver.ComputeSuccessChance(lowAction, shop) == min;
    }

    static bool TestCandyAndAlert(CsvDatabase database)
    {
        RunState lootRun;
        RunState moveRun;
        ActionRow loot;
        ActionRow risky;
        if (!database.TryGetAction("ACT_007", out loot) || !database.TryGetAction("ACT_003", out risky))
            return false;
        if (!RunState.TryStart(database, "SHOP_001", out lootRun) || !lootRun.SetPhase("LOOT"))
            return false;
        var lootResolver = new ActionResolver(database, new FixedChanceRoll(true));
        ActionResolveResult lootResult;
        if (!lootResolver.TryResolve(lootRun, "ACT_007", out lootResult))
            return false;
        if (lootRun.Candy != loot.SuccessCandy || lootRun.Alert != loot.SuccessAlert)
            return false;

        if (!RunState.TryStart(database, "SHOP_001", out moveRun))
            return false;
        var failResolver = new ActionResolver(database, new FixedChanceRoll(false));
        ActionResolveResult fail;
        if (!failResolver.TryResolve(moveRun, "ACT_003", out fail) || fail.Success)
            return false;
        return moveRun.Candy == risky.FailCandy && moveRun.Alert == risky.FailAlert;
    }

    static bool TestAlertFail(CsvDatabase database)
    {
        RunState run;
        ActionRow action;
        if (!database.TryGetAction("ACT_003", out action))
            return false;
        if (!RunState.TryStart(database, "SHOP_001", out run))
            return false;

        int maxAlert = database.GetInt("MAX_ALERT");
        int raise = maxAlert - action.FailAlert - run.Alert;
        if (raise < 0 || !run.ApplyAlertDelta(raise))
            return false;

        var resolver = new ActionResolver(database, new FixedChanceRoll(false));
        ActionResolveResult result;
        if (!resolver.TryResolve(run, "ACT_003", out result) || result.Success)
            return false;
        return run.Status == RunStatus.Failed
            && run.FailReason == RunFailReason.AlertLimit
            && run.Alert == maxAlert
            && !result.EventCheckDue;
    }

    static bool TestEscapeWin(CsvDatabase database)
    {
        RunState run;
        ActionRow escape;
        if (!database.TryGetAction("ACT_010", out escape))
            return false;
        if (!RunState.TryStart(database, "SHOP_001", out run))
            return false;
        if (!run.ApplyCandyDelta(database.GetInt("MIN_CANDY_TO_ESCAPE")))
            return false;
        if (!run.SetPhase("ESCAPE"))
            return false;

        var resolver = new ActionResolver(database, new FixedChanceRoll(true));
        ActionResolveResult result;
        if (!resolver.TryResolve(run, "ACT_010", out result) || !result.Success)
            return false;
        return run.Status == RunStatus.Won
            && run.CurrentPhase == escape.SuccessNextPhase
            && !result.EventCheckDue;
    }

    static bool TestNoTurnsFail(CsvDatabase database)
    {
        RunState run;
        ActionRow action;
        if (!database.TryGetAction("ACT_003", out action))
            return false;
        if (!RunState.TryStart(database, "SHOP_001", out run))
            return false;
        if (!run.ApplyTurnDelta(-(run.RemainingTurns - action.TurnCost)))
            return false;

        var resolver = new ActionResolver(database, new FixedChanceRoll(false));
        ActionResolveResult result;
        if (!resolver.TryResolve(run, "ACT_003", out result) || result.Success)
            return false;
        return run.RemainingTurns == 0
            && run.Status == RunStatus.Failed
            && run.FailReason == RunFailReason.NoTurns
            && !result.EventCheckDue;
    }

    static bool TestAlertBeatsEscape(CsvDatabase database)
    {
        CsvDatabase patched = PatchAction(database, "ACT_010", "SuccessAlert", "100");
        if (patched == null || patched.HasErrors)
            return false;

        RunState run;
        ActionRow escape;
        if (!patched.TryGetAction("ACT_010", out escape))
            return false;
        if (!RunState.TryStart(patched, "SHOP_001", out run))
            return false;
        if (!run.ApplyCandyDelta(patched.GetInt("MIN_CANDY_TO_ESCAPE")) || !run.SetPhase("ESCAPE"))
            return false;

        var resolver = new ActionResolver(patched, new FixedChanceRoll(true));
        ActionResolveResult result;
        if (!resolver.TryResolve(run, "ACT_010", out result) || !result.Success)
            return false;
        return run.Status == RunStatus.Failed
            && run.FailReason == RunFailReason.AlertLimit
            && run.Alert == patched.GetInt("MAX_ALERT")
            && !result.EventCheckDue;
    }

    static bool TestCsvDrivesResult(CsvDatabase database)
    {
        CsvDatabase zero = PatchAction(database, "ACT_001", "SuccessRate", "0");
        if (zero == null || zero.HasErrors)
            return false;

        ActionRow original;
        ActionRow patched;
        ShopRow shop;
        if (!database.TryGetAction("ACT_001", out original)
            || !zero.TryGetAction("ACT_001", out patched)
            || !database.TryGetShop("SHOP_002", out shop))
            return false;

        var liveResolver = new ActionResolver(database, new FixedChanceRoll(true));
        var patchedResolver = new ActionResolver(zero, new FixedChanceRoll(true));
        int liveChance = liveResolver.ComputeSuccessChance(original, shop);
        int patchedChance = patchedResolver.ComputeSuccessChance(patched, shop);
        if (liveChance == patchedChance || patched.SuccessRate != 0)
            return false;

        RunState liveRun;
        RunState patchedRun;
        if (!RunState.TryStart(database, "SHOP_002", out liveRun))
            return false;
        if (!RunState.TryStart(zero, "SHOP_002", out patchedRun))
            return false;

        ActionResolveResult liveResult;
        ActionResolveResult patchedResult;
        if (!liveResolver.TryResolve(liveRun, "ACT_001", out liveResult))
            return false;
        if (!patchedResolver.TryResolve(patchedRun, "ACT_001", out patchedResult))
            return false;

        bool patchedAlwaysFails = patchedChance <= 0;
        return liveResult.Success
            && liveRun.CurrentPhase == original.SuccessNextPhase
            && patchedAlwaysFails
            && !patchedResult.Success
            && patchedRun.CurrentPhase == "MOVE"
            && patchedRun.Alert == shop.StartAlert + patched.FailAlert;
    }

    static CsvDatabase PatchAction(CsvDatabase live, string actionId, string column, string value)
    {
        Dictionary<string, string> texts = LoadTableTexts();
        if (texts == null)
            return null;

        string patched;
        if (!TryPatchCsv(texts["ActionTable"], "ActionID", actionId, column, value, out patched))
            return null;

        texts["ActionTable"] = patched;
        CsvDatabase database = CsvDatabase.LoadFromTexts(texts);
        return database;
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

    static bool TryPatchCsv(string csv, string idColumn, string id, string valueColumn, string value, out string patched)
    {
        patched = null;
        if (!string.IsNullOrEmpty(csv) && csv[0] == '\uFEFF')
            csv = csv.Substring(1);
        string[] lines = csv.Replace("\r\n", "\n").Replace('\r', '\n').Split('\n');
        if (lines.Length < 2)
            return false;

        string[] header = lines[0].Split(',');
        int idIndex = -1;
        int valueIndex = -1;
        for (int i = 0; i < header.Length; i++)
        {
            if (header[i] == idColumn)
                idIndex = i;
            if (header[i] == valueColumn)
                valueIndex = i;
        }

        if (idIndex < 0 || valueIndex < 0)
            return false;

        var rebuilt = new List<string>();
        rebuilt.Add(lines[0]);
        bool found = false;
        for (int i = 1; i < lines.Length; i++)
        {
            if (string.IsNullOrEmpty(lines[i]))
                continue;
            string[] cells = lines[i].Split(',');
            if (cells.Length > idIndex && cells[idIndex] == id && cells.Length > valueIndex)
            {
                cells[valueIndex] = value;
                rebuilt.Add(string.Join(",", cells));
                found = true;
            }
            else
            {
                rebuilt.Add(lines[i]);
            }
        }

        if (!found)
            return false;

        patched = string.Join("\n", rebuilt.ToArray());
        return true;
    }

#if UNITY_EDITOR
    [UnityEditor.MenuItem("Candy Thief/V02/Run ActionResolver Tests")]
    static void MenuRunAll()
    {
        CsvDatabase database = CsvDatabase.Current;
        if (database == null || !database.Loaded || database.HasErrors)
            database = CsvLoadRunner.LoadAndReport();

        if (database == null || database.HasErrors)
            return;

        RunAll(database);
    }
#endif
}
