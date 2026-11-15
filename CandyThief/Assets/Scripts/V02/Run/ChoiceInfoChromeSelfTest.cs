using UnityEngine;
using UnityEngine.UI;

public static class ChoiceInfoChromeSelfTest
{
    const string LogPrefix = "[ChoiceInfoChrome] ";

    public static bool RunAll(CsvDatabase database)
    {
        if (database == null || !database.Loaded || database.HasErrors)
        {
            Debug.LogError(LogPrefix + "자가 검사 실패: CsvDatabase가 준비되지 않았습니다.");
            return false;
        }

        RunState previous = RunState.Current;
        int failed = 0;
        failed += Expect("1 일반 행동 카드 2장과 정보 크롬 1세트", TestActionCards(database));
        failed += Expect("2 이벤트 선택지 2장과 정보 크롬 1세트", TestEventOptions(database));
        failed += Expect("3 사탕 정보 클릭은 툴팁만 표시", TestCandyTooltip(database));
        failed += Expect("4 위험도 정보 클릭은 툴팁만 표시", TestAlertTooltip(database));
        failed += Expect("5 정보 클릭은 행동을 실행하지 않음", TestInfoDoesNotExecute(database));
        failed += Expect("6 툴팁이 열려도 타이머는 진행", TestTooltipKeepsTimer(database));
        failed += Expect("7 시간 초과는 툴팁을 닫고 자동 선택", TestTimeoutClosesTooltip(database));
        failed += Expect("8 턴 부족 카드는 비활성과 사유 표시", TestTurnShortfall(database));
        failed += Expect("9 반복 생성과 다시 바인딩해도 크롬은 1세트", TestNoDuplicateChrome(database));
        failed += Expect("10 툴팁 외부 클릭은 닫기만 한다", TestOutsideClickCloses(database));
        failed += Expect("11 다른 정보 버튼은 내용만 교체", TestReplaceTip(database));

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

    static bool TestActionCards(CsvDatabase database)
    {
        GameObject prefab = Resources.Load<GameObject>("V02/UI/ChoiceButton");
        if (prefab == null || !ChromeOnce(prefab.transform) || !InfoAnchors(prefab.transform))
            return false;
        if (FindDirect(prefab.transform, "ForecastTooltip") != null)
            return false;

        V02RunHud hud;
        RunState run;
        if (!TryHud(database, null, out hud, out run))
            return false;
        try
        {
            V02ChoiceButton[] cards = ActiveCards(hud, false);
            if (cards.Length != 2)
                return false;
            for (int i = 0; i < cards.Length; i++)
            {
                if (string.IsNullOrEmpty(cards[i].BoundId) || cards[i].TooltipOpen || !ChromeOnce(cards[i].transform) || !InfoAnchors(cards[i].transform))
                    return false;
                if (FindDirect(cards[i].transform, "ForecastTooltip") != null)
                    return false;
            }

            return TipClearsCards(hud);
        }
        finally
        {
            Object.DestroyImmediate(hud.gameObject);
        }
    }

    static bool TestEventOptions(CsvDatabase database)
    {
        V02RunHud hud;
        RunState run;
        if (!TryHud(database, new FixedChanceRoll(false), out hud, out run))
            return false;
        try
        {
            if (!run.TrySetPendingEvent("EVT_001"))
                return false;
            hud.Refresh();
            V02ChoiceButton[] options = ActiveCards(hud, true);
            V02ChoiceButton[] actions = ActiveCards(hud, false);
            if (!hud.ShowingEventOptions || options.Length != 2 || actions.Length != 0)
                return false;
            for (int i = 0; i < options.Length; i++)
            {
                if (string.IsNullOrEmpty(options[i].BoundId) || !ChromeOnce(options[i].transform))
                    return false;
            }

            return true;
        }
        finally
        {
            Object.DestroyImmediate(hud.gameObject);
        }
    }

    static bool TestCandyTooltip(CsvDatabase database)
    {
        V02RunHud hud;
        RunState run;
        V02ChoiceButton choice;
        if (!OpenChoice(database, "ACT_001", out hud, out run, out choice))
            return false;
        try
        {
            Button info = InfoButton(choice, "CandyInfo");
            if (info == null)
                return false;
            info.onClick.Invoke();
            return choice.TooltipOpen && choice.TooltipLabel == "성공 시 사탕 0~0";
        }
        finally
        {
            Object.DestroyImmediate(hud.gameObject);
        }
    }

    static bool TestAlertTooltip(CsvDatabase database)
    {
        V02RunHud hud;
        RunState run;
        V02ChoiceButton choice;
        if (!OpenChoice(database, "ACT_001", out hud, out run, out choice))
            return false;
        try
        {
            Button info = InfoButton(choice, "AlertInfo");
            if (info == null)
                return false;
            info.onClick.Invoke();
            return choice.TooltipOpen && choice.TooltipLabel == "실패 시 위험도 +5";
        }
        finally
        {
            Object.DestroyImmediate(hud.gameObject);
        }
    }

    static bool TestInfoDoesNotExecute(CsvDatabase database)
    {
        V02RunHud hud;
        RunState run;
        V02ChoiceButton choice;
        if (!OpenChoice(database, "ACT_001", out hud, out run, out choice))
            return false;
        try
        {
            int turns = run.RemainingTurns;
            Button card = choice.GetComponent<Button>();
            Button candy = InfoButton(choice, "CandyInfo");
            Button alert = InfoButton(choice, "AlertInfo");
            if (card == null || candy == null || alert == null || candy == card || alert == card)
                return false;
            candy.onClick.Invoke();
            alert.onClick.Invoke();
            return !hud.IsPresenting
                && string.IsNullOrEmpty(run.LastActionID)
                && run.RemainingTurns == turns
                && run.Status == RunStatus.InProgress
                && choice.TooltipOpen;
        }
        finally
        {
            Object.DestroyImmediate(hud.gameObject);
        }
    }

    static bool TestTooltipKeepsTimer(CsvDatabase database)
    {
        V02RunHud hud;
        RunState run;
        V02ChoiceButton choice;
        if (!OpenChoice(database, "ACT_001", out hud, out run, out choice))
            return false;
        try
        {
            Button candy = InfoButton(choice, "CandyInfo");
            if (candy == null)
                return false;
            candy.onClick.Invoke();
            int turns = run.RemainingTurns;
            hud.TickActionSelect(2f);
            return choice.TooltipOpen
                && hud.TimerCountdownRunning
                && Near(hud.TimerRemaining, 28f)
                && hud.TimerSecondsLabel == "28초"
                && string.IsNullOrEmpty(run.LastActionID)
                && run.RemainingTurns == turns;
        }
        finally
        {
            Object.DestroyImmediate(hud.gameObject);
        }
    }

    static bool TestTimeoutClosesTooltip(CsvDatabase database)
    {
        V02RunHud hud;
        RunState run;
        if (!TryHud(database, new FixedChanceRoll(true), out hud, out run))
            return false;
        try
        {
            V02ChoiceButton choice = FindChoice(hud, "ACT_001");
            Button candy = choice != null ? InfoButton(choice, "CandyInfo") : null;
            if (candy == null)
                return false;
            candy.onClick.Invoke();
            if (!choice.TooltipOpen)
                return false;
            hud.TickActionSelect(30f);
            return !choice.TooltipOpen
                && hud.IsPresenting
                && hud.TimerExpired
                && run.LastActionID == "ACT_001";
        }
        finally
        {
            Object.DestroyImmediate(hud.gameObject);
        }
    }

    static bool TestTurnShortfall(CsvDatabase database)
    {
        V02RunHud hud;
        RunState run;
        if (!TryHud(database, null, out hud, out run))
            return false;
        try
        {
            if (!run.ApplyTurnDelta(-(run.RemainingTurns - 2)))
                return false;
            hud.Refresh();
            V02ChoiceButton[] cards = ActiveCards(hud, false);
            if (cards.Length != 2 || run.RemainingTurns != 2)
                return false;
            int blocked = 0;
            int open = 0;
            for (int i = 0; i < cards.Length; i++)
            {
                ActionRow row;
                if (!database.TryGetAction(cards[i].BoundId, out row))
                    return false;
                Button click = cards[i].GetComponent<Button>();
                if (click == null)
                    return false;
                if (row.TurnCost <= run.RemainingTurns)
                {
                    if (!click.interactable || cards[i].DisabledReasonLabel != "실행하기")
                        return false;
                    open++;
                }
                else
                {
                    string reason = "턴 부족 · 필요 " + row.TurnCost + " / 남은 " + run.RemainingTurns;
                    if (click.interactable || cards[i].DisabledReasonLabel != reason)
                        return false;
                    blocked++;
                }
            }

            return blocked >= 1 && open >= 1;
        }
        finally
        {
            Object.DestroyImmediate(hud.gameObject);
        }
    }

    static bool TestOutsideClickCloses(CsvDatabase database)
    {
        V02RunHud hud;
        RunState run;
        V02ChoiceButton choice;
        if (!OpenChoice(database, "ACT_001", out hud, out run, out choice))
            return false;
        try
        {
            V02ForecastTip tip = hud.GetComponentInChildren<V02ForecastTip>(true);
            Button candy = InfoButton(choice, "CandyInfo");
            if (tip == null || candy == null)
                return false;
            int turns = run.RemainingTurns;
            candy.onClick.Invoke();
            if (!choice.TooltipOpen)
                return false;
            bool forwarded = tip.Route(new System.Collections.Generic.List<GameObject> { choice.gameObject });
            return !forwarded
                && !choice.TooltipOpen
                && !tip.IsOpen
                && !hud.IsPresenting
                && string.IsNullOrEmpty(run.LastActionID)
                && run.RemainingTurns == turns;
        }
        finally
        {
            Object.DestroyImmediate(hud.gameObject);
        }
    }

    static bool TestReplaceTip(CsvDatabase database)
    {
        V02RunHud hud;
        RunState run;
        if (!TryHud(database, null, out hud, out run))
            return false;
        try
        {
            V02ChoiceButton first = FindChoice(hud, "ACT_001");
            V02ChoiceButton second = FindChoice(hud, "ACT_003");
            V02ForecastTip tip = hud.GetComponentInChildren<V02ForecastTip>(true);
            Button candy = InfoButton(first, "CandyInfo");
            Button alert = InfoButton(second, "AlertInfo");
            if (tip == null || candy == null || alert == null)
                return false;
            candy.onClick.Invoke();
            if (!first.TooltipOpen || second.TooltipOpen)
                return false;
            bool forwarded = tip.Route(new System.Collections.Generic.List<GameObject> { alert.gameObject });
            int tips = 0;
            V02ForecastTip[] all = hud.GetComponentsInChildren<V02ForecastTip>(true);
            for (int i = 0; i < all.Length; i++)
            {
                if (all[i] != null && all[i].IsOpen)
                    tips++;
            }

            return forwarded
                && tips == 1
                && !first.TooltipOpen
                && second.TooltipOpen
                && second.TooltipLabel == "실패 시 위험도 +20"
                && !hud.IsPresenting
                && string.IsNullOrEmpty(run.LastActionID);
        }
        finally
        {
            Object.DestroyImmediate(hud.gameObject);
        }
    }

    static bool TestNoDuplicateChrome(CsvDatabase database)
    {
        GameObject prefab = Resources.Load<GameObject>("V02/UI/ChoiceButton");
        if (prefab == null)
            return false;
        GameObject first = Object.Instantiate(prefab);
        GameObject second = Object.Instantiate(prefab);
        try
        {
            if (!ChromeOnce(first.transform) || !ChromeOnce(second.transform))
                return false;
        }
        finally
        {
            Object.DestroyImmediate(first);
            Object.DestroyImmediate(second);
        }

        V02RunHud hud;
        RunState run;
        if (!TryHud(database, null, out hud, out run))
            return false;
        try
        {
            V02ChoiceButton[] cards = ActiveCards(hud, false);
            if (cards.Length != 2)
                return false;
            cards[0].BindForecast(0, 0, 0, 5, null, null);
            cards[0].BindForecast(0, 0, 0, 5, null, null);
            hud.Refresh();
            cards = ActiveCards(hud, false);
            V02ChoiceButton[] all = hud.GetComponentsInChildren<V02ChoiceButton>(true);
            if (cards.Length != 2 || all.Length != 2)
                return false;
            for (int i = 0; i < all.Length; i++)
            {
                if (!ChromeOnce(all[i].transform))
                    return false;
            }

            return true;
        }
        finally
        {
            Object.DestroyImmediate(hud.gameObject);
        }
    }

    static bool OpenChoice(CsvDatabase database, string id, out V02RunHud hud, out RunState run, out V02ChoiceButton choice)
    {
        choice = null;
        if (!TryHud(database, null, out hud, out run))
            return false;
        choice = FindChoice(hud, id);
        if (choice != null)
            return true;
        Object.DestroyImmediate(hud.gameObject);
        hud = null;
        return false;
    }

    static bool TryHud(CsvDatabase database, IChanceRoll chance, out V02RunHud hud, out RunState run)
    {
        hud = null;
        run = null;
        V02RunHud prefab = Resources.Load<V02RunHud>(V02RunHud.ResourcePath);
        if (prefab == null || !RunState.TryStart(database, "SHOP_001", out run))
            return false;
        hud = Object.Instantiate(prefab);
        hud.Bind(database, run, null, chance, null, null, new FixedIntRoll(0), new FixedIntRoll(0));
        return true;
    }

    static V02ChoiceButton[] ActiveCards(V02RunHud hud, bool options)
    {
        var found = new System.Collections.Generic.List<V02ChoiceButton>();
        V02ChoiceButton[] buttons = hud.GetComponentsInChildren<V02ChoiceButton>(true);
        for (int i = 0; i < buttons.Length; i++)
        {
            if (buttons[i] != null && buttons[i].gameObject.activeInHierarchy && buttons[i].IsOption == options)
                found.Add(buttons[i]);
        }

        return found.ToArray();
    }

    static V02ChoiceButton FindChoice(V02RunHud hud, string id)
    {
        V02ChoiceButton[] cards = ActiveCards(hud, false);
        for (int i = 0; i < cards.Length; i++)
        {
            if (cards[i].BoundId == id)
                return cards[i];
        }

        return null;
    }

    static Button InfoButton(V02ChoiceButton choice, string objectName)
    {
        if (choice == null)
            return null;
        Transform info = FindDirect(choice.transform, objectName);
        if (info == null)
            return null;
        return info.GetComponent<Button>();
    }

    static bool ChromeOnce(Transform root)
    {
        return root != null
            && CountDirect(root, "CandyInfo") == 1
            && CountDirect(root, "AlertInfo") == 1
            && CountDirect(root, "ForecastTooltip") == 0
            && IconOnce(root, "CandyInfo")
            && IconOnce(root, "AlertInfo");
    }

    static bool IconOnce(Transform root, string infoName)
    {
        Transform info = FindDirect(root, infoName);
        return info != null && info.GetComponent<Button>() != null && CountDirect(info, "Icon") == 1;
    }

    static bool InfoAnchors(Transform root)
    {
        // PLAY-UI-MOCK-v001 카드(507×623) 안의 정보 칩 줄: 사탕 (28,316,144×60), 위험도 (346,316,133×60). 카드 가운데 칩 줄 양끝에 있다.
        return Anchored(FindDirect(root, "CandyInfo"), new Vector2(28f / 507f, 1f - 376f / 623f), new Vector2(172f / 507f, 1f - 316f / 623f))
            && Anchored(FindDirect(root, "AlertInfo"), new Vector2(346f / 507f, 1f - 376f / 623f), new Vector2(479f / 507f, 1f - 316f / 623f));
    }

    static bool TipClearsCards(V02RunHud hud)
    {
        V02ForecastTip[] tips = hud.GetComponentsInChildren<V02ForecastTip>(true);
        if (tips.Length != 1 || tips[0].Panel == null || tips[0].IsOpen)
            return false;
        RectTransform panel = tips[0].Panel;
        Vector2 min = panel.anchorMin;
        Vector2 max = panel.anchorMax;
        if (min.x < 0f || min.y < 0.49f || max.x > 1f || max.y > 0.7f)
            return false;
        // 카드 스크롤(y 1076~1745), 타이머(y 1026~1082), 상황 문구(y 920~994). 1080×1920 기준 위에서부터의 픽셀을 아래서부터의 비율로 바꾼 값이다.
        if (Overlaps(min, max, new Vector2(0f, 1f - 1745f / 1920f), new Vector2(1f, 1f - 1076f / 1920f)))
            return false;
        if (Overlaps(min, max, new Vector2(0.022f, 1f - 1082f / 1920f), new Vector2(0.978f, 1f - 1026f / 1920f)))
            return false;
        if (Overlaps(min, max, new Vector2(0.035f, 1f - 994f / 1920f), new Vector2(0.965f, 1f - 920f / 1920f)))
            return false;
        return CountNamed(hud.transform, "ForecastTooltip") == 1
            && CountNamed(hud.transform, "ForecastDismiss") == 1;
    }

    static int CountNamed(Transform root, string objectName)
    {
        int count = 0;
        if (root.name == objectName)
            count++;
        for (int i = 0; i < root.childCount; i++)
            count += CountNamed(root.GetChild(i), objectName);
        return count;
    }

    static bool Overlaps(Vector2 aMin, Vector2 aMax, Vector2 bMin, Vector2 bMax)
    {
        return aMin.x < bMax.x && aMax.x > bMin.x && aMin.y < bMax.y && aMax.y > bMin.y;
    }

    static bool Anchored(Transform target, Vector2 min, Vector2 max)
    {
        RectTransform rect = target as RectTransform;
        if (rect == null)
            return false;
        return Near(rect.anchorMin.x, min.x) && Near(rect.anchorMin.y, min.y)
            && Near(rect.anchorMax.x, max.x) && Near(rect.anchorMax.y, max.y);
    }

    static Transform FindDirect(Transform root, string objectName)
    {
        if (root == null)
            return null;
        for (int i = 0; i < root.childCount; i++)
        {
            Transform child = root.GetChild(i);
            if (child.name == objectName)
                return child;
        }

        return null;
    }

    static int CountDirect(Transform root, string objectName)
    {
        int count = 0;
        for (int i = 0; i < root.childCount; i++)
        {
            if (root.GetChild(i).name == objectName)
                count++;
        }

        return count;
    }

    static bool Near(float value, float expected)
    {
        return Mathf.Abs(value - expected) < 0.001f;
    }

#if UNITY_EDITOR
    [UnityEditor.MenuItem("Candy Thief/V02/Run Choice Info Chrome Tests")]
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
