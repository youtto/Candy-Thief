using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Lobby blink: swaps the existing Player Image between Open and Closed sprites.
/// Does not change sizeDelta, pivot, or anchoredPosition. Does not create Images.
/// </summary>
[DefaultExecutionOrder(0)]
public sealed class V02LobbyBlinkAnimator : MonoBehaviour
{
    [SerializeField] Sprite openSprite;
    [SerializeField] Sprite closedSprite;

    [Header("Blink Timing")]
    [SerializeField, Min(0f)]
    [Tooltip("Minimum seconds to keep Open before a blink.")]
    float blinkIntervalMin = 3f;

    [SerializeField, Min(0f)]
    [Tooltip("Maximum seconds to keep Open before a blink.")]
    float blinkIntervalMax = 6f;

    [SerializeField, Min(0f)]
    [Tooltip("Seconds to show Closed before returning to Open.")]
    float closedDuration = 0.1f;

    [SerializeField]
    [Tooltip("When enabled, blink starts once the lobby Player Image is available.")]
    bool playOnEnable = true;

    V02LobbyArtController art;
    Image playerImage;
    Coroutine loop;
    bool loopActive;

    void OnEnable()
    {
        TryStart();
    }

    void Start()
    {
        TryStart();
    }

    void OnDisable()
    {
        Halt(false);
    }

    void Update()
    {
        Bind();
        if (CanBlink())
        {
            if (playOnEnable && !loopActive)
                BeginLoop();
        }
        else if (loopActive)
        {
            Halt(true);
        }
    }

    void TryStart()
    {
        Bind();
        if (playOnEnable && CanBlink() && !loopActive)
            BeginLoop();
    }

    void Bind()
    {
        if (art == null)
            art = GetComponent<V02LobbyArtController>();
        if (art == null)
            return;

        Image image = art.PlayerImage;
        if (image == null)
            playerImage = null;
        else
            playerImage = image;
    }

    bool HasImage()
    {
        return playerImage != null;
    }

    bool CanBlink()
    {
        return playOnEnable
            && HasImage()
            && playerImage.gameObject.activeInHierarchy
            && openSprite != null
            && closedSprite != null;
    }

    void BeginLoop()
    {
        Halt(true);
        if (!CanBlink())
            return;
        loop = StartCoroutine(BlinkLoop());
        loopActive = true;
    }

    void Halt(bool resetToOpen)
    {
        if (loop != null)
        {
            StopCoroutine(loop);
            loop = null;
        }

        loopActive = false;
        if (resetToOpen)
            ApplySprite(openSprite);
    }

    System.Collections.IEnumerator BlinkLoop()
    {
        while (CanBlink())
        {
            ApplySprite(openSprite);
            yield return new WaitForSecondsRealtime(NextOpenInterval());
            if (!CanBlink())
                break;

            ApplySprite(closedSprite);
            yield return new WaitForSecondsRealtime(ClosedHold());
            if (!CanBlink())
                break;

            ApplySprite(openSprite);
        }

        loop = null;
        loopActive = false;
        ApplySprite(openSprite);
    }

    void ApplySprite(Sprite sprite)
    {
        if (!HasImage() || sprite == null)
            return;
        playerImage.sprite = sprite;
    }

    float NextOpenInterval()
    {
        float a = Mathf.Max(0f, blinkIntervalMin);
        float b = Mathf.Max(0f, blinkIntervalMax);
        if (b < a)
        {
            float swap = a;
            a = b;
            b = swap;
        }

        return Random.Range(a, b);
    }

    float ClosedHold()
    {
        return Mathf.Max(0f, closedDuration);
    }
}
