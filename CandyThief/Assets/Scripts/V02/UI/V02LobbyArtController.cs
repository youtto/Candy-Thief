using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Lobby art presentation for Title. Owns sprites and character display size.
/// Pivot is the feet so idle breath/sway can keep the soles planted.
/// Does not replace V02AppFlow screen flow.
/// </summary>
[DefaultExecutionOrder(-20)]
public sealed class V02LobbyArtController : MonoBehaviour
{
    const float RefW = 940f;
    const float DesignW = 100f;
    const float DesignH = 130f;
    const float Nx = 0.4894f;
    const float NyFromTop = 0.5356f;

    [SerializeField] Sprite lobbyBackground;
    [SerializeField] Sprite thiefIdle;

    [Header("Character Scale")]
    [SerializeField, Range(0.5f, 2f)]
    [Tooltip("1.0 = 114.89 x 149.36 at 1080-wide lobby. 1.2 ≈ 138 x 179. Aspect locked. Idle must not overwrite this.")]
    float characterScale = 1f;

    RectTransform playerRect;
    Vector2 baseSize;
    Vector2 centerRest;
    Vector2 restAnchoredPosition;
    bool feetRestLocked;

    public RectTransform PlayerRect { get { return playerRect; } }
    public Vector2 RestAnchoredPosition { get { return restAnchoredPosition; } }
    public float CharacterScale { get { return Mathf.Clamp(characterScale, 0.5f, 2f); } }

    void Start()
    {
        Transform lobby = transform.Find("Canvas/SafeArea/Lobby");
        if (lobby == null)
        {
            Debug.LogError("[V02LobbyArtController] Lobby 오브젝트를 찾지 못했습니다.");
            return;
        }

        Image bg = lobby.GetComponent<Image>();
        if (bg != null && lobbyBackground != null)
        {
            bg.sprite = lobbyBackground;
            bg.color = Color.white;
            bg.type = Image.Type.Simple;
            bg.preserveAspect = true;
            bg.raycastTarget = false;
        }

        Transform player = lobby.Find("Player");
        if (player == null || thiefIdle == null)
        {
            Debug.LogError("[V02LobbyArtController] Player 또는 thiefIdle이 없습니다.");
            return;
        }

        var bob = player.GetComponent<V02IdleBob>();
        if (bob != null)
            bob.enabled = false;

        Image playerImage = player.GetComponent<Image>();
        playerRect = (RectTransform)player;
        playerImage.sprite = thiefIdle;
        playerImage.color = Color.white;
        playerImage.type = Image.Type.Simple;
        playerImage.preserveAspect = true;
        playerImage.raycastTarget = false;

        RectTransform lobbyRect = (RectTransform)lobby;
        float sx = lobbyRect.rect.width / RefW;
        baseSize = new Vector2(DesignW * sx, DesignH * sx);

        centerRest = new Vector2(
            (Nx - 0.5f) * lobbyRect.rect.width,
            (0.5f - NyFromTop) * lobbyRect.rect.height);

        playerRect.anchorMin = new Vector2(0.5f, 0.5f);
        playerRect.anchorMax = new Vector2(0.5f, 0.5f);
        playerRect.SetSiblingIndex(1);
        ApplyFeetLayout();
    }

    void Update()
    {
        ApplyFeetLayout();
    }

    void ApplyFeetLayout()
    {
        if (playerRect == null)
            return;

        playerRect.pivot = new Vector2(0.5f, 0f);
        playerRect.sizeDelta = baseSize * CharacterScale;

        if (!feetRestLocked && playerRect.sizeDelta.y > 1f)
        {
            restAnchoredPosition = centerRest - new Vector2(0f, playerRect.sizeDelta.y * 0.5f);
            feetRestLocked = true;
        }

        if (feetRestLocked)
            playerRect.anchoredPosition = restAnchoredPosition;
    }
}
