using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public sealed class V02ChoiceButton : MonoBehaviour
{
    // PLAY-UI-MOCK-v001 카드 비율 (1080×1920 기준 507×623). V02RunHudPrefabBuilder 의 CardSize 와 같아야 한다.
    public const float SlotWidth = 507f;
    public const float SlotHeight = 623f;
    public const string ExecuteLabel = "실행하기";

    static readonly Color ExecuteYellow = new Color(1f, 0.776f, 0.224f, 1f);
    static readonly Color ExecuteOff = new Color(0.78f, 0.74f, 0.68f, 1f);

    [SerializeField] Button button;
    [SerializeField] CanvasGroup canvasGroup;
    [SerializeField] Image typeIcon;
    [SerializeField] Image costIcon;
    [SerializeField] TextMeshProUGUI nameText;
    [SerializeField] TextMeshProUGUI descriptionText;
    [SerializeField] TextMeshProUGUI chanceText;
    [SerializeField] TextMeshProUGUI costText;
    [SerializeField] TextMeshProUGUI successText;
    [SerializeField] TextMeshProUGUI failText;
    [SerializeField] TextMeshProUGUI disabledReasonText;
    [SerializeField] Button candyInfoButton;
    [SerializeField] Button alertInfoButton;
    [SerializeField] float enabledAlpha = 1f;
    [SerializeField] float disabledAlpha = 0.45f;

    string candyTip = string.Empty;
    string alertTip = string.Empty;

    public string BoundId { get; private set; }
    public bool IsOption { get; private set; }
    public string ChanceLabel { get { return chanceText != null ? chanceText.text : string.Empty; } }
    public string CostLabel { get { return costText != null ? costText.text : string.Empty; } }
    public string SuccessLabel { get { return successText != null && successText.gameObject.activeSelf ? successText.text : string.Empty; } }
    public string FailLabel { get { return failText != null && failText.gameObject.activeSelf ? failText.text : string.Empty; } }
    public string CandyMainLabel { get { return SuccessLabel; } }
    public string AlertMainLabel { get { return FailLabel; } }
    public string TooltipLabel { get { return TooltipOpen && tipHost != null ? tipHost.Label : string.Empty; } }
    public bool TooltipOpen { get { return tipHost != null && tipHost.IsOpen && tipHost.Owner == this; } }

    V02ForecastTip tipHost;

    public void AttachTip(V02ForecastTip tip)
    {
        tipHost = tip;
    }
    public string DisabledReasonLabel { get { return disabledReasonText != null ? disabledReasonText.text : string.Empty; } }

    bool chromeMissingLogged;

    public void Bind(
        string id,
        bool isOption,
        string choiceName,
        string description,
        string chance,
        string cost,
        string successOutcome,
        string failOutcome,
        bool canExecute,
        string disabledReason,
        Sprite icon,
        Sprite hourglass,
        UnityAction onClick)
    {
        BoundId = id;
        IsOption = isOption;
        ResolveInfoChrome();
        LayoutElement layout = GetComponent<LayoutElement>();
        if (layout != null)
        {
            layout.preferredWidth = SlotWidth;
            layout.preferredHeight = SlotHeight;
            layout.minWidth = SlotWidth;
            layout.minHeight = SlotHeight;
            layout.flexibleWidth = 0f;
            layout.flexibleHeight = 0f;
        }

        HideTooltip();
        SetIcon(typeIcon, icon);
        SetIcon(costIcon, hourglass);
        SetText(nameText, choiceName);
        SetText(descriptionText, description);
        SetText(chanceText, chance);
        SetText(costText, cost);
        if (successText != null)
            successText.gameObject.SetActive(false);
        if (failText != null)
            failText.gameObject.SetActive(false);

        if (canvasGroup != null)
        {
            canvasGroup.alpha = 1f;
            canvasGroup.interactable = true;
            canvasGroup.blocksRaycasts = true;
        }

        if (disabledReasonText != null)
        {
            disabledReasonText.gameObject.SetActive(true);
            SetText(disabledReasonText, canExecute ? ExecuteLabel : disabledReason);
            disabledReasonText.color = Color.white;
        }

        Image executePlate = disabledReasonText != null ? disabledReasonText.transform.parent.GetComponent<Image>() : null;
        if (executePlate != null && disabledReasonText.transform.parent != transform)
            executePlate.color = canExecute ? ExecuteYellow : ExecuteOff;

        if (button != null)
        {
            button.interactable = canExecute;
            button.onClick.RemoveAllListeners();
            if (canExecute && onClick != null)
                button.onClick.AddListener(onClick);
        }
    }

    // 성공 사탕은 최소=최대(현재 SuccessCandy). 위험도 메인 숫자는 실패 변화량.
    public void BindForecast(int candyMin, int candyMax, int successAlert, int failAlert, Sprite candy, Sprite alert)
    {
        EnsureInfoChrome(candy, alert);
        if (successText != null)
        {
            successText.gameObject.SetActive(true);
            SetText(successText, Signed(candyMax));
            successText.color = Color.white;
            successText.alignment = TextAlignmentOptions.Center;
        }

        if (failText != null)
        {
            failText.gameObject.SetActive(true);
            SetText(failText, Signed(failAlert));
            failText.color = Color.white;
            failText.alignment = TextAlignmentOptions.Center;
        }

        candyTip = "성공 시 사탕 " + candyMin + "~" + candyMax;
        alertTip = "실패 시 위험도 " + Signed(failAlert);
        if (successAlert != 0)
            alertTip += "\n성공 시 위험도 " + Signed(successAlert);
        HideTooltip();
    }

    public void HideTooltip()
    {
        if (tipHost != null && tipHost.Owner == this)
            tipHost.Hide();
    }

    void EnsureInfoChrome(Sprite candy, Sprite alert)
    {
        ResolveInfoChrome();
        ApplyInfoIcon(candyInfoButton, candy);
        ApplyInfoIcon(alertInfoButton, alert);
        if ((candyInfoButton == null || alertInfoButton == null) && !chromeMissingLogged)
        {
            chromeMissingLogged = true;
            Debug.LogError("[V02ChoiceButton] 사탕 정보 또는 위험도 정보 버튼이 프리팹에 없습니다.");
        }

        WireInfo(candyInfoButton, true);
        WireInfo(alertInfoButton, false);
    }

    void ResolveInfoChrome()
    {
        if (candyInfoButton == null)
            candyInfoButton = FindButton("CandyInfo");
        if (alertInfoButton == null)
            alertInfoButton = FindButton("AlertInfo");
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

    static Button FindButton(Transform root, string objectName)
    {
        Transform existing = FindDirect(root, objectName);
        if (existing == null)
            return null;
        return existing.GetComponent<Button>();
    }

    Button FindButton(string objectName)
    {
        return FindButton(transform, objectName);
    }

    static void ApplyInfoIcon(Button info, Sprite sprite)
    {
        if (info == null)
            return;
        Transform icon = FindDirect(info.transform, "Icon");
        if (icon == null)
            return;
        SetIcon(icon.GetComponent<Image>(), sprite);
    }

    void WireInfo(Button info, bool candy)
    {
        if (info == null)
            return;
        info.onClick.RemoveAllListeners();
        info.onClick.AddListener(candy ? (UnityAction)ShowCandyTip : ShowAlertTip);
    }

    void ShowCandyTip()
    {
        ShowTip(candyTip);
    }

    void ShowAlertTip()
    {
        ShowTip(alertTip);
    }

    void ShowTip(string body)
    {
        if (tipHost == null)
            return;
        tipHost.Show(this, body);
    }

    static string Signed(int value)
    {
        if (value > 0)
            return "+" + value;
        return value.ToString();
    }

    static void SetIcon(Image image, Sprite sprite)
    {
        if (image == null)
            return;
        image.sprite = sprite;
        image.enabled = sprite != null;
        image.preserveAspect = true;
        image.color = Color.white;
    }

    static void SetText(TextMeshProUGUI label, string value)
    {
        if (label == null)
            return;
        label.text = value ?? string.Empty;
        label.textWrappingMode = TextWrappingModes.Normal;
        label.overflowMode = TextOverflowModes.Overflow;
    }
}
