using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Visual pressed scale for UI buttons. Does not change screen flow.
/// </summary>
public sealed class V02UiPressScale : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
{
    [SerializeField] float pressedScale = 0.94f;

    Vector3 restScale = Vector3.one;

    void Awake()
    {
        restScale = transform.localScale;
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        transform.localScale = restScale * pressedScale;
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        transform.localScale = restScale;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        transform.localScale = restScale;
    }
}
