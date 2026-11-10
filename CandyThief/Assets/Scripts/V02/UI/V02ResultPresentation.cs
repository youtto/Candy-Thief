using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public sealed class PresentedResolve
{
    public string SourceType;
    public string SourceId;
    public string DisplayName;
    public bool Success;
    public int CandyDelta;
    public int AlertDelta;
    public int TurnDelta;
    public bool RunEnded;
    public RunStatus Status;
    public RunFailReason FailReason;
}

public sealed class V02ResultPresentation : MonoBehaviour
{
    public const string SuccessDefault = "PRES_SUCCESS_DEFAULT";
    public const string FailDefault = "PRES_FAIL_DEFAULT";
    public const string FailAlert = "PRES_FAIL_ALERT";
    public const string RunFail = "PRES_RUN_FAIL";
    public const string RunWin = "PRES_RUN_WIN";

    public enum Phase
    {
        Idle,
        Anticipating,
        Revealing,
        Holding
    }

    [SerializeField] CanvasGroup canvasGroup;
    [SerializeField] Button skipCatcher;
    [SerializeField] RectTransform panel;
    [SerializeField] Image reactionImage;
    [SerializeField] Image flashImage;
    [SerializeField] TextMeshProUGUI titleText;
    [SerializeField] TextMeshProUGUI bodyText;
    [SerializeField] ScrollRect bodyScroll;
    [SerializeField] TextMeshProUGUI deltaText;
    [SerializeField] Button continueButton;
    [SerializeField] TextMeshProUGUI continueLabel;
    [SerializeField] Transform fxRoot;
    [SerializeField] Sprite[] successFrames;
    [SerializeField] Sprite[] failFrames;
    [SerializeField] Sprite sparkleSprite;
    [SerializeField] Sprite candySprite;
    [SerializeField] Sprite bangSprite;
    [SerializeField] Sprite sweatSprite;
    [SerializeField] Sprite capturedSprite;
    [SerializeField] Vector2 captureReactionMin = new Vector2(0.06f, 0.54f);
    [SerializeField] Vector2 captureReactionMax = new Vector2(0.94f, 0.97f);
    [SerializeField] float anticipateSeconds = 0.4f;
    [SerializeField] float revealSeconds = 0.55f;
    [SerializeField] float bounceScale = 1.18f;

    public Phase CurrentPhase { get; private set; }
    public bool IsPresenting { get { return CurrentPhase != Phase.Idle; } }
    public bool CanSkip { get { return CurrentPhase == Phase.Anticipating || CurrentPhase == Phase.Revealing; } }
    public string TitleLabel { get { return titleText != null ? titleText.text : string.Empty; } }
    public Sprite ReactionSprite { get { return reactionImage != null ? reactionImage.sprite : null; } }

    public UnityEvent Continued = new UnityEvent();
    public UnityEvent EndedRunContinued = new UnityEvent();

    PresentedResolve current;
    string currentBody;
    string currentPresentationId;
    bool endedContinue;
    Coroutine routine;
    bool storedDefaultReaction;
    Vector2 defaultReactionAnchorMin;
    Vector2 defaultReactionAnchorMax;
    Vector2 defaultReactionOffsetMin;
    Vector2 defaultReactionOffsetMax;

    public static V02ResultPresentation Ensure(RectTransform hudRoot)
    {
        V02ResultPresentation existing = hudRoot.GetComponentInChildren<V02ResultPresentation>(true);
        if (existing != null)
            return existing;

        RectTransform overlay = V02Ui.CreatePanel("ResultPresentation", hudRoot, new Color(0.13f, 0.11f, 0.16f, 0.55f));
        overlay.SetAsLastSibling();
        overlay.GetComponent<Image>().raycastTarget = true;
        return overlay.gameObject.AddComponent<V02ResultPresentation>();
    }

    public void BindSprites(Sprite[] success, Sprite[] fail, Sprite sparkle, Sprite candy, Sprite bang, Sprite sweat)
    {
        BindSprites(success, fail, sparkle, candy, bang, sweat, capturedSprite);
    }

