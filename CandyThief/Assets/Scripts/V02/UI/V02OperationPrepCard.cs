using System.Collections.Generic;
using System.Globalization;
using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 공통 가게 카드. 프리팹에는 칸과 스타일만 있고, 가게 내용는 Bind 가 채운다 (D-019).
public sealed class V02OperationPrepCard : MonoBehaviour
{
    public const string EmptyRumorLabel = "소문 없음";

    static readonly Color PlaceholderColor = new Color(0.93f, 0.86f, 0.78f, 1f);

    [SerializeField] Image background;
    [SerializeField] GameObject placeholder;
    [SerializeField] TextMeshProUGUI nameText;
    [SerializeField] TextMeshProUGUI distanceText;
    [SerializeField] TextMeshProUGUI scaleText;
    [SerializeField] TextMeshProUGUI securityText;
    [SerializeField] TextMeshProUGUI goodsText;
    [SerializeField] TextMeshProUGUI energyValue;
    [SerializeField] TextMeshProUGUI rumorLine0;
    [SerializeField] TextMeshProUGUI rumorLine1;
    [SerializeField] GameObject emptyRumor;
    [SerializeField] GameObject selectedMark;
    [SerializeField] GameObject checkMark;
    [SerializeField] GameObject riskRoot;
    [SerializeField] GameObject[] riskFilled;
    [SerializeField] Button button;

    // 이전 프리팹이 리빌드 전에 쓰던 칸. 새 프리팹에서는 비어 있다.
    [SerializeField] TextMeshProUGUI gradeText;
    [SerializeField] TextMeshProUGUI energyText;
    [SerializeField] TextMeshProUGUI rumorText;

    public string ShopId { get; private set; }
    public Button Button => button;
    public bool PlaceholderVisible => placeholder != null && placeholder.activeSelf;
    public Sprite BackgroundSprite => background != null ? background.sprite : null;

    public int RiskFilledCount
    {
        get
        {
            if (riskFilled == null)
                return 0;
            int count = 0;
            for (int i = 0; i < riskFilled.Length; i++)
            {
                if (riskFilled[i] != null && riskFilled[i].activeSelf)
                    count++;
            }

            return count;
        }
    }

    public void Bind(V02ShopCardData data, bool selected)
    {
        if (data == null)
            data = new V02ShopCardData();

        ShopId = data.ShopId ?? string.Empty;
        Set(nameText, data.ShopName);
        Set(distanceText, data.Distance);
        Set(scaleText, data.Scale);
        Set(securityText, data.Security);
        Set(goodsText, data.Goods);
        Set(energyValue, data.EnergyCost.ToString(CultureInfo.InvariantCulture));
        Set(rumorLine0, data.Rumor0);
        Set(rumorLine1, data.Rumor1);

        bool noRumor = string.IsNullOrEmpty(data.Rumor0) && string.IsNullOrEmpty(data.Rumor1);
        if (emptyRumor != null)
            emptyRumor.SetActive(noRumor);

        Set(gradeText, GradeLine(data));
        Set(energyText, FormatEnergy(data.EnergyCost));
        Set(rumorText, JoinRumors(data.Rumor0, data.Rumor1));

        if (background != null)
        {
            background.sprite = data.Background;
            background.color = data.Background != null ? Color.white : PlaceholderColor;
            background.preserveAspect = data.Background != null;
        }

        if (placeholder != null)
            placeholder.SetActive(data.Background == null);

        bool on = selected && ShopId.Length > 0;
        if (selectedMark != null)
            selectedMark.SetActive(on);
        if (checkMark != null)
            checkMark.SetActive(on);

        ApplyRisk(data);
    }

    public static string FormatGrades(ShopRow shop)
    {
        return "거리 " + DistanceLabel(shop.DistanceGrade)
            + "  규모 " + ScaleLabel(shop.ScaleGrade)
            + "  경비 " + SecurityLabel(shop.SecurityGrade)
            + "  상품 " + GoodsLabel(shop.GoodsGrade);
    }

    public static string FormatEnergy(int energyCost)
    {
        return "행동력 " + energyCost.ToString(CultureInfo.InvariantCulture);
    }

