using UnityEngine;
using UnityEngine.Serialization;

/// <summary>
/// Lobby idle: breathe by Scale Y from the feet, sway by rotation around the feet.
/// Does not change sizeDelta (Character Scale) or anchoredPosition.
/// </summary>
[DefaultExecutionOrder(10)]
public sealed class V02LobbyIdleAnimator : MonoBehaviour
{
    [Header("Idle Breath")]
    [SerializeField, Range(0f, 0.05f)]
    [Tooltip("Breath Scale Amount. Scale Y goes from 1.0 to 1.0 + this value.")]
    float breathScaleAmount = 0.015f;

    [FormerlySerializedAs("breathPeriodSeconds")]
    [SerializeField, Min(0.1f)]
    [Tooltip("Breath Period in seconds.")]
    float breathPeriod = 2.2f;

    [Header("Idle Sway")]
    [FormerlySerializedAs("swayAngleDegrees")]
    [SerializeField, Range(0f, 2f)]
    [Tooltip("Sway Angle in degrees. Applied as ± this value around the feet.")]
    float swayAngle = 0.5f;

    [FormerlySerializedAs("swayPeriodSeconds")]
    [SerializeField, Min(0.1f)]
    [Tooltip("Sway Period in seconds.")]
    float swayPeriod = 2.4f;

    V02LobbyArtController art;

    void Awake()
    {
        art = GetComponent<V02LobbyArtController>();
    }

    void Update()
    {
        if (art == null)
            art = GetComponent<V02LobbyArtController>();

        RectTransform rect = art != null ? art.PlayerRect : null;
        if (rect == null)
            return;

        float t = Time.unscaledTime;
        float breathT = EaseInOut01(t, breathPeriod);
        float scaleY = Mathf.Lerp(1f, 1f + Mathf.Max(0f, breathScaleAmount), breathT);
        float sway = Wave(t, swayPeriod) * Mathf.Min(swayAngle, 2f);

        rect.localScale = new Vector3(1f, scaleY, 1f);
        rect.localRotation = Quaternion.Euler(0f, 0f, sway);
    }

    static float EaseInOut01(float time, float period)
    {
        if (period < 0.1f)
            return 0f;
        return (1f - Mathf.Cos(time * (Mathf.PI * 2f / period))) * 0.5f;
    }

    static float Wave(float time, float period)
    {
        if (period < 0.1f)
            return 0f;
        return Mathf.Sin(time * (Mathf.PI * 2f / period));
    }
}
