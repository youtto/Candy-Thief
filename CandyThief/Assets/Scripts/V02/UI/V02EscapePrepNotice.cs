using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

// 가게 진입 전 탈출 준비 안내. 턴·위험도·사탕은 바꾸지 않는다.
public sealed class V02EscapePrepNotice : MonoBehaviour
{
    public const string TitleCopy = "아직 탈출을 준비할 수 없어!";
    public const string BodyCopy = "사탕 가게에 들어간 다음부터 탈출을 준비할 수 있어.";

    [SerializeField] TextMeshProUGUI titleText;
    [SerializeField] TextMeshProUGUI bodyText;
    [SerializeField] Button confirmButton;

    public bool IsOpen { get { return gameObject.activeSelf; } }
    public Button ConfirmButton { get { return confirmButton; } }

    public static V02EscapePrepNotice Ensure(RectTransform parent)
    {
        Transform found = parent.Find("EscapePrepNotice");
        V02EscapePrepNotice view = found != null ? found.GetComponent<V02EscapePrepNotice>() : null;
        bool created = view == null;
        if (created)
        {
            RectTransform overlay = V02Ui.CreatePanel(
                "EscapePrepNotice", parent, new Color(0.12f, 0.10f, 0.16f, 0.72f));
            view = overlay.gameObject.AddComponent<V02EscapePrepNotice>();
        }

        view.EnsureBuilt();
        if (created)
            view.gameObject.SetActive(false);
        return view;
    }

    void EnsureBuilt()
    {
        if (titleText != null)
            return;

        RectTransform card = V02Ui.CreatePanel("Card", (RectTransform)transform, new Color(1f, 0.96f, 0.90f, 1f));
        V02Ui.SetAnchors(card, new Vector2(0.1f, 0.38f), new Vector2(0.9f, 0.64f), Vector2.zero, Vector2.zero);
        titleText = V02Ui.CreateText("Title", card, TitleCopy, 36f, V02Ui.Ink, TextAlignmentOptions.Center);
        V02Ui.SetAnchors(titleText.rectTransform, new Vector2(0.08f, 0.58f), new Vector2(0.92f, 0.90f), Vector2.zero, Vector2.zero);
        titleText.textWrappingMode = TextWrappingModes.Normal;
        bodyText = V02Ui.CreateText("Body", card, BodyCopy, 28f, V02Ui.Ink, TextAlignmentOptions.Center);
        V02Ui.SetAnchors(bodyText.rectTransform, new Vector2(0.08f, 0.34f), new Vector2(0.92f, 0.58f), Vector2.zero, Vector2.zero);
        bodyText.textWrappingMode = TextWrappingModes.Normal;
        confirmButton = V02Ui.CreateButton("Confirm", card, "확인", V02Ui.Yellow, null);
        V02Ui.SetAnchors((RectTransform)confirmButton.transform, new Vector2(0.22f, 0.08f), new Vector2(0.78f, 0.28f), Vector2.zero, Vector2.zero);
    }

    public void Open(UnityAction onConfirm)
    {
        EnsureBuilt();
        if (titleText != null)
            titleText.text = TitleCopy;
        if (bodyText != null)
            bodyText.text = BodyCopy;
        if (confirmButton != null)
        {
            confirmButton.onClick.RemoveAllListeners();
            if (onConfirm != null)
                confirmButton.onClick.AddListener(onConfirm);
        }

        gameObject.SetActive(true);
        transform.SetAsLastSibling();
    }

    public void Close()
    {
        gameObject.SetActive(false);
    }
}
