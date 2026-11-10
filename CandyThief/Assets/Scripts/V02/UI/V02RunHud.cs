using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class V02RunHud : MonoBehaviour
{
    public const string ResourcePath = "V02/UI/RunHud";
    const string LogPrefix = "[V02RunHud] ";

    [SerializeField] TextMeshProUGUI turnsText;
    [SerializeField] TextMeshProUGUI alertText;
    [SerializeField] TextMeshProUGUI candyText;
    [SerializeField] Image illustration;
    [SerializeField] Sprite placeholderSprite;
    [SerializeField] Color placeholderColor = new Color(0.86f, 0.78f, 0.70f, 1f);
    [SerializeField] TextMeshProUGUI titleText;
    [SerializeField] TextMeshProUGUI bodyText;
    [SerializeField] GameObject resultRoot;
    [SerializeField] TextMeshProUGUI resultStatusText;
    [SerializeField] TextMeshProUGUI resultMessageText;
    [SerializeField] TextMeshProUGUI resultDeltaText;
    [SerializeField] Transform choiceList;
    [SerializeField] ScrollRect choiceScroll;
    [SerializeField] CanvasGroup choiceCanvasGroup;
    [SerializeField] V02ChoiceButton choiceButtonPrefab;
    [SerializeField] Button lobbyButton;
    [SerializeField] V02ResultPresentation resultPresentation;
    [SerializeField] Image turnChip;
    [SerializeField] Image turnIcon;
    [SerializeField] Image alertChip;
    [SerializeField] Image alertIcon;
    [SerializeField] Image alertTrack;
    [SerializeField] Image alertFill;
    [SerializeField] Image candyChip;
    [SerializeField] Image candyIcon;
    [SerializeField] Sprite hourglassSprite;
    [SerializeField] Sprite alertSprite;
    [SerializeField] Sprite candySprite;
    [SerializeField] Sprite moveIcon;
    [SerializeField] Sprite infiltrateIcon;
    [SerializeField] Sprite lootIcon;
    [SerializeField] Sprite escapeIcon;
    [SerializeField] Sprite eventIcon;

    const int LowTurnWarning = 3;
    static readonly Color ChipCream = new Color(1f, 0.96f, 0.88f, 1f);
    static readonly Color AlertSafe = new Color(0.42f, 0.78f, 0.45f, 1f);
    static readonly Color AlertCaution = new Color(0.96f, 0.72f, 0.28f, 1f);
    static readonly Color AlertDanger = new Color(0.86f, 0.32f, 0.30f, 1f);
    static readonly Color AlertFail = new Color(0.72f, 0.12f, 0.18f, 1f);
    static readonly Color TurnWarn = new Color(0.86f, 0.42f, 0.16f, 1f);

    readonly List<PresentedAction> presentedActions = new List<PresentedAction>();
    readonly List<PresentedOption> presentedOptions = new List<PresentedOption>();
    readonly List<V02ChoiceButton> pooledButtons = new List<V02ChoiceButton>();

    CsvDatabase database;
    RunState run;
    ActionResolver actionResolver;
    EventResolver eventResolver;
    ShopRow shop;

    public RectTransform Rect => (RectTransform)transform;
    public V02ResultPresentation ResultPresentation => resultPresentation;
    public bool IsPresenting { get { return resultPresentation != null && resultPresentation.IsPresenting; } }
    public int DisplayedTurns { get; private set; }
    public int DisplayedAlert { get; private set; }
    public int DisplayedCandy { get; private set; }
    public float AlertFillAmount { get { return alertFill != null ? alertFill.fillAmount : 0f; } }
    public bool ShowingEventOptions { get; private set; }
    public bool ChoicesLocked { get { return choiceCanvasGroup != null && !choiceCanvasGroup.interactable; } }
    public bool HasVisibleChoices
    {
        get
        {
            for (int i = 0; i < pooledButtons.Count; i++)
            {
                if (pooledButtons[i] != null && pooledButtons[i].gameObject.activeSelf)
                    return true;
            }

            return false;
        }
    }

    public void Bind(CsvDatabase csv, RunState runState, UnityEngine.Events.UnityAction onLobby)
    {
        Bind(csv, runState, onLobby, null, null);
    }

    public void Bind(CsvDatabase csv, RunState runState, UnityEngine.Events.UnityAction onLobby, IChanceRoll chanceRoll, IEventPick eventPick)
    {
        database = csv;
        run = runState;
        actionResolver = chanceRoll == null ? new ActionResolver(database) : new ActionResolver(database, chanceRoll);
        eventResolver = eventPick == null && chanceRoll == null
            ? new EventResolver(database)
            : new EventResolver(database, chanceRoll, eventPick);
        shop = default(ShopRow);
        if (database != null)
            database.TryGetShop(run.ShopID, out shop);

        if (lobbyButton != null)
        {
            lobbyButton.onClick.RemoveAllListeners();
            if (onLobby != null)
                lobbyButton.onClick.AddListener(onLobby);
        }

        EnsureHudChrome();
        EnsurePresentation();
        ClearResult();
        SetChoicesLocked(false);
        Refresh();
    }

    public void Refresh()
    {
        if (run == null)
            return;

        ApplyStats(run.RemainingTurns, run.Alert, run.Candy, false);
        BindCenter();
        BindChoices();
        ResetChoiceScroll();
    }

    void BindCenter()
    {
        if (run.HasPendingEvent)
        {
            EventRow evt;
            if (database != null && database.TryGetEvent(run.PendingEventID, out evt))
            {
                SetText(titleText, evt.EventName);
                SetText(bodyText, evt.EventDescription);
                BindIllustration(evt.ImageKey);
                return;
            }
        }

        PhaseRow phase;
        string phaseName = run.CurrentPhase;
        if (database != null && database.TryGetPhase(run.CurrentPhase, out phase))
            phaseName = phase.PhaseName;
        SetText(titleText, phaseName);

        ActionRow lastAction;
        if (!string.IsNullOrEmpty(run.LastActionID) && database != null && database.TryGetAction(run.LastActionID, out lastAction))
            SetText(bodyText, lastAction.Description);
        else
            SetText(bodyText, string.Empty);

        BindIllustration(string.Empty);
    }

    void BindIllustration(string imageKey)
    {
        if (illustration == null)
            return;

        Sprite sprite = placeholderSprite;
        Color color = placeholderColor;
        if (!string.IsNullOrEmpty(imageKey) && database != null)
        {
            AssetRow asset;
            if (database.TryGetAssetByImageKey(imageKey, out asset))
            {
                Sprite loaded = LoadSprite(asset.FilePath);
                if (loaded != null)
                {
                    sprite = loaded;
                    color = Color.white;
                }
            }
        }

        illustration.sprite = sprite;
        illustration.color = color;
        illustration.preserveAspect = true;
        Outline outline = illustration.GetComponent<Outline>();
        if (outline == null)
            outline = illustration.gameObject.AddComponent<Outline>();
        outline.effectColor = new Color(0.35f, 0.28f, 0.22f, 0.55f);
        outline.effectDistance = new Vector2(3f, -3f);
        Transform center = illustration.transform.parent;
        if (center != null)
        {
            Image centerImage = center.GetComponent<Image>();
            if (centerImage != null)
                centerImage.color = new Color(1f, 0.94f, 0.82f, 1f);
        }
    }

    void BindChoices()
    {
        HidePooled();
        ShowingEventOptions = false;
        if (run.IsEnded)
            return;

        if (run.HasPendingEvent)
        {
            ShowingEventOptions = true;
            eventResolver.CollectPresentedOptions(run, presentedOptions);
            for (int i = 0; i < presentedOptions.Count; i++)
            {
                PresentedOption presented = presentedOptions[i];
                EventOptionRow option = presented.Option;
                int chance = eventResolver.ComputeOptionChance(option, shop);
                BindButton(
                    GetButton(i),
                    option.OptionID,
                    true,
                    option.OptionName,
                    option.Description,
                    chance,
                    option.TurnCost,
                    option.SuccessCandy,
                    option.SuccessAlert,
                    option.SuccessTurnDelta,
                    option.FailCandy,
                    option.FailAlert,
                    option.FailTurnDelta,
                    presented.CanExecute,
                    presented.ExecuteReason == EventOptionRejectReason.InsufficientTurns
                        ? FormatTurnShortfall(option.TurnCost)
                        : string.Empty);
            }

            return;
        }

        actionResolver.CollectPresentedActions(run, presentedActions);
        for (int i = 0; i < presentedActions.Count; i++)
        {
            PresentedAction presented = presentedActions[i];
            ActionRow action = presented.Action;
            int chance = actionResolver.ComputeSuccessChance(action, shop);
            BindButton(
                GetButton(i),
                action.ActionID,
                false,
                action.ActionName,
                action.Description,
                chance,
                action.TurnCost,
                action.SuccessCandy,
                action.SuccessAlert,
                action.SuccessTurnDelta,
                action.FailCandy,
                action.FailAlert,
                action.FailTurnDelta,
                presented.CanExecute,
                presented.ExecuteReason == ActionRejectReason.InsufficientTurns
                    ? FormatTurnShortfall(action.TurnCost)
                    : string.Empty);
        }
    }

    void BindButton(
        V02ChoiceButton button,
        string id,
        bool isOption,
        string choiceName,
        string description,
        int chance,
        int turnCost,
        int successCandy,
        int successAlert,
        int successTurn,
        int failCandy,
        int failAlert,
        int failTurn,
        bool canExecute,
        string disabledReason)
    {
        string capturedId = id;
        bool capturedOption = isOption;
        button.gameObject.SetActive(true);
        button.Bind(
            id,
            isOption,
            choiceName,
            description,
            "성공률 " + chance + "%",
            "턴 -" + turnCost,
            FormatEffectLine("성공 시 ", successCandy, successAlert, successTurn),
            FormatEffectLine("실패 시 ", failCandy, failAlert, failTurn),
            canExecute,
            disabledReason,
            IconForChoice(isOption, id),
            hourglassSprite,
            () => OnChoiceClicked(capturedId, capturedOption));
    }

    public bool TryChoose(string id, bool isOption)
    {
        if (run == null || run.IsEnded || IsPresenting)
            return false;
        OnChoiceClicked(id, isOption);
        return IsPresenting;
    }

    public bool TryContinueResult()
    {
        return resultPresentation != null && resultPresentation.TryContinue();
    }

    void OnChoiceClicked(string id, bool isOption)
    {
        if (run == null || run.IsEnded)
            return;
        if (IsPresenting)
            return;

        SetChoicesLocked(true);
        int candyBefore = run.Candy;
        int alertBefore = run.Alert;
        int turnsBefore = run.RemainingTurns;

        if (isOption)
        {
            EventOptionRow option;
            EventOptionResolveResult result;
            if (!database.TryGetEventOption(id, out option))
            {
                SetChoicesLocked(false);
                return;
            }

            if (!eventResolver.TryResolveOption(run, id, out result) || !result.Applied)
            {
                SetChoicesLocked(false);
                return;
            }

            PresentResolve(
                "OPTION",
                option.OptionID,
                option.OptionName,
                result.Success,
                run.Candy - candyBefore,
                run.Alert - alertBefore,
                run.RemainingTurns - turnsBefore);
            RefreshStats(true);
            return;
        }

        ActionRow action;
        if (!database.TryGetAction(id, out action))
        {
            SetChoicesLocked(false);
            return;
        }

        string phaseBefore = run.CurrentPhase;
        ActionResolveResult resolved;
        if (!actionResolver.TryResolve(run, id, out resolved) || !resolved.Applied)
        {
            SetChoicesLocked(false);
            return;
        }

        if (resolved.EventCheckDue)
        {
            EventDrawResult draw;
            eventResolver.TryDraw(run, resolved.EventCheckDue, resolved.EventChance, phaseBefore, out draw);
        }

        PresentResolve(
            "ACTION",
            action.ActionID,
            action.ActionName,
            resolved.Success,
            run.Candy - candyBefore,
            run.Alert - alertBefore,
            run.RemainingTurns - turnsBefore);
        RefreshStats(true);
    }

    void PresentResolve(string sourceType, string sourceId, string displayName, bool success, int candyDelta, int alertDelta, int turnDelta)
    {
        var presented = new PresentedResolve
        {
            SourceType = sourceType,
            SourceId = sourceId,
            DisplayName = displayName,
            Success = success,
            CandyDelta = candyDelta,
            AlertDelta = alertDelta,
            TurnDelta = turnDelta,
            RunEnded = run != null && run.IsEnded,
            Status = run != null ? run.Status : RunStatus.InProgress,
            FailReason = run != null ? run.FailReason : RunFailReason.None
        };

        ResultFeedbackRow feedback;
        string body = displayName;
        string presentationId = success ? V02ResultPresentation.SuccessDefault : V02ResultPresentation.FailDefault;
        if (database != null && database.TryGetResultFeedback(sourceType, sourceId, out feedback))
        {
            body = success ? feedback.SuccessText : feedback.FailText;
            presentationId = success ? feedback.SuccessPresentationID : feedback.FailPresentationID;
        }

        if (presented.RunEnded && presented.Status == RunStatus.Won)
            presentationId = V02ResultPresentation.RunWin;
        else if (presented.RunEnded)
            presentationId = V02ResultPresentation.RunFail;
        else if (!success && presented.AlertDelta > 0 && presentationId == V02ResultPresentation.FailDefault)
            presentationId = V02ResultPresentation.FailAlert;

        EnsurePresentation();
        if (resultRoot != null)
            resultRoot.SetActive(false);
        resultPresentation.Present(presented, body, presentationId);
    }

    void RefreshStats()
    {
        RefreshStats(false);
    }

    void RefreshStats(bool animate)
    {
        if (run == null)
            return;
        EnsureHudChrome();
        ApplyStats(run.RemainingTurns, run.Alert, run.Candy, animate && Application.isPlaying);
    }

    void ApplyStats(int turns, int alert, int candy, bool animate)
    {
        DisplayedTurns = turns;
        DisplayedAlert = alert;
        DisplayedCandy = candy;
        SetText(turnsText, turns.ToString());
        SetText(alertText, alert.ToString());
        SetText(candyText, candy.ToString());
        ApplyTurnWarning(turns);
        ApplyAlertGauge(alert, animate);
        if (animate)
        {
            Punch(turnsText);
            if (alert > 0)
                Punch(alertFill != null ? alertFill.rectTransform : null);
            Punch(candyText);
        }
    }

    void ApplyTurnWarning(int turns)
    {
        Color color = turns <= LowTurnWarning ? TurnWarn : V02Ui.Ink;
        if (turnsText != null)
            turnsText.color = color;
        if (turnChip != null)
            turnChip.color = turns <= LowTurnWarning ? new Color(1f, 0.86f, 0.70f, 1f) : ChipCream;
    }

    void ApplyAlertGauge(int alert, bool animate)
    {
        int maxAlert = 100;
        if (database != null)
            maxAlert = Mathf.Max(1, database.GetInt("MAX_ALERT"));
        float fill = Mathf.Clamp01((float)alert / maxAlert);
        Color color = AlertColor(fill);
        if (alertFill != null)
        {
            if (animate)
                StartCoroutine(LerpFill(alertFill, fill, color));
            else
            {
                alertFill.fillAmount = fill;
                alertFill.color = color;
            }
        }

        if (alertChip != null)
            alertChip.color = fill >= 1f ? new Color(1f, 0.78f, 0.76f, 1f) : ChipCream;
    }

    static Color AlertColor(float fill)
    {
        if (fill >= 1f)
            return AlertFail;
        if (fill >= 0.67f)
            return AlertDanger;
        if (fill >= 0.34f)
            return AlertCaution;
        return AlertSafe;
    }

    IEnumerator LerpFill(Image image, float target, Color color)
    {
        float start = image.fillAmount;
        float t = 0f;
        while (t < 0.28f)
        {
            t += Time.unscaledDeltaTime;
            image.fillAmount = Mathf.Lerp(start, target, Mathf.Clamp01(t / 0.28f));
            image.color = color;
            yield return null;
        }

        image.fillAmount = target;
        image.color = color;
    }

    void Punch(Component target)
    {
        if (target == null || !isActiveAndEnabled)
            return;
        StartCoroutine(PunchRoutine(target.transform));
    }

    IEnumerator PunchRoutine(Transform target)
    {
        Vector3 baseScale = Vector3.one;
        float t = 0f;
        while (t < 0.22f)
        {
            t += Time.unscaledDeltaTime;
            float u = Mathf.Clamp01(t / 0.22f);
            float s = 1f + 0.16f * Mathf.Sin(u * Mathf.PI);
            target.localScale = baseScale * s;
            yield return null;
        }

        target.localScale = baseScale;
    }

    Sprite IconForChoice(bool isOption, string id)
    {
        if (isOption)
            return eventIcon;
        if (id == "ACT_013")
            return escapeIcon;
        ActionRow action;
        if (database == null || !database.TryGetAction(id, out action))
            return moveIcon;
        if (action.PhaseID == "INFILTRATE")
            return infiltrateIcon;
        if (action.PhaseID == "LOOT")
            return lootIcon;
        if (action.PhaseID == "ESCAPE")
            return escapeIcon;
        return moveIcon;
    }

    void EnsureHudChrome()
    {
        hourglassSprite = LoadUiSprite(hourglassSprite, "CT_Hud_Hourglass_v001");
        alertSprite = LoadUiSprite(alertSprite, "CT_Hud_Alert_v001");
        candySprite = LoadUiSprite(candySprite, "CT_Hud_Candy_v001");
        moveIcon = LoadUiSprite(moveIcon, "CT_Icon_Move_v001");
        infiltrateIcon = LoadUiSprite(infiltrateIcon, "CT_Icon_Infiltrate_v001");
        lootIcon = LoadUiSprite(lootIcon, "CT_Hud_Candy_v001");
        escapeIcon = LoadUiSprite(escapeIcon, "CT_Icon_Escape_v001");
        eventIcon = LoadUiSprite(eventIcon, "CT_Icon_Event_v001");

        Transform top = transform.Find("TopBar");
        if (top == null)
            return;

        HideLegacyTopLabel("Turns");
        HideLegacyTopLabel("Alert");
        HideLegacyTopLabel("Candy");

        if (turnChip == null)
            turnChip = CreateStatChip(top, "TurnChip", hourglassSprite, "남은 턴", ref turnsText, ref turnIcon);
        if (alertChip == null)
            BuildAlertChip(top);
        if (candyChip == null)
            candyChip = CreateStatChip(top, "CandyChip", candySprite, "사탕", ref candyText, ref candyIcon);

        if (turnIcon != null)
            turnIcon.sprite = hourglassSprite;
        if (alertIcon != null)
            alertIcon.sprite = alertSprite;
        if (candyIcon != null)
            candyIcon.sprite = candySprite;

        Sprite fillSprite = LoadUiSprite(null, "CT_Hud_Fill_v001");
        if (fillSprite != null)
        {
            if (alertTrack != null)
                alertTrack.sprite = fillSprite;
            if (alertFill != null)
            {
                alertFill.sprite = fillSprite;
                alertFill.type = Image.Type.Filled;
                alertFill.fillMethod = Image.FillMethod.Horizontal;
            }
        }
    }

    void HideLegacyTopLabel(string name)
    {
        Transform child = transform.Find("TopBar/" + name);
        if (child != null && child.GetComponent<Image>() == null)
        {
            child.gameObject.SetActive(false);
            TextMeshProUGUI label = child.GetComponent<TextMeshProUGUI>();
            if (label != null)
                label.text = string.Empty;
        }
    }

    Image CreateStatChip(Transform top, string name, Sprite icon, string caption, ref TextMeshProUGUI valueText, ref Image iconImage)
    {
        RectTransform chip = CreateChipRoot(top, name);
        iconImage = CreateHudIcon(chip, name + "Icon", icon);
        RectTransform column = CreateChipColumn(chip, name + "Column");
        CreateCaption(column, caption);
        if (valueText == null || valueText.transform.parent != column)
            valueText = CreateValue(column, name + "Value", "0", 48f);
        return chip.GetComponent<Image>();
    }

    void BuildAlertChip(Transform top)
    {
        RectTransform chip = CreateChipRoot(top, "AlertChip");
        alertChip = chip.GetComponent<Image>();
        alertIcon = CreateHudIcon(chip, "AlertIcon", alertSprite);
        RectTransform column = CreateChipColumn(chip, "AlertColumn");
        CreateCaption(column, "위험도");
        Transform existingTrack = column.Find("AlertTrack");
        RectTransform track;
        if (existingTrack != null)
            track = (RectTransform)existingTrack;
        else
        {
            track = V02Ui.CreatePanel("AlertTrack", column, new Color(0.90f, 0.84f, 0.76f, 1f));
            LayoutElement trackLayout = track.gameObject.AddComponent<LayoutElement>();
            trackLayout.minHeight = 18f;
            trackLayout.preferredHeight = 20f;
            trackLayout.flexibleWidth = 1f;
        }

        alertTrack = track.GetComponent<Image>();
        Transform existingFill = track.Find("AlertFill");
        RectTransform fill;
        if (existingFill != null)
            fill = (RectTransform)existingFill;
        else
            fill = V02Ui.CreateStretch("AlertFill", track);
        alertFill = fill.GetComponent<Image>();
        if (alertFill == null)
            alertFill = fill.gameObject.AddComponent<Image>();
        Sprite fillSprite = LoadUiSprite(null, "CT_Hud_Fill_v001");
        if (fillSprite != null)
        {
            alertTrack.sprite = fillSprite;
            alertFill.sprite = fillSprite;
        }

        alertFill.color = AlertSafe;
        alertFill.type = Image.Type.Filled;
        alertFill.fillMethod = Image.FillMethod.Horizontal;
        alertFill.fillOrigin = (int)Image.OriginHorizontal.Left;
        alertFill.fillAmount = 0f;
        alertFill.raycastTarget = false;
        if (alertText == null || alertText.transform.parent != column)
            alertText = CreateValue(column, "AlertValue", "0", 28f);
    }

    static RectTransform CreateChipRoot(Transform top, string name)
    {
        Transform existing = top.Find(name);
        if (existing != null)
            return (RectTransform)existing;

        RectTransform chip = V02Ui.CreatePanel(name, top, ChipCream);
        LayoutElement layout = chip.gameObject.AddComponent<LayoutElement>();
        layout.minHeight = 96f;
        layout.flexibleWidth = 1f;
        HorizontalLayoutGroup row = chip.gameObject.AddComponent<HorizontalLayoutGroup>();
        row.padding = new RectOffset(12, 14, 10, 10);
        row.spacing = 8f;
        row.childAlignment = TextAnchor.MiddleLeft;
        row.childControlWidth = false;
        row.childControlHeight = true;
        row.childForceExpandWidth = false;
        row.childForceExpandHeight = true;
        return chip;
    }

    static Image CreateHudIcon(Transform parent, string name, Sprite sprite)
    {
        Transform existing = parent.Find(name);
        Image image;
        if (existing != null)
        {
            image = existing.GetComponent<Image>();
        }
        else
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(LayoutElement));
            go.transform.SetParent(parent, false);
            image = go.GetComponent<Image>();
            LayoutElement layout = go.GetComponent<LayoutElement>();
            layout.minWidth = 56f;
            layout.minHeight = 56f;
            layout.preferredWidth = 56f;
            layout.preferredHeight = 56f;
        }

        image.sprite = sprite;
        image.preserveAspect = true;
        image.raycastTarget = false;
        image.color = Color.white;
        return image;
    }

    static RectTransform CreateChipColumn(Transform parent, string name)
    {
        Transform existing = parent.Find(name);
        if (existing != null)
            return (RectTransform)existing;

        var go = new GameObject(name, typeof(RectTransform), typeof(LayoutElement), typeof(VerticalLayoutGroup));
        go.transform.SetParent(parent, false);
        LayoutElement layout = go.GetComponent<LayoutElement>();
        layout.flexibleWidth = 1f;
        layout.minHeight = 64f;
        VerticalLayoutGroup vertical = go.GetComponent<VerticalLayoutGroup>();
        vertical.spacing = 2f;
        vertical.childAlignment = TextAnchor.MiddleLeft;
        vertical.childControlWidth = true;
        vertical.childControlHeight = true;
        vertical.childForceExpandWidth = true;
        vertical.childForceExpandHeight = false;
        return (RectTransform)go.transform;
    }

    static TextMeshProUGUI CreateCaption(Transform parent, string caption)
    {
        string name = caption + "Caption";
        Transform existing = parent.Find(name);
        if (existing != null)
            return existing.GetComponent<TextMeshProUGUI>();

        TextMeshProUGUI label = V02Ui.CreateText(name, parent, caption, 20f, V02Ui.Ink, TextAlignmentOptions.Left);
        LayoutElement layout = label.gameObject.AddComponent<LayoutElement>();
        layout.minHeight = 22f;
        layout.preferredHeight = 22f;
        return label;
    }

    static TextMeshProUGUI CreateValue(Transform parent, string name, string value, float size)
    {
        Transform existing = parent.Find(name);
        if (existing != null)
            return existing.GetComponent<TextMeshProUGUI>();

        TextMeshProUGUI label = V02Ui.CreateText(name, parent, value, size, V02Ui.Ink, TextAlignmentOptions.Left);
        LayoutElement layout = label.gameObject.AddComponent<LayoutElement>();
        layout.minHeight = size;
        layout.preferredHeight = size + 4f;
        return label;
    }

    public void EnsureRuntimeChrome()
    {
        EnsureHudChrome();
    }

    void OnResultContinued()
    {
        SetChoicesLocked(false);
        Refresh();
    }

    void EnsurePresentation()
    {
        if (choiceScroll == null)
            choiceScroll = GetComponentInChildren<ScrollRect>(true);
        if (choiceCanvasGroup == null && choiceScroll != null)
        {
            choiceCanvasGroup = choiceScroll.GetComponent<CanvasGroup>();
            if (choiceCanvasGroup == null)
                choiceCanvasGroup = choiceScroll.gameObject.AddComponent<CanvasGroup>();
        }

        if (resultPresentation == null)
            resultPresentation = GetComponentInChildren<V02ResultPresentation>(true);
        if (resultPresentation == null)
            resultPresentation = V02ResultPresentation.Ensure(Rect);
        resultPresentation.BindSprites(
            new[] { LoadFeedbackSprite("CT_Feedback_Success_Jump_v001"), LoadFeedbackSprite("CT_Feedback_Success_Cheer_v001") },
            new[] { LoadFeedbackSprite("CT_Feedback_Fail_Flinch_v001"), LoadFeedbackSprite("CT_Feedback_Fail_Freeze_v001") },
            LoadFeedbackSprite("CT_Feedback_Sparkle_v001"),
            LoadFeedbackSprite("CT_Feedback_Candy_v001"),
            LoadFeedbackSprite("CT_Feedback_Bang_v001"),
            LoadFeedbackSprite("CT_Feedback_Sweat_v001"),
            LoadFeedbackSprite("CT_Feedback_Captured_v001"));
        resultPresentation.Continued.RemoveListener(OnResultContinued);
        resultPresentation.Continued.AddListener(OnResultContinued);
    }

    void SetChoicesLocked(bool locked)
    {
        if (choiceCanvasGroup != null)
        {
            choiceCanvasGroup.interactable = !locked;
            choiceCanvasGroup.blocksRaycasts = !locked;
        }
    }

    void ResetChoiceScroll()
    {
        if (choiceList != null)
        {
            RectTransform listRect = (RectTransform)choiceList;
            listRect.pivot = new Vector2(0.5f, 1f);
            listRect.anchorMin = new Vector2(0f, 1f);
            listRect.anchorMax = new Vector2(1f, 1f);
            listRect.anchoredPosition = Vector2.zero;
        }

        Canvas.ForceUpdateCanvases();
        if (choiceScroll != null)
            choiceScroll.verticalNormalizedPosition = 1f;
    }

    void ClearResult()
    {
        if (resultRoot != null)
            resultRoot.SetActive(false);
        SetText(resultStatusText, string.Empty);
        SetText(resultMessageText, string.Empty);
        SetText(resultDeltaText, string.Empty);
    }

    string FormatTurnShortfall(int turnCost)
    {
        return "턴 부족 · 필요 " + turnCost + " / 남은 " + run.RemainingTurns;
    }

    static string FormatEffectLine(string prefix, int candy, int alert, int turn)
    {
        string body = FormatDeltas(candy, alert, turn);
        if (string.IsNullOrEmpty(body))
            return string.Empty;
        return prefix + body;
    }

    static string FormatDeltas(int candy, int alert, int turn)
    {
        var parts = new List<string>();
        if (candy != 0)
            parts.Add("사탕 " + Signed(candy));
        if (alert != 0)
            parts.Add("위험도 " + Signed(alert));
        if (turn != 0)
            parts.Add("턴 " + Signed(turn));
        if (parts.Count == 0)
            return string.Empty;
        return string.Join(" · ", parts.ToArray());
    }

    static string Signed(int value)
    {
        return value > 0 ? "+" + value : value.ToString();
    }

    V02ChoiceButton GetButton(int index)
    {
        while (pooledButtons.Count <= index)
        {
            if (choiceButtonPrefab == null)
            {
                Debug.LogError(LogPrefix + "ChoiceButton 프리팹이 없습니다.");
                var fallback = new GameObject("ChoiceButton", typeof(RectTransform));
                fallback.transform.SetParent(choiceList, false);
                pooledButtons.Add(fallback.AddComponent<V02ChoiceButton>());
                continue;
            }

            V02ChoiceButton created = Instantiate(choiceButtonPrefab, choiceList);
            pooledButtons.Add(created);
        }

        return pooledButtons[index];
    }

    void HidePooled()
    {
        for (int i = 0; i < pooledButtons.Count; i++)
        {
            if (pooledButtons[i] != null)
                pooledButtons[i].gameObject.SetActive(false);
        }
    }

    static void SetText(TextMeshProUGUI label, string value)
    {
        if (label != null)
            label.text = value ?? string.Empty;
    }

    static Sprite LoadUiSprite(Sprite assigned, string name)
    {
        if (assigned != null)
            return assigned;
        Sprite fromResources = Resources.Load<Sprite>("V02/UI/" + name);
        if (fromResources != null)
            return fromResources;
        return LoadSprite("Assets/Art/V02/UI/" + name + ".png");
    }

    static Sprite LoadFeedbackSprite(string name)
    {
        Sprite fromResources = Resources.Load<Sprite>("V02/Feedback/" + name);
        if (fromResources != null)
            return fromResources;
        return LoadSprite("Assets/Art/V02/Feedback/" + name + ".png");
    }

    static Sprite LoadSprite(string assetPath)
    {
        if (string.IsNullOrEmpty(assetPath))
            return null;

#if UNITY_EDITOR
        return UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
#else
        return null;
#endif
    }
}
