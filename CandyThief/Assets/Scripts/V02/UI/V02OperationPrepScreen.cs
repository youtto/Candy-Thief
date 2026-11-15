using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

// OP-DEV-06 작전 준비 화면. 2×2 카드와 버튼 배치는 프리팹이 정한다.
// 런타임은 ShopListManager 의 목록·리롤·런 시작만 연결하고 RectTransform 은 바꾸지 않는다.
public sealed class V02OperationPrepScreen : MonoBehaviour
{
    public const string ResourcePath = "V02/UI/OperationPrep";
    // 5: M4-6 COMP-OperationPrep-v003 재구현. Builder 는 이 값보다 낮은 프리팹만 다시 만든다.
    public const int CurrentLayout = 5;

    [SerializeField] int layoutVersion;
    [SerializeField] V02OperationPrepCard[] cards;
    [SerializeField] Button rerollButton;
    [SerializeField] TextMeshProUGUI rerollLabel;
    [SerializeField] Button startButton;
    [SerializeField] Button backButton;
    [SerializeField] TextMeshProUGUI messageText;

    V02LocalProfileService profiles;
    CsvDatabase database;
    IIntRoll roll;
    UnityAction<string> onStart;
    string selectedShopId = string.Empty;

    public RectTransform Rect => (RectTransform)transform;
    public V02OperationPrepCard[] Cards => cards;
    public Button RerollButton => rerollButton;
    public Button StartButton => startButton;
    public string SelectedShopId => selectedShopId;
    public string Message => messageText != null ? messageText.text : string.Empty;
    public string RerollCount => rerollLabel != null ? rerollLabel.text : string.Empty;
    public int LayoutVersion => layoutVersion;

    public bool Open(V02LocalProfileService profileService, CsvDatabase csv, UnityAction<string> start, UnityAction back, IIntRoll intRoll = null)
    {
        profiles = profileService;
        database = csv;
        onStart = start;
        roll = intRoll;
        selectedShopId = string.Empty;
        Wire(back);
        return Apply(Manager(), string.Empty);
    }

    void Wire(UnityAction back)
    {
        if (rerollButton != null)
        {
            rerollButton.onClick.RemoveAllListeners();
            rerollButton.onClick.AddListener(OnReroll);
        }

        if (startButton != null)
        {
            startButton.onClick.RemoveAllListeners();
            startButton.onClick.AddListener(OnStart);
        }

        if (backButton != null)
        {
            backButton.onClick.RemoveAllListeners();
            if (back != null)
                backButton.onClick.AddListener(back);
        }

        if (cards == null)
            return;

        for (int i = 0; i < cards.Length; i++)
        {
            if (cards[i] == null || cards[i].Button == null)
                continue;
            int index = i;
            cards[i].Button.onClick.RemoveAllListeners();
            cards[i].Button.onClick.AddListener(() => OnCard(index));
        }
    }

    void OnCard(int index)
    {
        if (cards == null || index < 0 || index >= cards.Length || cards[index] == null)
            return;
        if (string.IsNullOrEmpty(cards[index].ShopId))
            return;

        selectedShopId = cards[index].ShopId;
        Apply(Manager(), string.Empty);
    }

    void OnReroll()
    {
        ShopListManager manager = Manager();
        ShopListResult result = manager.TryReroll();
        if (result.Ok)
        {
            selectedShopId = string.Empty;
            Apply(manager, string.Empty);
            return;
        }

        string message = result.Status == ShopListOpStatus.LimitReached
            ? "무료 리롤을 모두 사용했습니다."
            : "리롤에 실패했습니다. 목록은 그대로입니다.";
        Apply(manager, message);
    }

    void OnStart()
    {
        if (string.IsNullOrEmpty(selectedShopId))
        {
            SetMessage("가게를 선택하세요.");
            return;
        }

        if (onStart != null)
            onStart.Invoke(selectedShopId);
    }

    bool Apply(ShopListManager manager, string message)
    {
        if (manager == null || cards == null || cards.Length != 4
            || !manager.EnsureList().Ok || manager.CurrentShopIds.Count != 4)
        {
            ClearCards();
            SetReroll(false, 0, 0);
            SetMessage(string.IsNullOrEmpty(message) ? "가게 목록을 만들지 못했습니다." : message);
            if (startButton != null)
                startButton.interactable = false;
            return false;
        }

        IReadOnlyList<string> ids = manager.CurrentShopIds;
        bool selectedStillThere = false;
        for (int i = 0; i < cards.Length; i++)
        {
            string shopId = ids[i];
            ShopRow shop;
            if (!database.TryGetShop(shopId, out shop))
            {
                cards[i].Bind(null, false);
                continue;
            }

            if (shopId == selectedShopId)
                selectedStillThere = true;

            List<string> rumorIds = manager.GetRumorIds(shopId);
            var lines = new List<string>();
            for (int r = 0; r < rumorIds.Count; r++)
            {
                ShopRumorRow rumor;
                if (database.TryGetRumor(rumorIds[r], out rumor))
                    lines.Add(V02OperationPrepCard.RumorLine(rumor));
            }

            V02ShopCardData data = V02ShopCardData.FromShop(shop, lines, ResolveBackground(shop.BackgroundImageKey));
            cards[i].Bind(data, shopId == selectedShopId);
        }

        if (!selectedStillThere)
            selectedShopId = string.Empty;

        int freeUsed, freeLimit, totalUsed, totalLimit;
        bool budget = manager.TryReadRerollBudget(out freeUsed, out freeLimit, out totalUsed, out totalLimit);
        int remaining = budget ? freeLimit - freeUsed : 0;
        if (remaining < 0)
            remaining = 0;
        bool canReroll = budget && V02OperationPrepCard.CanFreeReroll(freeUsed, freeLimit, totalUsed, totalLimit);
        SetReroll(canReroll, remaining, budget ? freeLimit : 0);
        if (startButton != null)
            startButton.interactable = selectedShopId.Length > 0;
        SetMessage(message);
        return true;
    }

    void ClearCards()
    {
        if (cards == null)
            return;
        for (int i = 0; i < cards.Length; i++)
        {
            if (cards[i] != null)
                cards[i].Bind(null, false);
        }
    }

    public static string FormatRefresh(int remaining, int limit)
    {
        if (remaining < 0)
            remaining = 0;
        if (limit < 0)
            limit = 0;
        return "(" + remaining.ToString(System.Globalization.CultureInfo.InvariantCulture)
            + "/" + limit.ToString(System.Globalization.CultureInfo.InvariantCulture) + ")";
    }

    void SetReroll(bool interactable, int remaining, int limit)
    {
        string text = FormatRefresh(remaining, limit);
        if (rerollButton != null)
        {
            rerollButton.interactable = interactable;
            TextMeshProUGUI buttonText = rerollButton.GetComponentInChildren<TextMeshProUGUI>(true);
            if (buttonText != null)
                buttonText.text = text;
        }

        if (rerollLabel == null)
            return;
        bool separate = rerollButton == null || !rerollLabel.transform.IsChildOf(rerollButton.transform);
        if (separate)
            rerollLabel.gameObject.SetActive(false);
        else
            rerollLabel.text = text;
    }

    void SetMessage(string message)
    {
        if (messageText != null)
            messageText.text = message ?? string.Empty;
    }

    ShopListManager Manager()
    {
        return new ShopListManager(profiles, database, roll);
    }

    Sprite ResolveBackground(string imageKey)
    {
        return V02ShopSprites.LoadByImageKey(database, imageKey);
    }
}
