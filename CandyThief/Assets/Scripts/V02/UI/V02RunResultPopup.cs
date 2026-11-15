using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

// 작전 결과 4종. 칸은 프리팹에 두고, 런타임에는 문구와 색만 바꾼다.
public sealed class V02RunResultPopup : MonoBehaviour
{
    public const string ResourcePath = "V02/UI/RunResultPopup";
    public const string EmptyLootCopy = "획득한 전리품이 없어요.";

    [SerializeField] Image backdrop;
    [SerializeField] Image frame;
    [SerializeField] Image decor;
    [SerializeField] TextMeshProUGUI titleText;
    [SerializeField] TextMeshProUGUI subtitleText;
    [SerializeField] Image characterImage;
    [SerializeField] TextMeshProUGUI characterPlaceholder;
    [SerializeField] Image messagePlate;
    [SerializeField] TextMeshProUGUI messageText;
    [SerializeField] Image statsPlate;
    [SerializeField] TextMeshProUGUI acquiredValue;
    [SerializeField] TextMeshProUGUI settledValue;
    [SerializeField] TextMeshProUGUI alertValue;
    [SerializeField] TextMeshProUGUI turnsText;
    [SerializeField] RectTransform lootContent;
    [SerializeField] TextMeshProUGUI emptyLootText;
    [SerializeField] Button retryButton;
    [SerializeField] Button lobbyButton;

    readonly List<V02RunLootSlot> slots = new List<V02RunLootSlot>();

    public UnityEvent RetryClicked = new UnityEvent();
    public UnityEvent LobbyClicked = new UnityEvent();

    public bool IsOpen { get { return gameObject.activeSelf; } }
    public string Title { get { return titleText != null ? titleText.text : string.Empty; } }
    public string Subtitle { get { return subtitleText != null ? subtitleText.text : string.Empty; } }
    public string Message { get { return messageText != null ? messageText.text : string.Empty; } }
    public string AcquiredLabel { get { return acquiredValue != null ? acquiredValue.text : string.Empty; } }
    public string SettledLabel { get { return settledValue != null ? settledValue.text : string.Empty; } }
    public string AlertLabel { get { return alertValue != null ? alertValue.text : string.Empty; } }
    public string TurnsLabel { get { return turnsText != null ? turnsText.text : string.Empty; } }
    public string EmptyLootLabel { get { return emptyLootText != null ? emptyLootText.text : string.Empty; } }
    public bool EmptyLootVisible { get { return emptyLootText != null && emptyLootText.gameObject.activeSelf; } }
    public Image CharacterImage { get { return characterImage; } }
    public Button RetryButton { get { return retryButton; } }
    public Button LobbyButton { get { return lobbyButton; } }

    public void Present(RunState run, IReadOnlyList<RunLootGrant> loot)
    {
        if (run == null)
            return;

        RunResultCopy copy = RunResultCopy.For(run.Outcome);
        Color theme = copy.Theme;
        if (backdrop != null)
            backdrop.color = new Color(theme.r, theme.g, theme.b, 1f);
        if (frame != null)
            frame.color = Color.Lerp(theme, Color.white, 0.55f);
        if (decor != null)
            decor.color = Color.Lerp(theme, Color.white, 0.25f);
        if (messagePlate != null)
            messagePlate.color = new Color(1f, 0.98f, 0.94f, 0.96f);
        if (statsPlate != null)
            statsPlate.color = new Color(1f, 0.97f, 0.90f, 1f);
        if (characterImage != null)
        {
            characterImage.color = characterImage.sprite != null ? Color.white : Color.Lerp(theme, Color.white, 0.35f);
            if (characterPlaceholder != null)
                characterPlaceholder.gameObject.SetActive(characterImage.sprite == null);
        }

        Set(titleText, copy.Title);
        Set(subtitleText, copy.Subtitle);
        Set(messageText, copy.Message);
        Set(acquiredValue, Signed(run.Candy));
        Set(settledValue, Signed(run.SettledCandy));
        int maxAlert = run.AlertMax > 0 ? run.AlertMax : 100;
        Set(alertValue, run.Alert + "/" + maxAlert);
        Set(turnsText, "남은 턴: " + run.RemainingTurns + "턴");
        BindLoot(loot);
        Bind(retryButton, RetryClicked);
        Bind(lobbyButton, LobbyClicked);
        gameObject.SetActive(true);
    }

    public void Hide()
    {
        gameObject.SetActive(false);
    }

    public void CollectSlots(List<V02RunLootSlot> destination)
    {
        destination.Clear();
        if (lootContent == null)
            return;
        var found = lootContent.GetComponentsInChildren<V02RunLootSlot>(true);
        for (int i = 0; i < found.Length; i++)
            destination.Add(found[i]);
    }

    void BindLoot(IReadOnlyList<RunLootGrant> loot)
    {
        slots.Clear();
        CollectSlots(slots);
        int count = loot != null ? loot.Count : 0;
        while (count > slots.Count && slots.Count > 0 && lootContent != null)
        {
            V02RunLootSlot copy = Instantiate(slots[0], lootContent);
            copy.name = "Slot" + slots.Count;
            slots.Add(copy);
        }

        if (emptyLootText != null)
        {
            emptyLootText.text = EmptyLootCopy;
            emptyLootText.gameObject.SetActive(count == 0);
        }

        for (int i = 0; i < slots.Count; i++)
        {
            bool show = i < count;
            slots[i].gameObject.SetActive(show);
            if (show)
                slots[i].Bind(loot[i]);
        }
    }

    static void Bind(Button button, UnityEvent callback)
    {
        if (button == null)
            return;
        button.onClick.RemoveAllListeners();
        if (callback != null)
            button.onClick.AddListener(callback.Invoke);
    }

    static void Set(TextMeshProUGUI label, string value)
    {
        if (label != null)
            label.text = value ?? string.Empty;
    }

    static string Signed(int value)
    {
        return value > 0 ? "+" + value : value.ToString();
    }

    struct RunResultCopy
    {
        public string Title;
        public string Subtitle;
        public string Message;
        public Color Theme;

        public static RunResultCopy For(RunOutcome outcome)
        {
            if (outcome == RunOutcome.OperationSuccess)
            {
                return new RunResultCopy
                {
                    Title = "작전 성공!",
                    Subtitle = "사탕을 왕창 훔쳤어요!",
                    Message = "가게의 사탕을 잔뜩 챙겨 무사히 탈출했어요!",
                    Theme = new Color(0.55f, 0.86f, 0.62f, 1f)
                };
            }

            if (outcome == RunOutcome.EmptyEscape)
            {
                return new RunResultCopy
                {
                    Title = "탈출 성공!",
                    Subtitle = "그런데 빈손이네...",
                    Message = "무사히 탈출하긴 했지만... 사탕은 하나도 못 챙겼어요.",
                    Theme = new Color(0.55f, 0.78f, 0.95f, 1f)
                };
            }

            if (outcome == RunOutcome.Captured)
            {
                return new RunResultCopy
                {
                    Title = "들켰다!",
                    Subtitle = "경비에게 붙잡혔어요!",
                    Message = "경비에게 들켜 현장에서 체포되었어요!",
                    Theme = new Color(0.93f, 0.45f, 0.48f, 1f)
                };
            }

            return new RunResultCopy
            {
                Title = "탈출 실패 !",
                Subtitle = "탈출 기회를 놓쳐버렸다!",
                Message = "우물쭈물하다 기회를 다 써버렸어요. 망했다 !",
                Theme = new Color(0.62f, 0.52f, 0.86f, 1f)
            };
        }
    }
}
