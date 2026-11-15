using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// OP-DEV-06. 프리팹 배치와 화면 바인딩. 정식 소문 CSV 는 만들지 않는다.
public static class V02OperationPrepSelfTest
{
    const string LogPrefix = "[OperationPrep] ";

    // M4-6 v003 배치 (1080×1920 기준 픽셀). V02OperationPrepPrefabBuilder 의 CardPos / CardSize 와 같아야 한다.
    static readonly Vector2[] CardPos =
    {
        new Vector2(10f, 158f),
        new Vector2(548f, 158f),
        new Vector2(10f, 960f),
        new Vector2(548f, 960f)
    };

    static readonly Vector2 CardSize = new Vector2(522f, 778f);
    static readonly Vector2[] CardMin = new Vector2[4];
    static readonly Vector2[] CardMax = new Vector2[4];

    static V02OperationPrepSelfTest()
    {
        for (int i = 0; i < 4; i++)
        {
            CardMin[i] = new Vector2(CardPos[i].x / 1080f, 1f - (CardPos[i].y + CardSize.y) / 1920f);
            CardMax[i] = new Vector2((CardPos[i].x + CardSize.x) / 1080f, 1f - CardPos[i].y / 1920f);
        }
    }

    static readonly List<string> TempDirs = new List<string>();
    static readonly List<GameObject> Spawned = new List<GameObject>();

