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
        failed += Expect("7 일반 런은 가게 배경 키, 파일 없으면 플레이스홀더", TestShopBackground(database));
        failed += Expect("8 이벤트 이미지가 있으면 우선, 없으면 가게 배경", TestIllustrationPriority());
        failed += Expect("9 상단 페이즈와 페이즈 내러티브", TestPhaseAndNarrative(database));
        failed += Expect("10 이벤트 중에는 EventDescription, 타이머는 새 30초", TestEventNarrativeHidesTimer(database));
        failed += Expect("11 타이머 칸은 30초 가득, 연출 중에는 숨김", TestTimerSlot(database));

        RunState.Current = previous;
        if (failed == 0)
        {
            Debug.Log(LogPrefix + "자가 검사 11건 통과");
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
            if (maxAlert != 100 || hud.DisplayedAlert != 0 || hud.AlertLabel != "0/100" || hud.AlertFillAmount > 0.001f)
                return false;
            if (!run.ApplyAlertDelta(50))
                return false;
            hud.Refresh();
            return hud.DisplayedAlert == 50
                && hud.AlertLabel == "50/100"
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
                && first.CostLabel.IndexOf(action.TurnCost + "턴") >= 0;
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
            if (move.CandyMainLabel != "0")
                return false;
            if (move.AlertMainLabel.IndexOf("+5") < 0)
                return false;

            run.SetPhase("LOOT");
            hud.Refresh();
            V02ChoiceButton loot = FindChoice(hud, "ACT_007");
            return loot != null
                && loot.CandyMainLabel.IndexOf("+100") >= 0
                && loot.AlertMainLabel.IndexOf("+5") >= 0
                && loot.TooltipOpen == false;
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
            if (!run.ApplyTurnDelta(-(run.RemainingTurns - 2)))
                return false;
            hud.Refresh();
            int shown = 0;
            int executable = 0;
            var buttons = hud.GetComponentsInChildren<V02ChoiceButton>(true);
            for (int i = 0; i < buttons.Length; i++)
            {
                if (!buttons[i].gameObject.activeInHierarchy || buttons[i].IsOption)
                    continue;
                ActionRow row;
                if (!database.TryGetAction(buttons[i].BoundId, out row))
                    return false;
                UnityEngine.UI.Button click = buttons[i].GetComponent<UnityEngine.UI.Button>();
                if (click == null)
                    return false;
                shown++;
                if (row.TurnCost <= run.RemainingTurns)
                {
                    if (!click.interactable)
                        return false;
                    executable++;
                }
                else if (click.interactable)
                {
                    return false;
                }
            }

            return shown == 2 && executable >= 1 && hud.DisplayedTurns == 2;
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

    static bool TestShopBackground(CsvDatabase database)
    {
        V02RunHud hud;
        RunState run;
        if (!TryCreateHud(database, out hud, out run))
            return false;
        try
        {
            ShopRow tableShop;
            if (run.Shop == null || !database.TryGetShop(run.ShopID, out tableShop))
                return false;
            Sprite shared = V02ShopSprites.LoadByImageKey(database, tableShop.BackgroundImageKey);
            if (tableShop.BackgroundImageKey != "shop_bg_001"
                || run.Shop.BackgroundImageKey != tableShop.BackgroundImageKey
                || hud.IllustrationKey != tableShop.BackgroundImageKey
                || hud.IllustrationSprite != shared
                || hud.IllustrationIsPlaceholder != (shared == null))
                return false;

            EventRow evt;
            if (!database.TryGetEvent("EVT_001", out evt) || evt.ImageKey != "event_guard_sleep")
                return false;
            if (!run.TrySetPendingEvent(evt.EventID))
                return false;
            hud.Refresh();
            return hud.IllustrationKey == tableShop.BackgroundImageKey
                && hud.IllustrationIsPlaceholder == (shared == null)
                && hud.ShowingEventOptions;
        }
        finally
        {
            Object.DestroyImmediate(hud.gameObject);
        }
    }

    static bool TestPhaseAndNarrative(CsvDatabase database)
    {
        V02RunHud hud;
        RunState run;
        if (!TryCreateHud(database, out hud, out run))
            return false;
        try
        {
            PhaseRow move;
            if (!database.TryGetPhase("MOVE", out move))
                return false;
            if (hud.PhaseLabel != move.PhaseName || hud.NarrativeText != move.SituationText)
                return false;
            if (string.IsNullOrEmpty(move.SituationText) || hud.NarrativeText == move.Notes)
                return false;
            if (!run.SetPhase("LOOT"))
                return false;
            hud.Refresh();
            PhaseRow loot;
            if (!database.TryGetPhase("LOOT", out loot))
                return false;
            return hud.PhaseLabel == loot.PhaseName
                && hud.NarrativeText == loot.SituationText
                && hud.NarrativeText != loot.Notes;
        }
        finally
        {
            Object.DestroyImmediate(hud.gameObject);
        }
    }

    static bool TestEventNarrativeHidesTimer(CsvDatabase database)
    {
        V02RunHud hud;
        RunState run;
        if (!TryCreateHud(database, out hud, out run))
            return false;
        try
        {
            EventRow evt;
            PhaseRow move;
            if (!database.TryGetEvent("EVT_001", out evt) || !database.TryGetPhase("MOVE", out move))
                return false;
            if (!run.TrySetPendingEvent(evt.EventID))
                return false;
            hud.Refresh();
            return hud.NarrativeText == evt.EventDescription
                && hud.PhaseLabel == move.PhaseName
                && hud.TimerSlotVisible
                && hud.TimerSecondsLabel == V02RunHud.FormatTimerSeconds(30f)
                && hud.ShowingEventOptions;
        }
        finally
        {
            Object.DestroyImmediate(hud.gameObject);
        }
    }

    static bool TestTimerSlot(CsvDatabase database)
    {
        if (database.GetInt("ACTION_SELECT_TIME_SEC") != 30)
            return false;
        if (V02RunHud.FormatTimerSeconds(29.1f) != "30초" || V02RunHud.FormatTimerSeconds(30f) != "30초")
            return false;

        V02RunHud hud;
        RunState run;
        if (!TryCreateHud(database, out hud, out run))
            return false;
        try
        {
            string label = V02RunHud.FormatTimerSeconds(30f);
            if (!hud.TimerSlotVisible || hud.TimerSecondsLabel != label || hud.TimerSlotFillAmount < 0.99f)
                return false;
            hud.Refresh();
            if (!hud.TimerSlotVisible || hud.TimerSecondsLabel != label || hud.TimerSlotFillAmount < 0.99f)
                return false;
            if (!hud.TryChoose("ACT_001", false) || !hud.IsPresenting)
                return false;
            return !hud.TimerSlotVisible && hud.TimerSecondsLabel == label;
        }
        finally
        {
            Object.DestroyImmediate(hud.gameObject);
        }
    }

    static bool TestIllustrationPriority()
    {
        Sprite shop = MakeSprite(Color.red);
        Sprite evt = MakeSprite(Color.blue);
        try
        {
            return V02RunHud.PickIllustration(evt, shop) == evt
                && V02RunHud.PickIllustration(null, shop) == shop
                && V02RunHud.PickIllustration(null, null) == null;
        }
        finally
        {
            DestroySprite(evt);
            DestroySprite(shop);
        }
    }

    static Sprite MakeSprite(Color color)
    {
        var texture = new Texture2D(2, 2);
        texture.SetPixel(0, 0, color);
        texture.Apply();
        return Sprite.Create(texture, new Rect(0f, 0f, 2f, 2f), new Vector2(0.5f, 0.5f));
    }

    static void DestroySprite(Sprite sprite)
    {
        if (sprite == null)
            return;
        Texture texture = sprite.texture;
        Object.DestroyImmediate(sprite);
        if (texture != null)
            Object.DestroyImmediate(texture);
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
        hud.Bind(database, run, null, null, null, null, new FixedIntRoll(0));
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
