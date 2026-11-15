using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

// 사탕 0개로 LOOT을 떠날 때의 확인. 닫기만 하면 런은 그대로다.
public sealed class V02EmptyHandEscapeConfirm : MonoBehaviour
{
    public const string TitleCopy = "빈손으로 탈출할까?";
    public const string BodyCopy = "아직 사탕을 하나도 훔치지 못했어. 지금 탈출하면 작전은 실패로 처리돼. 그래도 탈출할까?";
    public const string LeaveCopy = "탈출하기";
    public const string StayCopy = "계속 훔치기";

    [SerializeField] TextMeshProUGUI titleText;
    [SerializeField] TextMeshProUGUI bodyText;
    [SerializeField] Button stayButton;
    [SerializeField] Button leaveButton;

    public bool IsOpen { get { return gameObject.activeSelf; } }
    public Button StayButton { get { return stayButton; } }
    public Button LeaveButton { get { return leaveButton; } }
    public string Title { get { return titleText != null ? titleText.text : string.Empty; } }
    public string Body { get { return bodyText != null ? bodyText.text : string.Empty; } }

    public static V02EmptyHandEscapeConfirm Ensure(RectTransform parent)
    {
        Transform found = parent.Find("EmptyHandEscapeConfirm");
        V02EmptyHandEscapeConfirm view = found != null ? found.GetComponent<V02EmptyHandEscapeConfirm>() : null;
        bool created = view == null;
        if (created)
        {
            RectTransform overlay = V02Ui.CreatePanel(
                "EmptyHandEscapeConfirm", parent, new Color(0.12f, 0.10f, 0.16f, 0.72f));
            view = overlay.gameObject.AddComponent<V02EmptyHandEscapeConfirm>();
        }

        view.EnsureBuilt();
        if (created)
            view.gameObject.SetActive(false);
        return view;
    }

    void Awake()
    {
        EnsureBuilt();
    }

    void EnsureBuilt()
    {
        if (titleText != null)
            return;

        RectTransform card = V02Ui.CreatePanel("Card", (RectTransform)transform, new Color(1f, 0.96f, 0.90f, 1f));
        V02Ui.SetAnchors(card, new Vector2(0.08f, 0.32f), new Vector2(0.92f, 0.70f), Vector2.zero, Vector2.zero);
        titleText = V02Ui.CreateText("Title", card, TitleCopy, 36f, V02Ui.Ink, TextAlignmentOptions.Center);
        V02Ui.SetAnchors(titleText.rectTransform, new Vector2(0.08f, 0.72f), new Vector2(0.92f, 0.92f), Vector2.zero, Vector2.zero);
        titleText.textWrappingMode = TextWrappingModes.Normal;
        bodyText = V02Ui.CreateText("Body", card, BodyCopy, 28f, V02Ui.Ink, TextAlignmentOptions.Center);
        V02Ui.SetAnchors(bodyText.rectTransform, new Vector2(0.08f, 0.34f), new Vector2(0.92f, 0.70f), Vector2.zero, Vector2.zero);
        bodyText.textWrappingMode = TextWrappingModes.Normal;
        stayButton = V02Ui.CreateButton("Stay", card, StayCopy, V02Ui.White, null);
        V02Ui.SetAnchors((RectTransform)stayButton.transform, new Vector2(0.06f, 0.08f), new Vector2(0.48f, 0.28f), Vector2.zero, Vector2.zero);
        leaveButton = V02Ui.CreateButton("Leave", card, LeaveCopy, V02Ui.Yellow, null);
        V02Ui.SetAnchors((RectTransform)leaveButton.transform, new Vector2(0.52f, 0.08f), new Vector2(0.94f, 0.28f), Vector2.zero, Vector2.zero);
    }

    public void Open(UnityAction onStay, UnityAction onLeave)
    {
        EnsureBuilt();
        if (titleText != null)
            titleText.text = TitleCopy;
        if (bodyText != null)
            bodyText.text = BodyCopy;
        Bind(stayButton, onStay);
        Bind(leaveButton, onLeave);
        gameObject.SetActive(true);
        transform.SetAsLastSibling();
    }

    public void Close()
    {
        gameObject.SetActive(false);
    }

    static void Bind(Button button, UnityAction action)
    {
        if (button == null)
            return;
        button.onClick.RemoveAllListeners();
        if (action != null)
            button.onClick.AddListener(action);
    }
}
