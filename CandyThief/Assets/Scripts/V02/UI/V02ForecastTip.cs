using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public sealed class V02ForecastTip : MonoBehaviour, IPointerClickHandler
{
    [SerializeField] GameObject layer;
    [SerializeField] RectTransform panel;
    [SerializeField] TextMeshProUGUI label;

    public bool IsOpen { get { return layer != null && layer.activeSelf; } }
    public string Label { get { return label != null ? label.text : string.Empty; } }
    public V02ChoiceButton Owner { get; private set; }
    public RectTransform Panel { get { return panel; } }

    public void Show(V02ChoiceButton source, string body)
    {
        Owner = source;
        if (label != null)
            label.text = body ?? string.Empty;
        if (layer != null)
            layer.SetActive(true);
    }

    public void Hide()
    {
        Owner = null;
        if (layer != null)
            layer.SetActive(false);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData == null || EventSystem.current == null)
        {
            Hide();
            return;
        }

        var hits = new List<RaycastResult>();
        EventSystem.current.RaycastAll(eventData, hits);
        var objects = new List<GameObject>(hits.Count);
        for (int i = 0; i < hits.Count; i++)
            objects.Add(hits[i].gameObject);
        Route(objects);
    }

    public bool Route(IList<GameObject> hits)
    {
        if (hits != null)
        {
            for (int i = 0; i < hits.Count; i++)
            {
                GameObject hit = hits[i];
                if (hit == null || hit == gameObject)
                    continue;
                if (IsPanel(hit))
                    return true;
                if (hit.name != "CandyInfo" && hit.name != "AlertInfo")
                    continue;
                Button info = hit.GetComponent<Button>();
                if (info == null)
                    continue;
                info.onClick.Invoke();
                return true;
            }
        }

        Hide();
        return false;
    }

    bool IsPanel(GameObject hit)
    {
        if (panel == null || hit == null)
            return false;
        return hit.transform == panel || hit.transform.IsChildOf(panel);
    }
}
