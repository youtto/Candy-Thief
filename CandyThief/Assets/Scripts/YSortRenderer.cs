using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class YSortRenderer : MonoBehaviour
{
    [SerializeField] Transform sortOrigin;
    [SerializeField] float sortPointOffset;
    [SerializeField] int orderOffset;

    SpriteRenderer spriteRenderer;

    void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (sortOrigin == null)
            sortOrigin = transform;
    }

    void LateUpdate()
    {
        float sortY = sortOrigin.position.y + sortPointOffset;
        spriteRenderer.sortingOrder = orderOffset - Mathf.RoundToInt(sortY * 100f);
    }

    public void Configure(Transform origin, float verticalOffset, int baseOrder = 0)
    {
        sortOrigin = origin;
        sortPointOffset = verticalOffset;
        orderOffset = baseOrder;
    }
}