    public void BindSprites(Sprite[] success, Sprite[] fail, Sprite sparkle, Sprite candy, Sprite bang, Sprite sweat, Sprite captured)
    {
        if (success != null && success.Length > 0)
            successFrames = success;
        if (fail != null && fail.Length > 0)
            failFrames = fail;
        if (sparkle != null)
            sparkleSprite = sparkle;
        if (candy != null)
            candySprite = candy;
        if (bang != null)
            bangSprite = bang;
        if (sweat != null)
            sweatSprite = sweat;
        if (captured != null)
            capturedSprite = captured;
    }

    void Awake()
    {
        EnsureBuilt();
        if (CurrentPhase == Phase.Idle)
            HideImmediate();
    }

    void EnsureBuilt()
    {
        if (canvasGroup == null)
            Build((RectTransform)transform);
    }

    void Build(RectTransform overlay)
    {
        canvasGroup = overlay.GetComponent<CanvasGroup>();
        if (canvasGroup == null)
            canvasGroup = overlay.gameObject.AddComponent<CanvasGroup>();

        skipCatcher = overlay.GetComponent<Button>();
        if (skipCatcher == null)
            skipCatcher = overlay.gameObject.AddComponent<Button>();
        skipCatcher.transition = Selectable.Transition.None;
        skipCatcher.onClick.RemoveAllListeners();
        skipCatcher.onClick.AddListener(SkipToHold);

        panel = V02Ui.CreatePanel("Panel", overlay, new Color(1f, 0.96f, 0.90f, 1f));
        V02Ui.SetAnchors(panel, new Vector2(0.07f, 0.18f), new Vector2(0.93f, 0.86f), Vector2.zero, Vector2.zero);

        flashImage = V02Ui.CreatePanel("Flash", overlay, new Color(1f, 0.35f, 0.32f, 0f)).GetComponent<Image>();
        flashImage.raycastTarget = false;

        reactionImage = CreateImage("Reaction", panel);
        V02Ui.SetAnchors((RectTransform)reactionImage.transform, new Vector2(0.5f, 0.74f), new Vector2(0.5f, 0.74f), new Vector2(-120f, -10f), new Vector2(120f, 220f));
        reactionImage.preserveAspect = true;
        reactionImage.raycastTarget = false;
        StoreDefaultReactionLayout();

        fxRoot = new GameObject("FxRoot", typeof(RectTransform)).transform;
        fxRoot.SetParent(panel, false);
        V02Ui.Stretch((RectTransform)fxRoot);
        ((RectTransform)fxRoot).SetAsLastSibling();

        titleText = V02Ui.CreateText("Title", panel, "", 44f, V02Ui.Ink, TextAlignmentOptions.Center);
        V02Ui.SetAnchors(titleText.rectTransform, new Vector2(0.08f, 0.52f), new Vector2(0.92f, 0.62f), Vector2.zero, Vector2.zero);
        titleText.textWrappingMode = TextWrappingModes.Normal;
        titleText.overflowMode = TextOverflowModes.Overflow;

        RectTransform bodyHost = V02Ui.CreatePanel("BodyHost", panel, new Color(1f, 1f, 1f, 0f));
        V02Ui.SetAnchors(bodyHost, new Vector2(0.08f, 0.28f), new Vector2(0.92f, 0.50f), Vector2.zero, Vector2.zero);
        Image bodyHostImage = bodyHost.GetComponent<Image>();
        bodyHostImage.raycastTarget = true;
        bodyScroll = bodyHost.gameObject.AddComponent<ScrollRect>();
        bodyScroll.horizontal = false;
        bodyScroll.vertical = true;
        bodyScroll.movementType = ScrollRect.MovementType.Clamped;
        RectTransform viewport = V02Ui.CreateStretch("Viewport", bodyHost);
        viewport.gameObject.AddComponent<RectMask2D>();
        bodyScroll.viewport = viewport;
        bodyText = V02Ui.CreateText("Body", viewport, "", 30f, V02Ui.Ink, TextAlignmentOptions.Top);
        RectTransform bodyRect = bodyText.rectTransform;
        bodyRect.anchorMin = new Vector2(0f, 1f);
        bodyRect.anchorMax = new Vector2(1f, 1f);
        bodyRect.pivot = new Vector2(0.5f, 1f);
        bodyRect.anchoredPosition = Vector2.zero;
        bodyRect.sizeDelta = Vector2.zero;
        bodyText.textWrappingMode = TextWrappingModes.Normal;
        bodyText.overflowMode = TextOverflowModes.Overflow;
        ContentSizeFitter bodyFit = bodyText.gameObject.AddComponent<ContentSizeFitter>();
        bodyFit.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        bodyScroll.content = bodyRect;

        deltaText = V02Ui.CreateText("Deltas", panel, "", 28f, V02Ui.Ink, TextAlignmentOptions.Center);
        V02Ui.SetAnchors(deltaText.rectTransform, new Vector2(0.08f, 0.16f), new Vector2(0.92f, 0.27f), Vector2.zero, Vector2.zero);
        deltaText.textWrappingMode = TextWrappingModes.Normal;
        deltaText.overflowMode = TextOverflowModes.Overflow;

        continueButton = V02Ui.CreateButton("Continue", panel, "계속", V02Ui.Yellow, OnContinueClicked);
        V02Ui.SetAnchors((RectTransform)continueButton.transform, new Vector2(0.18f, 0.04f), new Vector2(0.82f, 0.14f), Vector2.zero, Vector2.zero);
        continueLabel = continueButton.GetComponentInChildren<TextMeshProUGUI>();
        if (continueLabel != null)
            continueLabel.fontSize = 36f;
    }

