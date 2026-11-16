using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public sealed class V02RunHud : MonoBehaviour
{
    public const string ResourcePath = "V02/UI/RunHud";
    // PLAY-UI-MOCK-v001 기준 프리팹 배치 버전. V02RunHudPrefabBuilder 가 프리팹에 기록한다.
    public const int CurrentLayout = 1;
    const string LogPrefix = "[V02RunHud] ";

    [SerializeField] int layoutVersion;
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
    [SerializeField] Button backButton;
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
    [SerializeField] Sprite turnCostSprite;
    [SerializeField] Sprite alertSprite;
    [SerializeField] Sprite candySprite;
    [SerializeField] Sprite moveIcon;
    [SerializeField] Sprite infiltrateIcon;
    [SerializeField] Sprite lootIcon;
    [SerializeField] Sprite escapeIcon;
    [SerializeField] Sprite eventIcon;
    [SerializeField] TextMeshProUGUI phaseText;
    [SerializeField] GameObject timerRoot;
    [SerializeField] Image timerFill;
    [SerializeField] TextMeshProUGUI timerSecondsText;

    const int LowTurnWarning = 3;
    // 칩 면 색 (PLAY-UI-MOCK-v001). 글자는 항상 흰색 + 어두운 외곽선이다.
    static readonly Color TurnChipNormal = new Color(0.306f, 0.792f, 0.541f, 1f);
    static readonly Color TurnChipWarn = new Color(0.93f, 0.45f, 0.20f, 1f);
    static readonly Color AlertChipNormal = new Color(0.267f, 0.165f, 0.118f, 1f);
    static readonly Color AlertChipFull = new Color(0.50f, 0.12f, 0.14f, 1f);
    static readonly Color AlertSafe = new Color(0.42f, 0.78f, 0.45f, 1f);
    static readonly Color AlertCaution = new Color(0.96f, 0.72f, 0.28f, 1f);
    static readonly Color AlertDanger = new Color(0.86f, 0.32f, 0.30f, 1f);
    static readonly Color AlertFail = new Color(0.72f, 0.12f, 0.18f, 1f);
    static readonly Color EscapeNormal = new Color(0.44f, 0.27f, 0.18f, 1f);
    static readonly Color EscapeReady = new Color(0.96f, 0.77f, 0.26f, 1f);
    static readonly Color EscapeOff = new Color(0.78f, 0.74f, 0.68f, 1f);

    readonly List<PresentedAction> presentedActions = new List<PresentedAction>();
    readonly List<PresentedOption> presentedOptions = new List<PresentedOption>();
    readonly List<V02ChoiceButton> pooledButtons = new List<V02ChoiceButton>();

    CsvDatabase database;
    RunState run;
    ActionResolver actionResolver;
    ActionHandDrawer actionHand;
    IIntRoll actionHandRoll;
    IIntRoll timeoutRoll;
    EventResolver eventResolver;
    V02RunResultPopup operationResult;
    System.Action operationRetryRequested;
    System.Action operationLobbyRequested;
    ShopRow shop;
    UnityAction leaveToLobby;
    UnityAction abandonConfirmed;
    V02AbandonConfirm abandonConfirm;
    V02EscapePrepNotice escapeNotice;
    V02EmptyHandEscapeConfirm emptyHandConfirm;
    bool abandonBusy;
    bool handReady;
    string handPhase;
    int handEpoch = -1;
    string handA;
    string handB;
    float timerRemaining;
    float timerAllowance;
    bool timerRunning;
    bool timerExpired;
    int timerExpiryRecords;
    int timerEmptyRecords;
    bool roundClaimed;
    string timerKey = string.Empty;

    public RectTransform Rect => (RectTransform)transform;
    public int LayoutVersion => layoutVersion;
    public V02ResultPresentation ResultPresentation => resultPresentation;
    public V02RunResultPopup OperationResult => operationResult;
    public bool IsPresenting { get { return resultPresentation != null && resultPresentation.IsPresenting; } }
    public int DisplayedTurns { get; private set; }
    public int DisplayedAlert { get; private set; }
    public int DisplayedCandy { get; private set; }
    public float AlertFillAmount { get { return alertFill != null ? alertFill.fillAmount : 0f; } }
    public string PhaseLabel { get { return phaseText != null ? phaseText.text : string.Empty; } }
    public string AlertLabel { get { return alertText != null ? alertText.text : string.Empty; } }
    public string NarrativeText { get { return bodyText != null ? bodyText.text : string.Empty; } }
    public bool TimerSlotVisible { get { return timerRoot != null && timerRoot.activeSelf; } }
    public string TimerSecondsLabel { get { return timerSecondsText != null ? timerSecondsText.text : string.Empty; } }
    public float TimerSlotFillAmount { get { return timerFill != null ? timerFill.fillAmount : 0f; } }
    public float TimerRemaining { get { return timerRemaining; } }
    public bool TimerCountdownRunning { get { return timerRunning; } }
    public bool TimerExpired { get { return timerExpired; } }
    public int TimerExpiryRecords { get { return timerExpiryRecords; } }
    public int TimerEmptyRecords { get { return timerEmptyRecords; } }

    public static string FormatTimerSeconds(float seconds)
    {
        int shown = seconds <= 0f ? 0 : Mathf.CeilToInt(seconds);
        return shown.ToString() + "초";
    }
    public bool ShowingEventOptions { get; private set; }
    public bool ChoicesLocked { get { return choiceCanvasGroup != null && !choiceCanvasGroup.interactable; } }
    public Button LobbyButton { get { return backButton != null ? backButton : lobbyButton; } }
    public Button BackButton { get { return backButton != null ? backButton : lobbyButton; } }
    public Button EscapePrepButton { get { return lobbyButton; } }
    public V02AbandonConfirm AbandonConfirm { get { return abandonConfirm; } }
    public bool AbandonConfirmOpen { get { return abandonConfirm != null && abandonConfirm.IsOpen; } }
    public V02EscapePrepNotice EscapePrepNotice { get { return escapeNotice; } }
    public bool EscapePrepNoticeOpen { get { return escapeNotice != null && escapeNotice.IsOpen; } }
    public V02EmptyHandEscapeConfirm EmptyHandConfirm { get { return emptyHandConfirm; } }
    public bool EmptyHandConfirmOpen { get { return emptyHandConfirm != null && emptyHandConfirm.IsOpen; } }
    public string IllustrationKey { get; private set; }
    public Sprite IllustrationSprite { get { return illustration != null ? illustration.sprite : null; } }
    public bool IllustrationIsPlaceholder { get { return illustration != null && illustration.sprite == placeholderSprite; } }

    // 이벤트 스프라이트가 있으면 그걸 쓴다. 없으면 가게 배경. 둘 다 없으면 null 이고 호출부가 플레이스홀더를 켠다.
    public static Sprite PickIllustration(Sprite eventSprite, Sprite shopSprite)
    {
        return eventSprite != null ? eventSprite : shopSprite;
    }

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
        Bind(csv, runState, onLobby, null, null, null);
    }

    public void Bind(CsvDatabase csv, RunState runState, UnityEngine.Events.UnityAction onLobby, IChanceRoll chanceRoll, IEventPick eventPick)
    {
        Bind(csv, runState, onLobby, chanceRoll, eventPick, null);
    }

    public void Bind(
        CsvDatabase csv,
        RunState runState,
        UnityEngine.Events.UnityAction onLobby,
        IChanceRoll chanceRoll,
        IEventPick eventPick,
        UnityEngine.Events.UnityAction onAbandon)
    {
        Bind(csv, runState, onLobby, chanceRoll, eventPick, onAbandon, null);
    }

    public void Bind(
        CsvDatabase csv,
        RunState runState,
        UnityEngine.Events.UnityAction onLobby,
        IChanceRoll chanceRoll,
        IEventPick eventPick,
        UnityEngine.Events.UnityAction onAbandon,
        IIntRoll handRoll)
    {
        Bind(csv, runState, onLobby, chanceRoll, eventPick, onAbandon, handRoll, null);
    }

    public void Bind(
        CsvDatabase csv,
        RunState runState,
        UnityEngine.Events.UnityAction onLobby,
        IChanceRoll chanceRoll,
        IEventPick eventPick,
        UnityEngine.Events.UnityAction onAbandon,
        IIntRoll handRoll,
        IIntRoll timeoutPick)
    {
        leaveToLobby = onLobby;
        abandonConfirmed = onAbandon;
        abandonBusy = false;
        handReady = false;
        handPhase = null;
        handEpoch = -1;
        handA = null;
        handB = null;
        timerKey = string.Empty;
        timerRemaining = 0f;
        timerAllowance = 0f;
        timerRunning = false;
        timerExpired = false;
        timerExpiryRecords = 0;
        timerEmptyRecords = 0;
        roundClaimed = false;
        timeoutRoll = timeoutPick ?? new UnityIntRoll();
        database = csv;
        run = runState;
        actionHandRoll = handRoll ?? new UnityIntRoll();
        actionResolver = chanceRoll == null ? new ActionResolver(database) : new ActionResolver(database, chanceRoll);
        actionHand = new ActionHandDrawer(database, actionResolver, actionHandRoll);
        eventResolver = eventPick == null && chanceRoll == null
            ? new EventResolver(database)
            : new EventResolver(database, chanceRoll, eventPick);
        shop = default(ShopRow);
        if (database != null)
            database.TryGetShop(run.ShopID, out shop);
        if (run.Shop != null)
            shop = run.Shop;

        if (abandonConfirm != null && abandonConfirm.IsOpen)
            abandonConfirm.Close();

        if (backButton != null)
        {
            backButton.onClick.RemoveAllListeners();
            backButton.onClick.AddListener(OnBackClicked);
        }

        if (lobbyButton != null)
        {
            lobbyButton.onClick.RemoveAllListeners();
            lobbyButton.onClick.AddListener(OnEscapePrepClicked);
        }

        EnsureHudChrome();
        EnsurePresentation();
        ClearResult();
        SetChoicesLocked(false);
        Refresh();
    }

    void OnEscapePrepClicked()
    {
        if (run == null || run.IsEnded || IsPresenting || run.HasPendingEvent)
            return;
        if (abandonConfirm != null && abandonConfirm.IsOpen)
            return;

        string phase = run.CurrentPhase ?? string.Empty;
        if (phase == "ESCAPE")
            return;
        if (phase == "MOVE" || phase == "INFILTRATE")
        {
            escapeNotice = V02EscapePrepNotice.Ensure(Rect);
            SetChoicesLocked(true);
            escapeNotice.Open(CloseEscapePrepNotice);
            ApplyTimerSlot();
            return;
        }

        if (phase != "LOOT")
            return;
        if (emptyHandConfirm != null && emptyHandConfirm.IsOpen)
            return;

        if (run.Candy <= 0)
        {
            emptyHandConfirm = V02EmptyHandEscapeConfirm.Ensure(Rect);
            SetChoicesLocked(true);
            emptyHandConfirm.Open(CloseEmptyHandConfirm, ConfirmEmptyHandEscape);
            ApplyTimerSlot();
            return;
        }

        CommitEscapePrep();
    }

    void ConfirmEmptyHandEscape()
    {
        CommitEscapePrep();
    }

    void CloseEmptyHandConfirm()
    {
        if (emptyHandConfirm != null)
            emptyHandConfirm.Close();
        SetChoicesLocked(IsPresenting || AbandonConfirmOpen);
        ApplyTimerSlot();
    }

    void CommitEscapePrep()
    {
        if (run == null || !run.TryPrepareEscape())
            return;

        if (emptyHandConfirm != null)
            emptyHandConfirm.Close();
        InvalidateActionHand();
        SetChoicesLocked(false);
        Refresh();
    }

    void InvalidateActionHand()
    {
        handReady = false;
        handPhase = null;
        handEpoch = -1;
        handA = null;
        handB = null;
    }

    void CloseEscapePrepNotice()
    {
        if (escapeNotice != null)
            escapeNotice.Close();
        SetChoicesLocked(IsPresenting || AbandonConfirmOpen);
        ApplyTimerSlot();
    }

    void ApplyEscapePrepState()
    {
        if (lobbyButton == null || run == null)
            return;

        string phase = run.CurrentPhase ?? string.Empty;
        bool blocked = run.IsEnded || IsPresenting || run.HasPendingEvent || phase == "ESCAPE";
        lobbyButton.interactable = !blocked;
        Image plate = lobbyButton.targetGraphic as Image;
        if (plate == null)
            plate = lobbyButton.GetComponent<Image>();
        if (plate != null)
        {
            if (phase == "LOOT" && !blocked)
                plate.color = EscapeReady;
            else if (blocked)
                plate.color = EscapeOff;
            else
                plate.color = EscapeNormal;
        }
    }

    void OnBackClicked()
    {
        if (escapeNotice != null && escapeNotice.IsOpen)
            CloseEscapePrepNotice();
        if (run != null && !run.IsEnded)
        {
            ShowAbandonConfirm();
            return;
        }

        if (leaveToLobby != null)
            leaveToLobby.Invoke();
    }

    void ShowAbandonConfirm()
    {
        if (escapeNotice != null && escapeNotice.IsOpen)
            escapeNotice.Close();
        if (abandonConfirm != null && abandonConfirm.IsOpen)
            return;

        abandonConfirm = V02AbandonConfirm.Ensure(Rect);
        SetChoicesLocked(true);
        abandonConfirm.Open(OnAbandonCancel, OnAbandonPressed);
        ApplyTimerSlot();
    }

    void OnAbandonCancel()
    {
        if (abandonBusy)
            return;
        CloseAbandonConfirm();
    }

    void OnAbandonPressed()
    {
        if (abandonBusy || abandonConfirm == null || !abandonConfirm.IsOpen)
            return;
        if (run == null || run.IsEnded)
            return;

        abandonBusy = true;
        abandonConfirm.SetInteractable(false);
        if (abandonConfirmed != null)
            abandonConfirmed.Invoke();
    }

    public void CloseAbandonConfirm()
    {
        abandonBusy = false;
        if (abandonConfirm != null)
            abandonConfirm.Close();
        SetChoicesLocked(IsPresenting);
        ApplyTimerSlot();
    }

    public void AbandonAttemptFailed()
    {
        abandonBusy = false;
        if (abandonConfirm != null)
            abandonConfirm.SetInteractable(true);
    }

    public void SetOperationActions(System.Action onRetry, System.Action onLobby)
    {
        operationRetryRequested = onRetry;
        operationLobbyRequested = onLobby;
    }

    public void Refresh()
    {
        if (run == null)
            return;

        if (choiceScroll != null && !run.IsEnded)
            choiceScroll.gameObject.SetActive(true);
        if (operationResult != null && !run.IsEnded)
            operationResult.Hide();
        ApplyStats(run.RemainingTurns, run.Alert, run.Candy, false);
        BindCenter();
        BindChoices();
        ApplyTimerSlot();
        ApplyEscapePrepState();
        ResetChoiceScroll();
    }

    void BindCenter()
    {
        BindPhase();
        SetText(titleText, string.Empty);
        if (run.HasPendingEvent)
        {
            EventRow evt;
            if (database != null && database.TryGetEvent(run.PendingEventID, out evt))
            {
                SetText(bodyText, evt.EventDescription);
                BindIllustration(evt.ImageKey);
                return;
            }
        }

        PhaseRow phase;
        string narrative = string.Empty;
        if (database != null && database.TryGetPhase(run.CurrentPhase, out phase))
            narrative = phase.SituationText ?? string.Empty;
        SetText(bodyText, narrative);
        if (bodyText != null)
            bodyText.alignment = TextAlignmentOptions.Center;
        BindIllustration(string.Empty);
    }

    void BindPhase()
    {
        PhaseRow phase;
        string phaseName = run.CurrentPhase ?? string.Empty;
        if (database != null && database.TryGetPhase(run.CurrentPhase, out phase) && !string.IsNullOrEmpty(phase.PhaseName))
            phaseName = phase.PhaseName;
        SetText(phaseText, phaseName);
    }

    void Update()
    {
        if (!Application.isPlaying)
            return;
        TickActionSelect(Time.unscaledDeltaTime);
    }

    public void TickActionSelect(float deltaSeconds)
    {
        if (deltaSeconds <= 0f || !timerRunning || timerExpired)
            return;

        timerRemaining -= deltaSeconds;
        if (timerRemaining > 0f)
        {
            PaintTimer();
            return;
        }

        timerRemaining = 0f;
        timerRunning = false;
        if (!timerExpired)
        {
            timerExpired = true;
            timerExpiryRecords++;
            PaintTimer();
            ResolveTimeout();
        }
    }

    void ApplyTimerSlot()
    {
        BeginActionSelectRoundIfNeeded();
        bool show = ShowsActionSelectTimer();
        if (timerRoot != null && timerRoot.activeSelf != show)
            timerRoot.SetActive(show);

        timerRunning = CountsActionSelectTimer();
        PaintTimer();
    }

    bool ShowsActionSelectTimer()
    {
        return run != null
            && !run.IsEnded
            && !IsPresenting
            && (operationResult == null || !operationResult.IsOpen);
    }

    bool CountsActionSelectTimer()
    {
        return ShowsActionSelectTimer()
            && !timerExpired
            && !roundClaimed
            && !AbandonConfirmOpen
            && !EscapePrepNoticeOpen
            && !EmptyHandConfirmOpen;
    }

    void BeginActionSelectRoundIfNeeded()
    {
        if (run == null || run.IsEnded || IsPresenting)
            return;
        if (operationResult != null && operationResult.IsOpen)
            return;

        string key = run.HasPendingEvent
            ? "E:" + run.PendingEventID
            : "A:" + run.ChoiceEpoch + ":" + (run.CurrentPhase ?? string.Empty);
        if (timerKey == key)
            return;

        timerAllowance = database != null ? Mathf.Max(0, database.GetInt("ACTION_SELECT_TIME_SEC")) : 0;
        timerRemaining = timerAllowance;
        timerExpired = false;
        timerExpiryRecords = 0;
        timerEmptyRecords = 0;
        roundClaimed = false;
        timerKey = key;
    }

    void ResolveTimeout()
    {
        if (!TryBeginChoice())
            return;

        HideChoiceTooltips();
        TimeoutCandidate picked;
        if (!TimeoutChoice.TryPick(BuildTimeoutCandidates(), timeoutRoll, out picked))
        {
            if (timerEmptyRecords == 0)
            {
                timerEmptyRecords = 1;
                string surface = run != null && run.HasPendingEvent ? "EVENT" : "ACTION";
                string pending = run != null ? run.PendingEventID : string.Empty;
                string phase = run != null ? run.CurrentPhase : string.Empty;
                Debug.Log(LogPrefix + "시간 초과 후보가 없습니다. surface=" + surface
                    + " phase=" + phase + " event=" + pending + " executable=0");
            }

            SetChoicesLocked(true);
            return;
        }

        if (!ResolveChoice(picked.Id, picked.IsOption))
            SetChoicesLocked(true);
    }

    List<TimeoutCandidate> BuildTimeoutCandidates()
    {
        var candidates = new List<TimeoutCandidate>();
        if (run != null && run.HasPendingEvent)
        {
            for (int i = 0; i < presentedOptions.Count; i++)
            {
                PresentedOption option = presentedOptions[i];
                if (option.Option == null)
                    continue;
                candidates.Add(new TimeoutCandidate
                {
                    Id = option.Option.OptionID,
                    IsOption = true,
                    CanExecute = option.CanExecute,
                    SpawnWeight = 0
                });
            }

            return candidates;
        }

        for (int i = 0; i < presentedActions.Count; i++)
        {
            PresentedAction action = presentedActions[i];
            if (action.Action == null)
                continue;
            candidates.Add(new TimeoutCandidate
            {
                Id = action.Action.ActionID,
                IsOption = false,
                CanExecute = action.CanExecute,
                SpawnWeight = action.Action.SpawnWeight
            });
        }

        return candidates;
    }

    V02ForecastTip forecastTip;

    V02ForecastTip ForecastTip()
    {
        if (forecastTip == null)
            forecastTip = GetComponentInChildren<V02ForecastTip>(true);
        return forecastTip;
    }

    void HideChoiceTooltips()
    {
        for (int i = 0; i < pooledButtons.Count; i++)
        {
            if (pooledButtons[i] != null)
                pooledButtons[i].HideTooltip();
        }
    }

    void PaintTimer()
    {
        SetText(timerSecondsText, FormatTimerSeconds(timerRemaining));
        if (timerFill == null)
            return;
        timerFill.fillAmount = timerAllowance <= 0f
            ? 0f
            : Mathf.Clamp01(timerRemaining / timerAllowance);
    }

    void BindIllustration(string eventImageKey)
    {
        Sprite eventSprite = LoadEventIllustration(eventImageKey);
        string shopKey = shop != null && !string.IsNullOrEmpty(shop.BackgroundImageKey) ? shop.BackgroundImageKey : string.Empty;
        Sprite shopSprite = V02ShopSprites.LoadByImageKey(database, shopKey);
        Sprite picked = PickIllustration(eventSprite, shopSprite);
        IllustrationKey = eventSprite != null ? (eventImageKey ?? string.Empty) : shopKey;

        if (illustration == null)
            return;

        Sprite sprite = placeholderSprite;
        Color color = placeholderColor;
        if (picked != null)
        {
            sprite = picked;
            color = Color.white;
        }

        illustration.sprite = sprite;
        illustration.color = color;

        // 프레임을 채우되 그림은 찌그러뜨리지 않는다. AspectRatioFitter(Envelope)가 있으면 비율만 맞춘다.
        AspectRatioFitter fit = illustration.GetComponent<AspectRatioFitter>();
        if (fit == null)
        {
            illustration.preserveAspect = true;
            return;
        }

        illustration.preserveAspect = false;
        RectTransform frame = illustration.transform.parent as RectTransform;
        float ratio = 1f;
        if (picked != null && picked.rect.height > 0f)
            ratio = picked.rect.width / picked.rect.height;
        else if (frame != null && frame.rect.height > 0f)
            ratio = frame.rect.width / frame.rect.height;
        fit.aspectRatio = ratio;
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

        EnsureActionHand();
        presentedActions.Clear();
        AddDealtAction(handA);
        AddDealtAction(handB);
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

    void EnsureActionHand()
    {
        if (handReady && handPhase == run.CurrentPhase && handEpoch == run.ChoiceEpoch)
            return;

        string[] avoid = null;
        if (handReady && !string.IsNullOrEmpty(handA))
            avoid = new[] { handA, handB };
        if (actionHand == null)
            actionHand = new ActionHandDrawer(database, actionResolver, actionHandRoll);
        var drawn = new List<PresentedAction>(ActionHandDrawer.HandSize);
        bool dealt = actionHand.Draw(run, avoid, drawn);
        handPhase = run.CurrentPhase;
        handEpoch = run.ChoiceEpoch;
        handReady = true;
        handA = dealt && drawn.Count == 2 ? drawn[0].Action.ActionID : null;
        handB = dealt && drawn.Count == 2 ? drawn[1].Action.ActionID : null;
    }

    void AddDealtAction(string actionId)
    {
        if (string.IsNullOrEmpty(actionId) || actionResolver == null)
            return;

        ActionRow action;
        bool canExecute;
        ActionRejectReason reason;
        if (!actionResolver.IsActionPresented(run, actionId, out action, out canExecute, out reason))
            return;

        presentedActions.Add(new PresentedAction
        {
            Action = action,
            CanExecute = canExecute,
            ExecuteReason = reason
        });
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
        button.AttachTip(ForecastTip());
        button.Bind(
            id,
            isOption,
            choiceName,
            description,
            chance + "%",
            turnCost + "턴",
            FormatEffectLine("성공 시 ", successCandy, successAlert, successTurn),
            FormatEffectLine("실패 시 ", failCandy, failAlert, failTurn),
            canExecute,
            disabledReason,
            IconForChoice(isOption, id),
            turnCostSprite,
            () => OnChoiceClicked(capturedId, capturedOption));
        button.BindForecast(successCandy, successCandy, successAlert, failAlert, candySprite, alertSprite);
    }

    public bool TryChoose(string id, bool isOption)
    {
        if (run == null || run.IsEnded || IsPresenting || roundClaimed)
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
        if (!TryBeginChoice())
            return;
        if (!ResolveChoice(id, isOption))
        {
            roundClaimed = false;
            SetChoicesLocked(false);
            ApplyTimerSlot();
        }
    }

    bool TryBeginChoice()
    {
        if (run == null || run.IsEnded || IsPresenting || roundClaimed)
            return false;
        roundClaimed = true;
        timerRunning = false;
        SetChoicesLocked(true);
        return true;
    }

    bool ResolveChoice(string id, bool isOption)
    {
        int candyBefore = run.Candy;
        int alertBefore = run.Alert;
        int turnsBefore = run.RemainingTurns;

        if (isOption)
        {
            EventOptionRow option;
            EventOptionResolveResult result;
            if (!database.TryGetEventOption(id, out option))
                return false;

            if (!eventResolver.TryResolveOption(run, id, out result) || !result.Applied)
                return false;

            PresentResolve(
                "OPTION",
                option.OptionID,
                option.OptionName,
                result.Success,
                run.Candy - candyBefore,
                run.Alert - alertBefore,
                run.RemainingTurns - turnsBefore);
            RefreshStats(true);
            return true;
        }

        ActionRow action;
        if (!database.TryGetAction(id, out action))
            return false;

        string phaseBefore = run.CurrentPhase;
        ActionResolveResult resolved;
        if (!actionResolver.TryResolve(run, id, out resolved) || !resolved.Applied)
            return false;

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
        return true;
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
        Sprite dedicated = null;
        if (database != null && database.TryGetResultFeedback(sourceType, sourceId, out feedback))
        {
            body = success ? feedback.SuccessText : feedback.FailText;
            presentationId = success ? feedback.SuccessPresentationID : feedback.FailPresentationID;
            FeedbackImageResolver.TryResolve(database, feedback, success, out dedicated);
        }

        presented.DedicatedReaction = dedicated;
        if (!success && presented.AlertDelta > 0 && presentationId == V02ResultPresentation.FailDefault)
            presentationId = V02ResultPresentation.FailAlert;
        if (!string.IsNullOrEmpty(displayName))
            body = displayName + "\n" + body;

        EnsurePresentation();
        if (resultRoot != null)
            resultRoot.SetActive(false);
        resultPresentation.Present(presented, body, presentationId);
        ApplyTimerSlot();
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
        SetText(alertText, alert.ToString() + "/" + AlertMax().ToString());
        if (candyText != null)
        {
            candyText.text = candy.ToString("N0", System.Globalization.CultureInfo.InvariantCulture);
            candyText.textWrappingMode = TextWrappingModes.NoWrap;
            candyText.overflowMode = TextOverflowModes.Overflow;
        }
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
        if (turnChip != null)
            turnChip.color = turns <= LowTurnWarning ? TurnChipWarn : TurnChipNormal;
    }

    int AlertMax()
    {
        int maxAlert = database != null ? database.GetInt("MAX_ALERT") : 100;
        return Mathf.Max(1, maxAlert);
    }

    void ApplyAlertGauge(int alert, bool animate)
    {
        int maxAlert = AlertMax();
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
            alertChip.color = fill >= 1f ? AlertChipFull : AlertChipNormal;
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

    // 칩·게이지·아이콘 오브젝트는 프리팹에 이미 있다(D-019). 여기서는 스프라이트만 채운다. 오브젝트를 만들거나 RectTransform 을 바꾸지 않는다.
    void EnsureHudChrome()
    {
        hourglassSprite = LoadUiSprite(hourglassSprite, "CT_Hud_Hourglass_v003");
        turnCostSprite = LoadUiSprite(turnCostSprite, "CT_Icon_TurnCost_v006");
        alertSprite = LoadUiSprite(alertSprite, "CT_Icon_Alert_v005");
        candySprite = LoadUiSprite(candySprite, "CT_Icon_Candy_v005");
        moveIcon = LoadUiSprite(moveIcon, "CT_Icon_Move_v001");
        infiltrateIcon = LoadUiSprite(infiltrateIcon, "CT_Icon_Infiltrate_v001");
        lootIcon = LoadUiSprite(lootIcon, "CT_Icon_Candy_v005");
        escapeIcon = LoadUiSprite(escapeIcon, "CT_Icon_Exit_v005");
        eventIcon = LoadUiSprite(eventIcon, "CT_Icon_Event_v001");

        if (turnIcon != null)
            turnIcon.sprite = hourglassSprite;
        if (alertIcon != null)
            alertIcon.sprite = alertSprite;
        if (candyIcon != null)
            candyIcon.sprite = candySprite;

        Sprite fillSprite = LoadUiSprite(null, "CT_Hud_Fill_v001");
        if (fillSprite != null)
        {
            if (alertTrack != null && alertTrack.sprite == null)
                alertTrack.sprite = fillSprite;
            if (alertFill != null)
            {
                alertFill.sprite = fillSprite;
                alertFill.type = Image.Type.Filled;
                alertFill.fillMethod = Image.FillMethod.Horizontal;
            }
        }
    }

    public void EnsureRuntimeChrome()
    {
        EnsureHudChrome();
    }

    void OnResultContinued()
    {
        SetChoicesLocked(false);
        if (run != null && run.IsEnded && run.FailReason != RunFailReason.Abandon)
        {
            ShowOperationResult();
            return;
        }

        Refresh();
    }

    void ShowOperationResult()
    {
        HidePooled();
        if (choiceScroll != null)
            choiceScroll.gameObject.SetActive(false);
        EnsureOperationResult();
        if (operationResult == null)
        {
            Debug.LogError(LogPrefix + "작전 결과 프리팹이 없습니다.");
            ApplyTimerSlot();
            return;
        }

        operationResult.Present(run, null);
        operationResult.transform.SetAsLastSibling();
        ApplyTimerSlot();
    }

    void EnsureOperationResult()
    {
        if (operationResult != null)
            return;

        operationResult = GetComponentInChildren<V02RunResultPopup>(true);
        if (operationResult == null)
        {
            V02RunResultPopup prefab = Resources.Load<V02RunResultPopup>(V02RunResultPopup.ResourcePath);
            if (prefab == null)
                return;
            operationResult = Instantiate(prefab, Rect);
            operationResult.name = "RunResultPopup";
            V02Ui.Stretch((RectTransform)operationResult.transform);
        }

        operationResult.RetryClicked.RemoveListener(OnOperationRetry);
        operationResult.RetryClicked.AddListener(OnOperationRetry);
        operationResult.LobbyClicked.RemoveListener(OnOperationLobby);
        operationResult.LobbyClicked.AddListener(OnOperationLobby);
    }

    void OnOperationRetry()
    {
        if (operationRetryRequested != null)
            operationRetryRequested.Invoke();
    }

    void OnOperationLobby()
    {
        if (operationLobbyRequested != null)
            operationLobbyRequested.Invoke();
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
        if (choiceScroll == null)
            return;

        choiceScroll.horizontal = false;
        choiceScroll.vertical = false;
        choiceScroll.horizontalNormalizedPosition = 0f;
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

    // 프리팹 ChoiceList 에 미리 놓인 카드(Edit Mode 배치 확인용)를 풀에 편입한다. 새로 만들지 않고 그대로 재사용한다.
    void AdoptPlacedButtons()
    {
        if (choiceList == null)
            return;
        for (int i = 0; i < choiceList.childCount; i++)
        {
            V02ChoiceButton placed = choiceList.GetChild(i).GetComponent<V02ChoiceButton>();
            if (placed != null && !pooledButtons.Contains(placed))
                pooledButtons.Add(placed);
        }
    }

    V02ChoiceButton GetButton(int index)
    {
        AdoptPlacedButtons();
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
        AdoptPlacedButtons();
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

    Sprite LoadEventIllustration(string imageKey)
    {
        if (database == null || string.IsNullOrEmpty(imageKey))
            return null;

        AssetRow asset;
        if (!database.TryGetAssetByImageKey(imageKey, out asset))
            return null;

        // 가게 경로는 Resources 만 본다. 에디터 AssetDatabase 로 우회하면 빌드와 어긋난다.
        if (!string.IsNullOrEmpty(V02ShopSprites.ResourcesKey(asset.FilePath)))
            return V02ShopSprites.Load(asset.FilePath);

        return LoadSprite(asset.FilePath);
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
