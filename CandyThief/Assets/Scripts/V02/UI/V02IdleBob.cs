using UnityEngine;

public sealed class V02IdleBob : MonoBehaviour
{
    [SerializeField] float amplitudePixels = 3f;
    [SerializeField] float periodSeconds = 1.75f;

    RectTransform rect;
    Vector2 rest;

    void Awake()
    {
        rect = (RectTransform)transform;
        rest = rect.anchoredPosition;
    }

    void OnEnable()
    {
        if (rect == null)
            rect = (RectTransform)transform;
        rest = rect.anchoredPosition;
    }

    void Update()
    {
        if (rect == null || periodSeconds < 0.01f)
            return;

        float y = Mathf.Sin(Time.unscaledTime * (Mathf.PI * 2f / periodSeconds)) * amplitudePixels;
        rect.anchoredPosition = rest + new Vector2(0f, y);
    }
}