    static Image CreateImage(string name, Transform parent)
    {
        RectTransform rect = V02Ui.CreatePanel(name, parent, Color.white);
        Image image = rect.GetComponent<Image>();
        image.color = Color.white;
        image.raycastTarget = false;
        return image;
    }

    public void Present(PresentedResolve resolve, string body, string presentationId)
    {
        current = resolve;
        currentBody = body ?? string.Empty;
        currentPresentationId = presentationId ?? string.Empty;
        endedContinue = resolve != null && resolve.RunEnded;
        if (routine != null)
            StopCoroutine(routine);
        EnsureBuilt();
        gameObject.SetActive(true);
        CurrentPhase = Phase.Anticipating;
        if (isActiveAndEnabled && gameObject.activeInHierarchy && Application.isPlaying)
            routine = StartCoroutine(PlayRoutine(body, presentationId));
        else
            EnterHold();
    }

    public bool TryContinue()
    {
        if (CurrentPhase != Phase.Holding)
            return false;
        OnContinueClicked();
        return true;
    }

    public void SkipToHold()
    {
        if (!CanSkip)
            return;
        if (routine != null)
            StopCoroutine(routine);
        EnterHold();
    }

    IEnumerator PlayRoutine(string body, string presentationId)
    {
        CurrentPhase = Phase.Anticipating;
        EnsureBuilt();
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 1f;
            canvasGroup.blocksRaycasts = true;
            canvasGroup.interactable = true;
        }
        if (continueButton != null)
            continueButton.gameObject.SetActive(false);
        if (skipCatcher != null)
            skipCatcher.interactable = true;
        SetText(titleText, "…");
        SetText(bodyText, string.Empty);
        SetText(deltaText, string.Empty);
        if (reactionImage != null)
        {
            reactionImage.enabled = true;
            reactionImage.preserveAspect = true;
            reactionImage.sprite = UseCapturedReaction() ? capturedSprite : FrameAt(failFrames, 0);
            if (reactionImage.sprite == null)
                reactionImage.sprite = FrameAt(successFrames, 0);
            reactionImage.color = new Color(1f, 1f, 1f, 0.35f);
            ApplyReactionLayout(UseCapturedReaction() && reactionImage.sprite == capturedSprite);
        }

        float waited = 0f;
        while (waited < anticipateSeconds)
        {
            waited += Time.unscaledDeltaTime;
            yield return null;
        }

        CurrentPhase = Phase.Revealing;
        bool success = current != null && current.Success;
        ApplyPresentation(presentationId, success, body);
        float t = 0f;
        Vector3 baseScale = panel != null ? Vector3.one : Vector3.one;
        while (t < revealSeconds)
        {
            t += Time.unscaledDeltaTime;
            float u = Mathf.Clamp01(t / revealSeconds);
            float bounce = 1f + (bounceScale - 1f) * Mathf.Sin(u * Mathf.PI);
            if (titleText != null)
                titleText.rectTransform.localScale = Vector3.one * bounce;
            if (panel != null && !success)
                panel.anchoredPosition = new Vector2(Mathf.Sin(u * 28f) * (12f * (1f - u)), 0f);
            if (reactionImage != null)
                reactionImage.color = new Color(1f, 1f, 1f, Mathf.Lerp(0.35f, 1f, u));
            yield return null;
        }

