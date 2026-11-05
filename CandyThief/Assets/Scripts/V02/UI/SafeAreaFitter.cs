using UnityEngine;

[RequireComponent(typeof(RectTransform))]
public class SafeAreaFitter : MonoBehaviour
{
    RectTransform rect;
    Rect lastSafe;
    Vector2Int lastSize;

    void Awake()
    {
        rect = (RectTransform)transform;
        Apply();
    }

    void Update()
    {
        if (Screen.safeArea != lastSafe || Screen.width != lastSize.x || Screen.height != lastSize.y)
            Apply();
    }

    void Apply()
    {
        lastSafe = Screen.safeArea;
        lastSize = new Vector2Int(Screen.width, Screen.height);

        Rect safe = Screen.safeArea;
        float w = Mathf.Max(Screen.width, 1);
        float h = Mathf.Max(Screen.height, 1);
        rect.anchorMin = new Vector2(safe.xMin / w, safe.yMin / h);
        rect.anchorMax = new Vector2(safe.xMax / w, safe.yMax / h);
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }
}