    public static bool RunAll(CsvDatabase database)
    {
        if (database == null || !database.Loaded || database.HasErrors)
        {
            Debug.LogError(LogPrefix + "자가 검사 실패: CsvDatabase가 준비되지 않았습니다.");
            return false;
        }

        RunState previous = RunState.Current;
        int failed = 0;
        try
        {
            failed += Expect("1 등급 라벨과 행동력 표기", TestLabels());
            failed += Expect("2 소문 0개 빈 문자열, 1~2개 줄", TestRumorLines());
            failed += Expect("3 프리팹 2×2 배치가 Edit Mode 계층에 있다", TestPrefabLayout());
            failed += Expect("4 카드 4종, 중복 없음, 이미지 없으면 플레이스홀더, Rect 유지", TestFourCards(database));
            failed += Expect("5 선택 가게 ShopID 로 런 시작", TestSelectStartsRun(database));
            failed += Expect("6 무료 리롤 1회 후 선택 해제, 소진 시 버튼 비활성", TestReroll(database));
            failed += Expect("7 리롤 저장 실패 시 카드와 선택 유지", TestRerollFailureKeepsCards(database));
            failed += Expect("8 소문 1개·2개 힌트 표시", TestRumorBinding());
            failed += Expect("9 가게 배경은 Resources 키로 읽고, 파일 없으면 null", TestShopSpriteKey());
            failed += Expect("10 위험도 규칙 없으면 숨김, 값이 있으면 칸만 켠다", TestRiskStaysHidden(database));
        }
        finally
        {
            RunState.Current = previous;
            for (int i = 0; i < TempDirs.Count; i++)
            {
                if (Directory.Exists(TempDirs[i]))
                    Directory.Delete(TempDirs[i], true);
            }

            TempDirs.Clear();
            for (int i = 0; i < Spawned.Count; i++)
            {
                if (Spawned[i] != null)
                    UnityEngine.Object.DestroyImmediate(Spawned[i]);
            }

            Spawned.Clear();
        }

        if (failed == 0)
            Debug.Log(LogPrefix + "OP-DEV-06 자가 검사 10건 통과");
        else
            Debug.LogError(LogPrefix + "자가 검사 실패 " + failed + "건");
        return failed == 0;
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

    static bool TestLabels()
    {
        var shop = new ShopRow
        {
            DistanceGrade = DistanceGrade.NEAR,
            ScaleGrade = ScaleGrade.SMALL,
            SecurityGrade = SecurityGrade.LOOSE,
            GoodsGrade = GoodsGrade.NORMAL,
            EnergyCost = 0
        };
        string grades = V02OperationPrepCard.FormatGrades(shop);
        var far = new ShopRow
        {
            DistanceGrade = DistanceGrade.FAR,
            ScaleGrade = ScaleGrade.LARGE,
            SecurityGrade = SecurityGrade.STRICT,
            GoodsGrade = GoodsGrade.TOP
        };
        return grades == "거리 가까움  규모 소형  경비 허술  상품 보통"
            && V02OperationPrepCard.FormatGrades(far) == "거리 멀리  규모 대형  경비 삼엄  상품 최고급"
            && V02OperationPrepCard.FormatEnergy(0) == "행동력 0"
            && V02OperationPrepCard.FormatEnergy(2) == "행동력 2"
            && V02OperationPrepCard.FormatRerollRemaining(1) == "무료 리롤 1회 남음"
            && V02OperationPrepCard.CanFreeReroll(0, 1, 0, 1)
            && !V02OperationPrepCard.CanFreeReroll(1, 1, 1, 1)
            && !V02OperationPrepCard.CanFreeReroll(0, 1, 1, 1);
    }

    static bool TestRumorLines()
    {
        string none = V02OperationPrepCard.FormatRumors(new string[0]);
        string one = V02OperationPrepCard.FormatRumors(new[] { "힌트 하나" });
        string two = V02OperationPrepCard.FormatRumors(new[] { "힌트 하나", "힌트 둘" });
        var rumor = new ShopRumorRow { RumorName = "이름", HintText = "힌트" };
        var nameOnly = new ShopRumorRow { RumorName = "이름만", HintText = "" };
        return none == string.Empty && one == "힌트 하나" && two == "힌트 하나\n힌트 둘"
            && V02OperationPrepCard.RumorLine(rumor) == "힌트"
            && V02OperationPrepCard.RumorLine(nameOnly) == "이름만"
            && V02OperationPrepCard.RumorLine(null) == string.Empty;
    }

    static bool TestShopSpriteKey()
    {
        string planned = V02ShopSprites.ResourcesKey("Assets/Art/Shops/shop_bg_001.png");
        string direct = V02ShopSprites.ResourcesKey("Assets/Resources/V02/Shops/shop_bg_001.png");
        return planned == "V02/Shops/shop_bg_001"
            && direct == planned
            && V02ShopSprites.ResourcesKey("Assets/Art/Events/event_patrol.png") == string.Empty
            && V02ShopSprites.Load("Assets/Art/Shops/shop_bg_missing_test.png") == null;
    }

    static bool TestRiskStaysHidden(CsvDatabase db)
    {
        ShopRow shop;
        if (!db.TryGetShop("SHOP_003", out shop) || shop.Difficulty <= 0)
            return false;
        V02ShopCardData fromShop = V02ShopCardData.FromShop(shop, null, null);
        if (fromShop.HasRisk || fromShop.RiskSteps != 0)
            return false;

        V02OperationPrepScreen screen = Spawn();
        if (screen == null || screen.Cards == null || screen.Cards[0] == null)
            return false;
        screen.Cards[0].Bind(fromShop, false);
        if (!Inactive(screen.Cards[0].transform, "Face/Risk") || screen.Cards[0].RiskFilledCount != 0)
            return false;

        var shown = new V02ShopCardData { HasRisk = true, RiskSteps = 2, ShopId = shop.ShopID };
        screen.Cards[0].Bind(shown, false);
        Transform risk = screen.Cards[0].transform.Find("Face/Risk");
        return risk != null && risk.gameObject.activeSelf && screen.Cards[0].RiskFilledCount == 2;
    }

    static bool TestPrefabLayout()
    {
        V02OperationPrepScreen prefab = LoadPrefab();
        if (prefab == null || prefab.Cards == null || prefab.Cards.Length != 4)
            return false;
        if (prefab.RerollButton == null || prefab.StartButton == null)
            return false;
        if (prefab.transform.Find("Back") == null || prefab.transform.Find("Header") == null)
            return false;

        for (int i = 0; i < 4; i++)
        {
            if (prefab.Cards[i] == null || prefab.Cards[i].name != "Card" + i)
                return false;
            RectTransform rect = prefab.Cards[i].GetComponent<RectTransform>();
            if (rect == null || !Near(rect.anchorMin, CardMin[i]) || !Near(rect.anchorMax, CardMax[i]))
                return false;
            if (!Inactive(prefab.Cards[i].transform, "Selected"))
                return false;
            if (!Inactive(prefab.Cards[i].transform, "Face/Check"))
                return false;
            if (prefab.Cards[i].transform.Find("Face/ArtMask/Background/Placeholder") == null)
                return false;
            if (prefab.Cards[i].transform.Find("Face/Distance/Value") == null
                || prefab.Cards[i].transform.Find("Face/Scale/Value") == null
                || prefab.Cards[i].transform.Find("Face/Security/Value") == null
                || prefab.Cards[i].transform.Find("Face/Goods/Value") == null)
                return false;
            if (prefab.Cards[i].transform.Find("Face/RumorPanel/Rumor0") == null || prefab.Cards[i].transform.Find("Face/RumorPanel/Rumor1") == null)
                return false;
            if (prefab.Cards[i].transform.Find("Face/RumorPanel/EmptyRumor") == null || prefab.Cards[i].transform.Find("Face/EnergyValue") == null)
                return false;
            if (!Inactive(prefab.Cards[i].transform, "Face/Risk"))
                return false;
            if (prefab.Cards[i].transform.Find("Face/Risk/Pip0/On") == null || prefab.Cards[i].RiskFilledCount != 0)
                return false;
        }

        if (!SameCardSource(prefab))
            return false;

        // 버튼 터치 영역: 컨테이너 Image 가 Raycast Target 이어야 실제 포인터 클릭이 닿는다.
        if (!Raycastable(prefab.RerollButton.gameObject) || !Raycastable(prefab.StartButton.gameObject)
            || !Raycastable(prefab.transform.Find("Back").gameObject))
            return false;
        for (int i = 0; i < 4; i++)
        {
            if (!Raycastable(prefab.Cards[i].Button.gameObject))
                return false;
        }

        TextMeshProUGUI header = prefab.transform.Find("Header").GetComponent<TextMeshProUGUI>();
        TextMeshProUGUI rerollText = prefab.RerollButton.GetComponentInChildren<TextMeshProUGUI>();
        TextMeshProUGUI startText = prefab.StartButton.GetComponentInChildren<TextMeshProUGUI>();
        Transform count = prefab.transform.Find("RerollCount");
        return header != null && header.text == "작전 준비"
            && rerollText != null && rerollText.text == "(1/1)"
            && (count == null || !count.gameObject.activeSelf)
            && startText != null && startText.text == "작전 시작"
            && prefab.LayoutVersion == V02OperationPrepScreen.CurrentLayout;
    }

    static bool TestFourCards(CsvDatabase db)
    {
        string path;
        V02LocalProfileService profiles = Login(out path);
        V02OperationPrepScreen screen = Spawn();
        if (profiles == null || screen == null)
            return false;

        var before = new Vector2[4];
        for (int i = 0; i < 4; i++)
            before[i] = screen.Cards[i].GetComponent<RectTransform>().anchorMin;

        if (!screen.Open(profiles, db, null, null, new ShopListDrawerSelfTest.SeededIntRoll(21)))
            return false;

        var seen = new HashSet<string>();
        for (int i = 0; i < 4; i++)
        {
            V02OperationPrepCard card = screen.Cards[i];
            if (string.IsNullOrEmpty(card.ShopId) || !seen.Add(card.ShopId) || card.PlaceholderVisible != (card.BackgroundSprite == null))
                return false;
            ShopRow shop;
            if (!db.TryGetShop(card.ShopId, out shop))
                return false;
            if (string.IsNullOrEmpty(shop.BackgroundImageKey)
                || card.BackgroundSprite != V02ShopSprites.LoadByImageKey(db, shop.BackgroundImageKey))
                return false;
            if (TextAt(card.transform, "Face/Name") != shop.ShopName)
                return false;
            if (TextAt(card.transform, "Face/Distance/Value") != V02OperationPrepCard.DistanceLabel(shop.DistanceGrade))
                return false;
            if (TextAt(card.transform, "Face/Scale/Value") != V02OperationPrepCard.ScaleLabel(shop.ScaleGrade))
                return false;
            if (TextAt(card.transform, "Face/Security/Value") != V02OperationPrepCard.SecurityLabel(shop.SecurityGrade))
                return false;
            if (TextAt(card.transform, "Face/Goods/Value") != V02OperationPrepCard.GoodsLabel(shop.GoodsGrade))
                return false;
            if (TextAt(card.transform, "Face/EnergyValue") != shop.EnergyCost.ToString(CultureInfo.InvariantCulture))
                return false;
            if (TextAt(card.transform, "Face/RumorPanel/Rumor0") != string.Empty || TextAt(card.transform, "Face/RumorPanel/Rumor1") != string.Empty)
                return false;
            Transform empty = card.transform.Find("Face/RumorPanel/EmptyRumor");
            if (empty == null || !empty.gameObject.activeSelf || card.RiskFilledCount != 0)
                return false;
            if (!Inactive(card.transform, "Face/Risk"))
                return false;
            if (screen.Cards[i].GetComponent<RectTransform>().anchorMin != before[i])
                return false;
        }

        return seen.Count == 4 && !screen.StartButton.interactable;
    }

    static bool TestSelectStartsRun(CsvDatabase db)
    {
        string path;
        V02LocalProfileService profiles = Login(out path);
        V02OperationPrepScreen screen = Spawn();
        if (profiles == null || screen == null)
            return false;

        string started = null;
        if (!screen.Open(profiles, db, shopId =>
        {
            RunState run;
            ShopListResult result = new ShopListManager(profiles, db, null).StartRun(shopId, out run);
            if (result.Ok && run != null)
                started = run.ShopID;
        }, null, new ShopListDrawerSelfTest.SeededIntRoll(31)))
            return false;

        screen.Cards[0].Button.onClick.Invoke();
        screen.Cards[2].Button.onClick.Invoke();
        if (screen.SelectedShopId != screen.Cards[2].ShopId || !screen.StartButton.interactable)
            return false;
        if (screen.Cards[2].transform.Find("Selected") == null || !screen.Cards[2].transform.Find("Selected").gameObject.activeSelf)
            return false;
        if (screen.Cards[2].transform.Find("Face/Check") == null || !screen.Cards[2].transform.Find("Face/Check").gameObject.activeSelf)
            return false;
        if (screen.Cards[0].transform.Find("Selected").gameObject.activeSelf)
            return false;

        screen.StartButton.onClick.Invoke();
        return started == screen.Cards[2].ShopId && RunState.Current != null && RunState.Current.ShopID == started;
    }

    static bool TestReroll(CsvDatabase db)
    {
        string path;
        V02LocalProfileService profiles = Login(out path);
        V02OperationPrepScreen screen = Spawn();
        if (profiles == null || screen == null)
            return false;
        if (!screen.Open(profiles, db, null, null, new ShopListDrawerSelfTest.SeededIntRoll(41)))
            return false;

        screen.Cards[0].Button.onClick.Invoke();
        string[] first = Ids(screen);
        string selected = screen.SelectedShopId;
        if (!screen.RerollButton.interactable || ButtonText(screen.RerollButton) != "(1/1)")
            return false;
        screen.RerollButton.onClick.Invoke();
        string[] second = Ids(screen);
        if (screen.SelectedShopId != string.Empty || screen.StartButton.interactable)
            return false;
        if (Same(first, second) || !FourUnique(second) || profiles.CurrentProfile.shopList.freeRerollUsed != 1)
            return false;
        if (screen.RerollButton.interactable || ButtonText(screen.RerollButton) != "(0/1)")
            return false;

        screen.RerollButton.onClick.Invoke();
        return Same(second, Ids(screen)) && profiles.CurrentProfile.shopList.freeRerollUsed == 1
            && profiles.CurrentProfile.shopList.totalRerollUsed == 1 && selected.Length > 0;
    }

    static bool TestRerollFailureKeepsCards(CsvDatabase db)
    {
        string path;
        V02LocalProfileService profiles = Login(out path);
        V02OperationPrepScreen screen = Spawn();
        if (profiles == null || screen == null)
            return false;
        if (!screen.Open(profiles, db, null, null, new ShopListDrawerSelfTest.SeededIntRoll(51)))
            return false;

        screen.Cards[1].Button.onClick.Invoke();
        string[] before = Ids(screen);
        string selected = screen.SelectedShopId;
        if (File.Exists(path))
            File.Delete(path);
        Directory.CreateDirectory(path);

        screen.RerollButton.onClick.Invoke();
        return Same(before, Ids(screen)) && screen.SelectedShopId == selected
            && screen.Message.Contains("그대로")
            && profiles.CurrentProfile.shopList.freeRerollUsed == 0
            && screen.RerollButton.interactable;
    }

    static bool TestRumorBinding()
    {
        CsvDatabase two = RumorDb(0, 0, 100);
        CsvDatabase one = RumorDb(0, 100, 0);
        return ShowsRumors(two, 2) && ShowsRumors(one, 1);
    }

    static bool ShowsRumors(CsvDatabase db, int perShop)
    {
        if (db == null)
            return false;
        string path;
        V02LocalProfileService profiles = Login(out path);
        V02OperationPrepScreen screen = Spawn();
        if (profiles == null || screen == null)
            return false;
        if (!screen.Open(profiles, db, null, null, new ShopListDrawerSelfTest.SeededIntRoll(61 + perShop)))
            return false;

        for (int i = 0; i < 4; i++)
        {
            string line0 = TextAt(screen.Cards[i].transform, "Face/RumorPanel/Rumor0");
            string line1 = TextAt(screen.Cards[i].transform, "Face/RumorPanel/Rumor1");
            Transform empty = screen.Cards[i].transform.Find("Face/RumorPanel/EmptyRumor");
            if (line0 == null || line1 == null || empty == null || empty.gameObject.activeSelf)
                return false;
            if (perShop == 1 && (line1.Length != 0 || line0.IndexOf("힌트", StringComparison.Ordinal) < 0))
                return false;
            if (perShop == 2 && (line0.IndexOf("힌트", StringComparison.Ordinal) < 0 || line1.IndexOf("힌트", StringComparison.Ordinal) < 0))
                return false;
            if (screen.Cards[i].GetComponent<RectTransform>().anchorMin != CardMin[i])
                return false;
        }

        return true;
    }

    static CsvDatabase RumorDb(int weight0, int weight1, int weight2)
    {
        Dictionary<string, string> texts = ShopRumorSelfTest.LiveTexts();
        if (texts == null)
            return null;
        texts["GameConfig"] = texts["GameConfig"]
            .Replace("RUMOR_COUNT_WEIGHT_0,40,", "RUMOR_COUNT_WEIGHT_0," + weight0.ToString(CultureInfo.InvariantCulture) + ",")
            .Replace("RUMOR_COUNT_WEIGHT_1,45,", "RUMOR_COUNT_WEIGHT_1," + weight1.ToString(CultureInfo.InvariantCulture) + ",")
            .Replace("RUMOR_COUNT_WEIGHT_2,15,", "RUMOR_COUNT_WEIGHT_2," + weight2.ToString(CultureInfo.InvariantCulture) + ",");

        var candidates = new StringBuilder(ShopRumorSelfTest.CandidateHeader);
        for (int i = 1; i <= 20; i++)
        {
            string id = "SHOP_" + i.ToString("D3", CultureInfo.InvariantCulture);
            candidates.Append(ShopRumorSelfTest.C(id, "T_RUM_SUCC", 10));
            candidates.Append(ShopRumorSelfTest.C(id, "T_RUM_EVT", 10));
        }

        texts["ShopRumorTable"] = ShopRumorSelfTest.RumorHeader
            + ShopRumorSelfTest.R("T_RUM_SUCC", "", 5, 0, 0f, 0, 1)
            + ShopRumorSelfTest.R("T_RUM_EVT", "", 0, 0, 0f, 10, 1);
        texts["ShopRumorCandidate"] = candidates.ToString();
        CsvDatabase db = CsvDatabase.LoadFromTexts(texts);
        return db != null && db.Loaded && !db.HasErrors ? db : null;
    }

    static V02OperationPrepScreen Spawn()
    {
        V02OperationPrepScreen prefab = LoadPrefab();
        if (prefab == null)
            return null;
        V02OperationPrepScreen screen = UnityEngine.Object.Instantiate(prefab);
        screen.name = "OperationPrepTest";
        Spawned.Add(screen.gameObject);
        return screen;
    }

    static V02OperationPrepScreen LoadPrefab()
    {
        V02OperationPrepScreen loaded = Resources.Load<V02OperationPrepScreen>(V02OperationPrepScreen.ResourcePath);
#if UNITY_EDITOR
        if (loaded == null)
            loaded = UnityEditor.AssetDatabase.LoadAssetAtPath<V02OperationPrepScreen>("Assets/Resources/V02/UI/OperationPrep.prefab");
#endif
        return loaded;
    }

    static V02LocalProfileService Login(out string path)
    {
        string dir = Path.Combine(Path.GetTempPath(), "ct_prep_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        TempDirs.Add(dir);
        path = Path.Combine(dir, "profiles.json");
        var service = new V02LocalProfileService(path);
        V02PlayerProfile profile;
        string error;
        return service.TryLogin("작전", out profile, out error) ? service : null;
    }

    static string[] Ids(V02OperationPrepScreen screen)
    {
        var ids = new string[4];
        for (int i = 0; i < 4; i++)
            ids[i] = screen.Cards[i].ShopId;
        return ids;
    }

    static bool FourUnique(string[] ids)
    {
        var seen = new HashSet<string>();
        for (int i = 0; i < ids.Length; i++)
        {
            if (string.IsNullOrEmpty(ids[i]) || !seen.Add(ids[i]))
                return false;
        }

        return seen.Count == 4;
    }

    static bool Same(string[] a, string[] b)
    {
        if (a == null || b == null || a.Length != b.Length)
            return false;
        for (int i = 0; i < a.Length; i++)
        {
            if (a[i] != b[i])
                return false;
        }

        return true;
    }

    static bool Raycastable(GameObject go)
    {
        Graphic graphic = go.GetComponent<Graphic>();
        return graphic != null && graphic.raycastTarget;
    }

    static bool Near(Vector2 value, Vector2 expect)
    {
        return Mathf.Abs(value.x - expect.x) < 0.001f && Mathf.Abs(value.y - expect.y) < 0.001f;
    }

    static string ButtonText(Button button)
    {
        if (button == null)
            return null;
        TextMeshProUGUI label = button.GetComponentInChildren<TextMeshProUGUI>(true);
        return label != null ? label.text : null;
    }

    static bool Inactive(Transform root, string path)
    {
        Transform found = root.Find(path);
        return found != null && !found.gameObject.activeSelf;
    }

    static string TextAt(Transform root, string path)
    {
        Transform found = root.Find(path);
        if (found == null)
            return null;
        TextMeshProUGUI label = found.GetComponent<TextMeshProUGUI>();
        return label != null ? label.text : null;
    }

    static bool SameCardSource(V02OperationPrepScreen prefab)
    {
#if UNITY_EDITOR
        UnityEngine.Object first = UnityEditor.PrefabUtility.GetCorrespondingObjectFromSource(prefab.Cards[0].gameObject);
        if (first == null || first.name != "ShopCard")
            return false;
        for (int i = 1; i < prefab.Cards.Length; i++)
        {
            UnityEngine.Object source = UnityEditor.PrefabUtility.GetCorrespondingObjectFromSource(prefab.Cards[i].gameObject);
            if (source != first)
                return false;
        }
#endif
        return true;
    }
}
