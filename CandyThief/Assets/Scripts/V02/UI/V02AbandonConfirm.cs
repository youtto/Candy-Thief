using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

// 런 HUD 위의 포기 확인. 칸은 한 번만 만들고, 다시 열 때는 표시와 클릭만 바꾼다.
public sealed class V02AbandonConfirm : MonoBehaviour
{
    public const string TitleCopy = "작전을 포기할까?";
    public const string BodyCopy = "포기하면 이번 작전은 실패로 처리돼.";

    [SerializeField] TextMeshProUGUI titleText;
    [SerializeField] TextMeshProUGUI bodyText;
    [SerializeField] Button cancelButton;
    [SerializeField] Button confirmButton;

    public bool IsOpen { get { return gameObject.activeSelf; } }
    public Button CancelButton { get { return cancelButton; } }
    public Button ConfirmButton { get { return confirmButton; } }
    public string Title { get { return titleText != null ? titleText.text : string.Empty; } }
    public string Body { get { return bodyText != null ? bodyText.text : string.Empty; } }

    public static V02AbandonConfirm Ensure(RectTransform parent)
    {
        Transform found = parent.Find("AbandonConfirm");
        V02AbandonConfirm view = found != null ? found.GetComponent<V02AbandonConfirm>() : null;
        bool created = view == null;
        if (created)
        {
            RectTransform overlay = V02Ui.CreatePanel(
                "AbandonConfirm", parent, new Color(0.12f, 0.10f, 0.16f, 0.72f));
            view = overlay.gameObject.AddComponent<V02AbandonConfirm>();
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

        RectTransform overlay = (RectTransform)transform;
        RectTransform card = V02Ui.CreatePanel("Card", overlay, new Color(1f, 0.96f, 0.90f, 1f));
        V02Ui.SetAnchors(card, new Vector2(0.1f, 0.36f), new Vector2(0.9f, 0.64f), Vector2.zero, Vector2.zero);

        titleText = V02Ui.CreateText("Title", card, TitleCopy, 40f, V02Ui.Ink, TextAlignmentOptions.Center);
        V02Ui.SetAnchors(titleText.rectTransform, new Vector2(0.08f, 0.62f), new Vector2(0.92f, 0.90f), Vector2.zero, Vector2.zero);
        titleText.textWrappingMode = TextWrappingModes.Normal;

        bodyText = V02Ui.CreateText("Body", card, BodyCopy, 30f, V02Ui.Ink, TextAlignmentOptions.Center);
        V02Ui.SetAnchors(bodyText.rectTransform, new Vector2(0.08f, 0.38f), new Vector2(0.92f, 0.62f), Vector2.zero, Vector2.zero);
        bodyText.textWrappingMode = TextWrappingModes.Normal;

        cancelButton = V02Ui.CreateButton("Cancel", card, "취소", V02Ui.White, null);
        V02Ui.SetAnchors((RectTransform)cancelButton.transform, new Vector2(0.08f, 0.08f), new Vector2(0.46f, 0.30f), Vector2.zero, Vector2.zero);
        confirmButton = V02Ui.CreateButton("Confirm", card, "포기하기", V02Ui.Pink, null);
        V02Ui.SetAnchors((RectTransform)confirmButton.transform, new Vector2(0.54f, 0.08f), new Vector2(0.92f, 0.30f), Vector2.zero, Vector2.zero);
    }

    public void Open(UnityAction onCancel, UnityAction onConfirm)
    {
        EnsureBuilt();
        Bind(cancelButton, onCancel);
        Bind(confirmButton, onConfirm);
        SetInteractable(true);
        gameObject.SetActive(true);
        transform.SetAsLastSibling();
    }

    public void Close()
    {
        gameObject.SetActive(false);
    }

    public void SetInteractable(bool interactable)
    {
        if (cancelButton != null)
            cancelButton.interactable = interactable;
        if (confirmButton != null)
            confirmButton.interactable = interactable;
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
