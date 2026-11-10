using UnityEngine;

public static class HudPresentationSelfTest
{
    public const string LogPrefix = "[HudPresentation] ";

    public static bool RunAll(CsvDatabase database)
    {
        if (database == null || !database.Loaded || database.HasErrors)
        {
            Debug.LogError(LogPrefix + "자가 검사 실패: CsvDatabase가 준비되지 않았습니다.");
            return false;
        }

        RunState previous = RunState.Current;
        int failed = 0;
        failed += Expect("1 턴·사탕 표시가 RunState와 일치", TestTurnAndCandyMatch(database));
        failed += Expect("2 위험도 게이지가 Alert/MAX_ALERT와 일치", TestAlertGauge(database));
        failed += Expect("3 선택지 성공률·턴 비용", TestChoiceChanceAndCost(database));
        failed += Expect("4 0이 아닌 성공 보상·실패 위험만 표시", TestNonZeroEffects(database));
        failed += Expect("5 턴 부족 선택지는 비활성", TestInsufficientTurns(database));
        failed += Expect("6 UI 라벨은 위험도", TestRiskLabel(database));

        RunState.Current = previous;
        if (failed == 0)
        {
            Debug.Log(LogPrefix + "자가 검사 6건 통과");
            return true;
        }

        Debug.LogError(LogPrefix + "자가 검사 실패: " + failed + "건");
        return false;
    }

    static int Expect(string name, bool passed)
    {
        if (passed)
        {
            Debug.Log(LogPrefix + "PASS " + name);
            return 0;
        }

        Debug.LogError(LogPrefix + "FAIL " + name);
        return 1;
    }

    static bool TestTurnAndCandyMatch(CsvDatabase database)
    {
        V02RunHud hud;
        RunState run;
        if (!TryCreateHud(database, out hud, out run))
            return false;
        try
        {
            return hud.DisplayedTurns == run.RemainingTurns
                && hud.DisplayedCandy == run.Candy
                && run.RemainingTurns == 15
                && run.Candy == 0;
        }
        finally
        {
            Object.DestroyImmediate(hud.gameObject);
        }
    }

    static bool TestAlertGauge(CsvDatabase database)
    {
        V02RunHud hud;
        RunState run;
        if (!TryCreateHud(database, out hud, out run))
            return false;
        try
        {
            int maxAlert = database.GetInt("MAX_ALERT");
            if (maxAlert != 100 || hud.DisplayedAlert != 0 || hud.AlertFillAmount > 0.001f)
                return false;
            if (!run.ApplyAlertDelta(50))
                return false;
            hud.Refresh();
            return hud.DisplayedAlert == 50
                && Mathf.Abs(hud.AlertFillAmount - 0.5f) < 0.02f;
        }
        finally
        {
            Object.DestroyImmediate(hud.gameObject);
        }
    }

    static bool TestChoiceChanceAndCost(CsvDatabase database)
    {
        V02RunHud hud;
        RunState run;
        if (!TryCreateHud(database, out hud, out run))
            return false;
        try
        {
            V02ChoiceButton first = FindChoice(hud, "ACT_001");
            if (first == null)
                return false;
            var resolver = new ActionResolver(database);
            ActionRow action;
            if (!database.TryGetAction("ACT_001", out action))
                return false;
            ShopRow shop;
            database.TryGetShop(run.ShopID, out shop);
            int chance = resolver.ComputeSuccessChance(action, shop);
            return first.ChanceLabel.IndexOf(chance + "%") >= 0
                && first.CostLabel.IndexOf("턴 -" + action.TurnCost) >= 0;
        }
        finally
        {
            Object.DestroyImmediate(hud.gameObject);
        }
    }

    static bool TestNonZeroEffects(CsvDatabase database)
    {
        V02RunHud hud;
        RunState run;
        if (!TryCreateHud(database, out hud, out run))
            return false;
        try
        {
            V02ChoiceButton move = FindChoice(hud, "ACT_001");
            if (move == null)
                return false;
            if (!string.IsNullOrEmpty(move.SuccessLabel))
                return false;
            if (move.FailLabel.IndexOf("위험도 +5") < 0)
                return false;

            run.SetPhase("LOOT");
            hud.Refresh();
            V02ChoiceButton loot = FindChoice(hud, "ACT_007");
            return loot != null
                && loot.SuccessLabel.IndexOf("사탕 +100") >= 0
                && loot.FailLabel.IndexOf("위험도 +5") >= 0;
        }
        finally
        {
            Object.DestroyImmediate(hud.gameObject);
        }
    }

    static bool TestInsufficientTurns(CsvDatabase database)
    {
        V02RunHud hud;
        RunState run;
        if (!TryCreateHud(database, out hud, out run))
            return false;
        try
        {
            ActionRow action;
            if (!database.TryGetAction("ACT_001", out action))
                return false;
            if (!run.ApplyTurnDelta(-(run.RemainingTurns - (action.TurnCost - 1))))
                return false;
            hud.Refresh();
            V02ChoiceButton choice = FindChoice(hud, "ACT_001");
            return choice != null
                && hud.DisplayedTurns < action.TurnCost
                && choice.GetComponent<UnityEngine.UI.Button>() != null
                && !choice.GetComponent<UnityEngine.UI.Button>().interactable;
        }
        finally
        {
            Object.DestroyImmediate(hud.gameObject);
        }
    }

    static bool TestRiskLabel(CsvDatabase database)
    {
        V02RunHud hud;
        RunState run;
        if (!TryCreateHud(database, out hud, out run))
            return false;
        try
        {
            bool found = false;
            var labels = hud.GetComponentsInChildren<TMPro.TextMeshProUGUI>(true);
            for (int i = 0; i < labels.Length; i++)
            {
                if (!labels[i].gameObject.activeInHierarchy)
                    continue;
                if (labels[i].text.IndexOf("경계도") >= 0)
                    return false;
                if (labels[i].text.IndexOf("위험도") >= 0)
                    found = true;
            }

            return found;
        }
        finally
        {
            Object.DestroyImmediate(hud.gameObject);
        }
    }

    static V02ChoiceButton FindChoice(V02RunHud hud, string id)
    {
        var buttons = hud.GetComponentsInChildren<V02ChoiceButton>(true);
        for (int i = 0; i < buttons.Length; i++)
        {
            if (buttons[i].gameObject.activeInHierarchy && buttons[i].BoundId == id)
                return buttons[i];
        }

        return null;
    }

    static bool TryCreateHud(CsvDatabase database, out V02RunHud hud, out RunState run)
    {
        hud = null;
        run = null;
        V02RunHud prefab = Resources.Load<V02RunHud>(V02RunHud.ResourcePath);
        if (prefab == null || !RunState.TryStart(database, "SHOP_001", out run))
            return false;
        hud = Object.Instantiate(prefab);
        hud.Bind(database, run, null);
        return true;
    }

#if UNITY_EDITOR
    [UnityEditor.MenuItem("Candy Thief/V02/Run HudPresentation Tests")]
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
