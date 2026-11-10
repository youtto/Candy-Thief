using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public sealed class V02ChoiceButton : MonoBehaviour
{
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
    [SerializeField] float enabledAlpha = 1f;
    [SerializeField] float disabledAlpha = 0.45f;

    public string BoundId { get; private set; }
    public bool IsOption { get; private set; }
    public string ChanceLabel { get { return chanceText != null ? chanceText.text : string.Empty; } }
    public string CostLabel { get { return costText != null ? costText.text : string.Empty; } }
    public string SuccessLabel { get { return successText != null && successText.gameObject.activeSelf ? successText.text : string.Empty; } }
    public string FailLabel { get { return failText != null && failText.gameObject.activeSelf ? failText.text : string.Empty; } }

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
        EnsureCardFit();
        EnsureChoiceChrome();
        SetIcon(typeIcon, icon);
        SetIcon(costIcon, hourglass);
        SetText(nameText, choiceName);
        SetText(descriptionText, description);
        SetText(chanceText, chance);
        SetText(costText, cost);
        SetOptionalLine(successText, successOutcome);
        SetOptionalLine(failText, failOutcome);
        SetText(disabledReasonText, canExecute ? string.Empty : disabledReason);
        if (disabledReasonText != null)
            disabledReasonText.gameObject.SetActive(!canExecute && !string.IsNullOrEmpty(disabledReason));

        if (canvasGroup != null)
        {
            canvasGroup.alpha = canExecute ? enabledAlpha : disabledAlpha;
            canvasGroup.interactable = canExecute;
            canvasGroup.blocksRaycasts = true;
        }

        if (button != null)
        {
            button.interactable = canExecute;
            button.onClick.RemoveAllListeners();
            if (canExecute && onClick != null)
                button.onClick.AddListener(onClick);
        }

        LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)transform);
    }

    void EnsureCardFit()
    {
        LayoutElement rootLayout = GetComponent<LayoutElement>();
        if (rootLayout != null)
            rootLayout.preferredHeight = -1f;

        ContentSizeFitter fitter = GetComponent<ContentSizeFitter>();
        if (fitter == null)
            fitter = gameObject.AddComponent<ContentSizeFitter>();
        fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        Image background = GetComponent<Image>();
        if (background != null)
            background.color = new Color(1f, 0.97f, 0.90f, 1f);
    }

    void EnsureChoiceChrome()
    {
        Transform header = transform.Find("Header");
        if (header == null)
        {
            var headerGo = new GameObject("Header", typeof(RectTransform), typeof(LayoutElement), typeof(HorizontalLayoutGroup));
            headerGo.transform.SetParent(transform, false);
            headerGo.transform.SetSiblingIndex(0);
            LayoutElement headerLayout = headerGo.GetComponent<LayoutElement>();
            headerLayout.minHeight = 72f;
            headerLayout.preferredHeight = 76f;
            HorizontalLayoutGroup headerRow = headerGo.GetComponent<HorizontalLayoutGroup>();
            headerRow.spacing = 12f;
            headerRow.childAlignment = TextAnchor.MiddleLeft;
            headerRow.childControlWidth = true;
            headerRow.childControlHeight = true;
            headerRow.childForceExpandWidth = false;
            headerRow.childForceExpandHeight = true;
            header = headerGo.transform;
        }

        if (typeIcon == null)
        {
            Transform existing = transform.Find("TypeIcon");
            if (existing == null)
                existing = header.Find("TypeIcon");
            if (existing != null)
                typeIcon = existing.GetComponent<Image>();
            else
                typeIcon = CreateIcon("TypeIcon", header, 64f);
        }

        if (typeIcon != null && typeIcon.transform.parent != header)
            typeIcon.transform.SetParent(header, false);
        if (nameText != null && nameText.transform.parent != header)
        {
            nameText.transform.SetParent(header, false);
            LayoutElement nameLayout = nameText.GetComponent<LayoutElement>();
            if (nameLayout != null)
                nameLayout.flexibleWidth = 1f;
        }

        Transform costRow = transform.Find("CostRow");
        if (costRow == null && costText != null)
        {
            var costGo = new GameObject("CostRow", typeof(RectTransform), typeof(LayoutElement), typeof(HorizontalLayoutGroup));
            costGo.transform.SetParent(transform, false);
            LayoutElement costLayout = costGo.GetComponent<LayoutElement>();
            costLayout.minHeight = 40f;
            costLayout.preferredHeight = 44f;
            HorizontalLayoutGroup costGroup = costGo.GetComponent<HorizontalLayoutGroup>();
            costGroup.spacing = 8f;
            costGroup.childAlignment = TextAnchor.MiddleLeft;
            costGroup.childControlWidth = false;
            costGroup.childControlHeight = true;
            costGroup.childForceExpandWidth = false;
            costGroup.childForceExpandHeight = true;
            costRow = costGo.transform;
            costText.transform.SetParent(costRow, false);
        }

        if (costIcon == null)
        {
            Transform existing = costRow != null ? costRow.Find("CostIcon") : null;
            if (existing == null && costText != null)
                existing = costText.transform.Find("CostIcon");
            if (existing != null)
                costIcon = existing.GetComponent<Image>();
            else if (costRow != null)
                costIcon = CreateIcon("CostIcon", costRow, 36f);
        }

        if (costIcon != null && costRow != null && costIcon.transform.parent != costRow)
            costIcon.transform.SetParent(costRow, false);
        if (costIcon != null)
            costIcon.transform.SetSiblingIndex(0);
        if (costText != null && costRow != null && costText.transform.parent != costRow)
            costText.transform.SetParent(costRow, false);

        if (nameText != null)
            nameText.fontSize = 36f;
        if (chanceText != null)
        {
            chanceText.fontSize = 30f;
            chanceText.fontStyle = FontStyles.Bold;
        }

        if (costText != null)
        {
            costText.fontSize = 30f;
            costText.fontStyle = FontStyles.Bold;
        }

        if (descriptionText != null)
            descriptionText.fontSize = 24f;

        SetSibling(header, 0);
        SetSibling(chanceText != null ? chanceText.transform : null, 1);
        SetSibling(costRow, 2);
        SetSibling(successText != null ? successText.transform : null, 3);
        SetSibling(failText != null ? failText.transform : null, 4);
        SetSibling(descriptionText != null ? descriptionText.transform : null, 5);
        SetSibling(disabledReasonText != null ? disabledReasonText.transform : null, 6);
    }

    static void SetSibling(Transform child, int index)
    {
        if (child != null)
            child.SetSiblingIndex(index);
    }

    public void EnsureRuntimeChrome()
    {
        EnsureCardFit();
        EnsureChoiceChrome();
    }

    static Image CreateIcon(string name, Transform parent, float size)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(LayoutElement));
        go.transform.SetParent(parent, false);
        Image image = go.GetComponent<Image>();
        image.preserveAspect = true;
        image.raycastTarget = false;
        LayoutElement layout = go.GetComponent<LayoutElement>();
        layout.minWidth = size;
        layout.minHeight = size;
        layout.preferredWidth = size;
        layout.preferredHeight = size;
        return image;
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

    static void SetOptionalLine(TextMeshProUGUI label, string value)
    {
        if (label == null)
            return;
        bool show = !string.IsNullOrEmpty(value);
        label.gameObject.SetActive(show);
        if (show)
            SetText(label, value);
    }

    static void SetText(TextMeshProUGUI label, string value)
    {
        if (label == null)
            return;
        label.text = value ?? string.Empty;
        label.textWrappingMode = TextWrappingModes.Normal;
        label.overflowMode = TextOverflowModes.Overflow;
        LayoutElement layout = label.GetComponent<LayoutElement>();
        if (layout != null)
        {
            label.ForceMeshUpdate();
            float height = Mathf.Max(layout.minHeight, label.preferredHeight);
            layout.preferredHeight = height;
        }
    }
}