        if (titleText != null)
            titleText.rectTransform.localScale = Vector3.one;
        if (panel != null)
            panel.anchoredPosition = Vector2.zero;
        EnterHold();
    }

    void ApplyPresentation(string presentationId, bool success, string body)
    {
        string title = success ? "성공!" : "실패…";
        if (current != null && current.RunEnded)
        {
            if (current.Status == RunStatus.Won)
                title = "성공! · 승리";
            else if (current.FailReason == RunFailReason.AlertLimit)
                title = "들켰다!";
            else if (current.FailReason == RunFailReason.NoTurns)
                title = "실패… · 턴";
            else
                title = "실패… · 종료";
        }

        SetText(titleText, title);
        SetText(bodyText, body);
        SetText(deltaText, FormatDeltas(current));
        if (bodyScroll != null)
            bodyScroll.verticalNormalizedPosition = 1f;

        if (reactionImage != null)
        {
            bool capture = UseCapturedReaction();
            if (capture)
                reactionImage.sprite = capturedSprite;
            else
            {
                Sprite[] frames = success ? successFrames : failFrames;
                reactionImage.sprite = FrameAt(frames, frames != null && frames.Length > 1 ? 1 : 0);
            }

            reactionImage.preserveAspect = true;
            reactionImage.enabled = reactionImage.sprite != null;
            ApplyReactionLayout(capture && reactionImage.sprite == capturedSprite);
        }

        if (titleText != null)
            titleText.color = current != null && current.FailReason == RunFailReason.AlertLimit
                ? new Color(0.72f, 0.12f, 0.18f)
                : (success ? new Color(0.22f, 0.45f, 0.28f) : new Color(0.62f, 0.18f, 0.22f));

        ClearFx();
        bool alertFx = presentationId == FailAlert || presentationId == RunFail || (current != null && current.AlertDelta > 0 && !success);
        bool candyFx = current != null && current.CandyDelta > 0;
        if (success)
        {
            SpawnBurst(sparkleSprite, 5, 80f, 160f);
            if (candyFx)
                SpawnBurst(candySprite, 4, 40f, 140f);
        }
        else
        {
            SpawnBurst(sweatSprite, 3, 90f, 40f);
            if (alertFx)
            {
                SpawnBurst(bangSprite, 2, 120f, 70f);
                StartCoroutine(AlertFlash());
            }
        }
    }

    void EnterHold()
    {
        CurrentPhase = Phase.Holding;
        EnsureBuilt();
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 1f;
            canvasGroup.blocksRaycasts = true;
            canvasGroup.interactable = true;
        }

        if (skipCatcher != null)
            skipCatcher.interactable = false;
        if (continueButton != null)
        {
            continueButton.gameObject.SetActive(true);
            continueButton.interactable = true;
        }

        if (titleText != null)
            titleText.rectTransform.localScale = Vector3.one;
        if (panel != null)
            panel.anchoredPosition = Vector2.zero;
        if (reactionImage != null)
            reactionImage.color = Color.white;
        if (current != null && (titleText == null || string.IsNullOrEmpty(titleText.text) || titleText.text == "…"))
            ApplyPresentation(currentPresentationId, current.Success, currentBody);
    }

    bool UseCapturedReaction()
    {
        return current != null
            && current.RunEnded
            && current.FailReason == RunFailReason.AlertLimit
            && capturedSprite != null;
    }

    void StoreDefaultReactionLayout()
    {
        if (reactionImage == null || storedDefaultReaction)
            return;
        RectTransform rect = (RectTransform)reactionImage.transform;
        defaultReactionAnchorMin = rect.anchorMin;
        defaultReactionAnchorMax = rect.anchorMax;
        defaultReactionOffsetMin = rect.offsetMin;
        defaultReactionOffsetMax = rect.offsetMax;
        storedDefaultReaction = true;
    }

    void ApplyReactionLayout(bool capture)
    {
        if (reactionImage == null)
            return;
        StoreDefaultReactionLayout();
        RectTransform rect = (RectTransform)reactionImage.transform;
        if (capture)
            V02Ui.SetAnchors(rect, captureReactionMin, captureReactionMax, Vector2.zero, Vector2.zero);
        else if (storedDefaultReaction)
            V02Ui.SetAnchors(rect, defaultReactionAnchorMin, defaultReactionAnchorMax, defaultReactionOffsetMin, defaultReactionOffsetMax);
        reactionImage.preserveAspect = true;
    }

    public static string ResolveId(PresentedResolve resolve)
    {
        if (resolve == null)
            return FailDefault;
        if (resolve.RunEnded && resolve.Status == RunStatus.Won)
            return RunWin;
        if (resolve.RunEnded)
            return RunFail;
        return string.Empty;
    }

    void OnContinueClicked()
    {
        if (CurrentPhase != Phase.Holding)
            return;

        bool ended = endedContinue;
        HideImmediate();
        if (Continued != null)
            Continued.Invoke();
        if (ended)
            EndedRunContinued.Invoke();
    }

    public void HideImmediate()
    {
        if (routine != null)
        {
            StopCoroutine(routine);
            routine = null;
        }

        CurrentPhase = Phase.Idle;
        current = null;
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0f;
            canvasGroup.blocksRaycasts = false;
            canvasGroup.interactable = false;
        }

        if (skipCatcher != null)
            skipCatcher.interactable = false;
        if (continueButton != null)
            continueButton.gameObject.SetActive(false);
    }

    IEnumerator AlertFlash()
    {
        if (flashImage == null)
            yield break;
        float t = 0f;
        while (t < 0.35f)
        {
            t += Time.unscaledDeltaTime;
            float a = Mathf.Sin(t / 0.35f * Mathf.PI) * 0.28f;
            flashImage.color = new Color(1f, 0.28f, 0.25f, a);
            yield return null;
        }

        flashImage.color = new Color(1f, 0.28f, 0.25f, 0f);
    }

    void SpawnBurst(Sprite sprite, int count, float spreadX, float startY)
    {
        if (sprite == null || fxRoot == null)
            return;
        for (int i = 0; i < count; i++)
            StartCoroutine(BurstOne(sprite, spreadX, startY, i));
    }

    IEnumerator BurstOne(Sprite sprite, float spreadX, float startY, int index)
    {
        var go = new GameObject("Fx", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        go.transform.SetParent(fxRoot, false);
        RectTransform rect = (RectTransform)go.transform;
        Image image = go.GetComponent<Image>();
        image.sprite = sprite;
        image.preserveAspect = true;
        image.raycastTarget = false;
        float x = ((index % 2 == 0) ? -1f : 1f) * (40f + (index + 1) * 18f);
        if (x > spreadX)
            x = spreadX;
        if (x < -spreadX)
            x = -spreadX;
        rect.anchoredPosition = new Vector2(x, startY);
        rect.sizeDelta = new Vector2(56f, 56f);
        float t = 0f;
        while (t < 0.7f)
        {
            t += Time.unscaledDeltaTime;
            float u = t / 0.7f;
            rect.anchoredPosition = new Vector2(x, startY + 90f * u);
            image.color = new Color(1f, 1f, 1f, 1f - u);
            rect.localScale = Vector3.one * (1f + 0.25f * u);
            yield return null;
        }

        Destroy(go);
    }

    void ClearFx()
    {
        if (fxRoot == null)
            return;
        for (int i = fxRoot.childCount - 1; i >= 0; i--)
            Destroy(fxRoot.GetChild(i).gameObject);
    }

    static Sprite FrameAt(Sprite[] frames, int index)
    {
        if (frames == null || frames.Length == 0)
            return null;
        if (index < 0)
            index = 0;
        if (index >= frames.Length)
            index = frames.Length - 1;
        return frames[index];
    }

    static void SetText(TextMeshProUGUI label, string value)
    {
        if (label != null)
            label.text = value ?? string.Empty;
    }

    public static string FormatDeltas(PresentedResolve resolve)
    {
        if (resolve == null)
            return string.Empty;
        var parts = new System.Collections.Generic.List<string>();
        if (resolve.CandyDelta != 0)
            parts.Add("사탕 " + Signed(resolve.CandyDelta));
        if (resolve.AlertDelta != 0)
            parts.Add("위험도 " + Signed(resolve.AlertDelta));
        if (resolve.TurnDelta != 0)
            parts.Add("턴 " + Signed(resolve.TurnDelta));
        if (parts.Count == 0)
            return "변화 없음";
        return string.Join(" · ", parts.ToArray());
    }

    static string Signed(int value)
    {
        return value > 0 ? "+" + value : value.ToString();
    }
}