    public static string FormatRumors(IReadOnlyList<string> lines)
    {
        string line0;
        string line1;
        SplitRumors(lines, out line0, out line1);
        return JoinRumors(line0, line1);
    }

    public static void SplitRumors(IReadOnlyList<string> lines, out string line0, out string line1)
    {
        line0 = string.Empty;
        line1 = string.Empty;
        if (lines == null)
            return;

        for (int i = 0; i < lines.Count; i++)
        {
            if (string.IsNullOrEmpty(lines[i]))
                continue;
            if (line0.Length == 0)
                line0 = lines[i];
            else if (line1.Length == 0)
            {
                line1 = lines[i];
                return;
            }
        }
    }

    public static string RumorLine(ShopRumorRow rumor)
    {
        if (rumor == null)
            return string.Empty;
        if (!string.IsNullOrEmpty(rumor.HintText))
            return rumor.HintText;
        return rumor.RumorName ?? string.Empty;
    }

    public static string FormatRerollRemaining(int remaining)
    {
        if (remaining < 0)
            remaining = 0;
        return "무료 리롤 " + remaining.ToString(CultureInfo.InvariantCulture) + "회 남음";
    }

    public static bool CanFreeReroll(int freeUsed, int freeLimit, int totalUsed, int totalLimit)
    {
        return freeUsed < freeLimit && totalUsed < totalLimit;
    }

    public static string DistanceLabel(DistanceGrade grade)
    {
        switch (grade)
        {
            case DistanceGrade.NEAR: return "가까움";
            case DistanceGrade.MID: return "보통";
            case DistanceGrade.FAR: return "멀리";
            default: return "—";
        }
    }

    public static string ScaleLabel(ScaleGrade grade)
    {
        switch (grade)
        {
            case ScaleGrade.SMALL: return "소형";
            case ScaleGrade.MEDIUM: return "중형";
            case ScaleGrade.LARGE: return "대형";
            default: return "—";
        }
    }

    public static string SecurityLabel(SecurityGrade grade)
    {
        switch (grade)
        {
            case SecurityGrade.LOOSE: return "허술";
            case SecurityGrade.NORMAL: return "보통";
            case SecurityGrade.STRICT: return "삼엄";
            default: return "—";
        }
    }

    public static string GoodsLabel(GoodsGrade grade)
    {
        switch (grade)
        {
            case GoodsGrade.NORMAL: return "보통";
            case GoodsGrade.HIGH: return "고급";
            case GoodsGrade.TOP: return "최고급";
            default: return "—";
        }
    }

    void ApplyRisk(V02ShopCardData data)
    {
        bool show = data != null && data.HasRisk;
        if (riskRoot != null)
            riskRoot.SetActive(show);
        if (!show)
        {
            ClearRisk();
            return;
        }

        int steps = data.RiskSteps;
        if (riskFilled == null)
            return;
        if (steps < 0)
            steps = 0;
        if (steps > riskFilled.Length)
            steps = riskFilled.Length;
        for (int i = 0; i < riskFilled.Length; i++)
        {
            if (riskFilled[i] != null)
                riskFilled[i].SetActive(i < steps);
        }
    }

    void ClearRisk()
    {
        if (riskFilled == null)
            return;
        for (int i = 0; i < riskFilled.Length; i++)
        {
            if (riskFilled[i] != null)
                riskFilled[i].SetActive(false);
        }
    }

    static string GradeLine(V02ShopCardData data)
    {
        if (data == null || string.IsNullOrEmpty(data.Distance))
            return string.Empty;
        return "거리 " + data.Distance + "  규모 " + data.Scale + "  경비 " + data.Security + "  상품 " + data.Goods;
    }

    static string JoinRumors(string line0, string line1)
    {
        var builder = new StringBuilder();
        if (!string.IsNullOrEmpty(line0))
            builder.Append(line0);
        if (!string.IsNullOrEmpty(line1))
        {
            if (builder.Length > 0)
                builder.Append('\n');
            builder.Append(line1);
        }

        return builder.ToString();
    }

    static void Set(TextMeshProUGUI label, string value)
    {
        if (label != null)
            label.text = value ?? string.Empty;
    }
}
