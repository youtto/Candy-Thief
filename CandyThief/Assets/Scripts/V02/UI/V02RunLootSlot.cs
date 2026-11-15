using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class V02RunLootSlot : MonoBehaviour
{
    [SerializeField] Image icon;
    [SerializeField] TextMeshProUGUI nameText;
    [SerializeField] TextMeshProUGUI countText;
    [SerializeField] TextMeshProUGUI keptText;

    public Image Icon { get { return icon; } }

    public void Bind(RunLootGrant grant)
    {
        if (grant == null)
            return;
        if (nameText != null)
            nameText.text = grant.DisplayName ?? string.Empty;
        if (countText != null)
            countText.text = "×" + grant.Count;
        if (keptText != null)
            keptText.text = grant.Kept ? "보유" : "획득";
    }
}
